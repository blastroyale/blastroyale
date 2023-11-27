#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.Shared.TLS.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Pkcs;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Best.TLSSecurity.Databases.ClientCredentials
{
    public sealed class BestHTTPTlsCertificate : BcTlsCertificate
    {
        public X509CertificateStructure Cert => base.m_certificate;

        public BestHTTPTlsCertificate(X509CertificateStructure certificate) : base(new FastTlsCrypto(new SecureRandom()), certificate)
        {
        }
    }

    public sealed class ClientCredential
    {
        public Certificate Certificate;
        public PrivateKeyInfo KeyInfo;
    }

    public sealed class ClientCredentialParser : IDiskContentParser<ClientCredential>
    {
        public void Encode(Stream stream, ClientCredential content)
        {
            EncodeCertificate(stream, content);

            var encoded = content.KeyInfo.GetEncoded();
            stream.Write(encoded, 0, encoded.Length);
        }

        private void EncodeCertificate(Stream stream, ClientCredential content)
        {
            List<byte[]> derEncodings = new List<byte[]>(content.Certificate.Length);

            int totalLength = 0;
            for (int i = 0; i < content.Certificate.Length; i++)
            {
                var entry = content.Certificate.GetCertificateAt(i);
                byte[] derEncoding = entry.GetEncoded();
                derEncodings.Add(derEncoding);
                totalLength += derEncoding.Length + 3;
            }

            TlsUtilities.CheckUint24(totalLength);
            TlsUtilities.WriteUint24(totalLength, stream);

            foreach (byte[] derEncoding in derEncodings)
                TlsUtilities.WriteOpaque24(derEncoding, stream);
        }

        public ClientCredential Parse(Stream stream, int length)
        {
            long pos = stream.Position;

            var result = new ClientCredential();

            // Certificate
            result.Certificate = ParseCertificate(stream);

            int read = (int)(stream.Position - pos);

            // Private Key Info
            Asn1StreamParser parser = new Asn1StreamParser(stream, length - read);
            var obj = parser.ReadObject();
            result.KeyInfo = PrivateKeyInfo.GetInstance(obj);

            return result;
        }

        public Certificate ParseCertificate(Stream stream)
        {
            int totalLength = TlsUtilities.ReadUint24(stream);
            if (totalLength == 0)
            {
                return null;
            }

            byte[] certListData = TlsUtilities.ReadFully(totalLength, stream);

            MemoryStream buf = new MemoryStream(certListData, false);

            var certificate_list = new List<X509CertificateStructure>();
            while (buf.Position < buf.Length)
            {
                byte[] berEncoding = TlsUtilities.ReadOpaque24(buf);
                Asn1Object asn1Cert = TlsUtilities.ReadAsn1Object(berEncoding);
                certificate_list.Add(X509CertificateStructure.GetInstance(asn1Cert));
            }

            TlsCertificate[] certificateList = new TlsCertificate[certificate_list.Count];
            for (int i = 0; i < certificate_list.Count; ++i)
                certificateList[i] = new BestHTTPTlsCertificate((X509CertificateStructure)certificate_list[i]);

            return new Certificate(certificateList);
        }
    }
}
#endif
