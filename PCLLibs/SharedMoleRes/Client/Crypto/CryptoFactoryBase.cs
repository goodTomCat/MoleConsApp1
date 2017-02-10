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


        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract SymmetricAlgorithm CreateSymmAlgInst(string nameOfCryptoProvider,
             string nameOfAlg, KeyDataForSymmetricAlgorithm keyData);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract SymmetricAlgorithm CreateSymmAlgInst(string nameOfCryptoProvider, string nameOfAlg);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm<TKeyOptions>(
            string nameOfCryptoProvider, string nameOfAlg, int keyLength,
            TKeyOptions options);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract IAsymmetricEncrypter CreateAsymmetricAlgoritm(string nameOfCryptoProvider, string nameOfAlg);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract IAsymmetricEncrypter CreateAsymmetricAlgoritm<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int length, TKeyOptions options);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract ISign CreateSignAlgoritm(string nameOfProvider, string nameOfAlg);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract ISign CreateSignAlgoritm<TKeyOptions>(string nameOfProvider, string nameOfAlg, int length, TKeyOptions options);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract HashAlgorithm CreateHashAlgorithm(string nameOfProvider, string nameOfAlg);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract byte[] CreateAsymmetricKey<TKeyOptions>(string nameOfProvider, string nameOfAlg, int length, TKeyOptions options);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract KeyDataForSymmetricAlgorithm CreateSymmetricKey<TKeyOptions>(string nameOfProvider, string nameOfAlg, int keyLength,
            int ivLength, TKeyOptions options);
        /// <exception cref="ArgumentOutOfRangeException">nameOfCryptoProvider. -or- nameOfAlg.</exception>
        public abstract Tuple<ICryptoTransform, ICryptoTransform> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg, KeyDataForSymmetricAlgorithm keyData);

    }
}
