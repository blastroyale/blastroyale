using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing.Comparers
{
    public sealed class StringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return x.CompareTo(y);
        }
    }
}
