using System;
using System.Security.Cryptography;

namespace SharedMoleRes.Server
{
    public class KeyDataForSymmetricAlgorithm
    {
        
        /// <exception cref="ArgumentNullException">symmetricKeyBlob == null. -или- symmetricIvBlob == null.</exception>
        public KeyDataForSymmetricAlgorithm(byte[] symmetricKeyBlob, byte[] symmetricIvBlob)
        {
            if (symmetricKeyBlob == null)
                throw new ArgumentNullException(nameof(symmetricKeyBlob)) {Source = GetType().AssemblyQualifiedName};
            if (symmetricIvBlob == null)
                throw new ArgumentNullException(nameof(symmetricIvBlob)) {Source = GetType().AssemblyQualifiedName};

            SymmetricKeyBlob = symmetricKeyBlob;
            SymmetricIvBlob = symmetricIvBlob;
        }
        /// <exception cref="ArgumentNullException">keyData == null.</exception>
        public KeyDataForSymmetricAlgorithm(KeyDataForSymmetricAlgorithm keyData)
        {
            if (keyData == null) throw new ArgumentNullException(nameof(keyData)) {Source = GetType().AssemblyQualifiedName};

            if (keyData.SymmetricIvBlob != null)
                SymmetricIvBlob = (byte[])keyData.SymmetricIvBlob.Clone();
            if (keyData.SymmetricKeyBlob != null)
                SymmetricKeyBlob = (byte[]) keyData.SymmetricKeyBlob.Clone();
        }
        /// <exception cref="ArgumentNullException">algorithm == null.</exception>
        public KeyDataForSymmetricAlgorithm(SymmetricAlgorithm algorithm)
        {
            if (algorithm == null) throw new ArgumentNullException(nameof(algorithm)) {Source = GetType().AssemblyQualifiedName};

            if (algorithm.IV == null)
                algorithm.GenerateIV();
            if (algorithm.Key == null)
                algorithm.GenerateKey();

            SymmetricIvBlob = algorithm.IV;
            SymmetricKeyBlob = algorithm.Key;
        }


        public byte[] SymmetricKeyBlob { get; }
        public byte[] SymmetricIvBlob { get; }
    }
}
