using System;

namespace SharedMoleRes.Server
{
    public class AuthenticationFormClassic : IAuthenticationFormClassic
    {
        private byte[] _hashOfPassword;
        private string _login;

        public AuthenticationMethod AuthenticationMethod => AuthenticationMethod.Classic;
        public string CryptoProvider { get; set; }
        public string HashAlgotitm { get; set; }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentException">Value cannot be an empty collection.</exception>
        public virtual byte[] HashOfPassword
        {
            get { return _hashOfPassword; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (value.Length == 0)
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                _hashOfPassword = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        public virtual string Login
        {
            get { return _login; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                _login = value;
            }
        }


        //public virtual bool VerifyPassword(string password, ResultOfOperation resultOfOperation)
        //{
        //    if (password == null) throw new ArgumentNullException(nameof(password)) {Source = GetType().AssemblyQualifiedName};
        //    if (HashOfPassword == null)
        //        throw new InvalidOperationException("Свойство HashOfPassword не задано.")
        //        { Source = GetType().AssemblyQualifiedName};

        //    resultOfOperation.OperationWasFinishedSuccessful = false;
        //    switch (CryptoProvider)
        //    {
        //        case CryptoProvider.CngMicrosoft:
        //            HashAlgorithm hashAlgorithm = SHA1.Create();
        //            switch (HashAlgotitm)
        //            {
        //                case HashAlgotitm.Md5:
        //                    hashAlgorithm = MD5.Create();
        //                    break;
        //                case HashAlgotitm.Sha1:
        //                    hashAlgorithm = SHA1.Create();
        //                    break;
        //                case HashAlgotitm.Sha256:
        //                    hashAlgorithm = SHA256.Create();
        //                    break;
        //                case HashAlgotitm.Sha384:
        //                    hashAlgorithm = SHA384.Create();
        //                    break;
        //                case HashAlgotitm.Sha512:
        //                    hashAlgorithm = SHA512.Create();
        //                    break;
        //            }
        //            var hashOfPasswordReally = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(password));
        //            if (!hashOfPasswordReally.SequenceEqual(HashOfPassword))
        //            {
        //                resultOfOperation.ErrorMessage = "Неверный пароль.";
        //                resultOfOperation.OperationWasFinishedSuccessful = false;
        //            }
        //            resultOfOperation.OperationWasFinishedSuccessful = true;
        //            break;
        //    }
        //    return resultOfOperation.OperationWasFinishedSuccessful;
        //}
    }
}
