using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.MetadataIndexFinders
{
    public sealed class DefaultEmptyMetadataIndexFinder<MetadataType> : IEmptyMetadataIndexFinder<MetadataType> where MetadataType : Metadata
    {
        public int FindFreeIndex(List<MetadataType> metadatas)
        {
            return metadatas.Count;
        }
    }
}
