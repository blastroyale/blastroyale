using System;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    public sealed class NonblockingUnderlyingStream : PeekableContentProviderStream
    {
        private System.IO.Stream _stream;
        private int _receiving;
        private uint _maxBufferSize;

        private LoggingContext _context;

        private object _locker = new object();
        private int peek_listIdx;
        private int peek_pos;

        public NonblockingUnderlyingStream(System.IO.Stream stream, uint maxBufferSize, LoggingContext context)
        {
            this._stream = stream;
            this._context = context;
            this._maxBufferSize = maxBufferSize;

            if (!stream.CanRead)
                throw new NotSupportedException("Stream.Read");

            BeginReceive();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (this._locker)
            {
                int readCount = base.Read(buffer, offset, count);

                if (base.Length <= this._maxBufferSize)
                    BeginReceive();

                return readCount;
            }
        }

        private void BeginReceive()
        {
            if (base._length < this._maxBufferSize && Interlocked.CompareExchange(ref this._receiving, 1, 0) == 0 && this._stream.CanRead)
            {
                long readCount = this._maxBufferSize - base._length;
                var readBuffer = BufferPool.Get(readCount, true, this._context);

                try
                {
                    var ar = this._stream.BeginRead(readBuffer, 0, (int)readCount, OnReceived, readBuffer);

                    if (ar.CompletedSynchronously)
                        HTTPManager.Logger.Warning(nameof(NonblockingUnderlyingStream), $"CompletedSynchronously!", this._context);
                }
                catch (Exception ex)
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Exception(nameof(NonblockingUnderlyingStream), $"{nameof(this._stream.BeginRead)}", ex, this._context);

                    BufferPool.Release(Interlocked.Exchange(ref readBuffer, null));

                    this.Consumer.OnError(ex);
                }
            }
        }

        private void OnReceived(IAsyncResult ar)
        {
            int readCount = 0;
            bool isClosed = true;
            var readBuffer = ar.AsyncState as byte[];

            try
            {
                readCount = this._stream.EndRead(ar);
                isClosed = readCount <= 0;

                if (!isClosed)
                {
                    lock (this._locker)
                        base.Write(readBuffer.AsBuffer(0, readCount));

                    try
                    {
                        this.Consumer?.OnContent();
                    }
                    catch (Exception e)
                    {
                        HTTPManager.Logger.Exception(nameof(NonblockingUnderlyingStream), "ContentConsumer.OnContent", e, this._context);
                    }
                }
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception(nameof(NonblockingUnderlyingStream), $"{nameof(OnReceived)}", ex, this._context);
            }
            finally
            {
                if (!isClosed)
                {
                    Interlocked.Exchange(ref this._receiving, 0);
                    BeginReceive();
                }
                else
                {
                    BufferPool.Release(readBuffer);

                    try
                    {
                        this.Consumer?.OnConnectionClosed();
                    }
                    catch (Exception e)
                    {
                        HTTPManager.Logger.Exception(nameof(NonblockingUnderlyingStream), "Consumer.OnConnectionClosed", e, this._context);
                    }
                }
            }
        }

        public override void BeginPeek()
        {
            lock (this._locker)
            {
                peek_listIdx = 0;
                peek_pos = base.bufferList.Count > 0 ? base.bufferList[0].Offset : 0;
            }
        }

        public override int PeekByte()
        {
            lock (this._locker)
            {
                if (base.bufferList.Count == 0)
                    return -1;

                var segment = base.bufferList[this.peek_listIdx];
                if (peek_pos >= segment.Offset + segment.Count)
                {
                    if (base.bufferList.Count <= this.peek_listIdx + 1)
                        return -1;

                    segment = base.bufferList[++this.peek_listIdx];
                    this.peek_pos = segment.Offset;
                }

                return segment.Data[this.peek_pos++];
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._stream.Dispose();
        }
    }
}
