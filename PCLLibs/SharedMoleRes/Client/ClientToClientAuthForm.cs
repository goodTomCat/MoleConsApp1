using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

namespace SharedMoleRes.Client
{
    public class ClientToClientAuthForm
    {
        public string Login { get; set; }
        public byte[] Hash { get; set; }
        public byte[] Sign { get; set; }


        /// <exception cref="ArgumentNullException">factory == null. -or- signImpl == null.</exception>
        /// <exception cref="InvalidOperationException">Login == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="CryptoFactoryBase.CreateHashAlgorithm(string, string)"/></exception>
        public byte[] CreateSign(CryptoFactoryBase factory, string hashAlgName, string providerName, ISign signImpl,
            out byte[] hash)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory)) {Source = GetType().AssemblyQualifiedName};
            if (signImpl == null)
                throw new ArgumentNullException(nameof(signImpl)) {Source = GetType().AssemblyQualifiedName};
            if (Login == null)
                throw new InvalidOperationException("Login == null.")
                    {Source = GetType().AssemblyQualifiedName};

            try
            {
                var hashAlg = factory.CreateHashAlgorithm(providerName, hashAlgName);
                var loginAsBytes = Encoding.UTF8.GetBytes(Login);
                hash = hashAlg.ComputeHash(loginAsBytes);
                var sign = signImpl.SignData(hash);
                return sign;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //factory.CreateHashAlgorithm(providerName, hashAlgName);
                throw;
            }
        }
        /// <exception cref="ArgumentNullException">factory == null. -or- signImpl == null. -or- 
        /// publicKeyForm == null.</exception>
        /// <exception cref="InvalidOperationException">Login == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="CryptoFactoryBase.CreateHashAlgorithm(string, string)"/></exception>
        public byte[] CreateSign(CryptoFactoryBase factory, PublicKeyForm publicKeyForm, ISign signImpl,
            out byte[] hash)
        {
            if (publicKeyForm == null)
                throw new ArgumentNullException(nameof(publicKeyForm)) {Source = GetType().AssemblyQualifiedName};

            return CreateSign(factory, publicKeyForm.HashAlg, publicKeyForm.CryptoProvider, signImpl, out hash);
        }
        /// <exception cref="Exception">All exceptions from Source: <see cref="CreateSign(CryptoFactoryBase, string, string, ISign,out byte[])"/>.</exception>
        public void CreateSign(CryptoFactoryBase factory, string hashAlgName, string providerName, ISign signImpl)
        {
            byte[] hash;
            Sign = CreateSign(factory, hashAlgName, providerName, signImpl, out hash);
            Hash = hash;
        }
        /// <exception cref="Exception">All exceptions from Source: <see cref="CreateSign(CryptoFactoryBase, PublicKeyForm, ISign, out byte[])"/>.</exception>
        public void CreateSign(CryptoFactoryBase factory, PublicKeyForm publicKeyForm, ISign signImpl)
        {
            byte[] hash;
            Sign = CreateSign(factory, publicKeyForm, signImpl, out hash);
            Hash = hash;
        }
        /// <exception cref="ArgumentNullException">cryptoFactory == null. -or- publicKeyForm == null. -or- 
        /// Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>. -or- cryptoProvider == null. -or- hashAlgName == null. -or- 
        /// signAlgName == null. -or- publicKey == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="CryptoFactoryBase.CreateHashAlgorithm(string, string)"/>. -or- 
        /// Source: <see cref="CryptoFactoryBase.CreateSignAlgoritm(string, string)"/>. -or- 
        /// Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>.</exception>
        /// <exception cref="CryptographicException">Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>.</exception>
        /// <exception cref="InvalidOperationException">Login == null.</exception>
        public bool ValidateSign(CryptoFactoryBase cryptoFactory, PublicKeyForm publicKeyForm)
        {
            if (cryptoFactory == null)
                throw new ArgumentNullException(nameof(cryptoFactory)) {Source = GetType().AssemblyQualifiedName};
            if (publicKeyForm == null)
                throw new ArgumentNullException(nameof(publicKeyForm)) {Source = GetType().AssemblyQualifiedName};
            if (Login == null)
                throw new InvalidOperationException("Login == null.") { Source = GetType().AssemblyQualifiedName };

            try
            {
                return ValidateSignImpl(cryptoFactory, publicKeyForm.CryptoProvider, publicKeyForm.HashAlg,
                    publicKeyForm.CryptoAlg, publicKeyForm.Key);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //cryptoFactory.CreateHashAlgorithm(publicKeyForm.CryptoProvider, publicKeyForm.HashAlg);
                //cryptoFactory.CreateSignAlgoritm(publicKeyForm.CryptoProvider, publicKeyForm.CryptoAlg);
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }
            catch (ArgumentNullException ex)
            {
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }
            catch (CryptographicException ex)
            {
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }

        }
        /// <exception cref="ArgumentNullException">cryptoFactory == null. -or- 
        /// Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>. -or- cryptoProvider == null. -or- hashAlgName == null. -or- 
        /// signAlgName == null. -or- publicKey == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="CryptoFactoryBase.CreateHashAlgorithm(string, string)"/>. -or- 
        /// Source: <see cref="CryptoFactoryBase.CreateSignAlgoritm(string, string)"/>. -or- 
        /// Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>.</exception>
        /// <exception cref="CryptographicException">Source: <see cref="IAsymmetricKeysExchange.Import(byte[])"/>.</exception>
        /// <exception cref="InvalidOperationException">Login == null.</exception>
        public bool ValidateSign(CryptoFactoryBase cryptoFactory, string cryptoProvider, string hashAlgName, string signAlgName,
            byte[] publicKey)
        {
            if (cryptoFactory == null)
                throw new ArgumentNullException(nameof(cryptoFactory)) { Source = GetType().AssemblyQualifiedName };
            if (cryptoProvider == null)
                throw new ArgumentNullException(nameof(cryptoProvider)) { Source = GetType().AssemblyQualifiedName };
            if (hashAlgName == null)
                throw new ArgumentNullException(nameof(hashAlgName)) { Source = GetType().AssemblyQualifiedName };
            if (signAlgName == null)
                throw new ArgumentNullException(nameof(signAlgName)) { Source = GetType().AssemblyQualifiedName };
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey)) { Source = GetType().AssemblyQualifiedName };
            if (Login == null)
                throw new InvalidOperationException("Login == null.") { Source = GetType().AssemblyQualifiedName };

            try
            {
                return ValidateSignImpl(cryptoFactory, cryptoProvider, hashAlgName, signAlgName, publicKey);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //cryptoFactory.CreateHashAlgorithm(publicKeyForm.CryptoProvider, publicKeyForm.HashAlg);
                //cryptoFactory.CreateSignAlgoritm(publicKeyForm.CryptoProvider, publicKeyForm.CryptoAlg);
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }
            catch (ArgumentNullException ex)
            {
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }
            catch (CryptographicException ex)
            {
                //signAlg.Import(publicKeyForm.Key);
                throw;
            }

        }


        private bool ValidateSignImpl(CryptoFactoryBase cryptoFactory, string cryptoProvider, string hashAlgName,
            string signAlgName,
            byte[] publicKey)
        {
            var hashAlg = cryptoFactory.CreateHashAlgorithm(cryptoProvider, hashAlgName);
            var loginAsBytes = Encoding.UTF8.GetBytes(Login);
            var hashTrue = hashAlg.ComputeHash(loginAsBytes);
            if (!hashTrue.SequenceEqual(Hash))
                return false;

            var signAlg = cryptoFactory.CreateSignAlgoritm(cryptoProvider, signAlgName);
            signAlg.Import(publicKey);
            return signAlg.VerifySign(hashTrue, Sign);
        }
    }

}
