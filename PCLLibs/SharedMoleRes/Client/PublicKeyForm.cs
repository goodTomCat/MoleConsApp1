using System;
using System.Linq;
//using System.Reflection;
using System.Security.Cryptography;
//using System.Threading.Tasks;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server.Surrogates;

namespace SharedMoleRes.Client
{
    public class PublicKeyForm
    {
        /// <summary>
        /// DbContext.
        /// </summary>
        public UserFormSurrogate UserForm { get; set; }
        /// <summary>
        /// DbContext.
        /// </summary>
        public int UserFormId { get; set; }
        /// <summary>
        /// DbContext.
        /// </summary>
        public int Id { get; set; }



        /// <summary>
        /// Открытый ключ.
        /// </summary>
        public byte[] Key { get; set; }
        /// <summary>
        /// Название крипто-провайдера.
        /// </summary>
        public string CryptoProvider { get; set; }
        /// <summary>
        /// Название асимметричного алгоритма.
        /// </summary>
        public string CryptoAlg { get; set; }
        /// <summary>
        /// Название хеш алгоритма.
        /// </summary>
        public string HashAlg { get; set; }
        /// <summary>
        /// Хеш сумма открытого ключа.
        /// </summary>
        public byte[] Hash { get; set; }
        /// <summary>
        /// Подпись хеш суммы открытого ключа.
        /// </summary>
        public byte[] Sign { get; set; }


        /// <exception cref="ArgumentNullException">cryptoFactory == null. -or- publicKeySign == null.</exception>
        /// <exception cref="InvalidOperationException">Одно из свойств не было указано.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="CryptoFactoryBase.CreateHashAlgorithm(string, string)"/>. -or- 
        /// Source: <see cref="CryptoFactoryBase.CreateSignAlgoritm(string, string)"/>.</exception>
        /// <exception cref="CryptographicException">Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>.</exception>
        public bool ValidateSign(CryptoFactoryBase cryptoFactory, byte[] publicKeySign)
        {
            if (cryptoFactory == null)
                throw new ArgumentNullException(nameof(cryptoFactory)) {Source = GetType().AssemblyQualifiedName};
            if (publicKeySign == null)
                throw new ArgumentNullException(nameof(publicKeySign)) {Source = GetType().AssemblyQualifiedName};
            if (Key == null || CryptoProvider == null || CryptoAlg == null || HashAlg == null || Hash == null ||
                Sign == null)
                throw new InvalidOperationException("Одно из свойств не было указано.")
                    {Source = GetType().AssemblyQualifiedName};

            var hashAlg = cryptoFactory.CreateHashAlgorithm(CryptoProvider, HashAlg);
            var hashTrue = hashAlg.ComputeHash(Key);
            if (!hashTrue.SequenceEqual(Hash))
                return false;

            var signAlg = cryptoFactory.CreateSignAlgoritm(CryptoProvider, CryptoAlg);
            signAlg.Import(publicKeySign);
            return signAlg.VerifySign(hashTrue, Sign);

        }

    }
}
