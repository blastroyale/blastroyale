using System;

namespace Best.HTTP.Request.Settings
{
    public delegate bool OnBeforeRedirectionDelegate(HTTPRequest req, HTTPResponse resp, Uri redirectUri);

    /// <summary>
    /// Represents settings related to handling HTTP request redirection.
    /// </summary>
    public class RedirectSettings
    {
        /// <summary>
        /// Indicates whether the request has been redirected.
        /// A request's IsRedirected might be true while <see cref="RedirectCount"/> is zero if the redirection is made to the local cache.
        /// </summary>
        public bool IsRedirected { get; internal set; }

        /// <summary>
        /// The Uri that the request is redirected to.
        /// </summary>
        public Uri RedirectUri { get; internal set; }

        /// <summary>
        /// How many redirection is supported for this request. The default is 10. Zero or a negative value means no redirections are supported.
        /// </summary>

        /// <summary>
        /// Gets or sets the maximum number of redirections supported for this request. The default is <c>10</c>.
        /// A value of zero or a negative value means no redirections are supported.
        /// </summary>
        public int MaxRedirects { get; set; }

        /// <summary>
        /// Gets the number of times the request has been redirected.
        /// </summary>
        public int RedirectCount { get; internal set; }

        /// <summary>
        /// Occurs before the plugin makes a new request to the new URI during redirection.
        /// The return value of this event handler controls whether the redirection is aborted (<c>false</c>) or allowed (<c>true</c>).
        /// This event is called on a thread other than the main Unity thread.
        /// </summary>
        public event OnBeforeRedirectionDelegate OnBeforeRedirection
        {
            add { onBeforeRedirection += value; }
            remove { onBeforeRedirection -= value; }
        }
        private OnBeforeRedirectionDelegate onBeforeRedirection;

        /// <summary>
        /// Initializes a new instance of the RedirectSettings class with the specified maximum redirections.
        /// </summary>
        /// <param name="maxRedirects">The maximum number of redirections allowed.</param>
        public RedirectSettings(int maxRedirects)
        {
            this.MaxRedirects = maxRedirects;
            this.RedirectCount = 0;
        }

        /// <summary>
        /// Resets <see cref="IsRedirected"/> and <see cref="RedirectCount"/> to their default values.
        /// </summary>
        public void Reset()
        {
            this.IsRedirected = false;
            this.RedirectCount = 0;
        }

        internal bool CallOnBeforeRedirection(HTTPRequest req, HTTPResponse resp, Uri redirectUri)
        {
            if (onBeforeRedirection != null)
                return onBeforeRedirection(req, resp, redirectUri);

            return true;
        }
    }
}
