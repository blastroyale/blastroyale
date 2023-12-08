#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    public sealed class FrameworkTLSByteForwarder : Stream, ITCPStreamerContentConsumer
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get { return this._length; } }
        private long _length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private TCPStreamer _streamer;
        private LoggingContext _context;
        private ITCPStreamerContentConsumer _contentConsumer;

        private Queue<BufferSegment> _segmentsToReadFrom = new Queue<BufferSegment>(8);

        private AutoResetEvent _are = new AutoResetEvent(false);
        private ReaderWriterLockSlim _rws = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private BufferSegment _currentReadSegment = BufferSegment.Empty;

        private uint _maxBufferSize;

        public FrameworkTLSByteForwarder(TCPStreamer streamer, ITCPStreamerContentConsumer contentConsumer, uint maxBufferSize, LoggingContext context)
        {
            this._streamer = streamer;
            this._streamer.ContentConsumer = this;

            this._contentConsumer = contentConsumer;

            this._context = context;
            this._maxBufferSize = maxBufferSize;
        }

        public void /*ITCPStreamerContentConsumer.*/ Write(BufferSegment buffer)
        {
            using var _ = new AutoReleaseBuffer(buffer);
            this.Write(buffer.Data, buffer.Offset, buffer.Count);
        }

        int _pullContentInProgress;

        void PullContentFromStreamer()
        {
            //using var _ = new WriteLock(this._rws);
            if (Interlocked.CompareExchange(ref _pullContentInProgress, 1, 0) != 0)
                return;

            try
            {
                while (this._streamer.Length > 0 && this._length < this._maxBufferSize)
                {
                    var tmp = this._streamer.DequeueReceived();

                    if (tmp.Count <= 0)
                    {
                        HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"DequeueReceived({tmp}) !", this._context);

                        BufferPool.Release(tmp);
                        return;
                    }

                    this._segmentsToReadFrom.Enqueue(tmp);
                    this._length += tmp.Count;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _pullContentInProgress, 0);
            }
        }

        public void /*ITCPStreamerContentConsumer.*/ OnContent(TCPStreamer streamer)
        {
            HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"OnContent({streamer?.Length})", this._context);

            PullContentFromStreamer();

            this._are?.Set();

            this._contentConsumer?.OnContent(streamer);
        }

        public void /*ITCPStreamerContentConsumer.*/ OnConnectionClosed(TCPStreamer streamer) => this._contentConsumer?.OnConnectionClosed(streamer);

        public void /*ITCPStreamerContentConsumer.*/ OnError(TCPStreamer streamer, Exception ex) => this._contentConsumer?.OnError(streamer, ex);

        // Called by SslStream.Read expecting encrypted content
        public override int Read(byte[] buffer, int offset, int count)
        {
            HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"Read({offset}, {count})", this._context);

            PullContentFromStreamer();

            int sumReadCount = 0;

            while (this._currentReadSegment == BufferSegment.Empty && this._segmentsToReadFrom.Count == 0)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"WaitOne() for new data!", this._context);

                if (this.Length == 0)
                    this._are.WaitOne();

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"WaitOne() returned!", this._context);
            }

            while ((this._currentReadSegment != BufferSegment.Empty || this._segmentsToReadFrom.Count > 0) && count > 0)
            {
                if (this._currentReadSegment != BufferSegment.Empty)
                {
                    int readCount = Math.Min(count, this._currentReadSegment.Count);
                    Array.Copy(this._currentReadSegment.Data, this._currentReadSegment.Offset, buffer, offset, readCount);
                    offset += readCount;
                    count -= readCount;
                    sumReadCount += readCount;

                    if (this._currentReadSegment.Count <= readCount)
                        this._currentReadSegment = BufferSegment.Empty;
                    else
                        this._currentReadSegment = this._currentReadSegment.Slice(this._currentReadSegment.Offset + readCount);
                }
                else
                {
                    this._currentReadSegment = this._segmentsToReadFrom.Dequeue();
                }
            }

            this._length -= sumReadCount;

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"Read() returns with readCount: {sumReadCount}, remaining: {this._length}", this._context);

            return sumReadCount;
        }

        // Called by SslStream.Write with encrypted payload
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(FrameworkTLSByteForwarder), $"Write({buffer.AsBuffer(offset, count)})", this._context);

            var queued = BufferPool.Get(count, true, this._context);

            Array.Copy(buffer, offset, queued, 0, count);

            this._streamer.EnqueueToSend(queued.AsBuffer(count));
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._are?.Dispose();
            this._are = null;

            this._rws?.Dispose();
            this._rws = null;

            this._streamer?.Dispose();
        }
    }
}
#endif
