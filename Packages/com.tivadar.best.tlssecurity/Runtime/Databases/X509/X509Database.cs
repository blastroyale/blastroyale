#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509.Store;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections;

namespace Best.TLSSecurity.Databases.X509
{
    public sealed class X509Database : Database<X509Certificate, X509Metadata, X509DatabaseIndexingService, x509MetadataService>, IStore<X509Certificate> //IX509Store
    {
        public X509Database(string directory,
            DatabaseOptions options,
            X509DatabaseIndexingService indexingService,
            IDiskContentParser<X509Certificate> diskContentParser,
            x509MetadataService metadataService)
            : base(directory, options, indexingService, diskContentParser, metadataService)
        {

        }

        public List<X509Certificate> FindBySubjectDN(X509Name subject) => FromMetadataIndexes(this.IndexingService.index_Subject.Find(subject));
        public List<X509Certificate> FindBySubjectKeyIdentifier(byte[] subjectKeyIdentifier) => FromMetadataIndexes(this.IndexingService.index_SubjectKeyIdentifier.Find(subjectKeyIdentifier));

        public void Add(X509Certificate cert, MetadataFlags flags)
        {
            using (new WriteLock(this.rwlock))
            {
                (int filePos, int length) = this.DiskManager.Append(cert);
                this.MetadataService.Create(cert, flags, filePos, length);

                this.isDirty = true;
            }
        }

        public void CompactAndSave()
        {
            using (new WriteLock(this.rwlock))
            {
                // 1.
                var all = new List<X509Metadata>(from m in this.MetadataService.Metadatas where !m.IsDeleted select m);

                // 2.
                var contents = this.DiskManager.LoadAll(all);

                // 3.
                ClearEverything();

                // 4
                if (contents != null)
                    foreach (var kvp in contents)
                        Add(kvp.Value, kvp.Key.Flags);

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

        public ICollection GetMatches(ISelector<X509Certificate> s)
        {
            using (new ReadLock(this.rwlock))
            {
                var selector = s as X509CertStoreSelector;

                List<X509Certificate> result = null;
                if (result == null && selector.Subject != null)
                    result = FindBySubjectDN(selector.Subject);

                if (result == null && selector.SubjectKeyIdentifier != null)
                    result = FindBySubjectKeyIdentifier(selector.SubjectKeyIdentifier);

                return result;
            }
        }

        public IEnumerable<X509Certificate> EnumerateMatches(ISelector<X509Certificate> s)
        {
            using (new ReadLock(this.rwlock))
            {
                var selector = s as X509CertStoreSelector;

                List<X509Certificate> result = null;
                if (result == null && selector.Subject != null)
                    result = FindBySubjectDN(selector.Subject);

                if (result == null && selector.SubjectKeyIdentifier != null)
                    result = FindBySubjectKeyIdentifier(selector.SubjectKeyIdentifier);

                return result;
            }
        }
    }
}
#endif
