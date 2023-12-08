namespace Best.HTTP.Request.Authenticators
{
    /// <summary>
    /// Represents an interface for various authentication implementations used in HTTP requests.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Set required headers or content for the HTTP request. Called right before the request is sent out.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The SetupRequest method will be called every time the request is redirected or retried.
        /// </para>
        /// </remarks>
        /// <param name="request">The HTTP request to which headers or content will be added.</param>
        void SetupRequest(HTTPRequest request);

        /// <summary>
        /// Called when the server is sending a 401 (Unauthorized) response with an WWW-Authenticate header.
        /// The authenticator might find additional knowledge about the authentication requirements (like what auth method it should use).
        /// If the authenticator is confident it can successfully (re)authenticate the request it can return true and the request will be resent to the server.
        /// </summary>
        /// <remarks>
        /// More details can be found here:
        /// <list type="bullet">
        ///     <item><description><see href="https://www.rfc-editor.org/rfc/rfc9110.html#status.401">RFC-9110 - 401 Unauthorized</see></description></item>
        ///     <item><description><see href="https://www.rfc-editor.org/rfc/rfc9110.html#name-www-authenticate">RFC-9110 - WWW-Authenticate header</see></description></item>
        /// </list>
        /// </remarks>
        /// <param name="req">The HTTP request that received the 401 response.</param>
        /// <param name="resp">The HTTP response containing the 401 (Unauthorized) status.</param>
        /// <returns><c>true</c> if the challange is handled by the authenticator and the request can be re-sent with authentication.</returns>
        bool HandleChallange(HTTPRequest req, HTTPResponse resp);
    }
}
