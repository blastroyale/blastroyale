#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Threading;

namespace Best.TLSSecurity.Databases.Shared
{
    public abstract class Database<ContentType, MetadataType, IndexingServiceType, MetadataServiceType> : IDisposable
        where MetadataType : Metadata, new()
        where IndexingServiceType : IndexingService<ContentType, MetadataType>
        where MetadataServiceType : MetadataService<MetadataType, ContentType>
    {
        public string SaveDir { get; private set; }
        public string Name { get { return this.Options.Name; } }

        public string MetadataFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), SecurityOptions.FolderAndFileOptions.MetadataExtension); } }
        public string DatabaseFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), SecurityOptions.FolderAndFileOptions.DatabaseExtension); } }
        public string DatabaseFreeListFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), SecurityOptions.FolderAndFileOptions.DatabaseFreeListExtension); } }
        public string HashFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), SecurityOptions.FolderAndFileOptions.HashExtension); } }

        public MetadataServiceType MetadataService { get; private set; }

        protected DatabaseOptions Options { get; private set; }
        protected IndexingServiceType IndexingService { get; private set; }        
        protected DiskManager<ContentType> DiskManager { get; private set; }

        protected bool isDirty = false;

        protected ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public Database(string directory,
            DatabaseOptions options,
            IndexingServiceType indexingService,
            IDiskContentParser<ContentType> diskContentParser,
            MetadataServiceType metadataService)
        {
            this.SaveDir = directory;
            this.Options = options;
            this.IndexingService = indexingService;
            this.MetadataService = metadataService;

            var dir = Path.GetDirectoryName(this.DatabaseFileName);
            if (!HTTPManager.IOService.DirectoryExists(dir))
                HTTPManager.IOService.DirectoryCreate(dir);

            this.DiskManager = new DiskManager<ContentType>(
                HTTPManager.IOService.CreateFileStream(this.DatabaseFileName, FileStreamModes.OpenReadWrite),
                HTTPManager.IOService.CreateFileStream(this.DatabaseFreeListFileName, FileStreamModes.OpenReadWrite),
                diskContentParser,
                options.DiskManager);

            using (var fileStream = HTTPManager.IOService.CreateFileStream(this.MetadataFileName, FileStreamModes.OpenReadWrite))
                using (var stream = new BufferedStream(fileStream))
                    this.MetadataService.LoadFrom(stream);
        }

        public int Clear(bool keepUserAdded)
        {
            using (new WriteLock(this.rwlock))
            {
                // get all non-locked and non-user added indexes
                MetadataFlags flags = MetadataFlags.Locked;
                if (keepUserAdded)
                    flags |= MetadataFlags.UserAdded;

                var metadataIndexes = this.IndexingService.FindByFlags(~flags);

                // delete them
                int deletedCount = Delete(metadataIndexes);

                Save();

                return deletedCount;
            }
        }

        public int Delete(IEnumerable<MetadataType> metadatas)
        {
            if (metadatas == null)
                return 0;

            using (new WriteLock(this.rwlock))
            {
                int deletedCount = 0;
                foreach (var metadata in metadatas)
                    if (DeleteMetadata(metadata))
                        deletedCount++;

                this.isDirty = deletedCount > 0;

                Save();

                return deletedCount;
            }
        }

        public int Delete(IEnumerable<int> metadataIndexes)
        {
            if (metadataIndexes == null)
                return 0;

            using (new WriteLock(this.rwlock))
            {
                int deletedCount = 0;
                foreach (int idx in metadataIndexes)
                {
                    var metadata = this.MetadataService.Metadatas[idx];

                    if (DeleteMetadata(metadata))
                        deletedCount++;
                }

                this.isDirty = deletedCount > 0;

                Save();

                return deletedCount;
            }
        }

        protected bool DeleteMetadata(MetadataType metadata)
        {
            if (!metadata.IsLocked)
            {
                this.DiskManager.Delete(metadata);
                this.MetadataService.Remove(metadata);
            }

            return !metadata.IsLocked;
        }

        public void Save()
        {
            if (!this.isDirty)
                return;

            using (new WriteLock(this.rwlock))
            {
                using(var fileStream = HTTPManager.IOService.CreateFileStream(this.MetadataFileName, FileStreamModes.Create))
                    using (var stream = new BufferedStream(fileStream))
                        this.MetadataService.SaveTo(stream);

                if (this.Options.UseHashFile)
                {
                    using (var hashStream = HTTPManager.IOService.CreateFileStream(this.HashFileName, FileStreamModes.Create))
                    {
                        var hash = this.DiskManager.CalculateHash();
                        hashStream.Write(hash.Data, 0, hash.Count);
                        BufferPool.Release(hash);
                    }
                }

                this.DiskManager.Save();

                this.isDirty = false;
            }
        }

        public List<ContentType> FromMetadatas(IEnumerable<MetadataType> metadatas) => FromMetadataIndexes((from m in metadatas select m.Index).ToList());

        public List<ContentType> FromMetadataIndexes(List<int> metadataIndexes)
        {
            using (new ReadLock(this.rwlock))
            {
                if (metadataIndexes != null && metadataIndexes.Count > 0)
                {
                    var result = new List<ContentType>();

                    foreach (int metadataIndex in metadataIndexes)
                    {
                        var metadata = this.MetadataService.Metadatas[metadataIndex];
                        var content = this.DiskManager.Load(metadata);
                        result.Add(content);
                    }

                    return result;
                }

                return null;
            }
        }

        public void Dispose()
        {
            Save();
            this.DiskManager.Dispose();
            this.rwlock.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
#endif
