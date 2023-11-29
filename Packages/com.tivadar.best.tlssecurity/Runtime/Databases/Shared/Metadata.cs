#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.IO;

using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.TLSSecurity.Databases.Shared
{
    [Flags]
    public enum MetadataFlags : byte
    {
        None        = 0x01,
        UserAdded   = 0x02,
        Locked      = 0x04
    }

    public abstract class Metadata
    {
        public int Index;
        public int FilePosition;
        public int Length;

        public bool IsDeleted => this.FilePosition == -1 && this.Length == -1;

        public MetadataFlags Flags;

        public bool IsUserAdded { get { return HasFlag(MetadataFlags.UserAdded); } set { SetFlag(MetadataFlags.UserAdded, value); } }
        public bool IsLocked { get { return HasFlag(MetadataFlags.Locked); } set { SetFlag(MetadataFlags.Locked, value); } }

        public void MarkForDelete()
        {
            this.FilePosition = -1;
            this.Length = -1;
        }

        public bool HasFlag(MetadataFlags flag) => (this.Flags & flag) == flag;
        public void SetFlag(MetadataFlags flag, bool on) => this.Flags = (this.Flags ^ flag) & (MetadataFlags)((on ? 1 : 0) * (byte)flag);

        public virtual void SaveTo(Stream stream)
        {
            stream.WriteByte((byte)(this.FilePosition >> 16));
            stream.WriteByte((byte)(this.FilePosition >> 8));
            stream.WriteByte((byte)(this.FilePosition));

            stream.WriteByte((byte)(this.Length >> 16));
            stream.WriteByte((byte)(this.Length >> 8));
            stream.WriteByte((byte)(this.Length));

            stream.WriteByte((byte)(this.Flags));
        }

        public virtual void LoadFrom(Stream stream)
        {
            var buff = BufferPool.Get(7, true);
            stream.Read(buff, 0, 7);

            this.FilePosition = buff[0] << 16 | buff[1] << 8 | buff[2];
            this.Length = buff[3] << 16 | buff[4] << 8 | buff[5];

            this.Flags = (MetadataFlags)buff[6];

            BufferPool.Release(buff);
        }

        public override string ToString()
        {
            return $"[Metadata Idx: {Index}, Pos: {FilePosition}, Length: {Length}, Flags: {Flags}, IsDeleted: {IsDeleted}]";
        }
    }
}
#endif
