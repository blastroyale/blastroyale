using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Threading;

using UnityEngine;

using static System.Math;
using static Best.HTTP.Hosts.Connections.HTTP1.Constants;
using static Best.HTTP.Response.HTTPStatusCodes;

namespace Best.HTTP.Caching
{
    internal sealed class HTTPCacheAcquireLockException : Exception
    {
        public HTTPCacheAcquireLockException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Types of errors that can occur during cache validation.
    /// </summary>
    public enum ErrorTypeForValidation
    {
        /// <summary>
        /// Indicates that no error has occurred during validation.
        /// </summary>
        None,

        /// <summary>
        /// Indicates a server error has occurred during validation.
        /// </summary>
        ServerError,

        /// <summary>
        /// Indicates a connection error has occurred during validation.
        /// </summary>
        ConnectionError
    }

    /// <summary>
    /// Represents a delegate that can be used to perform actions before caching of an entity begins.
    /// </summary>
    /// <param name="method">The HTTP method used in the request.</param>
    /// <param name="uri">The URI of the HTTP request.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="headers">The HTTP response headers.</param>
    /// <param name="context">An optional logging context for debugging.</param>
    public delegate void OnBeforeBeginCacheDelegate(HTTPMethods method, Uri uri, int statusCode, Dictionary<string, List<string>> headers, LoggingContext context = null);

    /// <summary>
    /// Represents a delegate that can be used to handle cache size change events.
    /// </summary>
    public delegate void OnCacheSizeChangedDelegate();

    /// <summary>
    /// Manages caching of HTTP responses and associated metadata.
    /// </summary>
    /// <remarks>
    /// <para>The `HTTPCache` class provides a powerful caching mechanism for HTTP responses in Unity applications. 
    /// It allows you to store and retrieve HTTP responses efficiently, reducing network requests and improving 
    /// the performance of your application. By utilizing HTTP caching, you can enhance user experience, reduce 
    /// bandwidth usage, and optimize loading times.
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    ///     <item><term>Optimal User Experience</term><description>Users experience faster load times and smoother interactions, enhancing user satisfaction.</description></item>
    ///     <item><term>Efficient Caching</term><description>It enables efficient caching of HTTP responses, reducing the need to fetch data from the network repeatedly.</description></item>
    ///     <item><term>Improved Performance</term><description>Caching helps improve the performance of your Unity application by reducing latency and decreasing loading times.</description></item>
    ///     <item><term>Bandwidth Optimization</term><description>By storing and reusing cached responses, you can minimize bandwidth usage, making your application more data-friendly.</description></item>
    ///     <item><term>Offline Access</term><description>Cached responses allow your application to function even when the device is offline or has limited connectivity.</description></item>
    ///     <item><term>Reduced Server Load</term><description>Fewer network requests mean less load on your server infrastructure, leading to cost savings and improved server performance.</description></item>
    ///     <item><term>Manual Cache Control</term><description>You can also manually control caching by adding, removing, or updating cached responses.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstruction]
    public class HTTPCache : IDisposable, IHeartbeat
    {
        /// <summary>
        /// Constants defining folder and file names used in the HTTP cache storage.
        /// </summary>

        private const string RootFolderName = "LocalCache";
        private const string DatabaseFolderName = "Database";
        private const string ContentFolderName = "Content";
        private const string HeaderFileName = "headers.txt";
        private const string ContentFileName = "content.bin";

        /// <summary>
        /// This is the reversed domain the plugin uses for file paths when it have to load content from the local cache.
        /// </summary>
        public const string CacheHostName = "com.Tivadar.Best.HTTP.Local.Cache";

        /// <summary>
        /// Event that is triggered when the size of the cache changes.
        /// </summary>
        public OnCacheSizeChangedDelegate OnCacheSizeChanged;

        /// <summary>
        /// Gets the options that define the behavior of the HTTP cache.
        /// </summary>
        public HTTPCacheOptions Options { get; private set; }

        /// <summary>
        /// Gets the current size of the HTTP cache in bytes.
        /// </summary>
        public long CacheSize { get => this._cacheSize; }
        private long _cacheSize;

        /// <summary>
        /// Called before the plugin calls <see cref="BeginCache(HTTPMethods, Uri, int, Dictionary{string, List{string}}, LoggingContext)"/> to decide whether the content will be cached or not.
        /// </summary>
        public OnBeforeBeginCacheDelegate OnBeforeBeginCache;

        private int _subscribed;

        private bool _isSupported;
        private HTTPCacheDatabase _database;
        private string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of the HTTPCache class with the specified cache options.
        /// </summary>
        /// <param name="options">The HTTP cache options specifying cache size and deletion policy.</param>
        public HTTPCache(HTTPCacheOptions options)
        {
            this.Options = options ?? new HTTPCacheOptions();

            try
            {
                _baseDirectory = Path.Combine(HTTPManager.GetRootSaveFolder(), RootFolderName);

#if UNITY_WEBGL && !UNITY_EDITOR
                this._isSupported = false;
                this._database = null;
#else            

                var dbBaseDir = Path.Combine(_baseDirectory, DatabaseFolderName);

                if (!HTTPManager.IOService.DirectoryExists(dbBaseDir))
                    HTTPManager.IOService.DirectoryCreate(dbBaseDir);

                _database = new HTTPCacheDatabase(dbBaseDir);

                var cacheDir = Path.Combine(_baseDirectory, ContentFolderName);
                if (!HTTPManager.IOService.DirectoryExists(cacheDir))
                    HTTPManager.IOService.DirectoryCreate(cacheDir);

                _isSupported = true;
#endif
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTPCache), "ctr", ex);
                _isSupported = false;
                _database?.Dispose();
            }
        }

        /// <summary>
        /// Calculates a unique hash identifier based on the HTTP method and URI.
        /// </summary>
        /// <param name="method">The HTTP method used in the request.</param>
        /// <param name="uri">The URI of the HTTP request.</param>
        /// <returns>A unique hash identifier for the combination of method and URI.</returns>
        public static Hash128 CalculateHash(HTTPMethods method, Uri uri)
        {
            Hash128 hash = new Hash128();

            hash.Append((byte)method);
            hash.Append(uri.ToString());

            return hash;
        }

        /// <summary>
        /// Generates the directory path based on the given hash where cached content is stored.
        /// </summary>
        /// <param name="hash">A unique hash identifier for the cached content, returned by <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/>.</param>
        /// <returns>The directory path for the cached content associated with the given hash.</returns>
        public string GetHashDirectory(Hash128 hash)
            => Path.Combine(_baseDirectory, ContentFolderName, hash.ToString());

        /// <summary>
        /// Generates the file path for the header cache associated with the given hash.
        /// </summary>
        /// <param name="hash">A unique hash identifier for the cached content, returned by <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/>.</param>
        /// <returns>The file path for the header cache associated with the given hash.</returns>
        public string GetHeaderPathFromHash(Hash128 hash)
            => Path.Combine(_baseDirectory, ContentFolderName, hash.ToString(), "headers.cache");

        /// <summary>
        /// Generates the file path for the content cache associated with the given hash.
        /// </summary>
        /// <param name="hash">A unique hash identifier for the cached content, returned by <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/>.</param>
        /// <returns>The file path for the content cache associated with the given hash.</returns>
        public string GetContentPathFromHash(Hash128 hash)
            => Path.Combine(_baseDirectory, ContentFolderName, hash.ToString(), "content.cache");

        /// <summary>
        /// Checks whether cache files (header and content) associated with the given hash exist.
        /// </summary>
        /// <param name="hash">A unique hash identifier for the cached content.</param>
        /// <returns><c>true</c> if both header and content cache files exist, otherwise <c>false</c>.</returns>
        public bool AreCacheFilesExists(Hash128 hash)
            => HTTPManager.IOService.FileExists(GetHeaderPathFromHash(hash)) &&
               HTTPManager.IOService.FileExists(GetContentPathFromHash(hash));

        /// <summary>
        /// Sets up validation headers on an HTTP request if a locally cached response exists.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> to which validation headers will be added.</param>
        public void SetupValidationHeaders(HTTPRequest request)
        {
            var hash = CalculateHash(request.MethodType, request.CurrentUri);

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(SetupValidationHeaders)}({request}, {hash})", request.Context);

            request.RemoveHeader("If-None-Match");
            request.RemoveHeader("If-Modified-Since");

            if (!_isSupported)
                return;

            if (!hash.isValid)
                return;

            // find&load content for the hash
            var content = _database.FindByHashAndUpdateRequestTime(hash, request.Context);

            if (content == null)
                return;

            if (!AreCacheFilesExists(hash))
            {
                Delete(hash, request.Context);
                return;
            }

            if (!string.IsNullOrEmpty(content.ETag))
                request.SetHeader("If-None-Match", content.ETag);

            if (content.LastModified != DateTime.MinValue)
                request.SetHeader("If-Modified-Since", content.LastModified.ToString("R"));
        }

        /// <summary>
        /// If necessary tries to make enough space in the cache by calling Maintain.
        /// </summary>
        internal bool IsThereEnoughSpaceAfterMaintain(ulong spaceNeeded, LoggingContext context)
        {
            // Run maintenance and see whether we have enough space for the new content.
            if ((ulong)(CacheSize + (long)spaceNeeded) > Options.MaxCacheSize)
                Maintain(contentLength: spaceNeeded, deleteLockedEntries: false, context: context);

            return (ulong)(CacheSize + (long)spaceNeeded) <= Options.MaxCacheSize;
        }

        /// <summary>
        /// Initiates the caching process for an HTTP response, creating an <see cref="HTTPCacheContentWriter"/> if caching is enabled and all predconditions are met.
        /// </summary>
        /// <param name="method">The <see cref="HTTPRequest"/> method used to fetch the response.</param>
        /// <param name="uri">The URI for the response.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="headers">The HTTP headers of the response.</param>
        /// <param name="context">An optional logging context for debugging.</param>
        /// <returns>An <see cref="HTTPCacheContentWriter"/> instance for writing the response content to the cache, or null if caching is not enabled or not possible.</returns>
        public HTTPCacheContentWriter BeginCache(HTTPMethods method, Uri uri, int statusCode, Dictionary<string, List<string>> headers, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(BeginCache)}({method}, {uri}, {statusCode}, {headers?.Count})", context);

            if (!_isSupported)
                return null;

            // Check if the response is cacheable based on method, URI, and status code.
            // The original IsCachable got split into two:
            //  - first check method, uri and status code before calling OnBeforeBeginCache
            if (!IsCacheble(method, uri, statusCode))
                return null;

            if (headers == null)
                return null;

            // Log caching headers for debugging purposes.
            LogCachingHeaders(headers, context);

            var onBeforeBeginCache = OnBeforeBeginCache;
            if (onBeforeBeginCache != null)
            {
                try
                {
                    HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(BeginCache)} - Calling {nameof(OnBeforeBeginCache)}", context);

                    // Invoke the OnBeforeBeginCache callback if provided.
                    onBeforeBeginCache?.Invoke(method, uri, statusCode, headers, context);

                    // Log caching headers after the callback.
                    LogCachingHeaders(headers, context);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(HTTPCache), nameof(OnBeforeBeginCache), ex, context);
                }
            }

            // Check if there is enough space in the cache for the response content.
            var contentLengthStr = headers.GetFirstHeaderValue("content-length");
            if (ulong.TryParse(contentLengthStr, out var contentLength))
            {
                if (!IsThereEnoughSpaceAfterMaintain(contentLength, context))
                {
                    HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(BeginCache)} - Not enough space({contentLength:N0}) in cache({CacheSize:N0}), even after Maintain!", context);
                    return null;
                }
            }

            // Check if the response headers indicate that the response is cacheable.
            //  (second half of the original IsCachable)
            //  - then existence of the required caching headers after OnBeforeBeginCache
            if (!IsCacheble(headers))
                return null;

            // Check if the calculated hash is valid.
            var hash = CalculateHash(method, uri);
            if (!hash.isValid)
                return null;

            // Try to get a lock on the cache entity. Prevents other requests from updating or loading from it.
            if (!_database.TryAcquireWriteLock(hash, headers, context))
            {
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(BeginCache)} - Couldn't acquire write lock!", context);
                return null;
            }

            // Add or replace the "Date" header in the response if it is missing or invalid.
            // https://www.rfc-editor.org/rfc/rfc9110#section-6.6.1-8
            // A recipient with a clock that receives a response message without a Date header field
            // MUST record the time it was received and append a corresponding Date header field
            // to the message's header section if it is cached or forwarded downstream.

            var date = headers.GetFirstHeaderValue("date");
            if (string.IsNullOrEmpty(date) || !DateTime.TryParse(date, out var _))
            {
                // A recipient with a clock that receives a response with an invalid Date header field value
                // MAY replace that value with the time that response was received.
                headers.RemoveHeader("date");
                headers.AddHeader("date", DateTime.Now.ToString("R"));
            }

            Stream contentStream = null;
            try
            {
                // Create the cache directory if it does not exist.
                var hashDir = GetHashDirectory(hash);
                if (!HTTPManager.IOService.DirectoryExists(hashDir))
                    HTTPManager.IOService.DirectoryCreate(hashDir);

                // Create and write the header cache file.
                using (var headStream = HTTPManager.IOService.CreateFileStream(GetHeaderPathFromHash(hash), FileStreamModes.Create))
                    WriteHeaders(headStream, headers);

                // Create/open the content cache file.
                contentStream = HTTPManager.IOService.CreateFileStream(GetContentPathFromHash(hash), FileStreamModes.Create);
            }
            catch (Exception ex)
            {
                // Handle exceptions that may occur during cache file creation

                HTTPManager.Logger.Exception(nameof(HTTPCache), nameof(BeginCache), ex, context);

                contentStream?.Dispose();
                contentStream = null;

                // Delete the cache entry if an exception occurs.
                Delete(hash, context);
            }

            // Return an HTTPCacheContentWriter for writing response content to the cache.
            return new HTTPCacheContentWriter(this, hash, contentStream, contentLength, context);
        }

        /// <summary>
        /// Finalizes the caching process and takes appropriate actions based on the completion status.
        /// </summary>
        /// <param name="cacheResult">The <see cref="HTTPCacheContentWriter"/> instance representing the caching operation.</param>
        /// <param name="completedWithoutIssue">A boolean indicating whether the caching process completed without issues.</param>
        /// <param name="context">An optional logging context for debugging.</param>
        public void EndCache(HTTPCacheContentWriter cacheResult, bool completedWithoutIssue, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(EndCache)}({cacheResult}, {completedWithoutIssue})", context);

            if (cacheResult == null || !cacheResult.Hash.isValid || !_isSupported)
                return;

            var hash = cacheResult.Hash;

            cacheResult.Close();

            if (completedWithoutIssue)
            {
                _database.ReleaseWriteLock(hash, cacheResult.ProcessedLength, cacheResult.Context);
                IncrementCacheSize(cacheResult.ProcessedLength);
            }
            else
            {
                Delete(hash, cacheResult.Context);
            }
        }

        /// <summary>
        /// Initiates the process of reading cached content associated with a given hash. Call BeginReadContent to acquire a Stream object that points to the cached resource.
        /// </summary>
        /// <param name="hash">A hash from <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/> identifying the resource.</param>
        /// <param name="context">An optional <see cref="LoggingContext"/></param>
        /// <returns>A stream for reading the cached content, or null if the content could not be read (the resource isn't cached or currently downloading).</returns>
        public Stream BeginReadContent(Hash128 hash, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(BeginReadContent)}({hash})", context);

            if (!_isSupported)
                return null;

            if (!_database.TryAcquireReadLock(hash, context))
                return null;

            _database.UpdateLastAccessTime(hash, context);

            var contentPath = GetContentPathFromHash(hash);

            return HTTPManager.IOService.CreateFileStream(contentPath, FileStreamModes.OpenRead);
        }

        /// <summary>
        /// Finalizes the process of reading cached content associated with a given hash.
        /// </summary>
        /// <param name="hash">The unique hash identifier for the cached content.</param>
        /// <param name="context">An optional logging context for debugging.</param>
        public void EndReadContent(Hash128 hash, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(EndReadContent)}({hash})", context);

            if (!_isSupported)
                return;

            _database.ReleaseReadLock(hash, context);
        }

        /// <summary>
        /// Deletes a cached entry identified by the given hash, including its associated header and content files.
        /// </summary>
        /// <param name="hash">The unique hash identifier for the cached entry to be deleted.</param>
        /// <param name="context">An optional logging context for debugging.</param>
        public void Delete(Hash128 hash, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(Delete)}({hash})", context);

            if (!_isSupported)
                return;

            // Calling this function more than once with the same hash should be fine, the DB is locked and
            //  only one will find the metadata.

            try
            {
                _database.EnterWriteLock(context);

                try
                {
                    var headerPath = GetHeaderPathFromHash(hash);
                    if (HTTPManager.IOService.FileExists(headerPath))
                        HTTPManager.IOService.FileDelete(headerPath);

                    var contentPath = GetContentPathFromHash(hash);
                    if (HTTPManager.IOService.FileExists(contentPath))
                        HTTPManager.IOService.FileDelete(contentPath);

                    var hashDirectory = GetHashDirectory(hash);
                    if (HTTPManager.IOService.DirectoryExists(hashDirectory))
                        HTTPManager.IOService.DirectoryDelete(hashDirectory);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(HTTPCache), $"{nameof(Delete)}({hash})", ex, context);
                }

                DecrementCacheSize(_database.Delete(hash, context));
            }
            finally
            {
                _database.ExitWriteLock(context);
            }
        }

        /// <summary>
        /// Refreshes the headers of a cached HTTP response with new headers.
        /// </summary>
        /// <param name="hash">A unique hash identifier for the cached response from a <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/> call.</param>
        /// <param name="newHeaders">A dictionary of new headers to replace or merge with existing headers.</param>
        /// <param name="context">Used by the plugin to add an addition logging context for debugging. It can be <c>null</c>.</param>
        /// <returns><c>true</c> if the headers were successfully refreshed; otherwise, <c>false</c>.</returns>
        public bool RefreshHeaders(Hash128 hash, Dictionary<string, List<string>> newHeaders, LoggingContext context)
        {
            // To Refresh stored cache related values from the headers described here:
            // 1.) https://www.rfc-editor.org/rfc/rfc9111.html#name-freshening-stored-responses
            // 2.) https://www.rfc-editor.org/rfc/rfc9111.html#name-updating-stored-header-fiel

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(RefreshHeaders)}({hash}, {newHeaders?.Count})", context);

            if (!_isSupported)
                return false;

            // Log the new headers for debugging purposes.
            LogCachingHeaders(newHeaders, context);

            // Update the metadata with the new headers.
            if (_database.Update(hash, newHeaders, context))
            {
                // https://www.rfc-editor.org/rfc/rfc9111.html#name-updating-stored-header-fiel
                //  Load stored header, merge them with the received ones and store them.
                try
                {
                    using (var headerStream = HTTPManager.IOService.CreateFileStream(GetHeaderPathFromHash(hash), FileStreamModes.OpenReadWrite))
                    {
                        // Load existing headers.
                        var oldHeaders = LoadHeaders(headerStream);

                        foreach (var kvp in newHeaders)
                        {
                            if (oldHeaders.TryGetValue(kvp.Key, out var value))
                            {
                                // Replace existing header values with new values.
                                value.Clear();
                                value.AddRange(kvp.Value);
                            }
                            else
                            {
                                // Add new headers if they don't already exist.
                                oldHeaders.Add(kvp.Key, value);
                            }
                        }

                        // Seek to the beginning of the header file and write the updated headers.
                        headerStream.Seek(0, SeekOrigin.Begin);
                        headerStream.SetLength(0);
                        WriteHeaders(headerStream, oldHeaders);
                    }

                    // Everything went as expected, return true
                    return true;
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Warning(nameof(HTTPCache), $"{nameof(RefreshHeaders)} - Couldn't merge/store headers. Exception: {ex}", context);

                    // Delete the cached response associated with the hash.
                    Delete(hash, context);
                }
            }

            return false;
        }

        private bool IsCacheble(Dictionary<string, List<string>> headers)
        {
            if (!_isSupported)
                return false;

            // Responses with byte ranges not supported.
            var byteRanges = headers.GetHeaderValues("content-range");
            if (byteRanges != null)
                return false;

            //http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.9.2
            bool hasValidMaxAge = false;
            var cacheControls = headers.GetHeaderValues("cache-control");

            if (cacheControls != null)
            {
                // A local function that checks the header value for any indication that prohibits caching.
                // So, it must return TRUE, if it's NOT cachable.
                bool CheckHeader(string headerValue)
                {
                    HeaderParser parser = new HeaderParser(headerValue);
                    if (parser.Values != null && parser.Values.Count > 0)
                    {
                        for (int i = 0; i < parser.Values.Count; ++i)
                        {
                            var value = parser.Values[i];

                            // https://csswizardry.com/2019/03/cache-control-for-civilians/#no-store
                            if (value.Key == "no-store")
                                return true;

                            if (value.Key == "max-age" && value.HasValue)
                            {
                                double maxAge;
                                if (double.TryParse(value.Value, out maxAge))
                                {
                                    // A negative max-age value is a no cache
                                    if (maxAge <= 0)
                                        return true;
                                    hasValidMaxAge = true;
                                }
                            }
                        }
                    }

                    return false;
                }

                if (cacheControls.Exists(CheckHeader))
                    return false;
            }

            // It has an ETag header
            var etag = headers.GetFirstHeaderValue("etag");
            if (!string.IsNullOrEmpty(etag))
                return true;

            // It has an Expires header, and it's in the future
            var expires = headers.GetFirstHeaderValue("expires").ToDateTime(DateTime.FromBinary(0));
            if (expires > DateTime.Now)
                return true;

            // It has a Last-Modified header
            if (headers.GetFirstHeaderValue("last-modified") != null)
                return true;

            return hasValidMaxAge;
        }

        private bool IsCacheble(HTTPMethods method, Uri uri, int statusCode)
        {
            if (!_isSupported)
                return false;

            if (!uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;

            if (method != HTTPMethods.Get)
                return false;

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/204
            if (statusCode != OK && statusCode != NoContent)
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the caches resource identified by the hash is can be served from the local store with the given error conditions. 
        /// </summary>
        /// <remarks>This check reflects the very current state, even if it returns <c>true</c>, a request might just executing to get a write lock on it to refresh the content.</remarks>
        /// <param name="hash"><see cref="Hash128"/> hash returned by <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/> identifying a resource.</param>
        /// <param name="errorType">Possible error condition that can occur during validation. Servers can provision that certain stalled resources can be served if revalidation fails.</param>
        /// <param name="context">Used by the plugin to add an addition logging context for debugging. It can be <c>null</c>.</param>
        /// <returns><c>true</c> if the cached response can be served without validating it with the origin server; otherwise, <c>false</c></returns>
        public bool CanServeWithoutValidation(Hash128 hash, ErrorTypeForValidation errorType, LoggingContext context)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(CanServeWithoutValidation)}({hash}, {errorType})", context);

            if (!_isSupported || !hash.isValid)
                return false;

            // Attempt to find the cached content and metadata for the given hash.
            var (content, metadata) = _database.FindContentAndMetadata(hash);
            if (content == null)
                return false;

            // 
            if (metadata.Lock == LockTypes.Write)
                return false;

            // Check if cache files associated with the hash exist.
            if (!AreCacheFilesExists(hash))
            {
                Delete(hash, context);
                return false;
            }

            if ((content.Flags & CacheFlags.NoCache) != 0)
                return false;

            // Calculate the current age of the cached content, described here:
            // 1.) https://www.rfc-editor.org/rfc/rfc9111.html#name-freshness
            // 2.) https://www.rfc-editor.org/rfc/rfc9111.html#name-calculating-age
            if (content.MaxAge > 0)
            {
                long current_age = content.Age;

                // If there are more than one requests accessing the same resource it's possible that the first one sets the RequestTime
                //  but ResponseTime is the same old value while the second request tries to calculate the resrouce's Age. In this case,
                // we will just use the received Age.
                if (content.ResponseTime > content.RequestTime)
                {
                    var apparent_age = Max(0, (int)(content.ResponseTime - content.Date).TotalSeconds);
                    var response_delay = (int)(content.ResponseTime - content.RequestTime).TotalSeconds;
                    var corrected_age_value = content.Age + response_delay;

                    var corrected_initial_age = Max(apparent_age, corrected_age_value);

                    var resident_time = DateTime.Now - content.ResponseTime;
                    current_age = corrected_initial_age + (int)resident_time.TotalSeconds;
                }

                var maxAge = content.MaxAge;

                switch(errorType)
                {
                    case ErrorTypeForValidation.None:
                        // https://www.rfc-editor.org/rfc/rfc5861.html#section-1
                        // The stale-while-revalidate HTTP Cache-Control extension allows a
                        // cache to immediately return a stale response while it revalidates it
                        // in the background, thereby hiding latency (both in the network and on
                        // the server) from clients.

                        // If it's stalled but there's a value set for StaleWhileRevalidate and it's fresh with its value
                        if (current_age > maxAge && content.StaleWhileRevalidate > 0 && current_age <= (maxAge + content.StaleWhileRevalidate))
                        {
                            maxAge += content.StaleWhileRevalidate;
                            // TODO: send revalidate request
                        }
                        break;

                    case ErrorTypeForValidation.ServerError:
                    case ErrorTypeForValidation.ConnectionError:
                        // Handle stale-if-error caching extension:
                        // https://www.rfc-editor.org/rfc/rfc5861.html#section-4
                        if (content.StaleIfError > 0)
                            maxAge += content.StaleIfError;
                        break;
                }

                return current_age <= maxAge;
            }

            // Check if the content has not expired based on the 'Expires' header.
            return content.Expires > DateTime.Now;
        }

        /// <summary>
        /// Redirects a request to a cached entity.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> that will be redirected.</param>
        /// <param name="hash">Hash obtained from <see cref="HTTPCache.CalculateHash(HTTPMethods, Uri)"/>.</param>
        public void Redirect(HTTPRequest request, Hash128 hash)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(Redirect)}({request}, {hash})", request.Context);

            if (!_isSupported || request == null || !hash.isValid)
                return;

            // Redirect to the local cache
            request.RedirectSettings.RedirectUri = new Uri($"file://{CacheHostName}/{hash}");
            request.RedirectSettings.IsRedirected = true;
        }

        internal void Maintain(ulong contentLength, bool deleteLockedEntries, LoggingContext context)
        {
            if (!_isSupported)
                return;

            HTTPManager.Logger.Information(nameof(HTTPCache), $"Maintain({contentLength:N0}, {deleteLockedEntries}, {System.Threading.Thread.CurrentThread.ManagedThreadId})", context);

            if (HTTPUpdateDelegator.Instance.IsMainThread())
                ThreadedRunner.RunShortLiving<ulong, bool, DateTime, LoggingContext>(MaintainImplementation, contentLength, deleteLockedEntries, HTTPManager.CurrentFrameDateTime, context);
            else
                MaintainImplementation(contentLength, deleteLockedEntries, HTTPManager.CurrentFrameDateTime, context);
        }

        private void ZeroOutCacheSize()
        {
            //HTTPManager.Logger.Information(nameof(HTTPCache), $"CacheSize - ZeroOutCacheSize()");

            Interlocked.Exchange(ref this._cacheSize, 0);
            if (Interlocked.CompareExchange(ref this._subscribed, 1, 0) == 0)
                HTTPManager.Heartbeats.Subscribe(this);
        }

        private void IncrementCacheSize(ulong withSize)
        {
            //HTTPManager.Logger.Information(nameof(HTTPCache), $"CacheSize - IncrementCacheSize({withSize:N0}) => {Interlocked.Add(ref this._cacheSize, (long)withSize):N0}");

            Interlocked.Add(ref this._cacheSize, (long)withSize);

            if (Interlocked.CompareExchange(ref this._subscribed, 1, 0) == 0)
                HTTPManager.Heartbeats.Subscribe(this);
        }

        private void DecrementCacheSize(ulong withSize)
        {
            //HTTPManager.Logger.Information(nameof(HTTPCache), $"CacheSize - DecrementCacheSize({-(long)withSize:N0}) => {Interlocked.Add(ref this._cacheSize, -(long)withSize):N0}");

            Interlocked.Add(ref this._cacheSize, -(long)withSize);

            if (Interlocked.CompareExchange(ref this._subscribed, 1, 0) == 0)
                HTTPManager.Heartbeats.Subscribe(this);
        }

        private void MaintainImplementation(ulong contentLength, bool deleteLockedEntries, DateTime now, LoggingContext context)
        {
            List<Hash128> markedForDelete = null;

            // lock down the whole database
            _database.EnterWriteLock(null);
            ZeroOutCacheSize();
            try
            {
                var deleteOlderDT = Options.DeleteOlder == TimeSpan.MaxValue ? DateTime.MinValue : now - Options.DeleteOlder;

                // Go through hashes in the DB metadata and compare them to the directory names in the cache folder
                // delete those that aren't in the DB/file system.

                for (int i = 0; i < _database.MetadataService.Metadatas.Count; ++i)
                {
                    var metadata = _database.MetadataService.Metadatas[i];

                    // When Maintain first called on startup, we can search for locked entries.
                    // An entry can remeain write locked if the process is terminated unexpectedly while a download is in progress.
                    // By deleting it here we can prevent serving incomplete content.
                    bool isIncomplete = deleteLockedEntries && metadata.Lock != LockTypes.Unlocked;
                    if (isIncomplete)
                        HTTPManager.Logger.Warning(nameof(HTTPCache), $"Incomplete cache entry({metadata}) found!", context);

                    bool isAnyFileMissing = !AreCacheFilesExists(metadata.Hash) && metadata.Lock == LockTypes.Unlocked;

                    if (isAnyFileMissing || isIncomplete || metadata.LastAccessTime <= deleteOlderDT)
                    {
                        if (markedForDelete == null)
                            markedForDelete = new List<Hash128>();

                        markedForDelete.Add(metadata.Hash);
                        metadata.MarkForDelete();
                    }
                    else
                    {
                        IncrementCacheSize(metadata.ContentLength);
                    }
                }

                var sortedMetadatas = new List<CacheMetadata>(_database.MetadataService.Metadatas);
                sortedMetadatas.Sort((x, y) => x.LastAccessTime.CompareTo(y.LastAccessTime));

                var cacheSize = CacheSize;
                var targetCacheSize = (long)(Options.MaxCacheSize - contentLength);
                for (int i = 0; i < sortedMetadatas.Count && cacheSize > targetCacheSize; ++i)
                {
                    var metadata = sortedMetadatas[i];

                    // already marked for deletion
                    if (metadata.IsDeleted)
                        continue;

                    // is in use
                    if (metadata.Lock != LockTypes.Unlocked)
                        continue;

                    if (markedForDelete == null)
                        markedForDelete = new List<Hash128>();

                    markedForDelete.Add(metadata.Hash);
                    cacheSize -= (long)metadata.ContentLength;
                }
            }
            finally
            {
                _database.ExitWriteLock(null);
            }

            if (markedForDelete != null)
            {
                HTTPManager.Logger.Information(nameof(HTTPCache), $"Maintain - collected {markedForDelete.Count} entries for deletion!", context);

                foreach (Hash128 key in markedForDelete)
                    Delete(key, context);

                markedForDelete.Clear();
            }
            else
                HTTPManager.Logger.Information(nameof(HTTPCache), "Maintain - collected 0 entries for deletion!", context);
        }

        private static void WriteHeaders(Stream headerStream, Dictionary<string, List<string>> headers)
        {
            if (headerStream == null || headers == null)
                return;

            foreach (var kvp in headers)
            {
                // https://www.rfc-editor.org/rfc/rfc9111.html#name-storing-header-and-trailer-
                // TODO: expand the no-save list
                if (kvp.Key.Equals("alt-svc", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("content-encoding", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("transfer-encoding", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("connection", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("proxy-authenticate", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (kvp.Value == null)
                {
                    headerStream.WriteString(kvp.Key);
                    headerStream.WriteString(":");
                    headerStream.WriteString(string.Empty);
                    headerStream.WriteLine();

                    continue;
                }

                foreach (var value in kvp.Value)
                {
                    headerStream.WriteString(kvp.Key);
                    headerStream.WriteString(":");
                    headerStream.WriteString(value);
                    headerStream.WriteLine();
                }
            }

            headerStream.WriteLine();
            headerStream.Flush();
        }

        private static Dictionary<string, List<string>> LoadHeaders(Stream headersStream)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            string headerName = HTTPResponse.ReadTo(headersStream, (byte)':', LF);
            while (headerName != string.Empty)
            {
                string value = HTTPResponse.ReadTo(headersStream, LF);

                result.AddHeader(headerName, value);

                headerName = HTTPResponse.ReadTo(headersStream, (byte)':', LF);
            }

            return result;
        }

        public void Dispose()
        {
            HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(Dispose)}");

            ZeroOutCacheSize();

            try
            {
                _database?.Dispose();
                _database = null;
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTPCache), $"{nameof(Dispose)}", ex);
            }

            HTTPManager.Logger.Information(nameof(HTTPCache), $"{nameof(Dispose)} - Disposed!");
        }

        private static void LogCachingHeaders(Dictionary<string, List<string>> headers, LoggingContext context)
        {
            if (!HTTPManager.Logger.IsDiagnostic)
                return;

            var etag = headers.GetFirstHeaderValue("etag");
            var expires = headers.GetFirstHeaderValue("expires");
            var lastModified = headers.GetFirstHeaderValue("last-modified");
            var age = headers.GetFirstHeaderValue("age");
            var date = headers.GetFirstHeaderValue("date");
            var cacheControl = headers.GetFirstHeaderValue("cache-control");

            if (etag != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "ETag: " + etag, context);

            if (expires != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "Expires: " + expires, context);

            if (lastModified != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "Last-Modified: " + lastModified, context);

            if (age != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "Age: " + age, context);

            if (date != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "Date: " + date, context);

            if (cacheControl != null)
                HTTPManager.Logger.Verbose(nameof(HTTPCache), "Cache-Control: " + cacheControl, context);
        }

        /// <summary>
        /// Clears the HTTP cache by removing all cached entries and associated metadata.
        /// </summary>
        public void Clear()
        {
            if (!_isSupported)
                return;

            //_database.EnterWriteLock(null);
            try
            {
                var copyOfMetadatas = new List<CacheMetadata>(_database.MetadataService.Metadatas);

                foreach (var metadata in copyOfMetadatas)
                    Delete(metadata.Hash, null);
            }
            finally
            {
                //_database.ExitWriteLock(null);
            }
        }

        void IHeartbeat.OnHeartbeatUpdate(DateTime now, TimeSpan dif)
        {
            try
            {
                this.OnCacheSizeChanged?.Invoke();
            }
            catch(Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTPCache), "OnCacheSizeChanged", ex, null);
            }

            HTTPManager.Heartbeats.Unsubscribe(this);
            Interlocked.Exchange(ref this._subscribed, 0);
        }
    }
}
