#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2

using System;
using System.Collections.Generic;

using Best.HTTP.Request.Upload;
using Best.HTTP.Request.Timings;
using Best.HTTP.Response;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Hosts.Connections.HTTP2
{
    // https://httpwg.org/specs/rfc7540.html#StreamStates
    //
    //                                      Idle
    //                                       |
    //                                       V
    //                                      Open
    //                Receive END_STREAM  /  |   \  Send END_STREAM
    //                                   v   |R   V
    //                  Half Closed Remote   |S   Half Closed Locale
    //                                   \   |T  /
    //     Send END_STREAM | RST_STREAM   \  |  /    Receive END_STREAM | RST_STREAM
    //     Receive RST_STREAM              \ | /     Send RST_STREAM
    //                                       V
    //                                     Closed
    // 
    // IDLE -> send headers -> OPEN -> send data -> HALF CLOSED - LOCAL -> receive headers -> receive Data -> CLOSED
    //               |                                     ^                      |                             ^
    //               +-------------------------------------+                      +-----------------------------+
    //                      END_STREAM flag present?                                   END_STREAM flag present?
    //

    public enum HTTP2StreamStates
    {
        Idle,
        //ReservedLocale,
        //ReservedRemote,
        Open,
        HalfClosedLocal,
        HalfClosedRemote,
        Closed
    }

    /// <summary>
    /// Implements an HTTP/2 logical stream.
    /// </summary>
    public class HTTP2Stream : IDownloadContentBufferAvailable
    {
        public UInt32 Id { get; private set; }

        public HTTP2StreamStates State {
            get { return this._state; }

            protected set {
                var oldState = this._state;

                this._state = value;

                if (oldState != this._state && HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] State changed from {1} to {2}", this.Id, oldState, this._state), this.Context);
            }
        }
        private HTTP2StreamStates _state;

        /// <summary>
        /// This flag is checked by the connection to decide whether to do a new processing-frame sending round before sleeping until new data arrives
        /// </summary>
        public virtual bool HasFrameToSend
        {
            get
            {
                // Don't let the connection sleep until
                return this.outgoing.Count > 0 || // we already booked at least one frame in advance
                       (this.State == HTTP2StreamStates.Open && this.remoteWindow > 0 && this.lastReadCount > 0); // we are in the middle of sending request data
            }
        }

        /// <summary>
        /// Next interaction scheduled by the stream relative to *now*. Its default is TimeSpan.MaxValue == no interaction.
        /// </summary>
        public virtual TimeSpan NextInteraction { get; } = TimeSpan.MaxValue;

        public HTTPRequest AssignedRequest { get; protected set; }

        public LoggingContext Context { get; protected set; }

        protected uint downloaded;

        protected HTTP2SettingsManager settings;
        protected HPACKEncoder encoder;

        // Outgoing frames. The stream will send one frame per Process call, but because one step might be able to
        // generate more than one frames, we use a list.
        protected Queue<HTTP2FrameHeaderAndPayload> outgoing = new Queue<HTTP2FrameHeaderAndPayload>();

        protected Queue<HTTP2FrameHeaderAndPayload> incomingFrames = new Queue<HTTP2FrameHeaderAndPayload>();

        protected FramesAsStreamView headerView;

        protected Int64 localWindow;
        protected Int64 remoteWindow;

        protected uint windowUpdateThreshold;

        protected UInt32 assignDataLength;

        protected long sentData;

        protected bool isRSTFrameSent;
        protected bool isEndSTRReceived;

        protected HTTP2Response response;

        protected int lastReadCount;

        protected HTTP2ContentConsumer _parentHandler;

        /// <summary>
        /// Constructor to create a client stream.
        /// </summary>
        public HTTP2Stream(UInt32 id, HTTP2ContentConsumer parentHandler, HTTP2SettingsManager registry, HPACKEncoder hpackEncoder)
        {
            this.Id = id;
            this._parentHandler = parentHandler;
            this.settings = registry;
            this.encoder = hpackEncoder;

            this.Context = new LoggingContext(this);
            this.Context.Add("id", id);
            this.Context.Add("Parent", parentHandler.Context);

            this.remoteWindow = this.settings.RemoteSettings[HTTP2Settings.INITIAL_WINDOW_SIZE];
            this.settings.RemoteSettings.OnSettingChangedEvent += OnRemoteSettingChanged;

            // Room for improvement: If INITIAL_WINDOW_SIZE is small (what we can consider a 'small' value?), threshold must be higher
            this.windowUpdateThreshold = (uint)(this.remoteWindow / 2);
        }

        public virtual void Assign(HTTPRequest request)
        {
            this.Context.Add("Request", request.Context);

            request.Timing.StartNext(TimingEventNames.Request_Sent);

            HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] Request assigned to stream. Remote Window: {1:N0}. Uri: {2}", this.Id, this.remoteWindow, request.CurrentUri.ToString()), this.Context);
            this.AssignedRequest = request;

            this.downloaded = 0;
        }

        public void Process(List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            if (this.AssignedRequest.IsCancellationRequested && !this.isRSTFrameSent)
            {
                HTTPManager.Logger.Information("HTTP2Stream", $"[{this.Id}] Process({this.State}) - IsCancellationRequested", this.Context);

                // These two are already set in HTTPRequest's Abort().
                //this.AssignedRequest.Response = null;
                //this.AssignedRequest.State = this.AssignedRequest.IsTimedOut ? HTTPRequestStates.TimedOut : HTTPRequestStates.Aborted;

                this.outgoing.Clear();
                if (this.State != HTTP2StreamStates.Idle)
                    this.outgoing.Enqueue(HTTP2FrameHelper.CreateRSTFrame(this.Id, HTTP2ErrorCodes.CANCEL, this.Context));

                // We can close the stream if already received headers, or not even sent one
                if (this.State == HTTP2StreamStates.HalfClosedRemote || this.State == HTTP2StreamStates.HalfClosedLocal || this.State == HTTP2StreamStates.Idle)
                    this.State = HTTP2StreamStates.Closed;

                this.isRSTFrameSent = true;
            }

            // 1.) Go through incoming frames
            ProcessIncomingFrames(outgoingFrames);

            // 2.) Create outgoing frames based on the stream's state and the request processing state.
            ProcessState(outgoingFrames);

            // 3.) Send one frame per Process call
            if (this.outgoing.Count > 0)
            {
                HTTP2FrameHeaderAndPayload frame = this.outgoing.Dequeue();

                outgoingFrames.Add(frame);

                // If END_Stream in header or data frame is present => half closed local
                if ((frame.Type == HTTP2FrameTypes.HEADERS && (frame.Flags & (byte)HTTP2HeadersFlags.END_STREAM) != 0) ||
                    (frame.Type == HTTP2FrameTypes.DATA && (frame.Flags & (byte)HTTP2DataFlags.END_STREAM) != 0))
                {
                    this.State = HTTP2StreamStates.HalfClosedLocal;
                }
            }
        }

        public void AddFrame(HTTP2FrameHeaderAndPayload frame, List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            // Room for improvement: error check for forbidden frames (like settings) and stream state

            this.incomingFrames.Enqueue(frame);

            ProcessIncomingFrames(outgoingFrames);
        }

        public void Abort(string msg)
        {
            HTTPManager.Logger.Information("HTTP2Stream", $"[{this.Id}] Abort(\"{msg}\", {this.State}, {this.AssignedRequest.State})", this.Context);

            if (this.State != HTTP2StreamStates.Closed)
            {
                if (this.AssignedRequest.State != HTTPRequestStates.Processing)
                {
                    // do nothing, its state is already set.
                }
                else if (this.AssignedRequest.IsCancellationRequested)
                {
                    // These two are already set in HTTPRequest's Abort().
                    //this.AssignedRequest.Response = null;
                    //this.AssignedRequest.State = this.AssignedRequest.IsTimedOut ? HTTPRequestStates.TimedOut : HTTPRequestStates.Aborted;

                    this.State = HTTP2StreamStates.Closed;
                }
                else if (this.AssignedRequest.RetrySettings.Retries >= this.AssignedRequest.RetrySettings.MaxRetries)
                {
                    this.AssignedRequest.Timing.StartNext(TimingEventNames.Queued_For_Disptach);
                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest, HTTPRequestStates.Error, new Exception(msg)));

                    this.State = HTTP2StreamStates.Closed;
                }
                else
                {
                    this.AssignedRequest.RetrySettings.Retries++;

                    this.AssignedRequest.Response?.Dispose();
                    this.AssignedRequest.Response = null;

                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest, RequestEvents.Resend));
                }
            }

            this.Removed();
        }

        protected void ProcessIncomingFrames(List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            while (this.incomingFrames.Count > 0)
            {
                HTTP2FrameHeaderAndPayload frame = this.incomingFrames.Dequeue();

                if ((this.isRSTFrameSent || this.AssignedRequest.IsCancellationRequested) && frame.Type != HTTP2FrameTypes.HEADERS && frame.Type != HTTP2FrameTypes.CONTINUATION)
                {
                    BufferPool.Release(frame.Payload);
                    continue;
                }

                if (/*HTTPManager.Logger.Level == Logger.Loglevels.All && */frame.Type != HTTP2FrameTypes.DATA && frame.Type != HTTP2FrameTypes.WINDOW_UPDATE)
                    HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] Process - processing frame: {1}", this.Id, frame.ToString()), this.Context);

                switch (frame.Type)
                {
                    case HTTP2FrameTypes.HEADERS:
                    case HTTP2FrameTypes.CONTINUATION:
                        if (this.State != HTTP2StreamStates.HalfClosedLocal && this.State != HTTP2StreamStates.Open && this.State != HTTP2StreamStates.Idle)
                        {
                            // ERROR!
                            continue;
                        }

                        // payload will be released by the view
                        frame.DontUseMemPool = true;

                        if (this.headerView == null)
                        {
                            this.AssignedRequest.Timing.StartNext(TimingEventNames.Headers);

                            this.headerView = new FramesAsStreamView(new HeaderFrameView());
                        }

                        this.headerView.AddFrame(frame);

                        // END_STREAM may arrive sooner than an END_HEADERS, so we have to store that we already received it
                        if ((frame.Flags & (byte)HTTP2HeadersFlags.END_STREAM) != 0)
                            this.isEndSTRReceived = true;

                        if ((frame.Flags & (byte)HTTP2HeadersFlags.END_HEADERS) != 0)
                        {
                            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();

                            try
                            {
                                this.encoder.Decode(this, this.headerView, headers);
                            }
                            catch(Exception ex)
                            {
                                HTTPManager.Logger.Exception("HTTP2Stream", string.Format("[{0}] ProcessIncomingFrames - Header Frames: {1}, Encoder: {2}", this.Id, this.headerView.ToString(), this.encoder.ToString()), ex, this.Context);
                            }

                            this.headerView.Close();
                            this.headerView = null;

                            this.AssignedRequest.Timing.StartNext(TimingEventNames.Response_Received);

                            if (this.isRSTFrameSent)
                            {
                                this.State = HTTP2StreamStates.Closed;
                                break;
                            }

                            if (this.response == null)
                                this.AssignedRequest.Response = this.response = new HTTP2Response(this.AssignedRequest, false);

                            this.response.AddHeaders(headers);

                            if (this.isEndSTRReceived)
                            {
                                // If there's any trailing header, no data frame has an END_STREAM flag
                                this.response.FinishProcessData();

                                FinishRequest();

                                if (this.State == HTTP2StreamStates.HalfClosedLocal)
                                    this.State = HTTP2StreamStates.Closed;
                                else
                                    this.State = HTTP2StreamStates.HalfClosedRemote;
                            }
                        }
                        break;

                    case HTTP2FrameTypes.DATA:
                        ProcessIncomingDATAFrame(ref frame);
                        break;

                    case HTTP2FrameTypes.WINDOW_UPDATE:
                        HTTP2WindowUpdateFrame windowUpdateFrame = HTTP2FrameHelper.ReadWindowUpdateFrame(frame);

                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] Received Window Update: {1:N0}, new remoteWindow: {2:N0}, initial remote window: {3:N0}, total data sent: {4:N0}", this.Id, windowUpdateFrame.WindowSizeIncrement, this.remoteWindow + windowUpdateFrame.WindowSizeIncrement, this.settings.RemoteSettings[HTTP2Settings.INITIAL_WINDOW_SIZE], this.sentData), this.Context);

                        this.remoteWindow += windowUpdateFrame.WindowSizeIncrement;
                        break;

                    case HTTP2FrameTypes.RST_STREAM:
                        // https://httpwg.org/specs/rfc7540.html#RST_STREAM

                        // It's possible to receive an RST_STREAM on a closed stream. In this case, we have to ignore it.
                        if (this.State == HTTP2StreamStates.Closed)
                            break;

                        var rstStreamFrame = HTTP2FrameHelper.ReadRST_StreamFrame(frame);

                        //HTTPManager.Logger.Error("HTTP2Stream", string.Format("[{0}] RST Stream frame ({1}) received in state {2}!", this.Id, rstStreamFrame, this.State), this.Context);

                        Abort(string.Format("RST_STREAM frame received! Error code: {0}({1})", rstStreamFrame.Error.ToString(), rstStreamFrame.ErrorCode));
                        break;

                    default:
                        HTTPManager.Logger.Warning("HTTP2Stream", string.Format("[{0}] Unexpected frame ({1}, Payload: {2}) in state {3}!", this.Id, frame, frame.PayloadAsHex(), this.State), this.Context);
                        break;
                }

                if (!frame.DontUseMemPool)
                    BufferPool.Release(frame.Payload);
            }
        }

        void IDownloadContentBufferAvailable.BufferAvailable(DownloadContentStream stream)
        {
            // Signal the http2 thread, window update will be sent out in ProcessOpenState.
            this._parentHandler.SignalThread();
        }

        protected virtual void ProcessIncomingDATAFrame(ref HTTP2FrameHeaderAndPayload frame)
        {
            if (this.State != HTTP2StreamStates.HalfClosedLocal && this.State != HTTP2StreamStates.Open)
            {
                // ERROR!
                return;
            }

            HTTP2DataFrame dataFrame = HTTP2FrameHelper.ReadDataFrame(frame);

            this.downloaded += (uint)dataFrame.Data.Count;

            this.response.Prepare(this);

            this.response.ProcessData(dataFrame.Data);
            frame.DontUseMemPool = true;

            // Because of padding, frame.Payload.Count can be larger than dataFrame.Data.Count!
            // "The entire DATA frame payload is included in flow control, including the Pad Length and Padding fields if present."
            this.localWindow -= frame.Payload.Count;

            this.isEndSTRReceived = (frame.Flags & (byte)HTTP2DataFlags.END_STREAM) != 0;

            if (this.isEndSTRReceived)
            {
               this.response.FinishProcessData();

                HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] All data arrived, data length: {1:N0}", this.Id, this.downloaded), this.Context);

                FinishRequest();

                if (this.State == HTTP2StreamStates.HalfClosedLocal)
                    this.State = HTTP2StreamStates.Closed;
                else
                    this.State = HTTP2StreamStates.HalfClosedRemote;
            }
            else if (this.AssignedRequest.DownloadSettings.OnDownloadProgress != null)
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest,
                                                                     RequestEvents.DownloadProgress,
                                                                     downloaded,
                                                                     this.response.ExpectedContentLength));
        }

        protected void ProcessState(List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            switch (this.State)
            {
                case HTTP2StreamStates.Idle:
                    // hpack encode the request's headers
                    this.encoder.Encode(this, this.AssignedRequest, this.outgoing, this.Id);

                    // HTTP/2 uses DATA frames to carry message payloads.
                    // The chunked transfer encoding defined in Section 4.1 of [RFC7230] MUST NOT be used in HTTP/2.

                    if (this.AssignedRequest.UploadSettings.UploadStream == null)
                    {
                        this.State = HTTP2StreamStates.HalfClosedLocal;
                        //this.AssignedRequest.Timing.Finish(TimingEventNames.Request_Sent);
                        this.AssignedRequest.Timing.StartNext(TimingEventNames.Waiting_TTFB);
                    }
                    else
                    {
                        this.State = HTTP2StreamStates.Open;
                        this.lastReadCount = 1;

                        if (this.AssignedRequest.UploadSettings.UploadStream is UploadStreamBase upStream)
                            upStream.BeforeSendBody(this.AssignedRequest, this._parentHandler);
                    }

                    // Change the initial window size to the request's DownloadSettings.ContentStreamMaxBuffered and send it to the server.
                    //  After this initial setup the sending out window_update frames faces two problems:
                    //      1.) The local window should be bound to the Down-stream's MaxBuffered (ContentStreamMaxBuffered) and how its current length.
                    //      2.) Even while the its bound to the stream's current values, when the download finishes, we still have to update the global window.
                    // So, there's two options to follow:
                    //  1.) Update the local window based on the stream's usage
                    //  2.a) Send global window_update for every DATA frame processed/received

                    UInt32 initiatedInitialWindowSize = this.settings.InitiatedMySettings[HTTP2Settings.INITIAL_WINDOW_SIZE];
                    this.localWindow = initiatedInitialWindowSize;

                    // Maximize max buffered to HTTP/2's limit
                    if (this.AssignedRequest.DownloadSettings.ContentStreamMaxBuffered > HTTP2ContentConsumer.MaxValueFor31Bits)
                        this.AssignedRequest.DownloadSettings.ContentStreamMaxBuffered = HTTP2ContentConsumer.MaxValueFor31Bits;

                    long localWindowDiff = this.AssignedRequest.DownloadSettings.ContentStreamMaxBuffered - this.localWindow;

                    if (localWindowDiff > 0)
                    {
                        this.localWindow += localWindowDiff;
                        this.outgoing.Enqueue(HTTP2FrameHelper.CreateWindowUpdateFrame(this.Id, (UInt32)localWindowDiff, this.Context));
                    }
                    break;

                case HTTP2StreamStates.Open:
                    ProcessOpenState(outgoingFrames);
                    //HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] New DATA frame created! remoteWindow: {1:N0}", this.Id, this.remoteWindow), this.Context);
                    break;

                case HTTP2StreamStates.HalfClosedLocal:
                    if (this.response?.DownStream != null)
                    {
                        var windowIncrement = this.response.DownStream.MaxBuffered - this.localWindow - this.response.DownStream.Length;
                        if (windowIncrement > 0)
                        {
                            this.localWindow += windowIncrement;
                            outgoingFrames.Add(HTTP2FrameHelper.CreateWindowUpdateFrame(this.Id, (UInt32)windowIncrement, this.Context));

                            HTTPManager.Logger.Information("HTTP2Stream", $"[{this.Id}] Sending window inc. update: {windowIncrement:N0}, {this.localWindow:N0}/{this.response.DownStream.MaxBuffered:N0}", this.Context);
                        }
                    }
                    break;

                case HTTP2StreamStates.HalfClosedRemote:
                    break;

                case HTTP2StreamStates.Closed:
                    break;
            }
        }

        protected virtual void ProcessOpenState(List<HTTP2FrameHeaderAndPayload> outgoingFrames)
        {
            // remote Window can be negative! See https://httpwg.org/specs/rfc7540.html#InitialWindowSize
            if (this.remoteWindow <= 0)
            {
                HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] Skipping data sending as remote Window is {1}!", this.Id, this.remoteWindow), this.Context);
                return;
            }

            // This step will send one frame per ProcessOpenState call.

            Int64 maxFrameSize = Math.Min(this.AssignedRequest.UploadSettings.UploadChunkSize, Math.Min(this.remoteWindow, this.settings.RemoteSettings[HTTP2Settings.MAX_FRAME_SIZE]));

            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.DATA;
            frame.StreamId = this.Id;

            frame.Payload = BufferPool.Get(maxFrameSize, true)
                .AsBuffer((int)maxFrameSize);

            // Expect a readCount of zero if it's end of the stream. But, to enable non-blocking scenario to wait for data, going to treat a negative value as no data.
            this.lastReadCount = this.AssignedRequest.UploadSettings.UploadStream.Read(frame.Payload.Data, 0, (int)Math.Min(maxFrameSize, int.MaxValue));
            if (this.lastReadCount <= 0)
            {
                BufferPool.Release(frame.Payload);

                frame.Payload = BufferSegment.Empty;

                if (this.lastReadCount < 0)
                    return;
            }
            else
                frame.Payload = frame.Payload.Slice(0, this.lastReadCount);

            frame.DontUseMemPool = false;

            if (this.lastReadCount <= 0)
            {
                this.AssignedRequest.UploadSettings.Dispose();

                frame.Flags = (byte)(HTTP2DataFlags.END_STREAM);

                this.State = HTTP2StreamStates.HalfClosedLocal;

                this.AssignedRequest.Timing.StartNext(TimingEventNames.Waiting_TTFB);
            }

            this.outgoing.Enqueue(frame);

            this.remoteWindow -= frame.Payload.Count;

            this.sentData += (uint)frame.Payload.Count;

            if (this.AssignedRequest.UploadSettings.OnUploadProgress != null)
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest, RequestEvents.UploadProgress, this.sentData, this.AssignedRequest.UploadSettings.UploadStream.Length));
        }

        protected void OnRemoteSettingChanged(HTTP2SettingsRegistry registry, HTTP2Settings setting, uint oldValue, uint newValue)
        {
            switch (setting)
            {
                case HTTP2Settings.INITIAL_WINDOW_SIZE:
                    // https://httpwg.org/specs/rfc7540.html#InitialWindowSize
                    // "Prior to receiving a SETTINGS frame that sets a value for SETTINGS_INITIAL_WINDOW_SIZE,
                    // an endpoint can only use the default initial window size when sending flow-controlled frames."
                    // "In addition to changing the flow-control window for streams that are not yet active,
                    // a SETTINGS frame can alter the initial flow-control window size for streams with active flow-control windows
                    // (that is, streams in the "open" or "half-closed (remote)" state). When the value of SETTINGS_INITIAL_WINDOW_SIZE changes,
                    // a receiver MUST adjust the size of all stream flow-control windows that it maintains by the difference between the new value and the old value."

                    // So, if we created a stream before the remote peer's initial settings frame is received, we
                    // will adjust the window size. For example: initial window size by default is 65535, if we later
                    // receive a change to 1048576 (1 MB) we will increase the current remoteWindow by (1 048 576 - 65 535 =) 983 041

                    // But because initial window size in a setting frame can be smaller then the default 65535 bytes,
                    // the difference can be negative:
                    // "A change to SETTINGS_INITIAL_WINDOW_SIZE can cause the available space in a flow-control window to become negative.
                    // A sender MUST track the negative flow-control window and MUST NOT send new flow-controlled frames
                    // until it receives WINDOW_UPDATE frames that cause the flow-control window to become positive.

                    // For example, if the client sends 60 KB immediately on connection establishment
                    // and the server sets the initial window size to be 16 KB, the client will recalculate
                    // the available flow - control window to be - 44 KB on receipt of the SETTINGS frame.
                    // The client retains a negative flow-control window until WINDOW_UPDATE frames restore the
                    // window to being positive, after which the client can resume sending."

                    this.remoteWindow += newValue - oldValue;

                    HTTPManager.Logger.Information("HTTP2Stream", string.Format("[{0}] Remote Setting's Initial Window Updated from {1:N0} to {2:N0}, diff: {3:N0}, new remoteWindow: {4:N0}, total data sent: {5:N0}", this.Id, oldValue, newValue, newValue - oldValue, this.remoteWindow, this.sentData), this.Context);
                    break;
            }
        }

        protected void FinishRequest()
        {
            HTTPManager.Logger.Information("HTTP2Stream", $"FinishRequest({this.Id})", this.Context);

            try
            {
                this.AssignedRequest.Timing.StartNext(TimingEventNames.Queued);

                bool resendRequest;
                HTTPConnectionStates proposedConnectionStates; // ignored
                KeepAliveHeader keepAliveHeader = null; // ignored

                ConnectionHelper.HandleResponse(this.AssignedRequest, out resendRequest, out proposedConnectionStates, ref keepAliveHeader, this.Context);

                if (resendRequest && !this.AssignedRequest.IsCancellationRequested)
                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest, RequestEvents.Resend));
                else if (this.AssignedRequest.State == HTTPRequestStates.Processing && !this.AssignedRequest.IsCancellationRequested)
                {
                    //stream.AssignedRequest.State = HTTPRequestStates.Finished;
                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.AssignedRequest, HTTPRequestStates.Finished, null));
                }
                else
                {
                    // Already set in HTTPRequest's Abort().
                    //if (stream.AssignedRequest.State == HTTPRequestStates.Processing && stream.AssignedRequest.IsCancellationRequested)
                    //    stream.AssignedRequest.State = stream.AssignedRequest.IsTimedOut ? HTTPRequestStates.TimedOut : HTTPRequestStates.Aborted;
                }
            }
            catch(Exception ex)
            {
                HTTPManager.Logger.Exception("HTTP2Stream", "FinishRequest", ex, this.AssignedRequest.Context);
            }
        }

        public void Removed()
        {
            this.AssignedRequest.UploadSettings.Dispose();

            // After receiving a RST_STREAM on a stream, the receiver MUST NOT send additional frames for that stream, with the exception of PRIORITY.
            this.outgoing.Clear();

            // https://github.com/Benedicht/BestHTTP-Issues/issues/77
            // Unsubscribe from OnSettingChangedEvent to remove reference to this instance.
            this.settings.RemoteSettings.OnSettingChangedEvent -= OnRemoteSettingChanged;

            this.headerView?.Close();
            
            HTTPManager.Logger.Information("HTTP2Stream", "Stream removed: " + this.Id.ToString(), this.Context);
        }
    }
}

#endif
