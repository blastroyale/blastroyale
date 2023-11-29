#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.X509
{
    public sealed class X509DatabaseOptions : DatabaseOptions
    {
        public X509DatabaseOptions(string dbName)
            :base(dbName)
        {
            this.UseHashFile = true;
        }
    }
}
#endif
