using System;

using Best.HTTP.Shared.PlatformSupport.Text;

namespace Best.HTTP.Shared.PlatformSupport.Memory
{
    /// <summary>
    /// Represents a segment (a continuous section) of a byte array, providing functionalities to 
    /// work with a portion of the data without copying.
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public readonly struct BufferSegment
    {
        internal const int ToStringMaxDumpLength = 128;

        /// <summary>
        /// Represents an empty buffer segment.
        /// </summary>
        public static readonly BufferSegment Empty = new BufferSegment(null, 0, 0);

        /// <summary>
        /// The underlying data of the buffer segment.
        /// </summary>
        public readonly byte[] Data;

        /// <summary>
        /// The starting offset of the segment within the data.
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// The number of bytes in the segment that contain valid data.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Initializes a new instance of the BufferSegment struct.
        /// </summary>
        /// <param name="data">The data for the buffer segment.</param>
        /// <param name="offset">The starting offset of the segment.</param>
        /// <param name="count">The number of bytes in the segment.</param>
        public BufferSegment(byte[] data, int offset, int count)
        {
            this.Data = data;
            this.Offset = offset;
            this.Count = count;
        }

        /// <summary>
        /// Converts the buffer segment to an AutoReleaseBuffer to use it in a local using statement.
        /// </summary>
        /// <returns>A new AutoReleaseBuffer instance containing the data of the buffer segment.</returns>
        public AutoReleaseBuffer AsAutoRelease() => new AutoReleaseBuffer(this);

        /// <summary>
        /// Creates a new segment starting from the specified offset.
        /// </summary>
        /// <remarks>The new segment will reference the same underlying byte[] as the original, without creating a copy of the data.</remarks>
        /// <param name="newOffset">The starting offset of the new segment.</param>
        /// <returns>A new buffer segment that references the same underlying data.</returns>
        public BufferSegment Slice(int newOffset) => new BufferSegment(this.Data, newOffset, this.Count - (newOffset - this.Offset));

        /// <summary>
        /// Creates a new segment with the specified offset and count.
        /// </summary>
        /// <remarks>The new segment will reference the same underlying byte[] as the original, without creating a copy of the data.</remarks>
        /// <param name="offset">The starting offset of the new segment.</param>
        /// <param name="count">The number of bytes in the new segment.</param>
        /// <returns>A new buffer segment that references the same underlying data.</returns>
        public BufferSegment Slice(int offset, int count) => new BufferSegment(this.Data, offset, count);

        /// <summary>
        /// Copyies the buffer's content to the received array.
        /// </summary>
        /// <param name="to">The array the data will be copied into.</param>
        public void CopyTo(byte[] to) => Array.Copy(this.Data, this.Offset, to, 0, this.Count);

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is BufferSegment))
                return false;

            return Equals((BufferSegment)obj);
        }

        public bool Equals(BufferSegment other) => this.Data == other.Data && this.Offset == other.Offset && this.Count == other.Count;
        public bool Equals(AutoReleaseBuffer other) => this.Data == other.Data && this.Offset == other.Offset && this.Count == other.Count;

        public override int GetHashCode() => (this.Data != null ? this.Data.GetHashCode() : 0) * 21 + this.Offset + this.Count;

        public static bool operator ==(BufferSegment left, BufferSegment right) => left.Equals(right);
        public static bool operator !=(BufferSegment left, BufferSegment right) => !left.Equals(right);
        public static bool operator ==(BufferSegment left, AutoReleaseBuffer right) => left.Equals(right);
        public static bool operator !=(BufferSegment left, AutoReleaseBuffer right) => !left.Equals(right);
        public static implicit operator byte[](BufferSegment left) => left.Data;

        public override string ToString()
        {
            var sb = StringBuilderPool.Get(this.Count + 5);
            sb.Append("[BufferSegment ");

            if (this.Count > 0)
            {
                sb.AppendFormat("Offset: {0:N0} ", this.Offset);
                sb.AppendFormat("Count: {0:N0} ", this.Count);
                sb.Append("Data: [");

                if (this.Count > 0)
                {
                    int dumpCount = Math.Min(this.Count, ToStringMaxDumpLength);

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
    }
}
