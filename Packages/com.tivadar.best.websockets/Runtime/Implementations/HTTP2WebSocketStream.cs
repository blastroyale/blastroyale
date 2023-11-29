#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2 && !BESTHTTP_DISABLE_WEBSOCKET
using System;
using System.Collections.Generic;

using Best.HTTP;
using Best.HTTP.Hosts.Connections.HTTP2;
using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.WebSockets.Implementations
{
    public sealed class HTTP2WebSocketStream : HTTP2Stream
    {
        public override bool HasFrameToSend
        {
            get
            {
                // Don't let the connection sleep until
                return this.outgoing.Count > 0 || // we already booked at least one frame in advance
                       (this.State == HTTP2StreamStates.Open &&
                        this.remoteWindow > 0 &&
                        this.lastReadCount > 0 &&
                        (this.websocketOverHTTP2.BufferedFramesCount > 0 || this.chunkQueue.Count > 0)); // we are in the middle of sending request data
            }
        }

        public override TimeSpan NextInteraction => this.websocketOverHTTP2.GetNextInteraction();

        private OverHTTP2 websocketOverHTTP2;

        // local list of websocket header-data pairs
        private List<KeyValuePair<BufferSegment, BufferSegment>> chunkQueue = new List<KeyValuePair<BufferSegment, BufferSegment>>();

        public HTTP2WebSocketStream(uint id, HTTP2ContentConsumer parentHandler, HTTP2SettingsManager registry, HPACKEncoder hpackEncoder)
            : base(id, parentHandler, registry, hpackEncoder)
        {}

        public override void Assign(HTTPRequest request)
        {
            base.Assign(request);

            this.websocketOverHTTP2 = request.Tag as OverHTTP2;
            this.websocketOverHTTP2.SetThreadSignaler(this._parentHandler);
        }

        protected override void ProcessIncomingDATAFrame(ref HTTP2FrameHeaderAndPayload frame)
        {
            try
            {
                if (this.State != HTTP2StreamStates.HalfClosedLocal && this.State != HTTP2StreamStates.Open)
                {
                    // ERROR!
                    return;
                }

                this.downloaded += (uint)frame.Payload.Count;

                this.websocketOverHTTP2.OnReadThread(frame.Payload);

                // frame's buffer will be released later
                frame.DontUseMemPool = true;

                this.localWindow -= frame.Payload.Count;

                if ((frame.Flags & (byte)HTTP2DataFlags.END_STREAM) != 0)
                    this.isEndSTRReceived = true;

                if (this.isEndSTRReceived)
                {
                    HTTPManager.Logger.Information(nameof(HTTP2WebSocketStream), string.Format("[{0}] All data arrived, data length: {1:N0}", this.Id, this.downloaded), this.Context);

                    FinishRequest();

                    if (this.State == HTTP2StreamStates.HalfClosedLocal)
                        this.State = HTTP2StreamStates.Closed;
                    else
                        this.State = HTTP2StreamStates.HalfClosedRemote;
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTP2WebSocketStream), nameof(ProcessIncomingDATAFrame), ex, this.Context);
            }
        }

        protected override void ProcessOpenState(List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            try
            {
                // remote Window can be negative! See https://httpwg.org/specs/rfc7540.html#InitialWindowSize
                if (this.remoteWindow <= 0)
                {
                    HTTPManager.Logger.Information(nameof(HTTP2WebSocketStream), string.Format("[{0}] Skipping data sending as remote Window is {1}!", this.Id, this.remoteWindow), this.Context);
                    return;
                }

                this.websocketOverHTTP2.PreReadCallback();

                Int64 maxFragmentSize = Math.Min(Best.WebSockets.WebSocket.MaxFragmentSize, this.settings.RemoteSettings[HTTP2Settings.MAX_FRAME_SIZE]);
                Int64 maxFrameSize = Math.Min(maxFragmentSize, this.remoteWindow);

                if (chunkQueue.Count == 0)
                {
                    if (this.websocketOverHTTP2.TryDequeueFrame(out var frame))
                        frame.WriteTo((header, data) => chunkQueue.Add(new KeyValuePair<BufferSegment, BufferSegment>(header, data)), (uint)maxFragmentSize, false, this.Context);
                }

                while (this.remoteWindow >= 6 && chunkQueue.Count > 0)
                {
                    var kvp = chunkQueue[0];

                    BufferSegment header = kvp.Key;
                    BufferSegment data = kvp.Value;

                    int minBytes = header.Count;
                    int maxBytes = minBytes + data.Count;

                    // remote window is less than the minimum we have to send, or
                    // the frame has data but we have space only to send the websocket header
                    if (this.remoteWindow < minBytes || (maxBytes > minBytes && this.remoteWindow == minBytes))
                        return;

                    HTTP2FrameHeaderAndPayload headerFrame = new HTTP2FrameHeaderAndPayload();
                    headerFrame.Type = HTTP2FrameTypes.DATA;
                    headerFrame.StreamId = this.Id;
                    
                    headerFrame.Payload = header;
                    headerFrame.DontUseMemPool = false;

                    if (data.Count > 0)
                    {
                        HTTP2FrameHeaderAndPayload dataFrame = new HTTP2FrameHeaderAndPayload();
                        dataFrame.Type = HTTP2FrameTypes.DATA;
                        dataFrame.StreamId = this.Id;

                        var buff = data.Slice(data.Offset, (int)Math.Min(data.Count, maxFrameSize));
                        
                        dataFrame.Payload = buff;

                        data = data.Slice(buff.Offset + buff.Count);
                        if (data.Count == 0)
                            chunkQueue.RemoveAt(0);
                        else
                            chunkQueue[0] = new KeyValuePair<BufferSegment, BufferSegment>(header, data);

                        // release the buffer only with the final frame and with the final frame's last data chunk
                        bool isLast = (header.Data[header.Offset] & 0x80) != 0 /*&& chunkQueue.Count == 0*/;
                        dataFrame.DontUseMemPool = !isLast;

                        this.outgoing.Enqueue(headerFrame);
                        this.outgoing.Enqueue(dataFrame);
                    }
                    else
                    {
                        this.outgoing.Enqueue(headerFrame);
                        chunkQueue.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTP2WebSocketStream), nameof(ProcessOpenState), ex, this.Context);
            }
        }
    }
}

#endif
