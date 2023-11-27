using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Shared.Databases
{
    public sealed class DiskManagerOptions
    {
        // avg. size of certificates is 1390 (calculated from 2621 intermediate certificate)
        public int MaxCacheSizeInBytes = 5 * 1024;

        public string HashDigest = "SHA256";
    }

    public interface IDiskContentParser<T>
    {
        T Parse(Stream stream, int length);
        void Encode(Stream stream, T content);
    }

    public sealed class DiskManager<T> : IDisposable
    {
        // TODO: store usage date/count and delete the oldest/least used?

        struct CachePointer<CacheType>
        {
            public static readonly CachePointer<CacheType> Empty = new CachePointer<CacheType> { Position = -1, Length = -1, Content = default(CacheType) };

            public int Position;
            public int Length;
            public CacheType Content;

            public override string ToString()
            {
                return $"[CachePointer<{this.Content.GetType().Name}>({Position}, {Length}, {Content})]";
            }
        }

        /// <summary>
        /// Sum size of the cached contents
        /// </summary>
        public int CacheSize { get; private set; }

        private Stream stream;

        private List<CachePointer<T>> cache = new List<CachePointer<T>>();
        private IDiskContentParser<T> diskContentParser;
        private DiskManagerOptions options;
        private FreeListManager freeListManager;

        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public DiskManager(Stream stream, Stream freeListStream, IDiskContentParser<T> contentParser, DiskManagerOptions options)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("DiskManager - stream can't seek!");

            this.stream = stream;
            this.freeListManager = new FreeListManager(freeListStream);
            this.diskContentParser = contentParser;
            this.options = options;
        }

        public (int, int) Append(T content)
        {
            using (new WriteLock(this.rwLock))
            {
                int pos = -1;
                int length = -1;

                using (var buffer = new BufferPoolMemoryStream())
                {
                    diskContentParser.Encode(buffer, content);

                    length = (int)buffer.Length;

                    var idx = this.freeListManager.FindFreeIndex(length);

                    if (idx >= 0)
                        pos = this.freeListManager.Occupy(idx, length);
                    else
                        pos = (int)this.stream.Length;

                    this.stream.Seek(pos, SeekOrigin.Begin);
                    buffer.Seek(0, SeekOrigin.Begin);
                    buffer.CopyTo(this.stream);
                }

                this.stream.Flush();

                return (pos, length);
            }
        }

        public void SaveChanged(Metadata metadata, T content)
        {
            using var _ = new WriteLock(this.rwLock);

            int pos = -1;
            int length = -1;

            using (var buffer = new BufferPoolMemoryStream())
            {
                diskContentParser.Encode(buffer, content);

                length = (int)buffer.Length;

                this.freeListManager.Add(metadata.FilePosition, metadata.Length);
                var idx = this.freeListManager.FindFreeIndex(length);

                if (idx >= 0)
                    pos = this.freeListManager.Occupy(idx, length);
                else
                    pos = (int)this.stream.Length;

                this.stream.Seek(pos, SeekOrigin.Begin);
                buffer.Seek(0, SeekOrigin.Begin);
                buffer.CopyTo(this.stream);
            }

            this.stream.Flush();

            metadata.FilePosition = pos;
            metadata.Length = length;
        }

        public void Delete(Metadata metadata)
        {
            using (new WriteLock(this.rwLock))
            {
                this.freeListManager.Add(metadata.FilePosition, metadata.Length);
                this.stream.Seek(metadata.FilePosition, SeekOrigin.Begin);

                var buffer = BufferPool.Get(BufferPool.MinBufferSize, true);
                Array.Clear(buffer, 0, (int)BufferPool.MinBufferSize);

                int length = metadata.Length;
                int iterationCount = length / (int)BufferPool.MinBufferSize;
                for (int i = 0; i < iterationCount; ++i)
                {
                    this.stream.Write(buffer, 0, (int)BufferPool.MinBufferSize);
                    length -= (int)BufferPool.MinBufferSize;
                }
                this.stream.Write(buffer, 0, length);

                this.stream.Flush();
                BufferPool.Release(buffer);
            }
        }

        public T Load(Metadata metadata)
        {
            using (new WriteLock(this.rwLock))
            {
                T parsedContent = default(T);

                var cachePointer = GetCached(metadata.FilePosition);
                if (cachePointer.Position != -1 && cachePointer.Content != null)
                    return cachePointer.Content;

                this.stream.Seek(metadata.FilePosition, SeekOrigin.Begin);

                parsedContent = diskContentParser.Parse(this.stream, metadata.Length);

                AddToCache(parsedContent, metadata.FilePosition, metadata.Length);

                return parsedContent;
            }
        }

        public List<KeyValuePair<Meta, T>> LoadAll<Meta>(List<Meta> metadatas) where Meta : Metadata
        {
            using (new WriteLock(this.rwLock))
            {
                if (metadatas == null || metadatas.Count == 0)
                    return null;

                metadatas.Sort((m1, m2) => m1.FilePosition.CompareTo(m2.FilePosition));

                List<KeyValuePair<Meta, T>> result = new List<KeyValuePair<Meta, T>>(metadatas.Count);

                for (int i = 0; i < metadatas.Count; ++i)
                {
                    var metadata = metadatas[i];

                    result.Add(new KeyValuePair<Meta, T>(metadata, Load(metadata)));
                }

                return result;
            }
        }

        public void Clear()
        {
            using (new WriteLock(this.rwLock))
            {
                this.freeListManager.Clear();
                this.stream.SetLength(0);
                this.stream.Flush();
                this.cache.Clear();
                this.CacheSize = 0;
            }
        }

        private CachePointer<T> GetCached(int position)
        {
            for (int i = 0; i < this.cache.Count; ++i)
            {
                var cache = this.cache[i];

                if (cache.Position == position)
                    return cache;
            }

            return CachePointer<T>.Empty;
        }

        private void AddToCache(T parsedContent, int pos, int length)
        {
            if (this.options.MaxCacheSizeInBytes >= length)
            {
                this.cache.Insert(0, new CachePointer<T>
                {
                    Position = pos,
                    Length = length,
                    Content = parsedContent
                });
                this.CacheSize += length;
            }
            
            while (this.CacheSize > this.options.MaxCacheSizeInBytes && this.cache.Count > 0)
            {
                var removingCache = this.cache[this.cache.Count - 1];
                this.cache.RemoveAt(this.cache.Count - 1);

                this.CacheSize -= removingCache.Length;
            }
        }

        public BufferSegment CalculateHash()
        {
            using (new WriteLock(this.rwLock))
            {
                this.stream.Seek(0, SeekOrigin.Begin);

#if UNITY_WEBGL && !UNITY_EDITOR
                var hash = System.Security.Cryptography.HashAlgorithm.Create(this.options.HashDigest);
                var result = hash.ComputeHash(this.stream);
                return new BufferSegment(result, 0, result.Length);
#elif !BESTHTTP_DISABLE_ALTERNATE_SSL
                var digest = Best.HTTP.SecureProtocol.Org.BouncyCastle.Security.DigestUtilities.GetDigest(this.options.HashDigest);

                byte[] buffer = BufferPool.Get(4 * 1024, true);
                int readCount = 0;
                while ((readCount = this.stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    digest.BlockUpdate(buffer, 0, readCount);
                }

                BufferPool.Release(buffer);

                byte[] result = BufferPool.Get(digest.GetDigestSize(), true);

                int length = digest.DoFinal(result, 0);
                return new BufferSegment(result, 0, length);
#else
                throw new NotImplementedException(nameof(CalculateHash));
#endif
            }
        }

        public void Save()
        {
            this.stream.Flush();
            this.freeListManager.Save();
        }

        public void Dispose()
        {
            this.freeListManager.Dispose();
            this.stream.Flush();
            this.stream.Close();
            this.stream = null;
            this.rwLock.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
