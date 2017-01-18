using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JabyLib.Other;

namespace SharedMoleRes.Client.Crypto
{
    public class MoleRsaCng : IAsymmetricEncrypter
    {
        protected RSACng Rsa;
        protected CustomBinarySerializerBase Ser = new ProtoBufSerializer();


        public MoleRsaCng()
        {
            Rsa = new RSACng();
            KeySize = Rsa.KeySize;
        }
        /// <exception cref="ArgumentOutOfRangeException">keySize.</exception>
        public MoleRsaCng(int keySize) : this()
        {
            if (!Rsa.LegalKeySizes.Any(sizes => keySize <= sizes.MaxSize && keySize >= sizes.MinSize && keySize != sizes.SkipSize))
                throw new ArgumentOutOfRangeException(nameof(keySize)) { Source = GetType().AssemblyQualifiedName };

            KeySize = keySize;
            Rsa = new RSACng(keySize);
        }
        /// <exception cref="ArgumentNullException">key == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">key.Algorithm != CngAlgorithm.Rsa.</exception>
        public MoleRsaCng(CngKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key)) { Source = GetType().AssemblyQualifiedName };
            if (key.Algorithm != CngAlgorithm.Rsa)
                throw new ArgumentOutOfRangeException(nameof(key.Algorithm)) {Source = GetType().AssemblyQualifiedName};

            Rsa = new RSACng(key);
            KeySize = Rsa.KeySize;
        }


        public int KeySize { get; }
        public virtual KeySizes[] LegalKeySizes => Rsa.LegalKeySizes;


        public virtual void Import(byte[] keys)
        {
            var rsaParams = Ser.Deserialize<RSAParameters>(keys, false);
            Rsa.ImportParameters(rsaParams);
        }
        public virtual byte[] Export(bool includePrivateKey)
        {
            var rsaParams = Rsa.ExportParameters(includePrivateKey);
            return Ser.Serialize(rsaParams, false);
        }
        public virtual void Dispose()
        {
            Rsa.Dispose();
        }
        public virtual byte[] Encrypt(byte[] dataForEncryption)
        {
            return Rsa.Encrypt(dataForEncryption, RSAEncryptionPadding.Pkcs1);
        }
        public virtual byte[] Encrypt<TParams>(byte[] dataForEncryption, TParams additionalParams)
        {
            var moleRsaParams = additionalParams as MoleRsaCngParams;
            if (moleRsaParams != null)
                return Rsa.Encrypt(dataForEncryption, moleRsaParams.Params);

            throw new NotImplementedException();
        }
        public virtual Task<byte[]> EncryptAsync(byte[] dataForEncryption)
        {
            return Task.Run(() => Rsa.Encrypt(dataForEncryption, RSAEncryptionPadding.Pkcs1));
        }
        public virtual Task<byte[]> EncryptAsync<TParams>(byte[] dataForEncryption, TParams additionalParams)
        {
            var moleRsaParams = additionalParams as MoleRsaCngParams;
            if (moleRsaParams != null)
                return Task.Run(() => Rsa.Encrypt(dataForEncryption, moleRsaParams.Params));

            throw new NotImplementedException();
        }
        public virtual byte[] Decrypt(byte[] dataForDecryption)
        {
            return Rsa.Decrypt(dataForDecryption, RSAEncryptionPadding.Pkcs1);
        }
        public virtual byte[] Decrypt<TParams>(byte[] dataForDecryption, TParams additionalParams)
        {
            var moleRsaParams = additionalParams as MoleRsaCngParams;
            if (moleRsaParams != null)
                return Rsa.Decrypt(dataForDecryption, moleRsaParams.Params);

            throw new NotImplementedException();
        }
        public virtual Task<byte[]> DecryptAsync(byte[] dataForDecryption)
        {
            return Task.Run(() => Rsa.Decrypt(dataForDecryption, RSAEncryptionPadding.Pkcs1));
        }
        public virtual Task<byte[]> DecryptAsync<TParams>(byte[] dataForDecryption, TParams additionalParams)
        {
            var moleRsaParams = additionalParams as MoleRsaCngParams;
            if (moleRsaParams != null)
                return Task.Run(() => Rsa.Decrypt(dataForDecryption, moleRsaParams.Params));

            throw new NotImplementedException();
        }
    }
}
