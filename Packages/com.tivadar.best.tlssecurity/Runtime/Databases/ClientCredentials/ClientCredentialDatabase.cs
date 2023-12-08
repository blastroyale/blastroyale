#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Linq;
using System.Collections.Generic;

using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;

namespace Best.TLSSecurity.Databases.ClientCredentials
{
    public sealed class ClientCredentialDatabase : Database<ClientCredential, ClientCredentialMetadata, ClientCredentialIndexingService, ClientCredentialsMetadataService>
    {
        public ClientCredentialDatabase(string directory,
            ClientCredentialDatabaseOptions options,
            ClientCredentialIndexingService indexingService,
            IDiskContentParser<ClientCredential> diskContentParser,
            ClientCredentialsMetadataService metadataService)
            : base(directory, options, indexingService, diskContentParser, metadataService)
        {
        }

        public List<ClientCredential> FindByAuthority(X509Name authority) => FromMetadataIndexes(this.IndexingService.index_Authority.Find(authority));
        public List<ClientCredential> FindByTargetDomain(string domain) => FromMetadataIndexes(this.IndexingService.index_TargetDomain.Find(domain));

        public void Add(string targetDomain, ClientCredential content)
        {
            using (new WriteLock(this.rwlock))
            {
                (int filePos, int length) = this.DiskManager.Append(content);

                /*MetadataType metadata = */
                (this.MetadataService as ClientCredentialsMetadataService).Create(targetDomain, content, MetadataFlags.UserAdded, filePos, length);

                this.isDirty = true;

                Save();
            }
        }

        public void CompactAndSave()
        {
            using (new WriteLock(this.rwlock))
            {
                // 1.
                var all = new List<ClientCredentialMetadata>(from m in this.MetadataService.Metadatas where !m.IsDeleted select m);

                // 2.
                var contents = this.DiskManager.LoadAll(all);

                // 3.
                ClearEverything();

                // 4
                if (contents != null)
                    foreach (var kvp in contents)
                        Add(kvp.Key.TargetDomain, kvp.Value);

                Save();
            }
        }

        void ClearEverything()
        {
            using (new WriteLock(this.rwlock))
            {
                this.MetadataService.Clear();
                this.DiskManager.Clear();

                this.isDirty = true;

                Save();
            }
        }
    }
}
#endif
