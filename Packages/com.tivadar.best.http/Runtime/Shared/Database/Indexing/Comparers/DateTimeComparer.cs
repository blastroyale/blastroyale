using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing.Comparers
{
    public sealed class DateTimeComparer : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            return x.CompareTo(y);
        }
    }
}
