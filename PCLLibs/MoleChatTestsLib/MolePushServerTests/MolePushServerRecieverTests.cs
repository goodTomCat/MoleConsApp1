using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;
using Xunit;

namespace MoleChatTestsLib.MolePushServerTests
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
