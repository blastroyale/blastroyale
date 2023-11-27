namespace Best.HTTP.Request.Settings
{
    /// <summary>
    /// Represents settings related to request retry behavior.
    /// </summary>
    public class RetrySettings
    {
        /// <summary>
        /// Gets the number of times that the plugin has retried the request.
        /// </summary>
        public int Retries { get; internal set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts allowed. To disable retries, set this value to <c>0</c>.
        /// The default value is <c>1</c> for GET requests, otherwise <c>0</c>.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Initializes a new instance of the RetrySettings class with the specified maximum retry attempts.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts allowed.</param>
        public RetrySettings(int maxRetries)
            => this.MaxRetries = maxRetries;
    }
}
