using System;
using System.IO;
using System.Threading.Tasks;

namespace MoleClientLib.RemoteFileStream
{
    public class RemoteFileStreamForTests : RemoteFileStreamNaive
    {
        protected Stream StreamForReading;


        public RemoteFileStreamForTests(Stream fileStreamForSaving, string name, Stream streamForReading) : base(fileStreamForSaving, name)
        {
            if (streamForReading == null)
                throw new ArgumentNullException(nameof(streamForReading))
                { Source = GetType().AssemblyQualifiedName };
            if (!streamForReading.CanSeek)
                throw new ArgumentOutOfRangeException(nameof(streamForReading.CanSeek))
                { Source = GetType().AssemblyQualifiedName };
            if (!streamForReading.CanRead)
                throw new ArgumentOutOfRangeException(nameof(streamForReading.CanRead))
                { Source = GetType().AssemblyQualifiedName };

            StreamForReading = streamForReading;
        }
        public RemoteFileStreamForTests(string name, long length, Stream streamForReading) : base(name, length)
        {
            if (streamForReading == null)
                throw new ArgumentNullException(nameof(streamForReading))
                { Source = GetType().AssemblyQualifiedName };
            if (!streamForReading.CanSeek)
                throw new ArgumentOutOfRangeException(nameof(streamForReading.CanSeek))
                { Source = GetType().AssemblyQualifiedName };
            if (!streamForReading.CanRead)
                throw new ArgumentOutOfRangeException(nameof(streamForReading.CanRead))
                { Source = GetType().AssemblyQualifiedName };

            StreamForReading = streamForReading;
        }


        //public override Task InitializeAsync()
        //{

        //    LengthOfPart = 3;
        //    Parts = new bool[4];
        //    return Task.CompletedTask;
        //}
        public override void Flush()
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        protected override async Task<byte[]> GetPartAsync(long pos, int length)
        {
            StreamForReading.Seek(pos, SeekOrigin.Begin);
            var bytes = new byte[0];
            if (length <= Length - Position)
            {
                bytes = new byte[length];
                await StreamForReading.ReadAsync(bytes, 0, length).ConfigureAwait(false);
            }
            else
            {
                bytes = new byte[Length - Position];
                await StreamForReading.ReadAsync(bytes, 0, (int)(Length - Position)).ConfigureAwait(false);
            }
            return bytes;
        }
    }
}
