namespace Best.HTTP
{
    /// <summary>
    /// Represents an HTTP range that specifies the byte range of a response content, received as an answer for a range-request.
    /// </summary>
    public sealed class HTTPRange
    {
        /// <summary>
        /// Gets the position of the first byte in the range that the server sent.
        /// </summary>
        public long FirstBytePos { get; private set; }

        /// <summary>
        /// Gets the position of the last byte in the range that the server sent.
        /// </summary>
        public long LastBytePos { get; private set; }

        /// <summary>
        /// Gets the total length of the full entity-body on the server. Returns -1 if this length is unknown or difficult to determine.
        /// </summary>
        public long ContentLength { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the HTTP range is valid.
        /// </summary>
        public bool IsValid { get; private set; }

        internal HTTPRange()
        {
            this.ContentLength = -1;
            this.IsValid = false;
        }

        internal HTTPRange(int contentLength)
        {
            this.ContentLength = contentLength;
            this.IsValid = false;
        }

        internal HTTPRange(long firstBytePosition, long lastBytePosition, long contentLength)
        {
            this.FirstBytePos = firstBytePosition;
            this.LastBytePos = lastBytePosition;
            this.ContentLength = contentLength;

            // A byte-content-range-spec with a byte-range-resp-spec whose last-byte-pos value is less than its first-byte-pos value, or whose instance-length value is less than or equal to its last-byte-pos value, is invalid.
            this.IsValid = this.FirstBytePos <= this.LastBytePos && this.ContentLength > this.LastBytePos;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}/{2} (valid: {3})", FirstBytePos, LastBytePos, ContentLength, IsValid);
        }
    }
}
