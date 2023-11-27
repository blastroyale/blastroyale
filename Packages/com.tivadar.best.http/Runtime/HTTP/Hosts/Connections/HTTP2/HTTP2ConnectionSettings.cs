#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2
using System;

namespace Best.HTTP.Hosts.Connections.HTTP2
{
    /// <summary>
    /// Settings for HTTP/2 connections when the Connect protocol is available.
    /// </summary>
    public sealed class WebSocketOverHTTP2Settings
    {
        /// <summary>
        /// Set it to false to disable Websocket Over HTTP/2 (RFC 8441). It's true by default.
        /// </summary>
        public bool EnableWebSocketOverHTTP2 { get; set; } = true;

        /// <summary>
        /// Set it to disable fallback logic from the Websocket Over HTTP/2 implementation to the 'old' HTTP/1 implementation when it fails to connect.
        /// </summary>
        public bool EnableImplementationFallback { get; set; } = true;
    }

    /// <summary>
    /// Settings for HTTP/2 connections.
    /// </summary>
    public sealed class HTTP2ConnectionSettings
    {
        /// <summary>
        /// When set to false, the plugin will not try to use HTTP/2 connections.
        /// </summary>
        public bool EnableHTTP2Connections = true;

        /// <summary>
        /// Maximum size of the HPACK header table.
        /// </summary>
        public UInt32 HeaderTableSize = 4096; // Spec default: 4096

        /// <summary>
        /// Maximum concurrent http2 stream on http2 connection will allow. Its default value is 128;
        /// </summary>
        public UInt32 MaxConcurrentStreams = 128; // Spec default: not defined

        /// <summary>
        /// Initial window size of a http2 stream. Its default value is 65535, can be controlled through the HTTPRequest's DownloadSettings object.
        /// </summary>
        public UInt32 InitialStreamWindowSize = UInt16.MaxValue; // Spec default: 65535

        /// <summary>
        /// Global window size of a http/2 connection. Its default value is the maximum possible value on 31 bits.
        /// </summary>
        public UInt32 InitialConnectionWindowSize = HTTP2ContentConsumer.MaxValueFor31Bits; // Spec default: 65535

        /// <summary>
        /// Maximum size of a http2 frame.
        /// </summary>
        public UInt32 MaxFrameSize = 16384; // 16384 spec def.

        /// <summary>
        /// Not used.
        /// </summary>
        public UInt32 MaxHeaderListSize = UInt32.MaxValue; // Spec default: infinite

        /// <summary>
        /// With HTTP/2 only one connection will be open so we can keep it open longer as we hope it will be reused more.
        /// </summary>
        public TimeSpan MaxIdleTime = TimeSpan.FromSeconds(120);

        /// <summary>
        /// Time between two ping messages.
        /// </summary>
        public TimeSpan PingFrequency = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Timeout to receive a ping acknowledgement from the server. If no ack reveived in this time the connection will be treated as broken.
        /// </summary>
        public TimeSpan Timeout = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Set to true to enable RFC 8441 "Bootstrapping WebSockets with HTTP/2" (https://tools.ietf.org/html/rfc8441).
        /// </summary>
        public bool EnableConnectProtocol = false;

        /// <summary>
        /// Settings for WebSockets over HTTP/2 (RFC 8441)
        /// </summary>
        public WebSocketOverHTTP2Settings WebSocketOverHTTP2Settings = new WebSocketOverHTTP2Settings();
    }
}
#endif
