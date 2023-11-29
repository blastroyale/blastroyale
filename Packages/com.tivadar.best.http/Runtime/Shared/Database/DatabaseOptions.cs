namespace Best.HTTP.Shared.Databases
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
