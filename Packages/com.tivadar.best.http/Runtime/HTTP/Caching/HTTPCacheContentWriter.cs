using System;
using System.IO;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

using UnityEngine;

namespace Best.HTTP.Caching
{
    /// <summary>
    /// Represents a writer for caching HTTP response content.
    /// </summary>
    public class HTTPCacheContentWriter
    {
        /// <summary>
        /// Gets the parent HTTPCache instance associated with this content writer.
        /// </summary>
        public HTTPCache Cache { get; private set; }

        /// <summary>
        /// Hash identifying the resource. If <see cref="Write(BufferSegment)"/> fails, it becomes an invalid one.
        /// </summary>
        public Hash128 Hash { get; private set; }

        /// <summary>
        /// Expected length of the content. Has a non-zero value only when the server is sending a "content-length" header.
        /// </summary>
        public ulong ExpectedLength { get; private set; }

        /// <summary>
        /// Number of bytes written to the cache.
        /// </summary>
        public ulong ProcessedLength { get; private set; }

        /// <summary>
        /// Context of this cache writer used for logging.
        /// </summary>
        public LoggingContext Context { get; private set; }

        /// <summary>
        /// Underlying stream the download bytes are written into.
        /// </summary>
        private Stream _contentStream;

        internal HTTPCacheContentWriter(HTTPCache cache, Hash128 hash, Stream contentStream, ulong expectedLength, LoggingContext loggingContext)
        {
            this.Cache = cache;
            this.Hash = hash;
            this._contentStream = contentStream;
            this.ExpectedLength = expectedLength;
            this.Context = loggingContext;
        }

        /// <summary>
        /// Writes content to the underlying stream. 
        /// </summary>
        /// <param name="segment"><see cref="BufferSegment"/> holding a reference to the data and containing information about the offset and count of the valid range of data.</param>
        public void Write(BufferSegment segment)
        {
            if (!this.Hash.isValid)
                return;

            try
            {
                this._contentStream?.Write(segment.Data, segment.Offset, segment.Count);
                this.ProcessedLength += (ulong)segment.Count;

                if (this.ProcessedLength > this.ExpectedLength)
                {
                    if (!this.Cache.IsThereEnoughSpaceAfterMaintain(this.ProcessedLength, this.Context))
                    {
                        HTTPManager.Logger.Information(nameof(HTTPCacheContentWriter), $"Not enough space({this.ProcessedLength:N0}) in cache({this.Cache.CacheSize:N0}), even after Maintain!", this.Context);

                        this.Cache?.EndCache(this, false, this.Context);
                    }
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Warning(nameof(HTTPCacheContentWriter), $"{nameof(Write)}({segment}): {ex}", this.Context);

                // EndCache will call Close, we don't have to in this catch block
                this.Cache?.EndCache(this, false, this.Context);
            }
        }

        /// <summary>
        /// Close the underlying stream and invalidate the hash.
        /// </summary>
        internal void Close()
        {
            this._contentStream?.Close();
            this._contentStream = null;

            // this will set an invalid cache, further HTTPCaching.EndCache calls will return early.
            this.Hash = new Hash128();
        }

        public override string ToString() => $"[{nameof(HTTPCacheContentWriter)} {Hash} {ProcessedLength:N0}]";
    }
}
