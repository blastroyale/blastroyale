#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
    public class KeyParameter
		: ICipherParameters
    {
        private readonly byte[] m_key;

		public KeyParameter(byte[] key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			m_key = (byte[])key.Clone();
		}

		public KeyParameter(byte[] key, int keyOff, int keyLen)
        {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (keyOff < 0 || keyOff > key.Length)
				throw new ArgumentOutOfRangeException(nameof(keyOff));
            if (keyLen < 0 || keyLen > (key.Length - keyOff))
				throw new ArgumentOutOfRangeException(nameof(keyLen));

			m_key = new byte[keyLen];
            Array.Copy(key, keyOff, m_key, 0, keyLen);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
        public KeyParameter(ReadOnlySpan<byte> key)
        {
            m_key = key.ToArray();
        }
#endif

        public byte[] GetKey()
        {
			return (byte[])m_key.Clone();
        }
    }
}
#pragma warning restore
#endif
