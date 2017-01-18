using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

//using System.Security.Cryptography.

namespace CMCryptoFactoryLib
{
    public class CngMicrosoftFactory : CryptoFactoryBase
    {
        public CngMicrosoftFactory()
        {
            PossibleCryptoAlgs = new PossibleCryptoInfo(new[] { "CngMicrosoft" }, new[] { "Sha1", "Md5", "Sha256" },
                new[] { "Rsa" }, new[] { "Aes", "Des" }, new[] { "Ecc" });
        }


        public override PossibleCryptoInfo PossibleCryptoAlgs { get; }
        public override IReadOnlyDictionary<string, int> KeySizes { get; }


        public override SymmetricAlgorithm CreateSymmAlgInst(string nameOfCryptoProvider, string nameOfAlg)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            //ICryptoTransform encrypter = null, decrypter = null;
            //KeyDataForSymmetricAlgorithm keys = null;
            SymmetricAlgorithm alg = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            alg = Aes.Create();
                            break;
                        case "Des":
                            alg = TripleDES.Create();
                            break;
                    }
                    break;
            }
            alg.Mode = CipherMode.ECB;
            return alg;
        }
        public override SymmetricAlgorithm CreateSymmAlgInst(
            string nameOfCryptoProvider, string nameOfAlg, KeyDataForSymmetricAlgorithm keyData)
        {
            if (keyData == null)
                throw new ArgumentNullException(nameof(keyData)) { Source = GetType().AssemblyQualifiedName };
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            SymmetricAlgorithm alg = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            alg = Aes.Create();
                            break;
                        case "Des":
                            alg = TripleDES.Create();
                            break;
                    }
                    break;
            }
            alg.Key = keyData.SymmetricKeyBlob;
            alg.IV = keyData.SymmetricIvBlob;
            alg.Mode = CipherMode.ECB;
            //alg.Padding = PaddingMode.Zeros;
            return alg;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось.</exception>
        public override Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            ICryptoTransform encrypter = null, decrypter = null;
            KeyDataForSymmetricAlgorithm keys = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            using (var aes = Aes.Create())
                            {
                                //aes.Padding = PaddingMode.Zeros;
                                aes.Mode = CipherMode.ECB;
                                encrypter = aes.CreateEncryptor();
                                decrypter = aes.CreateDecryptor();
                                keys = new KeyDataForSymmetricAlgorithm(aes);
                            }
                            break;
                        case "Des":
                            using (var des = TripleDES.Create())
                            {
                                des.Mode = CipherMode.ECB;
                                //des.Padding = PaddingMode.Zeros;
                                encrypter = des.CreateEncryptor();
                                decrypter = des.CreateDecryptor();
                                keys = new KeyDataForSymmetricAlgorithm(des);
                            }
                            break;
                    }
                    break;
            }
            return new Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm>(encrypter, decrypter,
                keys);
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось. -or- Такой размер ключа для симметричного алгоритма шифрования не допустим.</exception>
        public override Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> CreateSymmetricAlgoritm
            <TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int keyLength,
                TKeyOptions options)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            ICryptoTransform encrypter = null, decrypter = null;
            KeyDataForSymmetricAlgorithm keys = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            //var aes = new AesCng()
                            using (var aes = Aes.Create())
                            {
                                if (
                                    !aes.LegalKeySizes.Any(
                                        sizes =>
                                            keyLength >= sizes.MinSize && keyLength <= sizes.MaxSize &&
                                            keyLength != sizes.SkipSize))
                                    throw new ArgumentOutOfRangeException(nameof(keyLength),
                                        "Такой размер ключа для симметричного алгоритма шифрования не допустим.");
                                aes.KeySize = keyLength;
                                //aes.Padding = PaddingMode.Zeros;
                                aes.Mode = CipherMode.ECB;
                                aes.GenerateKey();
                                encrypter = aes.CreateEncryptor();
                                decrypter = aes.CreateDecryptor();
                                keys = new KeyDataForSymmetricAlgorithm(aes);
                            }
                            break;
                        case "Des":
                            //var des = new TripleDESCng()
                            using (var des = TripleDES.Create())
                            {
                                if (
                                    !des.LegalKeySizes.Any(
                                        sizes =>
                                            keyLength >= sizes.MinSize && keyLength <= sizes.MaxSize &&
                                            keyLength != sizes.SkipSize))
                                    throw new ArgumentOutOfRangeException(nameof(keyLength),
                                        "Такой размер ключа для симметричного алгоритма шифрования не допустим.");
                                des.KeySize = keyLength;
                                //des.Padding = PaddingMode.Zeros;
                                des.Mode = CipherMode.ECB;
                                des.GenerateKey();
                                encrypter = des.CreateEncryptor();
                                decrypter = des.CreateDecryptor();
                                keys = new KeyDataForSymmetricAlgorithm(des);
                            }
                            break;
                    }
                    break;
            }
            return new Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm>(encrypter, decrypter,
                keys);
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось.</exception>
        public override IAsymmetricEncrypter CreateAsymmetricAlgoritm(string nameOfCryptoProvider, string nameOfAlg)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Rsa":
                            return new MoleRsaCng();
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null. -or- 
        /// key == null. Source: <see cref="MoleRsaCng(CngKey)"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось. -or- 
        /// key.Algorithm != CngAlgorithm.Rsa. Source: <see cref="MoleRsaCng(CngKey)"/>. -or- 
        /// keySize. Source: <see cref="MoleRsaCng(int)"/>.</exception>
        public override IAsymmetricEncrypter CreateAsymmetricAlgoritm<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int length,
            TKeyOptions options)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Rsa":
                            var optionsAsCngKey = options as CngKey;
                            return optionsAsCngKey != null ? new MoleRsaCng(optionsAsCngKey) : new MoleRsaCng(length);
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось.</exception>
        public override ISign CreateSignAlgoritm(string nameOfCryptoProvider, string nameOfAlg)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Ecc":
                            return new MoleECDsaCng();
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null. -or- 
        /// key == null. Source: <see cref="MoleECDsaCng(CngKey)"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось. -or- 
        /// key.Algorithm. Source: <see cref="MoleECDsaCng(CngKey)"/>. -or- 
        /// keySize. Source: <see cref="MoleECDsaCng(int)"/>.</exception>
        public override ISign CreateSignAlgoritm<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int length, TKeyOptions options)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Rsa":
                            var optionsAsCngKey = options as CngKey;
                            return optionsAsCngKey != null ? new MoleECDsaCng(optionsAsCngKey) : new MoleECDsaCng(length);
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось.</exception>
        public override HashAlgorithm CreateHashAlgorithm(string nameOfCryptoProvider, string nameOfAlg)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Sha1":
                            return SHA1.Create();
                        case "Md5":
                            return MD5.Create();
                        case "Sha256":
                            return SHA256.Create();
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null. -or- 
        /// key == null. Source: <see cref="MoleECDsaCng(CngKey)"/>. -or- 
        /// key == null. Source: <see cref="MoleRsaCng(CngKey)"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось. -or- 
        /// key.Algorithm. Source: <see cref="MoleECDsaCng(CngKey)"/>. -or- 
        /// keySize. Source: <see cref="MoleECDsaCng(int)"/>. -or- 
        /// key.Algorithm != CngAlgorithm.Rsa. Source: <see cref="MoleRsaCng(CngKey)"/>. -or- 
        /// keySize. Source: <see cref="MoleRsaCng(int)"/>.</exception>
        public override byte[] CreateAsymmetricKey<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int length, TKeyOptions options)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Rsa":
                            var rsa = new MoleRsaCng(length);
                            return rsa.Export(true);
                        case "Ecc":
                            var ecc = new MoleECDsaCng(length);
                            break;
                    }
                    break;
            }
            return null;
        }
        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- nameOfAlg == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Криптопровайдера с таким названием не нашлось. -or- 
        /// Криптоалгоритма с таким названием не нашлось.</exception>
        public override KeyDataForSymmetricAlgorithm CreateSymmetricKey<TKeyOptions>(string nameOfCryptoProvider, string nameOfAlg, int keyLength,
            int ivLength, TKeyOptions options)
        {
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            KeyDataForSymmetricAlgorithm keys = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            using (var aes = Aes.Create())
                            {
                                keys = new KeyDataForSymmetricAlgorithm(aes);
                            }
                            break;
                        case "Des":
                            using (var des = TripleDES.Create())
                            {
                                keys = new KeyDataForSymmetricAlgorithm(des);
                            }
                            break;
                    }
                    break;
            }
            return keys;
        }
        public override Tuple<ICryptoTransform, ICryptoTransform> CreateSymmetricAlgoritm(
            string nameOfCryptoProvider, string nameOfAlg, KeyDataForSymmetricAlgorithm keyData)
        {
            if (keyData == null)
                throw new ArgumentNullException(nameof(keyData)) {Source = GetType().AssemblyQualifiedName};
            CheckParams(nameOfCryptoProvider, nameOfAlg);

            SymmetricAlgorithm alg = null;
            switch (nameOfCryptoProvider)
            {
                case "CngMicrosoft":
                    switch (nameOfAlg)
                    {
                        case "Aes":
                            alg = Aes.Create();
                            break;
                        case "Des":
                            alg = TripleDES.Create();
                            break;
                    }
                    break;
            }
            alg.Key = keyData.SymmetricKeyBlob;
            alg.IV = keyData.SymmetricIvBlob;
            alg.Mode = CipherMode.ECB;
            //alg.Padding = PaddingMode.Zeros;
            return new Tuple<ICryptoTransform, ICryptoTransform>(alg.CreateEncryptor(), alg.CreateDecryptor());
        }


        private void CheckParams(string nameOfCryptoProvider, string nameOfAlg)
        {
            if (nameOfCryptoProvider == null) throw new ArgumentNullException(nameof(nameOfCryptoProvider)) { Source = GetType().AssemblyQualifiedName };
            if (nameOfAlg == null) throw new ArgumentNullException(nameof(nameOfAlg)) { Source = GetType().AssemblyQualifiedName };
            if (!PossibleCryptoAlgs.Providers.Contains(nameOfCryptoProvider))
                throw new ArgumentOutOfRangeException(nameof(nameOfCryptoProvider), "Криптопровайдера с таким названием не нашлось.")
                { Source = GetType().AssemblyQualifiedName };
            //if (!PossibleCryptoAlgs.Symmetric.Contains(nameOfAlg))
            //    throw new ArgumentOutOfRangeException(nameof(nameOfAlg), "Криптоалгоритма с таким названием не нашлось.")
            //    { Source = GetType().AssemblyQualifiedName };
        }
    }
}
