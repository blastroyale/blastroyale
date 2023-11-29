#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Best.HTTP.Shared.TLS.Crypto.Impl;

namespace Best.HTTP.Shared.TLS.Crypto
{
    public sealed class FastTlsCrypto : BcTlsCrypto
    {
        public FastTlsCrypto(SecureRandom entropySource)
            : base(entropySource)
        {
        }

        public override TlsCipher CreateCipher(TlsCryptoParameters cryptoParams, int encryptionAlgorithm, int macAlgorithm)
        {
            HTTPManager.Logger.Verbose(nameof(FastTlsCrypto), $"CreateCipher({encryptionAlgorithm}, {macAlgorithm})");

            switch (encryptionAlgorithm)
            {
                case EncryptionAlgorithm.CHACHA20_POLY1305:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateChaCha20Poly1305(cryptoParams);
                        
                        BcChaCha20Poly1305 encrypt = new BcChaCha20Poly1305(true);
                        BcChaCha20Poly1305 decrypt = new BcChaCha20Poly1305(false);

                        return new TlsAeadCipher(cryptoParams, encrypt, decrypt, 32, 16, TlsAeadCipher.AEAD_CHACHA20_POLY1305);
                    }

                case EncryptionAlgorithm.AES_128_CBC:
                case EncryptionAlgorithm.ARIA_128_CBC:
                case EncryptionAlgorithm.CAMELLIA_128_CBC:
                case EncryptionAlgorithm.SEED_CBC:
                case EncryptionAlgorithm.SM4_CBC:
                    {
                        //return CreateCipher_Cbc(cryptoParams, encryptionAlgorithm, 16, macAlgorithm);
                        FastTlsBlockCipherImpl encrypt = new FastTlsBlockCipherImpl(CreateCbcBlockCipher(encryptionAlgorithm), true);
                        FastTlsBlockCipherImpl decrypt = new FastTlsBlockCipherImpl(CreateCbcBlockCipher(encryptionAlgorithm), false);

                        TlsHmac clientMac = CreateMac(cryptoParams, macAlgorithm);
                        TlsHmac serverMac = CreateMac(cryptoParams, macAlgorithm);

                        return new FastTlsBlockCipher(cryptoParams, encrypt, decrypt, clientMac, serverMac, 16);
                    }

                case EncryptionAlgorithm.AES_256_CBC:
                case EncryptionAlgorithm.ARIA_256_CBC:
                case EncryptionAlgorithm.CAMELLIA_256_CBC:
                    {
                        //return CreateCipher_Cbc(cryptoParams, encryptionAlgorithm, 32, macAlgorithm);
                        FastTlsBlockCipherImpl encrypt = new FastTlsBlockCipherImpl(CreateCbcBlockCipher(encryptionAlgorithm), true);
                        FastTlsBlockCipherImpl decrypt = new FastTlsBlockCipherImpl(CreateCbcBlockCipher(encryptionAlgorithm), false);

                        TlsHmac clientMac = CreateMac(cryptoParams, macAlgorithm);
                        TlsHmac serverMac = CreateMac(cryptoParams, macAlgorithm);

                        return new FastTlsBlockCipher(cryptoParams, encrypt, decrypt, clientMac, serverMac, 32);
                    }

                case EncryptionAlgorithm.AES_128_CCM:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Ccm(cryptoParams, 16, 16);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), false);
                        
                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 16, 16, TlsAeadCipher.AEAD_CCM);

                    }
                case EncryptionAlgorithm.AES_128_CCM_8:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Ccm(cryptoParams, 16, 8);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), false);

                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 16, 8, TlsAeadCipher.AEAD_CCM);
                    }
                case EncryptionAlgorithm.AES_256_CCM:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Ccm(cryptoParams, 32, 16);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), false);

                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 32, 16, TlsAeadCipher.AEAD_CCM);
                    }
                case EncryptionAlgorithm.AES_256_CCM_8:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Ccm(cryptoParams, 32, 8);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Ccm(), false);

                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 32, 8, TlsAeadCipher.AEAD_CCM);
                    }

                case EncryptionAlgorithm.AES_128_GCM:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Gcm(cryptoParams, 16, 16);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Gcm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Gcm(), false);

                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 16, 16, TlsAeadCipher.AEAD_GCM);
                    }

                case EncryptionAlgorithm.AES_256_GCM:
                    {
                        // NOTE: Ignores macAlgorithm
                        //return CreateCipher_Aes_Gcm(cryptoParams, 32, 16);
                        FastTlsAeadCipherImpl encrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Gcm(), true);
                        FastTlsAeadCipherImpl decrypt = new FastTlsAeadCipherImpl(CreateAeadCipher_Aes_Gcm(), false);

                        return new FastTlsAeadCipher(cryptoParams, encrypt, decrypt, 32, 16, TlsAeadCipher.AEAD_GCM);
                    }

                default:
                    return base.CreateCipher(cryptoParams, encryptionAlgorithm, macAlgorithm);
            }
        }

        protected override IBlockCipher CreateAesEngine()
        {
            //return new AesEngine();
            return new FastAesEngine();
        }

        protected override IAeadCipher CreateCcmMode(IBlockCipher engine)
        {
            return new FastCcmBlockCipher(engine);
        }

        protected override IAeadCipher CreateGcmMode(IBlockCipher engine)
        {
            // TODO Consider allowing custom configuration of multiplier
            return new FastGcmBlockCipher(engine);
        }

        protected override IBlockCipher CreateCbcBlockCipher(IBlockCipher blockCipher)
        {
            return new FastCbcBlockCipher(blockCipher);
        }
    }
}
#endif
