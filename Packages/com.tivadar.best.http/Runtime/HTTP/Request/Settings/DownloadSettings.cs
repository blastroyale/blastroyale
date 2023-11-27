using System.Collections.Generic;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Response;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Request.Settings
{
    /// <summary>
    /// Delegate for handling the event when headers are received in a response.
    /// </summary>
    /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
    /// <param name="resp">The <see cref="HTTPResponse"/> object.</param>
    /// <param name="headers">The headers received from the server.</param>
    public delegate void OnHeadersReceivedDelegate(HTTPRequest req, HTTPResponse resp, Dictionary<string, List<string>> headers);

    /// <summary>
    /// Delegate for handling progress during the download.
    /// </summary>
    /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
    /// <param name="progress">The number of bytes downloaded so far.</param>
    /// <param name="length">The total length of the content being downloaded, or -1 if the length cannot be determined.</param>
    public delegate void OnProgressDelegate(HTTPRequest req, long progress, long length);

    /// <summary>
    /// Delegate for handling the event when the download of content starts.
    /// </summary>
    /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
    /// <param name="resp">The <see cref="HTTPResponse"/> object.</param>
    /// <param name="stream">The <see cref="DownloadContentStream"/> used for receiving downloaded content.</param>
    public delegate void OnDownloadStartedDelegate(HTTPRequest req, HTTPResponse resp, DownloadContentStream stream);

    /// <summary>
    /// Delegate for creating a new <see cref="DownloadContentStream"/> object.
    /// </summary>
    /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
    /// <param name="resp">The <see cref="HTTPResponse"/> object.</param>
    /// <param name="bufferAvailableHandler">An interface for notifying connections that the buffer has free space for downloading data.</param>
    /// <returns>The newly created <see cref="DownloadContentStream"/>.</returns>
    public delegate DownloadContentStream OnCreateDownloadStreamDelegate(HTTPRequest req, HTTPResponse resp, IDownloadContentBufferAvailable bufferAvailableHandler);

#if !UNITY_WEBGL || UNITY_EDITOR
    /// <summary>
    /// Delegate for handling the event when a response is upgraded.
    /// </summary>
    /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
    /// <param name="resp">The <see cref="HTTPResponse"/> object.</param>
    /// <param name="contentProvider">A stream that provides content for the upgraded response.</param>
    /// <returns><c>true</c> to keep the underlying connection open; otherwise, <c>false</c>.</returns>
    public delegate bool OnUpgradedDelegate(HTTPRequest req, HTTPResponse resp, PeekableContentProviderStream contentProvider);
#endif

    /// <summary>
    /// Represents settings for configuring an HTTP request's download behavior.
    /// </summary>
    public class DownloadSettings
    {
        /// <summary>
        /// Gets or sets the maximum number of bytes the <see cref="DownloadContentStream"/> will buffer before pausing the download until its buffer has free space again.
        /// </summary>
        /// <remarks>
        /// When the download content stream buffers data up to this specified limit, it will temporarily pause downloading until it has free space in its buffer.
        /// Increasing this value may help reduce the frequency of pauses during downloads, but it also increases memory usage.
        /// </remarks>
        public long ContentStreamMaxBuffered = 1024 * 1024;

        /// <summary>
        /// Gets or sets a value indicating whether caching should be enabled for this request.
        /// </summary>
        public bool DisableCache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the response's <see cref="DownloadContentStream"/> should be populated with downloaded data or if the content should be written only to the local cache when available.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c> and the content isn't cacheable (e.g., it doesn't have any cache-related headers), the content will be downloaded but will be lost.
        /// </remarks>
        /// <summary>
        /// Gets or sets a value indicating whether the response's <see cref="HTTPResponse.DownStream"/> should be populated with downloaded data or if the content should be written only to the local cache when available.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c> and the content isn't cacheable (e.g., it doesn't have any cache-related headers), the content will be downloaded but will be lost.
        /// This is because the downloaded data would be written exclusively to the local cache and will not be stored in memory or the response's <see cref="HTTPResponse.DownStream"/> for further use.
        /// </remarks>
        public bool CacheOnly { get; set; }

        /// <summary>
        /// This event is called when the plugin received and parsed all headers.
        /// </summary>
        public OnHeadersReceivedDelegate OnHeadersReceived;

        /// <summary>
        /// Represents a function that creates a new <see cref="DownloadContentStream"/> object when needed for downloading content.
        /// </summary>
        public OnCreateDownloadStreamDelegate DownloadStreamFactory = (req, resp, bufferAvailableHandler)
            => new DownloadContentStream(resp, req.DownloadSettings.ContentStreamMaxBuffered, bufferAvailableHandler);

        /// <summary>
        /// Event for handling the start of the download process for 2xx status code responses.
        /// </summary>
        /// <param name="req">The <see cref="HTTPRequest"/> object.</param>
        /// <param name="resp">The <see cref="HTTPResponse"/> object representing the response.</param>
        /// <param name="stream">
        /// The <see cref="DownloadContentStream"/> containing the downloaded data. It might already be populated with some content.
        /// </param>
        /// <remarks>
        /// This event is called when the plugin expects the server to send content. When called, the <see cref="DownloadContentStream"/>
        /// might already be populated with some content. It is specifically meant for responses with 2xx status codes.
        /// </remarks>
        public OnDownloadStartedDelegate OnDownloadStarted;

        /// <summary>
        /// Gets or sets the event that is called when new data is downloaded from the server.
        /// </summary>
        /// <remarks>
        /// The first parameter is the original <see cref="HTTPRequest"/> object itself, the second parameter is the downloaded bytes, and the third parameter is the content length.
        /// There are download modes where we can't figure out the exact length of the final content. In these cases, we guarantee that the third parameter will be at least the size of the second one.
        /// </remarks>
        public OnProgressDelegate OnDownloadProgress;

#if !UNITY_WEBGL || UNITY_EDITOR
#pragma warning disable 0649
        /// <summary>
        /// Called when a response with status code 101 (upgrade), "<c>connection: upgrade</c>" header and value or an "<c>upgrade</c>" header received.
        /// </summary>
        /// <remarks>This callback might be called on a thread other than the main one!</remarks>
        /// <remarks>Isn't available under WebGL!</remarks>
        public OnUpgradedDelegate OnUpgraded;
#pragma warning restore 0649
#endif
    }
}
