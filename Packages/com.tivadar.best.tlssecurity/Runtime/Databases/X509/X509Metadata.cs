#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.IO;

using Best.TLSSecurity.Databases.Shared;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders;

namespace Best.TLSSecurity.Databases.X509
{
    public sealed class X509Metadata : Metadata
    {
        public X509Name Subject;
        public X509Name Issuer;

        public byte[] SubjectKeyIdentifier;

        public override void LoadFrom(Stream stream)
        {
            base.LoadFrom(stream);

            this.Subject = X509Name.GetInstance(Asn1Object.FromStream(stream));
            this.Issuer = X509Name.GetInstance(Asn1Object.FromStream(stream));

            int length = stream.ReadByte();
            if (length > 0)
            {
                this.SubjectKeyIdentifier = new byte[length];
                stream.Read(this.SubjectKeyIdentifier, 0, this.SubjectKeyIdentifier.Length);
            }
        }

        public override void SaveTo(Stream stream)
        {
            base.SaveTo(stream);

            byte[] temp = this.Subject.GetEncoded();
            stream.Write(temp, 0, temp.Length);

            temp = this.Issuer.GetEncoded();
            stream.Write(temp, 0, temp.Length);

            if (this.SubjectKeyIdentifier != null && this.SubjectKeyIdentifier.Length > 0)
            {
                if (this.SubjectKeyIdentifier.Length >= 0xFF)
                    throw new Exception($"SubjectKeyIdentifier({Hex.ToHexString(this.SubjectKeyIdentifier)}) is longer({this.SubjectKeyIdentifier.Length}) than expected!");

                stream.WriteByte((byte)(this.SubjectKeyIdentifier.Length));

                stream.Write(this.SubjectKeyIdentifier, 0, this.SubjectKeyIdentifier.Length);
            }
            else
            {
                stream.WriteByte(0);
            }
        }

#if UNITY_EDITOR
        private string cachedSubjectStr;
        public string GetSubjectStr()
        {
            if (cachedSubjectStr == null)
                cachedSubjectStr = GetNameFrom(this.Subject);

            return cachedSubjectStr;
        }

        private string cachedIssuerStr;
        public string GetIssuerStr()
        {
            if (cachedIssuerStr == null)
                cachedIssuerStr = GetNameFrom(this.Issuer);

            return cachedIssuerStr;
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
    }
}
#endif
