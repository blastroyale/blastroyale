using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing.Comparers
{
    public sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            int result = x.Length.CompareTo(y.Length);
            if (result != 0)
                return result;

            for (int i = 0; i < x.Length; ++i)
            {
                result = x[i].CompareTo(y[i]);

                if (result != 0)
                    return result;
            }

            return 0;
        }
    }
}
