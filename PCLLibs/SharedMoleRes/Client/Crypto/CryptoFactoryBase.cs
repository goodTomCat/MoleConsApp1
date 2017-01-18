using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using SharedMoleRes.Server;

namespace SharedMoleRes.Client.Crypto
{
    public abstract class CryptoFactoryBase
    {
        public abstract PossibleCryptoInfo PossibleCryptoAlgs { get; }
        public abstract IReadOnlyDictionary<string, int> KeySizes { get; }

        public abstract SymmetricAlgorithm CreateSymmAlgInst(string nameOfCryptoProvider,
             string nameOfAlg, KeyDataForSymmetricAlgorithm keyData);
        public abstract SymmetricAlgorithm CreateSymmAlgInst(string nameOfCryptoProvider, string nameOfAlg);
        public abstract Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg);
        public abstract Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm<TKeyOptions>(
            string nameOfCryptoProvider, string nameOfAlg, int keyLength,
            TKeyOptions options);
        public abstract IAsymmetricEncrypter CreateAsymmetricAlgoritm(string nameOfCryptoProvider, string nameOfAlg);
        public abstract IAsymmetricEncrypter CreateAsymmetricAlgoritm<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int length, TKeyOptions options);
        public abstract ISign CreateSignAlgoritm(string nameOfProvider, string nameOfAlg);
        public abstract ISign CreateSignAlgoritm<TKeyOptions>(string nameOfProvider, string nameOfAlg, int length, TKeyOptions options);
        public abstract HashAlgorithm CreateHashAlgorithm(string nameOfProvider, string nameOfAlg);
        public abstract byte[] CreateAsymmetricKey<TKeyOptions>(string nameOfProvider, string nameOfAlg, int length, TKeyOptions options);
        public abstract KeyDataForSymmetricAlgorithm CreateSymmetricKey<TKeyOptions>(string nameOfProvider, string nameOfAlg, int keyLength,
            int ivLength, TKeyOptions options);
        public abstract Tuple<ICryptoTransform, ICryptoTransform> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg, KeyDataForSymmetricAlgorithm keyData);

    }
}
