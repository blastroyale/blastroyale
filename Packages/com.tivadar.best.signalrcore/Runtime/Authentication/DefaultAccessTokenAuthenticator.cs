using System;

namespace Best.SignalR.Authentication
{
    /// <summary>
    /// Represents the default access token authenticator that uses the Bearer token scheme for HTTP and WebSockets.
    /// </summary>
    public sealed class DefaultAccessTokenAuthenticator : IAuthenticationProvider
    {
        /// <summary>
        /// Indicates that no pre-authentication step is required for this type of authentication.
        /// </summary>
        public bool IsPreAuthRequired { get { return false; } }

#pragma warning disable 0067
        /// <summary>
        /// This event is not used because <see cref="IsPreAuthRequired"/> is <c>false</c>.
        /// </summary>
        public event OnAuthenticationSuccededDelegate OnAuthenticationSucceded;

        /// <summary>
        /// This event is not used because <see cref="IsPreAuthRequired"/> is <c>false</c>.
        /// </summary>
        public event OnAuthenticationFailedDelegate OnAuthenticationFailed;

#pragma warning restore 0067

        private HubConnection _connection;

        /// <summary>
        /// Initializes a new instance of the DefaultAccessTokenAuthenticator class.
        /// </summary>
        /// <param name="connection">The <see cref="HubConnection"/> for this authenticator.</param>
        public DefaultAccessTokenAuthenticator(HubConnection connection) => this._connection = connection;

        /// <summary>
        /// Not used as IsPreAuthRequired is false
        /// </summary>
        public void StartAuthentication() { }

        /// <summary>
        /// Prepares the HTTP request by adding appropriate authentication headers or query parameters based on the request type.
        /// </summary>
        /// <param name="request">The HTTP request to prepare.</param>
        public void PrepareRequest(Best.HTTP.HTTPRequest request)
        {
            if (this._connection.NegotiationResult == null)
                return;

            // Add Authorization header to http requests, add access_token param to the uri otherwise
            if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(request.CurrentUri) == Best.HTTP.Hosts.Connections.SupportedProtocols.HTTP)
                request.SetHeader("Authorization", "Bearer " + this._connection.NegotiationResult.AccessToken);
            else
#if !BESTHTTP_DISABLE_WEBSOCKET
                if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(request.Uri) != Best.HTTP.Hosts.Connections.SupportedProtocols.WebSocket)
                    request.Uri = PrepareUriImpl(request.Uri);
#else
                ;
#endif
        }

        /// <summary>
        /// Prepares the URI by appending the access token if necessary.
        /// </summary>
        /// <param name="uri">The original URI.</param>
        /// <returns>The prepared URI with the access token appended if necessary.</returns>
        public Uri PrepareUri(Uri uri)
        {
            if (this._connection.NegotiationResult == null)
                return uri;

            if (uri.Query.StartsWith("??"))
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Query = builder.Query.Substring(2);

                return builder.Uri;
            }

#if !BESTHTTP_DISABLE_WEBSOCKET
            if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(uri) == Best.HTTP.Hosts.Connections.SupportedProtocols.WebSocket)
                uri = PrepareUriImpl(uri);
#endif

            return uri;

        }

        /// <summary>
        /// Internal method to prepare the URI by appending the access token.
        /// </summary>
        /// <param name="uri">The original URI.</param>
        /// <returns>The prepared URI with the access token appended.</returns>
        private Uri PrepareUriImpl(Uri uri)
        {
            if (this._connection.NegotiationResult != null && !string.IsNullOrEmpty(this._connection.NegotiationResult.AccessToken))
            {
                string query = string.IsNullOrEmpty(uri.Query) ? "" : uri.Query + "&";
                UriBuilder uriBuilder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath, query + "access_token=" + this._connection.NegotiationResult.AccessToken);
                return uriBuilder.Uri;
            }

            return uri;
        }

        /// <summary>
        /// Cancels any ongoing authentication operations.
        /// </summary>
        public void Cancel()
        { }
    }
}
