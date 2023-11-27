#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
	/// <remarks>Parameters for mask derivation functions.</remarks>
    public sealed class MgfParameters
		: IDerivationParameters
    {
        private readonly byte[] m_seed;

		public MgfParameters(byte[] seed)
			: this(seed, 0, seed.Length)
        {
        }

		public MgfParameters(byte[] seed, int off, int len)
        {
            m_seed = Arrays.CopyOfRange(seed, off, len);
        }

        public byte[] GetSeed()
        {
            return (byte[])m_seed.Clone();
        }

        public void GetSeed(byte[] buffer, int offset)
        {
            m_seed.CopyTo(buffer, offset);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
        public void GetSeed(Span<byte> output)
        {
            m_seed.CopyTo(output);
        }
#endif

        public int SeedLength => m_seed.Length;
    }
}
#pragma warning restore
#endif
