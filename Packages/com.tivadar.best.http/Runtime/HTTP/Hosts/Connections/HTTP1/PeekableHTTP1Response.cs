using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Request.Timings;
using Best.HTTP.Response;
using Best.HTTP.Response.Decompression;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

using static Best.HTTP.Hosts.Connections.HTTP1.Constants;
using static Best.HTTP.Response.HTTPStatusCodes;

namespace Best.HTTP.Hosts.Connections.HTTP1
{
    /// <summary>
    /// An HTTP 1.1 response implementation that can utilieze a peekable stream.
    /// Its main entry point is the ProcessPeekable method that should be called after every chunk of data downloaded.
    /// </summary>
    public class PeekableHTTP1Response : HTTPResponse
    {
        public PeekableReadState ReadState
        {
            get => this._readState;
            private set
            {
                if (this._readState != value)
                    HTTPManager.Logger.Information(nameof(PeekableHTTP1Response), $"{this._readState} => {value}", this.Context);
                this._readState = value;
            }
        }
        private PeekableReadState _readState;

        public bool ForceDepleteContent;

        enum ContentDeliveryMode
        {
            Raw,
            RawUnknownLength,
            Chunked,
        }

        public enum PeekableReadState
        {
            StatusLine,
            Headers,
            WaitForContentSent, // when received a 100-continue
            PrepareForContent,
            ContentSetup,
            Content,
            Finished
        }

        private ContentDeliveryMode _deliveryMode;
        private long _expectedLength;
        private Dictionary<string, List<string>> _newHeaders;

        long _downloaded = 0;
        IDecompressor _decompressor = null;
        bool _compressed;
        bool sendProgressChanged;

        int _chunkLength = -1;

        enum ReadChunkedStates
        {
            ReadChunkLength,
            ReadChunk,
            ReadTrailingCRLF,
            ReadTrailingHeaders
        }
        ReadChunkedStates _readChunkedState = ReadChunkedStates.ReadChunkLength;

        IDownloadContentBufferAvailable _bufferAvailableHandler;

        public PeekableHTTP1Response(HTTPRequest request, bool isFromCache, IDownloadContentBufferAvailable bufferAvailableHandler)
            : base(request, isFromCache)
        {
            this._bufferAvailableHandler = bufferAvailableHandler;
        }

        private int _isProccessing;

        public void ProcessPeekable(PeekableContentProviderStream peekable)
        {
            // To avoid executing ProcessPeekable in parallel on two threads, do an atomic CompareExchange and return if the old value wan't 0.
            if (Interlocked.CompareExchange(ref this._isProccessing, 1, 0) != 0)
                return;

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(PeekableHTTP1Response), $"ProcessPeekable({this.ReadState}, {peekable.Length})", this.Context);

            try
            {
                // The first call after setting it to PeekableReadState.WaitForContentSent is after the the client could send its content.
                // This alsow works when "If the request did not contain an Expect header field containing the 100-continue expectation,
                // the client can simply discard this interim response."
                // (https://www.rfc-editor.org/rfc/rfc9110#section-15.2.1-3)
                if (this._readState == PeekableReadState.WaitForContentSent)
                {
                    this._newHeaders?.Clear();
                    this.Headers?.Clear();

                    this._readState = PeekableReadState.StatusLine;
                }

                // It's an unexpected network closure, exept when we reading the content in the RawUnknownLength delivery mode.
                if (peekable == null && ReadState != PeekableReadState.Content && this._deliveryMode != ContentDeliveryMode.RawUnknownLength)
                    throw new Exception("Server closed the connection unexpectedly!");

                switch (ReadState)
                {
                    case PeekableReadState.StatusLine:
                        if (!IsNewLinePresent(peekable))
                            return;

                        Request.Timing.StartNext(TimingEventNames.Headers);

                        var statusLine = HTTPResponse.ReadTo(peekable, (byte)' ');
                        string[] versions = statusLine.Split(new char[] { '/', '.' });
                        
                        this.HTTPVersion = new Version(int.Parse(versions[1]), int.Parse(versions[2]));

                        int statusCode;
                        string statusCodeStr = NoTrimReadTo(peekable, (byte)' ', LF);

                        if (!int.TryParse(statusCodeStr, out statusCode))
                            throw new Exception($"Couldn't parse '{statusCodeStr}' as a status code!");

                        this.StatusCode = statusCode;

                        if (statusCodeStr.Length > 0 && (byte)statusCodeStr[statusCodeStr.Length - 1] != LF && (byte)statusCodeStr[statusCodeStr.Length - 1] != CR)
                            this.Message = ReadTo(peekable, LF);
                        else
                        {
                            HTTPManager.Logger.Warning(nameof(PeekableHTTP1Response), "Skipping Status Message reading!", this.Context);

                            this.Message = string.Empty;
                        }

                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Verbose(nameof(PeekableHTTP1Response), $"HTTP/'{this.HTTPVersion}' '{this.StatusCode}' '{this.Message}'", this.Context);

                        if (this.Request?.DownloadSettings?.OnHeadersReceived != null)
                            this._newHeaders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                        this.ReadState = PeekableReadState.Headers;
                        goto case PeekableReadState.Headers;

                    case PeekableReadState.Headers:
                        ProcessReadHeaders(peekable, PeekableReadState.PrepareForContent);
                        if (this.ReadState == PeekableReadState.PrepareForContent)
                        {
#if !UNITY_WEBGL || UNITY_EDITOR
                            // When upgraded, we don't want to read the content here, so set the state to Finished.
                            if (this.StatusCode == 101 && (HasHeaderWithValue("connection", "upgrade") || HasHeader("upgrade")) && this.Request?.DownloadSettings?.OnUpgraded != null)
                            {
                                HTTPManager.Logger.Information(nameof(PeekableHTTP1Response), "Request Upgraded!", this.Context);

                                this.IsUpgraded = this.Request.DownloadSettings.OnUpgraded(this.Request, this, peekable);

                                if (this.IsUpgraded)
                                {
                                    this._readState = PeekableReadState.Finished;
                                    goto case PeekableReadState.Finished;
                                }
                            }
#endif

                            // If it's a 100-continue, restart reading the response after the client could send its content.
                            if (this.StatusCode == Continue)
                            {
                                this._readState = PeekableReadState.WaitForContentSent;
                                break;
                            }

                            // https://www.rfc-editor.org/rfc/rfc9110#name-informational-1xx
                            // A 1xx response is terminated by the end of the header section; it cannot contain content or trailers.
                            if ((this.StatusCode >= Continue && this.StatusCode < OK) ||

                                // https://www.rfc-editor.org/rfc/rfc9110#name-204-no-content
                                // A 204 response is terminated by the end of the header section; it cannot contain content or trailers.
                                this.StatusCode == NoContent ||

                                // https://www.rfc-editor.org/rfc/rfc9110#name-304-not-modified
                                // A 304 response is terminated by the end of the header section; it cannot contain content or trailers.
                                this.StatusCode == NotModified)
                            {
                                this._readState = PeekableReadState.Finished;
                                goto case PeekableReadState.Finished;
                            }

                            Request.Timing.StartNext(TimingEventNames.Response_Received);

                            // if not an upgraded response, or OnUpgraded returned false, go for the content too.
                            goto case PeekableReadState.PrepareForContent;
                        }
                        break;

                    case PeekableReadState.PrepareForContent:
                        BeginReceiveContent();

                        // A content-length header might come with chunked transfer-encoding too.
                        var contentLengthHeader = GetFirstHeaderValue("content-length");
                        long.TryParse(contentLengthHeader, out this._expectedLength);

                        if (HasHeaderWithValue("transfer-encoding", "chunked"))
                        {
                            this._deliveryMode = ContentDeliveryMode.Chunked;
                            this.ReadState = PeekableReadState.ContentSetup;
                        }
                        else
                        {
                            this._deliveryMode = ContentDeliveryMode.Raw;
                            this.ReadState = PeekableReadState.ContentSetup;
                            var contentRangeHeaders = GetHeaderValues("content-range");

                            if (contentLengthHeader == null && contentRangeHeaders == null)
                            {
                                this._deliveryMode = ContentDeliveryMode.RawUnknownLength;
                            }
                            else if (contentLengthHeader == null && contentRangeHeaders != null)
                            {
                                HTTPRange range = GetRange();

                                this._expectedLength = (range.LastBytePos - range.FirstBytePos) + 1;
                            }
                        }

                        HTTPManager.Logger.Information(nameof(PeekableHTTP1Response), $"PrepareForContent - delivery mode selected: {this._deliveryMode}, {this._expectedLength}!", this.Context);

                        CreateDownloadStream(this._bufferAvailableHandler);
                        
                        string encoding = IsFromCache ? null : GetFirstHeaderValue("content-encoding");

                        this._compressed = !string.IsNullOrEmpty(encoding);
                        this._decompressor = DecompressorFactory.GetDecompressor(encoding, this.Context);
                        this.sendProgressChanged = this.Request.DownloadSettings.OnDownloadProgress != null && this.IsSuccess;

                        this.ReadState = PeekableReadState.Content;
                        goto case PeekableReadState.Content;

                    case PeekableReadState.Content:
                        var downStream = this.DownStream;
                        if (downStream != null && downStream.MaxBuffered <= downStream.Length)
                            return;

                        switch (this._deliveryMode)
                        {
                            case ContentDeliveryMode.Raw: ProcessReadRaw(peekable); break;
                            case ContentDeliveryMode.RawUnknownLength: ProcessReadRawUnknownLength(peekable); break;
                            case ContentDeliveryMode.Chunked: ProcessReadChunked(peekable); break;
                        }

                        if (this.ReadState == PeekableReadState.Finished)
                            goto case PeekableReadState.Finished;
                        break;

                    case PeekableReadState.Finished:
                        //baseRequest.Timing.StartNext(TimingEventNames.Queued_For_Disptach);
                        break;
                }
            }
            finally
            {
                Interlocked.Exchange(ref this._isProccessing, 0);
            }
        }

        bool IsNewLinePresent(PeekableStream peekable)
        {
            peekable.BeginPeek();

            int nextByte = peekable.PeekByte();
            while (nextByte >= 0 && nextByte != 0x0A)
                nextByte = peekable.PeekByte();

            return nextByte == 0x0A;
        }

        private void ProcessReadHeaders(PeekableStream peekable, PeekableReadState targetState)
        {
            if (!IsNewLinePresent(peekable))
                return;

            do
            {
                string headerName = ReadTo(peekable, (byte)':', LF);
                if (headerName == string.Empty)
                {
                    this.ReadState = targetState;

                    if (this.Request?.DownloadSettings?.OnHeadersReceived != null)
                        RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, this._newHeaders));
                    return;
                }

                string value = ReadTo(peekable, LF);

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(PeekableHTTP1Response), $"Header - '{headerName}': '{value}'", this.Context);

                AddHeader(headerName, value);

                if (this._newHeaders != null)
                {
                    List<string> values;
                    if (!this._newHeaders.TryGetValue(headerName, out values))
                        this._newHeaders.Add(headerName, values = new List<string>(1));

                    values.Add(value);
                }
            } while (IsNewLinePresent(peekable));
        }

        private void ProcessReadRawUnknownLength(PeekableStream peekable)
        {
            if (peekable == null)
            {
                if (sendProgressChanged)
                    RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, RequestEvents.DownloadProgress, this._downloaded, this._expectedLength));

                PostProcessContent();

                this.ReadState = PeekableReadState.Finished;

                return;
            }

            while (peekable.Length > 0)
            {
                var buffer = BufferPool.Get(64 * 1024, true, this.Context);

                var readCount = peekable.Read(buffer, 0, buffer.Length);

                ProcessChunk(buffer.AsBuffer(readCount));
            }

            if (sendProgressChanged)
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, RequestEvents.DownloadProgress, this._downloaded, this._expectedLength));
        }

        private bool TryReadChunkLength(PeekableStream peekable, out int result)
        {
            result = -1;
            if (!IsNewLinePresent(peekable))
                return false;

            // Read until the end of line, then split the string so we will discard any optional chunk extensions
            string line = ReadTo(peekable, LF);
            string[] splits = line.Split(';');
            string num = splits[0];

            return int.TryParse(num, System.Globalization.NumberStyles.AllowHexSpecifier, null, out result);
        }

        void ProcessReadChunked(PeekableStream peekable)
        {
            switch(this._readChunkedState)
            {
                case ReadChunkedStates.ReadChunkLength:
                    this._readChunkedState = ReadChunkedStates.ReadChunkLength;

                    if (TryReadChunkLength(peekable, out this._chunkLength))
                    {
                        if (this._chunkLength == 0)
                        {
                            PostProcessContent();

                            if (this.Request?.DownloadSettings?.OnHeadersReceived != null)
                                this._newHeaders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                            goto case ReadChunkedStates.ReadTrailingHeaders;
                        }

                        goto case ReadChunkedStates.ReadChunk;
                    }
                    break;

                case ReadChunkedStates.ReadChunk:
                    this._readChunkedState = ReadChunkedStates.ReadChunk;

                    while (this._chunkLength > 0 && peekable.Length > 0)
                    {
                        int targetReadCount = Math.Min(Math.Min(64 * 1024, this._chunkLength), (int)peekable.Length);

                        var buffer = BufferPool.Get(targetReadCount, true, this.Context);

                        var readCount = peekable.Read(buffer, 0, targetReadCount);

                        if (readCount < 0)
                        {
                            BufferPool.Release(buffer);
                            throw ExceptionHelper.ServerClosedTCPStream();
                        }

                        this._chunkLength -= readCount;

                        ProcessChunk(buffer.AsBuffer(readCount));
                    }

                    if (sendProgressChanged)
                        RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, RequestEvents.DownloadProgress, this._downloaded, this._expectedLength));

                    // Every chunk data has a trailing CRLF
                    if (this._chunkLength == 0)
                        goto case ReadChunkedStates.ReadTrailingCRLF;
                    break;

                case ReadChunkedStates.ReadTrailingCRLF:
                    this._readChunkedState = ReadChunkedStates.ReadTrailingCRLF;

                    if (IsNewLinePresent(peekable))
                    {
                        ReadTo(peekable, LF);
                        goto case ReadChunkedStates.ReadChunkLength;
                    }
                    break;

                case ReadChunkedStates.ReadTrailingHeaders:
                    this._readChunkedState = ReadChunkedStates.ReadTrailingHeaders;

                    ProcessReadHeaders(peekable, PeekableReadState.Finished);
                    break;
            }
        }

        void ProcessReadRaw(PeekableStream peekable)
        {
            if (this.DownStream == null)
                throw new ArgumentNullException(nameof(this.DownStream));
            if (peekable == null)
                throw new ArgumentNullException(nameof(peekable));

            while (peekable.Length > 0 && !this.DownStream.IsFull)
            {
                var buffer = BufferPool.Get(64 * 1024, true, this.Context);
                
                var readCount = peekable.Read(buffer, 0, buffer.Length);

                if (readCount < 0)
                {
                    BufferPool.Release(buffer);
                    throw ExceptionHelper.ServerClosedTCPStream();
                }

                ProcessChunk(buffer.AsBuffer(readCount));
            }

            if (sendProgressChanged)
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.Request, RequestEvents.DownloadProgress, this._downloaded, this._expectedLength));

            if (this._downloaded >= this._expectedLength)
            {
                PostProcessContent();
                this.ReadState = PeekableReadState.Finished;
            }
        }

        void ProcessChunk(BufferSegment chunk)
        {
            this._downloaded += chunk.Count;

            if (this._compressed)
            {
                var (decompressed, release) = this._decompressor.Decompress(chunk, false, true, this.Context);
                if (decompressed != BufferSegment.Empty)
                    FeedDownloadedContentChunk(decompressed);

                //if (decompressed.Data != chunk.Data)
                if (release)
                    BufferPool.Release(chunk);
            }
            else
            {
                FeedDownloadedContentChunk(chunk);
            }
        }

        void PostProcessContent()
        {
            if (this._compressed)
            {
                var (decompressed, release) = this._decompressor.Decompress(BufferSegment.Empty, true, true, this.Context);
                if (decompressed != BufferSegment.Empty)
                    FeedDownloadedContentChunk(decompressed);
            }

            FinishedContentReceiving();
            
            if (this._decompressor != null)
            {
                this._decompressor.Dispose();
                this._decompressor = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._decompressor?.Dispose();
        }
    }
}
