using System;

namespace MoleClientLib
{
    public class ExceptionCatchedEventArgs : EventArgs
    {
        public ExceptionCatchedEventArgs(Exception ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex)) {Source = GetType().AssemblyQualifiedName};

            Exception = ex;
        }


        public Exception Exception { get; protected set; }
    }
}
