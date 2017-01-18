using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JabyLib.Other;
using MoleClientLib;
using MoleClientLib.RemoteFileStream;
using ProtoBuf.Meta;
using SharedMoleRes.Client;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;
using TestLib.MolePushServerTests;

//using MoleChatTests.ClientToPushServerTests;

namespace TestLib.ClientToClientTests
{
    public class ClientFixture
    {
        protected ClientListner ListnerClientAF;
        protected ClientListner ListnerClientBF;
        protected PushServerListner ServerListnerF;


        public ClientFixture()
        {
            try
            {
                PushServerFixture.PrepareProtoBufSerrializer();
                PrepareProtoBuf();
                ServerListnerF = new PushServerListner();
                var users = ServerListnerF.DbContext.Users.ToArray();
                ServerListnerF.DbContext.RemoveRange(users);
                ServerListnerF.DbContext.SaveChanges();

                while (ServerListnerF.CryptoFactories == null || ServerListnerF.PossibleCrypto == null)
                { }
                IPAddress[] localIPs = Dns.GetHostAddressesAsync(Dns.GetHostName()).Result;
                var ip = localIPs.First(address => address.AddressFamily == AddressFamily.InterNetwork);
                var rand = new Random();
                var clientATupl = PushServerFixture.CreateForm("Frodo1", true, ip, rand);
                clientATupl.Item1.Accessibility = new AccessibilityInfo(100, 5);
                clientATupl.Item1.Accessibility.AddToConst("Frodo2");

                CoreClientAF = new MoleClientCore(new ProtoBufSerializer(), @"C:\MoleFileSavingA",
                    ServerListnerF.CryptoFactories, clientATupl.Item1);
                var clientBTupl = PushServerFixture.CreateForm("Frodo2", true, ip, rand);
                clientBTupl.Item1.Accessibility = new AccessibilityInfo(100, 5);
                clientBTupl.Item1.Accessibility.AddToConst("Frodo1");
                CoreClientBF = new MoleClientCore(new ProtoBufSerializer(), @"C:\MoleFileSavingB",
                    ServerListnerF.CryptoFactories, clientBTupl.Item1);

                PServSenderFromClientA = new MolePushServerSender(ServerListnerF.CryptoFactories, ServerListnerF.PossibleCrypto);
                PServSenderFromClientA.InitializeConnectionAsync(ServerListnerF.EndPoint.Address,
                    ServerListnerF.EndPoint.Port).Wait();
                PServSenderFromClientA.RegisterNewUserAsync(CoreClientAF.MyUserForm).Wait();
                PServSenderFromClientB = new MolePushServerSender(ServerListnerF.CryptoFactories, ServerListnerF.PossibleCrypto);
                PServSenderFromClientB.InitializeConnectionAsync(ServerListnerF.EndPoint.Address,
                    ServerListnerF.EndPoint.Port).Wait();
                PServSenderFromClientB.RegisterNewUserAsync(CoreClientBF.MyUserForm).Wait();

                ListnerClientAF = new ClientListner(clientATupl, CoreClientAF.DirForFileSaving, PServSenderFromClientA,
                    new IPEndPoint(ip, rand.Next(20000, 60000)), ServerListnerF.CryptoFactories, CoreClientAF);
                ListnerClientBF = new ClientListner(clientBTupl, CoreClientBF.DirForFileSaving, PServSenderFromClientB,
                    new IPEndPoint(ip, rand.Next(20000, 60000)), ServerListnerF.CryptoFactories, CoreClientBF);
                SenderToClientA = new MoleClientSender(CoreClientAF.MyUserForm, ServerListnerF.PossibleCrypto, CoreClientBF);
                SenderToClientA.Inicialize(ListnerClientAF.EndPoint).Wait();
                SenderToClientB = new MoleClientSender(CoreClientBF.MyUserForm, ServerListnerF.PossibleCrypto, CoreClientAF);
                SenderToClientB.Inicialize(ListnerClientBF.EndPoint).Wait();
                //SetFuncOnEvents(CoreClientAF);
            }
            catch (Exception)
            {
                
                throw;
            }
            
        }


        public MoleClientCore CoreClientAF { get; protected set; }
        public MoleClientCore CoreClientBF { get; protected set; }
        public MoleClientSender SenderToClientA { get; protected set; }
        public MoleClientSender SenderToClientB { get; protected set; }
        public MolePushServerSender PServSenderFromClientA { get; protected set; }
        public MolePushServerSender PServSenderFromClientB { get; protected set; }


        private void SetFuncOnEvents(MoleClientCore core)
        {
            //core.RegisterNewContactEvent += (o, args) =>
            //{
            //    args.AllowContinueRegistration(true);
            //    return Task.CompletedTask;
            //};
            //core.TextMessageRecievedEvent += (o, args) =>
            //{
            //    Debug.WriteLine($"From: [{args.Contact.Login}], Message: [{args.Message}]");
            //    return Task.CompletedTask;
            //};
            //core.RecieveOfFileTransferRequestEvent += (o, args) =>
            //{
            //    args.AllowContinueRegistration(true);
            //    return Task.CompletedTask;
            //};
            //core.FileRecievingPreparedEvent += (o, args) => args.
        }
        private void PrepareProtoBuf()
        {
            var model = RuntimeTypeModel.Default;
            //var typesInModel = model.GetTypes().Cast<MetaType>().Select((type => type.Type));

            var metaRsaParams = model.Add(typeof(ClientToClientAuthForm), true);
            metaRsaParams.Add("Login", "PrivateKey");

            var metaFileRecieveRequest = model.Add(typeof(FileRecieveRequest), true);
            metaFileRecieveRequest.Add("Length", "Name");

            var metaRequestPartOfFile = model.Add(typeof(RequestPartOfFile), true);
            metaRequestPartOfFile.Add("NameOfFile", "Position", "Length");

            //var metaResponsePartOfFile = model.Add(typeof(ResponsePartOfFile), true);
            //metaResponsePartOfFile.Add("NameOfFile", "PartOfFile");

            //var metaPublicKeyForm = model.Add(typeof(PublicKeyForm), true);
            //metaPublicKeyForm.Add("NameOfCryptoProvider", "Key");

            //var resultOperMetaType =
            //    model.GetTypes().Cast<MetaType>().First(type => type.Type.Equals(typeof(ResultOfOperation)));
            //resultOperMetaType.AddSubType(800, typeof(CurrentResult<PublicKeyForm>));
            //var metaCurrentResult = model.Add(typeof(CurrentResult<PublicKeyForm>), true);
            //metaCurrentResult.Add("Result");
            //CurrentResult<ResponsePartOfFile>
            //resultOperMetaType.AddSubType(900, typeof(CurrentResult<ResponsePartOfFile>));
            //metaCurrentResult = model.Add(typeof(CurrentResult<ResponsePartOfFile>), true);
            //metaCurrentResult.Add("Result");
            //CurrentResult<byte[]>
            //resultOperMetaType.AddSubType(1000, typeof(CurrentResult<byte[]>));
            //metaCurrentResult = model.Add(typeof(CurrentResult<byte[]>), true);
            //metaCurrentResult.Add("Result");

            var metaContactForm = model.Add(typeof(ContactForm), false);
            metaContactForm.SetSurrogate(typeof(UserFormSurrogate));
        }
    }
}
