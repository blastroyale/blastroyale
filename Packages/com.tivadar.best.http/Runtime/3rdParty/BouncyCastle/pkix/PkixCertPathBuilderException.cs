#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Pkix
{
    [Serializable]
    public class PkixCertPathBuilderException
		: GeneralSecurityException
	{
		public PkixCertPathBuilderException()
			: base()
		{
		}

		public PkixCertPathBuilderException(string message)
			: base(message)
		{
		}

		public PkixCertPathBuilderException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PkixCertPathBuilderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
