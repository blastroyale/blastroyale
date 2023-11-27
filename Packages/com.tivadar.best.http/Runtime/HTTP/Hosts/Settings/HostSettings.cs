using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;

namespace Best.HTTP.Hosts.Settings
{
#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
    /// <summary>
    /// Delegate for creating a TLS 1.3 client instance.
    /// </summary>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="protocols">A list of supported TLS ALPN protocols.</param>
    /// <param name="context">The logging context for the operation.</param>
    /// <returns>A TLS 1.3 client instance.</returns>
    public delegate Best.HTTP.Shared.TLS.AbstractTls13Client TlsClientFactoryDelegate(Uri uri, List<SecureProtocol.Org.BouncyCastle.Tls.ProtocolName> protocols, LoggingContext context);
#endif

    /// <summary>
    /// Delegate for selecting a client certificate.
    /// </summary>
    /// <param name="targetHost">The target host.</param>
    /// <param name="localCertificates">A collection of local certificates.</param>
    /// <param name="remoteCertificate">The remote certificate.</param>
    /// <param name="acceptableIssuers">An array of acceptable certificate issuers.</param>
    /// <returns>The selected X.509 certificate.</returns>
    public delegate X509Certificate ClientCertificateSelector(string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers);

    /// <summary>
    /// Available TLS handlers.
    /// </summary>
    public enum TLSHandlers
    {
        /// <summary>
        /// To use the 3rd party BouncyCastle implementation.
        /// </summary>
        BouncyCastle,

        /// <summary>
        /// To use .net's SslStream.
        /// </summary>
        Framework
    }

    /// <summary>
    /// Settings for HTTP requests.
    /// </summary>
    public class HTTRequestSettings
    {
        /// <summary>
        /// The timeout for establishing a connection.
        /// </summary>
        public TimeSpan ConnectTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// The maximum time allowed for the request to complete.
        /// </summary>
        public TimeSpan RequestTimeout = TimeSpan.MaxValue;
    }

    /// <summary>
    /// Settings for HTTP/1 connections.
    /// </summary>
    public class HTTP1ConnectionSettings
    {
        /// <summary>
        /// Indicates whether the connection should be open after receiving the response.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, internal TCP connections will be reused whenever possible.
        /// If making rare requests to the server, it's recommended to change this to <c>false</c>.
        /// </remarks>
        public bool TryToReuseConnections = true;

        /// <summary>
        /// The maximum time a connection can remain idle before being closed.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime = TimeSpan.FromSeconds(20);
    }

#if !UNITY_WEBGL || UNITY_EDITOR

    /// <summary>
    /// Settings for Bouncy Castle TLS.
    /// </summary>
    public class BouncyCastleSettings
    {
#if !BESTHTTP_DISABLE_ALTERNATE_SSL
        /// <summary>
        /// Delegate for creating a TLS 1.3 client instance using Bouncy Castle.
        /// </summary>
        public TlsClientFactoryDelegate TlsClientFactory;

        /// <summary>
        /// The default TLS 1.3 client factory.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="protocols">A list of supported TLS ALPN protocols.</param>
        /// <param name="context">The logging context for the operation.</param>
        /// <returns>A TLS 1.3 client instance.</returns>
        public static Best.HTTP.Shared.TLS.AbstractTls13Client DefaultTlsClientFactory(Uri uri, List<Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.ProtocolName> protocols, LoggingContext context)
        {
            // http://tools.ietf.org/html/rfc3546#section-3.1
            // -It is RECOMMENDED that clients include an extension of type "server_name" in the client hello whenever they locate a server by a supported name type.
            // -Literal IPv4 and IPv6 addresses are not permitted in "HostName".

            // User-defined list has a higher priority
            List<Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.ServerName> hostNames = null;

            // If there's no user defined one and the host isn't an IP address, add the default one
            if (!uri.IsHostIsAnIPAddress())
            {
                hostNames = new List<Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.ServerName>(1);
                hostNames.Add(new Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.ServerName(0, System.Text.Encoding.UTF8.GetBytes(uri.Host)));
            }

            return new Best.HTTP.Shared.TLS.DefaultTls13Client(hostNames, protocols, context);
        }
#endif
    }

    /// <summary>
    /// Settings for .NET's SslStream based handler.
    /// </summary>
    public class FrameworkTLSSettings
    {
        /// <summary>
        /// The supported TLS versions.
        /// </summary>
        public System.Security.Authentication.SslProtocols TlsVersions = System.Security.Authentication.SslProtocols.Tls12;

        /// <summary>
        /// Indicates whether to check certificate revocation.
        /// </summary>
        public bool CheckCertificateRevocation = true;

        /// <summary>
        /// The default certification validator.
        /// </summary>
        public static Func<string, X509Certificate, X509Chain, SslPolicyErrors, bool> DefaultCertificationValidator = (host, certificate, chain, sslPolicyErrors) => true;

        public Func<string, X509Certificate, X509Chain, SslPolicyErrors, bool> CertificationValidator = DefaultCertificationValidator;

        /// <summary>
        /// Delegate for providing a client certificate.
        /// </summary>
        public ClientCertificateSelector ClientCertificationProvider;
    }

    /// <summary>
    /// Settings for TLS.
    /// </summary>
    public class TLSSettings
    {
        /// <summary>
        /// The selected TLS handler.
        /// </summary>
        public TLSHandlers TLSHandler = TLSHandlers.BouncyCastle;

        /// <summary>
        /// Settings for Bouncy Castle.
        /// </summary>
        public BouncyCastleSettings BouncyCastleSettings = new BouncyCastleSettings();

        /// <summary>
        /// .NET's SslStream settings.
        /// </summary>
        public FrameworkTLSSettings FrameworkTLSSettings = new FrameworkTLSSettings();
    }
#endif

    /// <summary>
    /// Settings for <see cref="HostSetting.HostVariant"/>s.
    /// </summary>
    public class HostVariantSettings
    {
        /// <summary>
        /// The maximum number of connections allowed per host variant.
        /// </summary>
        public int MaxConnectionPerVariant = 6;
    }

    /// <summary>
    /// Represents the low-level TCP buffer settings for connections.
    /// </summary>
    public class LowLevelConnectionSettings
    {
        /// <summary>
        /// Gets or sets the size of the TCP write buffer in bytes. 
        /// </summary>
        /// <remarks>
        /// <para>Default value is 1 MiB.</para>
        /// <para>This determines the maximum amount of data that that the <see cref="TCPStreamer"/> class can buffer up if it's already in a write operation.
        /// Increasing this value can potentially improve write performance, especially for large messages or data streams. 
        /// However, setting it too high might consume a significant amount of memory, especially if there are many active connections.
        /// </para>
        /// </remarks>
        /// <value>The size of the TCP write buffer in bytes.</value>
        public uint TCPWriteBufferSize = 1024 * 1024;

        /// <summary>
        /// Gets or sets the size of the read buffer in bytes. 
        /// </summary>
        /// <value>The size of the read buffer in bytes.</value>
        /// <remarks>
        /// <para>Default value is 1 MiB.</para>
        /// <para>This determines the maximum amount of data that low level streams and the <see cref="TCPStreamer"/> can buffer up for consuming by higher level layers.
        /// Adjusting this value can affect the read performance of the application. 
        /// Like the write buffer, setting this too high might be memory-intensive, especially with many connections. 
        /// It's advised to find a balance that suits the application's needs and resources.
        /// </para>
        /// </remarks>
        public uint ReadBufferSize = 1024 * 1024;
    }

    /// <summary>
    /// Contains settings that can be associated with a specific host or host variant.
    /// </summary>
    public class HostSettings
    {
        /// <summary>
        /// Gets or sets the low-level TCP buffer settings for connections associated with the host or host variant.
        /// </summary>
        /// <value>The low-level TCP buffer settings.</value>
        /// <remarks>
        /// These settings determine the buffer sizes for reading from and writing to TCP connections, 
        /// which can impact performance and memory usage.
        /// </remarks>
        public LowLevelConnectionSettings LowLevelConnectionSettings = new LowLevelConnectionSettings();

        /// <summary>
        /// Settings related to HTTP requests made to this host or host variant.
        /// </summary>
        public HTTRequestSettings RequestSettings = new HTTRequestSettings();

        /// <summary>
        /// Settings related to HTTP/1.x connection behavior.
        /// </summary>
        public HTTP1ConnectionSettings HTTP1ConnectionSettings = new HTTP1ConnectionSettings();

#if !UNITY_WEBGL || UNITY_EDITOR
        /// <summary>
        /// Settings related to TCP Ringmaster used in non-webgl platforms.
        /// </summary>
        public TCPRingmasterSettings TCPRingmasterSettings = new TCPRingmasterSettings();

#if !BESTHTTP_DISABLE_HTTP2
        /// <summary>
        /// Settings related to HTTP/2 connection behavior.
        /// </summary>
        public Best.HTTP.Hosts.Connections.HTTP2.HTTP2ConnectionSettings HTTP2ConnectionSettings = new Connections.HTTP2.HTTP2ConnectionSettings();
#endif

        /// <summary>
        /// Settings related to TLS (Transport Layer Security) behavior.
        /// </summary>
        public TLSSettings TLSSettings = new TLSSettings();
#endif

        /// <summary>
        /// Settings related to <see cref="HostSetting.HostVariant"/> behavior.
        /// </summary>
        public HostVariantSettings HostVariantSettings = new HostVariantSettings();
    }
}
