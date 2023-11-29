using System;
using System.Text;

using Best.HTTP.Request.Authentication;
using Best.HTTP.Shared;

namespace Best.HTTP.Request.Authenticators
{
    /// <summary>
    /// An <see cref="IAuthenticator"/> implementation for HTTP Basic or Digest authentication.
    /// </summary>
    public class CredentialAuthenticator : IAuthenticator
    {
        /// <summary>
        /// Gets or sets the <see cref="Authentication.Credentials"/> associated with this authenticator.
        /// </summary>
        public Credentials Credentials { get; set; }

        /// <summary>
        /// Initializes a new instance of the CrendetialAuthenticator class with the specified <see cref="Authentication.Credentials"/>.
        /// </summary>
        /// <param name="credentials">The <see cref="Authentication.Credentials"/> to use for authentication.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="credentials"/> is null.</exception>
        public CredentialAuthenticator(Credentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            this.Credentials = credentials;
        }

        /// <summary>
        /// Sets up the required headers for the HTTP request based on the provided credentials.
        /// </summary>
        /// <param name="request">The HTTP request for which headers should be added.</param>
        public void SetupRequest(HTTPRequest request)
        {
            HTTPManager.Logger.Information(nameof(CredentialAuthenticator), $"SetupRequest({request}, {Credentials?.Type})", request.Context);

            if (Credentials == null)
                return;

            switch (Credentials.Type)
            {
                case AuthenticationTypes.Basic:
                    // With Basic authentication we don't want to wait for a challenge, we will send the hash with the first request
                    request.SetHeader("Authorization", string.Concat("Basic ", Convert.ToBase64String(Encoding.UTF8.GetBytes(Credentials.UserName + ":" + Credentials.Password))));
                    break;

                case AuthenticationTypes.Unknown:
                case AuthenticationTypes.Digest:
                    var digest = DigestStore.Get(request.CurrentUri);
                    if (digest != null)
                    {
                        string authentication = digest.GenerateResponseHeader(Credentials, false, request.MethodType, request.CurrentUri);
                        if (!string.IsNullOrEmpty(authentication))
                            request.SetHeader("Authorization", authentication);
                    }

                    break;
            }

        }

        /// <summary>
        /// Handles the server response with a 401 (Unauthorized) status code and a WWW-Authenticate header.
        /// The authenticator might determine the authentication method to use and initiate authentication if needed.
        /// </summary>
        /// <param name="req">The HTTP request that received the 401 response.</param>
        /// <param name="resp">The HTTP response containing the 401 (Unauthorized) status.</param>
        /// <returns><c>true</c> if the challenge is handled by the authenticator and the request can be resent with authentication; otherwise, <c>false</c>.</returns>
        public bool HandleChallange(HTTPRequest req, HTTPResponse resp)
        {
            var www_authenticate = resp.GetHeaderValues("www-authenticate");

            HTTPManager.Logger.Information(nameof(CredentialAuthenticator), $"HandleChallange({req}, {resp}, \"{www_authenticate}\")", req.Context);

            string authHeader = DigestStore.FindBest(www_authenticate);
            if (!string.IsNullOrEmpty(authHeader))
            {
                var digest = DigestStore.GetOrCreate(req.CurrentUri);
                digest.ParseChallange(authHeader);

                if (this.Credentials != null && digest.IsUriProtected(req.CurrentUri) && (!req.HasHeader("Authorization") || digest.Stale))
                    return true;
            }

            return false;
        }
    }
}
