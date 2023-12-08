using System;

using Best.HTTP.Shared;

namespace Best.HTTP.Request.Settings
{
    /// <summary>
    /// Represents settings related to connection-timeouts and processing duration.
    /// </summary>
    public class TimeoutSettings
    {
        /// <summary>
        /// Gets the timestamp when the request was queued for processing.
        /// </summary>
        public DateTime QueuedAt { get; internal set; }

        /// <summary>
        /// Gets the timestamp when the processing of the request started by a connection.
        /// </summary>
        public DateTime ProcessingStarted { get; internal set; }

        /// <summary>
        /// Gets or sets the maximum time to wait for establishing the connection to the target server.
        /// If set to <c>TimeSpan.Zero</c> or lower, no connect timeout logic is executed. Default value is 20 seconds.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get => this._connectTimeout ?? HTTPManager.PerHostSettings.Get(this._request.CurrentHostKey.Host).RequestSettings.ConnectTimeout;
            set => this._connectTimeout = value;
        }
        private TimeSpan? _connectTimeout;

        /// <summary>
        /// Gets or sets the maximum time to wait for the request to finish after the connection is established.
        /// </summary>
        public TimeSpan Timeout
        {
            get => this._timeout ?? HTTPManager.PerHostSettings.Get(this._request.CurrentHostKey.Host).RequestSettings.RequestTimeout;
            set => this._timeout = value;
        }
        private TimeSpan? _timeout;

        /// <summary>
        /// Returns <c>true</c> if the request has been stuck in the connection phase for too long.
        /// </summary>
        /// <param name="now">The current timestamp.</param>
        /// <returns><c>true</c> if the connection has timed out; otherwise, <c>false</c>.</returns>
        public bool IsConnectTimedOut(DateTime now) => this.QueuedAt != DateTime.MinValue && now - this.QueuedAt > this.ConnectTimeout;

        /// <summary>
        /// Returns <c>true</c> if the time has passed the specified Timeout setting since processing started or if the connection has timed out.
        /// </summary>
        /// <param name="now">The current timestamp.</param>
        /// <returns><c>true</c> if the request has timed out; otherwise, <c>false</c>.</returns>
        public bool IsTimedOut(DateTime now)
        {
            bool result = (this.ProcessingStarted != DateTime.MinValue && now - this.ProcessingStarted > this.Timeout) || this.IsConnectTimedOut(now); ;
            return result;
        }

        private HTTPRequest _request;

        /// <summary>
        /// Initializes a new instance of the TimeoutSettings class for a specific <see cref="HTTPRequest"/>.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> associated with these timeout settings.</param>
        public TimeoutSettings(HTTPRequest request)
            => this._request = request;
    }
}
