#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.Linq;

using Best.TLSSecurity.Databases.OCSP;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509.Extension;
using Best.HTTP.Shared.Logger;

namespace Best.TLSSecurity.OCSP
{
    public static class OCSPValidation
    {
        public static void Validate(List<X509Certificate> chain, HTTP.SecureProtocol.Org.BouncyCastle.Tls.CertificateStatus certificateStatus, LoggingContext loggingContext)
        {
            if (chain.Count < 2)
                throw new Exception("At least leaf and issuer certificate expected!");

            var leaf = chain[0];
            var issuer = chain[1];

            // we can skip checking for revocation if it's a short lifespan certificate
            if (leaf.NotAfter - leaf.NotBefore > SecurityOptions.OCSP.ShortLifeSpanThreshold)
            {
                var aia = AuthorityInformationAccess.FromExtensions(leaf.CertificateStructure.TbsCertificate.Extensions);
                if (aia == null)
                {
                    // TODO: at least warn about the missing extension!
                    return;
                }

                if (certificateStatus == null && SecurityOptions.OCSP.EnableOCSPQueries)
                {
                    Status status = OCSPCache.GetCertificateStatus(chain, aia, loggingContext);

                    switch(status)
                    {
                        case Status.Good:
                            return;

                        case Status.Unknown:
                            if (SecurityOptions.OCSP.FailHard)
                                throw new Exception("Couldn't verify leaf certificate's revocation status!");
                            return;

                        case Status.Revoked:
                            throw new Exception("Revoked certificate found!");
                    }
                }

                SingleResponse singleResponse = null;
                if (certificateStatus != null)
                {
                    singleResponse = OCSPValidation.GetOCSPResponse(certificateStatus, chain, loggingContext);

                    if (singleResponse.CertStatus.Status != DerNull.Instance)
                    {
                        if (singleResponse.CertStatus.TagNo == (byte)Status.Revoked) // revoked
                        {
                            var revokedInfo = RevokedInfo.GetInstance(singleResponse.CertStatus.Status);
                            var reason = revokedInfo.RevocationReason != null ? revokedInfo.RevocationReason.ToString() : "unknown";

                            throw new Exception($"Cert status isn't Good! RevocationTime: {revokedInfo.RevocationTime.TimeString}, reason: {reason}");
                        }

                        throw new Exception($"Cert status is unknown!");
                    }
                }

                // validate the issuer 
                if (singleResponse != null)
                    OCSPValidation.ValidateIssuerWithOCSP(singleResponse, leaf, issuer, loggingContext);
            }
        }

        public static X509Certificate ValidateSignerCertificate(X509CertificateStructure certStruct)
        {
            var signerCertificate = new X509Certificate(certStruct);
            signerCertificate.CheckValidity();

            // id-pkix-ocsp-nocheck extension
            // http://oid-info.com/get/1.3.6.1.5.5.7.48.1.5        
            var noCheckExt = X509ExtensionUtilities.FromExtensionValue(signerCertificate.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNocheck));
            if (noCheckExt == null || noCheckExt != DerNull.Instance)
                return null;//throw new Exception("Missing id-pkix-ocsp-nocheck extension from certificate!");

            return signerCertificate;
        }

        public static SingleResponse GetOCSPResponse(Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.CertificateStatus certificateStatus, List<X509Certificate> chain, LoggingContext loggingContext)
        {
            OcspResponse response = certificateStatus.OcspResponse;
            if (response.ResponseStatus.IntValueExact != OcspResponseStatus.Successful)
                throw new Exception("OCSP failed. Status: " + response.ResponseStatus.IntValueExact);

            ResponseBytes responseBytes = response.ResponseBytes;
            BasicOcspResponse basicOcspResponse = BasicOcspResponse.GetInstance(DerSequence.GetInstance(responseBytes.Response.GetOctets()));

            // find signing certificate
            X509Certificate signerCertificate = null;

            X509Name byName = basicOcspResponse.TbsResponseData.ResponderID.Name;
            if (byName != null)
            {
                if (basicOcspResponse.Certs != null)
                {
                    foreach (var cert in basicOcspResponse.Certs)
                    {
                        var certStruct = X509CertificateStructure.GetInstance(cert);
                        if (certStruct.Subject.Equivalent(byName) &&
                            (signerCertificate = ValidateSignerCertificate(certStruct)) != null)
                            break;
                    }
                }

                if (signerCertificate == null)
                    signerCertificate = chain.FirstOrDefault(c => c.SubjectDN.Equivalent(byName));
            }
            else
            {
                //issuer = certificates.FirstOrDefault(c => c.key)
                //KeyHash::= OCTET STRING--SHA - 1 hash of responder's public key
                //             --(excluding the tag and length fields)
                byte[] keyHash = basicOcspResponse.TbsResponseData.ResponderID.GetKeyHash();

                if (basicOcspResponse.Certs != null)
                {
                    // The OCSP response has its own certificate, and the OCSP response is signed with that
                    // https://security.stackexchange.com/questions/15564/what-is-the-most-secure-way-to-do-ocsp-signing-without-creating-validation-loops
                    foreach (var cert in basicOcspResponse.Certs)
                    {
                        var certStruct = X509CertificateStructure.GetInstance(cert);
                        byte[] publicKey = certStruct.SubjectPublicKeyInfo.PublicKeyData.GetOctets();
                        byte[] hash = DigestUtilities.CalculateDigest("SHA1", publicKey);

                        if (Arrays.AreEqual(keyHash, hash) && (signerCertificate = ValidateSignerCertificate(certStruct)) != null)
                            break;
                    }
                }

                if (signerCertificate == null)
                {
                    signerCertificate = chain.FirstOrDefault(c =>
                    {
                        byte[] publicKey = c.CertificateStructure.SubjectPublicKeyInfo.PublicKeyData.GetOctets();
                        byte[] hash = DigestUtilities.CalculateDigest("SHA1", publicKey);

                        return Arrays.AreEqual(keyHash, hash);
                    });
                }

                // What if we couldn't find the certificate in the server-sent ones? Should we try to find it in the stored intermediates?
            }

            if (signerCertificate == null)
                throw new Exception("Couldn't find issuer certificate for responder name or hash!");

            // verify signature
            if (!new BasicOcspResp(basicOcspResponse).Verify(signerCertificate.GetPublicKey()))
                throw new Exception("Verifying OCSP response failed!");

            // TODO: validate signer certificates extended key usage: https://www.feistyduck.com/bulletproof-tls-newsletter/issue_67_intermediate_certificates_with_ocsp_capability_cause_trouble

            if (basicOcspResponse.TbsResponseData.Responses.Count >= 1)
            {
                var singleResponse = new SingleResponse(Asn1Sequence.GetInstance(basicOcspResponse.TbsResponseData.Responses[0]));

                return singleResponse;
            }

            return null;
        }

        public static void ValidateIssuerWithOCSP(SingleResponse singleResponse, X509Certificate leaf, X509Certificate issuerCertificate, LoggingContext loggingContext)
        {
            CertificateID expectedCertificateID = new CertificateID(singleResponse.CertId.HashAlgorithm.Algorithm.Id, issuerCertificate, leaf.SerialNumber);

            if (!expectedCertificateID.SerialNumber.Equals(singleResponse.CertId.SerialNumber.Value))
                throw new Exception("Response's SerialNumber isn't the expected value!");

            if (!Arrays.AreEqual(expectedCertificateID.GetIssuerNameHash(), singleResponse.CertId.IssuerNameHash.GetOctets()))
                throw new Exception("Response's Issuer isn't the expected value!");

            if (!Arrays.AreEqual(expectedCertificateID.GetIssuerKeyHash(), singleResponse.CertId.IssuerKeyHash.GetOctets()))
                throw new Exception("Responser's IssuerKeyHash isn't the expected value!");
        }
    }
}

#endif
