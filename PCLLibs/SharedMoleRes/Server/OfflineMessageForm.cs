using System;

namespace SharedMoleRes.Server
{
    public class OfflineMessageForm
    {
        private string _loginOfReciever;
        private byte[] _messageAsBytes;

        public byte[] Message
        {
            get { return _messageAsBytes; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (value.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна 0.");

                _messageAsBytes = value;
            }
        }

        public string LoginOfReciever
        {
            get { return _loginOfReciever; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (value.Length > 15 || value.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Длина логина превышает 15 символов или равна нулю.")
                    { Source = GetType().AssemblyQualifiedName };

                _loginOfReciever = value;
            }
        }
        
    }
}
