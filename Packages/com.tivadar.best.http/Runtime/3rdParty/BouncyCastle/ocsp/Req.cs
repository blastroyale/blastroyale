#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Ocsp
{
	public class Req
		: X509ExtensionBase
	{
		private Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp.Request req;

		public Req(
            Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp.Request req)
		{
			this.req = req;
		}

		public CertificateID GetCertID()
		{
			return new CertificateID(req.ReqCert);
		}

		public X509Extensions SingleRequestExtensions
		{
			get { return req.SingleRequestExtensions; }
		}

		protected override X509Extensions GetX509Extensions()
		{
			return SingleRequestExtensions;
		}
	}
}
#pragma warning restore
#endif
