#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using Best.HTTP.Request.Authentication;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

using static Best.HTTP.Hosts.Connections.HTTP1.Constants;

namespace Best.HTTP.Proxies
{
    /// <summary>
    /// Represents an HTTP proxy server that can be used to route HTTP requests through.
    /// </summary>
    /// <remarks>
    /// The HTTPProxy class is an implementation of the <see cref="Proxy"/> base class, specifically designed for
    /// HTTP proxy servers. It provides features such as transparent proxy support, sending the entire URI, and handling proxy
    /// authentication. This class is used to configure and manage HTTP proxy settings for HTTP requests.
    /// </remarks>
    public sealed class HTTPProxy : Proxy
    {
        /// <summary>
        /// Gets or sets whether the proxy can act as a transparent proxy. Default value is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// A transparent proxy forwards client requests without modifying them. When set to <c>true</c>, the proxy behaves as a transparent
        /// proxy, meaning it forwards requests as-is. If set to <c>false</c>, it may modify requests, and this can be useful for certain
        /// advanced proxy configurations.
        /// </remarks>
        public bool IsTransparent { get; set; }

        /// <summary>
        /// Gets or sets whether the proxy - when it's in non-transparent mode - excepts only the path and query of the request URI. Default value is <c>true</c>.
        /// </summary>
        public bool SendWholeUri { get; set; }

        /// <summary>
        /// Gets or sets whether the plugin will use the proxy as an explicit proxy for secure protocols (HTTPS://, WSS://).
        /// </summary>
        /// <remarks>
        /// When set to <c>true</c>, the plugin will issue a CONNECT request to the proxy for secure protocols, even if the proxy is
        /// marked as transparent. This is commonly used for ensuring proper handling of encrypted traffic through the proxy.
        /// </remarks>
        public bool NonTransparentForHTTPS { get; set; }

        /// <summary>
        /// Creates a new instance of the HTTPProxy class with the specified proxy address.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        public HTTPProxy(Uri address)
            :this(address, null, true)
        {}

        /// <summary>
        /// Creates a new instance of the HTTPProxy class with the specified proxy address and credentials.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication.</param>
        public HTTPProxy(Uri address, Credentials credentials)
            :this(address, credentials, true)
        {}

        /// <summary>
        /// Creates a new instance of the HTTPProxy class with the specified proxy address, credentials, and transparency settings.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication.</param>
        /// <param name="isTransparent">Specifies whether the proxy can act as a transparent proxy (<c>true</c>) or not (<c>false</c>).</param>
        public HTTPProxy(Uri address, Credentials credentials, bool isTransparent)
            :this(address, credentials, isTransparent, true)
        { }

        /// <summary>
        /// Creates a new instance of the HTTPProxy class with the specified proxy address, credentials, transparency settings, and URI handling.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication.</param>
        /// <param name="isTransparent">Specifies whether the proxy can act as a transparent proxy (<c>true</c>) or not (<c>false</c>).</param>
        /// <param name="sendWholeUri">Specifies whether the proxy should send the entire URI (<c>true</c>) or just the path and query (<c>false</c>) for non-transparent proxies.</param>
        public HTTPProxy(Uri address, Credentials credentials, bool isTransparent, bool sendWholeUri)
            : this(address, credentials, isTransparent, sendWholeUri, true)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="HTTPProxy"/> class with the specified proxy address, credentials, transparency settings, URI handling, and HTTPS behavior.
        /// </summary>
        /// <param name="address">The address of the proxy server.</param>
        /// <param name="credentials">The credentials for proxy authentication.</param>
        /// <param name="isTransparent">Specifies whether the proxy can act as a transparent proxy (<c>true</c>) or not (<c>false</c>).</param>
        /// <param name="sendWholeUri">Specifies whether the proxy should send the entire URI (<c>true</c>) or just the path and query (<c>false</c>) for non-transparent proxies.</param>
        /// <param name="nonTransparentForHTTPS">Specifies whether the plugin should use the proxy as an explicit proxy for secure protocols (HTTPS://, WSS://) (<c>true</c>) or not (<c>false</c>).</param>
        public HTTPProxy(Uri address, Credentials credentials, bool isTransparent, bool sendWholeUri, bool nonTransparentForHTTPS)
            :base(address, credentials)
        {
            this.IsTransparent = isTransparent;
            this.SendWholeUri = sendWholeUri;
            this.NonTransparentForHTTPS = nonTransparentForHTTPS;
        }

        internal override string GetRequestPath(Uri uri)
        {
            return this.SendWholeUri ? uri.OriginalString : uri.GetRequestPathAndQueryURL();
        }

        internal override bool SetupRequest(HTTPRequest request)
        {
            if (request == null || request.Response == null || !this.IsTransparent)
                return false;

            string authHeader = DigestStore.FindBest(request.Response.GetHeaderValues("proxy-authenticate"));
            if (!string.IsNullOrEmpty(authHeader))
            {
                var digest = DigestStore.GetOrCreate(this.Address);
                digest.ParseChallange(authHeader);

                if (this.Credentials != null && digest.IsUriProtected(this.Address) && (!request.HasHeader("Proxy-Authorization") || digest.Stale))
                {
                    switch (this.Credentials.Type)
                    {
                        case AuthenticationTypes.Basic:
                            // With Basic authentication we don't want to wait for a challenge, we will send the hash with the first request
                            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Credentials.UserName + ":" + this.Credentials.Password));
                            request.SetHeader("Proxy-Authorization", $"Basic {token}");
                            return true;

                        case AuthenticationTypes.Unknown:
                        case AuthenticationTypes.Digest:
                            //var digest = DigestStore.Get(request.Proxy.Address);
                            if (digest != null)
                            {
                                string authentication = digest.GenerateResponseHeader(this.Credentials, true, request.MethodType, request.CurrentUri);
                                if (!string.IsNullOrEmpty(authentication))
                                {
                                    request.SetHeader("Proxy-Authorization", authentication);
                                    return true;
                                }
                            }

                            break;
                    }
                }
            }

            return false;
        }

        internal override void BeginConnect(ProxyConnectParameters parameters)
        {
            if (!this.IsTransparent || (parameters.createTunel && this.NonTransparentForHTTPS))
            {
                using (var bufferedStream = new WriteOnlyBufferedStream(parameters.stream, 4 * 1024, parameters.context))
                using (var outStream = new BinaryWriter(bufferedStream, Encoding.UTF8))
                {
                    // https://www.rfc-editor.org/rfc/rfc9110.html#name-connect

                    string connectStr = string.Format("CONNECT {0}:{1} HTTP/1.1", parameters.uri.Host, parameters.uri.Port.ToString());

                    HTTPManager.Logger.Information("HTTPProxy", "Sending " + connectStr, parameters.context);

                    outStream.SendAsASCII(connectStr);
                    outStream.Write(EOL);

                    outStream.SendAsASCII(string.Format("Host: {0}:{1}", parameters.uri.Host, parameters.uri.Port.ToString()));
                    outStream.Write(EOL);

                    outStream.SendAsASCII("Proxy-Connection: Keep-Alive");
                    outStream.Write(EOL);

                    outStream.SendAsASCII("Connection: Keep-Alive");
                    outStream.Write(EOL);

                    // Proxy Authentication
                    if (this.Credentials != null)
                    {
                        switch (this.Credentials.Type)
                        {
                            case AuthenticationTypes.Basic:
                                {
                                    // With Basic authentication we don't want to wait for a challenge, we will send the hash with the first request
                                    var buff = $"Proxy-Authorization: Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Credentials.UserName + ":" + this.Credentials.Password))}"
                                        .GetASCIIBytes();
                                    outStream.Write(buff.Data, buff.Offset, buff.Count);
                                    BufferPool.Release(buff);

                                    outStream.Write(EOL);
                                    break;
                                }

                            case AuthenticationTypes.Unknown:
                            case AuthenticationTypes.Digest:
                                {
                                    var digest = DigestStore.Get(this.Address);
                                    if (digest != null)
                                    {
                                        string authentication = digest.GenerateResponseHeader(this.Credentials, true, HTTPMethods.Connect, parameters.uri);
                                        if (!string.IsNullOrEmpty(authentication))
                                        {
                                            string auth = string.Format("Proxy-Authorization: {0}", authentication);
                                            if (HTTPManager.Logger.Level <= Loglevels.Information)
                                                HTTPManager.Logger.Information("HTTPProxy", "Sending proxy authorization header: " + auth, parameters.context);

                                            var buff = auth.GetASCIIBytes();
                                            outStream.Write(buff.Data, buff.Offset, buff.Count);
                                            BufferPool.Release(buff);

                                            outStream.Write(EOL);
                                        }
                                    }

                                    break;
                                }
                        }
                    }

                    outStream.Write(EOL);

                    // Make sure to send all the wrote data to the wire
                    outStream.Flush();
                } // using outstream

                new HTTPProxyResponse(parameters)
                    .OnFinished = OnProxyResponse;
            }
            else
                parameters.OnSuccess?.Invoke(parameters);
        }

        void OnProxyResponse(ProxyConnectParameters connectParameters, HTTPProxyResponse resp, Exception error)
        {
            HTTPManager.Logger.Information(nameof(HTTPProxyResponse), $"{nameof(OnProxyResponse)}({connectParameters}, {resp}, {error})", connectParameters.context);

            if (error != null)
            {
                // Resend request if the proxy response could be read && status code is 407 (authentication required) && we have credentials
                connectParameters.OnError?.Invoke(connectParameters, error, resp.ReadState == HTTPProxyResponse.PeekableReadState.Finished && resp.StatusCode == 407 && this.Credentials != null);
            }
            else
            {
                if (resp.StatusCode == 200)
                {
                    connectParameters.OnSuccess?.Invoke(connectParameters);
                }
                else if (resp.StatusCode == 407)
                {
                    // Proxy authentication required
                    // http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.4.8

                    bool retryNeogitiation = false;
                    string authHeader = DigestStore.FindBest(resp.GetHeaderValues("proxy-authenticate"));
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        var digest = DigestStore.GetOrCreate(this.Address);
                        digest.ParseChallange(authHeader);

                        retryNeogitiation = connectParameters.AuthenticationAttempts < ProxyConnectParameters.MaxAuthenticationAttempts &&
                            this.Credentials != null &&
                            digest.IsUriProtected(this.Address) &&
                            (/*connectParameters.request == null || !connectParameters.request.HasHeader("Proxy-Authorization") ||*/ digest.Stale);
                    }

                    if (!retryNeogitiation)
                        connectParameters.OnError?.Invoke(connectParameters, new Exception($"Can't authenticate Proxy! AuthenticationAttempts: {connectParameters.AuthenticationAttempts} {resp}"), false);
                    else
                    {
                        connectParameters.AuthenticationAttempts++;
                        BeginConnect(connectParameters);
                    }
                }
                else
                {
                    connectParameters.OnError?.Invoke(connectParameters, new Exception($"Proxy returned {resp}"), false);
                }
            }
        }
    }
}

#endif
