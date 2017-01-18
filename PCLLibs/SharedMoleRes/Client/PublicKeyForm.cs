using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SharedMoleRes.Client
{
    public class PublicKeyForm
    {
        protected byte[] _key;
        protected string _nameOfCryptoProvider;


        public PublicKeyForm() { }

        public PublicKeyForm(string nameOfCryptoProvider, CngKey key)
        {
            if (nameOfCryptoProvider == null) throw new ArgumentNullException(nameof(nameOfCryptoProvider)) { Source = GetType().AssemblyQualifiedName };
            if (key == null) throw new ArgumentNullException(nameof(key)) { Source = GetType().AssemblyQualifiedName };

            _nameOfCryptoProvider = nameOfCryptoProvider;
            _key = key.Export(CngKeyBlobFormat.GenericPublicBlob);
        }


        public string NameOfCryptoProvider {
            get { return _nameOfCryptoProvider; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

                _nameOfCryptoProvider = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentException">Value cannot be an empty collection.</exception>
        public virtual byte[] Key
        {
            get { return _key; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (value.Length == 0)
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(value)) { Source = GetType().AssemblyQualifiedName };

                _key = value;
            }
        }

        
        /// <exception cref="ArgumentNullException">key == null</exception>
        public Task SetKeyAsync(CngKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key)) { Source = GetType().AssemblyQualifiedName };

            return Task.Run(() => _key = key.Export(CngKeyBlobFormat.GenericPublicBlob));
        }
    }
}
