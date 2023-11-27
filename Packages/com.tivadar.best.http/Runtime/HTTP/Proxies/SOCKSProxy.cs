#if !UNITY_WEBGL || UNITY_EDITOR
using System;

using Best.HTTP.Proxies.Implementations;
using Best.HTTP.Request.Authentication;
using Best.HTTP.Shared.Extensions;

namespace Best.HTTP.Proxies
{
    /// <summary>
    /// Represents a SOCKS proxy used for making HTTP requests, supporting SOCKS version 5 (v5).
    /// </summary>
    public sealed class SOCKSProxy : Proxy
    {
        /// <summary>
        /// Initializes a new instance of the SOCKSProxy class with the specified proxy address and credentials.
        /// </summary>
        /// <param name="address">The address of the SOCKS proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication (if required).</param>
        public SOCKSProxy(Uri address, Credentials credentials)
            : base(address, credentials)
        { }

        internal override string GetRequestPath(Uri uri) => uri.GetRequestPathAndQueryURL();
        internal override bool SetupRequest(HTTPRequest request) => false;
        internal override void BeginConnect(ProxyConnectParameters parameters) => new SOCKSV5Negotiator(this, parameters);
    }
}
#endif
