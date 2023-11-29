namespace Best.HTTP.Request.Upload
{
    /// <summary>
    /// Provides constants representing different, special body lengths for HTTP requests with upload streams.
    /// </summary>
    public static class BodyLengths
    {
        /// <summary>
        /// The <see cref="UploadStreamBase"/>'s length is unknown and the plugin have to send data with '<c>chunked</c>' transfer-encoding.
        /// </summary>
        public const long UnknownWithChunkedTransferEncoding = -2;

        /// <summary>
        /// The <see cref="UploadStreamBase"/>'s length is unknown and the plugin have to send data as-is, without any encoding.
        /// </summary>
        public const long UnknownRaw = -1;

        /// <summary>
        /// No content to send.
        /// </summary>
        public const long NoBody = 0;
    }
}
