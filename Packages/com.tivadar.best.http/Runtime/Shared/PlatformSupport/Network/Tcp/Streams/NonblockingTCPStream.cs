#if !UNITY_WEBGL || UNITY_EDITOR
using System;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    /// <summary>
    /// A non-blocking-read stream over a TCPStreamer that buffers the received bytes from the network in a Peekable stream.
    /// </summary>
    public sealed class NonblockingTCPStream : PeekableContentProviderStream, ITCPStreamerContentConsumer
    {
        private TCPStreamer _streamer;
        private bool _disposeStreamer;
        private uint _maxBufferSize;

        private int peek_listIdx;
        private int peek_pos;

        private object _locker = new object();

        public NonblockingTCPStream(TCPStreamer streamer, bool disposeStreamer, uint maxBufferSize)
        {
            this._streamer = streamer;
            this._streamer.ContentConsumer = this;
            this._disposeStreamer = disposeStreamer;
            this._maxBufferSize = maxBufferSize;
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

            if (this._disposeStreamer)
                this._streamer?.Dispose();
            this._streamer = null;
        }

        // PeekableStream's default implementation of write would place the buffer into its inner segment list,
        // but here we want to send it to the server instead.
        public override void Write(byte[] buffer, int offset, int count) => this._streamer.EnqueueToSend(buffer.CopyAsBuffer(offset, count));

        public override void Write(BufferSegment buffer) => this._streamer.EnqueueToSend(buffer);

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (this._locker)
            {
                int readCount = base.Read(buffer, offset, count);

                // pull content from the streamer, if buffered amount is less then the desired.
                if (base.Length <= this._maxBufferSize)
                {
                    DequeueFromStreamer();
                    this._streamer.BeginReceive();
                }

                return readCount;
            }
        }

        public void OnContent(TCPStreamer streamer)
        {
            lock (this._locker)
            {
                DequeueFromStreamer();

                this.Consumer?.OnContent();
            }
        }

        public void OnConnectionClosed(TCPStreamer streamer)
        {
            this.Consumer?.OnConnectionClosed();
        }

        public void OnError(TCPStreamer streamer, Exception ex)
        {
            this.Consumer?.OnError(ex);
        }

        void DequeueFromStreamer()
        {
            while (this._streamer.Length > 0 && this._length < this._maxBufferSize)
            {
                var segment = this._streamer.DequeueReceived();

                if (segment.Count <= 0)
                    return;

                base.Write(segment);
            }
        }
    }
}
#endif
