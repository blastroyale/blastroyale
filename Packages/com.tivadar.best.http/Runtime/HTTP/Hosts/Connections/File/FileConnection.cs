using System;
using System.Threading;

using Best.HTTP.Caching;
using Best.HTTP.Hosts.Connections.HTTP1;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Timings;
using Best.HTTP.Response;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Hosts.Connections.File
{
    internal sealed class FileConnection : ConnectionBase, IContentConsumer, IDownloadContentBufferAvailable
    {
        public PeekableContentProviderStream ContentProvider { get; private set; }

        PeekableHTTP1Response _response;
        NonblockingUnderlyingStream _stream;

        UnityEngine.Hash128 _cacheHash;

        public FileConnection(HostKey hostKey)
            : base(hostKey)
        { }

        protected override void ThreadFunc()
        {
            this.CurrentRequest.Timing.StartNext(TimingEventNames.Waiting_TTFB);

            bool isFromLocalCache = this.CurrentRequest.CurrentUri.Host.Equals(HTTPCache.CacheHostName, StringComparison.OrdinalIgnoreCase);

            if (this._response == null)
                this.CurrentRequest.Response = this._response = new PeekableHTTP1Response(this.CurrentRequest, isFromLocalCache, this);

            StreamList stream = new StreamList();
            try
            {
                var headers = new BufferPoolMemoryStream();
                stream.AppendStream(headers);
                
                headers.WriteLine("HTTP/1.1 200 Ok");

                System.IO.Stream contentStream = null;

                if (isFromLocalCache)
                {
                    var hashStr = this.CurrentRequest.CurrentUri.AbsolutePath.Substring(1);
                    var hash = UnityEngine.Hash128.Parse(hashStr);

                    // BeginReadContent tries to acquire a read lock on the content and returns null if couldn't.
                    contentStream = HTTPManager.LocalCache?.BeginReadContent(hash, this.Context);
                    if (contentStream == null)
                        throw new HTTPCacheAcquireLockException($"Coulnd't acquire read lock on cached entity.");

                    this._cacheHash = hash;

                    headers.WriteLine($"BestHTTP-Origin: cachefile({hashStr})");

                    stream.AppendStream(HTTPManager.IOService.CreateFileStream(HTTPManager.LocalCache.GetHeaderPathFromHash(hash), FileStreamModes.OpenRead));
                }
                else
                {
                    headers.WriteLine($"BestHTTP-Origin: file");
                    headers.WriteLine("Content-Type: application/octet-stream");

                    contentStream = HTTPManager.IOService.CreateFileStream(this.CurrentRequest.CurrentUri.LocalPath, FileStreamModes.OpenRead);
                }

                headers.WriteLine($"Content-Length: {contentStream.Length.ToString()}");
                if (!isFromLocalCache)
                    headers.WriteLine();

                headers.Seek(0, System.IO.SeekOrigin.Begin);

                stream.AppendStream(contentStream);

                this.CurrentRequest.TimeoutSettings.QueuedAt = DateTime.MinValue;
                this.CurrentRequest.TimeoutSettings.ProcessingStarted = DateTime.Now;
                
                this._stream = new NonblockingUnderlyingStream(stream, 1024 * 1024, this.Context);
                this._stream.SetTwoWayBinding(this);

                this.CurrentRequest.OnCancellationRequested += OnCancellationRequested;
            }
            catch(Exception ex)
            {
                FinishedProcessing(ex);
                stream?.Dispose();
            }
        }

        void IDownloadContentBufferAvailable.BufferAvailable(DownloadContentStream stream)
        {
            //HTTPManager.Logger.Verbose(nameof(FileConnection), "IDownloadContentBufferAvailable.BufferAvailable", this.Context);

            // Here we should trigger somehow the read stream and that should call OnContent(IPeekableContentProvider provider, PeekableStream peekable)
            //  to go the regular route.

            if (this._response != null)
                OnContent();
        }

        public void SetBinding(PeekableContentProviderStream contentProvider)
        {
            this.ContentProvider = contentProvider;
        }

        public void UnsetBinding() => this.ContentProvider = null;

        public void OnContent()
        {
            try
            {
                if (this.CurrentRequest.TimeoutSettings.IsTimedOut(DateTime.Now))
                    throw new TimeoutException();

                if (this.CurrentRequest.IsCancellationRequested)
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
            if (this._response != null && this._response.ReadState == PeekableHTTP1Response.PeekableReadState.Finished)
                FinishedProcessing(null);
        }

        public void OnConnectionClosed()
        {
            HTTPManager.Logger.Information(nameof(FileConnection), $"OnConnectionClosed({this.ContentProvider?.Length}, {this._response?.ReadState})", this.Context);

            // If the consumer still have a request: error it and close the connection
            if (this.CurrentRequest != null && this._response != null)
            {
                FinishedProcessing(new Exception("Underlying TCP connection closed unexpectedly!"));
            }
            else // If no current request: close the connection
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
        }

        public void OnError(Exception e)
        {
            HTTPManager.Logger.Information(nameof(FileConnection), $"OnError({this.ContentProvider?.Length}, {this._response?.ReadState}, {this.ShutdownType})", this.Context);

            if (this.ShutdownType == ShutdownTypes.Immediate)
                return;

            FinishedProcessing(e);
        }

        private void OnCancellationRequested(HTTPRequest req)
        {
            HTTPManager.Logger.Information(nameof(FileConnection), "OnCancellationRequested()", this.Context);

            Interlocked.Exchange(ref this._response, null);
            req.OnCancellationRequested -= OnCancellationRequested;
            this._stream.Dispose();
        }

        void FinishedProcessing(Exception ex)
        {
            // Warning: FinishedProcessing might be called from different threads in parallel:
            //  - send thread triggered by a write failure
            //  - read thread oncontent/OnError/OnConnectionClosed

            var resp = Interlocked.Exchange(ref this._response, null);
            if (resp == null)
                return;

            HTTPManager.Logger.Verbose(nameof(FileConnection), $"{nameof(FinishedProcessing)}({resp}, {ex})", this.Context);

            HTTPManager.LocalCache?.EndReadContent(this._cacheHash, this.Context);
            this._cacheHash = new UnityEngine.Hash128();

            // Unset the consumer, we no longer expect another OnContent call until further notice.
            this._stream?.Unbind();
            this._stream?.Dispose();
            this._stream = null;

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

            HTTPManager.Logger.Verbose(nameof(FileConnection), $"{nameof(FinishedProcessing)} final decision. ResendRequest: {resendRequest}, RequestState: {requestState}, ConnectionState: {connectionState}", this.Context);

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
    }
}
