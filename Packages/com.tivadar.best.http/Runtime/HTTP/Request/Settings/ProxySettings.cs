using System;
using System.Text;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Authentication;
using Best.HTTP.Shared;

namespace Best.HTTP.Request.Settings
{
    /// <summary>
    /// Represents settings related to using a proxy server for HTTP requests.
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// Checks if there is a proxy configured for the given URI.
        /// </summary>
        /// <param name="uri">The URI to check for proxy usage.</param>
        /// <returns><c>true</c> if a proxy is configured and should be used for the URI; otherwise, <c>false</c>.</returns>
        public bool HasProxyFor(Uri uri) => Proxy != null && Proxy.UseProxyForAddress(uri);

        /// <summary>
        /// Gets or sets the proxy object used for the request.
        /// </summary>
        public Proxies.Proxy Proxy { get; set; } = HTTPManager.Proxy;

        /// <summary>
        /// Sets up the HTTP request for passing through a proxy server.
        /// </summary>
        /// <param name="request">The HTTP request to set up.</param>
        public void SetupRequest(HTTPRequest request)
        {
            var currentUri = request.CurrentUri;

            bool tryToKeepAlive = HTTPManager.PerHostSettings.Get(currentUri.Host)
                .HTTP1ConnectionSettings
                .TryToReuseConnections;

            if (!HTTPProtocolFactory.IsSecureProtocol(currentUri) && this.HasProxyFor(currentUri) && !request.HasHeader("Proxy-Connection"))
                request.AddHeader("Proxy-Connection", tryToKeepAlive ? "Keep-Alive" : "Close");

            // Proxy Authentication
            if (!HTTPProtocolFactory.IsSecureProtocol(currentUri) && HasProxyFor(currentUri) && this.Proxy.Credentials != null)
            {
                switch (Proxy.Credentials.Type)
                {
                    case AuthenticationTypes.Basic:
                        // With Basic authentication we don't want to wait for a challenge, we will send the hash with the first request
                        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Proxy.Credentials.UserName + ":" + this.Proxy.Credentials.Password));
                        request.SetHeader("Proxy-Authorization", $"Basic {token}");
                        break;

                    case AuthenticationTypes.Unknown:
                    case AuthenticationTypes.Digest:
                        var digest = DigestStore.Get(this.Proxy.Address);
                        if (digest != null)
                        {
                            string authentication = digest.GenerateResponseHeader(this.Proxy.Credentials, false, request.MethodType, currentUri);
                            if (!string.IsNullOrEmpty(authentication))
                                request.SetHeader("Proxy-Authorization", authentication);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Handles the proxy's response with status code <c>407</c>.
        /// </summary>
        /// <param name="request">The HTTP request that received a <c>407</c> response.</param>
        /// <returns><c>true</c> to resend the request through the proxy; otherwise, <c>false</c>.</returns>
        public bool Handle407(HTTPRequest request)
        {
            if (this.Proxy == null)
                return false;

            return this.Proxy.SetupRequest(request);
        }

        /// <summary>
        /// Adds the proxy address to a hash for the given request URI.
        /// </summary>
        /// <param name="requestUri">The request URI for which the proxy address is added to the hash.</param>
        /// <param name="hash">The hash to which the proxy address is added.</param>
        public void AddToHash(Uri requestUri, ref UnityEngine.Hash128 hash)
        {
            if (HasProxyFor(requestUri))
                HostKey.Append(this.Proxy.Address, ref hash);
        }

        public override string ToString()
        {
            return this.Proxy?.Address?.ToString();
        }
    }
}
