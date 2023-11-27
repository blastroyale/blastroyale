#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.IO;

using Best.HTTP.Shared.Databases.Utils;
using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;

namespace Best.TLSSecurity.Databases.ClientCredentials
{
    public sealed class ClientCredentialMetadata : Metadata
    {
        public X509Name Authority;
        public string TargetDomain;

        public override void LoadFrom(Stream stream)
        {
            base.LoadFrom(stream);

            this.Authority = X509Name.GetInstance(Asn1Object.FromStream(stream));
            this.TargetDomain = stream.ReadLengthPrefixedString();
        }

        public override void SaveTo(Stream stream)
        {
            base.SaveTo(stream);

            byte[] temp = this.Authority.GetEncoded();
            stream.Write(temp, 0, temp.Length);

            stream.WriteLengthPrefixedString(this.TargetDomain);
        }

#if UNITY_EDITOR
        private string cachedAuthorityStr;
        public string GetAuthorityStr()
        {
            if (this.cachedAuthorityStr == null)
                this.cachedAuthorityStr = GetNameFrom(this.Authority);

            return this.cachedAuthorityStr;
        }

        private string GetNameFrom(X509Name x509Name)
        {
            var list = x509Name.GetValueList(X509Name.CN);
            if (list != null && list.Count > 0)
                return list[0].ToString();
            else
            {
                list = x509Name.GetValueList(X509Name.OU);
                if (list != null && list.Count > 0)
                    return list[0].ToString();
            }

            return "➖";
        }
#endif

        public override string ToString()
        {
            return string.Format("[ClientCredentialMetadata Idx: {0}, Pos: {1}, Length: {2}, Flags: {3}, TargetDomain: \"{4}\", Authority: \"{5}\"]",
                this.Index,
                this.FilePosition,
                this.Length,
                this.Flags,
                this.TargetDomain,
                this.Authority);
        }
    }
}
#endif
