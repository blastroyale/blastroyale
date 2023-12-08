using System;
using System.Text;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Settings;
using Best.HTTP.Shared;
using Best.HTTP.Hosts.Connections;
using Best.WebSockets.Implementations;

#if !UNITY_WEBGL || UNITY_EDITOR
using Best.HTTP.Hosts.Connections.HTTP2;
using Best.WebSockets.Extensions;
#endif

using Best.HTTP.Shared.Compression.Zlib;

/*
 * This is a wrapper class organizing where and how the websocket request is sent out. If there's an already open HTTP/2 connection with
 * an ENABLE_CONNECT_PROTOCOL flag, it tries using the HTTP/2 connection first and if it fails, a new HTTP/1 connection is created.
 * */

namespace Best.WebSockets
{
    /// <summary>
    /// Implements the WebSocket standard for duplex, two-way communications.
    /// </summary>
    public sealed class WebSocket
    {
        /// <summary>
        /// Maximum payload size of a websocket frame. Its default value is 32 KiB.
        /// </summary>
        public static uint MaxFragmentSize = UInt16.MaxValue / 2;

#if !UNITY_WEBGL || UNITY_EDITOR
        public static IExtension[] GetDefaultExtensions()
        {
            return new IExtension[] { new PerMessageCompression(/*compression level: */        CompressionLevel.Default,
                                                             /*clientNoContextTakeover: */     false,
                                                             /*serverNoContextTakeover: */     false,
                                                             /*clientMaxWindowBits: */         ZlibConstants.WindowBitsMax,
                                                             /*desiredServerMaxWindowBits: */  ZlibConstants.WindowBitsMax,
                                                             /*minDatalengthToCompress: */     PerMessageCompression.MinDataLengthToCompressDefault) };
        }
#endif

        public WebSocketStates State { get { return this.implementation.State; } }

        /// <summary>
        /// The connection to the WebSocket server is open.
        /// </summary>
        public bool IsOpen { get { return this.implementation.IsOpen; } }

        /// <summary>
        /// Data waiting to be written to the wire.
        /// </summary>
        public int BufferedAmount { get { return this.implementation.BufferedAmount; } }

#if !UNITY_WEBGL || UNITY_EDITOR

        /// <summary>
        /// Set to <c>true</c> to start sending Ping frames to the WebSocket server.
        /// </summary>
        public bool SendPings { get; set; }

        /// <summary>
        /// The delay between two Pings in milliseconds. Minimum value is 100ms, default is 10 seconds.
        /// </summary>
        public TimeSpan PingFrequency { get; set; }

        /// <summary>
        /// If <see cref="SendPings"/> set to <c>true</c>, the plugin will close the connection and emit an <see cref="OnClosed"/> event if no
        /// message is received from the server in the given time. Its default value is 2 sec.
        /// </summary>
        public TimeSpan CloseAfterNoMessage { get; set; }

        /// <summary>
        /// The internal <see cref="Best.HTTP.HTTPRequest"/> object.
        /// </summary>
        public HTTP.HTTPRequest InternalRequest { get { return this.implementation.InternalRequest; } }

        /// <summary>
        /// <see cref="IExtension"/> implementations the plugin will negotiate with the server to use.
        /// </summary>
        public IExtension[] Extensions { get; private set; }

        /// <summary>
        /// Latency calculated from ping-pong message round-trip times.
        /// </summary>
        public int Latency { get { return this.implementation.Latency; } }

        /// <summary>
        /// When the WebSocket instance received the last message from the server.
        /// </summary>
        public DateTime LastMessageReceived { get { return this.implementation.LastMessageReceived; } }

        /// <summary>
        /// When the <c>Websocket Over HTTP/2</c> implementation fails to connect and <see cref="WebSocketOverHTTP2Settings"/><c>.</c><see cref="WebSocketOverHTTP2Settings.EnableImplementationFallback"/> is <c>true</c>, the plugin tries to fall back to the HTTP/1 implementation.
        /// When this happens a new <see cref="InternalRequest"/> is created and all previous custom modifications (like added headers) are lost. With OnInternalRequestCreated these modifications can be reapplied.
        /// </summary>
        public Action<WebSocket, HTTP.HTTPRequest> OnInternalRequestCreated;
#endif

        /// <summary>
        /// Called when the connection to the WebSocket server is established.
        /// </summary>
        public OnWebSocketOpenDelegate OnOpen;

        /// <summary>
        /// Called when a new textual message is received from the server.
        /// </summary>
        public OnWebSocketMessageDelegate OnMessage;

        /// <summary>
        /// Called when a Binary message received. 
        /// The content of the <see cref="BufferSegment"/> must be used or copied to a new array in the callbacks because the plugin reuses the memory immediately after the callback by placing it back to the <see cref="BufferPool"/>!
        /// </summary>
        /// <remarks>Note that the memory will be reused when this event returns. Either process it in this call or make a copy from the received data.</remarks>
        public OnWebSocketBinaryNoAllocDelegate OnBinary;

        /// <summary>
        /// Called when the WebSocket connection is closed.
        /// </summary>
        public OnWebSocketClosedDelegate OnClosed;

        /// <summary>
        /// Logging context of this websocket instance.
        /// </summary>
        public LoggingContext Context { get; private set; }

        /// <summary>
        /// The underlying, real implementation.
        /// </summary>
        private WebSocketBaseImplementation implementation;

        /// <summary>
        /// Creates a WebSocket instance from the given uri.
        /// </summary>
        /// <param name="uri">The uri of the WebSocket server</param>
        public WebSocket(Uri uri)
            :this(uri, string.Empty, string.Empty)
        {
#if (!UNITY_WEBGL || UNITY_EDITOR)
            this.Extensions = WebSocket.GetDefaultExtensions();
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        /// <summary>
        /// Creates a WebSocket instance from the given uri.
        /// </summary>
        /// <param name="uri">Uri of the WebSocket endpoint.</param>
        /// <param name="origin">Where the WebSocket originating from.</param>
        /// <param name="protocol">The application-level protocol that the client want to use(eg. "chat", "leaderboard", etc.). Can be null or empty string if not used.</param>
        public WebSocket(Uri uri, string origin, string protocol)
            :this(uri, origin, protocol, null)
        {
#if (!UNITY_WEBGL || UNITY_EDITOR)
            this.Extensions = WebSocket.GetDefaultExtensions();
#endif
        }
#endif

        /// <summary>
        /// Creates a WebSocket instance from the given uri, protocol and origin.
        /// </summary>
        /// <param name="uri">The uri of the WebSocket server</param>
        /// <param name="origin">Servers that are not intended to process input from any web page but only for certain sites SHOULD verify the |Origin| field is an origin they expect.
        /// If the origin indicated is unacceptable to the server, then it SHOULD respond to the WebSocket handshake with a reply containing HTTP 403 Forbidden status code.</param>
        /// <param name="protocol">The application-level protocol that the client want to use(eg. "chat", "leaderboard", etc.). Can be null or empty string if not used.</param>
        /// <param name="extensions">Optional <see cref="IExtension"/> implementations</param>
        public WebSocket(Uri uri, string origin, string protocol
#if !UNITY_WEBGL || UNITY_EDITOR
            , params IExtension[] extensions
#endif
            )

        {
            this.Context = new LoggingContext(this);

#if !UNITY_WEBGL || UNITY_EDITOR
            this.Extensions = extensions;
#endif

            SelectImplementation(uri, origin, protocol);

            // Under WebGL when only the WebSocket protocol is used Setup() isn't called, so we have to call it here.
            HTTPManager.Setup();
        }

        internal WebSocketBaseImplementation SelectImplementation(Uri uri, string origin, string protocol)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
#if !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2
            
            if (HTTPProtocolFactory.IsSecureProtocol(uri) &&
                HTTPManager.PerHostSettings.Get(uri).HTTP2ConnectionSettings.WebSocketOverHTTP2Settings.EnableWebSocketOverHTTP2)
            {
                // Try to find a HTTP/2 connection that supports the connect protocol.
                var connectionKey = HostKey.From(new UriBuilder("https", uri.Host, uri.Port).Uri, GetProxy(uri));

                var con = HostManager.GetHostVariant(connectionKey).Find(c => {
                    var httpConnection = c as HTTPOverTCPConnection;
                    var http2Handler = httpConnection?.requestHandler as HTTP2ContentConsumer;

                    return http2Handler != null && http2Handler.settings.RemoteSettings[HTTP2Settings.ENABLE_CONNECT_PROTOCOL] != 0;
                });

                if (con != null)
                {
                    HTTPManager.Logger.Information("WebSocket", "Connection with enabled Connect Protocol found!", this.Context);

                    var httpConnection = con as HTTPOverTCPConnection;
                    var http2Handler = httpConnection?.requestHandler as HTTP2ContentConsumer;

                    this.implementation = new OverHTTP2(this, uri, origin, protocol);
                }
            }
#endif
            if (this.implementation == null)
                this.implementation = new OverHTTP1(this, uri, origin, protocol);
#else
            this.implementation = new WebGLBrowser(this, uri, origin, protocol);
#endif

            return this.implementation;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        internal void FallbackToHTTP1()
        {
            HTTPManager.Logger.Verbose("WebSocket", "FallbackToHTTP1", this.Context);

            if (this.implementation == null)
                return;

            this.implementation = new OverHTTP1(this, this.implementation.Uri, this.implementation.Origin, this.implementation.Protocol);
            this.implementation.StartOpen();
        }
#endif

        /// <summary>
        /// Start the opening process.
        /// </summary>
        /// <remarks>It's a non-blocking call. To get notified when the WebSocket instance is considered open and can send/receive, use the <see cref="OnOpen"/> event.</remarks>
        public void Open()
        {
            this.implementation.StartOpen();
        }

        /// <summary>
        /// It will send the given textual message to the remote server.
        /// </summary>
        public void Send(string message)
        {
            if (!IsOpen)
                return;

            this.implementation.Send(message);
        }

        /// <summary>
        /// It will send the given binary message to the remote server.
        /// </summary>
        public void Send(byte[] buffer)
        {
            if (!IsOpen)
                return;

            this.implementation.Send(buffer);
        }

        /// <summary>
        /// It will send the given binary message to the remote server.
        /// </summary>
        public void Send(byte[] buffer, ulong offset, ulong count)
        {
            if (!IsOpen)
                return;

            this.implementation.Send(buffer, offset, count);
        }

        /// <summary>
        /// Will send the data in one or more binary frame and takes ownership over it calling BufferPool.Release when the data sent.
        /// </summary>
        public void SendAsBinary(BufferSegment data)
        {
            if (!IsOpen)
            {
                BufferPool.Release(data);
                return;
            }

            this.implementation.SendAsBinary(data);
        }

        /// <summary>
        /// Will send data as a text frame and takes owenership over the memory region releasing it to the BufferPool as soon as possible.
        /// </summary>
        public void SendAsText(BufferSegment data)
        {
            if (!IsOpen)
            {
                BufferPool.Release(data);
                return;
            }

            this.implementation.SendAsText(data);
        }

        /// <summary>
        /// It will initiate the closing of the connection to the server.
        /// </summary>
        public void Close()
        {
            if (State >= WebSocketStates.Closing)
                return;

            this.implementation.StartClose(WebSocketStatusCodes.NormalClosure, "Bye!");
        }

        /// <summary>
        /// It will initiate the closing of the connection to the server sending the given code and message.
        /// </summary>
        public void Close(WebSocketStatusCodes code, string message)
        {
            if (!IsOpen)
                return;

            this.implementation.StartClose(code, message);
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        internal ProxySettings GetProxy(Uri uri)
        {
            // WebSocket is not a request-response based protocol, so we need a 'tunnel' through the proxy
            var proxy = HTTPManager.Proxy as HTTP.Proxies.HTTPProxy;
            if (proxy != null && proxy.UseProxyForAddress(uri))
                proxy = new HTTP.Proxies.HTTPProxy(proxy.Address,
                                      proxy.Credentials,
                                      false, /*turn on 'tunneling'*/
                                      false, /*sendWholeUri*/
                                      proxy.NonTransparentForHTTPS);

            return new ProxySettings { Proxy = proxy };
        }

        internal void DisposeExtensions()
        {
            if (this.Extensions != null)
            {
                for (int i = 0; i < this.Extensions.Length; ++i)
                {
                    var ext = this.Extensions[i];

                    try
                    {
                        ext?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("OverHTTP1", "OnInternalRequestCallback - extension dispose", ex, this.Context);
                    }

                    this.Extensions[i] = null;
                }

                this.Extensions = null;
            }
        }
#endif

#if !UNITY_WEBGL || UNITY_EDITOR

        internal static BufferSegment EncodeCloseData(WebSocketStatusCodes code, string message)
        {
            //If there is a body, the first two bytes of the body MUST be a 2-byte unsigned integer
            // (in network byte order) representing a status code with value /code/ defined in Section 7.4 (http://tools.ietf.org/html/rfc6455#section-7.4). Following the 2-byte integer,
            // the body MAY contain UTF-8-encoded data with value /reason/, the interpretation of which is not defined by this specification.
            // This data is not necessarily human readable but may be useful for debugging or passing information relevant to the script that opened the connection.
            int msgLen = Encoding.UTF8.GetByteCount(message);
            using (var ms = new BufferPoolMemoryStream(2 + msgLen))
            {
                byte[] buff = BitConverter.GetBytes((ushort)code);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buff, 0, buff.Length);

                ms.Write(buff, 0, buff.Length);

                buff = Encoding.UTF8.GetBytes(message);
                ms.Write(buff, 0, buff.Length);

                buff = ms.ToArray();

                return buff.AsBuffer(buff.Length);
            }
        }

        internal static string GetSecKey(object[] from)
        {
            const int keysLength = 16;
            byte[] keys = BufferPool.Get(keysLength, true);
            int pos = 0;

            for (int i = 0; i < from.Length; ++i)
            {
                byte[] hash = BitConverter.GetBytes((Int32)from[i].GetHashCode());

                for (int cv = 0; cv < hash.Length && pos < keysLength; ++cv)
                    keys[pos++] = hash[cv];
            }

            var result = Convert.ToBase64String(keys, 0, keysLength);
            BufferPool.Release(keys);

            return result;
        }
#endif
    }
}