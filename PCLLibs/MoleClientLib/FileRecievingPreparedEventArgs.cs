using System;
using MoleClientLib.RemoteFileStream;

namespace MoleClientLib
{
    public class FileRecievingPreparedEventArgs
    {
        protected string Namee;
        protected long Lengthh;
        protected RemoteFileStreamBase Streamm;


        public FileRecievingPreparedEventArgs(string name, long length, RemoteFileStreamBase streamMole)
        {
            if (name == null) throw new ArgumentNullException(nameof(name)) {Source = GetType().AssemblyQualifiedName};
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Value cannot be null or empty.", nameof(name))
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name))
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length)) {Source = GetType().AssemblyQualifiedName};
            if (streamMole == null) throw new ArgumentNullException(nameof(streamMole)) {Source = GetType().AssemblyQualifiedName};
            if (!streamMole.IsInitialize)
                throw new ArgumentOutOfRangeException(nameof(streamMole.IsInitialize)) { Source = GetType().AssemblyQualifiedName };

            Namee = name;
            Lengthh = length;
            Streamm = streamMole;
        }


        public string Name => Namee;
        public long Length => Lengthh;
        public RemoteFileStreamBase Stream => Streamm;
    }
}
