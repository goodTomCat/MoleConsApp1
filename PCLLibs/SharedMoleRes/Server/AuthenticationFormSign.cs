using System;

namespace SharedMoleRes.Server
{
    public class AuthenticationFormSign : IAuthenticationFormSign
    {
        private string _login;
        private byte[] _sign;
        private byte[] _hash;


        public AuthenticationMethod AuthenticationMethod => AuthenticationMethod.Sign;
        public string CryptoProvider { get; set; }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        public virtual string Login
        {
            get { return _login; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

                _login = value;
            }
        }
        public virtual byte[] Hash
        {
            get { return _hash; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (value.Length == 0)
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                _hash = value;
            }
        }
        public string SignantureAlgoritmName { get; set; }
        public string HashAlgotitmName { get; set; }

        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentException">Value cannot be an empty collection.</exception>
        public virtual byte[] Sign
        {
            get { return _sign; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (value.Length == 0)
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                _sign = value;
            }
        }
    }
}
