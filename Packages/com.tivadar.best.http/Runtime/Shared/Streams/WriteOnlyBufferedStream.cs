using System;
using System.IO;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.Streams
{
    /// <summary>
    /// A custom buffer stream implementation that will not close the underlying stream.
    /// </summary>
    public sealed class WriteOnlyBufferedStream : Stream
    {
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return this.buffer.Length; } }

        public override long Position { get { return this._position; } set { throw new NotImplementedException("Position set"); } }
        private int _position;

        private byte[] buffer;
        private int _bufferSize;
        private Stream stream;

        private LoggingContext _context;

        public WriteOnlyBufferedStream(Stream stream, int bufferSize, LoggingContext context)
        {
            if (stream == null)
                throw new NullReferenceException(nameof(stream));

            this.stream = stream;
            this._context = context;

            this._bufferSize = bufferSize;
            this.buffer = BufferPool.Get(this._bufferSize, true, context);
            this._position = 0;
        }

        public override void Flush()
        {
            if (this._position > 0)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                // if the underlying stream is an ITCPStreamerContentConsumer, we can use an optimized path and avoid copying
                // the buffered bytes.
                var tcpStreamer = this.stream as Shared.PlatformSupport.Network.Tcp.ITCPStreamerContentConsumer;
                if (tcpStreamer != null)
                {
                    // First swap the buffers because tcpStreamer.Write might cause an exception and both the streamer
                    //  and WriteOnlyBufferedStream would release the same buffer
                    var buff = this.buffer.AsBuffer(this._position);
                    this.buffer = BufferPool.Get(this._bufferSize, true, this._context);

                    tcpStreamer.Write(buff);
                }
                else
#endif
                {
                    this.stream.Write(this.buffer, 0, this._position);
                    this.stream.Flush();
                }

                //if (HTTPManager.Logger.IsDiagnostic)
                //    HTTPManager.Logger.Information("WriteOnlyBufferedStream", string.Format("Flushed {0:N0} bytes", this._position));

                this._position = 0;
            }
        }

        public override void Write(byte[] bufferFrom, int offset, int count)
        {
            while (count > 0)
            {
                int writeCount = Math.Min(count, this.buffer.Length - this._position);
                Array.Copy(bufferFrom, offset, this.buffer, this._position, writeCount);
            
                this._position += writeCount;
                offset += writeCount;
                count -= writeCount;
            
                if (this._position == this.buffer.Length)
                    this.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value) { }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.buffer != null)
                BufferPool.Release(this.buffer);
            this.buffer = null;
        }
    }
}
