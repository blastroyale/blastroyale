#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Hosts.Settings;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Network.DNS.Cache;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams;
using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.TLS;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp
{
    /// <summary>
    /// Represents the different steps of the negotiation process.
    /// </summary>
    public enum NegotiationSteps
    {
        Start,
        DNSQuery,
        TCPRace,
        Proxy,
        TLSNegotiation,
        Finish
    }

    /// <summary>
    /// Interface for a peer that participates in the negotiation process.
    /// </summary>
    public interface INegotiationPeer
    {
        /// <summary>
        /// Gets the list of supported ALPN protocol names for negotiation.
        /// </summary>
        /// <param name="negotiator">The negotiation instance.</param>
        /// <returns>A list of supported ALPN protocol names.</returns>
        List<string> GetSupportedProtocolNames(Negotiator negotiator);

        /// <summary>
        /// Indicates whether the negotiation process must stop advancing to the next step.
        /// </summary>
        /// <param name="negotiator">The negotiation instance.</param>
        /// <param name="finishedStep">The step that has just finished.</param>
        /// <param name="nextStep">The next step in the negotiation process.</param>
        /// <param name="error">An optional error encountered during negotiation.</param>
        /// <returns><c>true</c> if negotiation must stop for any reason advancing to the next step; otherwise, <c>false</c>.</returns>
        bool MustStopAdvancingToNextStep(Negotiator negotiator, NegotiationSteps finishedStep, NegotiationSteps nextStep, Exception error);

        /// <summary>
        /// Handles the evaluation of a proxy negotiation failure.
        /// </summary>
        /// <param name="negotiator">The negotiation instance.</param>
        /// <param name="error">The error encountered during proxy negotiation.</param>
        /// <param name="resendForAuthentication">Indicates whether to resend for authentication.</param>
        void EvaluateProxyNegotiationFailure(Negotiator negotiator, Exception error, bool resendForAuthentication);

        /// <summary>
        /// Handles the negotiation failure.
        /// </summary>
        /// <param name="negotiator">The negotiation instance.</param>
        /// <param name="error">The error encountered during negotiation.</param>
        void OnNegotiationFailed(Negotiator negotiator, Exception error);

        /// <summary>
        /// Handles the successful completion of negotiation.
        /// </summary>
        /// <param name="negotiator">The negotiation instance.</param>
        /// <param name="stream">The negotiated stream.</param>
        /// <param name="streamer">The TCP streamer.</param>
        /// <param name="negotiatedProtocol">The negotiated protocol.</param>
        void OnNegotiationFinished(Negotiator negotiator, PeekableContentProviderStream stream, TCPStreamer streamer, string negotiatedProtocol);
    }

    /// <summary>
    /// Represents the parameters for a negotiation.
    /// </summary>
    public sealed class NegotiationParameters
    {
        /// <summary>
        /// Optional proxy instance must be used during negotiation.
        /// </summary>
        public HTTP.Proxies.Proxy proxy;

        /// <summary>
        /// Sets a value indicating whether to create a proxy tunnel.
        /// </summary>
        public bool createProxyTunel;

        /// <summary>
        /// Sets the target URI for negotiation.
        /// </summary>
        public Uri targetUri;

        /// <summary>
        /// Sets a value indicating whether to negotiate TLS.
        /// </summary>
        public bool negotiateTLS;

        /// <summary>
        /// Sets the cancellation token for negotiation.
        /// </summary>
        public CancellationToken token;

        /// <summary>
        /// Sets the <see cref="HostSettings"/> that can be used during the negotiation process.
        /// </summary>
        public HostSettings hostSettings;

        /// <summary>
        /// Optional logging context for debugging purposes.
        /// </summary>
        public LoggingContext context;
    }

    /// <summary>
    /// <para>The Negotiator class acts as a central coordinator for the negotiation of network connections, abstracting away the complexities of DNS resolution, TCP socket creation, proxy negotiation, and TLS setup.
    /// It allows for customization and extensibility, making it a versatile tool for establishing network connections in a flexible and controlled manner.</para>
    /// <list type="bullet">
    ///     <item><description>The Negotiator class represents a component responsible for managing the negotiation process. It helps facilitate communication with various network layers, such as DNS resolution, TCP socket creation, proxy handling, and TLS negotiation.</description></item>
    ///     <item><description>The class is designed to be flexible and extensible by allowing developers to define a custom negotiation peer that implements the INegotiationPeer interface. This allows developers to adapt the negotiation process to specific requirements and protocols.</description></item>
    ///     <item><description>It orchestrates the negotiation process through a series of steps defined by the NegotiationSteps enum. These steps include DNSQuery, TCPRace, Proxy, TLSNegotiation</description></item>
    ///     <item><description>Handles errors and exceptions that may occur during negotiation, ensuring graceful fallback or termination of the negotiation process when necessary.</description></item>
    ///     <item><description>When TLS negotiation is required, it selects the appropriate TLS negotiation method based on the configuration and available options, such as BouncyCastle or the system's TLS framework.</description></item>
    ///     <item><description>If a proxy is configured, the Negotiator class handles proxy negotiation and tunneling, allowing communication through a proxy server.</description></item>
    ///     <item><description>It supports cancellation through a CancellationToken, allowing the negotiation process to be canceled if needed.</description></item>
    ///     <item><description>The class uses extensive logging to provide information about the progress and outcomes of the negotiation process, making it easier to diagnose and debug issues.</description></item>
    /// </list>
    /// </summary>
    public class Negotiator
    {
        /// <summary>
        /// Gets the negotiation peer associated with this negotiator.
        /// </summary>
        public INegotiationPeer Peer { get => this._peer; }
        private INegotiationPeer _peer;

        /// <summary>
        /// Gets the negotiation parameters for this negotiator.
        /// </summary>
        public NegotiationParameters Parameters { get => this._parameters; }
        private NegotiationParameters _parameters;

        /// <summary>
        /// Gets the TCP streamer associated with this negotiator.
        /// </summary>
        public TCPStreamer Streamer { get => this._streamer; }
        private TCPStreamer _streamer;

        /// <summary>
        /// Gets the peekable content provider stream associated with this negotiator.
        /// </summary>
        public PeekableContentProviderStream Stream { get => this._stream; }
        private PeekableContentProviderStream _stream;

        /// <summary>
        /// Initializes a new instance of the Negotiator class.
        /// </summary>
        /// <param name="peer">The negotiation peer for this negotiator.</param>
        /// <param name="parameters">The negotiation parameters for this negotiator.</param>
        public Negotiator(INegotiationPeer peer, NegotiationParameters parameters)
        {
            this._peer = peer;
            this._parameters = parameters;
        }

        /// <summary>
        /// Starts the negotiation process.
        /// </summary>
        public void Start()
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(Start)}()", this._parameters.context);

            if (!this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.Start, NegotiationSteps.DNSQuery, null))
            {
                Uri target = this._parameters.proxy != null && this._parameters.proxy.UseProxyForAddress(this._parameters.targetUri) ? this._parameters.proxy.Address : this._parameters.targetUri;

                var parameters = new DNSQueryParameters(target);
                parameters.Token = this._parameters.token;
                parameters.Context = this._parameters.context;

                parameters.Callback = OnDNSCacheQueryFinished;

                DNSCache.Query(parameters);
            }
        }

        /// <summary>
        /// Handles cancellation requests during negotiation.
        /// </summary>
        public void OnCancellationRequested()
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnCancellationRequested)}()", this._parameters.context);

            this._streamer?.Dispose();
        }

        private void OnDNSCacheQueryFinished(DNSQueryParameters dnsParameters, DNSQueryResult result)
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnDNSCacheQueryFinished)}({dnsParameters}, {result})", this._parameters.context);

            if (!this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.DNSQuery, NegotiationSteps.TCPRace, result.Error))
            {
                var tcpParameters = new TCPRaceParameters
                {
                    Addresses = result.Addresses,
                    Hostname = dnsParameters.Address.Host,
                    Port = dnsParameters.Address.Port,
                    Context = this._parameters.context,
                    Token = this._parameters.token,
                    AnnounceWinnerCallback = OnTCPRaceFinished,
                };

                TCPRingmaster.StartCompetion(tcpParameters);
            }
        }

        private void OnTCPRaceFinished(TCPRaceParameters parameters, TCPRaceResult raceResult)
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnTCPRaceFinished)}({parameters}, {raceResult})", this._parameters.context);

            NegotiationSteps nextStep = this._parameters.proxy != null ? NegotiationSteps.Proxy : NegotiationSteps.TLSNegotiation;

            if (!this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.TCPRace, nextStep, raceResult.Error))
            {
                try
                {
                    SetupSocket(raceResult.WinningSocket, this.Parameters.hostSettings);

                    var lowLevelSettings = this.Parameters.hostSettings.LowLevelConnectionSettings;
                    this._streamer = new TCPStreamer(raceResult.WinningSocket,
                        lowLevelSettings.ReadBufferSize,
                        lowLevelSettings.TCPWriteBufferSize,
                        this._parameters.context);
                    //this._streamer._debugRequest = this._parameters.optionalRequest;

                    this._stream = new NonblockingTCPStream(this._streamer, false, lowLevelSettings.ReadBufferSize);

                    if (this._parameters.proxy != null)
                    {
                        var proxyParameters = new HTTP.Proxies.ProxyConnectParameters()
                        {
                            proxy = this._parameters.proxy,
                            uri = this._parameters.targetUri,
                            token = this._parameters.token,
                            stream = this._stream,
                            context = this._parameters.context,
                            createTunel = this._parameters.negotiateTLS || this._parameters.createProxyTunel,

                            //request = this._parameters.optionalRequest,

                            OnSuccess = OnProxyNegotiated,
                            OnError = OnProxyNegotiationFailed
                        };

                        this._parameters.proxy.BeginConnect(proxyParameters);
                    }
                    else
                    {
                        NegotiateTLS();
                    }
                }
                catch (Exception ex)
                {
                    this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.TCPRace, NegotiationSteps.Finish, ex);
                }
            }
        }

        private void OnProxyNegotiated(HTTP.Proxies.ProxyConnectParameters parameters)
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnProxyNegotiated)}({parameters})", this._parameters.context);

            NegotiationSteps nextStep = this._parameters.negotiateTLS ? NegotiationSteps.TLSNegotiation : NegotiationSteps.Finish;

            if (!this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.Proxy, nextStep, null))
                NegotiateTLS();
        }

        private void OnProxyNegotiationFailed(HTTP.Proxies.ProxyConnectParameters parameters, Exception error, bool resendForAuthentication)
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnProxyNegotiationFailed)}({parameters}, {error}, {resendForAuthentication})", this._parameters.context);

            this._peer.EvaluateProxyNegotiationFailure(this, error, resendForAuthentication);
        }

        private void NegotiateTLS()
        {
            try
            {
                var hostSettings = this.Parameters.hostSettings; //HTTPManager.PerHostSettings.Get(this._parameters.targetUri);

                if (this._parameters.negotiateTLS)
                {
                    HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(NegotiateTLS)}()", this._parameters.context);

                    var handlerType = hostSettings.TLSSettings.TLSHandler;
                    switch (handlerType)
                    {
                        case TLSHandlers.BouncyCastle:
                            {
                                List<ProtocolName> protocols = new List<ProtocolName>();

                                foreach (var protocol in this._peer.GetSupportedProtocolNames(this))
                                    protocols.Add(ProtocolName.AsUtf8Encoding(protocol));

                                AbstractTls13Client tlsClient = null;
                                if (hostSettings.TLSSettings.BouncyCastleSettings.TlsClientFactory == null)
                                {
                                    tlsClient = BouncyCastleSettings.DefaultTlsClientFactory(this._parameters.targetUri, protocols, this._parameters.context);
                                }
                                else
                                {
                                    try
                                    {
                                        tlsClient = hostSettings.TLSSettings.BouncyCastleSettings.TlsClientFactory(this._parameters.targetUri, protocols, this._parameters.context);
                                    }
                                    catch (Exception ex)
                                    {
                                        HTTPManager.Logger.Exception(nameof(Negotiator), nameof(hostSettings.TLSSettings.BouncyCastleSettings.TlsClientFactory), ex, this._parameters.context);
                                    }

                                    if (tlsClient == null)
                                        tlsClient = BouncyCastleSettings.DefaultTlsClientFactory(this._parameters.targetUri, protocols, this._parameters.context);
                                }

                                var handler = new TlsClientProtocol();
                                handler.Connect(tlsClient);

                                new NonblockingBCTLSStream(this._streamer, handler, tlsClient, true, this.Parameters.hostSettings.LowLevelConnectionSettings.ReadBufferSize)
                                    .OnNegotiated = OnBC_TLSNegotiated;
                            }
                            break;

                        case TLSHandlers.Framework:
                            new FrameworkTLSStream(this._streamer, this._parameters.targetUri.Host, this.Parameters.hostSettings)
                                .OnNegotiated = OnFramework_TLSNegotiated;
                            break;

                        default:
                            throw new NotImplementedException($"Not Implemented: {handlerType} TLS Negotiation");
                    }
                }
                else
                {
                    HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(this._peer.OnNegotiationFinished)}()", this._parameters.context);

                    this._stream = new NonblockingTCPStream(this._streamer, true, this.Parameters.hostSettings.LowLevelConnectionSettings.ReadBufferSize);
                    this._peer.OnNegotiationFinished(this, this._stream, this._streamer, HTTPProtocolFactory.W3C_HTTP1);
                }
            }
            catch (Exception ex)
            {
                this._peer.MustStopAdvancingToNextStep(this, NegotiationSteps.TLSNegotiation, NegotiationSteps.Finish, ex);
            }
        }

        private void OnFramework_TLSNegotiated(FrameworkTLSStream stream, TCPStreamer streamer, string alpn, Exception error)
        {
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnFramework_TLSNegotiated)}(\"{alpn}\", {error})", this._parameters.context);

            this._stream = stream;

            if (error != null)
                this._peer.OnNegotiationFailed(this, error);
            else
                this._peer.OnNegotiationFinished(this, stream, streamer, alpn);
        }

        private void OnBC_TLSNegotiated(NonblockingBCTLSStream stream, TCPStreamer streamer, AbstractTls13Client tlsClient, Exception error)
        {
            string alpn = tlsClient.GetNegotiatedApplicationProtocol();
            HTTPManager.Logger.Information(nameof(Negotiator), $"{nameof(OnBC_TLSNegotiated)}(\"{alpn}\", {error})", this._parameters.context);

            // set the stream early if we return because of PreprocessRequestState, Dispose wouldn't call stream's Dispose
            this._stream = stream;

            if (error != null)
                this._peer.OnNegotiationFailed(this, error);
            else
                this._peer.OnNegotiationFinished(this, stream, streamer, alpn);
        }

        private void SetupSocket(System.Net.Sockets.Socket socket, HostSettings hostSettings)
        {
#if UNITY_WINDOWS || UNITY_EDITOR
            // Set the keep-alive time and interval on windows

            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd877220%28v=vs.85%29.aspx
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ee470551%28v=vs.85%29.aspx
            try
            {
                SetKeepAlive(socket, true, 30000, 1000);
            }
            catch { }
#endif
            // data sending is buffered for all protocols, so when we put data into the socket we want to send them asap
            socket.NoDelay = true;
        }

#if UNITY_WINDOWS || UNITY_EDITOR
        private void SetKeepAlive(System.Net.Sockets.Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)keepAliveInterval).CopyTo(inOptionValues, size * 2);

            //client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            int dwBytesRet = 0;
            WSAIoctl(socket.Handle,
                /*SIO_KEEPALIVE_VALS*/ System.Net.Sockets.IOControlCode.KeepAliveValues,
                inOptionValues,
                inOptionValues.Length,
                /*NULL*/IntPtr.Zero,
                0,
                ref dwBytesRet,
                /*NULL*/IntPtr.Zero,
                /*NULL*/IntPtr.Zero);
        }

        [System.Runtime.InteropServices.DllImport("Ws2_32.dll")]
        private static extern int WSAIoctl(
            /* Socket, Mode */               IntPtr s, System.Net.Sockets.IOControlCode dwIoControlCode,
            /* Optional Or IntPtr.Zero, 0 */ byte[] lpvInBuffer, int cbInBuffer,
            /* Optional Or IntPtr.Zero, 0 */ IntPtr lpvOutBuffer, int cbOutBuffer,
            /* reference to receive Size */  ref int lpcbBytesReturned,
            /* IntPtr.Zero, IntPtr.Zero */   IntPtr lpOverlapped, IntPtr lpCompletionRoutine);
#endif
    }
}
#endif
