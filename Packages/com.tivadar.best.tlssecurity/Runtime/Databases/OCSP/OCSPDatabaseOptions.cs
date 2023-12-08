#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.OCSP
{
    public class OCSPDatabaseOptions : DatabaseOptions
    {
        public OCSPDatabaseOptions(string dbName) : base(dbName)
        {
            this.UseHashFile = false;
            this.DiskManager.MaxCacheSizeInBytes = 128;
        }
    }
}
#endif
