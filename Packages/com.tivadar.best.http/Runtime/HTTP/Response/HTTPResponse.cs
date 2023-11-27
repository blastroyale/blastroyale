using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

using static Best.HTTP.Response.HTTPStatusCodes;

namespace Best.HTTP
{
    using Best.HTTP.Caching;
    using Best.HTTP.Hosts.Connections;
    using Best.HTTP.Response;
    using Best.HTTP.Shared;
    using Best.HTTP.Shared.Extensions;
    using Best.HTTP.Shared.Logger;
    using Best.HTTP.Shared.PlatformSupport.Memory;

    /// <summary>
    /// Represents an HTTP response received from a remote server, containing information about the response status, headers, and data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The HTTPResponse class represents an HTTP response received from a remote server. It contains information about the response status, headers, and the data content.
    /// </para>
    /// <para>
    /// Key Features:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Response Properties</term>
    ///         <description>Provides access to various properties such as <see cref="HTTPVersion"/>, <see cref="StatusCode"/>, <see cref="Message"/>, and more, to inspect the response details.</description>
    ///     </item>
    ///     <item>
    ///         <term>Data Access</term>
    ///         <description>Allows access to the response data in various forms, including raw bytes, UTF-8 text, and as a <see cref="Texture2D"/> for image data.</description>
    ///     </item>
    ///     <item>
    ///         <term>Header Management</term>
    ///         <description>Provides methods to add, retrieve, and manipulate HTTP headers associated with the response, making it easy to inspect and work with header information.</description>
    ///     </item>
    ///     <item>
    ///         <term>Caching Support</term>
    ///         <description>Supports response caching, enabling the storage of downloaded data in local cache storage for future use.</description>
    ///     </item>
    ///     <item>
    ///         <term>Stream Management</term>
    ///         <description>Manages the download process and data streaming through a <see cref="DownloadContentStream"/> (<see cref="DownStream"/>) to optimize memory usage and ensure efficient handling of large response bodies.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class HTTPResponse : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// Gets the version of the HTTP protocol with which the response was received. Typically, this is HTTP/1.1 for local file and cache responses, even if the original response received with a different version.
        /// </summary>
        public Version HTTPVersion { get; protected set; }

        /// <summary>
        /// Gets the HTTP status code sent from the server, indicating the outcome of the HTTP request.
        /// </summary>
        public int StatusCode { get; protected set; }

        /// <summary>
        /// Gets the message sent along with the status code from the server. This message can add some details, but it's empty for HTTP/2 responses.
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the response represents a successful HTTP request. Returns true if the status code is in the range of [200..300[ or 304 (Not Modified).
        /// </summary>
        public bool IsSuccess { get { return (this.StatusCode >= OK && this.StatusCode < MultipleChoices) || this.StatusCode == NotModified; } }

        /// <summary>
        /// Gets a value indicating whether the response body is read from the cache.
        /// </summary>
        public bool IsFromCache { get; internal set; }

        /// <summary>
        /// Gets the headers sent from the server as key-value pairs. You can use additional methods to manage and retrieve header information.
        /// </summary>
        /// <remarks>
        /// The Headers property provides access to the headers sent by the server in the HTTP response. You can use the following methods to work with headers:
        /// <list type="bullet">
        ///     <item><term><see cref="AddHeader(string, string)"/> </term><description>Adds an HTTP header with the specified name and value to the response headers.</description></item>
        ///     <item><term><see cref="GetHeaderValues(string)"/> </term><description>Retrieves the list of values for a given header name as received from the server.</description></item>
        ///     <item><term><see cref="GetFirstHeaderValue(string)"/> </term><description>Retrieves the first value for a given header name as received from the server.</description></item>
        ///     <item><term><see cref="HasHeaderWithValue(string, string)"/> </term><description>Checks if a header with the specified name and value exists in the response headers.</description></item>
        ///     <item><term><see cref="HasHeader(string)"/> </term><description>Checks if a header with the specified name exists in the response headers.</description></item>
        ///     <item><term><see cref="GetRange()"/></term><description>Parses the 'Content-Range' header's value and returns a <see cref="HTTPRange"/> object representing the byte range of the response content.</description></item>
        /// </list>
        /// </remarks>
        public Dictionary<string, List<string>> Headers { get; protected set; }

        /// <summary>
        /// The data that downloaded from the server. All Transfer and Content encodings decoded if any(eg. chunked, gzip, deflate).
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (this._data != null)
                    return this._data;

                if (this.DownStream == null)
                    return null;

                CheckDisposed();

                this._data = new byte[this.DownStream.Length];
                try
                {
                    this.DownStream.Read(this._data, 0, this._data.Length);
                }
                catch (Exception ex)
                {
                    this._data = null;

                    HTTPManager.Logger.Exception(nameof(HTTPResponse), "get_Data", ex, this.Context);
                }
                finally
                {
                    this.DownStream.Dispose();
                }

                return this._data;
            }
        }
        private byte[] _data;

        /// <summary>
        /// The normal HTTP protocol is upgraded to an other.
        /// </summary>
        public bool IsUpgraded { get; internal set; }

        /// <summary>
        /// Cached, converted data.
        /// </summary>
        protected string dataAsText;

        /// <summary>
        /// The data converted to an UTF8 string.
        /// </summary>
        public string DataAsText
        {
            get
            {
                if (Data == null)
                    return string.Empty;

                if (!string.IsNullOrEmpty(dataAsText))
                    return dataAsText;

                CheckDisposed();

                return dataAsText = Encoding.UTF8.GetString(Data, 0, Data.Length);
            }
        }

        /// <summary>
        /// Cached converted data.
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// The data loaded to a Texture2D.
        /// </summary>
        public Texture2D DataAsTexture2D
        {
            get
            {
                if (Data == null)
                    return null;

                if (texture != null)
                    return texture;

                CheckDisposed();

                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.LoadImage(Data, true);

                return texture;
            }
        }

        /// <summary>
        /// Reference to the <see cref="DownloadContentStream"/> instance that contains the downloaded data.
        /// </summary>
        public DownloadContentStream DownStream { get; internal set; }

        /// <summary>
        /// IProtocol.LoggingContext implementation.
        /// </summary>
        public LoggingContext Context { get; private set; }

        /// <summary>
        /// The original request that this response is created for.
        /// </summary>
        public HTTPRequest Request { get; private set; }

        #endregion

        protected HTTPCacheContentWriter _cacheWriter;

        private bool _isDisposed;

        internal HTTPResponse(HTTPRequest request, bool isFromCache)
        {
            this.Request = request;

            this.IsFromCache = isFromCache;

            this.Context = new LoggingContext(this);
            this.Context.Add("BaseRequest", request.Context);
        }

        #region Header Management

        /// <summary>
        /// Adds an HTTP header with the specified name and value to the response headers.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        public void AddHeader(string name, string value) => this.Headers = this.Headers.AddHeader(name, value);

        /// <summary>
        /// Retrieves the list of values for a given header name as received from the server.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <returns>
        /// A list of header values if the header exists and contains values; otherwise, returns <c>null</c>.
        /// </returns>
        public List<string> GetHeaderValues(string name) => this.Headers.GetHeaderValues(name);

        /// <summary>
        /// Retrieves the first value for a given header name as received from the server.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <returns>
        /// The first header value if the header exists and contains values; otherwise, returns <c>null</c>.
        /// </returns>
        public string GetFirstHeaderValue(string name) => this.Headers.GetFirstHeaderValue(name);

        /// <summary>
        /// Checks if a header with the specified name and value exists in the response headers.
        /// </summary>
        /// <param name="headerName">The name of the header to check.</param>
        /// <param name="value">The value to check for in the header.</param>
        /// <returns>
        /// <c>true</c> if a header with the given name and value exists in the response headers; otherwise, <c>false</c>.
        /// </returns>
        public bool HasHeaderWithValue(string headerName, string value) => this.Headers.HasHeaderWithValue(headerName, value);

        /// <summary>
        /// Checks if a header with the specified name exists in the response headers.
        /// </summary>
        /// <param name="headerName">The name of the header to check.</param>
        /// <returns>
        /// <c>true</c> if a header with the given name exists in the response headers; otherwise, <c>false</c>.
        /// </returns>
        public bool HasHeader(string headerName) => this.Headers.HasHeader(headerName);

        /// <summary>
        /// Parses the <c>'Content-Range'</c> header's value and returns a <see cref="HTTPRange"/> object representing the byte range of the response content.
        /// </summary>
        /// <remarks>
        /// If the server ignores a byte-range-spec because it is syntactically invalid, the server SHOULD treat the request as if the invalid Range header field did not exist.
        /// (Normally, this means return a 200 response containing the full entity). In this case because there are no <c>'Content-Range'</c> header values, this function will return <c>null</c>.
        /// </remarks>
        /// <returns>
        /// A <see cref="HTTPRange"/> object representing the byte range of the response content, or <c>null</c> if no '<c>Content-Range</c>' header is found.
        /// </returns>
        public HTTPRange GetRange()
        {
            var rangeHeaders = this.Headers.GetHeaderValues("content-range");
            if (rangeHeaders == null)
                return null;

            // A byte-content-range-spec with a byte-range-resp-spec whose last- byte-pos value is less than its first-byte-pos value,
            //  or whose instance-length value is less than or equal to its last-byte-pos value, is invalid.
            // The recipient of an invalid byte-content-range- spec MUST ignore it and any content transferred along with it.

            // A valid content-range sample: "bytes 500-1233/1234"
            var ranges = rangeHeaders[0].Split(new char[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

            // A server sending a response with status code 416 (Requested range not satisfiable) SHOULD include a Content-Range field with a byte-range-resp-spec of "*".
            // The instance-length specifies the current length of the selected resource.
            // "bytes */1234"
            if (ranges[1] == "*")
                return new HTTPRange(int.Parse(ranges[2]));

            return new HTTPRange(int.Parse(ranges[1]), int.Parse(ranges[2]), ranges[3] != "*" ? int.Parse(ranges[3]) : -1);
        }

        #endregion

        #region Static Stream Management Helper Functions

        internal static string ReadTo(Stream stream, byte blocker)
        {
            byte[] readBuf = BufferPool.Get(1024, true);
            try
            {
                int bufpos = 0;

                int ch = stream.ReadByte();
                while (ch != blocker && ch != -1)
                {
                    if (ch > 0x7f) //replaces asciitostring
                        ch = '?';

                    //make buffer larger if too short
                    if (readBuf.Length <= bufpos)
                        BufferPool.Resize(ref readBuf, readBuf.Length * 2, true, false);

                    if (bufpos > 0 || !char.IsWhiteSpace((char)ch)) //trimstart
                        readBuf[bufpos++] = (byte)ch;
                    ch = stream.ReadByte();
                }

                while (bufpos > 0 && char.IsWhiteSpace((char)readBuf[bufpos - 1]))
                    bufpos--;

                return System.Text.Encoding.UTF8.GetString(readBuf, 0, bufpos);
            }
            finally
            {
                BufferPool.Release(readBuf);
            }
        }

        internal static string ReadTo(Stream stream, byte blocker1, byte blocker2)
        {
            byte[] readBuf = BufferPool.Get(1024, true);
            try
            {
                int bufpos = 0;

                int ch = stream.ReadByte();
                while (ch != blocker1 && ch != blocker2 && ch != -1)
                {
                    if (ch > 0x7f) //replaces asciitostring
                        ch = '?';

                    //make buffer larger if too short
                    if (readBuf.Length <= bufpos)
                        BufferPool.Resize(ref readBuf, readBuf.Length * 2, true, true);

                    if (bufpos > 0 || !char.IsWhiteSpace((char)ch)) //trimstart
                        readBuf[bufpos++] = (byte)ch;
                    ch = stream.ReadByte();
                }

                while (bufpos > 0 && char.IsWhiteSpace((char)readBuf[bufpos - 1]))
                    bufpos--;

                return System.Text.Encoding.UTF8.GetString(readBuf, 0, bufpos);
            }
            finally
            {
                BufferPool.Release(readBuf);
            }
        }

        internal static string NoTrimReadTo(Stream stream, byte blocker1, byte blocker2)
        {
            byte[] readBuf = BufferPool.Get(1024, true);
            try
            {
                int bufpos = 0;

                int ch = stream.ReadByte();
                while (ch != blocker1 && ch != blocker2 && ch != -1)
                {
                    if (ch > 0x7f) //replaces asciitostring
                        ch = '?';

                    //make buffer larger if too short
                    if (readBuf.Length <= bufpos)
                        BufferPool.Resize(ref readBuf, readBuf.Length * 2, true, true);

                    if (bufpos > 0 || !char.IsWhiteSpace((char)ch)) //trimstart
                        readBuf[bufpos++] = (byte)ch;
                    ch = stream.ReadByte();
                }

                return System.Text.Encoding.UTF8.GetString(readBuf, 0, bufpos);
            }
            finally
            {
                BufferPool.Release(readBuf);
            }
        }

        #endregion

        protected void BeginReceiveContent()
        {
            CheckDisposed();

            if (!Request.DownloadSettings.DisableCache && !IsFromCache)
            {
                // If caching is enabled and the response not from cache and it's cacheble we will cache the downloaded data
                // by writing it to the stream returned by BeginCache
                _cacheWriter = HTTPManager.LocalCache?.BeginCache(Request.MethodType, Request.CurrentUri, this.StatusCode, this.Headers, this.Context);
            }
        }

        /// <summary>
        /// Add data to the fragments list.
        /// </summary>
        /// <param name="buffer">The buffer to be added.</param>
        /// <param name="pos">The position where we start copy the data.</param>
        /// <param name="length">How many data we want to copy.</param>
        protected void FeedDownloadedContentChunk(BufferSegment segment)
        {
            if (segment == BufferSegment.Empty)
                return;

            CheckDisposed();

            _cacheWriter?.Write(segment);

            if (!this.Request.DownloadSettings.CacheOnly)
                this.DownStream.Write(segment);
            else
                BufferPool.Release(segment);
        }

        protected void FinishedContentReceiving()
        {
            CheckDisposed();

            _cacheWriter?.Cache?.EndCache(_cacheWriter, true, this.Context);
            _cacheWriter = null;
        }

        protected void CreateDownloadStream(IDownloadContentBufferAvailable bufferAvailable)
        {
            if (this.DownStream != null)
                this.DownStream.Dispose();

            this.DownStream = this.Request.DownloadSettings.DownloadStreamFactory(this.Request, this, bufferAvailable);

            HTTPManager.Logger.Information(this.GetType().Name, $"{nameof(DownloadContentStream)} initialized with Maximum Buffer Size: {this.DownStream.MaxBuffered:N0}", this.Context);

            // Send download-started event only when the final content is expected (2xx status codes).
            // Otherwise, for one request multiple download-started even would be trigger every time it gets redirected.
            if (this.StatusCode >= OK && this.StatusCode < MultipleChoices)
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, RequestEvents.DownloadStarted));
        }

        protected void CheckDisposed()
        {
            if (this._isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this._isDisposed)
            {
                _cacheWriter?.Cache?.EndCache(_cacheWriter, false, this.Context);
                _cacheWriter = null;

                if (this.DownStream != null && !this.DownStream.IsDetached)
                {
                    this.DownStream.Dispose();
                    this.DownStream = null;
                }
            }

            this._isDisposed = true;
        }
    }
}
