#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Cms
{
	public interface CmsReadable
	{
		Stream GetInputStream();
	}
}
#pragma warning restore
#endif
