using System;
using System.IO;
using System.Text;

using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.Databases.Utils
{
    public static class StreamUtil
    {
        public static void WriteLengthPrefixedString(this Stream stream, string str)
        {
            if (str != null)
            {
                var byteCount = Encoding.UTF8.GetByteCount(str);

                if (byteCount >= 1 << 16)
                    throw new ArgumentException($"byteCount({byteCount})");

                stream.EncodeUnsignedVariableByteInteger((ulong)byteCount);

                byte[] tmp = BufferPool.Get(byteCount, true);

                Encoding.UTF8.GetBytes(str, 0, str.Length, tmp, 0);
                stream.Write(tmp, 0, byteCount);

                BufferPool.Release(tmp);
            }
            else
            {
                stream.WriteByte(0);
            }
        }

        public static string ReadLengthPrefixedString(this Stream stream)
        {
            int strLength = (int)stream.DecodeUnsignedVariableByteInteger();
            string result = null;

            if (strLength != 0)
            {
                byte[] buffer = BufferPool.Get(strLength, true);

                stream.Read(buffer, 0, strLength);
                result = System.Text.Encoding.UTF8.GetString(buffer, 0, strLength);

                BufferPool.Release(buffer);
            }

            return result;
        }

        public static void EncodeUnsignedVariableByteInteger(this Stream encodeTo, ulong value)
        {
            if (value < 0)
                throw new NotSupportedException($"Can't encode negative value({value:N0})!");

            byte encodedByte;
            do
            {
                encodedByte = (byte)(value % 128);
                value /= 128;
                // if there are more data to encode, set the top bit of this byte
                if (value > 0)
                    encodedByte = (byte)(encodedByte | 128);

                encodeTo.WriteByte(encodedByte);
            }
            while (value > 0);
        }

        public static ulong DecodeUnsignedVariableByteInteger(this Stream decodeFrom)
        {
            ulong multiplier = 1;
            ulong value = 0;
            byte encodedByte = 0;
            do
            {
                encodedByte = (byte)decodeFrom.ReadByte();
                value += (ulong)((ulong)(encodedByte & 127) * multiplier);
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }

        public static void EncodeSignedVariableByteInteger(this Stream encodeTo, long value)
        {
            bool more = true;

            while (more)
            {
                byte chunk = (byte)(value & 0x7fL); // extract a 7-bit chunk
                value >>= 7;

                bool signBitSet = (chunk & 0x40) != 0; // sign bit is the msb of a 7-bit byte, so 0x40
                more = !((value == 0 && !signBitSet) || (value == -1 && signBitSet));
                if (more) { chunk |= 0x80; } // set msb marker that more bytes are coming

                encodeTo.WriteByte(chunk);
            };
        }

        public static long DecodeSignedVariableByteInteger(this Stream stream)
        {
            long value = 0;
            int shift = 0;
            bool more = true;
            bool signBitSet = false;

            while (more)
            {
                var next = stream.ReadByte();
                if (next < 0)
                    throw new InvalidOperationException("Unexpected end of stream");

                byte b = (byte)next;

                more = (b & 0x80) != 0; // extract msb
                signBitSet = (b & 0x40) != 0; // sign bit is the msb of a 7-bit byte, so 0x40

                long chunk = b & 0x7fL; // extract lower 7 bits
                value |= chunk << shift;
                shift += 7;
            };

            // extend the sign of shorter negative numbers
            if (shift < (sizeof(long) * 8) && signBitSet) { value |= -1L << shift; }

            return value;
        }
    }
}
