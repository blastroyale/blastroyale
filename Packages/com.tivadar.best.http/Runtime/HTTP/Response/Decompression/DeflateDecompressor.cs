using System;

using Best.HTTP.Shared.Compression.Zlib;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Response.Decompression
{
    public sealed class DeflateDecompressor : IDecompressor
    {
        private BufferPoolMemoryStream decompressorInputStream;
        private BufferPoolMemoryStream decompressorOutputStream;
        private DeflateStream decompressorStream;

        private int MinLengthToDecompress = 256;

        public static bool IsSupported = true;

        public DeflateDecompressor(int minLengthToDecompress)
        {
            this.MinLengthToDecompress = minLengthToDecompress;
        }

        public (BufferSegment decompressed, bool releaseTheOld) Decompress(BufferSegment segment, bool forceDecompress, bool dataCanBeLarger, LoggingContext context)
        {
            if (decompressorInputStream == null)
                decompressorInputStream = new BufferPoolMemoryStream(segment.Count);

            if (segment.Data != null)
                decompressorInputStream.Write(segment.Data, segment.Offset, segment.Count);

            if (!forceDecompress && decompressorInputStream.Length < MinLengthToDecompress)
                return (BufferSegment.Empty, true);

            decompressorInputStream.Position = 0;

            if (decompressorStream == null)
            {
                // Had to change from this
                // _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.DEFLATE, leaveOpen);
                // this this:
                // _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
                // Because there are two bytes in the header additionally to what the deflate flavor expects that the zlib one handles fine.
                decompressorStream = new DeflateStream(decompressorInputStream,
                                                    CompressionMode.Decompress,
                                                    CompressionLevel.Default,
                                                    true);
                decompressorStream.FlushMode = FlushType.Sync;
            }

            if (decompressorOutputStream == null)
                decompressorOutputStream = new BufferPoolMemoryStream();
            decompressorOutputStream.SetLength(0);

            byte[] copyBuffer = BufferPool.Get(1024, true);

            int readCount;
            int sumReadCount = 0;
            while ((readCount = decompressorStream.Read(copyBuffer, 0, copyBuffer.Length)) != 0)
            {
                decompressorOutputStream.Write(copyBuffer, 0, readCount);
                sumReadCount += readCount;
            }

            BufferPool.Release(copyBuffer);

            // If no read is done (returned with any data) don't zero out the input stream, as it would delete any not yet used data.
            if (sumReadCount > 0)
                decompressorStream.SetLength(0);

            byte[] result = decompressorOutputStream.ToArray(dataCanBeLarger, context);

            return (new BufferSegment(result, 0, dataCanBeLarger ? (int)decompressorOutputStream.Length : result.Length), true);
        }

        ~DeflateDecompressor()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (decompressorInputStream != null)
                decompressorInputStream.Dispose();
            decompressorInputStream = null;

            if (decompressorOutputStream != null)
                decompressorOutputStream.Dispose();
            decompressorOutputStream = null;

            if (decompressorStream != null)
            {
                // If the decompressor closed before receiving data, or it's incomplete, disposing (eg. closing) it
                // throws an execption like this:
                // "Missing or incomplete GZIP trailer. Expected 8 bytes, got 0."
                try
                {
                    decompressorStream.Dispose();
                }
                catch
                { }
            }
            decompressorStream = null;

            GC.SuppressFinalize(this);
        }
    }
}
