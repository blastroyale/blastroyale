#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.IO;
using System.Linq;

using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace Best.TLSSecurity.Databases.OCSP
{
    public sealed class OCSPDatabase : Database<OCSPCacheEntry, OCSPMetadata, OCSPIndexingService, OCSPMetadataService>
    {
        public OCSPDatabase(string rootFolder)
            :this(Path.Combine(rootFolder, SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.OCSP.OCSPCache.FolderName),
                 SecurityOptions.OCSP.OCSPCache.DatabaseOptions,
                 OCSPIndexingService.Instance,
                 new OCSPDiskContentParser(),
                 new OCSPMetadataService(OCSPIndexingService.Instance))
        { }

        public OCSPDatabase(string directory, DatabaseOptions options, OCSPIndexingService indexingService, IDiskContentParser<OCSPCacheEntry> diskContentParser, OCSPMetadataService metadataService)
            : base(directory, options, indexingService, diskContentParser, metadataService)
        {
        }

        public (byte[], OCSPCacheEntry) FindBy(X509Certificate certificate)
        {
            var hash = DigestUtilities.CalculateDigest("SHA-384", certificate.GetEncoded());

            using (new ReadLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.index_Hash.Find(hash);

                if (metadataIndexes == null || metadataIndexes.Count == 0)
                    return (hash, null);

                if (metadataIndexes.Count > 1)
                {
                    throw new Exception($"Hash({Hex.ToHexString(hash)}) collision for certificate({certificate})!");
                }

                return (hash, FromMetadataIndexes(metadataIndexes).FirstOrDefault());
            }            
        }

        public void Set(byte[] hash, OCSPCacheEntry cacheEntry)
        {
            using (new WriteLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.index_Hash.Find(hash);
                if (metadataIndexes == null)
                {
                    (int filePos, int length) = this.DiskManager.Append(cacheEntry);
                    this.MetadataService.Create(hash, cacheEntry, MetadataFlags.None, filePos, length);
                }
                else
                {
                    var index = metadataIndexes.FirstOrDefault();
                    var metadata = this.MetadataService.Metadatas[index];

                    // delete from the disk only, we want to keep the indexes
                    this.DiskManager.Delete(metadata);
                    (int filePos, int length) = this.DiskManager.Append(cacheEntry);

                    metadata.FilePosition = filePos;
                    metadata.Length = length;
                }

                this.isDirty = true;

                Save();
            }
        }

        public void UpdateIfNotRevoked(byte[] hash, OCSPCacheEntry cacheEntry)
        {
            using (new WriteLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.index_Hash.Find(hash);
                if (metadataIndexes == null)
                {
                    (int filePos, int length) = this.DiskManager.Append(cacheEntry);
                    this.MetadataService.Create(hash, cacheEntry, MetadataFlags.None, filePos, length);
                }
                else
                {
                    var index = metadataIndexes.FirstOrDefault();
                    var metadata = this.MetadataService.Metadatas[index];

                    var oldEntry = this.DiskManager.Load(metadata);

                    if (oldEntry != null && oldEntry.Status != Status.Revoked)
                    {
                        // delete from the disk only, we want to keep the indexes
                        this.DiskManager.Delete(metadata);
                        (int filePos, int length) = this.DiskManager.Append(cacheEntry);

                        metadata.FilePosition = filePos;
                        metadata.Length = length;
                    }
                }

                this.isDirty = true;

                Save();
            }
        }
    }
}
#endif
