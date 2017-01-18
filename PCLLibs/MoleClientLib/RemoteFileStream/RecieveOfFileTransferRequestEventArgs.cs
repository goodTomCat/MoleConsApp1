using System;
using System.Net;

namespace MoleClientLib.RemoteFileStream
{
    public class RecieveOfFileTransferRequestEventArgs : EventArgs
    {
        protected bool Allow;
        private bool _wasSeted;


        public IPAddress Ip { get; set; }
        public string Login { get; set; }
        public bool IsAllow => Allow;
        public string FileName { get; set; }
        public long Length { get; set; }
        public RemoteFileStreamBase RemoteFileStream { get; protected set; }



        public bool TryAllowContinueRegistration(bool allow, RemoteFileStreamBase remoteFileStream)
        {
            if (_wasSeted)
                return false;

            Allow = allow;
            _wasSeted = true;
            RemoteFileStream = remoteFileStream;
            return true;
        }
        /// <exception cref="InvalidOperationException">Разрешение уже было установлено.</exception>
        public bool AllowContinueRegistration(bool allow, RemoteFileStreamBase remoteFileStream)
        {
            if (_wasSeted)
                throw new InvalidOperationException("Разрешение уже было установлено.") { Source = GetType().AssemblyQualifiedName };

            Allow = allow;
            _wasSeted = true;
            RemoteFileStream = remoteFileStream;
            return true;
        }
    }
}
