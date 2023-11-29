#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Hosts.Settings;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    /*
     * --> FrameworkTLSStream.Write => SslStream.Write => TLSByteForwarder.Write => TCPStream.EnqueueToSend
     * 
     * --> TLSByteForwarder.OnContent => SslStream.Read => FrameworkTLSStream.Read
     * */
    public sealed class FrameworkTLSStream : PeekableContentProviderStream, ITCPStreamerContentConsumer
    {
        public Action<FrameworkTLSStream, TCPStreamer, string /*negotiated appplication protocol*/, Exception> OnNegotiated;

        private string _targetHost;
        private TCPStreamer _streamer;
        private FrameworkTLSByteForwarder _forwarder;
        private SslStream _sslStream;
        private LoggingContext _context;
        private HostSettings _hostSettings;
        private uint _maxBufferSize;

        private int peek_listIdx;
        private int peek_pos;

#if UNITY_2021_2_OR_NEWER
        private static bool loggedWarning = false;
#endif
        private object locker = new object();

        public FrameworkTLSStream(TCPStreamer streamer, string targetHost, HostSettings hostSettings)
        {
            this._streamer = streamer;
            this._targetHost = targetHost;
            this._context = new LoggingContext(this);
            this._context.Add("streamer", this._streamer.Context);

            this._hostSettings = hostSettings;
            this._maxBufferSize = hostSettings.LowLevelConnectionSettings.ReadBufferSize;

            this._forwarder = new FrameworkTLSByteForwarder(this._streamer, this, this._maxBufferSize, this._context);
            this._sslStream = new SslStream(this._forwarder,
                leaveInnerStreamOpen: false,
                OnUserCertificationValidation,
                OnUserCertificationSelection,
                EncryptionPolicy.RequireEncryption);

            this._sslStream.BeginAuthenticateAsClient(targetHost,
                null,
                this._hostSettings.TLSSettings.FrameworkTLSSettings.TlsVersions,
                true,
                OnAuthenticatedAsClient,
                null);
        }

        private bool OnUserCertificationValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            HTTPManager.Logger.Information(nameof(FrameworkTLSStream), $"{nameof(OnUserCertificationValidation)}({sender}, {certificate}, {chain}, {sslPolicyErrors})", this._context);

            var validator = this._hostSettings.TLSSettings.FrameworkTLSSettings.CertificationValidator;
            if (validator == null)
                return FrameworkTLSSettings.DefaultCertificationValidator(_targetHost, certificate, chain, sslPolicyErrors);

            return validator(this._targetHost, certificate, chain, sslPolicyErrors);
        }

        private X509Certificate OnUserCertificationSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            HTTPManager.Logger.Information(nameof(FrameworkTLSStream), $"{nameof(OnUserCertificationSelection)}({sender}, {targetHost}, {localCertificates}, {remoteCertificate}, {acceptableIssuers?.Length})", this._context);

            return this._hostSettings.TLSSettings.FrameworkTLSSettings.ClientCertificationProvider?.Invoke(targetHost, localCertificates, remoteCertificate, acceptableIssuers);
        }

        private void OnAuthenticatedAsClient(IAsyncResult ar)
        {
            HTTPManager.Logger.Information(nameof(FrameworkTLSStream), $"{nameof(OnAuthenticatedAsClient)}()", this._context);

            try
            {
                this._sslStream.EndAuthenticateAsClient(ar);

                string alpn = string.Empty;

#if UNITY_2021_2_OR_NEWER
                try
                {
                    alpn = this._sslStream.NegotiatedApplicationProtocol.ToString();
                }
                catch (Exception ex)
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Exception(nameof(FrameworkTLSStream), $"{nameof(OnAuthenticatedAsClient)}() - NegotiatedApplicationProtocol", ex, this._context);

                    if (!loggedWarning)
                    {
                        loggedWarning = true;
                        HTTPManager.Logger.Warning(nameof(FrameworkTLSStream), $"{nameof(OnAuthenticatedAsClient)}(): SslStream's NegotiatedApplicationProtocol inaccessible! Using http/1.", this._context);
                    }
                }
#endif

                if (string.IsNullOrEmpty(alpn))
                    alpn = HTTPProtocolFactory.W3C_HTTP1;

                CallOnNegotiated(alpn, null);

                BeginRead();
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(FrameworkTLSStream), $"{nameof(OnAuthenticatedAsClient)}()", ex, this._context);

                CallOnNegotiated(null, ex);
            }
        }

        bool CallOnNegotiated(string alpn, Exception error)
        {
            HTTPManager.Logger.Verbose(nameof(FrameworkTLSStream), $"CallOnNegotiated(\"{alpn}\", {error})", this._context);

            var callback = Interlocked.CompareExchange(ref this.OnNegotiated, null, this.OnNegotiated);
            if (callback != null)
            {
                try
                {
                    callback(this, this._streamer, alpn, error);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(FrameworkTLSStream), "OnContent - OnNegotiated", ex, this._streamer.Context);
                }
            }

            return callback != null;
        }

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

        public void OnContent(TCPStreamer streamer)
        {
            if (this._sslStream.IsAuthenticated)
                BeginRead();
        }

        int _reading;
        private void BeginRead()
        {
            if (Interlocked.CompareExchange(ref _reading, 1, 0) != 0)
            {
                HTTPManager.Logger.Warning(nameof(FrameworkTLSStream), $"{nameof(BeginRead)}() - already reading!", this._context);
                return;
            }

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(FrameworkTLSStream), $"{nameof(BeginRead)}()", this._context);

            var buffer = BufferPool.Get(this._maxBufferSize, true, this._context);

            this._sslStream.ReadAsync(buffer, 0, buffer.Length)
                //.AsTask()
                .ContinueWith((ti) =>
                {
                    int readCount = 0;
                    try
                    {
                        readCount = ti.Result;

                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Verbose(nameof(FrameworkTLSStream), $"{nameof(OnRead)}({readCount}, {this.Length})", this._context);

                        if (readCount > 0)
                        {
                            lock (locker)
                                base.Write(buffer.AsBuffer(readCount));
                        }

                        this.Consumer?.OnContent();
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _reading, 0);

                        if (readCount > 0)
                            BeginRead();
                    }
                })
                .ConfigureAwait(false);

            /*IAsyncResult ar = null;
            try
            {
                do
                {
                    var buffer = BufferPool.Get(this._maxBufferSize, true, this._context);

                    ar = this._sslStream.BeginRead(buffer, 0, buffer.Length, OnRead, buffer);
                } while (ar != null && ar.CompletedSynchronously);
            }
            finally
            {
                Interlocked.Exchange(ref _reading, 0);

                //if (ar is not null && ar.CompletedSynchronously)
                //    BeginRead();
            }*/
        }

        private void OnRead(IAsyncResult ar)
        {
            try
            {
                var readCount = this._sslStream.EndRead(ar);

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(FrameworkTLSStream), $"{nameof(OnRead)}({readCount}, {ar.CompletedSynchronously})", this._context);

                if (readCount > 0)
                {
                    var buffer = ar.AsyncState as byte[];
                    lock (locker)
                        base.Write(buffer.AsBuffer(readCount));

                    this.Consumer?.OnContent();
                }

                // This call might fail if the read completed synchronously.
                BeginRead();
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(FrameworkTLSStream), $"EndRead", ex, this._context);
            }
            finally
            {

            }
        }

        public void OnConnectionClosed(TCPStreamer streamer) => this.Consumer?.OnConnectionClosed();

        public void OnError(TCPStreamer streamer, Exception ex) => this.Consumer?.OnError(ex);

        public override int Read(byte[] buffer, int offset, int count) { lock (locker) return base.Read(buffer, offset, count); }

        public override void Write(byte[] buffer, int offset, int count) => this._sslStream.Write(buffer, offset, count);

        public override void Write(BufferSegment bufferSegment)
        {
            using var _ = new AutoReleaseBuffer(bufferSegment);
            this.Write(bufferSegment.Data, bufferSegment.Offset, bufferSegment.Count);
        }

        public override void Flush() => this._sslStream.Flush();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._sslStream?.Dispose();
            this._sslStream = null;
        }
    }
}
#endif
