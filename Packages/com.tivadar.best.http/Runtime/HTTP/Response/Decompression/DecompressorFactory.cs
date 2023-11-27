using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Text;

namespace Best.HTTP.Response.Decompression
{
    public static class DecompressorFactory
    {
        public const int MinLengthToDecompress = 256;

        // cached header value
        private static string AcceptEncoding = null;

        public static void SetupHeaders(HTTPRequest request)
        {
            if (!request.HasHeader("Accept-Encoding"))
            {
                if (AcceptEncoding == null)
                {
                    var sb = StringBuilderPool.Get(4);

                    if (BrotliDecompressor.IsSupported())
                        sb.Append("br, ");

                    if (GZipDecompressor.IsSupported)
                        sb.Append("gzip, ");

                    if (DeflateDecompressor.IsSupported)
                        sb.Append("deflate, ");

                    sb.Append("identity");

                    AcceptEncoding = StringBuilderPool.ReleaseAndGrab(sb);
                }

                request.AddHeader("Accept-Encoding", AcceptEncoding);
            }
        }

        public static IDecompressor GetDecompressor(string encoding, LoggingContext context)
        {
            if (encoding == null)
                return null;

            switch (encoding.ToLowerInvariant())
            {
                case "identity":
                case "utf-8":
                    break;

                case "gzip": return new GZipDecompressor(MinLengthToDecompress);

                case "deflate": return new DeflateDecompressor(MinLengthToDecompress);

                case "br":
                    if (BrotliDecompressor.IsSupported())
                        return new BrotliDecompressor(MinLengthToDecompress);
                    else
                        goto default;

                default:
                    HTTPManager.Logger.Warning("DecompressorFactory", "GetDecompressor - unsupported encoding: " + encoding, context);
                    break;
            }

            return null;
        }
    }
}
