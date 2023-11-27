using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.Streams
{
    public class BufferSegmentStream : Stream
    {
        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return this._length; } }
        protected long _length;

        public override long Position { get { return 0; } set { } }

        protected List<BufferSegment> bufferList = new List<BufferSegment>();

        private byte[] _tempByteArray = new byte[1];

        public override int ReadByte()
        {
            if (Read(this._tempByteArray, 0, 1) == 0)
                return -1;

            return this._tempByteArray[0];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int sumReadCount = 0;

            while (count > 0 && bufferList.Count > 0)
            {
                BufferSegment buff = this.bufferList[0];

                int readCount = Math.Min(count, buff.Count);

                Array.Copy(buff.Data, buff.Offset, buffer, offset, readCount);

                sumReadCount += readCount;
                offset += readCount;
                count -= readCount;

                this.bufferList[0] = buff = buff.Slice(buff.Offset + readCount);

                if (buff.Count == 0)
                {
                    this.bufferList.RemoveAt(0);
                    BufferPool.Release(buff.Data);
                }
            }

            Interlocked.Add(ref this._length, -sumReadCount);

            return sumReadCount;
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(new BufferSegment(buffer, offset, count));

        public virtual void Write(BufferSegment bufferSegment)
        {
            this.bufferList.Add(bufferSegment);
            Interlocked.Add(ref this._length, bufferSegment.Count);
        }

        public virtual void Reset()
        {
            BufferPool.ReleaseBulk(this.bufferList);

            this.bufferList.Clear();
            Interlocked.Exchange(ref this._length, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Reset();
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
    }
}
