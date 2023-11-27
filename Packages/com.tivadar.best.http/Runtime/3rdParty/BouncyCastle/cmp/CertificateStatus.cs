#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Cms;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Math;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    public class CertificateStatus
    {
        private static readonly DefaultSignatureAlgorithmIdentifierFinder sigAlgFinder = new DefaultSignatureAlgorithmIdentifierFinder();

        private readonly DefaultDigestAlgorithmIdentifierFinder digestAlgFinder;
        private readonly CertStatus certStatus;

        public CertificateStatus(DefaultDigestAlgorithmIdentifierFinder digestAlgFinder, CertStatus certStatus)
        {
            this.digestAlgFinder = digestAlgFinder;
            this.certStatus = certStatus;
        }

        public virtual PkiStatusInfo StatusInfo => certStatus.StatusInfo;

        public virtual BigInteger CertRequestID => certStatus.CertReqID.Value;

        public virtual bool IsVerified(X509Certificate cert)
        {
            AlgorithmIdentifier digAlg = digestAlgFinder.Find(sigAlgFinder.Find(cert.SigAlgName));
            if (null == digAlg)
                throw new CmpException("cannot find algorithm for digest from signature " + cert.SigAlgName);

            byte[] digest = DigestUtilities.CalculateDigest(digAlg.Algorithm, cert.GetEncoded());

            return Arrays.ConstantTimeAreEqual(certStatus.CertHash.GetOctets(), digest);
        }
    }
}
#pragma warning restore
#endif
