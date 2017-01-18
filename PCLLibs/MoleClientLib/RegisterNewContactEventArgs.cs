using System;
using System.Net;

namespace MoleClientLib
{
    public class RegisterNewContactEventArgs : EventArgs
    {
        protected bool Allow;
        private bool _wasSeted;


        public IPAddress Ip { get; set; }
        public string Login { get; set; }
        public bool IsAllow => Allow;


        public bool TryAllowContinueRegistration(bool allow)
        {
            if (_wasSeted)
                return false;

            Allow = allow;
            _wasSeted = true;
            return true;
        }
        /// <exception cref="InvalidOperationException">Разрешение уже было установлено.</exception>
        public bool AllowContinueRegistration(bool allow)
        {
            if (_wasSeted)
                throw new InvalidOperationException("Разрешение уже было установлено.") {Source = GetType().AssemblyQualifiedName};

            Allow = allow;
            _wasSeted = true;
            return true;
        }
    }
}
