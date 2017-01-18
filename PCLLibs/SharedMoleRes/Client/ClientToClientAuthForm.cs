using System;
using SharedMoleRes.Server;

namespace SharedMoleRes.Client
{
    public class ClientToClientAuthForm
    {
        protected string Log;
        protected KeyDataForSymmetricAlgorithm PrivKey;

        public virtual string Login
        {
            get { return Log; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

                Log = value;
            }
        }

        public virtual KeyDataForSymmetricAlgorithm PrivateKey
        {
            get { return PrivKey; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                PrivKey = value;
            }
        }
    }
}
