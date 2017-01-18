using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;
using Xunit;

namespace TestLib.MolePushServerTests
{
    public class MolePushServerRecieverTests : IClassFixture<PushServerFixture>
    {
        protected MolePushServerSender Sender1F;
        protected MolePushServerSender Sender2F;
        protected PushServerListner ListnerF;
        protected Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> UserFormTuplFrodo1;
        protected Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> UserFormTuplFrodo2;


        public MolePushServerRecieverTests(PushServerFixture fixture)
        {
            Sender1F = fixture.Sender1;
            Sender2F = fixture.Sender2;
            ListnerF = fixture.Listner;
            UserFormTuplFrodo1 = fixture.UserFormTuplFrodo1;
            UserFormTuplFrodo2 = fixture.UserFormTuplFrodo2;
        }


        [Fact]
        public void InitializeConnectionTest()
        {
            if (Sender1F.IsConnected)
                return;

            Sender1F.InitializeConnectionAsync(ListnerF.EndPoint.Address, ListnerF.EndPoint.Port).Wait();
            Sender2F.InitializeConnectionAsync(ListnerF.EndPoint.Address, ListnerF.EndPoint.Port).Wait();
        }
        [Fact]
        public void RegisterNewUserTest()
        {
            if (Sender1F.IsReg || Sender2F.IsReg)
                return;
            if (!Sender1F.IsConnected || !Sender2F.IsConnected)
                InitializeConnectionTest();

            Sender1F.RegisterNewUserAsync(UserFormTuplFrodo1.Item1).Wait();
            Sender2F.RegisterNewUserAsync(UserFormTuplFrodo2.Item1).Wait();
        }
        [Fact]
        public void FinedUserTest()
        {
            if (!Sender1F.IsReg || !Sender1F.IsConnected)
                RegisterNewUserTest();

            ICollection<UserFormSurrogate> userFinded = Sender1F.FinedUserAsync("Frod").Result;
            var logins = userFinded.Select(form => form.Login).ToArray();
            Assert.Contains("Frodo2", logins);
            Assert.Contains("Frodo2", logins);
        }
        [Fact]
        public void CryptoTest()
        {
            if (!Sender1F.IsReg || !Sender2F.IsReg)
                RegisterNewUserTest();

            if (Sender1F.AssDecrypter == null)
                Sender1F.SetPublicKeyAsync().Wait();
            if (Sender1F.SymDecrypter == null)
                Sender1F.GetSessionKey().Wait();

            //var reciver =
            //    ListnerF.Recievers.FirstOrDefault(reciever => reciever.Form.Login == UserFormTuplFrodo1.Item1.Login);
            var reciver = ListnerF.Recievers[0];
            var mesAsBytes = new byte[3] {3, 3, 3};
            var aes = Sender1F.FactoryChoosen.CreateSymmAlgInst(Sender1F.CryptoInfoChoosen.Provider,
                Sender1F.CryptoInfoChoosen.Symmetric);
            aes.Mode = CipherMode.ECB;
            var enc = aes.CreateEncryptor();
            var dec = aes.CreateDecryptor();
            var encMes = enc.TransformFinalBlock(mesAsBytes, 0, mesAsBytes.Length);
            var decMes = dec.TransformFinalBlock(encMes, 0, encMes.Length);

            //aes.GenerateIV();
            //enc = aes.CreateEncryptor();
            //dec = aes.CreateDecryptor();
            for (int i = 0; i < 1000; i++)
            {
                encMes = enc.TransformFinalBlock(mesAsBytes, 0, mesAsBytes.Length);
                decMes = dec.TransformFinalBlock(encMes, 0, encMes.Length);
                if (!decMes.SequenceEqual(mesAsBytes))
                {
                    
                }
            }
            
            //var aes = Sender1F.FactoryChoosen.CreateSymmetricAlgoritm(Sender1F.CryptoInfoChoosen.Provider,
            //    Sender1F.CryptoInfoChoosen.Symmetric);
            //var encMes = aes.Item1.TransformFinalBlock(mesAsBytes, 0, mesAsBytes.Length);
            //var decMes = aes.Item2.TransformFinalBlock(encMes, 0, encMes.Length);
            //encMes = aes.Item1.TransformFinalBlock(mesAsBytes, 0, mesAsBytes.Length);
            //decMes = aes.Item2.TransformFinalBlock(encMes, 0, encMes.Length);
            //var encMes = Sender1F.SymEncrypter.TransformFinalBlock(mesAsBytes, 0, mesAsBytes.Length);
            //var decMes = Sender1F.SymDecrypter.TransformFinalBlock(encMes, 0, encMes.Length);
            //decMes = reciver.Decryptor.TransformFinalBlock(encMes, 0, encMes.Length);

        }
        [Fact]
        public void AuthTest()
        {
            if (!Sender1F.IsReg || !Sender1F.IsConnected)
                RegisterNewUserTest();

            var oldTupl = UserFormTuplFrodo1;
            var oldSender = Sender1F;
            Sender1F = new MolePushServerSender(ListnerF.CryptoFactories, ListnerF.PossibleCrypto, true);
            Sender1F.InitializeConnectionAsync(ListnerF.EndPoint.Address, ListnerF.EndPoint.Port).Wait();
            var authForm = new AuthenticationFormSign()
            {
                CryptoProvider = Sender2F.CryptoInfoChoosen.Provider,
                Login = UserFormTuplFrodo1.Item1.Login,
                HashAlgotitmName = Sender2F.CryptoInfoChoosen.Hash,
                SignantureAlgoritmName = Sender2F.CryptoInfoChoosen.Sign
            };
            var signAlg = oldTupl.Item3 as ISign;
            Assert.NotNull(signAlg);
            var hashAlg = oldSender.FactoryChoosen.CreateHashAlgorithm(oldSender.CryptoInfoChoosen.Provider,
                oldSender.CryptoInfoChoosen.Hash);
            authForm.Hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(oldTupl.Item1.Password));
            authForm.Sign = signAlg.SignDataAsync(authForm.Hash).Result;
            Sender1F.GetSessionKey().Wait();
            var offlineMess = Sender1F.AuthenticateUserAsync(authForm).Result;
        }

    }
}
