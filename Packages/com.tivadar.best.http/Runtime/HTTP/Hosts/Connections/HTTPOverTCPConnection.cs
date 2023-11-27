#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.Collections.Generic;

using Best.HTTP.Hosts.Connections.HTTP1;
using Best.HTTP.Hosts.Connections.HTTP2;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Timings;
using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Hosts.Connections
{
    // DNS -> TCP -> [ Proxy ] -> [ BC TLS | Framework TLS ] -> (HTTP/1 | HTTP/2)

    /// <summary>
    /// Represents and manages a connection to a server.
    /// </summary>
    public sealed class HTTPOverTCPConnection : ConnectionBase, INegotiationPeer
    {
        public PeekableContentProviderStream TopStream { get => this._negotiator.Stream; }
        public TCPStreamer Streamer { get => this._negotiator.Streamer; }

        public IHTTPRequestHandler requestHandler;

        public override TimeSpan KeepAliveTime
        {
            get
            {
                if (this.requestHandler != null && this.requestHandler.KeepAlive != null)
                {
                    if (this.requestHandler.KeepAlive.MaxRequests > 0)
                    {
                        if (base.KeepAliveTime < this.requestHandler.KeepAlive.TimeOut)
                            return base.KeepAliveTime;
                        else
                            return this.requestHandler.KeepAlive.TimeOut;
                    }
                    else
                        return TimeSpan.Zero;
                }

                return base.KeepAliveTime;
            }

            protected set
            {
                base.KeepAliveTime = value;
            }
        }

        public override bool CanProcessMultiple
        {
            get
            {
                if (this.requestHandler != null)
                    return this.requestHandler.CanProcessMultiple;
                return base.CanProcessMultiple;
            }
        }

        private Negotiator _negotiator;

        internal HTTPOverTCPConnection(HostKey hostKey)
            : base(hostKey)
        { }

        internal override void Process(HTTPRequest request)
        {
            this.LastProcessedUri = request.CurrentUri;
            this.CurrentRequest = request;
            this.State = HTTPConnectionStates.Processing;

            if (this.requestHandler == null)
            {
                try
                {
                    NegotiationParameters parameters = new NegotiationParameters();
                    parameters.context = this.Context;
                    parameters.proxy = CurrentRequest.ProxySettings.Proxy;
                    parameters.targetUri = CurrentRequest.CurrentUri;
                    parameters.negotiateTLS = HTTPProtocolFactory.IsSecureProtocol(CurrentRequest.CurrentUri);
                    parameters.token = CurrentRequest.CancellationTokenSource.Token;

                    //parameters.tryToKeepAlive = HTTPManager.PerHostSettings.Get(CurrentRequest.CurrentUri.Host).HTTP1ConnectionSettings.TryToReuseConnections;

                    parameters.hostSettings = HTTPManager.PerHostSettings.Get(CurrentRequest.CurrentUri.Host);

                    this._negotiator = new Negotiator(this, parameters);
                    this._negotiator.Start();
                }
                catch(Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(HTTPOverTCPConnection), $"Process({request})", ex, this.Context);
                    TrySetErrorState(request, ex);
                }
            }
            else
            {
                this.requestHandler.Process(request);
                LastProcessTime = DateTime.Now;
            }
        }

        List<string> INegotiationPeer.GetSupportedProtocolNames(Negotiator negotiator)
        {
            List<string> protocols = new List<string>();

#if !BESTHTTP_DISABLE_HTTP2
            SupportedProtocols protocol = HTTPProtocolFactory.GetProtocolFromUri(negotiator.Parameters.targetUri);

            if (protocol == SupportedProtocols.HTTP && negotiator.Parameters.hostSettings.HTTP2ConnectionSettings.EnableHTTP2Connections)
            {
                // http/2 over tls (https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xhtml#alpn-protocol-ids)
                protocols.Add(HTTPProtocolFactory.W3C_HTTP2);
            }
#endif

            protocols.Add(HTTPProtocolFactory.W3C_HTTP1);

            return protocols;
        }

        bool INegotiationPeer.MustStopAdvancingToNextStep(Negotiator negotiator, NegotiationSteps finishedStep, NegotiationSteps nextStep, Exception error)
        {
            if (TrySetErrorState(CurrentRequest, error))
                return true;

            switch (finishedStep)
            {
                case NegotiationSteps.Start:
                    this.LastProcessTime = DateTime.Now;

                    this.CurrentRequest.Timing.StartNext(TimingEventNames.DNS_Lookup);
                    break;

                case NegotiationSteps.DNSQuery:
                    this.CurrentRequest.Timing.StartNext(TimingEventNames.TCP_Connection);
                    break;

                case NegotiationSteps.TCPRace:
                    CurrentRequest.OnCancellationRequested += OnCancellationRequested;

                    CurrentRequest.Timing.StartNext(TimingEventNames.Proxy_Negotiation);
                    break;

                case NegotiationSteps.Proxy:
                    CurrentRequest.Timing.StartNext(TimingEventNames.TLS_Negotiation);
                    break;

                case NegotiationSteps.TLSNegotiation:
                    break;

                case NegotiationSteps.Finish:

                    break;
            }

            return false;
        }

        void INegotiationPeer.EvaluateProxyNegotiationFailure(Negotiator negotiator, Exception error, bool resendForAuthentication)
        {
            if (resendForAuthentication && !this.TrySetErrorState(CurrentRequest, null))
            {
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(CurrentRequest, RequestEvents.Resend));
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
            }
            else if (!this.TrySetErrorState(CurrentRequest, error))
            {
                // TODO: what?
            }
        }

        void INegotiationPeer.OnNegotiationFailed(Negotiator negotiator, Exception error)
        {
            PreprocessRequestState(error);
        }

        void INegotiationPeer.OnNegotiationFinished(Negotiator negotiator, PeekableContentProviderStream stream, TCPStreamer streamer, string negotiatedProtocol)
        {
            if (!PreprocessRequestState(null))
                StartWithNegotiatedProtocol(negotiatedProtocol, stream);
        }

        private void OnCancellationRequested(HTTPRequest req)
        {
            
            HTTPManager.Logger.Information(nameof(HTTPOverTCPConnection), $"{nameof(OnCancellationRequested)}({req})", this.Context);
        
            CurrentRequest.OnCancellationRequested -= OnCancellationRequested;

            this._negotiator?.OnCancellationRequested();            

            //ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
        }

        private bool PreprocessRequestState(Exception error)
        {
            CurrentRequest.OnCancellationRequested -= OnCancellationRequested;

            HTTPManager.Logger.Information(nameof(HTTPOverTCPConnection), $"PreprocessRequestState({CurrentRequest}, {error})", this.Context);

            // OnTLSNegotiated might get called _after_ the request is aborted. In this case, we must not set its State!
            // So here we have to check its State, if it's one of the Finished state (Finished, Error, etc.) we have to quit early and only enqueue a connection event.
            if (CurrentRequest.State >= HTTPRequestStates.Finished)
            {
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
                return true;
            }

            return TrySetErrorState(CurrentRequest, error);
        }

        /// <summary>
        /// Returns true if an error state is set to the request and the connection is closing.
        /// </summary>
        bool TrySetErrorState(HTTPRequest request, Exception ex)
        {
            // Check wether the request is already in a finshed state.
            // For example it can happen in the following case:
            //  1.) HTTP proxy sends out a CONNECT request to the proxy
            //  2.) Request times out and RequestEventHelper.AbortRequestWhenTimedOut is called
            //      2.a) Request's state set to ConnectionTimedOut
            //  3.) Request's callback is called
            //  4.) Either the Proxy connects or fails to connect to the remote host, but one of the first call in the callbacks is TrySetErrorState,
            //          where we would try to set the request's State. If we would set a different state (like Error or TimedOut) than the one we already set (ConnectionTimedOut in this specific case)
            //          then a new RequestEvents.StateChange event would be queued up and resulting in a new calling the request's callback again! 
            if (request.State >= HTTPRequestStates.Finished)
            {
                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
                return true;
            }

            if (ex != null)
            {
                request.Timing.StartNext(TimingEventNames.Queued);

                ConnectionHelper.EnqueueEvents(this,
                    HTTPConnectionStates.Closed,
                    request,
                    ex is TimeoutException ? HTTPRequestStates.ConnectionTimedOut : HTTPRequestStates.Error,
                    ex is TimeoutException ? (Exception)null : ex);
                return true;
            }
            else if (request.TimeoutSettings.IsConnectTimedOut(DateTime.Now))
            {
                TrySetErrorState(request, new TimeoutException("request.IsConnectTimedOut"));
                return true;
            }
            else if (request.IsCancellationRequested)
            {
                ConnectionHelper.EnqueueEvents(this, HTTPConnectionStates.Closed, request, HTTPRequestStates.Aborted, null);
                return true;
            }

            return false;
        }

        void StartWithNegotiatedProtocol(string negotiatedProtocol, PeekableContentProviderStream stream)
        {
            this.CurrentRequest.Timing.StartNext(TimingEventNames.Queued);

            if (string.IsNullOrEmpty(negotiatedProtocol))
                negotiatedProtocol = HTTPProtocolFactory.W3C_HTTP1;

            HTTPManager.Logger.Information(nameof(HTTPOverTCPConnection), $"Negotiated protocol through ALPN: '{negotiatedProtocol}'", this.Context);

            switch (negotiatedProtocol)
            {
                case HTTPProtocolFactory.W3C_HTTP1:
                    var http1Consumer = new HTTP1ContentConsumer(this);

                    this.requestHandler = http1Consumer;
                    stream.SetTwoWayBinding(http1Consumer);

                    ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HostProtocolSupport.HTTP1));
                    break;

#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2
                case HTTPProtocolFactory.W3C_HTTP2:
                    var http2Consumer = new HTTP2ContentConsumer(this);

                    this.requestHandler = http2Consumer;
                    stream.SetTwoWayBinding(http2Consumer);

                    this.CurrentRequest = null;

                    ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HostProtocolSupport.HTTP2));
                    break;
#endif

                default:
                    HTTPManager.Logger.Error(nameof(HTTPOverTCPConnection), $"Unknown negotiated protocol: {negotiatedProtocol}", this.Context);

                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(CurrentRequest, RequestEvents.Resend));
                    ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this, HTTPConnectionStates.Closed));
                    return;
            }

            this.requestHandler.Context.Add("Connection", this.Context.GetStringField("Hash"));
            this.Context.Add("RequestHandler", this.requestHandler.Context.GetStringField("Hash"));

            LastProcessTime = DateTime.Now;
            if (IsThreaded)
                ThreadedRunner.RunLongLiving(ThreadFunc);
            else
                ThreadFunc();
        }

        protected override void ThreadFunc()
        {
            this.requestHandler.RunHandler();
        }

        public override void Shutdown(ShutdownTypes type)
        {
            base.Shutdown(type);

            if (this.requestHandler != null)
                this.requestHandler.Shutdown(type);
            else
            {
                // if the request handler is null, we can't do a gentle shutdown.
                this._negotiator?.Streamer?.Close();
            }

            switch (this.ShutdownType)
            {
                case ShutdownTypes.Immediate:
                    this._negotiator.Stream?.Dispose();
                    break;

                //case ShutdownTypes.Gentle:
                //    this._streamer?.Close();
                //    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LastProcessedUri = null;
                if (this.State != HTTPConnectionStates.WaitForProtocolShutdown)
                {
                    this._negotiator?.Stream?.Dispose();

                    if (this.requestHandler != null)
                    {
                        try
                        {
                            this.requestHandler.Dispose();
                        }
                        catch
                        { }
                        this.requestHandler = null;
                    }

                    this._negotiator?.Streamer?.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}

#endif
