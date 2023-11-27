using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.MetadataIndexFinders
{
    public interface IEmptyMetadataIndexFinder<MetadataType> where MetadataType : Metadata
    {
        int FindFreeIndex(List<MetadataType> metadatas);
    }
}
