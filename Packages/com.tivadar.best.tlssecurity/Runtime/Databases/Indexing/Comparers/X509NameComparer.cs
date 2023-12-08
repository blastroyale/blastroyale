#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;

namespace Best.TLSSecurity.Databases.Indexing.Comparers
{
    public sealed class X509NameComparer : IComparer<X509Name>
    {
        public int Compare(X509Name x, X509Name y)
        {
            return string.CompareOrdinal(x.ToString(), y.ToString());
        }
    }
}
#endif
