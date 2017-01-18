using System;
using System.IO;
using System.Threading.Tasks;

namespace MoleClientLib.RemoteFileStream
{
    public abstract class RemoteFileStreamBase : Stream
    {
        protected Stream StreamForTempSaving;
        protected long Lengthh;
        protected long Positionn;


        /// <exception cref="ArgumentNullException">streamForTempSaving == null. -or- string name Value cannot be null or empty. -or- 
        /// string name Value cannot be null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Поток должен поддерживать операции записи, чтения и поиска.</exception>
        protected RemoteFileStreamBase(Stream streamForTempSaving, string name)
        {
            if (streamForTempSaving == null) throw new ArgumentNullException(nameof(streamForTempSaving)) {Source = GetType().AssemblyQualifiedName};
            if (name == null) throw new ArgumentNullException(nameof(name)) { Source = GetType().AssemblyQualifiedName };
            if (!streamForTempSaving.CanRead || !streamForTempSaving.CanSeek || !streamForTempSaving.CanWrite)
                throw new ArgumentOutOfRangeException(nameof(streamForTempSaving),
                        "Поток должен поддерживать операции записи, чтения и поиска.")
                    {Source = GetType().AssemblyQualifiedName};
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Value cannot be null or empty.", nameof(name))
                    {Source = GetType().AssemblyQualifiedName};
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name))
                {
                    Source = GetType().AssemblyQualifiedName
                };

            StreamForTempSaving = streamForTempSaving;
            Lengthh = StreamForTempSaving.Length;
            Name = name;
        }
        protected RemoteFileStreamBase(string name, long length)
        {
            if (name == null) throw new ArgumentNullException(nameof(name)) { Source = GetType().AssemblyQualifiedName };
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            Name = name;
            Lengthh = length;
        }


        public override long Position
        {
            get { return Positionn; }
            set
            {
                if (StreamForTempSaving != null)
                    StreamForTempSaving.Seek(value, SeekOrigin.Begin);
                else if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                Positionn = value;
            }
        }
        public sealed override long Length => Lengthh;
        public bool IsInitialize { get; protected set; }
        public string Name { get; protected set; }


        public abstract Task InitializeAsync();
        public new abstract Task<int> ReadAsync(byte[] buffer, int offset, int count);


        /// <summary>
        /// Запрашивает и получает удаленную часть файла.
        /// </summary>
        protected abstract Task<byte[]> GetPartAsync(long pos, int length);
        /// <summary>
        /// Запрашивает и получает удаленную часть файла.
        /// </summary>
        protected virtual Task<byte[]> GetPartAsync(long pos, long length)
        {
            try
            {
                return GetPartAsync(pos, (int) length);
            }
            catch (InvalidCastException ex)
            {
                var exc = new InvalidCastException("Длина запрашиваемой удаленной части файла не должна превышать int32.", ex);
                throw exc;
            }
        }
        protected override void Dispose(bool disposing)
        {
            StreamForTempSaving?.Dispose();
            base.Dispose(disposing);
        }
    }
}
