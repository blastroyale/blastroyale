using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Request.Authentication;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Proxies
{
    /// <summary>
    /// Represents parameters used when connecting through a proxy server.
    /// </summary>
    /// <remarks>
    /// The ProxyConnectParameters struct defines the parameters required when initiating a connection
    /// through a proxy server. It includes information about the proxy, target URI, and callbacks for success and error handling.
    /// This struct is commonly used during the negotiation steps in the <see cref="Best.HTTP.Shared.PlatformSupport.Network.Tcp.Negotiator"/> class.
    /// </remarks>
    public struct ProxyConnectParameters
    {
        /// <summary>
        /// The maximum number of authentication attempts allowed during proxy connection.
        /// </summary>
        public const int MaxAuthenticationAttempts = 1;

        /// <summary>
        /// The proxy server through which the connection is established.
        /// </summary>
        public Proxy proxy;

        /// <summary>
        /// The stream used for communication with the proxy server.
        /// </summary>
        public PeekableContentProviderStream stream;

        /// <summary>
        /// The target URI to reach through the proxy server.
        /// </summary>
        public Uri uri;

        /// <summary>
        /// A cancellation token that allows canceling the proxy connection operation.
        /// </summary>
        public CancellationToken token;

        /// <summary>
        /// The number of authentication attempts made during proxy connection.
        /// </summary>
        public int AuthenticationAttempts;

        /// <summary>
        /// Gets or sets a value indicating whether to create a proxy tunnel.
        /// </summary>
        /// <remarks>
        /// A proxy tunnel, also known as a TCP tunnel, is established when communication between the client and the target server
        /// needs to be relayed through the proxy without modification. Setting this field to <c>true</c> indicates the intention
        /// to create a tunnel, allowing the data to pass through the proxy without interpretation or alteration by the proxy.
        /// This is typically used for protocols like HTTPS, where end-to-end encryption is desired, and the proxy should act as a
        /// pass-through conduit.
        /// </remarks>
        public bool createTunel;

        /// <summary>
        /// The logging context for debugging purposes.
        /// </summary>
        public LoggingContext context;

        /// <summary>
        /// A callback to be executed upon successful proxy connection.
        /// </summary>
        public Action<ProxyConnectParameters> OnSuccess;

        /// <summary>
        /// A callback to be executed upon encountering an error during proxy connection.
        /// </summary>
        /// <remarks>
        /// The callback includes parameters for the current connection parameters, the encountered exception,
        /// and a flag indicating whether the connection should be retried for authentication.
        /// </remarks>
        public Action<ProxyConnectParameters, Exception, bool> OnError;
    }

    /// <summary>
    /// Base class for proxy implementations, providing common proxy configuration and behavior.
    /// </summary>
    /// <remarks>
    /// The Proxy class serves as the base class for various proxy client implementations,
    /// such as <see cref="HTTPProxy"/> and <see cref="SOCKSProxy"/>. It provides a foundation for configuring proxy settings and handling
    /// proxy-related functionality common to all proxy types, like connecting to a proxy, setting up a request to go through the proxy
    /// and deciding whether an address is usable with the proxy or the plugin must connect directly.
    /// </remarks>
    public abstract class Proxy
    {
        /// <summary>
        /// Address of the proxy server. It has to be in the http://proxyaddress:port form.
        /// </summary>
        public Uri Address { get; set; }

        /// <summary>
        /// Credentials for authenticating with the proxy server.
        /// </summary>
        public Credentials Credentials { get; set; }

        /// <summary>
        /// List of exceptions for which the proxy should not be used. Elements of this list are compared to the Host (DNS or IP address) part of the uri.
        /// </summary>
        public List<string> Exceptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the Proxy class with the specified proxy address and credentials.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication.</param>
        internal Proxy(Uri address, Credentials credentials)
        {
            this.Address = address;
            this.Credentials = credentials;
        }

        /// <summary>
        /// Initiates a connection through the proxy server. Used during the negotiation steps.
        /// </summary>
        /// <param name="parameters">Parameters for the proxy connection.</param>
        internal abstract void BeginConnect(ProxyConnectParameters parameters);

        /// <summary>
        /// Gets the request path to be used for proxy communication. In some cases with HTTPProxy, the request must send the whole uri as the request path.
        /// </summary>
        /// <param name="uri">The target URI.</param>
        /// <returns>The request path for proxy communication.</returns>
        internal abstract string GetRequestPath(Uri uri);

        /// <summary>
        /// Sets up an HTTP request to use the proxy as needed.
        /// </summary>
        /// <param name="request">The HTTP request to set up.</param>
        /// <returns><c>true</c> if the request should use the proxy; otherwise, <c>false</c>.</returns>
        internal abstract bool SetupRequest(HTTPRequest request);

        /// <summary>
        /// Determines whether the proxy should be used for a specific address based on the configured exceptions.
        /// </summary>
        /// <param name="address">The address to check for proxy usage.</param>
        /// <returns><c>true</c> if the proxy should be used for the address; otherwise, <c>false</c>.</returns>
        public bool UseProxyForAddress(Uri address)
        {
            if (this.Exceptions == null)
                return true;

            string host = address.Host;

            // https://github.com/httplib2/httplib2/issues/94
            // If domain starts with a dot (example: .example.com):
            //  1. Use endswith to match any subdomain (foo.example.com should match)
            //  2. Remove the dot and do an exact match (example.com should also match)
            //
            // If domain does not start with a dot (example: example.com):
            //  1. It should be an exact match.
            for (int i = 0; i < this.Exceptions.Count; ++i)
            {
                var exception = this.Exceptions[i];

                if (exception == "*")
                    return false;

                if (exception.StartsWith("."))
                {
                    // Use EndsWith to match any subdomain
                    if (host.EndsWith(exception))
                        return false;

                    // Remove the dot and
                    exception = exception.Substring(1);
                }

                // do an exact match
                if (host.Equals(exception))
                    return false;
            }

            return true;
        }
    }
}
