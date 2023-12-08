#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2

using System;
using System.Collections.Generic;

using Best.HTTP.Response;
using Best.HTTP.Response.Decompression;
using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Hosts.Connections.HTTP2
{
    public sealed class HTTP2Response : HTTPResponse
    {
        // For progress report
        public long ExpectedContentLength { get; private set; }
        public bool HasContentEncoding { get => !string.IsNullOrEmpty(this.contentEncoding); }

        private string contentEncoding = null;

        bool isPrepared;
        private IDecompressor _decompressor;

        public HTTP2Response(HTTPRequest request, bool isFromCache)
            : base(request, isFromCache)
        {
            this.HTTPVersion = new Version(2, 0);
        }

        internal void AddHeaders(List<KeyValuePair<string, string>> headers)
        {
            this.ExpectedContentLength = -1;
            Dictionary<string, List<string>> newHeaders = this.Request.DownloadSettings.OnHeadersReceived != null ? new Dictionary<string, List<string>>() : null;

            for (int i = 0; i < headers.Count; ++i)
            {
                KeyValuePair<string, string> header = headers[i];

                if (header.Key.Equals(":status", StringComparison.Ordinal))
                {
                    base.StatusCode = int.Parse(header.Value);
                    base.Message = string.Empty;
                }
                else
                {
                    if (!this.HasContentEncoding && header.Key.Equals("content-encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        this.contentEncoding = header.Value;
                    }
                    else if (base.Request.DownloadSettings.OnDownloadProgress != null && header.Key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    {
                        long contentLength;
                        if (long.TryParse(header.Value, out contentLength))
                            this.ExpectedContentLength = contentLength;
                        else
                            HTTPManager.Logger.Information("HTTP2Response", string.Format("AddHeaders - Can't parse Content-Length as an int: '{0}'", header.Value), this.Context);
                    }

                    base.AddHeader(header.Key, header.Value);
                }

                if (newHeaders != null)
                {
                    List<string> values;
                    if (!newHeaders.TryGetValue(header.Key, out values))
                        newHeaders.Add(header.Key, values = new List<string>(1));

                    values.Add(header.Value);
                }
            }

            if (this.ExpectedContentLength == -1 && base.Request.DownloadSettings.OnDownloadProgress != null)
                HTTPManager.Logger.Information("HTTP2Response", "AddHeaders - No Content-Length header found!", this.Context);

            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, newHeaders));
        }

        internal void Prepare(IDownloadContentBufferAvailable bufferAvailable)
        {
            if (!this.isPrepared)
            {
                this.isPrepared = true;

                CreateDownloadStream(bufferAvailable);

                base.BeginReceiveContent();
            }
        }
               
        internal void ProcessData(BufferSegment payload)
        {
            if (this.HasContentEncoding)
            {
                if (this._decompressor == null)
                    this._decompressor = DecompressorFactory.GetDecompressor(this.contentEncoding, this.Context);

                BufferSegment result = BufferSegment.Empty;
                bool release;
                try
                {
                    (result, release) = this._decompressor.Decompress(payload, false, true, this.Context);
                }
                catch
                {
                    BufferPool.Release(payload);
                    throw;
                }
                if (release)
                    BufferPool.Release(payload);

                base.FeedDownloadedContentChunk(result);
            }
            else
                base.FeedDownloadedContentChunk(payload);
        }

        internal void FinishProcessData()
        {
            if (this._decompressor != null)
            {
                var (decompressed, _) = this._decompressor.Decompress(BufferSegment.Empty, true, true, this.Context);
                if (decompressed != BufferSegment.Empty)
                    base.FeedDownloadedContentChunk(decompressed);

                this._decompressor.Dispose();
                this._decompressor = null;
            }

            base.FinishedContentReceiving();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (this._decompressor != null)
                {
                    // In some cases, the request is aborted and the decompressor left in an incomplete state.
                    //  Closing it might cause an exception that we don't care about.
                    try
                    {
                        this._decompressor.Dispose();
                    }
                    catch
                    { }

                    this._decompressor = null;
                }
            }
        }
    }
}

#endif
