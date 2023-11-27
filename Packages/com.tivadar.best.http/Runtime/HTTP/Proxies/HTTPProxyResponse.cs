#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.Streams;

using static Best.HTTP.Hosts.Connections.HTTP1.Constants;

namespace Best.HTTP.Proxies
{
    internal sealed class HTTPProxyResponse : IContentConsumer
    {
        public PeekableReadState ReadState
        {
            get => this._readState;
            private set
            {
                if (this._readState != value)
                    HTTPManager.Logger.Information(nameof(HTTPProxyResponse), $"{this._readState} => {value}", this._parameters.context);
                this._readState = value;
            }
        }

        public int VersionMajor { get; private set; }
        public int VersionMinor { get; private set; }
        public int StatusCode { get; private set; }
        public string Message { get; private set; }

        public Dictionary<string, List<string>> Headers { get; private set; }

        public PeekableContentProviderStream ContentProvider { get; private set; }

        private PeekableReadState _readState;

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
            PrepareForContent,
            ContentSetup,
            RawContent,
            Content,
            Finished
        }


        private ContentDeliveryMode _deliveryMode;
        private ProxyConnectParameters _parameters;
        private long _expectedLength;
        private BufferPoolMemoryStream _output;
        int _chunkLength = -1;

        enum ReadChunkedStates
        {
            ReadChunkLength,
            ReadChunk,
            ReadTrailingCRLF,
            ReadTrailingHeaders
        }
        ReadChunkedStates _readChunkedState = ReadChunkedStates.ReadChunkLength;
        private long _downloaded;

        public Action<ProxyConnectParameters, HTTPProxyResponse, Exception> OnFinished;

        public string DataAsText { get; private set; }

        public HTTPProxyResponse(ProxyConnectParameters parameters)
        {
            this._parameters = parameters;
            this._parameters.stream.SetTwoWayBinding(this);

            this.Headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetBinding(PeekableContentProviderStream contentProvider) => this.ContentProvider = contentProvider;
        public void UnsetBinding() => this.ContentProvider = null;

        public void OnConnectionClosed()
        {
            Exception error = null;
            if (this.ReadState == PeekableReadState.Content && this._deliveryMode == ContentDeliveryMode.RawUnknownLength)
            {
                PostProcessContent();
                error = new Exception($"Proxy returned with {this.StatusCode} - '{this.Message}' : \"{this.DataAsText}\"");
            }
            else
            {
                error = new Exception("Connection to the proxy closed unexpectedly!");
            }

            CallFinished(error);
        }

        public void OnError(Exception ex)
        {
            //(this._parameters.stream as IPeekableContentProvider).Consumer = null;
            this.ContentProvider.Unbind();

            CallFinished(ex);
        }

        void CallFinished(Exception error)
        {
            var callback = Interlocked.Exchange(ref this.OnFinished, null);
            callback?.Invoke(this._parameters, this, error);
        }

        public void OnContent()
        {
            switch (ReadState)
            {
                case PeekableReadState.StatusLine:
                    if (!IsNewLinePresent(this.ContentProvider))
                        return;

                    var statusLine = HTTPResponse.ReadTo(this.ContentProvider, (byte)' ');
                    string[] versions = statusLine.Split(new char[] { '/', '.' });
                    this.VersionMajor = int.Parse(versions[1]);
                    this.VersionMinor = int.Parse(versions[2]);

                    int statusCode;
                    string statusCodeStr = HTTPResponse.NoTrimReadTo(this.ContentProvider, (byte)' ', LF);

                    if (!int.TryParse(statusCodeStr, out statusCode))
                        throw new Exception($"Couldn't parse '{statusCodeStr}' as a status code!");

                    this.StatusCode = statusCode;

                    if (statusCodeStr.Length > 0 && (byte)statusCodeStr[statusCodeStr.Length - 1] != LF && (byte)statusCodeStr[statusCodeStr.Length - 1] != CR)
                        this.Message = HTTPResponse.ReadTo(this.ContentProvider, LF);
                    else
                    {
                        HTTPManager.Logger.Warning(nameof(HTTPProxyResponse), "Skipping Status Message reading!", this._parameters.context);

                        this.Message = string.Empty;
                    }

                    if (HTTPManager.Logger.IsDiagnostic)
                        VerboseLogging($"HTTP/'{this.VersionMajor}.{this.VersionMinor}'  '{this.StatusCode}' '{this.Message}'");

                    this.ReadState = PeekableReadState.Headers;
                    goto case PeekableReadState.Headers;

                case PeekableReadState.Headers:
                    ProcessReadHeaders(this.ContentProvider, PeekableReadState.PrepareForContent);
                    if (this.ReadState == PeekableReadState.PrepareForContent)
                    {
                        if (this.StatusCode == 200)
                        {
                            this.ReadState = PeekableReadState.Finished;
                            goto case PeekableReadState.Finished;
                        }

                        // if it's an error response from the proxy, read all from the network
                        goto case PeekableReadState.PrepareForContent;
                    }
                    break;

                case PeekableReadState.PrepareForContent:
                    // A content-length header might come with chunked transfer-encoding too.
                    List<string> contentLengthHeaders = GetHeaderValues("content-length");
                    if (contentLengthHeaders != null)
                        this._expectedLength = long.Parse(contentLengthHeaders[0]);

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

                        if (contentLengthHeaders == null && contentRangeHeaders == null)
                        {
                            this._deliveryMode = ContentDeliveryMode.RawUnknownLength;
                        }
                        else if (contentLengthHeaders == null && contentRangeHeaders != null)
                        {
                            throw new NotImplementedException("ranges");
                        }
                    }

                    this._output = new BufferPoolMemoryStream(1024);

                    this.ReadState = PeekableReadState.Content;
                    goto case PeekableReadState.Content;

                case PeekableReadState.Content:
                    switch (this._deliveryMode)
                    {
                        case ContentDeliveryMode.Raw: ProcessReadRaw(this.ContentProvider); break;
                        case ContentDeliveryMode.RawUnknownLength: ProcessReadRawUnknownLength(this.ContentProvider); break;
                        case ContentDeliveryMode.Chunked: ProcessReadChunked(this.ContentProvider); break;
                    }

                    if (this.ReadState == PeekableReadState.Finished)
                        goto case PeekableReadState.Finished;
                    break;

                case PeekableReadState.Finished:
                    //(this._parameters.stream as IPeekableContentProvider).Consumer = null;
                    this.ContentProvider.Unbind();
                    if (this.StatusCode == 200)
                    {
                        CallFinished(null);
                    }
                    else
                    {
                        CallFinished(new Exception($"Proxy returned with {this.StatusCode} - '{this.Message}' : \"{this.DataAsText}\""));
                    }
                    break;
            }
        }

        public List<string> GetHeaderValues(string name)
        {
            if (Headers == null)
                return null;

            List<string> values;
            if (!Headers.TryGetValue(name, out values) || values.Count == 0)
                return null;

            return values;
        }

        public string GetFirstHeaderValue(string name)
        {
            if (Headers == null)
                return null;

            List<string> values;
            if (!Headers.TryGetValue(name, out values) || values.Count == 0)
                return null;

            return values[0];
        }

        public bool HasHeaderWithValue(string headerName, string value)
        {
            var values = GetHeaderValues(headerName);
            if (values == null)
                return false;

            for (int i = 0; i < values.Count; ++i)
                if (string.Compare(values[i], value, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;

            return false;
        }

        public void AddHeader(string name, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            List<string> values;
            if (!Headers.TryGetValue(name, out values))
                Headers.Add(name, values = new List<string>(1));

            values.Add(value);
        }

        private void VerboseLogging(string v)
        {
            HTTPManager.Logger.Verbose(nameof(HTTPProxyResponse), v, this._parameters.context);
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
                string headerName = HTTPResponse.ReadTo(peekable, (byte)':', LF);
                if (headerName == string.Empty)
                {
                    this.ReadState = targetState;
                    return;
                }

                string value = HTTPResponse.ReadTo(peekable, LF);

                if (HTTPManager.Logger.IsDiagnostic)
                    VerboseLogging($"Header - '{headerName}': '{value}'");

                AddHeader(headerName, value);
            } while (IsNewLinePresent(peekable));
        }

        private void ProcessReadRawUnknownLength(PeekableStream peekable)
        {
            while (peekable.Length > 0)
            {
                var buffer = BufferPool.Get(64 * 1024, true, this._parameters.context);
                using var _ = new AutoReleaseBuffer(buffer);

                var readCount = peekable.Read(buffer, 0, buffer.Length);

                ProcessChunk(buffer.AsBuffer(readCount));
            }
        }

        private bool TryReadChunkLength(PeekableStream peekable, out int result)
        {
            result = -1;
            if (!IsNewLinePresent(peekable))
                return false;

            // Read until the end of line, then split the string so we will discard any optional chunk extensions
            string line = HTTPResponse.ReadTo(peekable, LF);
            string[] splits = line.Split(';');
            string num = splits[0];

            return int.TryParse(num, System.Globalization.NumberStyles.AllowHexSpecifier, null, out result);
        }

        void ProcessReadChunked(PeekableStream peekable)
        {
            switch (this._readChunkedState)
            {
                case ReadChunkedStates.ReadChunkLength:
                    this._readChunkedState = ReadChunkedStates.ReadChunkLength;

                    if (TryReadChunkLength(peekable, out this._chunkLength))
                    {
                        if (this._chunkLength == 0)
                        {
                            PostProcessContent();
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

                        var buffer = BufferPool.Get(targetReadCount, true, this._parameters.context);
                        using var _ = new AutoReleaseBuffer(buffer);

                        var readCount = peekable.Read(buffer, 0, targetReadCount);

                        if (readCount < 0)
                            throw ExceptionHelper.ServerClosedTCPStream();

                        this._chunkLength -= readCount;

                        ProcessChunk(buffer.AsBuffer(readCount));
                    }

                    // Every chunk data has a trailing CRLF
                    if (this._chunkLength == 0)
                        goto case ReadChunkedStates.ReadTrailingCRLF;
                    break;

                case ReadChunkedStates.ReadTrailingCRLF:
                    this._readChunkedState = ReadChunkedStates.ReadTrailingCRLF;

                    if (IsNewLinePresent(peekable))
                    {
                        HTTPResponse.ReadTo(peekable, LF);
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
            while (peekable.Length > 0)
            {
                var buffer = BufferPool.Get(64 * 1024, true, this._parameters.context);
                using var _ = new AutoReleaseBuffer(buffer);

                var readCount = peekable.Read(buffer, 0, buffer.Length);

                if (readCount < 0)
                    throw ExceptionHelper.ServerClosedTCPStream();

                ProcessChunk(buffer.AsBuffer(readCount));
            }

            if (this._downloaded >= this._expectedLength)
            {
                PostProcessContent();
            }
        }

        void ProcessChunk(BufferSegment chunk)
        {
            this._downloaded += chunk.Count;
            this._output.Write(chunk.Data, chunk.Offset, chunk.Count);
        }

        void PostProcessContent()
        {
            this.ReadState = PeekableReadState.Finished;

            if (this._output != null)
            {
                var buff = this._output.GetBuffer();
                this.DataAsText = System.Text.Encoding.UTF8.GetString(buff, 0, (int)this._output.Length);

                this._output.Dispose();
                this._output = null;
            }
        }

        public override string ToString() => $"{StatusCode} - {Message}: \"{this.DataAsText}\"";
    }
}
#endif
