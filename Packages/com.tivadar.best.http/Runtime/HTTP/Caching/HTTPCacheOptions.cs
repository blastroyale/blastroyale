using System;

namespace Best.HTTP.Caching
{
    /// <summary>
    /// Represents the configuration options for the HTTP cache.
    /// </summary>
    public sealed class HTTPCacheOptions
    {
        /// <summary>
        /// Gets or sets the maximum duration for which cached entries will be retained.
        /// </summary>
        public TimeSpan DeleteOlder { get; internal set; } = TimeSpan.MaxValue;

        /// <summary>
        /// Gets or sets the maximum size, in bytes, that the cache can reach.
        /// </summary>
        public ulong MaxCacheSize { get; internal set; } = 512 * 1024 * 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="HTTPCacheOptions"/> class with default settings.
        /// </summary>
        public HTTPCacheOptions()
        {
            // Default constructor with no arguments.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HTTPCacheOptions"/> class with custom settings.
        /// </summary>
        /// <param name="deleteOlder">The maximum age for cached entries to be retained.</param>
        /// <param name="maxCacheSize">The maximum size, in bytes, that the cache can reach.</param>
        public HTTPCacheOptions(TimeSpan deleteOlder, ulong maxCacheSize)
        {
            this.DeleteOlder = deleteOlder;
            this.MaxCacheSize = maxCacheSize;
        }
    }
}
