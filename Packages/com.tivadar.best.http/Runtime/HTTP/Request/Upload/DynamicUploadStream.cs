using System;
using System.Collections.Concurrent;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Request.Upload
{
    /// <summary>
    /// A specialized upload stream designed to handle data that's generated on-the-fly or periodically.
    /// </summary>
    /// <remarks>
    /// This implementation is designed to handle scenarios where data may not always be immediately available for upload.
    /// The request will remain active until the <see cref="Complete"/> method is invoked, ensuring that data can continue to be fed into the stream even if it's temporarily empty during a Read operation.
    /// </remarks>
    public sealed class DynamicUploadStream : UploadStreamBase
    {
        /// <summary>
        /// Gets the length of the upload stream.
        /// </summary>
        /// <remarks>
        /// This implementation returns a constant value of <c>-1</c>, indicating that the length of the data to be uploaded is unknown. When the processing connection encounters this value, it should utilize chunked uploading to handle the data transfer.
        /// </remarks>
        /// <value>The constant value of <c>-1</c>, representing unknown length.</value>
        public override long Length
            => BodyLengths.UnknownWithChunkedTransferEncoding;

        /// <summary>
        /// Gets the length of data currently buffered and ready for upload.
        /// </summary>
        /// <value>The length of buffered data in bytes.</value>
        public long BufferedLength
            => this._bufferedLength;

        private long _bufferedLength;
        private bool _isCompleted;
        private ConcurrentQueue<BufferSegment> _chunks = new ConcurrentQueue<BufferSegment>();
        private BufferSegment _current;
        private string _contentType;

        /// <summary>
        /// Initializes a new instance of the DynamicUploadStream class with an optional content type.
        /// </summary>
        /// <param name="contentType">The MIME type of the content to be uploaded. Defaults to "<c>application/octet-stream</c>" if not specified.</param>
        /// <remarks>
        /// This constructor allows the caller to specify the content type of the data to be uploaded. If not provided, it defaults to a general binary data type.
        /// </remarks>
        public DynamicUploadStream(string contentType = "application/octet-stream")
            => this._contentType = contentType;

        /// <summary>
        /// Sets the necessary headers before sending the request.
        /// </summary>
        public override void BeforeSendHeaders(HTTPRequest request)
            => request.SetHeader("content-type", this._contentType);

        /// <summary>
        /// Prepares the stream before the request body is sent.
        /// </summary>
        public override void BeforeSendBody(HTTPRequest request, IThreadSignaler threadSignaler)
            => base.BeforeSendBody(request, threadSignaler);

        /// <summary>
        /// Reads data from the stream to be uploaded.
        /// </summary>
        /// <remarks>
        /// The returned value indicates the state of the stream:
        /// <list type="bullet">
        ///     <item><term>-1</term><description>More data is expected in the future, but isn't currently available. When new data is ready, the IThreadSignaler must be notified.</description></item>
        ///     <item><term>0</term><description>The stream has been closed and no more data will be provided.</description></item>
        ///     <item><description>Otherwise it returns with the number bytes copied to the buffer.</description></item>
        /// </list>
        /// Note: A zero return value can come after a -1 return, indicating a transition from waiting to completion.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = 0;

            while (readCount < count && (_current != BufferSegment.Empty || _chunks.TryDequeue(out _current)))
            {
                int copyCount = Math.Min(count - readCount, _current.Count);

                Array.Copy(_current.Data, _current.Offset, buffer, offset, copyCount);

                readCount += copyCount;
                offset += copyCount;

                if (_current.Offset + copyCount >= _current.Count)
                {
                    BufferPool.Release(_current);
                    _current = BufferSegment.Empty;
                }
                else
                {
                    _current = _current.Slice(_current.Offset + copyCount);
                }
            }

            if (!this._isCompleted && readCount == 0)
                return UploadReadConstants.WaitForMore;

            Interlocked.Add(ref this._bufferedLength, -readCount);

            return readCount;
        }

        /// <summary>
        /// Writes data to the stream, making it available for upload.
        /// </summary>
        /// <remarks>
        /// After writing data to the stream using this method, the connection is signaled that data is available to send.
        /// </remarks>
        /// <param name="buffer">The array of unsigned bytes from which to copy count bytes to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to write after the stream has been marked as complete.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (this._isCompleted)
                throw new InvalidOperationException("Complete() already called on the stream!");

            var localCopy = BufferPool.Get(count, true, base.Signaler?.Context);
            Array.Copy(buffer, 0, localCopy, offset, count);

            Write(localCopy.AsBuffer(count));
        }

        /// <summary>
        /// Writes a segment of data to the stream, making it available for upload.
        /// </summary>
        /// <param name="segment">A segment of data to be written to the stream.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to write after the stream has been marked as complete.</exception>
        /// <remarks>
        /// After writing a segment to the stream using this method, the connection is signaled that data is available to send.
        /// </remarks>
        public void Write(BufferSegment segment)
        {
            if (this._isCompleted)
                throw new InvalidOperationException("Complete() already called on the stream!");

            if (segment.Data == null)
                return;

            this._chunks.Enqueue(segment);
            Interlocked.Add(ref this._bufferedLength, segment.Count);

            this.Signaler?.SignalThread();
        }

        /// <summary>
        /// Marks the stream as complete, signaling that no more data will be added.
        /// </summary>
        /// <remarks>
        /// All remaining buffered data will be sent to the server.
        /// </remarks>
        public void Complete()
        {
            if (this._isCompleted)
                throw new InvalidOperationException("Complete() already called on the stream!");

            this._isCompleted = true;
            base.Signaler?.SignalThread();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new System.NotImplementedException();
        public override void SetLength(long value) => throw new System.NotImplementedException();
        public override void Flush() { }
    }
}
