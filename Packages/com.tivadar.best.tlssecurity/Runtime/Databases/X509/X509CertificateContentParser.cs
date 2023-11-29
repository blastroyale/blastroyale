#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.IO;

using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace Best.TLSSecurity.Databases.X509
{
    public sealed class X509CertificateContentParser : IDiskContentParser<X509Certificate>
    {
        X509CertificateParser parser = new X509CertificateParser();

        public void Encode(Stream stream, X509Certificate content)
        {
            var encoded = content.GetEncoded();

            stream.Write(encoded, 0, encoded.Length);
        }

        public X509Certificate Parse(Stream stream, int length)
        {
            // TODO: eliminate parser by calling ReadPemCertificate/ReadDerCertificate manually
            return this.parser.ReadCertificate(stream);
        }
    }
}
#endif
