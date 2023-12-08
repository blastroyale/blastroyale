#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.ClientCredentials
{
    public sealed class ClientCredentialsMetadataService : MetadataService<ClientCredentialMetadata, ClientCredential>
    {
        public ClientCredentialsMetadataService(ClientCredentialIndexingService indexingService)
            : base(indexingService)
        { }

        public ClientCredentialMetadata Create(string targetDomain, ClientCredential content, MetadataFlags flags, int filePos, int length)
        {
            return CreateDefault(content, flags, filePos, length, (c, metadata) =>
            {
                metadata.Authority = (c.Certificate.GetCertificateAt(0) as BestHTTPTlsCertificate).Cert.Issuer;

                if (!string.IsNullOrEmpty(targetDomain))
                    metadata.TargetDomain = targetDomain;
            });
        }
    }
}
#endif
