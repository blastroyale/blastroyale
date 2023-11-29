using System;
using System.Collections.Generic;

using Best.HTTP.Caching;
using Best.HTTP.Cookies;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

using static Best.HTTP.Response.HTTPStatusCodes;

namespace Best.HTTP.Hosts.Connections
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-thomson-hybi-http-timeout-03
    /// Test servers: http://tools.ietf.org/ http://nginx.org/
    /// </summary>
    public sealed class KeepAliveHeader
    {
        /// <summary>
        /// A host sets the value of the "timeout" parameter to the time that the host will allow an idle connection to remain open before it is closed. A connection is idle if no data is sent or received by a host.
        /// </summary>
        public TimeSpan TimeOut { get; private set; }

        /// <summary>
        /// The "max" parameter has been used to indicate the maximum number of requests that would be made on the connection.This parameter is deprecated.Any limit on requests can be enforced by sending "Connection: close" and closing the connection.
        /// </summary>
        public int MaxRequests { get; private set; }

        public void Parse(List<string> headerValues)
        {
            HeaderParser parser = new HeaderParser(headerValues[0]);
            HeaderValue value;

            this.TimeOut = TimeSpan.MaxValue;
            this.MaxRequests = int.MaxValue;

            if (parser.TryGet("timeout", out value) && value.HasValue)
            {
                int intValue = 0;
                if (int.TryParse(value.Value, out intValue) && intValue > 1)
                    this.TimeOut = TimeSpan.FromSeconds(intValue - 1);
            }

            if (parser.TryGet("max", out value) && value.HasValue)
            {
                int intValue = 0;
                if (int.TryParse("max", out intValue))
                    this.MaxRequests = intValue;
            }
        }
    }

    /// <summary>
    /// Static helper class to handle cases where the plugin has to do additional logic based on the received response. These are like connection management, handling redirections, loading from local cache, authentication challanges, etc.
    /// </summary>
    public static class ConnectionHelper
    {
        public static void ResendRequestAndCloseConnection(ConnectionBase connection, HTTPRequest request)
        {
            ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(connection, request));
        }

        public static void EnqueueEvents(ConnectionBase connection, HTTPConnectionStates connectionState, HTTPRequest request, HTTPRequestStates requestState, Exception error)
        {
            // SetState
            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(request, requestState, error));
            ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(connection, connectionState));
        }

        /// <summary>
        /// Called when the whole response received
        /// </summary>
        public static Exception HandleResponse(HTTPRequest request,
            out bool resendRequest,
            out HTTPConnectionStates proposedConnectionState,
            ref KeepAliveHeader keepAlive,
            LoggingContext loggingContext)
        {
            resendRequest = false;
            proposedConnectionState = HTTPConnectionStates.Recycle;

            var resp = request.Response;

            if (resp == null)
                return null;

            // Try to store cookies before we do anything else, as we may remove the response deleting the cookies as well.
            CookieJar.SetFromRequest(resp);

            switch (resp.StatusCode)
            {
                // Not authorized
                // https://www.rfc-editor.org/rfc/rfc9110.html#name-www-authenticate
                case Unauthorized:
                    if (request.Authenticator != null)
                        resendRequest = request.Authenticator.HandleChallange(request, resp);

                    goto default;

#if !UNITY_WEBGL || UNITY_EDITOR
                case ProxyAuthenticationRequired:
                    if (request.ProxySettings == null)
                        goto default;

                    resendRequest = request.ProxySettings.Handle407(request);

                    goto default;
#endif

                // https://www.rfc-editor.org/rfc/rfc9110#name-417-expectation-failed
                case ExpectationFailed: // expectation failed
                    // https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1-11.4
                    // A client that receives a 417 (Expectation Failed) status code in response to a request
                    // containing a 100-continue expectation SHOULD repeat that request without a 100-continue expectation,
                    // since the 417 response merely indicates that the response chain does not support expectations (e.g., it passes through an HTTP/1.0 server).

                    //request.UploadSettings.ResetExpects();
                    //resendRequest = true;
                    break;

                // Redirected
                case MovedPermanently: // http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.2
                case Found: // http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.3
                case SeeOther:
                case TemporaryRedirect: // http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.8
                case PermanentRedirect: // http://tools.ietf.org/html/rfc7238
                    {
                        if (request.RedirectSettings.RedirectCount >= request.RedirectSettings.MaxRedirects)
                            goto default;
                        request.RedirectSettings.RedirectCount++;

                        string location = resp.GetFirstHeaderValue("location");
                        if (!string.IsNullOrEmpty(location))
                        {
                            Uri redirectUri = ConnectionHelper.GetRedirectUri(request, location);

                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Verbose(nameof(ConnectionHelper), $"Redirected to Location: '{location}' redirectUri: '{redirectUri}'", loggingContext);

                            if (redirectUri == request.CurrentUri)
                            {
                                HTTPManager.Logger.Information(nameof(ConnectionHelper), "Redirected to the same location!", loggingContext);
                                goto default;
                            }

                            // Let the user to take some control over the redirection
                            if (!request.RedirectSettings.CallOnBeforeRedirection(request, resp, redirectUri))
                            {
                                HTTPManager.Logger.Information(nameof(ConnectionHelper), "OnBeforeRedirection returned False", loggingContext);
                                goto default;
                            }

                            if (!request.CurrentUri.Host.Equals(redirectUri.Host, StringComparison.OrdinalIgnoreCase))
                            {
#if !UNITY_WEBGL || UNITY_EDITOR
                                //DNSCache.Prefetch(redirectUri.Host);
                                Shared.PlatformSupport.Network.DNS.Cache.DNSCache.Query(new Shared.PlatformSupport.Network.DNS.Cache.DNSQueryParameters(redirectUri) { Context = loggingContext });
#endif

                                // Remove unsafe headers when redirected to an other host.
                                // Just like for https://www.rfc-editor.org/rfc/rfc9110#name-redirection-3xx
                                request.RemoveUnsafeHeaders();
                            }

                            // Set the Referer header to the last Uri.
                            request.SetHeader("Referer", request.CurrentUri.ToString());

                            // Set the new Uri, the CurrentUri will return this while the IsRedirected property is true
                            request.RedirectSettings.RedirectUri = redirectUri;

                            request.RedirectSettings.IsRedirected = true;

                            resendRequest = true;
                        }
                        else
                            return new Exception($"Got redirect status({resp.StatusCode}) without 'location' header!");

                        goto default;
                    }

                case NotModified:
                    if (request.DownloadSettings.DisableCache || HTTPManager.LocalCache == null)
                        break;

                    var hash = HTTPCache.CalculateHash(request.MethodType, request.CurrentUri);
                    if (HTTPManager.LocalCache.RefreshHeaders(hash, resp.Headers, request.Context))
                    {
                        HTTPManager.LocalCache.Redirect(request, hash);
                        resendRequest = true;
                    }
                    break;

                // https://www.rfc-editor.org/rfc/rfc5861.html#section-4
                // In this context, an error is any situation that would result in a
                //    500, 502, 503, or 504 HTTP response status code being returned.
                case var statusCode when statusCode == InternalServerError || (statusCode >= BadGateway && statusCode <= GatewayTimeout):
                    if (HTTPManager.LocalCache != null)
                    {
                        hash = HTTPCache.CalculateHash(request.MethodType, request.CurrentUri);
                        if (HTTPManager.LocalCache.CanServeWithoutValidation(hash, ErrorTypeForValidation.ServerError, request.Context))
                        {
                            HTTPManager.LocalCache.Redirect(request, hash);
                            resendRequest = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            // If we have a response and the server telling us that it closed the connection after the message sent to us, then
            //  we will close the connection too.
            bool closeByServer = resp.HasHeaderWithValue("connection", "close") ||
                                 resp.HasHeaderWithValue("proxy-connection", "close");

            bool tryToKeepAlive = HTTPManager.PerHostSettings.Get(request.CurrentHostKey).HTTP1ConnectionSettings.TryToReuseConnections;
            bool closeByClient = !tryToKeepAlive;

            if (closeByServer || closeByClient)
            {
                proposedConnectionState = HTTPConnectionStates.Closed;
            }
            else if (resp != null)
            {
                var keepAliveheaderValues = resp.GetHeaderValues("keep-alive");
                if (keepAliveheaderValues != null && keepAliveheaderValues.Count > 0)
                {
                    if (keepAlive == null)
                        keepAlive = new KeepAliveHeader();
                    keepAlive.Parse(keepAliveheaderValues);
                }
            }

            // Null out the response here instead of the redirected cases (301, 302, 307, 308)
            //  because response might have a Connection: Close header that we would miss to process.
            // If Connection: Close is present, the server is closing the connection and we would
            // reuse that closed connection.
            if (resendRequest)
            {
                HTTPManager.Logger.Verbose(nameof(ConnectionHelper), "HandleResponse - discarding response", request.Response?.Context ?? loggingContext);

                request.Response?.Dispose();
                // Discard the redirect response, we don't need it any more
                request.Response = null;

                if (proposedConnectionState == HTTPConnectionStates.Closed)
                    proposedConnectionState = HTTPConnectionStates.ClosedResendRequest;
            }

            if (!resendRequest && proposedConnectionState < HTTPConnectionStates.Closed && resp.IsUpgraded)
                proposedConnectionState = HTTPConnectionStates.WaitForProtocolShutdown;
            else
            {
                // Do nothing here, the what we are timing for is decived in the caller, outside code.
                //request.Timing.Finish(TimingEventNames.Response_Received);
            }

            return null;
        }

        public static Uri GetRedirectUri(HTTPRequest request, string location)
        {
            Uri result = null;
            try
            {
                result = new Uri(location);

                if (result.IsFile || result.AbsolutePath == location)
                    result = null;
            }
            catch
            {
                // Sometimes the server sends back only the path and query component of the new uri
                result = null;
            }

            if (result == null)
            {
                var baseURL = request.CurrentUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

                if (!location.StartsWith("/"))
                {
                    var segments = request.CurrentUri.Segments;
                    segments[segments.Length - 1] = location;

                    location = String.Join(string.Empty, segments);
                    if (location.StartsWith("//"))
                        location = location.Substring(1);
                }
                
                bool endsWithSlash = baseURL[baseURL.Length - 1] == '/';
                bool startsWithSlash = location[0] == '/';
                if (endsWithSlash && startsWithSlash)
                    result = new Uri(baseURL + location.Substring(1));
                else if (!endsWithSlash && !startsWithSlash)
                    result = new Uri(baseURL + '/' + location);
                else
                    result = new Uri(baseURL + location);
            }

            return result;
        }

    }
}
