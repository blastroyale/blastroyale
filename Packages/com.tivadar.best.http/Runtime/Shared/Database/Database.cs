using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Threading;

namespace Best.HTTP.Shared.Databases
{
    public sealed class FolderAndFileOptions
    {
        public string FolderName = "Best.HTTP.Shared.Databases";
        public string DatabaseFolderName = "Databases";
        public string MetadataExtension = "metadata";
        public string DatabaseExtension = "db";
        public string DatabaseFreeListExtension = "freelist";
        public string HashExtension = "hash";
    }

    public abstract class Database<ContentType, MetadataType, IndexingServiceType, MetadataServiceType> : IDisposable, IHeartbeat
        where MetadataType : Metadata, new()
        where IndexingServiceType : IndexingService<ContentType, MetadataType>
        where MetadataServiceType : MetadataService<MetadataType, ContentType>
    {
        public static FolderAndFileOptions FolderAndFileOptions = new FolderAndFileOptions();

        public string SaveDir { get; private set; }
        public string Name { get { return this.Options.Name; } }

        public string MetadataFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), FolderAndFileOptions.MetadataExtension); } }
        public string DatabaseFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), FolderAndFileOptions.DatabaseExtension); } }
        public string DatabaseFreeListFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), FolderAndFileOptions.DatabaseFreeListExtension); } }
        public string HashFileName { get { return Path.ChangeExtension(Path.Combine(this.SaveDir, this.Name), FolderAndFileOptions.HashExtension); } }

        public MetadataServiceType MetadataService { get; private set; }

        protected DatabaseOptions Options { get; private set; }
        protected IndexingServiceType IndexingService { get; private set; }        
        protected DiskManager<ContentType> DiskManager { get; private set; }

        protected int isDirty = 0;

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
                HTTPManager.IOService.CreateFileStream(this.DatabaseFileName, Best.HTTP.Shared.PlatformSupport.FileSystem.FileStreamModes.OpenReadWrite),
                HTTPManager.IOService.CreateFileStream(this.DatabaseFreeListFileName, Best.HTTP.Shared.PlatformSupport.FileSystem.FileStreamModes.OpenReadWrite),
                diskContentParser,
                options.DiskManager);

            using (var fileStream = HTTPManager.IOService.CreateFileStream(this.MetadataFileName, Best.HTTP.Shared.PlatformSupport.FileSystem.FileStreamModes.OpenReadWrite))
                using (var stream = new BufferedStream(fileStream))
                    this.MetadataService.LoadFrom(stream);
        }

        public int Clear()
        {
            using (new WriteLock(this.rwlock))
            {
                int count = this.MetadataService.Metadatas.Count;

                this.IndexingService.Clear();
                this.DiskManager.Clear();
                this.MetadataService.Clear();
                FlagDirty(1);

                return count;
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

                FlagDirty(deletedCount);

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

                FlagDirty(deletedCount);

                return deletedCount;
            }
        }

        protected bool DeleteMetadata(MetadataType metadata)
        {
            if (metadata.Length > 0)
                this.DiskManager.Delete(metadata);
            this.MetadataService.Remove(metadata);
            FlagDirty(1);

            return true;
        }

        /// <summary>
        /// Loads the first content from the metadata indexes.
        /// </summary>
        public ContentType FromFirstMetadataIndex(IEnumerable<int> metadataIndexes)
        {
            if (metadataIndexes == null)
                return default;

            var index = metadataIndexes.DefaultIfEmpty(-1).First();
            if (index < 0)
                return default;

            return FromMetadataIndex(index);
        }

        /// <summary>
        /// Loads the content from the metadata index.
        /// </summary>
        public ContentType FromMetadataIndex(int metadataIndex)
        {
            if (metadataIndex < 0 || metadataIndex >= this.MetadataService.Metadatas.Count)
                return default;

            //using (new ReadLock(this.rwlock))
            {
                var metadata = this.MetadataService.Metadatas[metadataIndex];
                return this.DiskManager.Load(metadata);
            }
        }

        public ContentType FromMetadata(MetadataType metadata) => this.DiskManager.Load(metadata);

        /// <summary>
        /// Loads all content from the metadatas.
        /// </summary>
        public IEnumerable<ContentType> FromMetadatas(IEnumerable<MetadataType> metadatas) => FromMetadataIndexes(from m in metadatas select m.Index);

        /// <summary>
        /// Loads all content from the metadata indexes.
        /// </summary>
        public IEnumerable<ContentType> FromMetadataIndexes(IEnumerable<int> metadataIndexes)
        {
            if (metadataIndexes == null)
                yield break;

            //using (new ReadLock(this.rwlock))
            {
                foreach (int metadataIndex in metadataIndexes)
                {
                    var metadata = this.MetadataService.Metadatas[metadataIndex];
                    var content = this.DiskManager.Load(metadata);
                    //result.Add(content);

                    yield return content;
                }
            }
        }

        protected void FlagDirty(int dirty)
        {
            if (dirty != 0 && Interlocked.CompareExchange(ref this.isDirty, dirty, 0) == 0)
                HTTPManager.Heartbeats.Subscribe(this);
        }

        public bool Save()
        {
            if (!this.rwlock.TryEnterWriteLock(TimeSpan.FromMilliseconds(0)))
                return false;

            try
            {
                int itWasDirty = Interlocked.CompareExchange(ref this.isDirty, 0, 1);
                if (itWasDirty == 0)
                    return true;

                using (var fileStream = HTTPManager.IOService.CreateFileStream(this.MetadataFileName, Best.HTTP.Shared.PlatformSupport.FileSystem.FileStreamModes.Create))
                using (var stream = new BufferedStream(fileStream))
                    this.MetadataService.SaveTo(stream);

                if (this.Options.UseHashFile)
                {
                    using (var hashStream = HTTPManager.IOService.CreateFileStream(this.HashFileName, Best.HTTP.Shared.PlatformSupport.FileSystem.FileStreamModes.Create))
                    {
                        var hash = this.DiskManager.CalculateHash();
                        hashStream.Write(hash.Data, 0, hash.Count);
                        BufferPool.Release(hash);
                    }
                }

                this.DiskManager.Save();

                Interlocked.Exchange(ref this.isDirty, 0);

                return true;
            }
            finally
            {
                this.rwlock.ExitWriteLock();
            }
        }

        void IHeartbeat.OnHeartbeatUpdate(DateTime now, TimeSpan dif)
        {
            if (this.Save())
                HTTPManager.Heartbeats.Unsubscribe(this);
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
