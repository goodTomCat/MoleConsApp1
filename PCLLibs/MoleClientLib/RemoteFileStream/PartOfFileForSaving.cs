using System;
using System.Threading.Tasks;

namespace MoleClientLib.RemoteFileStream
{
    public class PartOfFileForSaving
    {
        protected long _pos;
        protected Tuple<int> LenTup;

        public long Position
        {
            get { return _pos; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Значение меньше нуля.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                _pos = value;
            }
        }
        public Task<bool> IsSaved { get; set; }
        public Tuple<int> LengthTuple
        {
            get { return LenTup; }
            set { LenTup = value; }
        }
    }
}
