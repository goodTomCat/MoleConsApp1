using System;
using System.IO;
using System.Threading.Tasks;

namespace MoleClientLib.RemoteFileStream
{
    public class RemoteFileStreamMole : RemoteFileStreamNaive
    {
        private MoleClientSender Sender;


        /// <exception cref="ArgumentNullException">sender == null.</exception>
        public RemoteFileStreamMole(MoleClientSender sender, Stream streamForTempSaving, string name) : base(streamForTempSaving, name)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender))
            { Source = GetType().AssemblyQualifiedName};

            Sender = sender;
        }
        /// <exception cref="ArgumentNullException">sender == null.</exception>
        public RemoteFileStreamMole(MoleClientSender sender, string name, long length) : base(name, length)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender))
            { Source = GetType().AssemblyQualifiedName };

            Sender = sender;
        }


        protected override Task<byte[]> GetPartAsync(long pos, int length)
        {
            return length <= Length - Position
                ? Sender.GetPartOfFile(pos, length, Name)
                : Sender.GetPartOfFile(pos, (int) (Length - Position), Name);
        }
    }
}
