#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Crmf
{
    public class DefaultPKMacPrimitivesProvider
        : IPKMacPrimitivesProvider
    {
        public IDigest CreateDigest(AlgorithmIdentifier digestAlg)
        {
            return DigestUtilities.GetDigest(digestAlg.Algorithm);
        }

        public IMac CreateMac(AlgorithmIdentifier macAlg)
        {
            return MacUtilities.GetMac(macAlg.Algorithm);
        }
    }
}
#pragma warning restore
#endif
