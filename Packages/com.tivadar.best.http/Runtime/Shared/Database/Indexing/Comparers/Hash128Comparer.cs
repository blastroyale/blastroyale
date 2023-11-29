using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing.Comparers
{
    public sealed class Hash128Comparer : IComparer<UnityEngine.Hash128>
    {
        public int Compare(UnityEngine.Hash128 x, UnityEngine.Hash128 y)
        {
            return x.CompareTo(y);
        }
    }
}
