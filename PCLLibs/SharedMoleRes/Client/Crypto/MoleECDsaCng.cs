using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JabyLib.Other;

namespace SharedMoleRes.Client.Crypto
{
    public class MoleECDsaCng : ISign
    {
        protected ECDsaCng EcDsaCng;
        protected CustomBinarySerializerBase Ser = new ProtoBufSerializer();


        public MoleECDsaCng()
        {
            EcDsaCng = new ECDsaCng();
        }
        /// <exception cref="ArgumentOutOfRangeException">keySize.</exception>
        public MoleECDsaCng(int keySize)
        {
            var eccTemp = new ECDsaCng();
            if (!eccTemp.LegalKeySizes.Any(sizes => keySize <= sizes.MaxSize && keySize >= sizes.MinSize && keySize != sizes.SkipSize))
                throw new ArgumentOutOfRangeException(nameof(keySize)) { Source = GetType().AssemblyQualifiedName };

            EcDsaCng = new ECDsaCng(keySize);
        }
        /// <exception cref="ArgumentNullException">key.</exception>
        /// <exception cref="ArgumentOutOfRangeException">key.Algorithm.</exception>
        public MoleECDsaCng(CngKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key)) { Source = GetType().AssemblyQualifiedName };
            if (
                !new[] {CngAlgorithm.ECDsaP256, CngAlgorithm.ECDsaP384, CngAlgorithm.ECDsaP521}.Any(
                    algorithm => algorithm == key.Algorithm))
                throw new ArgumentOutOfRangeException(nameof(key.Algorithm)) {Source = GetType().AssemblyQualifiedName};

            EcDsaCng = new ECDsaCng(key);
        }
        public int KeySize => EcDsaCng.KeySize;
        public KeySizes[] LegalKeySizes => EcDsaCng.LegalKeySizes;


        public void Import(byte[] keys)
        {
            var key = new byte[keys.Length - 1];
            Array.Copy(keys, 1, key, 0, key.Length);
            EcDsaCng = keys[0] == 1
                ? new ECDsaCng(CngKey.Import(key, CngKeyBlobFormat.EccPrivateBlob))
                : new ECDsaCng(CngKey.Import(key, CngKeyBlobFormat.EccPublicBlob));
        }
        public byte[] Export(bool includePrivateKey)
        {
            var key = includePrivateKey
                ? EcDsaCng.Key.Export(CngKeyBlobFormat.EccPrivateBlob)
                : EcDsaCng.Key.Export(CngKeyBlobFormat.EccPublicBlob);
            var keyFull = new byte[key.Length + 1];
            if (includePrivateKey)
                keyFull[0] = 1;
            else
                keyFull[0] = 0;
            Array.Copy(key, 0, keyFull, 1, key.Length);
            return keyFull;
        }
        public void Dispose()
        {
            EcDsaCng.Dispose();
        }
        public byte[] SignData(byte[] dataForSign)
        {
            return EcDsaCng.SignData(dataForSign, HashAlgorithmName.SHA1);
        }
        public byte[] SignData<TParams>(byte[] dataForSign, TParams additionalParams)
        {
            var moleParams = additionalParams as MoleECDsaCngParams;
            if (moleParams != null)
            {
                return EcDsaCng.SignData(dataForSign, moleParams.HashAlgotitm);
            }
            throw new NotImplementedException();
        }
        public Task<byte[]> SignDataAsync(byte[] dataForSign)
        {
            return Task.Run(() => EcDsaCng.SignData(dataForSign, HashAlgorithmName.SHA1));
        }
        public Task<byte[]> SignDataAsync<TParams>(byte[] dataForSign, TParams additionalParams)
        {
            var moleParams = additionalParams as MoleECDsaCngParams;
            if (moleParams != null)
            {
                return Task.Run(() => EcDsaCng.SignData(dataForSign, moleParams.HashAlgotitm));
            }
            throw new NotImplementedException();
        }
        public bool VerifySign(byte[] dataForSign, byte[] sign)
        {
            return EcDsaCng.VerifyData(dataForSign, sign, HashAlgorithmName.SHA1);
        }
        public bool VerifySign<TParams>(byte[] dataForSign, byte[] sign, TParams additionalParams)
        {
            var moleParams = additionalParams as MoleECDsaCngParams;
            if (moleParams != null)
            {
                return EcDsaCng.VerifyData(dataForSign, sign, moleParams.HashAlgotitm);
            }
            throw new NotImplementedException();
        }
        public Task<bool> VerifySignAsync(byte[] dataForSign, byte[] sign)
        {
            return Task.Run(() => EcDsaCng.VerifyData(dataForSign, sign, HashAlgorithmName.SHA1));
        }
        public Task<bool> VerifySignAsync<TParams>(byte[] dataForSign, byte[] sign, TParams additionalParams)
        {
            var moleParams = additionalParams as MoleECDsaCngParams;
            if (moleParams != null)
            {
                return Task.Run(() => EcDsaCng.VerifyData(dataForSign, sign, moleParams.HashAlgotitm));
            }
            throw new NotImplementedException();
        }
    }
}
