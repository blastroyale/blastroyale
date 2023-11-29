#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement
{
    public sealed class X448Agreement
        : IRawAgreement
    {
        private X448PrivateKeyParameters m_privateKey;

        public void Init(ICipherParameters parameters)
        {
            m_privateKey = (X448PrivateKeyParameters)parameters;
        }

        public int AgreementSize
        {
            get { return X448PrivateKeyParameters.SecretSize; }
        }

        public void CalculateAgreement(ICipherParameters publicKey, byte[] buf, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
            CalculateAgreement(publicKey, buf.AsSpan(off));
#else
            m_privateKey.GenerateSecret((X448PublicKeyParameters)publicKey, buf, off);
#endif
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
        public void CalculateAgreement(ICipherParameters publicKey, Span<byte> buf)
        {
            m_privateKey.GenerateSecret((X448PublicKeyParameters)publicKey, buf);
        }
#endif
    }
}
#pragma warning restore
#endif
