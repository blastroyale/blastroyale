#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using Best.TLSSecurity.Databases.Indexing;
using Best.TLSSecurity.Databases.Indexing.Comparers;
using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.OCSP
{
    public class OCSPIndexingService : IndexingService<OCSPCacheEntry, OCSPMetadata>
    {
        public static OCSPIndexingService Instance = new OCSPIndexingService();

        public AVLTree<byte[], int> index_Hash = new AVLTree<byte[], int>(new ByteArrayComparer());

        public override void Index(OCSPMetadata metadata)
        {
            base.Index(metadata);

            this.index_Hash.Add(metadata.Hash, metadata.Index);
        }

        public override void Clear()
        {
            base.Clear();

            this.index_Hash.Clear();
        }

        public override void Remove(OCSPMetadata metadata)
        {
            base.Remove(metadata);

            this.index_Hash.Remove(metadata.Hash, metadata.Index);
        }
    }
}
#endif
