using System;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Response
{
    /// <summary>
    /// A blocking variant of the <see cref="DownloadContentStream"/> that allows clients to wait for downloaded data when the buffer is empty but not completed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BlockingDownloadContentStream is a specialized variant of the <see cref="DownloadContentStream"/> designed to provide a blocking mechanism for clients waiting for downloaded data.
    /// This class is particularly useful when clients need to read from the stream, but the buffer is temporarily empty due to ongoing downloads.
    /// </para>
    /// <para>
    /// Key Features:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Blocking Data Retrieval</term>
    ///         <description>Provides a blocking <see cref="Take()"/> method that allows clients to wait for data if the buffer is empty but not yet completed.</description>
    ///     </item>
    ///     <item>
    ///         <term>Timeout Support</term>
    ///         <description>The <see cref="Take(TimeSpan)"/> method accepts a timeout parameter, allowing clients to set a maximum wait time for data availability.</description>
    ///     </item>
    ///     <item>
    ///         <term>Exception Handling</term>
    ///         <description>Handles exceptions and errors that occur during download, ensuring that clients receive any relevant exception information.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// Clients can use the <see cref="Take()"/> method to retrieve data from the stream, and if the buffer is empty, the method will block until new data is downloaded or a timeout occurs.
    /// This blocking behavior is particularly useful in scenarios where clients need to consume data sequentially but can't proceed until data is available.
    /// </para>
    /// <para>
    /// When the download is completed or if an error occurs during download, this stream allows clients to inspect the completion status and any associated exceptions, just like the base <see cref="DownloadContentStream"/>.
    /// </para>
    /// </remarks>
    public sealed class BlockingDownloadContentStream : DownloadContentStream
    {
        private AutoResetEvent _are = new AutoResetEvent(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockingDownloadContentStream"/> class.
        /// </summary>
        /// <param name="response">The HTTP response associated with this download stream.</param>
        /// <param name="maxBuffered">The maximum size of the internal buffer.</param>
        /// <param name="bufferAvailableHandler">Handler for notifying when buffer space becomes available.</param>
        public BlockingDownloadContentStream(HTTPResponse response, long maxBuffered, IDownloadContentBufferAvailable bufferAvailableHandler)
            : base(response, maxBuffered, bufferAvailableHandler)
        {
        }

        /// <summary>
        /// Attempts to retrieve a downloaded content-segment from the stream, blocking if necessary until a segment is available.
        /// </summary>
        /// <param name="segment">When this method returns, contains the <see cref="BufferSegment"/> instance representing the data, if available; otherwise, contains the value of <see cref="BufferSegment.Empty"/>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if a segment could be retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// The TryTake function provides a blocking approach to retrieve data from the stream.
        /// If the stream has data available, it immediately returns the data.
        /// If there's no data available, the method will block until new data is downloaded or the buffer is marked as completed.
        /// </para>
        /// <para>
        /// This method is designed for scenarios where clients need to read from the stream sequentially and are willing to wait until data is available.
        /// It ensures that clients receive data as soon as it becomes available, without having to repeatedly check or poll the stream.
        /// </para>
        /// </remarks>
        public override bool TryTake(out BufferSegment segment)
        {
            segment = BufferSegment.Empty;

            while (!base.IsCompleted && segment == BufferSegment.Empty)
                segment = Take();

            return segment != BufferSegment.Empty;
        }

        /// <summary>
        /// Returns with a download content-segment. If the stream is currently empty but not completed the execution is blocked until new data downloaded.
        /// A segment is an arbitrary length array of bytes the plugin could read in one operation, it can range from couple of bytes to kilobytes.
        /// </summary>
        /// <returns>A BufferSegment holding a reference to the byte[] containing the downloaded data, offset and count of bytes in the array.</returns>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="InvalidOperationException">The stream is empty and marked as completed.</exception>
        public BufferSegment Take() => Take(TimeSpan.FromMilliseconds(-1));

        /// <summary>
        /// Returns with a download content-segment. If the stream is currently empty but not completed the execution is blocked until new data downloaded or the timeout is reached.
        /// A segment is an arbitrary length array of bytes the plugin could read in one operation, it can range from couple of bytes to kilobytes.
        /// </summary>
        /// <param name="timeout">A TimeSpan that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns>A BufferSegment holding a reference to the byte[] containing the downloaded data, offset and count of bytes in the array. In case of a timeout, BufferSegment.Empty returned.</returns>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="InvalidOperationException">The stream is empty and marked as completed.</exception>
        public BufferSegment Take(TimeSpan timeout)
        {
            this.IsDetached = true;

            while (!base.IsCompleted)
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (this._exceptionInfo != null)
                    this._exceptionInfo.Throw();

                if (this._segments.TryDequeue(out var segment) && segment.Count > 0)
                {
                    Interlocked.Add(ref base._length, -segment.Count);
                    this._bufferAvailableHandler?.BufferAvailable(this);
                    return segment;
                }

                if (base._isCompleted)
                    throw new InvalidOperationException("The stream is empty and marked as completed!");

                if (!this._are.WaitOne(timeout))
                    return BufferSegment.Empty;
            }

            return BufferSegment.Empty;
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This override of the <see cref="Read"/> method provides blocking behavior, meaning if there are no bytes available in the stream, the method will block until new data is downloaded or until the stream completes. Once data is available, or if the stream completes, the method will return with the number of bytes read.
        /// </para>
        /// <para>
        /// This behavior ensures that consumers of the stream can continue reading data sequentially, even if the stream's internal buffer is temporarily empty due to ongoing downloads.
        /// </para>
        /// </remarks>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = base.Read(buffer, offset, count);

            while (readCount == 0)
            {
                this._are?.WaitOne();
                readCount = base.Read(buffer, offset, count);
            }

            return readCount;
        }

        internal override void Write(BufferSegment segment)
        {
            base.Write(segment);

            this._are?.Set();
        }

        internal override void CompleteAdding(Exception error)
        {
            base.CompleteAdding(error);

            this._are?.Set();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._are?.Dispose();
            this._are = null;
        }
    }
}
