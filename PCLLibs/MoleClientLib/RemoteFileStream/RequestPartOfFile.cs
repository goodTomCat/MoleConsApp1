using System;

namespace MoleClientLib.RemoteFileStream
{
    public class RequestPartOfFile
    {
        protected string NameOfFilee;
        protected long Positionn;
        protected int Lengthh;


        /// <exception cref="ArgumentNullException">NameOfFile == null.</exception>
        /// <exception cref="ArgumentException">Value cannot be null or empty. -or- Value cannot be null or whitespace.</exception>
        public string NameOfFile
        {
            get { return NameOfFilee; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) { Source = GetType().AssemblyQualifiedName };

                NameOfFilee = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Значение меньше нуля.</exception>
        public long Position
        {
            get { return Positionn; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

                Positionn = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Значение меньше нуля.</exception>
        public int Length
        {
            get { return Lengthh; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

                Lengthh = value;
            }
        }
    }
}
