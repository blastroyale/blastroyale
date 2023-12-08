#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System.IO;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Cms
{
	/**
	* a holding class for a file of data to be processed.
	*/
	public class CmsProcessableFile
		: CmsProcessable, CmsReadable
	{
		private const int DefaultBufSize = 32 * 1024;

        private readonly FileInfo	_file;
		private readonly int		_bufSize;

        public CmsProcessableFile(FileInfo file)
			: this(file, DefaultBufSize)
		{
		}

        public CmsProcessableFile(FileInfo file, int bufSize)
		{
			_file = file;
			_bufSize = bufSize;
		}

        public virtual Stream GetInputStream()
		{
			return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, _bufSize);
		}

        public virtual void Write(Stream zOut)
		{
			using (var inStr = _file.OpenRead())
			{
                Streams.PipeAll(inStr, zOut, _bufSize);
            }
		}
	}
}
#pragma warning restore
#endif
