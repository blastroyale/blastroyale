#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.OCSP
{
    public class OCSPMetadataService : MetadataService<OCSPMetadata, OCSPCacheEntry>
    {
        public OCSPMetadataService(IndexingService<OCSPCacheEntry, OCSPMetadata> indexingService) : base(indexingService)
        {
        }

        public OCSPMetadata Create(byte[] hash, OCSPCacheEntry content, MetadataFlags flags, int filePos, int length)
        {
            return CreateDefault(content, flags, filePos, length, (c, metadata) =>
            {
                metadata.Hash = hash;
            });
        }
    }
}
#endif
