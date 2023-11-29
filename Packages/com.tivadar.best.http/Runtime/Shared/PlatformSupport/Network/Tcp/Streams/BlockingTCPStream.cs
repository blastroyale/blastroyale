#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.IO;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    public sealed class BlockingTCPStream : Stream, ITCPStreamerContentConsumer
    {
        private TCPStreamer _streamer;
        private AutoResetEvent _readEvent = new AutoResetEvent(false);
        private bool _disposeStreamer;
        private BufferSegment _currentReadSegment;

        byte[] _readByteBuffer = new byte[1];

        public BlockingTCPStream(TCPStreamer streamer, bool disposeStreamer)
        {
            this._streamer = streamer;
            this._streamer.ContentConsumer = this;
            this._disposeStreamer = disposeStreamer;
        }

        public override int ReadByte()
        {
            int readCount = Read(_readByteBuffer, 0, 1);
            return readCount == 1 ? _readByteBuffer[0] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;

            if (_currentReadSegment != BufferSegment.Empty)
            {
                int readCount = Math.Min(_currentReadSegment.Count, count);

                Array.Copy(_currentReadSegment.Data, _currentReadSegment.Offset, buffer, offset, readCount);

                _currentReadSegment = _currentReadSegment.Slice(_currentReadSegment.Offset + readCount);

                if (_currentReadSegment.Count == 0)
                    _currentReadSegment = BufferSegment.Empty;

                return readCount;
            }
            
            do
            {
                this._currentReadSegment = this._streamer.DequeueReceived();

                if (this._currentReadSegment.Count == 0)
                    this._readEvent.WaitOne();
                else if (this._currentReadSegment.Count > 0)
                    return Read(buffer, offset, count);
                else
                    return -1;
            } while (true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this._disposeStreamer)
                this._streamer?.Dispose();
            this._streamer = null;

            Interlocked.CompareExchange<AutoResetEvent>(ref this._readEvent, null, this._readEvent)
                ?.Dispose();
        }

        public void OnContent(TCPStreamer streamer) => this._readEvent?.Set();

        public void OnConnectionClosed(TCPStreamer streamer)
        {
            this._readEvent?.Set();
        }

        public void OnError(TCPStreamer streamer, Exception ex)
        {
            this._readEvent?.Set();
        }

        public void Write(BufferSegment buffer) => this._streamer.EnqueueToSend(buffer);
        public override void Write(byte[] buffer, int offset, int count) => this._streamer.EnqueueToSend(buffer.CopyAsBuffer(offset, count));

        public override bool CanRead => true;
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => true;
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
    }
}
#endif
