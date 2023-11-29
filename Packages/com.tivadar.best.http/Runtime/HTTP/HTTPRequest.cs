using System;
using System.Collections;
using System.Collections.Generic;

using Best.HTTP.Cookies;
using Best.HTTP.Hosts.Connections;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Authenticators;
using Best.HTTP.Request.Settings;
using Best.HTTP.Request.Timings;
using Best.HTTP.Request.Upload;
using Best.HTTP.Response.Decompression;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

namespace Best.HTTP
{
    /// <summary>
    /// Delegate for a callback function that is called after the request is fully processed.
    /// </summary>
    public delegate void OnRequestFinishedDelegate(HTTPRequest req, HTTPResponse resp);

    /// <summary>
    /// Delegate for enumerating headers during request preparation.
    /// </summary>
    /// <param name="header">The header name.</param>
    /// <param name="values">A list of header values.</param>
    internal delegate void OnHeaderEnumerationDelegate(string header, List<string> values);

    /// <summary>
    /// Represents an HTTP request that allows you to send HTTP requests to remote servers and receive responses asynchronously.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///     <item><term>Asynchronous HTTP requests</term><description>Utilize a Task-based API for performing HTTP requests asynchronously.</description></item>
    ///     <item><term>Unity coroutine support</term><description>Seamlessly integrate with Unity's coroutine system for coroutine-based request handling.</description></item>
    ///     <item><term>HTTP method support</term><description>Support for various HTTP methods including GET, POST, PUT, DELETE, and more.</description></item>
    ///     <item><term>Compression and decompression</term><description>Automatic request and response compression and decompression for efficient data transfer.</description></item>
    ///     <item><term>Timing information</term><description>Collect detailed timing information about the request for performance analysis.</description></item>
    ///     <item><term>Upload and download support</term><description>Support for uploading and downloading files with progress tracking.</description></item>
    ///     <item><term>Customizable</term><description>Extensive options for customizing request headers, handling cookies, and more.</description></item>
    ///     <item><term>Redirection handling</term><description>Automatic handling of request redirections for a seamless experience.</description></item>
    ///     <item><term>Proxy server support</term><description>Ability to route requests through proxy servers for enhanced privacy and security.</description></item>
    ///     <item><term>Authentication</term><description>Automatic authentication handling using authenticators for secure communication.</description></item>
    ///     <item><term>Cancellation support</term><description>Ability to cancel requests to prevent further processing and release resources.</description></item>
    /// </list>
    /// </remarks>
    public sealed class HTTPRequest : IEnumerator
    {
        /// <summary>
        /// Creates an <see cref="HTTPMethods.Get">HTTP GET</see> request with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <returns>An HTTPRequest instance for the GET request.</returns>
        public static HTTPRequest CreateGet(string url) => new HTTPRequest(url);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Get">HTTP GET</see> request with the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <returns>An HTTPRequest instance for the GET request.</returns>
        public static HTTPRequest CreateGet(Uri uri) => new HTTPRequest(uri);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Get">HTTP GET</see> request with the specified URL and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the GET request.</returns>
        public static HTTPRequest CreateGet(string url, OnRequestFinishedDelegate callback) => new HTTPRequest(url, callback);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Get">HTTP GET</see> request with the specified URI and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the GET request.</returns>
        public static HTTPRequest CreateGet(Uri uri, OnRequestFinishedDelegate callback) => new HTTPRequest(uri, callback);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Post">HTTP POST</see> request with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <returns>An HTTPRequest instance for the POST request.</returns>
        public static HTTPRequest CreatePost(string url) => new HTTPRequest(url, HTTPMethods.Post);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Post">HTTP POST</see> request with the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <returns>An HTTPRequest instance for the POST request.</returns>
        public static HTTPRequest CreatePost(Uri uri) => new HTTPRequest(uri, HTTPMethods.Post);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Post">HTTP POST</see> request with the specified URL and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the POST request.</returns>
        public static HTTPRequest CreatePost(string url, OnRequestFinishedDelegate callback) => new HTTPRequest(url, HTTPMethods.Post, callback);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Post">HTTP POST</see> request with the specified URI and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the POST request.</returns>
        public static HTTPRequest CreatePost(Uri uri, OnRequestFinishedDelegate callback) => new HTTPRequest(uri, HTTPMethods.Post, callback);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Put">HTTP PUT</see> request with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <returns>An HTTPRequest instance for the PUT request.</returns>
        public static HTTPRequest CreatePut(string url) => new HTTPRequest(url, HTTPMethods.Put);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Put">HTTP PUT</see> request with the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <returns>An HTTPRequest instance for the PUT request.</returns>
        public static HTTPRequest CreatePut(Uri uri) => new HTTPRequest(uri, HTTPMethods.Put);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Put">HTTP PUT</see> request with the specified URL and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the PUT request.</returns>
        public static HTTPRequest CreatePut(string url, OnRequestFinishedDelegate callback) => new HTTPRequest(url, HTTPMethods.Put, callback);

        /// <summary>
        /// Creates an <see cref="HTTPMethods.Put">HTTP PUT</see> request with the specified URI and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        /// <returns>An HTTPRequest instance for the PUT request.</returns>
        public static HTTPRequest CreatePut(Uri uri, OnRequestFinishedDelegate callback) => new HTTPRequest(uri, HTTPMethods.Put, callback);

        /// <summary>
        /// Cached uppercase values to save some cpu cycles and GC alloc per request.
        /// </summary>
        public static readonly string[] MethodNames = {
                                                          HTTPMethods.Get.ToString().ToUpper(),
                                                          HTTPMethods.Head.ToString().ToUpper(),
                                                          HTTPMethods.Post.ToString().ToUpper(),
                                                          HTTPMethods.Put.ToString().ToUpper(),
                                                          HTTPMethods.Delete.ToString().ToUpper(),
                                                          HTTPMethods.Patch.ToString().ToUpper(),
                                                          HTTPMethods.Merge.ToString().ToUpper(),
                                                          HTTPMethods.Options.ToString().ToUpper(),
                                                          HTTPMethods.Connect.ToString().ToUpper(),
                                                          HTTPMethods.Query.ToString().ToUpper()
                                                      };

        /// <summary>
        /// The method that how we want to process our request the server.
        /// </summary>
        public HTTPMethods MethodType { get; set; }

        /// <summary>
        /// The original request's Uri.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// If redirected it contains the RedirectUri.
        /// </summary>
        public Uri CurrentUri { get { return this.RedirectSettings.IsRedirected ? this.RedirectSettings.RedirectUri : Uri; } }

        /// <summary>
        /// A host-key that can be used to find the right host-variant for the request.
        /// </summary>
        public HostKey CurrentHostKey { get => HostKey.From(this); }

        /// <summary>
        /// The response received from the server.
        /// </summary>
        /// <remarks>If an exception occurred during reading of the response stream or can't connect to the server, this will be null!</remarks>
        public HTTPResponse Response { get; internal set; }

        /// <summary>
        /// Download related options and settings.
        /// </summary>
        public DownloadSettings DownloadSettings = new DownloadSettings();

        /// <summary>
        /// Upload related options and settings.
        /// </summary>
        public UploadSettings UploadSettings = new UploadSettings();

        /// <summary>
        /// Timeout settings for the request.
        /// </summary>
        public TimeoutSettings TimeoutSettings;

        /// <summary>
        /// Retry settings for the request.
        /// </summary>
        public RetrySettings RetrySettings;

        /// <summary>
        /// Proxy settings for the request.
        /// </summary>
        public ProxySettings ProxySettings;

        /// <summary>
        /// Redirect settings for the request.
        /// </summary>
        public RedirectSettings RedirectSettings { get; private set; } = new RedirectSettings(10);

        /// <summary>
        /// The callback function that will be called after the request is fully processed.
        /// </summary>
        public OnRequestFinishedDelegate Callback { get; set; }

        /// <summary>
        /// Indicates if <see cref="Abort"/> is called on this request.
        /// </summary>
        public bool IsCancellationRequested { get => this.CancellationTokenSource != null ? this.CancellationTokenSource.IsCancellationRequested : true; }

        /// <summary>
        /// Gets the cancellation token source for this request.
        /// </summary>
        internal System.Threading.CancellationTokenSource CancellationTokenSource { get; private set; }

        /// <summary>
        /// Action called when <see cref="Abort"/> function is invoked.
        /// </summary>
        public Action<HTTPRequest> OnCancellationRequested;

        /// <summary>
        /// Stores any exception that occurs during processing of the request or response.
        /// </summary>
        /// <remarks>This property if for debugging purposes as <see href="https://github.com/Benedicht/BestHTTP-Issues/issues/174">seen here</see>!</remarks>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Any user-object that can be passed with the request.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Current state of this request.
        /// </summary>
        public HTTPRequestStates State {
            get { return this._state; }
            internal set {
                if (!HTTPUpdateDelegator.Instance.IsMainThread())
                    HTTPManager.Logger.Error(nameof(HTTPRequest), $"State.Set({this._state} => {value}) isn't called on the main thread({HTTPUpdateDelegator.Instance.MainThreadId})!", this.Context);

                // In a case where the request is aborted its state is set to a >= Finished state then,
                // on another thread the reqest processing will fail too queuing up a >= Finished state again.
                if (this._state >= HTTPRequestStates.Finished && value >= HTTPRequestStates.Finished)
                {
                    HTTPManager.Logger.Warning(nameof(HTTPRequest), $"State.Set({this._state} => {value})", this.Context);
                    return;
                }

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(HTTPRequest), $"State.Set({this._state} => {value})", this.Context);

                this._state = value;
            }
        }
        private volatile HTTPRequestStates _state;

        /// <summary>
        /// Timing information about the request.
        /// </summary>
        public TimingCollector Timing { get; private set; }

        /// <summary>
        /// An IAuthenticator implementation that can be used to authenticate the request.
        /// </summary>
        /// <remarks>Out-of-the-box included authenticators are <see cref="CredentialAuthenticator"/> and <see cref="BearerTokenAuthenticator"/>.</remarks>
        public IAuthenticator Authenticator;

#if UNITY_WEBGL
        /// <summary>
        /// Its value will be set to the XmlHTTPRequest's withCredentials field. Its default value is HTTPManager.IsCookiesEnabled's value.
        /// </summary>
        /// <remarks>
        /// More details can be found here:
        /// <list type="bullet">
        ///     <item><description><see href="https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest/withCredentials">Mozilla Developer Networks - XMLHttpRequest.withCredentials</see></description></item>
        /// </list>
        /// </remarks>
        public bool WithCredentials { get; set; }
#endif

        /// <summary>
        /// Logging context of the request.
        /// </summary>
        public LoggingContext Context { get; private set; }

        private Dictionary<string, List<string>> Headers { get; set; }

        /// <summary>
        /// Creates an HTTP GET request with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        public HTTPRequest(string url)
            :this(new Uri(url)) {}

        /// <summary>
        /// Creates an HTTP GET request with the specified URL and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        public HTTPRequest(string url, OnRequestFinishedDelegate callback)
            : this(new Uri(url), callback) { }

        /// <summary>
        /// Creates an HTTP GET request with the specified URL and HTTP method type.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="methodType">The HTTP method type for the request (e.g., GET, POST, PUT).</param>
        public HTTPRequest(string url, HTTPMethods methodType)
            : this(new Uri(url), methodType) { }

        /// <summary>
        /// Creates an HTTP request with the specified URL, HTTP method type, and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="methodType">The HTTP method type for the request (e.g., GET, POST, PUT).</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        public HTTPRequest(string url, HTTPMethods methodType, OnRequestFinishedDelegate callback)
            : this(new Uri(url), methodType, callback) { }

        /// <summary>
        /// Creates an HTTP GET request with the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        public HTTPRequest(Uri uri)
            : this(uri, HTTPMethods.Get, null)
        {
        }

        /// <summary>
        /// Creates an HTTP GET request with the specified URI and registers a callback function to be called
        /// when the request is fully processed.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        public HTTPRequest(Uri uri, OnRequestFinishedDelegate callback)
            : this(uri, HTTPMethods.Get, callback)
        {
        }

        /// <summary>
        /// Creates an HTTP request with the specified URI and HTTP method type.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="methodType">The HTTP method type for the request (e.g., GET, POST, PUT).</param>
        public HTTPRequest(Uri uri, HTTPMethods methodType)
            : this(uri, methodType, null)
        {
        }

        /// <summary>
        /// Creates an HTTP request with the specified URI, HTTP method type, and registers a callback function
        /// to be called when the request is fully processed.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="methodType">The HTTP method type for the request (e.g., GET, POST, PUT).</param>
        /// <param name="callback">A callback function to be called when the request is finished.</param>
        public HTTPRequest(Uri uri, HTTPMethods methodType, OnRequestFinishedDelegate callback)
        {
            this.Uri = uri;
            this.MethodType = methodType;

            this.TimeoutSettings = new TimeoutSettings(this);
            this.ProxySettings = new ProxySettings() { Proxy = HTTPManager.Proxy };
            this.RetrySettings = new RetrySettings(methodType == HTTPMethods.Get ? 1 : 0);

            this.Callback = callback;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Just because cookies are enabled, it doesn't justify creating XHR with WithCredentials == 1.
            //this.WithCredentials = this.CookieSettings.IsCookiesEnabled;
#endif

            this.Context = new LoggingContext(this);
            this.Timing = new TimingCollector(this);

            this.CancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        /// <summary>
        /// Adds a header-value pair to the Headers. Use it to add custom headers to the request.
        /// </summary>
        /// <example>AddHeader("User-Agent', "FooBar 1.0")</example>
        public void AddHeader(string name, string value) => this.Headers = Headers.AddHeader(name, value);

        /// <summary>
        /// For the given header name, removes any previously added values and sets the given one.
        /// </summary>
        public void SetHeader(string name, string value) => this.Headers = this.Headers.SetHeader(name, value);

        /// <summary>
        /// Removes the specified header and all of its associated values. Returns <c>true</c>, if the header found and succesfully removed.
        /// </summary>
        public bool RemoveHeader(string name) => Headers.RemoveHeader(name);

        /// <summary>
        /// Returns <c>true</c> if the given head name is already in the <see cref="Headers"/>.
        /// </summary>
        public bool HasHeader(string name) => Headers.HasHeader(name);

        /// <summary>
        /// Returns the first header or <c>null</c> for the given header name.
        /// </summary>
        public string GetFirstHeaderValue(string name) => Headers.GetFirstHeaderValue(name);

        /// <summary>
        /// Returns all header values for the given header or <c>null</c>.
        /// </summary>
        public List<string> GetHeaderValues(string name) => Headers.GetHeaderValues(name);

        /// <summary>
        /// Removes all headers.
        /// </summary>
        public void RemoveHeaders() => Headers.RemoveHeaders();

        /// <summary>
        /// Sets the Range header to download the content from the given byte position. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.35
        /// </summary>
        /// <param name="firstBytePos">Start position of the download.</param>
        public void SetRangeHeader(long firstBytePos)
        {
            SetHeader("Range", string.Format("bytes={0}-", firstBytePos));
        }

        /// <summary>
        /// Sets the Range header to download the content from the given byte position to the given last position. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.35
        /// </summary>
        /// <param name="firstBytePos">Start position of the download.</param>
        /// <param name="lastBytePos">The end position of the download.</param>
        public void SetRangeHeader(long firstBytePos, long lastBytePos)
        {
            SetHeader("Range", string.Format("bytes={0}-{1}", firstBytePos, lastBytePos));
        }

        internal void RemoveUnsafeHeaders()
        {
            // https://www.rfc-editor.org/rfc/rfc9110.html#name-redirection-3xx
            /* 2. Remove header fields that were automatically generated by the implementation, replacing them with updated values as appropriate to the new request. This includes:
                1. Connection-specific header fields (see Section 7.6.1),
                2. Header fields specific to the client's proxy configuration, including (but not limited to) Proxy-Authorization,
                3. Origin-specific header fields (if any), including (but not limited to) Host,
                4. Validating header fields that were added by the implementation's cache (e.g., If-None-Match, If-Modified-Since), and
                5. Resource-specific header fields, including (but not limited to) Referer, Origin, Authorization, and Cookie.
               3. Consider removing header fields that were not automatically generated by the implementation
                    (i.e., those present in the request because they were added by the calling context) where there are security implications;
                    this includes but is not limited to Authorization and Cookie.
             * */

            // 2.1
            RemoveHeader("Connection");
            RemoveHeader("Proxy-Connection");
            RemoveHeader("Keep-Alive");
            RemoveHeader("TE");
            RemoveHeader("Transfer-Encoding");
            RemoveHeader("Upgrade");

            // 2.2
            RemoveHeader("Proxy-Authorization");

            // 2.3
            RemoveHeader("Host");

            // 2.4
            RemoveHeader("If-None-Match");
            RemoveHeader("If-Modified-Since");

            // 2.5 & 3
            RemoveHeader("Referer");
            RemoveHeader("Origin");
            RemoveHeader("Authorization");
            RemoveHeader("Cookie");

            RemoveHeader("Accept-Encoding");
            
            RemoveHeader("Content-Length");
        }

        internal void Prepare()
        {
            // Upload settings
            this.UploadSettings?.SetupRequest(this, true);
        }

        internal void EnumerateHeaders(OnHeaderEnumerationDelegate callback, bool callBeforeSendCallback)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!HasHeader("Host"))
            {
                if (CurrentUri.Port == 80 || CurrentUri.Port == 443)
                    SetHeader("Host", CurrentUri.Host);
                else
                    SetHeader("Host", CurrentUri.Authority);
            }

            DecompressorFactory.SetupHeaders(this);

            if (!DownloadSettings.DisableCache)
                HTTPManager.LocalCache?.SetupValidationHeaders(this);

            var hostSettings = HTTPManager.PerHostSettings.Get(this.CurrentUri.Host);

            // Websocket would be very, very sad if its "connection: upgrade" header would be overwritten!
            if (!HasHeader("Connection"))
                AddHeader("Connection", hostSettings.HTTP1ConnectionSettings.TryToReuseConnections ? "Keep-Alive, TE" : "Close, TE");

            if (hostSettings.HTTP1ConnectionSettings.TryToReuseConnections /*&& !HasHeader("Keep-Alive")*/)
            {
                // Send the server a slightly larger value to make sure it's not going to close sooner than the client
                int seconds = (int)Math.Ceiling(hostSettings.HTTP1ConnectionSettings.MaxConnectionIdleTime.TotalSeconds + 1);

                AddHeader("Keep-Alive", "timeout=" + seconds);
            }

            if (!HasHeader("TE"))
                AddHeader("TE", "identity");

            if (!string.IsNullOrEmpty(HTTPManager.UserAgent) && !HasHeader("User-Agent"))
                AddHeader("User-Agent", HTTPManager.UserAgent);
#endif
            long contentLength = -1;

            if (this.UploadSettings.UploadStream == null)
            {
                contentLength = 0;
            }
            else
            {
                contentLength = this.UploadSettings.UploadStream.Length;

                if (contentLength == BodyLengths.UnknownWithChunkedTransferEncoding)
                    SetHeader("Transfer-Encoding", "chunked");

                if (!HasHeader("Content-Type"))
                    SetHeader("Content-Type", "application/octet-stream");
            }

            // Always set the Content-Length header if possible
            // http://tools.ietf.org/html/rfc2616#section-4.4 : For compatibility with HTTP/1.0 applications, HTTP/1.1 requests containing a message-body MUST include a valid Content-Length header field unless the server is known to be HTTP/1.1 compliant.
            // 2018.06.03: Changed the condition so that content-length header will be included for zero length too.
            // 2022.05.25: Don't send a Content-Length (: 0) header if there's an Upgrade header. Upgrade is set for websocket, and it might be not true that the client doesn't send any bytes.
            if (contentLength >= BodyLengths.NoBody && !HasHeader("Content-Length") && !HasHeader("Upgrade"))
                SetHeader("Content-Length", contentLength.ToString());

            // Server authentication
            this.Authenticator?.SetupRequest(this);

            // Cookies
            //this.CookieSettings?.SetupRequest(this);
            CookieJar.SetupRequest(this);

            // Write out the headers to the stream
            if (callback != null && this.Headers != null)
                foreach (var kvp in this.Headers)
                    callback(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Starts processing the request.
        /// </summary>
        public HTTPRequest Send()
        {
            // TODO: Are we really want to 'reset' the token source? Two problems i see:
            //  1.) User code will not know about this change
            //  2.) We might dispose the source while the DNS and TCP queries are running and checking the source request's Token.
            //if (this.IsRedirected)
            //{
            //    this.CancellationTokenSource?.Dispose();
            //    this.CancellationTokenSource = new System.Threading.CancellationTokenSource();
            //}
            this.Exception = null;

            return HTTPManager.SendRequest(this);
        }

        /// <summary>
        /// Cancels any further processing of the HTTP request.
        /// </summary>
        public void Abort()
        {
            HTTPManager.Logger.Verbose("HTTPRequest", $"Abort({this.State})", this.Context);

            if (this.State >= HTTPRequestStates.Finished)
                return;

            //this.IsCancellationRequested = true;
            this.CancellationTokenSource.Cancel();

            // There's a race-condition here too, another thread might set it too.
            //  In this case, both state going to be queued up that we have to handle in RequestEvents.cs.
            if (this.TimeoutSettings.IsTimedOut(HTTPManager.CurrentFrameDateTime))
            {
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this, this.TimeoutSettings.IsConnectTimedOut(HTTPManager.CurrentFrameDateTime) ? HTTPRequestStates.ConnectionTimedOut : HTTPRequestStates.TimedOut, null));
            }
            else
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this, HTTPRequestStates.Aborted, null));

            if (this.OnCancellationRequested != null)
            {
                try
                {
                    this.OnCancellationRequested(this);
                }
                catch { }
            }
        }

        /// <summary>
        /// Resets the request for a state where switching MethodType is possible.
        /// </summary>
        public void Clear()
        {
            RemoveHeaders();

            this.RedirectSettings.Reset();
            this.Exception = null;
            this.CancellationTokenSource?.Dispose();
            this.CancellationTokenSource = new System.Threading.CancellationTokenSource();

            this.UploadSettings?.Dispose();
        }

        #region System.Collections.IEnumerator implementation

        /// <summary>
        /// <see cref="IEnumerator.Current"/> implementation, required for <see cref="UnityEngine.Coroutine"/> support.
        /// </summary>
        public object Current { get { return null; } }

        /// <summary>
        /// <see cref="IEnumerator.MoveNext"/> implementation, required for <see cref="UnityEngine.Coroutine"/> support.
        /// </summary>
        /// <returns><c>true</c> if the request isn't finished yet.</returns>
        public bool MoveNext() => this.State < HTTPRequestStates.Finished;

        /// <summary>
        /// <see cref="IEnumerator.MoveNext"/> implementation throwing <see cref="NotImplementedException"/>, required for <see cref="UnityEngine.Coroutine"/> support.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Reset() => throw new NotImplementedException();

        #endregion

        /// <summary>
        /// Disposes of resources used by the HTTPRequest instance.
        /// </summary>
        public void Dispose()
        {
            this.UploadSettings?.Dispose();
            this.Response?.Dispose();

            this.CancellationTokenSource?.Dispose();
            this.CancellationTokenSource = null;
        }

        public override string ToString()
        {
            return $"[HTTPRequest {this.State}, {this.Context.Hash}, {this.CurrentUri}, {this.CurrentHostKey}]";
        }
    }
}
