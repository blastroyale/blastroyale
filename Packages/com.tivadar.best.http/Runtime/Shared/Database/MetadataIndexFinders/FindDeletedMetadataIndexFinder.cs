using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.MetadataIndexFinders
{
    public sealed class FindDeletedMetadataIndexFinder<MetadataType> : IEmptyMetadataIndexFinder<MetadataType> where MetadataType : Metadata
    {
        public int FindFreeIndex(List<MetadataType> metadatas)
        {
            for (int i = 0; i < metadatas.Count; ++i)
                if (metadatas[i].IsDeleted)
                    return i;

            return metadatas.Count;
        }
    }
}
