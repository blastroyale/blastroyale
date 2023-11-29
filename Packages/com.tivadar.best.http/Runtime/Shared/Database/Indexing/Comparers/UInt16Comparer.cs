using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing.Comparers
{
    public sealed class UInt16Comparer : IComparer<UInt16>
    {
        public int Compare(ushort x, ushort y)
        {
            return x.CompareTo(y);
        }
    }
}
