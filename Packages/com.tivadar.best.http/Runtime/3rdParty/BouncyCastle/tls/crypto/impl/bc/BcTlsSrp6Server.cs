#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement.Srp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Math;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    internal sealed class BcTlsSrp6Server
        : TlsSrp6Server
    {
        private readonly Srp6Server m_srp6Server;

        internal BcTlsSrp6Server(Srp6Server srp6Server)
        {
            this.m_srp6Server = srp6Server;
        }

        public BigInteger GenerateServerCredentials()
        {
            return m_srp6Server.GenerateServerCredentials();
        }

        public BigInteger CalculateSecret(BigInteger clientA)
        {
            try
            {
                return m_srp6Server.CalculateSecret(clientA);
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter, e);
            }
        }
    }
}
#pragma warning restore
#endif
