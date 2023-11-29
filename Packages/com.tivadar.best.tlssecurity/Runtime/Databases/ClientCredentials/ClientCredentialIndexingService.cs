#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using Best.TLSSecurity.Databases.Indexing;
using Best.TLSSecurity.Databases.Indexing.Comparers;
using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;

namespace Best.TLSSecurity.Databases.ClientCredentials
{
    public sealed class ClientCredentialIndexingService : IndexingService<ClientCredential, ClientCredentialMetadata>
    {
        public AVLTree<X509Name, int> index_Authority = new AVLTree<X509Name, int>(new X509NameComparer());
        public AVLTree<string, int> index_TargetDomain = new AVLTree<string, int>(new Indexing.Comparers.StringComparer());

        public override void Index(ClientCredentialMetadata metadata)
        {
            base.Index(metadata);

            this.index_Authority.Add(metadata.Authority, metadata.Index);

            if (!string.IsNullOrEmpty(metadata.TargetDomain))
                this.index_TargetDomain.Add(metadata.TargetDomain, metadata.Index);
        }

        public override void Clear()
        {
            base.Clear();

            this.index_Authority.Clear();
            this.index_TargetDomain.Clear();
        }

        public override void Remove(ClientCredentialMetadata metadata)
        {
            base.Remove(metadata);

            this.index_Authority.Remove(metadata.Authority, metadata.Index);
            this.index_TargetDomain.Remove(metadata.TargetDomain, metadata.Index);
        }
    }
}
#endif
