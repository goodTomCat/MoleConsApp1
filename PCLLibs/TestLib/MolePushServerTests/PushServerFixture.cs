using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using MolePushServerLibPcl;
using ProtoBuf.Meta;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Client.Surrogates;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;

namespace TestLib.MolePushServerTests
{
    public class PushServerFixture
    {
        public PushServerFixture()
        {
            Listner = new PushServerListner();
            while (Listner.CryptoFactories == null || Listner.PossibleCrypto == null)
            {
                
            }
            Sender1 = new MolePushServerSender(Listner.CryptoFactories, Listner.PossibleCrypto);
            Sender2 = new MolePushServerSender(Listner.CryptoFactories, Listner.PossibleCrypto);
            var random = new Random();
            UserFormTuplFrodo1 = CreateForm(random: random, login: "Frodo1", ip: Listner.EndPoint.Address,
                listenPorts: true);
            UserFormTuplFrodo2 = CreateForm(random: random, login: "Frodo2", ip: Listner.EndPoint.Address,
                listenPorts: true);
            using (UserFormContext bdContext = new UserFormContext())
            {
                var users = bdContext.Users.ToArray();
                bdContext.RemoveRange(users);
                bdContext.SaveChanges();
            }

            PrepareProtoBufSerrializer();
        }


        public MolePushServerSender Sender1 { get; set; }
        public MolePushServerSender Sender2 { get; set; }
        public PushServerListner Listner { get; set; }
        public Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> UserFormTuplFrodo1 { get; set; }
        public Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> UserFormTuplFrodo2 { get; set; }


        public static Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> CreateForm(string login, bool listenPorts,
            IPAddress ip, Random random)
        {
            var rand = random ?? new Random();
            var ecdsa = new MoleECDsaCng();
            var publicKey = ecdsa.Export(false);
            var form = new UserForm()
            {
                Accessibility = new AccessibilityInfo(100, 5) { IsPublicProfile = true },
                Login = login,
                Password = "123456",
                PortClientToClient1 = (ushort)rand.Next(20000, 60000),
                PortClientToClient2 = (ushort)rand.Next(20000, 60000),
                PortClientToClient3 = (ushort)rand.Next(20000, 60000),
                PortServerToClient = (ushort)rand.Next(20000, 60000),
                KeyParametrsBlob = publicKey
            };
            //192.168.65.129
            var listners = new List<TcpListener>(4);
            if (listenPorts)
            {
                var listner = new TcpListener(ip, form.PortClientToClient1);
                listner.Start();
                listners.Add(listner);

                listner = new TcpListener(ip, form.PortClientToClient2);
                listner.Start();
                listners.Add(listner);

                listner = new TcpListener(ip, form.PortClientToClient3);
                listner.Start();
                listners.Add(listner);

                listner = new TcpListener(ip, form.PortServerToClient);
                listner.Start();
                listners.Add(listner);
            }
            return new Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange>(form, listners, ecdsa);
        }
        public static void PrepareProtoBufSerrializer()
        {
            var model = RuntimeTypeModel.Default;
            var typesInModel = model.GetTypes().Cast<MetaType>().Select((type => type.Type));

            if (!typesInModel.Contains(typeof(RSAParameters)))
            {
                var metaRsaParams = model.Add(typeof(RSAParameters), true);
                metaRsaParams.Add(new[] { "D", "DP", "DQ", "Exponent", "InverseQ", "Modulus", "P", "Q" });
            }

            if (!typesInModel.Contains(typeof(PossibleCryptoInfo)))
            {
                var metaSur = model.Add(typeof(PossibleCryptoInfoSur), true);
                metaSur.AddField(1, "Providers").OverwriteList = true;
                metaSur.AddField(2, "Hash").OverwriteList = true;
                metaSur.AddField(3, "Asymmetric").OverwriteList = true;
                metaSur.AddField(4, "Symmetric").OverwriteList = true;
                metaSur.AddField(5, "Sign").OverwriteList = true;

                var metaPossibleCryptoInfo = model.Add(typeof(PossibleCryptoInfo), false);
                metaPossibleCryptoInfo.SetSurrogate(typeof(PossibleCryptoInfoSur));
            }

            if (!typesInModel.Contains(typeof(CryptoInfo)))
            {
                var metaCryptoInfo = model.Add(typeof(CryptoInfo), true);
                metaCryptoInfo.Add("Provider", "Hash", "Asymmetric", "Symmetric", "Sign");
            }

            if (!typesInModel.Contains(typeof(IPAddress)))
            {
                var metaIpAddress = model.Add(typeof(IPAddress), false);
                metaIpAddress.SetSurrogate(typeof(IpAddressSurrogate));
            }

            if (!typesInModel.Contains(typeof(UserForm)))
            {
                var metaSur = model.Add(typeof(UserFormSurrogate), true);
                metaSur.Add("Login", "Password", "KeyParametrsBlob", "PortClientToClient1", "PortClientToClient2",
                    "PortClientToClient3", "PortServerToClient", "Ip", "AuthenticationForm", "Accessibility");

                var metaRegistrForm = model.Add(typeof(UserForm), false);
                metaRegistrForm.SetSurrogate(typeof(UserFormSurrogate));
            }

            if (!typesInModel.Contains(typeof(AccessibilityInfo)))
            {
                var metaSur = model.Add(typeof(AccessibilityInfoSur), true);
                metaSur.Add("IsPublicProfile", "ConstUsers", "TempUsers", "MaxConstUsers", "MaxTempUsers");

                var metaAccessibilityInfo = model.Add(typeof(AccessibilityInfo), false);
                metaAccessibilityInfo.SetSurrogate(typeof(AccessibilityInfoSur));
            }

            var metaOfflineMessagesConcurentSur = model.Add(typeof(OfflineMessagesConcurentSur), true);
            metaOfflineMessagesConcurentSur.Add("ReceiverLogin", "Messages");

            //if (!typesInModel.Contains(typeof(OfflineMessagesConcurent)))
            //{
            //    var metaOfflineMessagesConcurentSur = model.Add(typeof(OfflineMessagesConcurentSur), true);
            //    metaOfflineMessagesConcurentSur.Add("ReceiverLogin", "Messages");

            //    var metaOfOfflineMessages = model.Add(typeof(OfflineMessagesConcurent), false);
            //    metaOfOfflineMessages.SetSurrogate(typeof(OfflineMessagesConcurentSur));
            //}

            if (!typesInModel.Contains(typeof(ResultOfOperation)))
            {
                var metaResultOfOperation = model.Add(typeof(ResultOfOperation), true);
                metaResultOfOperation.AddSubType(100, typeof(CurrentResult<UserForm>));
                metaResultOfOperation.AddSubType(200, typeof(CurrentResult<ICollection<byte[]>>));
                metaResultOfOperation.AddSubType(300, typeof(CurrentResult<ICollection<UserForm>>));
                metaResultOfOperation.AddSubType(400, typeof(CurrentResult<OfflineMessagesConcurentSur>));
                metaResultOfOperation.AddSubType(500, typeof(CurrentResult<byte[]>));
                metaResultOfOperation.AddSubType(600, typeof(CurrentResult<KeyDataForSymmetricAlgorithm>));
                metaResultOfOperation.AddSubType(700, typeof(CurrentResult<ICollection<UserFormSurrogate>>));
                metaResultOfOperation.AddSubType(800, typeof(CurrentResult<PossibleCryptoInfo>));
                metaResultOfOperation.Add("ErrorCode", "ErrorMessage", "OperationWasFinishedSuccessful");

                var metaCurrentResult = model.Add(typeof(CurrentResult<UserForm>), true);
                metaCurrentResult.Add("Result");

                var metaCurrentResult2 = model.Add(typeof(CurrentResult<ICollection<byte[]>>), true);
                metaCurrentResult2.Add("Result");

                var metaCurrentResult3 = model.Add(typeof(CurrentResult<ICollection<UserForm>>), true);
                metaCurrentResult3.Add("Result");

                var metaCurrentResult4 = model.Add(typeof(CurrentResult<OfflineMessagesConcurentSur>), true);
                metaCurrentResult4.Add("Result");

                var metaCurrentResult5 = model.Add(typeof(CurrentResult<byte[]>), true);
                metaCurrentResult5.Add("Result");

                var metaCurrentResult6 = model.Add(typeof(CurrentResult<KeyDataForSymmetricAlgorithm>), true);
                metaCurrentResult6.Add("Result");

                var metaCurrentResult7 = model.Add(typeof(CurrentResult<ICollection<UserFormSurrogate>>), true);
                metaCurrentResult7.Add("Result");

                var metaCurrentResult8 = model.Add(typeof(CurrentResult<PossibleCryptoInfo>), true);
                metaCurrentResult8.Add("Result");
            }

            if (!typesInModel.Contains(typeof(KeyDataForSymmetricAlgorithm)))
            {
                var metaKeyDataForSymmetricAlgorithm = model.Add(typeof(KeyDataForSymmetricAlgorithm), false);
                metaKeyDataForSymmetricAlgorithm.SetSurrogate(typeof(KeyDataForSymmetricAlgorithmSurrogate));
            }

            if (!typesInModel.Contains(typeof(IAuthenticationForm)))
            {
                var metaOfIAuthenticationForm = model.Add(typeof(IAuthenticationForm), false);
                metaOfIAuthenticationForm.AddSubType(10, typeof(IAuthenticationFormClassic));
                metaOfIAuthenticationForm.AddSubType(20, typeof(IAuthenticationFormSign));
                metaOfIAuthenticationForm.SetSurrogate(typeof(AuthenticationFormSur));

                var metaOfIAuthenticationFormClassic = model.Add(typeof(IAuthenticationFormClassic), false);
                metaOfIAuthenticationFormClassic.AddSubType(30, typeof(AuthenticationFormClassic));
                metaOfIAuthenticationFormClassic.SetSurrogate(typeof(AuthenticationFormClassicSur));

                var metaOfIAuthenticationFormSign = model.Add(typeof(IAuthenticationFormSign), false);
                metaOfIAuthenticationFormSign.AddSubType(40, typeof(AuthenticationFormSign));
                metaOfIAuthenticationFormSign.SetSurrogate(typeof(AuthenticationFormSignSur));

                var metaOfAuthenticationFormSignCl = model.Add(typeof(AuthenticationFormSign), false);
                metaOfAuthenticationFormSignCl.SetSurrogate(typeof(AuthenticationFormSignSur));

                var metaOfAuthenticationFormClassicCl = model.Add(typeof(AuthenticationFormClassic), false);
                metaOfAuthenticationFormClassicCl.SetSurrogate(typeof(AuthenticationFormClassicSur));
            }

        }
        //public static void PrepareProtoBufSerrializer2()
        //{
        //    var model = RuntimeTypeModel.Default;
        //    var typesInModel = model.GetTypes().Cast<MetaType>().Select((type => type.Type));

        //    if (!typesInModel.Contains(typeof(RSAParameters)))
        //    {
        //        var metaRsaParams = model.Add(typeof(RSAParameters), true);
        //        metaRsaParams.Add(new[] { "D", "DP", "DQ", "Exponent", "InverseQ", "Modulus", "P", "Q" });
        //    }

        //    if (!typesInModel.Contains(typeof(ECParameters)))
        //    {
        //        var metaRsaParams = model.Add(typeof(RSAParameters), true);
        //        metaRsaParams.Add(new[] { "D", "DP", "DQ", "Exponent", "InverseQ", "Modulus", "P", "Q" });
        //    }

        //    if (!typesInModel.Contains(typeof(IPAddress)))
        //    {
        //        var metaIpAddress = model.Add(typeof(IPAddress), false);
        //        metaIpAddress.SetSurrogate(typeof(IpAddressSurrogate));
        //    }

        //    if (!typesInModel.Contains(typeof(UserForm)))
        //    {
        //        var metaRegistrForm = model.Add(typeof(UserForm), false);
        //        metaRegistrForm.SetSurrogate(typeof(UserFormSurrogate));
        //    }

        //    if (!typesInModel.Contains(typeof(ResultOfOperation)))
        //    {
        //        var metaResultOfOperation = model.Add(typeof(ResultOfOperation), true);
        //        metaResultOfOperation.AddSubType(100, typeof(CurrentResult<UserForm>));
        //        metaResultOfOperation.AddSubType(200, typeof(CurrentResult<ICollection<byte[]>>));
        //        metaResultOfOperation.AddSubType(300, typeof(CurrentResult<ICollection<UserForm>>));
        //        metaResultOfOperation.AddSubType(400, typeof(CurrentResult<List<OfflineMessages>>));
        //        metaResultOfOperation.Add("ErrorCode", "ErrorMessage", "OperationWasFinishedSuccessful");

        //        var metaCurrentResult = model.Add(typeof(CurrentResult<UserForm>), true);
        //        metaCurrentResult.Add("Result");

        //        var metaCurrentResult2 = model.Add(typeof(CurrentResult<ICollection<byte[]>>), true);
        //        metaCurrentResult2.Add("Result");

        //        var metaCurrentResult3 = model.Add(typeof(CurrentResult<ICollection<UserForm>>), true);
        //        metaCurrentResult3.Add("Result");

        //        var metaCurrentResult4 = model.Add(typeof(CurrentResult<List<OfflineMessages>>), true);
        //        metaCurrentResult4.Add("Result");
        //    }

        //    if (!typesInModel.Contains(typeof(KeyDataForSymmetricAlgorithm)))
        //    {
        //        var metaKeyDataForSymmetricAlgorithm = model.Add(typeof(KeyDataForSymmetricAlgorithm), false);
        //        metaKeyDataForSymmetricAlgorithm.SetSurrogate(typeof(KeyDataForSymmetricAlgorithmSurrogate));
        //    }

        //    if (!typesInModel.Contains(typeof(OfflineMessageForm)))
        //    {
        //        var metaOfflineMessage = model.Add(typeof(OfflineMessageForm), true);
        //        metaOfflineMessage.Add("Message", "LoginOfReciever");
        //    }

        //    if (!typesInModel.Contains(typeof(IAuthenticationForm)))
        //    {
        //        var metaOfIAuthenticationForm = model.Add(typeof(IAuthenticationForm), false);
        //        metaOfIAuthenticationForm.AddSubType(10, typeof(IAuthenticationFormClassic));
        //        metaOfIAuthenticationForm.AddSubType(20, typeof(IAuthenticationFormSign));
        //        metaOfIAuthenticationForm.SetSurrogate(typeof(AuthenticationFormSur));

        //        var metaOfIAuthenticationFormClassic = model.Add(typeof(IAuthenticationFormClassic), false);
        //        metaOfIAuthenticationFormClassic.AddSubType(30, typeof(AuthenticationFormClassic));
        //        metaOfIAuthenticationFormClassic.SetSurrogate(typeof(AuthenticationFormClassicSur));

        //        var metaOfIAuthenticationFormSign = model.Add(typeof(IAuthenticationFormSign), false);
        //        metaOfIAuthenticationFormSign.AddSubType(40, typeof(AuthenticationFormSign));
        //        metaOfIAuthenticationFormSign.SetSurrogate(typeof(AuthenticationFormSignSur));

        //        var metaOfAuthenticationFormSignCl = model.Add(typeof(AuthenticationFormSign), false);
        //        metaOfAuthenticationFormSignCl.SetSurrogate(typeof(AuthenticationFormSignSur));

        //        var metaOfAuthenticationFormClassicCl = model.Add(typeof(AuthenticationFormClassic), false);
        //        metaOfAuthenticationFormClassicCl.SetSurrogate(typeof(AuthenticationFormClassicSur));
        //    }

        //    if (!typesInModel.Contains(typeof(OfflineMessages)))
        //    {
        //        var metaOfOfflineMessages = model.Add(typeof(OfflineMessages), false);
        //        metaOfOfflineMessages.SetSurrogate(typeof(OfflineMessagesSur));
        //    }

        //}
    }
}
