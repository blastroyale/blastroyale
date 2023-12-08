using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.PlatformSupport.Memory
{
    /// <summary>
    /// Private data struct that contains the size - byte arrays mapping. 
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    struct BufferStore
    {
        /// <summary>
        /// Size/length of the arrays stored in the buffers.
        /// </summary>
        public readonly long Size;

        /// <summary>
        /// 
        /// </summary>
        public List<BufferDesc> buffers;

        public BufferStore(long size)
        {
            this.Size = size;
            this.buffers = new List<BufferDesc>();
        }

        /// <summary>
        /// Create a new store with its first byte[] to store.
        /// </summary>
        public BufferStore(long size, byte[] buffer)
            : this(size)
        {
            this.buffers.Add(new BufferDesc(buffer));
        }

        public override string ToString()
        {
            return string.Format("[BufferStore Size: {0:N0}, Buffers: {1}]", this.Size, this.buffers.Count);
        }
    }

    /// <summary>
    /// Helper struct for <see cref="BufferPool"/>.
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    struct BufferDesc
    {
        public static readonly BufferDesc Empty = new BufferDesc(null);

        /// <summary>
        /// The actual reference to the stored byte array.
        /// </summary>
        public byte[] buffer;

        /// <summary>
        /// When the buffer is put back to the pool. Based on this value the pool will calculate the age of the buffer.
        /// </summary>
        public DateTime released;

        public BufferDesc(byte[] buff)
        {
            this.buffer = buff;
            this.released = DateTime.Now;
        }
        public override string ToString()
        {
            return string.Format("[BufferDesc Size: {0}, Released: {1}]", this.buffer.Length, DateTime.Now - this.released);
        }
    }
}
