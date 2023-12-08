using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

#if !UNITY_WEBGL || UNITY_EDITOR
using Best.WebSockets.Implementations.Frames;
#endif

namespace Best.WebSockets.Implementations
{
    /// <summary>
    /// States of the underlying implementation's state.
    /// </summary>
    public enum WebSocketStates : byte
    {
        Connecting = 0,
        Open = 1,
        Closing = 2,
        Closed = 3,
        Unknown
    };

    public delegate void OnWebSocketOpenDelegate(WebSocket webSocket);
    public delegate void OnWebSocketMessageDelegate(WebSocket webSocket, string message);
    public delegate void OnWebSocketBinaryNoAllocDelegate(WebSocket webSocket, BufferSegment data);
    public delegate void OnWebSocketClosedDelegate(WebSocket webSocket, WebSocketStatusCodes code, string message);

#if !UNITY_WEBGL || UNITY_EDITOR
    public delegate void OnWebSocketIncompleteFrameDelegate(WebSocket webSocket, WebSocketFrameReader frame);
#endif

    /// <summary>
    /// Abstract class for concrete websocket communication implementations.
    /// </summary>
    public abstract class WebSocketBaseImplementation
    {
        /// <summary>
        /// Capacity of the RTT buffer where the latencies are kept.
        /// </summary>
        public static int RTTBufferCapacity = 5;

        public const string Timing_Name = "Websocket";

        public virtual WebSocketStates State { get; protected set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        public virtual bool IsOpen { get; protected set; }

        public virtual int BufferedAmount { get; protected set; }
#else
        public bool IsOpen => this.State == WebSocketStates.Open;

        public int BufferedAmount { get => this._bufferedAmount; }
        protected volatile int _bufferedAmount;

        public HTTP.HTTPRequest InternalRequest
        {
            get
            {
                if (this._internalRequest == null)
                    CreateInternalRequest();

                return this._internalRequest;
            }
        }
        protected HTTP.HTTPRequest _internalRequest;

        public virtual int Latency { get; protected set; }
        public virtual DateTime LastMessageReceived { get; protected set; }

        /// <summary>
        /// A circular buffer to store the last N rtt times calculated by the pong messages.
        /// </summary>
        protected CircularBuffer<int> rtts = new CircularBuffer<int>(WebSocketBaseImplementation.RTTBufferCapacity);

        /// <summary>
        /// When we sent out the last ping.
        /// </summary>
        protected DateTime lastPing = DateTime.MinValue;

        protected bool waitingForPong = false;

        protected List<WebSocketFrameReader> IncompleteFrames = new List<WebSocketFrameReader>();
        protected PeekableIncomingSegmentStream incomingSegmentStream = new PeekableIncomingSegmentStream();
        protected ConcurrentQueue<WebSocketFrameReader> CompletedFrames = new ConcurrentQueue<WebSocketFrameReader>();
        protected ConcurrentQueue<WebSocketFrame> frames = new ConcurrentQueue<WebSocketFrame>();

        /// <summary>
        /// True if we sent out a Close message to the server
        /// </summary>
        internal volatile bool _closeSent;
        internal volatile bool _closeReceived;
#endif

        public WebSocket Parent { get; }
        public Uri Uri { get; protected set; }
        public string Origin { get; }
        public string Protocol { get; }

        public WebSocketBaseImplementation(WebSocket parent, Uri uri, string origin, string protocol)
        {
            this.Parent = parent;
            this.Uri = uri;
            this.Origin = origin;
            this.Protocol = protocol;

#if !UNITY_WEBGL || UNITY_EDITOR
            this.LastMessageReceived = DateTime.MinValue;

            // Set up some default values.
            this.Parent.PingFrequency = TimeSpan.FromMilliseconds(10_000);
            this.Parent.CloseAfterNoMessage = TimeSpan.FromSeconds(2);
#endif
        }

        public abstract void StartOpen();
        public abstract void StartClose(WebSocketStatusCodes code, string message);

        public abstract void Send(string message);
        public abstract void Send(byte[] buffer);
        public abstract void Send(byte[] buffer, ulong offset, ulong count);
        public abstract void SendAsBinary(BufferSegment data);
        public abstract void SendAsText(BufferSegment data);

#if !UNITY_WEBGL || UNITY_EDITOR

        protected void ParseExtensionResponse(HTTP.HTTPResponse resp)
        {
            if (this.Parent.Extensions != null)
            {
                for (int i = 0; i < this.Parent.Extensions.Length; ++i)
                {
                    var ext = this.Parent.Extensions[i];

                    try
                    {
                        if (ext != null && !ext.ParseNegotiation(resp))
                            this.Parent.Extensions[i] = null; // Keep extensions only that successfully negotiated
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("WebSocketBaseImplementation", "ParseNegotiation", ex, this.Parent.Context);

                        // Do not try to use a defective extension in the future
                        this.Parent.Extensions[i] = null;
                    }
                }
            }
        }

        protected abstract void CreateInternalRequest();

        /// <summary>
        /// It will send the given frame to the server.
        /// </summary>
        public abstract void Send(WebSocketFrame frame);

        protected virtual void Cleanup()
        {
            for (int i = 0; i < this.IncompleteFrames.Count; ++i)
            {
                var frame = this.IncompleteFrames[i];
                BufferPool.Release(frame.Data);
            }
            this.IncompleteFrames.Clear();
            this.Parent.DisposeExtensions();
        }

        protected int CalculateLatency()
        {
            if (this.rtts.Count == 0)
                return 0;

            int sumLatency = 0;
            for (int i = 0; i < this.rtts.Count; ++i)
                sumLatency += this.rtts[i];

            return sumLatency / this.rtts.Count;
        }

        public static bool CanReadFullFrame(PeekableStream stream)
        {
            if (stream.Length < 2)
                return false;

            stream.BeginPeek();

            if (stream.PeekByte() == -1)
                return false;

            int maskAndLength = stream.PeekByte();
            if (maskAndLength == -1)
                return false;

            // The second byte is the Mask Bit and the length of the payload data
            var HasMask = (maskAndLength & 0x80) != 0;

            // if 0-125, that is the payload length.
            var Length = (UInt64)(maskAndLength & 127);

            // If 126, the following 2 bytes interpreted as a 16-bit unsigned integer are the payload length.
            if (Length == 126)
            {
                byte[] rawLen = BufferPool.Get(2, true);

                for (int i = 0; i < 2; i++)
                {
                    int data = stream.PeekByte();
                    if (data < 0)
                        return false;

                    rawLen[i] = (byte)data;
                }

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(rawLen, 0, 2);

                Length = (UInt64)BitConverter.ToUInt16(rawLen, 0);

                BufferPool.Release(rawLen);
            }
            else if (Length == 127)
            {
                // If 127, the following 8 bytes interpreted as a 64-bit unsigned integer (the
                // most significant bit MUST be 0) are the payload length.

                byte[] rawLen = BufferPool.Get(8, true);

                for (int i = 0; i < 8; i++)
                {
                    int data = stream.PeekByte();
                    if (data < 0)
                        return false;

                    rawLen[i] = (byte)data;
                }

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(rawLen, 0, 8);

                Length = (UInt64)BitConverter.ToUInt64(rawLen, 0);

                BufferPool.Release(rawLen);
            }

            // Header + Mask&Length
            Length += 2;

            // 4 bytes for Mask if present
            if (HasMask)
                Length += 4;

            return stream.Length >= (long)Length;
        }
#endif
    }
}
