using System;

namespace Best.SignalR
{
    /// <summary>
    /// Delegate for successful authentication events.
    /// </summary>
    public delegate void OnAuthenticationSuccededDelegate(IAuthenticationProvider provider);

    /// <summary>
    /// Delegate for failed authentication events.
    /// </summary>
    public delegate void OnAuthenticationFailedDelegate(IAuthenticationProvider provider, string reason);

    /// <summary>
    /// Interface for authentication providers.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets a value indicating whether pre-authentication is required before any request made.
        /// </summary>
        /// <remarks>If returns <c>true</c>, the implementation **MUST** implement the <see cref="StartAuthentication"/>, <see cref="Cancel"/> methods and use the <see cref="OnAuthenticationSucceded"/> and <see cref="OnAuthenticationFailed"/> events!</remarks>
        bool IsPreAuthRequired { get; }

        /// <summary>
        /// The concrete implementation must call this event when the pre-authentication is succeded. When <see cref="IsPreAuthRequired"/> is <c>false</c>, no-one will subscribe to this event.
        /// </summary>
        event OnAuthenticationSuccededDelegate OnAuthenticationSucceded;

        /// <summary>
        /// The concrete implementation must call this event when the pre-authentication is failed. When <see cref="IsPreAuthRequired"/> is <c>false</c>, no-one will subscribe to this event.
        /// </summary>
        event OnAuthenticationFailedDelegate OnAuthenticationFailed;

        /// <summary>
        /// This function called once, before the SignalR negotiation begins. If <see cref="IsPreAuthRequired"/> is <c>false</c>, then this step will be skipped.
        /// </summary>
        void StartAuthentication();

        /// <summary>
        /// Prepares a request by adding authentication information, before it's sent.
        /// </summary>
        /// <param name="request">The request to be prepared.</param>
        void PrepareRequest(HTTP.HTTPRequest request);

        /// <summary>
        /// Modifies the provided URI if necessary.
        /// </summary>
        /// <param name="uri">The original URI.</param>
        /// <returns>The modified URI or the original if no modifications are made.</returns>
        Uri PrepareUri(Uri uri);

        /// <summary>
        /// Cancels any ongoing authentication process.
        /// </summary>
        void Cancel();
    }
}
