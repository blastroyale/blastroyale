#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    internal class TlsClientContextImpl
        : AbstractTlsContext, TlsClientContext
    {
        internal TlsClientContextImpl(TlsCrypto crypto)
            : base(crypto, ConnectionEnd.client)
        {
        }

        public override bool IsServer
        {
            get { return false; }
        }
    }
}
#pragma warning restore
#endif
