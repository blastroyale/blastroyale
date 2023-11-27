#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Pkix;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509.Extension;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509.Store;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.TLS;
using Best.HTTP.Shared.TLS.Crypto;
using Best.TLSSecurity.Databases.ClientCredentials;
using Best.TLSSecurity.OCSP;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Best.TLSSecurity
{
    /// <summary>
    /// Provides a concrete implementation for handling TLS v1.2 and v1.3 client functionalities to provide enhanced TLS client functionalities to establish and maintain a secure connection
    /// </summary>
    /// <remarks>
    /// The SecureTlsClient class offers mechanisms to process, handle, and manage:
    /// <list type="bullet">
    /// <item><description>Supported protocol versions (v1.2 and v1.3).</description></item>
    /// <item><description>Client extensions.</description></item>
    /// <item><description>Signature algorithms from private keys.</description></item>
    /// <item><description>Creation of signer credentials based on provided algorithms and credentials.</description></item>
    /// <item><description>Client credentials handling for CertificateRequest.</description></item>
    /// <item><description>Server certificate validation using root and intermediate certificates from the local database.</description></item>
    /// <item><description>Domain validation for X509 certificates.</description></item>
    /// </list>
    /// The class relies heavily on integration with the BouncyCastle crypto library and includes a number of methods to facilitate
    /// the security aspects of the TLS protocol, from the handshaking process through to the verification of server certificates.
    /// </remarks>
    public sealed class SecureTlsClient : AbstractTls13Client
    {
        private Uri _currentUri = null;

        public SecureTlsClient(Uri uri, List<ServerName> hostNames, List<ProtocolName> clientSupportedProtocols, LoggingContext context)
            : base(hostNames, clientSupportedProtocols, new FastTlsCrypto(new SecureRandom()), context)
        {
            this._currentUri = uri;
        }

        protected override ProtocolVersion[] GetSupportedVersions() => ProtocolVersion.TLSv13.DownTo(ProtocolVersion.TLSv12);

        public override IDictionary<int, byte[]> GetClientExtensions()
        {
            HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(GetClientExtensions)}", this.Context);

            var clientExtensions = base.GetClientExtensions();

            //if (this._tag != null && this._tag.RemoveALPNExtension)
            //{
            //    HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(GetClientExtensions)} - RemoveALPNExtension found in the HTTPRequest, removing extension", this.Context);
            //    clientExtensions.Remove(ExtensionType.application_layer_protocol_negotiation);
            //}

            return clientExtensions;
        }

        SignatureAndHashAlgorithm FindSupportedSignatureAlgorithm(List<SignatureAndHashAlgorithm> signatureAndHashAlgorithms, byte signatureAlgorithm)
        {
            return (from sign in signatureAndHashAlgorithms where sign.Signature == signatureAlgorithm select sign).FirstOrDefault();
        }

        byte SignatureAlgorithmFromPrivateKey(HTTP.SecureProtocol.Org.BouncyCastle.Crypto.AsymmetricKeyParameter key)
        {
            if (key is HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)
            {
                return (byte)SignatureAlgorithm.rsa;
            }
            else if (key is HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters.DsaPrivateKeyParameters)
            {
                return (byte)SignatureAlgorithm.dsa;
            }
            else if (key is HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)
            {
                return (byte)SignatureAlgorithm.ecdsa;
            }
            else
            {
                HTTPManager.Logger.Warning(nameof(SecureTlsClient), $"{nameof(SignatureAlgorithmFromPrivateKey)} - Unsupported key('{key.GetType().Name}') found!", this.Context);
            }

            return 255;
        }

        TlsCredentials CreateSignerCredentials(List<SignatureAndHashAlgorithm> serverSupportedSignatureAlgorithms, List<ClientCredential> clientCredentials)
        {
            if (clientCredentials != null && clientCredentials.Count > 0)
            {
                // TODO: check CertificateTypes
                for (int i = 0; i < clientCredentials.Count; ++i)
                {
                    var credential = clientCredentials[i];

                    var key = PrivateKeyFactory.CreateKey(credential.KeyInfo);

                    byte signatureAlgorithm = SignatureAlgorithmFromPrivateKey(key);

                    // rsa: 1, dsa: 2, ecdsa: 3
                    if (signatureAlgorithm > 3)
                        continue;

                    var signatureAndHashAlgorithm = FindSupportedSignatureAlgorithm(serverSupportedSignatureAlgorithms, signatureAlgorithm);
                    if (signatureAndHashAlgorithm == null)
                        continue;

                    try
                    {
                        return new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(this.m_context), new FastTlsCrypto(this.m_context.Crypto.SecureRandom), key, credential.Certificate, signatureAndHashAlgorithm);
                    }
                    catch
                    { }
                }
            }

            return null;
        }

        public override TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
        {
            HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.GetClientCredentials)} - certificateRequest.CertificateAuthorities: {certificateRequest.CertificateAuthorities?.Count}", this.Context);

            var serverSupportedSignatureAlgorithms = certificateRequest.SupportedSignatureAlgorithms.Cast<SignatureAndHashAlgorithm>().ToList();

            if (certificateRequest.CertificateAuthorities?.Count > 0)
            {
                foreach (X509Name authority in certificateRequest.CertificateAuthorities)
                {
                    HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.GetClientCredentials)} - certificateRequest.CertificateAuthorities: {authority}", this.Context);

                    var clientCredentials = TLSSecurity.ClientCredentials.FindByAuthority(authority);

                    var signerCredentials = CreateSignerCredentials(serverSupportedSignatureAlgorithms, clientCredentials);
                    if (signerCredentials != null)
                        return signerCredentials;
                }
            }
            else
            {
                // Find by host
                // *.domain.com will match www.domain.com but not domain.com and not zzz.www.domain.com
                // If current host is www.domain.com here we going to try to get a credential for www.domain.com first,
                //  but if it fails, try for *.domain.com.

                string host = this._currentUri.Host;
                var clientCredentials = TLSSecurity.ClientCredentials.FindByTargetDomain(host);

                if (clientCredentials == null)
                {
                    var components = host.Split('.');
                    if (components.Length > 0)
                        components[0] = "*";
                    var newHost = string.Join(".", components);
                    clientCredentials = TLSSecurity.ClientCredentials.FindByTargetDomain(newHost);
                }

                if (clientCredentials != null)
                    return CreateSignerCredentials(serverSupportedSignatureAlgorithms, clientCredentials);
            }

            return null;
        }

        public override void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
            DateTime startedAt = DateTime.Now;
            try
            {
                HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)} - Certificates received: {serverCertificate.Certificate.Length}", this.Context);
                TLSSecurity.WaitForSetupFinish();
                HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)} - WaitForSetupFinish... ... done!", this.Context);

                var certificates = serverCertificate.Certificate.GetCertificateList().Select(c => new X509Certificate(c.GetEncoded())).ToList<X509Certificate>();

                if (HTTPManager.Logger.IsDiagnostic)
                    for (int i = 0; i < certificates.Count; ++i)
                        HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)} - Certificates received({i}): {certificates[i].ToString()}", this.Context);

                // find leaf - the one that's not CA (https://security.stackexchange.com/questions/38949/understanding-ssl-certificate-signing)
                X509Certificate leaf = certificates.Find(c =>
                {
                    Asn1OctetString str = c.GetExtensionValue(X509Extensions.BasicConstraints);
                    if (str == null)
                        return true;

                    var basicConstraints = BasicConstraints.GetInstance(X509ExtensionUtilities.FromExtensionValue(str));

                    return basicConstraints == null || !basicConstraints.IsCA();
                });

                if (leaf == null)
                    throw new Exception("No certificate could be found without a CA flag!");

                // Must-Staple check
                // https://www.thesslstore.com/blog/ocsp-ocsp-stapling-ocsp-must-staple/
                if (SecurityOptions.OCSP.FailOnMissingCertStatusWhenMustStaplePresent)
                {
                    // https://tools.ietf.org/html/rfc7633
                    var value = leaf.GetExtensionValue(new DerObjectIdentifier("1.3.6.1.5.5.7.1.24"));
                
                    if (value != null)
                    {
                        HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)} - Must-Staple found! value: {Hex.ToHexString(value.GetOctets())}", this.Context);
                
                        // The inclusion of a TLS feature extension advertising the
                        // status_request feature in the server end - entity certificate permits a
                        // client to fail immediately if the certificate status information is
                        // not provided by the server.
                        if (serverCertificate.CertificateStatus == null)
                            throw new Exception("Certificate status missing while must-staple flag present in the certificate!");
                    }
                }

                ValidateMatchingDomain(this._currentUri, leaf);

                var chain = BuildCertificateChain(leaf, certificates);

                if (HTTPManager.Logger.IsDiagnostic)
                    for (int i = 0; i < chain.Count; ++i)
                        HTTPManager.Logger.Verbose(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)} - chain({i}): {chain[i].ToString()}", this.Context);

                OCSPValidation.Validate(chain, serverCertificate.CertificateStatus, this.Context);
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception(nameof(SecureTlsClient), $"{nameof(TlsAuthentication.NotifyServerCertificate)}({ex.Message})", ex, this.Context);

                throw new TlsFatalAlert(AlertDescription.user_canceled, ex);
            }
            //finally
            //{
            //    // With server-initiated TLS renegotiation certificate validation can run twice during a TLS session after a call to AbstractTls13Client's NotifyHandshakeComplete where _request is set to null.
            //    this._request?.Timing?.Add("Certificate validation", DateTime.Now - startedAt);
            //}
        }

        void ValidateMatchingDomain(Uri targetUri, X509Certificate leaf)
        {
            // https://tools.ietf.org/html/rfc2818#section-3.1
            // https://tools.ietf.org/html/rfc2459#page-32
            // Names may contain the wildcard character * which is considered to match any single domain name
            // component or component fragment. E.g., *.a.com matches foo.a.com but not bar.foo.a.com. f*.com matches foo.com but not bar.com.

            string[] targetUriComponents = targetUri.Host.Split('.');

            Asn1OctetString altNames = leaf.GetExtensionValue(X509Extensions.SubjectAlternativeName);

            if (altNames != null)
            {
                Asn1Object asn1Object = X509ExtensionUtilities.FromExtensionValue(altNames);
                GeneralNames gns = GeneralNames.GetInstance(asn1Object);

                bool foundAtLeastOneGoodMatch = false;
                foreach (GeneralName SAN in gns.GetNames())
                {
                    switch (SAN.TagNo) 
                    {
                        case GeneralName.DnsName:
                        {
                            string allowedDomainName = SAN.Name.ToString();
                            // exact match or wildcard match
                            if (targetUri.Host.Equals(allowedDomainName, StringComparison.OrdinalIgnoreCase) || IsMatchingDomain(targetUriComponents, allowedDomainName))
                                foundAtLeastOneGoodMatch = true;

                            break;
                        }

                        case GeneralName.IPAddress:
                        {
                            var address = new System.Net.IPAddress((SAN.Name as DerOctetString).GetOctets());
                            if (System.Net.IPAddress.TryParse(targetUri.Host, out var targetAddress))
                                foundAtLeastOneGoodMatch = targetAddress.Equals(address);
                            break;
                        }
                    }

                    if (foundAtLeastOneGoodMatch)
                        break;
                }

                if (!foundAtLeastOneGoodMatch)
                    throw new Exception("Certification isn't made for this domain!");
            }
            else
            {
                var commonNames = leaf.SubjectDN.GetValueList(X509Name.CN);
                if (commonNames == null || commonNames.Count == 0)
                    throw new Exception("Missing CommonName!");

                bool foundMatching = false;
                for (int i = 0; i < commonNames.Count && !foundMatching; ++i)
                {
                    string CN = commonNames[i].ToString();

                    //if (!targetUri.Host.Equals(CN, StringComparison.OrdinalIgnoreCase) && !IsMatchingDomain(targetUriComponents, CN))
                    //    throw new Exception("Certification isn't made for this domain!");
                    foundMatching = targetUri.Host.Equals(CN, StringComparison.OrdinalIgnoreCase) || IsMatchingDomain(targetUriComponents, CN);
                }

                if (!foundMatching)
                    throw new Exception("Certification isn't made for this domain!");
            }
        }

        bool IsMatchingDomain(string[] targetUriComponents, string domain)
        {
            // *.domain.com is OK. It will match www.domain.com but not domain.com and not zzz.www.domain.com

            string[] domainComponents = domain.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (targetUriComponents.Length != domainComponents.Length)
                return false;

            for (int i = 0; i < domainComponents.Length; ++i)
            {
                string domainComp = domainComponents[i];
                string targetComp = targetUriComponents[i];

                if (domainComp != "*" && !domainComp.Equals(targetComp, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        List<X509Certificate> BuildCertificateChain(X509Certificate leafCertificate, List<X509Certificate> certs)
        {
            PkixCertPathBuilder builder = new PkixCertPathBuilder();

            IntermediateCertStore intermediateCerts = new IntermediateCertStore();
            HashSet<TrustAnchor> rootCerts = new HashSet<TrustAnchor>();

            // Include the leaf certificate too, otherwise builder.Build would fail
            intermediateCerts.Add(leafCertificate);

            // Rebuild chain starting from the leaf certificate
            var current = leafCertificate;
            while (current != null)
            {
                X509Certificate parent = null;

                // couldn't find the parent certificate in the trusted intermediates, maybe it's a root cert
                var matchedRoots = TLSSecurity.trustedRootCertificates.GetMatches(CreateSelector(current)) as IList;

                // if found, parent remains null to exit from the while loop
                if (matchedRoots != null && matchedRoots.Count > 0)
                {
                    parent = matchedRoots[0] as X509Certificate;
                    rootCerts.Add(new TrustAnchor(parent, null));
                    break;
                }

                if (parent == null)
                {
                    var selector = CreateSelector(current);
                    var matchedIntermediates = TLSSecurity.trustedIntermediateCertificates.GetMatches(selector) as IList;

                    if (matchedIntermediates != null && matchedIntermediates.Count > 0)
                    {
                        parent = matchedIntermediates[0] as X509Certificate;
                        intermediateCerts.Add(parent);
                    }
                    else if (SecurityOptions.UseServerSentIntermediateCertificates && (parent = certs.Find(cert => cert.SubjectDN.Equals(current.IssuerDN))) != null)
                        intermediateCerts.Add(parent);
                }

                if (parent != current)
                    current = parent;
                else
                    current = null;
            }

            // Create chain for this certificate
            X509CertStoreSelector holder = new X509CertStoreSelector();
            holder.Certificate = leafCertificate;

            PkixBuilderParameters builderParams = new PkixBuilderParameters(rootCerts, holder);
            builderParams.IsRevocationEnabled = false;
            //builderParams.AddStore(X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(intermediateCerts)));
            builderParams.AddStoreCert(intermediateCerts);

            PkixCertPathBuilderResult result = builder.Build(builderParams);

            var chain = result.CertPath.Certificates.Cast<X509Certificate>().ToList<X509Certificate>();
            var enumerator = rootCerts.GetEnumerator();
            if (enumerator.MoveNext())
                chain.Add((enumerator.Current as TrustAnchor).TrustedCert);
            return chain;
        }

        private static X509CertStoreSelector CreateSelector(X509Certificate certificate)
        {
            var selector = new X509CertStoreSelector();
            selector.Subject = certificate.IssuerDN;

            // If available add a criteria for authority-subject key identifier match too
            // https://tools.ietf.org/html/rfc5280#section-4.2.1.1
            // AKI must match its parent's SKI
            var aki = AuthorityKeyIdentifier.FromExtensions(certificate.CertificateStructure.TbsCertificate.Extensions);
            if (aki != null)
                selector.SubjectKeyIdentifier = aki.GetKeyIdentifier();

            return selector;
        }

        sealed class IntermediateCertStore : IStore<X509Certificate>
        {
            private readonly List<X509Certificate> m_contents = new List<X509Certificate>();

            public void Add(X509Certificate cert)
            {
                this.m_contents.Add(cert);
            }

            public IEnumerable<X509Certificate> EnumerateMatches(ISelector<X509Certificate> selector)
            {
                foreach (X509Certificate candidate in m_contents)
                {
                    if (selector == null || selector.Match(candidate))
                        yield return candidate;
                }
            }
        }
    }
}

#endif
