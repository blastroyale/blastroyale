using System;
using System.IO;

namespace Best.HTTP.Hosts.Connections
{
    public enum SupportedProtocols
    {
        Unknown,
        HTTP,

        WebSocket,

        ServerSentEvents
    }

    public static class HTTPProtocolFactory
    {
        public const string W3C_HTTP1 = "http/1.1";
#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2
        public const string W3C_HTTP2 = "h2";
#endif

        public static SupportedProtocols GetProtocolFromUri(Uri uri)
        {
            if (uri == null || uri.Scheme == null)
                throw new Exception("Malformed URI in GetProtocolFromUri");

            string scheme = uri.Scheme.ToLowerInvariant();
            switch (scheme)
            {
                case "ws":
                case "wss":
                    return SupportedProtocols.WebSocket;

                default:
                    return SupportedProtocols.HTTP;
            }
        }

        public static bool IsSecureProtocol(Uri uri)
        {
            if (uri == null || uri.Scheme == null)
                throw new Exception("Malformed URI in IsSecureProtocol");

            string scheme = uri.Scheme.ToLowerInvariant();
            switch (scheme)
            {
                // http
                case "https":

                // WebSocket
                case "wss":
                    return true;
            }

            return false;
        }
    }
}
