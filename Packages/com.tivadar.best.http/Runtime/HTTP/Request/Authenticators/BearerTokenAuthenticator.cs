namespace Best.HTTP.Request.Authenticators
{
    /// <summary>
    /// An <see cref="IAuthenticator"/> implementation for Bearer Token authentication.
    /// </summary>
    /// <remarks>
    /// Bearer Token authentication is a method used to access protected resources on a server.
    /// It involves including a bearer token in the Authorization header of an HTTP request to prove the identity of the requester.
    /// </remarks>
    public class BearerTokenAuthenticator : IAuthenticator
    {
        /// <summary>
        /// Initializes a new instance of the BearerTokenAuthenticator class with the specified Bearer Token.
        /// </summary>
        /// <param name="token">The Bearer Token to use for authentication.</param>
        public string Token { get; private set; }

        /// <summary>
        /// Sets up the required Authorization header with the Bearer Token for the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request for which the Authorization header should be added.</param>
        /// <remarks>
        /// When sending an HTTP request to a server that requires Bearer Token authentication,
        /// this method sets the Authorization header with the Bearer Token to prove the identity of the requester.
        /// This allows the requester to access protected resources on the server.
        /// </remarks>
        public BearerTokenAuthenticator(string token) => this.Token = token;

        public void SetupRequest(HTTPRequest request)
        {
            if (!string.IsNullOrEmpty(this.Token))
                request.SetHeader("Authorization", $"Bearer {this.Token}");
        }

        /// <summary>
        /// Handles the server response with a 401 (Unauthorized) status code and a WWW-Authenticate header.
        /// This authenticator does not handle challenges and always returns <c>false</c>.
        /// </summary>
        /// <param name="req">The HTTP request that received the 401 response.</param>
        /// <param name="resp">The HTTP response containing the 401 (Unauthorized) status.</param>
        /// <returns><c>false</c>, as this authenticator does not handle challenges.</returns>
        /// <remarks>
        /// Bearer Token authentication typically does not require handling challenges,
        /// as the Bearer Token is included directly in the Authorization header of the request.
        /// This method always returns <c>false</c>, as no additional challenge processing is needed.
        /// </remarks>
        public bool HandleChallange(HTTPRequest req, HTTPResponse resp) => false;
    }
}
