#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_WEBSOCKET
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

using Best.HTTP;
using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared.Streams;
using Best.WebSockets.Implementations.Frames;

namespace Best.WebSockets.Implementations
{
    /// <summary>
    /// Implements WebSocket communication through an HTTP/1 connection.
    /// </summary>
    internal sealed class OverHTTP1 : WebSocketBaseImplementation, IContentConsumer, IHeartbeat
    {
        public PeekableContentProviderStream ContentProvider { get; private set; }

        /// <summary>
        /// Indicates whether we sent out the connection request to the server.
        /// </summary>
        private bool requestSent;

        private volatile bool _closed;

        private ConcurrentQueue<WebSocketFrame> unsentFrames = new ConcurrentQueue<WebSocketFrame>();
        private volatile AutoResetEvent newFrameSignal = new AutoResetEvent(false);

        public OverHTTP1(WebSocket parent, Uri uri, string origin, string protocol) : base(parent, uri, origin, protocol)
        {
            string scheme = HTTPProtocolFactory.IsSecureProtocol(uri) ? "wss" : "ws";
            int port = uri.Port != -1 ? uri.Port : (scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            // Somehow if i use the UriBuilder it's not the same as if the uri is constructed from a string...
            //uri = new UriBuilder(uri.Scheme, uri.Host, uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ? 443 : 80, uri.PathAndQuery).Uri;
            base.Uri = new Uri(scheme + "://" + uri.Host + ":" + port + uri.GetRequestPathAndQueryURL());
        }

        protected override void CreateInternalRequest()
        {
            if (this._internalRequest != null)
                return;

            this._internalRequest = new HTTPRequest(base.Uri, OnInternalRequestCallback);

            this._internalRequest.Context.Add("WebSocket", this.Parent.Context);

            // Called when the regular GET request is successfully upgraded to WebSocket
            this._internalRequest.DownloadSettings.OnUpgraded = OnInternalRequestUpgraded;

            //http://tools.ietf.org/html/rfc6455#section-4

            // The request MUST contain an |Upgrade| header field whose value MUST include the "websocket" keyword.
            this._internalRequest.SetHeader("Upgrade", "websocket");

            // The request MUST contain a |Connection| header field whose value MUST include the "Upgrade" token.
            this._internalRequest.SetHeader("Connection", "Upgrade");

            // The request MUST include a header field with the name |Sec-WebSocket-Key|.  The value of this header field MUST be a nonce consisting of a
            // randomly selected 16-byte value that has been base64-encoded (see Section 4 of [RFC4648]).  The nonce MUST be selected randomly for each connection.
            this._internalRequest.SetHeader("Sec-WebSocket-Key", WebSocket.GetSecKey(new object[] { this, InternalRequest, base.Uri, new object() }));

            // The request MUST include a header field with the name |Origin| [RFC6454] if the request is coming from a browser client.
            // If the connection is from a non-browser client, the request MAY include this header field if the semantics of that client match the use-case described here for browser clients.
            // More on Origin Considerations: http://tools.ietf.org/html/rfc6455#section-10.2
            if (!string.IsNullOrEmpty(Origin))
                this._internalRequest.SetHeader("Origin", Origin);

            // The request MUST include a header field with the name |Sec-WebSocket-Version|.  The value of this header field MUST be 13.
            this._internalRequest.SetHeader("Sec-WebSocket-Version", "13");

            if (!string.IsNullOrEmpty(Protocol))
                this._internalRequest.SetHeader("Sec-WebSocket-Protocol", Protocol);

            // Disable caching
            this._internalRequest.SetHeader("Cache-Control", "no-cache");

            this._internalRequest.DownloadSettings.DisableCache = true;

#if !UNITY_WEBGL || UNITY_EDITOR
            this._internalRequest.ProxySettings = this.Parent.GetProxy(this.Uri);
#endif

            this._internalRequest.RedirectSettings.OnBeforeRedirection += InternalRequest_OnBeforeRedirection;

            if (this.Parent.OnInternalRequestCreated != null)
            {
                try
                {
                    this.Parent.OnInternalRequestCreated(this.Parent, this._internalRequest);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(OverHTTP1), "CreateInternalRequest", ex, this.Parent.Context);
                }
            }

            this._internalRequest.OnCancellationRequested += OnCancellationRequested;
        }

        private void OnCancellationRequested(HTTPRequest req)
        {
            HTTPManager.Logger.Error(nameof(OverHTTP1), $"{nameof(InternalRequest_OnBeforeRedirection)}", this.Parent.Context);

            this._closed = true;
            this.newFrameSignal?.Set();

            this._internalRequest.OnCancellationRequested -= OnCancellationRequested;
        }

        private bool InternalRequest_OnBeforeRedirection(HTTPRequest originalRequest, HTTPResponse response, Uri redirectUri)
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), $"{nameof(InternalRequest_OnBeforeRedirection)}", this.Parent.Context);

            // We have to re-select/reset the implementation in the parent Websocket, as the redirected request might gets served over a HTTP/2 connection!
            this.Parent.SelectImplementation(redirectUri, originalRequest.GetFirstHeaderValue("Origin"), originalRequest.GetFirstHeaderValue("Sec-WebSocket-Protocol"))
                .StartOpen();

            originalRequest.Callback = null;
            return false;
        }

        private bool OnInternalRequestUpgraded(HTTPRequest req, HTTPResponse resp, PeekableContentProviderStream contentProvider)
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), $"{nameof(OnInternalRequestUpgraded)}", this.Parent.Context);

            if (this.State == WebSocketStates.Closed)
                return false;

            if (!resp.HasHeader("sec-websocket-accept"))
                throw new Exception("No Sec-Websocket-Accept header is sent by the server!");

            base.ParseExtensionResponse(resp);

            // Save the provider
            this.ContentProvider = contentProvider;

            // Switch the comsumer to this websocket implementation instead of the http1 consumer.
            contentProvider.SetTwoWayBinding(this);

            // Start send thread
            Best.HTTP.Shared.PlatformSupport.Threading.ThreadedRunner.RunLongLiving(SendThread);

            return true;
        }

        private void OnInternalRequestCallback(HTTPRequest req, HTTPResponse resp)
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), $"{nameof(OnInternalRequestCallback)}", this.Parent.Context);

            Cleanup();

            string reason = string.Empty;

            switch (req.State)
            {
                case HTTPRequestStates.Finished:
                    HTTPManager.Logger.Information(nameof(OverHTTP1), string.Format("Request finished. Status Code: {0} Message: {1}", resp.StatusCode.ToString(), resp.Message), this.Parent.Context);

                    if (resp.IsUpgraded)
                    {
                        return;
                    }
                    else
                        reason = string.Format("Request Finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        resp.StatusCode,
                                                        resp.Message,
                                                        resp.DataAsText);
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    reason = req.Exception != null ? req.Exception.Message : string.Empty;
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    reason = "Request Aborted!";
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    reason = "Connection Timed Out!";
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    reason = "Processing the request Timed Out!";
                    break;

                default:
                    return;
            }

            /*if (this.State != WebSocketStates.Connecting || !string.IsNullOrEmpty(reason))
            {
                if (this.Parent.OnError != null)
                    this.Parent.OnError(this.Parent, reason);
                else if (!HTTPManager.IsQuitting)
                    HTTPManager.Logger.Error(nameof(OverHTTP1), reason, this.Parent.Context);
            }*/

            if (this.Parent.OnClosed != null)
            {
                this.Parent.OnClosed(this.Parent,
                    !string.IsNullOrEmpty(reason) ? WebSocketStatusCodes.ClosedAbnormally : WebSocketStatusCodes.NormalClosure,
                    reason ?? "Closed while opening");
            }

            this._closed = true;
            this.State = WebSocketStates.Closed;
        }

        private void SendThread()
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), "SendThread - created", this.Parent.Context);

            ThreadedRunner.SetThreadName("Best.WebSockets Send");

            try
            {
                bool doMask = !HTTPProtocolFactory.IsSecureProtocol(this.Uri);
                var pingFreq = this.Parent.PingFrequency;

                using (WriteOnlyBufferedStream bufferedStream = new WriteOnlyBufferedStream(this.ContentProvider as Stream, 16 * 1024, this.Parent.Context))
                {
                    while (!this._closed)
                    {
                        //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                        //    HTTPManager.Logger.Information(nameof(OverHTTP1), "SendThread - Waiting...", this.Context);

                        TimeSpan waitTime = TimeSpan.FromMilliseconds(int.MaxValue);

                        if (pingFreq != TimeSpan.Zero)
                        {
                            DateTime now = DateTime.Now;
                            waitTime = this.LastMessageReceived + pingFreq - now;

                            if (waitTime <= TimeSpan.Zero)
                            {
                                if (!waitingForPong && now - this.LastMessageReceived >= pingFreq)
                                    SendPing();

                                waitTime = this.Parent.CloseAfterNoMessage;
                            }

                            if (waitingForPong && now - lastPing > this.Parent.CloseAfterNoMessage)
                            {
                                HTTPManager.Logger.Warning(nameof(OverHTTP1),
                                    $"No pong received in the given time! LastPing: {this.lastPing}, PingFrequency: {pingFreq}, Close After: {this.Parent.CloseAfterNoMessage}, Now: {now}",
                                    this.Parent.Context);

                                //this._internalRequest.Timing.Finish(Timing_Name);

                                //this._internalRequest.Exception = new Exception("No PONG received in the given time!");
                                //this._internalRequest.State = HTTPRequestStates.Error;
                                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Error, new Exception("No PONG received in the given time!")));
                                this._closed = true;
                                continue;
                            }
                        }

                        newFrameSignal.WaitOne(waitTime);

                        try
                        {
                            //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                            //    HTTPManager.Logger.Information(nameof(OverHTTP1), "SendThread - Wait is over, about " + this.unsentFrames.Count.ToString() + " new frames!", this.Context);

                            WebSocketFrame frame;
                            while (!this._closeSent && this.unsentFrames.TryDequeue(out frame))
                            {
                                // save data count as per-message deflate can compress, and it would be different after calling WriteTo
                                int originalFrameDataLength = frame.Data.Count;

                                frame.WriteTo((header, chunk) =>
                                {
                                    bufferedStream.Write(header.Data, header.Offset, header.Count);
                                    BufferPool.Release(header);

                                    if (chunk != BufferSegment.Empty)
                                        bufferedStream.Write(chunk.Data, chunk.Offset, chunk.Count);
                                }, WebSocket.MaxFragmentSize, doMask, this.Parent.Context);
                                BufferPool.Release(frame.Data);

                                if (frame.Type == WebSocketFrameTypes.ConnectionClose)
                                {
                                    this._closeSent = true;
                                    if (this._closeReceived)
                                    {
                                        this._closed = true;
                                        this.State = WebSocketStates.Closed;
                                    }
                                }

                                Interlocked.Add(ref this._bufferedAmount, -originalFrameDataLength);
                            }

                            bufferedStream.Flush();
                        }
                        catch (Exception ex)
                        {
                            //this._internalRequest.Timing.Finish(Timing_Name);

                            if (HTTPUpdateDelegator.IsCreated)
                            {
                                //this._internalRequest.Exception = ex;
                                //this._internalRequest.State = HTTPRequestStates.Error;
                                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Error, ex));
                            }
                            else
                            {
                                //this._internalRequest.State = HTTPRequestStates.Aborted;
                                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Aborted, null));
                            }

                            HTTPManager.Logger.Exception(nameof(OverHTTP1), "Frame sending", ex);

                            this._closed = true;
                            this.State = WebSocketStates.Closed;
                        }
                    }

                    HTTPManager.Logger.Information(nameof(OverHTTP1), string.Format("Ending Send thread. Closed: {0}, closeSent: {1}", this._closed, this._closeSent), this.Parent.Context);
                }
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.Level == Loglevels.All)
                    HTTPManager.Logger.Exception(nameof(OverHTTP1), "SendThread", ex);
            }
            finally
            {
                HTTPManager.Logger.Information(nameof(OverHTTP1), "SendThread - Closed!", this.Parent.Context);
            }
        }

        private void SendPing()
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), "Sending Ping frame, waiting for a pong...", this.Parent.Context);

            lastPing = DateTime.Now;
            waitingForPong = true;

            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Ping, BufferSegment.Empty));
        }

        public override void StartOpen()
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), $"{nameof(StartOpen)}", this.Parent.Context);
            if (requestSent)
                throw new InvalidOperationException("Open already called! You can't reuse this WebSocket instance!");

            if (this.Parent.Extensions != null)
            {
                try
                {
                    for (int i = 0; i < this.Parent.Extensions.Length; ++i)
                    {
                        var ext = this.Parent.Extensions[i];
                        if (ext != null)
                            ext.AddNegotiation(InternalRequest);
                    }
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(OverHTTP1), "Open", ex, this.Parent.Context);
                }
            }

            InternalRequest.Send();
            requestSent = true;
            this.State = WebSocketStates.Connecting;
            HTTPManager.Heartbeats.Subscribe(this);
        }

        public override void StartClose(WebSocketStatusCodes code, string message)
        {
            HTTPManager.Logger.Information(nameof(OverHTTP1), $"{nameof(StartClose)}({code}, {message})", this.Parent.Context);
                        
            if (this.State == WebSocketStates.Connecting)
            {
                if (this.InternalRequest != null)
                    this.InternalRequest.Abort();

                this.State = WebSocketStates.Closed;
                if (this.Parent.OnClosed != null)
                    this.Parent.OnClosed(this.Parent, WebSocketStatusCodes.NormalClosure, string.Empty);
            }
            else
            {
                this.State = WebSocketStates.Closing;

                Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.ConnectionClose, WebSocket.EncodeCloseData(code, message), false));
            }
        }

        public override void Send(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message must not be null!");

            int count = System.Text.Encoding.UTF8.GetByteCount(message);
            byte[] data = BufferPool.Get(count, true);
            System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);

            Send(WebSocketFrameTypes.Text, data.AsBuffer(count));
        }

        public override void Send(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data must not be null!");

            WebSocketFrame frame = new WebSocketFrame(this.Parent, WebSocketFrameTypes.Binary, new BufferSegment(data, 0, data.Length));
            Send(frame);
        }

        public override void Send(byte[] data, ulong offset, ulong count)
        {
            if (data == null)
                throw new ArgumentNullException("data must not be null!");
            if (offset + count > (ulong)data.Length)
                throw new ArgumentOutOfRangeException("offset + count >= data.Length");

            WebSocketFrame frame = new WebSocketFrame(this.Parent, WebSocketFrameTypes.Binary, new BufferSegment(data, (int)offset, (int)count), true);
            Send(frame);
        }

        public void Send(WebSocketFrameTypes type, BufferSegment data)
        {
            WebSocketFrame frame = new WebSocketFrame(this.Parent, type, data, false);
            Send(frame);
        }

        public override void SendAsBinary(BufferSegment data)
        {
            Send(WebSocketFrameTypes.Binary, data);
        }

        public override void SendAsText(BufferSegment data)
        {
            Send(WebSocketFrameTypes.Text, data);
        }

        public override void Send(WebSocketFrame frame)
        {
            if (this._closed || this._closeSent)
                return;

            this.unsentFrames.Enqueue(frame);

            Interlocked.Add(ref this._bufferedAmount, frame.Data.Count);

            newFrameSignal.Set();
        }

        public void SetBinding(PeekableContentProviderStream stream)
        {
            this.ContentProvider = stream;

            // Read any frames already in the buffers
            OnContent();
        }

        public void UnsetBinding() => this.ContentProvider = null;

        public void OnContent()
        {
            this.LastMessageReceived = DateTime.Now;

            if (this._closeReceived || this._closed)
                return;

            while (OverHTTP2.CanReadFullFrame(this.ContentProvider))
            {
                WebSocketFrameReader frame = new WebSocketFrameReader();
                frame.Read(this.ContentProvider);
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(OverHTTP1), "Frame received: " + frame.ToString(), this.Parent.Context);

                if (!frame.IsFinal)
                {
                    IncompleteFrames.Add(frame);
                    continue;
                }

                switch (frame.Type)
                {
                    // For a complete documentation and rules on fragmentation see http://tools.ietf.org/html/rfc6455#section-5.4
                    // A fragmented Frame's last fragment's opcode is 0 (Continuation) and the FIN bit is set to 1.
                    case WebSocketFrameTypes.Continuation:
                        // Do an assemble pass only if OnFragment is not set. Otherwise put it in the CompletedFrames, we will handle it in the HandleEvent phase.
                        frame.Assemble(IncompleteFrames);

                        // Remove all incomplete frames
                        IncompleteFrames.Clear();

                        // Control frames themselves MUST NOT be fragmented. So, its a normal text or binary frame. Go, handle it as usual.
                        goto case WebSocketFrameTypes.Binary;

                    case WebSocketFrameTypes.Text:
                    case WebSocketFrameTypes.Binary:
                        frame.DecodeWithExtensions(this.Parent);
                        CompletedFrames.Enqueue(frame);
                        break;

                    // Upon receipt of a Ping frame, an endpoint MUST send a Pong frame in response, unless it already received a Close frame.
                    case WebSocketFrameTypes.Ping:
                        if (!_closeSent && this.State != WebSocketStates.Closed)
                        {
                            // copy data set to true here, as the frame's data is released back to the pool after the switch
                            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Pong, frame.Data, true));
                        }
                        break;

                    case WebSocketFrameTypes.Pong:
                        // https://tools.ietf.org/html/rfc6455#section-5.5
                        // A Pong frame MAY be sent unsolicited.  This serves as a
                        // unidirectional heartbeat.  A response to an unsolicited Pong frame is
                        // not expected. 
                        if (!waitingForPong)
                            break;

                        waitingForPong = false;
                        // the difference between the current time and the time when the ping message is sent
                        TimeSpan diff = DateTime.Now - lastPing;

                        // add it to the buffer
                        this.rtts.Add((int)diff.TotalMilliseconds);

                        // and calculate the new latency
                        base.Latency = CalculateLatency();
                        break;

                    // If an endpoint receives a Close frame and did not previously send a Close frame, the endpoint MUST send a Close frame in response.
                    case WebSocketFrameTypes.ConnectionClose:
                        this._closeReceived = true;

                        HTTPManager.Logger.Information(nameof(OverHTTP1), $"ConnectionClose packet received! ({this._closeReceived}, {this._closeSent})", this.Parent.Context);

                        CompletedFrames.Enqueue(frame);

                        if (!this._closeSent)
                        {
                            this.State = WebSocketStates.Closing;
                            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.ConnectionClose, BufferSegment.Empty));
                        }
                        else
                        {
                            this._closed = true;
                            this.State = WebSocketStates.Closed;
                            this.newFrameSignal.Set();
                        }
                        break;
                }
            }
        }

        public void OnConnectionClosed()
        {
            if (this._closed)
                return;

            //this._internalRequest.Timing.Finish(Timing_Name);

            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Error, new Exception("Connection closed unexpectedly!")));
        }

        public void OnError(Exception ex)
        {
            if (this._closed)
                return;

            //this._internalRequest.Timing.Finish(Timing_Name);

            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Error, ex));
        }

        public void OnHeartbeatUpdate(DateTime now, TimeSpan dif)
        {
            if (HTTPManager.IsQuitting)
                this.StartClose(WebSocketStatusCodes.GoingAway, "Editor closing");

            switch(this.State)
            {
                case WebSocketStates.Connecting:
                    if (requestSent && this._internalRequest?.Response?.IsUpgraded is bool upgraded && upgraded)
                    {
                        this.State = WebSocketStates.Open;

                        // The request upgraded successfully.
                        if (this.Parent.OnOpen != null)
                            this.Parent.OnOpen(this.Parent);

                        OnHeartbeatUpdate(now, dif);
                    }
                    break;

                case WebSocketStates.Closing:
                    // TODO: define and handle a timeout

                    HandleCompletedFrames();
                    break;

                case WebSocketStates.Closed:
                    HandleCompletedFrames();

                    HTTPManager.Heartbeats.Unsubscribe(this);
                    this.ContentProvider?.Unbind();
                    this.ContentProvider?.Dispose();

                    if (this._internalRequest != null && this._internalRequest.State < HTTPRequestStates.Finished)
                    {
                        //this._internalRequest.Timing.Finish(Timing_Name);

                        //this._internalRequest.State = HTTPRequestStates.Finished;
                        RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this._internalRequest, HTTPRequestStates.Finished, null));
                    }

                    // TODO: go through any lists and queues to empty and recycle buffer segments
                    // this.unsentFrames.TryDequeue(out frame)
                    break;

                default:
                    HandleCompletedFrames();
                    break;
            }
        }

        private void HandleCompletedFrames()
        {
            while (CompletedFrames.TryDequeue(out var frame))
            {
                try
                {
                    switch (frame.Type)
                    {
                        case WebSocketFrameTypes.Continuation:
                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose(nameof(OverHTTP1), "HandleEvents - OnIncompleteFrame", this.Parent.Context);
                            break;

                        case WebSocketFrameTypes.Text:
                            // Any not Final frame is handled as a fragment
                            if (!frame.IsFinal)
                                goto case WebSocketFrameTypes.Continuation;

                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose(nameof(OverHTTP1), $"HandleEvents - OnText(\"{frame.DataAsText}\")", this.Parent.Context);

                            if (this.Parent.OnMessage != null)
                                this.Parent.OnMessage(this.Parent, frame.DataAsText);
                            break;

                        case WebSocketFrameTypes.Binary:
                            // Any not Final frame is handled as a fragment
                            if (!frame.IsFinal)
                                goto case WebSocketFrameTypes.Continuation;

                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose(nameof(OverHTTP1), $"HandleEvents - OnBinary({frame.Data})", this.Parent.Context);

                            if (this.Parent.OnBinary != null)
                                this.Parent.OnBinary(this.Parent, frame.Data);
                            break;

                        case WebSocketFrameTypes.ConnectionClose:
                            HTTPManager.Logger.Verbose(nameof(OverHTTP1), "HandleEvents - Calling OnClosed", this.Parent.Context);

                            if (this.Parent.OnClosed != null)
                            {
                                try
                                {
                                    UInt16 statusCode = 0;
                                    string msg = string.Empty;

                                    // If we received any data, we will get the status code and the message from it
                                    if (/*CloseFrame != null && */ frame.Data != BufferSegment.Empty && frame.Data.Count >= 2)
                                    {
                                        if (BitConverter.IsLittleEndian)
                                            Array.Reverse(frame.Data.Data, frame.Data.Offset, 2);
                                        statusCode = BitConverter.ToUInt16(frame.Data.Data, frame.Data.Offset);

                                        if (frame.Data.Count > 2)
                                            msg = Encoding.UTF8.GetString(frame.Data.Data, frame.Data.Offset + 2, frame.Data.Count - 2);

                                        frame.ReleaseData();
                                    }

                                    this.Parent.OnClosed(this.Parent, (WebSocketStatusCodes)statusCode, msg);
                                    this.Parent.OnClosed = null;
                                }
                                catch (Exception ex)
                                {
                                    HTTPManager.Logger.Exception(nameof(OverHTTP1), "HandleEvents - OnClosed", ex, this.Parent.Context);
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(OverHTTP1), string.Format("HandleEvents({0})", frame.ToString()), ex, this.Parent.Context);
                }
                finally
                {
                    frame.ReleaseData();
                }
            }
        }
    }
}
#endif
