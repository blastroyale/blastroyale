#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Math;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    public struct RevocationDetails
    {
        private readonly RevDetails m_revDetails;

        public RevocationDetails(RevDetails revDetails)
        {
            m_revDetails = revDetails;
        }

        public X509Name Subject => m_revDetails.CertDetails.Subject;

        public X509Name Issuer => m_revDetails.CertDetails.Issuer;

        public BigInteger SerialNumber => m_revDetails.CertDetails.SerialNumber.Value;

        public RevDetails ToASN1Structure() => m_revDetails;
    }
}
#pragma warning restore
#endif
