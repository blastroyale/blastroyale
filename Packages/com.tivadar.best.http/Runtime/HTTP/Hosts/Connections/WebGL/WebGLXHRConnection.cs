#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.IO;
using System.Threading;

using Best.HTTP.Caching;
using Best.HTTP.Hosts.Connections.File;
using Best.HTTP.Hosts.Connections.HTTP1;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Authentication;
using Best.HTTP.Request.Timings;
using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Hosts.Connections.WebGL
{
    class PeekableIncomingSegmentContentProviderStream : PeekableContentProviderStream
    {
        private int peek_listIdx;
        private int peek_pos;

        public override void BeginPeek()
        {
            peek_listIdx = 0;
            peek_pos = base.bufferList.Count > 0 ? base.bufferList[0].Offset : 0;
        }

        public override int PeekByte()
        {
            if (base.bufferList.Count == 0)
                return -1;

            var segment = base.bufferList[this.peek_listIdx];
            if (peek_pos >= segment.Offset + segment.Count)
            {
                if (base.bufferList.Count <= this.peek_listIdx + 1)
                    return -1;

                segment = base.bufferList[++this.peek_listIdx];
                this.peek_pos = segment.Offset;
            }

            return segment.Data[this.peek_pos++];
        }
    }


    internal sealed class WebGLXHRConnection : ConnectionBase
    {
        public PeekableContentProviderStream ContentProvider { get; private set; }

        int NativeId;
        PeekableHTTP1Response _response;

        public WebGLXHRConnection(HostKey hostKey)
            : base(hostKey, false)
        {
            WebGLXHRNativeInterface.XHR_SetLoglevel((byte)HTTPManager.Logger.Level);
        }

        public override void Shutdown(ShutdownTypes type)
        {
            base.Shutdown(type);

            WebGLXHRNativeInterface.XHR_Abort(this.NativeId);
        }

        protected override void ThreadFunc()
        {
            // XmlHttpRequest setup

            CurrentRequest.Prepare();

            Credentials credentials = null;// CurrentRequest.Authenticator?.Credentials;

            this.NativeId = WebGLXHRNativeInterface.XHR_Create(HTTPRequest.MethodNames[(byte)CurrentRequest.MethodType],
                                       CurrentRequest.CurrentUri.OriginalString,
                                       credentials?.UserName, credentials?.Password, CurrentRequest.WithCredentials ? 1 : 0);
            WebGLXHRNativeConnectionLayer.Add(NativeId, this);

            CurrentRequest.EnumerateHeaders((header, values) =>
                {
                    if (!header.Equals("Content-Length"))
                        for (int i = 0; i < values.Count; ++i)
                            WebGLXHRNativeInterface.XHR_SetRequestHeader(NativeId, header, values[i]);
                }, /*callBeforeSendCallback:*/ true);

            WebGLXHRNativeConnectionLayer.SetupHandlers(NativeId, CurrentRequest);

            WebGLXHRNativeInterface.XHR_SetTimeout(NativeId, (uint)(CurrentRequest.TimeoutSettings.ConnectTimeout.TotalMilliseconds + CurrentRequest.TimeoutSettings.Timeout.TotalMilliseconds));

            Stream upStream = CurrentRequest.UploadSettings.UploadStream;
            byte[] body = null;
            int length = 0;
            bool releaseBodyBuffer = false;

            if (upStream != null)
            {
                var internalBuffer = BufferPool.Get(upStream.Length > 0 ? upStream.Length : CurrentRequest.UploadSettings.UploadChunkSize, true);
                using (BufferPoolMemoryStream ms = new BufferPoolMemoryStream(internalBuffer, 0, internalBuffer.Length, true, true, false, true))
                {
                    var buffer = BufferPool.Get(CurrentRequest.UploadSettings.UploadChunkSize, true);
                    int readCount = -1;
                    while ((readCount = upStream.Read(buffer, 0, buffer.Length)) > 0)
                        ms.Write(buffer, 0, readCount);

                    BufferPool.Release(buffer);

                    length = (int)ms.Position;
                    body = ms.GetBuffer();

                    releaseBodyBuffer = true;
                }
            }

            if (this._response == null)
                this.CurrentRequest.Response = this._response = new PeekableHTTP1Response(this.CurrentRequest, false, null);

            this.ContentProvider = new PeekableIncomingSegmentContentProviderStream();

            WebGLXHRNativeInterface.XHR_Send(NativeId, body, length);

            if (releaseBodyBuffer)
                BufferPool.Release(body);

            this.CurrentRequest.TimeoutSettings.QueuedAt = DateTime.MinValue;
            this.CurrentRequest.TimeoutSettings.ProcessingStarted = DateTime.Now;
            this.CurrentRequest.OnCancellationRequested += OnCancellationRequested;
        }

        #region Callback Implementations

        private void OnCancellationRequested(HTTPRequest req)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnCancellationRequested()", this.Context);

            Interlocked.Exchange(ref this._response, null);
            req.OnCancellationRequested -= OnCancellationRequested;

            WebGLXHRNativeInterface.XHR_Abort(this.NativeId);
        }

        internal void OnBuffer(BufferSegment buffer)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnBuffer({buffer})", this.Context);

            try
            {
                if (this.CurrentRequest.TimeoutSettings.IsTimedOut(DateTime.Now))
                    throw new TimeoutException();

                if (this.CurrentRequest.IsCancellationRequested)
                    throw new Exception("Cancellation requested!");

                this.ContentProvider?.Write(buffer);
                this._response.ProcessPeekable(this.ContentProvider);
            }
            catch (Exception e)
            {
                BufferPool.Release(buffer);

                if (this.ShutdownType == ShutdownTypes.Immediate)
                    return;

                FinishedProcessing(e);
            }

            // After an exception, this._response will be null!
            if (this._response != null && this._response.ReadState == PeekableHTTP1Response.PeekableReadState.Finished)
                FinishedProcessing(null);
        }

        internal void OnError(string error)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnError({error})", this.Context);

            FinishedProcessing(new Exception(error));
        }

        internal void OnResponse(BufferSegment payload)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnResponse({payload})", this.Context);

            this._response.DownStream.EmergencyIncreaseMaxBuffered();
            OnBuffer(payload);
        }

        void FinishedProcessing(Exception ex)
        {
            // Warning: FinishedProcessing might be called from different threads in parallel:
            //  - send thread triggered by a write failure
            //  - read thread oncontent/OnError/OnConnectionClosed

            var resp = Interlocked.Exchange(ref this._response, null);
            if (resp == null)
                return;

            HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{nameof(FinishedProcessing)}({resp}, {ex})", this.Context);

            // Unset the consumer, we no longer expect another OnContent call until further notice.
            this.ContentProvider?.Unbind();
            this.ContentProvider?.Dispose();
            this.ContentProvider = null;

            var req = this.CurrentRequest;

            req.OnCancellationRequested -= OnCancellationRequested;

            bool resendRequest = false;
            HTTPRequestStates requestState = HTTPRequestStates.Finished;
            HTTPConnectionStates connectionState = HTTPConnectionStates.Recycle;
            Exception error = ex;

            if (error != null)
            {
                // Timeout is a non-retryable error
                if (ex is TimeoutException)
                {
                    error = null;
                    requestState = HTTPRequestStates.TimedOut;
                }
                else if (ex is HTTPCacheAcquireLockException)
                {
                    error = null;
                    resendRequest = true;
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

                KeepAliveHeader keepAlive = null;
                error = ConnectionHelper.HandleResponse(req, out resendRequest, out connectionState, ref keepAlive, this.Context);
                connectionState = HTTPConnectionStates.Recycle;

                if (error != null)
                    requestState = HTTPRequestStates.Error;
                else if (!resendRequest && resp.IsUpgraded)
                    requestState = HTTPRequestStates.Processing;
            }

            req.Timing.StartNext(TimingEventNames.Queued);

            HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{nameof(FinishedProcessing)} final decision. ResendRequest: {resendRequest}, RequestState: {requestState}, ConnectionState: {connectionState}", this.Context);

            // If HandleResponse returned with ClosedResendRequest or there were an error and we can retry the request
            if (connectionState == HTTPConnectionStates.ClosedResendRequest || (resendRequest && connectionState == HTTPConnectionStates.Closed))
            {
                ConnectionHelper.ResendRequestAndCloseConnection(this, req);
            }
            else if (resendRequest && requestState == HTTPRequestStates.Finished)
            {
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(req, RequestEvents.Resend));
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, connectionState));
            }
            else
            {
                // Otherwise set the request's then the connection's state
                ConnectionHelper.EnqueueEvents(this, connectionState, req, requestState, error);
            }
        }

        internal void OnDownloadProgress(int down, int total) => RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.CurrentRequest, RequestEvents.DownloadProgress, down, total));
        internal void OnUploadProgress(int up, int total) => RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.CurrentRequest, RequestEvents.UploadProgress, up, total));

        internal void OnTimeout()
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnTimeout", this.Context);

            CurrentRequest.Response = null;
            CurrentRequest.State = HTTPRequestStates.TimedOut;
            ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
        }

        internal void OnAborted()
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"{this.NativeId} - OnAborted", this.Context);

            CurrentRequest.Response = null;
            CurrentRequest.State = HTTPRequestStates.Aborted;
            ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                WebGLXHRNativeConnectionLayer.Remove(NativeId);
                WebGLXHRNativeInterface.XHR_Release(NativeId);

                this.ContentProvider?.Dispose();
                this.ContentProvider = null;
            }
        }

        #endregion
    }
}

#endif
