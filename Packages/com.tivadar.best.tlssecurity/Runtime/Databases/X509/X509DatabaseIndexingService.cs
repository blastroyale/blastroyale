#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;

using Best.TLSSecurity.Databases.Indexing;
using Best.TLSSecurity.Databases.Indexing.Comparers;
using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace Best.TLSSecurity.Databases.X509
{
    public sealed class X509DatabaseIndexingService : IndexingService<X509Certificate, X509Metadata>
    {
        public AVLTree<X509Name, int> index_Subject = new AVLTree<X509Name, int>(new X509NameComparer());
        public AVLTree<byte[], int> index_SubjectKeyIdentifier = new AVLTree<byte[], int>(new ByteArrayComparer());

        public override void Clear()
        {
            base.Clear();

            this.index_Subject.Clear();
            this.index_SubjectKeyIdentifier.Clear();
        }

        public override void Index(X509Metadata metadata)
        {
            base.Index(metadata);

            this.index_Subject.Add(metadata.Subject, metadata.Index);

            if (metadata.SubjectKeyIdentifier != null)
                this.index_SubjectKeyIdentifier.Add(metadata.SubjectKeyIdentifier, metadata.Index);
        }

        public override void Remove(X509Metadata metadata)
        {
            base.Remove(metadata);

            this.index_Subject.Remove(metadata.Subject, metadata.Index);

            if (metadata.SubjectKeyIdentifier != null)
                this.index_SubjectKeyIdentifier.Remove(metadata.SubjectKeyIdentifier, metadata.Index);
        }

        // using index_Subject travel the tree depth-first and get the stored indexes.
        public override IEnumerable<int> GetOptimizedIndexes() => this.index_Subject.WalkHorizontal();
    }
}
#endif
