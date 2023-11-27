#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;

using Best.HTTP.Shared.TLS.Crypto;
using Best.HTTP.Shared.Logger;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;

namespace Best.HTTP.Shared.TLS
{
    public class DefaultTls13Client : AbstractTls13Client
    {
        public DefaultTls13Client(List<ServerName> sniServerNames, List<ProtocolName> protocols, LoggingContext context)
            : base(sniServerNames, protocols, new FastTlsCrypto(new SecureRandom()), context)
        {
        }
    }
}
#endif
