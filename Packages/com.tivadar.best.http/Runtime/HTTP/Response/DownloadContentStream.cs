using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Response
{
    /// <summary>
    /// A read-only stream that the plugin uses to store the downloaded content. This stream is designed to buffer downloaded data efficiently and provide it to consumers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The DownloadContentStream serves as a storage medium for content downloaded during HTTP requests.
    /// It buffers the downloaded data in segments and allows clients to read from the buffer as needed.
    /// This buffering mechanism is essential for optimizing download performance, especially in scenarios where the download rate may vary or be faster than the rate at which data is consumed.
    /// </para>
    /// <para>
    /// The stream operates in conjunction with the <see cref="IDownloadContentBufferAvailable"/> interface, which is used to signal connections when buffer space becomes available.
    /// Connections can then transfer additional data into the buffer for processing.
    /// </para>
    /// <para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>Efficient Buffering</term>
    ///         <description>The stream efficiently buffers downloaded content, ensuring that data is readily available for reading without extensive delays.</description>
    ///     </item>
    ///     <item>
    ///         <term>Dynamic Resizing</term>
    ///         <description>The internal buffer dynamically resizes to accommodate varying amounts of downloaded data, optimizing memory usage.</description>
    ///     </item>
    ///     <item>
    ///         <term>Asynchronous Signal Handling</term>
    ///         <description>Asynchronous signaling mechanisms are used to notify connections when buffer space is available, enabling efficient data transfer.</description>
    ///     </item>
    ///     <item>
    ///         <term>Error Handling</term>
    ///         <description>The stream captures and propagates errors that occur during download, allowing clients to handle exceptions gracefully.</description>
    ///     </item>
    ///     <item>
    ///         <term>Blocking Variant</term>
    ///         <description>A blocking variant, <see cref="BlockingDownloadContentStream"/>, allows clients to wait for data when the buffer is empty but not completed.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// Clients can read from this stream using standard stream reading methods, and the stream will release memory segments as data is read.
    /// When the download is completed or if an error occurs during download, this stream allows clients to inspect the completion status and any associated exceptions.
    /// </para>
    /// </remarks>
    public class DownloadContentStream : Stream
    {
        /// <summary>
        /// Gets the HTTP response from which this download stream originated.
        /// </summary>
        public HTTPResponse Response { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the download is completed, and there's no more data buffered in the stream to read.
        /// </summary>
        public bool IsCompleted { get => this._isCompleted && this._segments.Count == 0 && this._currentSegment == BufferSegment.Empty; }

        /// <summary>
        /// Gets a reference to an exception if the download completed with an error.
        /// </summary>
        public Exception CompletedWith { get => this._exceptionInfo?.SourceException; }

        /// <summary>
        /// Gets the length of the buffered data. Because downloads happen in parallel, a <see cref="Read(byte[], int, int)"/> call can return with more data after checking Length.
        /// </summary>
        public override long Length => Interlocked.Read(ref this._length);
        protected long _length;

        /// <summary>
        /// Gets the maximum size of the internal buffer of this stream.
        /// </summary>
        /// <remarks>In some cases, the plugin may put more data into the stream than the specified size.</remarks>
        public long MaxBuffered { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the internal buffer holds at least the <see cref="MaxBuffered"/> amount of data.
        /// </summary>
        public bool IsFull { get => this.Length >= this.MaxBuffered; }

        /// <summary>
        /// Gets or sets whether the stream is detached from the <see cref="HTTPRequest"/>/<see cref="HTTPResponse"/> when <see cref="Read(byte[], int, int)"/> is used before the request is finished.
        /// When the stream is detached from the response object, their lifetimes are not bound together,
        /// meaning that the stream isn't disposed automatically, and the client code is responsible for calling the stream's <see cref="System.IO.Stream.Dispose()"/> function.
        /// </summary>
        public bool IsDetached
        {
            get => this._isDetached;
            set
            {
                if (this._isDetached != value)
                {
                    HTTPManager.Logger.Verbose(nameof(DownloadContentStream), $"IsDetached {this._isDetached} => {value}", this.Response?.Context);

                    this._isDetached = value;
                }
            }
        }
        private bool _isDetached;

        /// <summary>
        /// There are cases where the plugin have to put more data into the buffer than its previously set maximum.
        /// For example when the underlying connection is closed, but the content provider still have buffered data,
        /// in witch case we have to push all processed data to the user facing download stream.
        /// </summary>
        internal void EmergencyIncreaseMaxBuffered() => this.MaxBuffered = long.MaxValue;

        protected IDownloadContentBufferAvailable _bufferAvailableHandler;
        protected ConcurrentQueue<BufferSegment> _segments = new ConcurrentQueue<BufferSegment>();
        protected BufferSegment _currentSegment = BufferSegment.Empty;

        protected bool _isCompleted;
        protected ExceptionDispatchInfo _exceptionInfo;

        /// <summary>
        /// Count of consecutive calls with DoFullCheck that found the stream fully buffered.
        /// </summary>
        private int _isFullCheckCount;

        protected bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the DownloadContentStream class.
        /// </summary>
        /// <param name="response">The HTTP response associated with this download stream.</param>
        /// <param name="maxBuffered">The maximum size of the internal buffer.</param>
        /// <param name="bufferAvailableHandler">Handler for notifying when buffer space becomes available.</param>
        public DownloadContentStream(HTTPResponse response, long maxBuffered, IDownloadContentBufferAvailable bufferAvailableHandler)
        {
            this.Response = response;
            this.MaxBuffered = maxBuffered;
            this._bufferAvailableHandler = bufferAvailableHandler;
        }

        /// <summary>
        /// Completes the download stream with an optional error. Called when the download is finished.
        /// </summary>
        /// <param name="error">The exception that occurred during download, if any.</param>
        internal virtual void CompleteAdding(Exception error)
        {
            HTTPManager.Logger.Information(nameof(DownloadContentStream), $"CompleteAdding({error})", this.Response?.Context);

            this._isCompleted = true;

            this._exceptionInfo = error != null ? ExceptionDispatchInfo.Capture(error) : null;
            this._bufferAvailableHandler = null;
        }

        /// <summary>
        /// Tries to remove a downloaded segment from the stream. If the stream is empty, it returns immediately with false.
        /// </summary>
        /// <param name="segment">A <see cref="BufferSegment"/> containing the reference to a byte[] and the offset and count of the data in the array.</param>
        /// <returns><c>true</c> if a downloaded segment was available and could return with, otherwise <c>false</c></returns>
        public virtual bool TryTake(out BufferSegment segment)
        {
            if (this._isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            this.IsDetached = true;

            if (this._segments.TryDequeue(out segment) && segment.Count > 0)
            {
                Interlocked.Add(ref this._length, -segment.Count);
                this._bufferAvailableHandler?.BufferAvailable(this);

                return true;
            }

            return false;
        }

        /// <summary>
        /// A non-blocking Read function. When it returns <c>0</c>, it doesn't mean the download is complete. If the download interrupted before completing, the next Read call can throw an exception.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes copied to the buffer, or zero if no downloaded data is available at the time of the call.</returns>
        /// <exception cref="ObjectDisposedException">If the stream is already disposed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            using var _ = new Unity.Profiling.ProfilerMarker($"{nameof(DownloadContentStream)}.{nameof(Read)}").Auto();

            if (this._isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (this._exceptionInfo != null)
                this._exceptionInfo.Throw();

            this.IsDetached = true;

            if (this._currentSegment == BufferSegment.Empty)
                this._segments.TryDequeue(out this._currentSegment);

            int sumReadCount = 0;
            while (sumReadCount < count && this._currentSegment != BufferSegment.Empty)
            {
                int readCount = Math.Min(count - sumReadCount, this._currentSegment.Count);
                Array.Copy(this._currentSegment.Data, this._currentSegment.Offset, buffer, offset, readCount);

                offset += readCount;
                sumReadCount += readCount;

                if (this._currentSegment.Count == readCount)
                {
                    BufferPool.Release(this._currentSegment);
                    if (!this._segments.TryDequeue(out this._currentSegment))
                        this._currentSegment = BufferSegment.Empty;
                }
                else
                    this._currentSegment = this._currentSegment.Slice(this._currentSegment.Offset + readCount);
            }
            
            Interlocked.Add(ref this._length, -sumReadCount);

            this._bufferAvailableHandler?.BufferAvailable(this);

            return sumReadCount;
        }

        /// <summary>
        /// Writes a downloaded data segment to the stream.
        /// </summary>
        /// <param name="segment">The downloaded data segment to write.</param>
        internal virtual void Write(BufferSegment segment)
        {
            if (this._isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (segment.Count <= 0)
                return;

            this._segments.Enqueue(segment);
            Interlocked.Add(ref this._length, segment.Count);

            this._isFullCheckCount = 0;
        }

        /// <summary>
        /// Checks whether the stream is fully buffered and increases a counter if it's full, resetting it otherwise.
        /// </summary>
        /// <param name="limit">The limit for the full check counter.</param>
        /// <returns><c>true</c> if the counter is equal to or larger than the limit parameter; otherwise <c>false</c>.</returns>
        internal bool DoFullCheck(int limit)
        {
            if (IsFull)
                _isFullCheckCount++;
            else
                _isFullCheckCount = 0;

            return _isFullCheckCount >= limit;
        }

        /// <summary>
        /// Disposes of the stream, releasing any resources held by it.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._isDisposed = true;

            using (var _ = new Unity.Profiling.ProfilerMarker("DownloadContentStream.Dispose").Auto())
            {
                BufferPool.Release(this._currentSegment);
                BufferPool.ReleaseBulk(this._segments);
                this._segments.Clear();
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Flush() => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}
