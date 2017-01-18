using MoleChatApp1.PCLLibs.SharedMoleRes.Server;
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    public class KeyDataForSymmetricAlgorithmSurrogate
    {
        public KeyDataForSymmetricAlgorithmSurrogate() { }
        public KeyDataForSymmetricAlgorithmSurrogate(KeyDataForSymmetricAlgorithm data)
        {
            if (data == null)
                return;

            SymmetricIvBlob = data.SymmetricIvBlob;
            SymmetricKeyBlob = data.SymmetricKeyBlob;
        }


        [ProtoMember(2, OverwriteList = true)]
        public byte[] SymmetricKeyBlob { get; set; }
        [ProtoMember(3, OverwriteList = true)]
        public byte[] SymmetricIvBlob { get; set; }


        public static implicit operator KeyDataForSymmetricAlgorithm(KeyDataForSymmetricAlgorithmSurrogate sur)
        {
            if (sur.SymmetricIvBlob == null)
                return null;
            if (sur.SymmetricKeyBlob == null)
                return null;

            return new KeyDataForSymmetricAlgorithm(sur.SymmetricKeyBlob, sur.SymmetricIvBlob);
        }
        public static implicit operator KeyDataForSymmetricAlgorithmSurrogate(KeyDataForSymmetricAlgorithm data)
        {
            if (data == null)
                return null;

            return new KeyDataForSymmetricAlgorithmSurrogate(data);
        }
    }
}
