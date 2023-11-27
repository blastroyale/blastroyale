using System;
using System.Collections.Generic;
using System.IO;

using Best.HTTP.Shared.Databases.MetadataIndexFinders;

namespace Best.HTTP.Shared.Databases
{
    public struct MetadataStats
    {
        public int min;
        public int max;
        public int count;
        public int sum;
    }

    public abstract class MetadataService<MetadataType, ContentType> where MetadataType : Metadata, new()
    {
        public List<MetadataType> Metadatas { get; protected set; }
        public IndexingService<ContentType, MetadataType> IndexingService { get; protected set; }
        public IEmptyMetadataIndexFinder<MetadataType> EmptyMetadataIndexFinder { get; protected set; }

        protected MetadataService(IndexingService<ContentType, MetadataType> indexingService, IEmptyMetadataIndexFinder<MetadataType> emptyMetadataIndexFinder)
        {
            this.IndexingService = indexingService;
            this.EmptyMetadataIndexFinder = emptyMetadataIndexFinder;

            this.Metadatas = new List<MetadataType>();
        }

        /// <summary>
        /// Called when metadata loaded from file
        /// </summary>
        public virtual MetadataType CreateFrom(Stream stream)
        {
            var metadata = new MetadataType();

            metadata.Index = this.Metadatas.Count;
            metadata.LoadFrom(stream);

            this.Metadatas.Add(metadata);

            this.IndexingService.Index(metadata);

            return metadata;
        }

        /// <summary>
        /// Called by a concrete MetadataService implementation to create a new metadata
        /// </summary>
        protected MetadataType CreateDefault(ContentType content, int filePos, int length, Action<ContentType, MetadataType> setupCallback)
        {
            var metadata = new MetadataType();
            metadata.FilePosition = filePos;
            metadata.Length = length;

            metadata.Index = this.EmptyMetadataIndexFinder.FindFreeIndex(this.Metadatas);

            if (setupCallback != null)
                setupCallback(content, metadata);

            if (metadata.Index == this.Metadatas.Count)
                this.Metadatas.Add(metadata);
            else
                this.Metadatas[metadata.Index] = metadata;

            this.IndexingService.Index(metadata);

            return metadata;
        }

        public void Remove(MetadataType metadata)
        {
            this.IndexingService.Remove(metadata);

            // Mark metadata for deletion. Next time we save, it's not going to written out to disk.
            metadata.MarkForDelete();
        }

        public void SaveTo(Stream stream)
        {
            stream.SetLength(0);

            var optimizedIndexes = this.IndexingService.GetOptimizedIndexes();

            if (optimizedIndexes != null)
            {
                foreach (var index in optimizedIndexes)
                {
                    var metadata = this.Metadatas[index];

                    if (metadata.IsDeleted)
                        continue;

                    metadata.SaveTo(stream);
                }
            }
            else
            {
            for (int i = 0; i < this.Metadatas.Count; ++i)
            {
                var metadata = this.Metadatas[i];

                if (metadata.IsDeleted)
                    continue;

                metadata.SaveTo(stream);
            }
        }
        }

        public void LoadFrom(Stream stream)
        {
            while (stream.Position < stream.Length)
                CreateFrom(stream);
        }

        public virtual void Clear()
        {
            this.Metadatas.Clear();
            this.IndexingService.Clear();
        }

        public MetadataStats GetStats()
        {
            int min = int.MaxValue;
            int max = 0;
            int sum = 0;
            int count = 0;
            foreach (var metadata in this.Metadatas)
            {
                if (metadata.IsDeleted)
                    continue;

                if (metadata.Length > max)
                    max = metadata.Length;
                if (metadata.Length < min)
                    min = metadata.Length;
                sum += metadata.Length;
                count++;
            }

            return new MetadataStats
            {
                count = count,
                min = min,
                max = max,
                sum = sum
            };
        }
    }
}
