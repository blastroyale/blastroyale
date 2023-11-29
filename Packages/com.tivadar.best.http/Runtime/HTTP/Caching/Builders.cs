using System;

namespace Best.HTTP.Caching
{
    /// <summary>
    /// A builder struct for constructing an instance of the HTTPCache class with optional configuration options and callbacks.
    /// </summary>
    public struct HTTPCacheBuilder
    {
        private HTTPCacheOptions _options;
        private OnBeforeBeginCacheDelegate _callback;

        /// <summary>
        /// Sets the configuration options for the HTTP cache.
        /// </summary>
        /// <param name="options">The <see cref="HTTPCacheOptions"/> containing cache configuration settings.</param>
        /// <returns>The current <see cref="HTTPCacheBuilder"/> instance for method chaining.</returns>
        public HTTPCacheBuilder WithOptions(HTTPCacheOptions options)
        {
            this._options = options;
            return this;
        }

        /// <summary>
        /// Sets the configuration options for the HTTP cache using an <see cref="HTTPCacheOptionsBuilder"/>.
        /// </summary>
        /// <param name="optionsBuilder">An <see cref="HTTPCacheOptionsBuilder"/> for building cache configuration settings.</param>
        /// <returns>The current <see cref="HTTPCacheBuilder"/> instance for method chaining.</returns>
        public HTTPCacheBuilder WithOptions(HTTPCacheOptionsBuilder optionsBuilder)
        {
            this._options = optionsBuilder.Build();
            return this;
        }

        /// <summary>
        /// Sets a callback delegate to be executed before caching of an entity begins.
        /// </summary>
        /// <param name="callback">The delegate to be executed before caching starts.</param>
        /// <returns>The current <see cref="HTTPCacheBuilder"/> instance for method chaining.</returns>
        public HTTPCacheBuilder WithBeforeBeginCacheCallback(OnBeforeBeginCacheDelegate callback)
        {
            this._callback = callback;
            return this;
        }

        /// <summary>
        /// Builds and returns an instance of the <see cref="HTTPCache"/> with the specified configuration options and callback delegate.
        /// </summary>
        /// <returns>An <see cref="HTTPCache"/> instance configured with the specified options and callback.</returns>
        public HTTPCache Build()
            => new HTTPCache(this._options) { OnBeforeBeginCache = this._callback };
    }

    /// <summary>
    /// A builder struct for constructing an instance of <see cref="HTTPCacheOptions"/> with optional configuration settings.
    /// </summary>
    public struct HTTPCacheOptionsBuilder
    {
        private HTTPCacheOptions _options;

        /// <summary>
        /// Sets the maximum cache size for the HTTP cache.
        /// </summary>
        /// <param name="maxCacheSize">The maximum size, in bytes, that the cache can reach.</param>
        /// <returns>The current <see cref="HTTPCacheOptionsBuilder"/> instance for method chaining.</returns>
        public HTTPCacheOptionsBuilder WithMaxCacheSize(ulong maxCacheSize)
        {
            this._options = this._options ?? new HTTPCacheOptions();
            this._options.MaxCacheSize = maxCacheSize;

            return this;
        }

        /// <summary>
        /// Sets the maximum duration for which cached entries will be retained.
        /// By default all entities (even stalled ones) are kept cached until they are evicted to make room for new, fresh ones.
        /// </summary>
        /// <param name="olderThan">The maximum age for cached entries to be retained.</param>
        /// <returns>The current <see cref="HTTPCacheOptionsBuilder"/> instance for method chaining.</returns>
        public HTTPCacheOptionsBuilder WithDeleteOlderThen(TimeSpan olderThan)
        {
            this._options = this._options ?? new HTTPCacheOptions();
            this._options.DeleteOlder = olderThan;

            return this;
        }

        /// <summary>
        /// Builds and returns an instance of <see cref="HTTPCacheOptions"/> with the specified configuration settings.
        /// </summary>
        /// <returns>An <see cref="HTTPCacheOptions"/> instance configured with the specified settings.</returns>
        public HTTPCacheOptions Build()
            => this._options ?? new HTTPCacheOptions();
    }
}
