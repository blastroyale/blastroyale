using System;

using Best.HTTP.Shared.PlatformSupport.Text;

namespace Best.HTTP.Shared.PlatformSupport.Memory
{
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public struct AutoReleaseBuffer : IDisposable
    {
        public static readonly AutoReleaseBuffer Empty = new AutoReleaseBuffer(null);

        public byte[] Data;
        public int Offset;
        public int Count;

        public AutoReleaseBuffer(byte[] data)
        {
            this.Data = data;
            this.Offset = 0;
            this.Count = data != null ? data.Length : 0;
        }

        public AutoReleaseBuffer(BufferSegment segment)
        {
            this.Data = segment.Data;
            this.Offset = segment.Offset;
            this.Count = segment.Count;
        }

        public AutoReleaseBuffer(byte[] data, int offset, int count)
        {
            this.Data = data;
            this.Offset = offset;
            this.Count = count;
        }

        public BufferSegment Slice(int newOffset) => new BufferSegment(this.Data, newOffset, this.Count - (newOffset - this.Offset));

        public BufferSegment Slice(int offset, int count) => new BufferSegment(this.Data, offset, count);

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is BufferSegment))
                return false;

            return Equals((BufferSegment)obj);
        }

        public bool Equals(BufferSegment other) => this.Data == other.Data && this.Offset == other.Offset && this.Count == other.Count;
        public bool Equals(AutoReleaseBuffer other) => this.Data == other.Data && this.Offset == other.Offset && this.Count == other.Count;

        public override int GetHashCode() => (this.Data != null ? this.Data.GetHashCode() : 0) * 21 + this.Offset + this.Count;

        public static bool operator ==(AutoReleaseBuffer left, AutoReleaseBuffer right) => left.Equals(right);

        public static bool operator !=(AutoReleaseBuffer left, AutoReleaseBuffer right) => !left.Equals(right);

        public static bool operator ==(AutoReleaseBuffer left, BufferSegment right) => left.Equals(right);

        public static bool operator !=(AutoReleaseBuffer left, BufferSegment right) => !left.Equals(right);

        public static implicit operator byte[](AutoReleaseBuffer left) => left.Data;
        public static implicit operator BufferSegment(AutoReleaseBuffer left) => new BufferSegment(left.Data, left.Offset, left.Count);

        public override string ToString()
        {
            var sb = StringBuilderPool.Get(this.Count + 5);
            sb.Append("[AutoReleaseBuffer ");

            if (this.Count > 0)
            {
                sb.AppendFormat("Offset: {0:N0} ", this.Offset);
                sb.AppendFormat("Count: {0:N0} ", this.Count);
                sb.Append("Data: [");

                if (this.Count > 0)
                {
                    int dumpCount = Math.Min(this.Count, BufferSegment.ToStringMaxDumpLength);

                    sb.AppendFormat("{0:X2}", this.Data[this.Offset]);
                    for (int i = 1; i < dumpCount; ++i)
                        sb.AppendFormat(", {0:X2}", this.Data[this.Offset + i]);

                    if (this.Count > dumpCount)
                        sb.Append(", ...");
                }

                sb.Append("]]");
            }
            else
                sb.Append(']');

            return StringBuilderPool.ReleaseAndGrab(sb);
        }

        public void Dispose()
        {
            if (this.Data != null)
                BufferPool.Release(this.Data);
            this.Data = null;
        }
    }
}
