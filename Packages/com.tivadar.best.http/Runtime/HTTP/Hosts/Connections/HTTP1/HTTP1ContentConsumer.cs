#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Threading;

using Best.HTTP.Request.Timings;
using Best.HTTP.Request.Upload;
using Best.HTTP.Response;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared.Streams;

using static Best.HTTP.Hosts.Connections.HTTP1.Constants;

namespace Best.HTTP.Hosts.Connections.HTTP1
{
    public sealed class HTTP1ContentConsumer : IHTTPRequestHandler, IContentConsumer, IDownloadContentBufferAvailable, IThreadSignaler
    {
        public ShutdownTypes ShutdownType { get; private set; }

        public KeepAliveHeader KeepAlive { get { return this._keepAlive; } }
        private KeepAliveHeader _keepAlive;

        public bool CanProcessMultiple { get { return false; } }

        public LoggingContext Context { get; private set; }

        public PeekableContentProviderStream ContentProvider { get; private set; }

        private readonly HTTPOverTCPConnection conn;
        private PeekableHTTP1Response _response;
        private int _isAlreadyProcessingContent;
        private AutoResetEvent _are = new AutoResetEvent(false);

        public HTTP1ContentConsumer(HTTPOverTCPConnection conn)
        {
            this.Context = new LoggingContext(this);
            this.conn = conn;
        }

        public void RunHandler()
        {
            HTTPManager.Logger.Information(nameof(HTTP1ContentConsumer), "Started processing request", this.Context);

            ThreadedRunner.SetThreadName("Best.HTTP1 Write");

            try
            {
                var now = DateTime.Now;

                if (this.conn.CurrentRequest.TimeoutSettings.IsTimedOut(now))
                    throw new TimeoutException();

                if (this.conn.CurrentRequest.IsCancellationRequested)
                    throw new Exception("Cancellation requested!");

                // create the response before we would send out the request, because sending out might cause an exception
                //  and the response is used for decision making in the FinishedProcessing call.
                if (this._response == null)
                    this.conn.CurrentRequest.Response = this._response = new PeekableHTTP1Response(this.conn.CurrentRequest, false, this);

                // Write the request to the stream
                this.conn.CurrentRequest.TimeoutSettings.QueuedAt = DateTime.MinValue;
                this.conn.CurrentRequest.TimeoutSettings.ProcessingStarted = now;

                this.conn.CurrentRequest.Timing.StartNext(TimingEventNames.Request_Sent);

                SendOutTo(this.conn.CurrentRequest, this.conn.TopStream);

                this.conn.CurrentRequest.OnCancellationRequested += OnCancellationRequested;
            }
            catch (Exception e)
            {
                if (this.ShutdownType == ShutdownTypes.Immediate)
                    return;

                FinishedProcessing(e);
            }
        }

        private void SendOutTo(HTTPRequest request, System.IO.Stream stream)
        {
            request.Prepare();

            string requestPathAndQuery =
                    request.ProxySettings.HasProxyFor(request.CurrentUri) ?
                        request.ProxySettings.Proxy.GetRequestPath(request.CurrentUri) :
                        request.CurrentUri.GetRequestPathAndQueryURL();

            string requestLine = string.Format("{0} {1} HTTP/1.1", HTTPRequest.MethodNames[(byte)request.MethodType], requestPathAndQuery);

            if (HTTPManager.Logger.Level <= Loglevels.Information)
                HTTPManager.Logger.Information("HTTPRequest", string.Format("Sending request: '{0}'", requestLine), request.Context);

            // Create a buffer stream that will not close 'stream' when disposed or closed.
            // buffersize should be larger than UploadChunkSize as it might be used for uploading user data and
            //  it should have enough room for UploadChunkSize data and additional chunk information.
            using (WriteOnlyBufferedStream bufferStream = new WriteOnlyBufferedStream(stream, (int)(request.UploadSettings.UploadChunkSize * 1.5f), request.Context))
            {
                var requestLineBytes = requestLine.GetASCIIBytes();
                bufferStream.WriteBufferSegment(requestLineBytes);
                bufferStream.WriteArray(EOL);

                BufferPool.Release(requestLineBytes);

                // Write headers to the buffer
                request.EnumerateHeaders((header, values) =>
                {
                    if (string.IsNullOrEmpty(header) || values == null)
                        return;

                    //var headerName = string.Concat(header, ": ").GetASCIIBytes();
                    var headerName = header.GetASCIIBytes();

                    for (int i = 0; i < values.Count; ++i)
                    {
                        if (string.IsNullOrEmpty(values[i]))
                        {
                            HTTPManager.Logger.Warning("HTTPRequest", string.Format("Null/empty value for header: {0}", header), request.Context);
                            continue;
                        }

                        if (HTTPManager.Logger.Level <= Loglevels.Information)
                            HTTPManager.Logger.Verbose("HTTPRequest", $"Header - '{header}': '{values[i]}'", request.Context);

                        var valueBytes = values[i].GetASCIIBytes();

                        bufferStream.WriteBufferSegment(headerName);
                        bufferStream.WriteArray(HeaderValueSeparator);
                        bufferStream.WriteBufferSegment(valueBytes);
                        bufferStream.WriteArray(EOL);

                        BufferPool.Release(valueBytes);
                    }

                    BufferPool.Release(headerName);
                }, /*callBeforeSendCallback:*/ true);

                bufferStream.WriteArray(EOL);

                // Send remaining data to the wire
                bufferStream.Flush();

                //if (!request.UploadSettings.Expect100Continue)
                SendContent();

                this.conn.CurrentRequest.Timing.StartNext(TimingEventNames.Waiting_TTFB);
            } // bufferStream.Dispose

            HTTPManager.Logger.Information("HTTPRequest", "Sent out '" + requestLine + "'", this.Context);
        }

        void SendContent()
        {
            System.IO.Stream uploadStream = this.conn.CurrentRequest.UploadSettings.UploadStream;
            if (uploadStream != null)
            {
                try
                {
                    if (uploadStream is Request.Upload.UploadStreamBase upStream)
                        upStream.BeforeSendBody(this.conn.CurrentRequest, this);

                    using WriteOnlyBufferedStream bufferStream = new WriteOnlyBufferedStream(this.conn.TopStream,
                            (int)(this.conn.CurrentRequest.UploadSettings.UploadChunkSize * 1.5f),
                            this.conn.CurrentRequest.Context);

                    long uploadLength = uploadStream.Length;
                    bool isChunked = uploadLength == BodyLengths.UnknownWithChunkedTransferEncoding;

                    // Initialize the progress report variables
                    long Uploaded = 0;

                    // Upload buffer. First we will read the data into this buffer from the UploadStream, then write this buffer to our outStream
                    byte[] buffer = BufferPool.Get(this.conn.CurrentRequest.UploadSettings.UploadChunkSize, true);
                    using var _ = new AutoReleaseBuffer(buffer);

                    // How many bytes was read from the UploadStream
                    int count = uploadStream.Read(buffer, 0, buffer.Length);
                    while (count != 0)
                    {
                        if (count <= 0)
                        {
                            this._are.WaitOne();
                            count = uploadStream.Read(buffer, 0, buffer.Length);
                            continue;
                        }

                        if (isChunked)
                        {
                            var countBytes = count.ToString("X").GetASCIIBytes();
                            bufferStream.WriteBufferSegment(countBytes);
                            bufferStream.WriteArray(EOL);

                            BufferPool.Release(countBytes);
                        }

                        // write out the buffer to the wire
                        bufferStream.Write(buffer, 0, count);

                        // chunk trailing EOL
                        if (uploadLength < 0)
                            bufferStream.WriteArray(EOL);

                        // update how many bytes are uploaded
                        Uploaded += count;

                        // Write to the wire
                        bufferStream.Flush();

                        if (this.conn.CurrentRequest.UploadSettings.OnUploadProgress != null)
                            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.conn.CurrentRequest, RequestEvents.UploadProgress, Uploaded, uploadLength));

                        if (this.conn.CurrentRequest.IsCancellationRequested)
                            return;

                        count = uploadStream.Read(buffer, 0, buffer.Length);
                    }

                    // All data from the stream are sent, write the 'end' chunk if necessary
                    if (isChunked)
                    {
                        byte[] noMoreChunkBytes = BufferPool.Get(1, true);
                        noMoreChunkBytes[0] = (byte)'0';
                        bufferStream.Write(noMoreChunkBytes, 0, 1);
                        bufferStream.WriteArray(EOL);
                        bufferStream.WriteArray(EOL);

                        BufferPool.Release(noMoreChunkBytes);
                    }

                    // Make sure all remaining data will be on the wire
                    bufferStream.Flush();
                }
                finally
                {
                    if (this.conn.CurrentRequest.UploadSettings.DisposeStream)
                        uploadStream.Dispose();
                }
            }
        }

        void IDownloadContentBufferAvailable.BufferAvailable(DownloadContentStream stream)
        {
            //HTTPManager.Logger.Verbose(nameof(HTTP1ContentConsumer), "IDownloadContentBufferAvailable.BufferAvailable", this.Context);

            // TODO: Do NOT call OnContent on the Unity main thread
            if (this._response != null)
                OnContent();
        }

        public void SetBinding(PeekableContentProviderStream contentProvider) => this.ContentProvider = contentProvider;

        public void UnsetBinding() => this.ContentProvider = null;

        public void OnContent()
        {
            if (Interlocked.CompareExchange(ref this._isAlreadyProcessingContent, 1, 0) != 0)
                return;

            try
            {
                //HTTPManager.Logger.Information(nameof(HTTP1ContentConsumer), $"OnContent({peekable?.Length}, {this._response?.ReadState})", this.Context, this.conn.CurrentRequest.Context);
                try
                {
                    if (this.conn.CurrentRequest.TimeoutSettings.IsTimedOut(DateTime.Now))
                        throw new TimeoutException();

                    if (this.conn.CurrentRequest.IsCancellationRequested)
                        throw new Exception("Cancellation requested!");

                    this._response.ProcessPeekable(this.ContentProvider);
                }
                catch (Exception e)
                {
                    if (this.ShutdownType == ShutdownTypes.Immediate)
                        return;

                    FinishedProcessing(e);
                }

                // After an exception, this._response will be null!
                if (this._response != null)
                {
                    if (this._response.ReadState == PeekableHTTP1Response.PeekableReadState.Finished)
                        FinishedProcessing(null);
                    else if (this._response.ReadState == PeekableHTTP1Response.PeekableReadState.WaitForContentSent)
                    {
                        SendContent();
                        this.conn.CurrentRequest.Timing.StartNext(TimingEventNames.Waiting_TTFB);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref this._isAlreadyProcessingContent, 0);
            }
        }

        public void OnConnectionClosed()
        {
            HTTPManager.Logger.Information(nameof(HTTP1ContentConsumer), $"OnConnectionClosed({this.ContentProvider?.Length}, {this._response?.ReadState})", this.Context);

            if (this.ContentProvider.Length > 0 &&
                this._response != null &&
                this._response.ReadState == PeekableHTTP1Response.PeekableReadState.Content &&
                this._response.DownStream != null)
            {
                // Let the stream comsume any buffered data first, and handle closure when the buffer depletes.
                // TODO: This require however that the PeekableResponse ping this HTTP1ContentConsumer when buffer space is available in the down-stream.
                //       Or force-add all remaining data to the stream and see whether we finished downloading or not.
                // Problems: 
                //  1.) OnContent might be already called and a call to it would be dropped. We could spin up a new thread waiting for its finish, then call it again.
                //throw new NotImplementedException();
                ThreadedRunner.RunShortLiving(() =>
                {
                    SpinWait spinWait = new SpinWait();

                    while (Interlocked.CompareExchange(ref this._isAlreadyProcessingContent, 1, 0) == 1)
                        spinWait.SpinOnce();

                    try
                    {
                        try
                        {
                            this._response.DownStream.EmergencyIncreaseMaxBuffered();
                            this._response.ProcessPeekable(this.ContentProvider);
                        }
                        catch (Exception e)
                        {
                            if (this.ShutdownType == ShutdownTypes.Immediate)
                                return;

                            FinishedProcessing(e);
                        }
                        finally
                        {
                            // After an exception, this._response will be null!
                            if (this._response != null)
                            {
                                if (this._response.ReadState == PeekableHTTP1Response.PeekableReadState.Finished)
                                    FinishedProcessing(null);
                                else
                                    FinishedProcessing(new Exception("Underlying TCP connection closed unexpectedly!"));
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref this._isAlreadyProcessingContent, 0);
                    }
                });

                return;
            }

            // If the consumer still have a request: error it and close the connection
            if (this.conn.CurrentRequest != null && this._response != null)
            {
                FinishedProcessing(new Exception("Underlying TCP connection closed unexpectedly!"));
            }
            else // If no current request: close the connection
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this.conn, HTTPConnectionStates.Closed));

        }

        public void OnError(Exception e)
        {
            HTTPManager.Logger.Information(nameof(HTTP1ContentConsumer), $"OnError({this.ContentProvider?.Length}, {this._response?.ReadState}, {this.ShutdownType})", this.Context);

            if (this.ShutdownType == ShutdownTypes.Immediate)
                return;

            FinishedProcessing(e);
        }

        private void OnCancellationRequested(HTTPRequest req)
        {
            HTTPManager.Logger.Information(nameof(HTTP1ContentConsumer), "OnCancellationRequested()", this.Context);

            Interlocked.Exchange(ref this._response, null);
            req.OnCancellationRequested -= OnCancellationRequested;
            this.conn?.Streamer?.Dispose();
        }

        void FinishedProcessing(Exception ex)
        {
            // Warning: FinishedProcessing might be called from different threads in parallel:
            //  - send thread triggered by a write failure
            //  - read thread oncontent/OnError/OnConnectionClosed

            var resp = Interlocked.Exchange(ref this._response, null);
            if (resp == null)
                return;

            HTTPManager.Logger.Verbose(nameof(HTTP1ContentConsumer), $"{nameof(FinishedProcessing)}({resp.ReadState}, {ex})", this.Context);

            // Unset the consumer, we no longer expect another OnContent call until further notice.
            //if (conn.TopStream is IPeekableContentProvider provider && provider?.Consumer == this)
            //    provider.Consumer = null;
            this.ContentProvider.UnbindIf(this);

            var req = this.conn.CurrentRequest;

            req.OnCancellationRequested -= OnCancellationRequested;

            bool resendRequest = false;
            HTTPRequestStates requestState = HTTPRequestStates.Finished;
            HTTPConnectionStates connectionState = ex != null ? HTTPConnectionStates.Closed : HTTPConnectionStates.Recycle;

            // We could finish the request, ignore the error.
            if (resp.ReadState == PeekableHTTP1Response.PeekableReadState.Finished)
                ex = null;

            Exception error = ex;

            if (error != null)
            {
                // Timeout is a non-retryable error
                if (ex is TimeoutException)
                {
                    error = null;
                    requestState = HTTPRequestStates.TimedOut;
                }
                else
                {
                    if (req.RetrySettings.Retries < req.RetrySettings.MaxRetries)
                    {
                        req.RetrySettings.Retries++;
                        error = null;
                        resendRequest = true;
                    }
                    else
                    {
                        requestState = HTTPRequestStates.Error;
                    }
                }

                // Any exception means that the connection is in an unknown state, we shouldn't try to reuse it.
                connectionState = HTTPConnectionStates.Closed;

                resp.Dispose();
            }
            else
            {
                // After HandleResponse connectionState can have the following values:
                //  - Processing: nothing interesting, caller side can decide what happens with the connection (recycle connection).
                //  - Closed: server sent an connection: close header.
                //  - ClosedResendRequest: in this case resendRequest is true, and the connection must not be reused.
                //      In this case we can send only one ConnectionEvent to handle both case and avoid concurrency issues.

                error = ConnectionHelper.HandleResponse(req, out resendRequest, out connectionState, ref this._keepAlive, this.Context);

                if (error != null)
                    requestState = HTTPRequestStates.Error;
                else if (!resendRequest && resp.IsUpgraded)
                    requestState = HTTPRequestStates.Processing;
            }

            req.Timing.StartNext(TimingEventNames.Queued);

            HTTPManager.Logger.Verbose(nameof(HTTP1ContentConsumer), $"{nameof(FinishedProcessing)} final decision. ResendRequest: {resendRequest}, RequestState: {requestState}, ConnectionState: {connectionState}", this.Context);

            // If HandleResponse returned with ClosedResendRequest or there were an error and we can retry the request
            if (connectionState == HTTPConnectionStates.ClosedResendRequest || (resendRequest && connectionState == HTTPConnectionStates.Closed))
            {
                ConnectionHelper.ResendRequestAndCloseConnection(this.conn, req);
            }
            else if (resendRequest && requestState == HTTPRequestStates.Finished)
            {
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(req, RequestEvents.Resend));
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this.conn, connectionState));
            }
            else
            {
                // Otherwise set the request's then the connection's state
                ConnectionHelper.EnqueueEvents(this.conn, connectionState, req, requestState, error);
            }
        }

        public void Process(HTTPRequest request)
        {
            (conn.TopStream as IPeekableContentProvider).SetTwoWayBinding(this);
            ThreadedRunner.RunLongLiving(RunHandler);
        }

        public void Shutdown(ShutdownTypes type)
        {
            HTTPManager.Logger.Verbose(nameof(HTTP1ContentConsumer), string.Format($"Shutdown({type})"), this.Context);
            this.ShutdownType = type;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._are.Dispose();
                this._are = null;
            }
        }

        void IThreadSignaler.SignalThread()
        {
            this._are?.Set();
        }
    }
}
#endif
