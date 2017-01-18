using System;
using System.IO;

namespace MoleClientLib
{
    public class FileRecieveRequest
    {
        protected long Len;
        protected string Nam = "Empty";


        public FileRecieveRequest() { }
        /// <exception cref="ArgumentNullException">fileInfo == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Файла не существует. -or- Длина файла равна нулю.</exception>
        public FileRecieveRequest(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo)) {Source = GetType().AssemblyQualifiedName};
            if (!fileInfo.Exists)
                throw new ArgumentOutOfRangeException(nameof(fileInfo.Exists), "Файла не существует.")
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (fileInfo.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(fileInfo.Length), "Длина файла равна нулю.")
                {
                    Source = GetType().AssemblyQualifiedName
                };

            Len = fileInfo.Length;
            Nam = fileInfo.Name;
        }


        /// <exception cref="ArgumentOutOfRangeException">Значение длины файла меньше нуля.</exception>
        public virtual long Length
        {
            get { return Len; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Значение длины файла меньше нуля.") {Source = GetType().AssemblyQualifiedName};

                Len = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentException">Value cannot be null or empty. -or- Value cannot be null or whitespace.</exception>
        public virtual string Name
        {
            get { return Nam; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                Nam = value;
            }
        }
    }
}
