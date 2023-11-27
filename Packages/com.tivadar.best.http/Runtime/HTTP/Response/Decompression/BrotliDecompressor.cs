using System;

using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Response.Decompression
{
    public sealed class BrotliDecompressor : IDecompressor
    {
#if ((NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))) && !BESTHTTP_DISABLE_BROTLI
        private BufferSegmentStream decompressorInputStream;
        private BufferPoolMemoryStream decompressorOutputStream;
        private System.IO.Compression.BrotliStream decompressorStream;

        byte[] copyBuffer = null;
#endif

        private int _minLengthToDecompress;

        public static bool IsSupported()
        {
            // Not enabled under android with the mono runtime
#if ((NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))) && !BESTHTTP_DISABLE_BROTLI
            return true;
#else
            return false;
#endif
        }

        public BrotliDecompressor(int minLengthToDecompress)
        {
            this._minLengthToDecompress = minLengthToDecompress;
        }

        public (BufferSegment decompressed, bool releaseTheOld) Decompress(BufferSegment segment, bool forceDecompress, bool dataCanBeLarger, LoggingContext context)
        {
#if ((NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))) && !BESTHTTP_DISABLE_BROTLI
            if (decompressorInputStream == null)
                decompressorInputStream = new BufferSegmentStream();

            if (segment.Data != null)
                decompressorInputStream.Write(segment);

            if (!forceDecompress && decompressorInputStream.Length < _minLengthToDecompress)
                return (BufferSegment.Empty, false);

            if (decompressorStream == null)
            {
                decompressorStream = new System.IO.Compression.BrotliStream(decompressorInputStream,
                                                             System.IO.Compression.CompressionMode.Decompress,
                                                             true);
            }

            if (decompressorOutputStream == null)
                decompressorOutputStream = new BufferPoolMemoryStream();
            decompressorOutputStream.SetLength(0);

            if (copyBuffer == null)
                copyBuffer = BufferPool.Get(4 * 1024, true);

            int readCount;
            int sumReadCount = 0;
            while ((readCount = decompressorStream.Read(copyBuffer, 0, copyBuffer.Length)) != 0)
            {
                decompressorOutputStream.Write(copyBuffer, 0, readCount);
                sumReadCount += readCount;
            }

            byte[] result = decompressorOutputStream.ToArray(dataCanBeLarger, context);

            return (new BufferSegment(result, 0, dataCanBeLarger ? (int)decompressorOutputStream.Length : result.Length), false);
#else
            return (BufferSegment.Empty, false);
#endif
        }

        public void Dispose()
        {
#if ((NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))) && !BESTHTTP_DISABLE_BROTLI
            if (decompressorStream != null)
                decompressorStream.Dispose();
            decompressorStream = null;

            if (decompressorInputStream != null)
                decompressorInputStream.Dispose();
            decompressorInputStream = null;

            if (decompressorOutputStream != null)
                decompressorOutputStream.Dispose();
            decompressorOutputStream = null;

            BufferPool.Release(copyBuffer);
            copyBuffer = null;
#endif
        }
    }
}
