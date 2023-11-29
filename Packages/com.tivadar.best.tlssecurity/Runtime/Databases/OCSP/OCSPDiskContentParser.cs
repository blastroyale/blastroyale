#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.IO;

using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.TLSSecurity.Databases.OCSP
{
    public class OCSPDiskContentParser : IDiskContentParser<OCSPCacheEntry>
    {
        public void Encode(Stream stream, OCSPCacheEntry content)
        {
            stream.WriteByte((byte)content.Status);
            byte[] buffer = BufferPool.Get(8, true);

            ToBuffer(content.ReceivedAt.Ticks, buffer);
            stream.Write(buffer, 0, 8);

            ToBuffer(content.GeneratedAt.Ticks, buffer);
            stream.Write(buffer, 0, 8);

            ToBuffer(content.NextUpdate.Ticks, buffer);
            stream.Write(buffer, 0, 8);

            ToBuffer(content.LastUsed.Ticks, buffer);
            stream.Write(buffer, 0, 8);

            BufferPool.Release(buffer);
        }

        public OCSPCacheEntry Parse(Stream stream, int length)
        {
            Status status = (Status)stream.ReadByte();
            byte[] buffer = BufferPool.Get(8, true);

            stream.Read(buffer, 0, 8);
            DateTime ReceivedAt = ToDateTime(buffer);

            stream.Read(buffer, 0, 8);
            DateTime GeneratedAt = ToDateTime(buffer);

            stream.Read(buffer, 0, 8);
            DateTime NextUpdate = ToDateTime(buffer);

            stream.Read(buffer, 0, 8);
            DateTime LastUsed = ToDateTime(buffer);

            BufferPool.Release(buffer);

            return new OCSPCacheEntry()
            {
                Status = status,
                ReceivedAt = ReceivedAt,
                GeneratedAt = GeneratedAt,
                NextUpdate = NextUpdate,
                LastUsed = LastUsed
            };
        }

        private static void ToBuffer(long ticks, byte[] buffer)
        {
            buffer[0] = (byte)(ticks >> 56);
            buffer[1] = (byte)(ticks >> 48);
            buffer[2] = (byte)(ticks >> 40);
            buffer[3] = (byte)(ticks >> 32);
            buffer[4] = (byte)(ticks >> 24);
            buffer[5] = (byte)(ticks >> 16);
            buffer[6] = (byte)(ticks >> 8);
            buffer[7] = (byte)ticks;
        }

        private static DateTime ToDateTime(byte[] buffer)
        {
            long ticks = (long)buffer[0] << 56 |
                         (long)buffer[1] << 48 |
                         (long)buffer[2] << 40 |
                         (long)buffer[3] << 32 |
                         (long)buffer[4] << 24 |
                         (long)buffer[5] << 16 |
                         (long)buffer[6] << 8 |
                         (long)buffer[7];

            return DateTime.FromBinary(ticks);
        }
    }
}
#endif
