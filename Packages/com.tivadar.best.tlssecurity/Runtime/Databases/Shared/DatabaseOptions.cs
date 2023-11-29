#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
namespace Best.TLSSecurity.Databases.Shared
{
    public class DatabaseOptions
    {
        public string Name;
        public bool UseHashFile;

        public DiskManagerOptions DiskManager = new DiskManagerOptions();

        public DatabaseOptions(string dbName)
        {
            this.Name = dbName;
        }
    }
}
#endif
