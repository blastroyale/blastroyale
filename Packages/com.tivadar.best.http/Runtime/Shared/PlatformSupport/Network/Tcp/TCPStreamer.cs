#define _LOG_TCP_STREAMER

#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp
{
    /// <summary>
    /// The ITCPStreamerContentConsumer interface represents a specialized content consumer for use with <see cref="TCPStreamer"/>. It offers methods for writing data to the streamer and handling content-related events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key Functions of ITCPStreamerContentConsumer:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Data Writing</term><description>Provides methods to write data to the associated <see cref="TCPStreamer"/> instance, allowing content to be sent over the TCP connection.
    /// </description></item>
    /// <item>
    /// <term>Content Handling</term><description>Defines event methods for notifying consumers when new content is available, the connection is closed, or errors occur during data transfer.
    /// </description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TCPStreamer"/>
    public interface ITCPStreamerContentConsumer
    {
        /// <summary>
        /// Writes the specified data buffer to the associated <see cref="TCPStreamer"/> instance. The data is copied into a new buffer and passed to the streamer for transmission.
        /// </summary>
        /// <param name="buffer">The byte array containing the data to be written.</param>
        /// <param name="offset">The zero-based byte offset in the buffer from which to begin writing.</param>
        /// <param name="count">The number of bytes to write from the buffer.</param>
        void Write(byte[] buffer, int offset, int count);

        /// <summary>
        /// Writes the specified <see cref="BufferSegment"/> directly to the associated <see cref="TCPStreamer"/> instance. The content of the buffer is passed to the streamer for transmission, and the ownership of the buffer is transferred to the <see cref="TCPStreamer"/> too.
        /// </summary>
        /// <param name="buffer">The <see cref="BufferSegment"/> containing the data to be written.</param>
        void Write(BufferSegment buffer);

        /// <summary>
        /// Called when new content is available from the associated <see cref="TCPStreamer"/> instance.
        /// </summary>
        /// <param name="streamer">The <see cref="TCPStreamer"/> instance providing the content.</param>
        void OnContent(TCPStreamer streamer);

        /// <summary>
        /// Called when the connection is closed by the remote peer. It notifies the content consumer about the connection closure.
        /// </summary>
        /// <param name="streamer">The <see cref="TCPStreamer"/> instance for which the connection is closed.</param>
        void OnConnectionClosed(TCPStreamer streamer);

        /// <summary>
        /// Called when an error occurs during content processing or connection handling. It provides the <see cref="TCPStreamer"/> instance and the <see cref="Exception"/> that caused the error.
        /// </summary>
        /// <param name="streamer">The <see cref="TCPStreamer"/> instance where the error occurred.</param>
        /// <param name="ex">The <see cref="Exception"/> that represents the error condition.</param>
        void OnError(TCPStreamer streamer, Exception ex);
    }

    sealed class ReadState
    {
        public int minReceiveBufferSize;

        public byte[] receiveBuffer = null;
        public int isReceiving;
        public long totalReceived;

        public long bufferedLength;
        public ConcurrentQueue<BufferSegment> bufferedSegments = new ConcurrentQueue<BufferSegment>();
    }

    sealed class WriteState
    {
        public byte[] _writeBuffer = null;
        public int _writeInProgress;

        public ConcurrentQueue<BufferSegment> _segmentsToWrite = new ConcurrentQueue<BufferSegment>();
        public long bufferedLength;

        public AutoResetEvent blockEvent = new AutoResetEvent(false);
    }

    /// <summary>
    /// The TCPStreamer class is a versatile component that abstracts the complexities of TCP communication, making it easier to handle data streaming between networked applications or devices. It ensures reliable and efficient data transfer while handling various aspects of network communication and error management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TCPStreamer serves several key functions:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Data Streaming</term><description>It enables the streaming of data between two endpoints over a TCP connection, ideal for scenarios involving the transfer of large data volumes in manageable chunks.
    /// </description></item>
    /// <item>
    /// <term>Buffer Management</term><description>The class efficiently manages buffering for both incoming and outgoing data, ensuring smooth and efficient data transfer.
    /// </description></item>
    /// <item>
    /// <term>Asynchronous Communication</term><description>Utilizing asynchronous communication patterns, it supports non-blocking operations, essential for applications requiring concurrent data processing.
    /// </description></item>
    /// <item>
    /// <term>Error Handling</term><description>Comprehensive error-handling mechanisms address exceptions that may occur during TCP communication, enhancing robustness in the face of network issues or errors.
    /// </description></item>
    /// <item>
    /// <term>Resource Management</term><description>It handles memory buffer management and resource disposal when the TCP connection is closed or the class is disposed.
    /// </description></item>
    /// <item>
    /// <term>Integration with Heartbeat</term><description>Implementing the <see cref="IHeartbeat"/> interface, it can be seamlessly integrated into systems using heartbeat mechanisms for network connection monitoring and management.
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class TCPStreamer : IDisposable//, IHeartbeat
    {
        /// <summary>
        /// Gets or sets the content consumer that interacts with this <see cref="TCPStreamer"/> instance, allowing data to be written to the streamer for transmission.
        /// </summary>
        public ITCPStreamerContentConsumer ContentConsumer { get => this._contentConsumer; set { this._contentConsumer = value; } }
        private ITCPStreamerContentConsumer _contentConsumer;

        /// <summary>
        /// Gets the underlying <see cref="Socket"/> associated with this <see cref="TCPStreamer"/> instance.
        /// </summary>
        public Socket Socket { get => this._socket; }

        /// <summary>
        /// Gets the optional <see cref="LoggingContext"/> associated with this <see cref="TCPStreamer"/> instance, facilitating logging and diagnostics.
        /// </summary>
        public LoggingContext Context { get => this._loggingContext; }

        /// <summary>
        /// Gets a value indicating whether the TCP connection is closed.
        /// </summary>
        public bool IsConnectionClosed { get => this._disposed || this._closed == 1 || this._closeInitiatedByServer; }

        /// <summary>
        /// Gets the minimum receive buffer size for the TCP socket.
        /// </summary>
        public int MinReceiveBufferSize { get => this.readState.minReceiveBufferSize; }

        /// <summary>
        /// Gets the total length of buffered data for reading from the stream.
        /// </summary>
        public long Length { get => this.readState.bufferedLength; }

        private ReadState readState = new ReadState();
        private WriteState writeState = new WriteState();
        
        private Socket _socket;
        private LoggingContext _loggingContext;

        private bool _disposed;
        private int _closed;
        private int _isDisconnected;
        public bool _closeInitiatedByServer;

        /// <summary>
        /// Gets or sets the maximum amount of buffered data allowed for reading from the stream.
        /// </summary>
        private uint MaxBufferedReadAmount;

        /// <summary>
        /// Gets or sets the maximum amount of buffered data allowed for writing to the stream.
        /// </summary>
        private uint MaxBufferedWriteAmount;

        /// <summary>
        /// Initializes a new instance of the TCPStreamer class with the specified <see cref="System.Net.Sockets.Socket"/> and parent <see cref="LoggingContext"/>.
        /// </summary>
        /// <param name="socket">The underlying <see cref="System.Net.Sockets.Socket"/> representing the TCP connection.</param>
        /// <param name="parentLoggingContext">The optional  parent <see cref="LoggingContext"/> for logging and diagnostics.</param>
        public TCPStreamer(Socket socket, uint maxReadBufferSize, uint maxWriteBufferSize, LoggingContext _parentLoggingContext)
        {
            this._socket = socket;
            this.readState.minReceiveBufferSize = this._socket.ReceiveBufferSize;
            this.MaxBufferedReadAmount = maxReadBufferSize;
            this.MaxBufferedWriteAmount = maxWriteBufferSize;

            this._loggingContext = new LoggingContext(this);
            this._loggingContext.Add("Parent", _parentLoggingContext);

#if LOG_TCP_STREAMER
            HTTPManager.Logger.Verbose(nameof(TCPStreamer), $"Created with minReceiveBufferSize: ({this.readState.minReceiveBufferSize:N0})", this._loggingContext);
#endif

            BeginReceive();

            //HTTPManager.Heartbeats.Subscribe(this);

            Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementCurrentConnections();
        }

        /// <summary>
        /// Dequeues received data from the stream's buffer and returns a <see cref="BufferSegment"/> containing the data.
        /// </summary>
        /// <returns>A <see cref="BufferSegment"/> containing the received data.</returns>
        public BufferSegment DequeueReceived()
        {
            if (this.readState.bufferedSegments.TryDequeue(out var segment))
            {
                Interlocked.Add(ref this.readState.bufferedLength, -segment.Count);
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementReceivedAndUnprocessed(-segment.Count);
            }
            else
            {
                if (this.IsConnectionClosed)
                    return new BufferSegment(null, 0, -1);
            }

            BeginReceive();

            return segment;
        }

        /// <summary>
        /// Begins receiving data from the TCP connection asynchronously. This method ensures that only one receive operation happens at a time.
        /// </summary>
        /// <remarks>
        /// When calling this method, it ensures that there is only one active receive operation at a time, preventing overlapping receives. This optimization helps prevent data loss and improves the reliability of the receive process.
        /// </remarks>
        public void BeginReceive()
        {
            var length = this.Length;
            var receiving = this.readState.isReceiving;
            if (!this.IsConnectionClosed && length < MaxBufferedReadAmount && (receiving = Interlocked.CompareExchange(ref this.readState.isReceiving, 1, 0)) == 0)
            {
#if LOG_TCP_STREAMER
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(BeginReceive)}()", this._loggingContext);
#endif

                var readBuffer = BufferPool.Get(this.readState.minReceiveBufferSize, true, this._loggingContext);
                try
                {
                    Interlocked.Exchange(ref this.readState.receiveBuffer, readBuffer);

                    this._socket.BeginReceive(
                        readBuffer, 0, readBuffer.Length,
                        SocketFlags.None,
                        OnReceived,
                        null);
                }
                catch (Exception e)
                {
                    BufferPool.Release(Interlocked.Exchange(ref this.readState.receiveBuffer, null));

                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Exception(nameof(TCPStreamer), $"{nameof(this._socket.BeginReceive)}", e, this._loggingContext);
                }
            }
        }

        private void OnReceived(IAsyncResult asyncResult)
        {
#if LOG_TCP_STREAMER
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnReceived)}()", this._loggingContext);
#endif

            bool isClosed = true;
            int readCount = 0;
            SocketError errorCode = SocketError.Success;
            try
            {
                var socket = this._socket;
                isClosed = socket == null || !socket.Connected;
                long newLength = this.Length;

                if (socket != null)
                {
                    readCount = socket.EndReceive(asyncResult, out errorCode);
                    if (errorCode != SocketError.Success)
                        isClosed = true;
                    else
                        //isClosed = readCount <= 0 && !this._disposed && this._closed == 0;
                        isClosed = readCount <= 0 || this.IsConnectionClosed;

                    if (!isClosed)
                    {
                        newLength = Interlocked.Add(ref this.readState.bufferedLength, readCount);
                        this.readState.totalReceived += readCount;
                    }
                }

                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementTotalNetworkBytesReceived(readCount);
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementReceivedAndUnprocessed(readCount);

#if LOG_TCP_STREAMER
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnReceived)}({readCount:N0}, {isClosed}, {errorCode}, {newLength:N0}, {this.readState.totalReceived:N0})", this._loggingContext);
#endif

                if (!isClosed)
                {
                    byte[] readBuffer = Interlocked.Exchange(ref this.readState.receiveBuffer, null);
                    this.readState.bufferedSegments.Enqueue(readBuffer.AsBuffer(readCount));

                    try
                    {
                        this.ContentConsumer?.OnContent(this);
                    }
                    catch (Exception e)
                    {
                        HTTPManager.Logger.Exception(nameof(TCPStreamer), "ContentConsumer.OnContent", e, this._loggingContext);
                    }
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(TCPStreamer), $"{nameof(OnReceived)}({errorCode})", ex, this._loggingContext);
            }
            finally
            {
                if (!isClosed)
                {
                    Interlocked.Exchange(ref this.readState.isReceiving, 0);
                    BeginReceive();
                }
                else
                {
                    BufferPool.Release(Interlocked.Exchange(ref this.readState.receiveBuffer, null));

                    Interlocked.Exchange(ref this.readState.isReceiving, 0);

                    // Close must be called only when all data read, or initiated by the client too.
                    this._closeInitiatedByServer = true;

                    if (this._closed == 0)
                        this.writeState.blockEvent.Set();

                    try
                    {
                        Interlocked.Exchange(ref this._contentConsumer, null)
                            ?.OnConnectionClosed(this);
                    }
                    catch (Exception e)
                    {
                        HTTPManager.Logger.Exception(nameof(TCPStreamer), "ContentConsumer.OnConnectionClosed", e, this._loggingContext);
                    }

#if LOG_TCP_STREAMER
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(TCPStreamer), $"{nameof(OnReceived)}({errorCode}) - closed (readCount({readCount}) <= 0, or other issues), not calling BeginReceive", this._loggingContext);
#endif
                }
            }
        }

        /// <summary>
        /// Enqueues data to be sent over the TCP connection. The data is added to the stream's outgoing buffer for transmission.
        /// </summary>
        /// <param name="buffer">The <see cref="BufferSegment"/> containing the data to be sent.</param>
        public void EnqueueToSend(BufferSegment buffer)
        {
            if (buffer.Count <= 0)
                return;

            if (this._closeInitiatedByServer)
            {
                BufferPool.Release(buffer);
                //throw new Exception("TCP connection closed by the server!");
                return;
            }

            if (this.IsConnectionClosed)
            {
                BufferPool.Release(buffer);
                //throw new Exception("TCP connection already closed!");
                return;
            }

            try
            {
                long buffered = Interlocked.Add(ref this.writeState.bufferedLength, buffer.Count);
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementBufferedToSend(buffer.Count);

                this.writeState._segmentsToWrite.Enqueue(buffer);

                if (buffered >= MaxBufferedWriteAmount)
                {
#if LOG_TCP_STREAMER
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(EnqueueToSend)} - Enqueued({buffer.Count:N0}) & blocking", this._loggingContext);
#endif

                    this.writeState.blockEvent.Reset();
                    this.writeState.blockEvent.WaitOne();
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.writeState._writeInProgress, 1, 0) == 0)
                        SendFromQueue();
#if LOG_TCP_STREAMER
                    else if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(EnqueueToSend)} - Enqueued({buffer.Count:N0})", this._loggingContext);
#endif
                }
            }
            catch
            {
                Interlocked.Exchange(ref this.writeState._writeInProgress, 0);

                BufferPool.Release(Interlocked.Exchange(ref this.writeState._writeBuffer, null));
                throw;
            }
        }

        private bool SendFromQueue()
        {
            var socket = this._socket;

            // TODO: merge buffers from the queue into a larger one, to send them at once

            if (this.writeState._segmentsToWrite.TryDequeue(out var writeBuffer))
            {
                Interlocked.Exchange(ref this.writeState._writeBuffer, writeBuffer.Data);

#if LOG_TCP_STREAMER
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(SendFromQueue)} - BeginSend({writeBuffer.Count:N0})", this._loggingContext);
#endif

                socket.BeginSend(writeBuffer.Data, writeBuffer.Offset, writeBuffer.Count, SocketFlags.None, OnWroteToNetwork, writeBuffer);

                return true;
            }

            return false;
        }

        private void OnWroteToNetwork(IAsyncResult ar)
        {
#if LOG_TCP_STREAMER
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnWroteToNetwork)}()", this._loggingContext);
#endif

            var writeBuffer = (BufferSegment)ar.AsyncState;
            bool success = false;
            try
            {
                var socket = this._socket;

                if (this.IsConnectionClosed)
                {
                    this.writeState.blockEvent.Set();
                    return;
                }

                int result = socket.EndSend(ar, out var errorCode);
                success = result > 0;

#if LOG_TCP_STREAMER
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnWroteToNetwork)} - OnWroteToNetwork({result:N0}, {errorCode})", this._loggingContext);
#endif

                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementTotalNetworkBytesSent(result);
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementBufferedToSend(-result);

                if (result > 0 && Interlocked.Add(ref this.writeState.bufferedLength, -result) < MaxBufferedWriteAmount)
                    this.writeState.blockEvent.Set();

                if (writeBuffer.Count != result)
                {
                    writeBuffer = writeBuffer.Data.AsBuffer(writeBuffer.Offset + result, writeBuffer.Count - result);

#if LOG_TCP_STREAMER
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnWroteToNetwork)} - OnWroteToNetwork({result:N0})", this._loggingContext);
#endif

                    socket.BeginSend(writeBuffer.Data, writeBuffer.Offset, writeBuffer.Count, SocketFlags.None, OnWroteToNetwork, writeBuffer);
                }
                else
                {
                    BufferPool.Release(Interlocked.Exchange(ref this.writeState._writeBuffer, null));

                    if (!SendFromQueue())
                    {
#if LOG_TCP_STREAMER
                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Information(nameof(TCPStreamer), $"{nameof(OnWroteToNetwork)} - set _writeInProgress = 0", this._loggingContext);
#endif

                        Interlocked.Exchange(ref this.writeState._writeInProgress, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                BufferPool.Release(Interlocked.Exchange(ref this.writeState._writeBuffer, null));

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception(nameof(TCPStreamer), $"{nameof(OnWroteToNetwork)}({ex.Message})", ex, this._loggingContext);
            }
        }

        /// <summary>
        /// Disposes of the <see cref="TCPStreamer"/> instance, releasing associated resources.
        /// </summary>
        public void Dispose()
        {
            if (this._disposed)
                return;
            this._disposed = true;

            this.Close();
            
            GC.SuppressFinalize(this);
        }

        //public void OnHeartbeatUpdate(DateTime now, TimeSpan dif) {}

        /// <summary>
        /// Closes the TCP connection gracefully and performs cleanup operations.
        /// </summary>
        internal void Close()
        {
#if LOG_TCP_STREAMER
            HTTPManager.Logger.Verbose(nameof(TCPStreamer), $"{nameof(Close)}({this._closed}, {this._socket?.Connected})", this._loggingContext);
#endif

            if (Interlocked.CompareExchange(ref this._closed, 1, 0) == 1)
                return;

            try
            {
                this.writeState.blockEvent.Set();

                //this._socket.Shutdown(SocketShutdown.Both);
                this._socket.BeginDisconnect(false, OnDisconnected, null);
            }
            catch
            {
                OnDisconnected(null);
            }
        }

        private void OnDisconnected(IAsyncResult ar)
        {
            // TODO: move cleanup code into a separate function and call it when both _writeInProgress & isReceiving are zero
#if LOG_TCP_STREAMER
            HTTPManager.Logger.Verbose(nameof(TCPStreamer), $"{nameof(OnDisconnected)}()", this._loggingContext);
#endif

            if (Interlocked.CompareExchange(ref this._isDisconnected, 1, 0) == 1)
                return;

            Best.HTTP.Profiler.Network.NetworkStatsCollector.DecrementCurrentConnections();
            
            if (ar != null)
            {
                try
                {
                    this._socket.EndDisconnect(ar);
                }
                catch { }
            }

            while (this.readState.bufferedSegments.TryDequeue(out var segment))
            {
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementReceivedAndUnprocessed(-segment.Count);
                BufferPool.Release(segment);
            }

            while (this.writeState._segmentsToWrite.TryDequeue(out var segment))
            {
                Best.HTTP.Profiler.Network.NetworkStatsCollector.IncrementBufferedToSend(-segment.Count);
                BufferPool.Release(segment);
            }

            BufferPool.Release(Interlocked.Exchange(ref this.writeState._writeBuffer, null));

            // Don't release the receiveBuffer until lower layer returns with the OnReceived callback because:
            //  the plugin would reuse it in other parts, while it's unknown what the lower layer is doing and it can
            //  decide to write into the buffer while we aren't expect it.
            if (this.readState.isReceiving == 0)
                BufferPool.Release(Interlocked.Exchange(ref this.readState.receiveBuffer, null));

            // todo: maybe dispose only if this.writeState._writeInProgress == 0
            //  otherwise we could dispose it here, and try to use it 
            this.writeState.blockEvent.Dispose();

            //HTTPManager.Heartbeats.Unsubscribe(this);

            try
            {
                Interlocked.Exchange(ref this._contentConsumer, null)
                    ?.OnConnectionClosed(this);
            }
            catch { }

            this.Dispose();
        }
    }
}
#endif
