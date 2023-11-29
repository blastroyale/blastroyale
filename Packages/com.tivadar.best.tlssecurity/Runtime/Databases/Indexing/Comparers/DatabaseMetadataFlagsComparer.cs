#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;

using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.Indexing.Comparers
{
    public sealed class DatabaseMetadataFlagsComparer : IComparer<MetadataFlags>
    {
        public int Compare(MetadataFlags x, MetadataFlags y)
        {
            return ((byte)x).CompareTo((byte)y);
        }
    }
}
#endif
