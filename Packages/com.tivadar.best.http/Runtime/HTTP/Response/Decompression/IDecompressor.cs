using System;

using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Response.Decompression
{
    public interface IDecompressor : IDisposable
    {
        (BufferSegment decompressed, bool releaseTheOld) Decompress(BufferSegment segment, bool forceDecompress, bool dataCanBeLarger, LoggingContext context);
    }
}
