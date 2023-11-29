#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Math;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Math.EC;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    public sealed class EdDsaPublicBcpgKey
        : ECPublicBcpgKey
    {
        internal EdDsaPublicBcpgKey(BcpgInputStream bcpgIn)
            : base(bcpgIn)
        {
        }

        public EdDsaPublicBcpgKey(DerObjectIdentifier oid, ECPoint point)
            : base(oid, point)
        {
        }

        public EdDsaPublicBcpgKey(DerObjectIdentifier oid, BigInteger encodedPoint)
            : base(oid, encodedPoint)
        {
        }
    }
}
#pragma warning restore
#endif
