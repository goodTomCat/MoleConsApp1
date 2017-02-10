using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedMoleRes.Client;
using ProtoBuf;
using ProtoBuf.Meta;
using JabyLib.Other;
using SharedMoleRes.Server;


namespace MoleConsApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var model = RuntimeTypeModel.Default;
            var metaPublicKeyForm = model.Add(typeof(PublicKeyForm), true);
            metaPublicKeyForm.Add("Key", "CryptoProvider", "CryptoAlg", "HashAlg", "Hash", "Sign");

            var publicKeyForm = new PublicKeyForm()
            {
                CryptoAlg = "ss1",
                Key = new byte[] {3, 3, 3},
                Hash = new byte[] {3, 3, 3},
                HashAlg = "ss1",
                CryptoProvider = "ss1",
                Sign = new byte[] {3, 3, 3}
            };
            //var ser = new ProtoBufSerializer();
            //var bytes = ser.Serialize(publicKeyForm, false);
            var stream = new MemoryStream();
            Serializer.Serialize(stream, publicKeyForm);
            stream.Seek(0, SeekOrigin.Begin);
            var form2 = Serializer.Deserialize<PublicKeyForm>(stream);
            //var form2 = ser.Deserialize<PublicKeyForm>(bytes, false);

            var metaCurrentResult9 = model.Add(typeof(CurrentResult<PublicKeyForm>), true);
            metaCurrentResult9.Add("Result");
            var result = new CurrentResult<PublicKeyForm>()
            {
                OperationWasFinishedSuccessful = true,
                Result = publicKeyForm
            };

        }
    }
}
