using System;

namespace SharedMoleRes.Client
{
    public class SignForm
    {
        public SignForm(string cryptoProvider, byte[] signPublicKeyBlob, string cryptoAlgoritmName, byte[] sign, byte[] data)
        {
            if (cryptoProvider == null) throw new ArgumentNullException(nameof(cryptoProvider)) {Source = GetType().AssemblyQualifiedName};
            if (signPublicKeyBlob == null) throw new ArgumentNullException(nameof(signPublicKeyBlob))
            { Source = GetType().AssemblyQualifiedName };
            if (cryptoAlgoritmName == null) throw new ArgumentNullException(nameof(cryptoAlgoritmName))
            { Source = GetType().AssemblyQualifiedName };
            if (sign == null) throw new ArgumentNullException(nameof(sign)) { Source = GetType().AssemblyQualifiedName };
            if (data == null) throw new ArgumentNullException(nameof(data)) { Source = GetType().AssemblyQualifiedName };

            CryptoProvider = cryptoProvider;
            SignPublicKeyBlob = signPublicKeyBlob;
            CryptoAlgoritmName = cryptoAlgoritmName;
            CryptoProvider = cryptoProvider;
            Sign = sign;
            Data = data;
        }


        public string CryptoProvider { get; }
        public byte[] SignPublicKeyBlob { get; }
        public string CryptoAlgoritmName { get; }
        public byte[] Sign { get; }
        public byte[] Data { get; }
    }
}
