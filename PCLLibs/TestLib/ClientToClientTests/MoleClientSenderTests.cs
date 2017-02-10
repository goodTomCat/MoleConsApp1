using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MoleClientLib;
using MoleClientLib.RemoteFileStream;
using SharedMoleRes.Client;
using Xunit;

//using MoleChatTests.ClientToPushServerTests;

namespace TestLib.ClientToClientTests
{
    public class MoleClientSenderTests : IClassFixture<ClientFixture>
    {
        protected MolePushServerSender PServSenderFromClientA;
        protected MolePushServerSender PServSenderFromClientB;
        protected MoleClientSender SenderClientA;
        protected MoleClientSender SenderClientB;
        protected MoleClientCore CoreClientA;
        protected MoleClientCore CoreClientB;
        protected ClientFixture Fixture;


        public MoleClientSenderTests(ClientFixture fixture)
        {
            PServSenderFromClientA = fixture.PServSenderFromClientA;
            PServSenderFromClientB = fixture.PServSenderFromClientB;
            SenderClientA = fixture.SenderToClientA;
            SenderClientB = fixture.SenderToClientB;
            CoreClientA = fixture.CoreClientAF;
            CoreClientB = fixture.CoreClientBF;
            Fixture = fixture;
        }


        [Fact]
        public void GetSessionKeyTest()
        {
            if (!SenderClientA.IsAuth)
                AuthTest();
            if (SenderClientA.SymmetricEncrypter != null)
                return;

            try
            {
                SenderClientA.GetPublicKeyAsync().Wait();
                SenderClientA.SetPublicKey().Wait();
                SenderClientA.GetSessionKey().Wait();
            }
            catch (Exception)
            {
                SenderClientB.GetPublicKeyAsync().Wait();
                //SenderClientA.Inicialize()
                throw;
            }
            
        }
        [Fact]
        public void AuthTest()
        {
            if (SenderClientA.IsAuth)
                return;

            SenderClientA.AuthenticateAsync().Wait();
        }
        [Fact]
        public void RegisterTest()
        {
            if (SenderClientA.IsRegistered)
                return;
            if (!SenderClientA.IsAuth)
                AuthTest();

            Func<object, RegisterNewContactEventArgs, Task> func = (o, args) =>
            {
                args.AllowContinueRegistration(true);
                return Task.CompletedTask;
            };
            CoreClientA.RegisterNewContactEvent += func;
            SenderClientA.RegisterAsync().Wait();
            CoreClientA.RegisterNewContactEvent -= func;
        }
        [Fact]
        public void SendTExtTest()
        {
            if (!SenderClientA.IsRegistered)
                RegisterTest();
            if (SenderClientA.SymmetricEncrypter == null)
                GetSessionKeyTest();

            Func<object, TextMessageRecievedEventArgs, Task> func = (o, args) =>
            {
                Debug.WriteLine($"args.Message: [{args.Message}]. From: [{args.Contact.Login}]");
                return Task.CompletedTask;
            };
            CoreClientA.TextMessageRecievedEvent += func;
            SenderClientA.SendText("Hi!").Wait();
            CoreClientA.TextMessageRecievedEvent -= func;
        }
        [Theory]
        [InlineData(@"C:\Users\jaby\Desktop\Новая папка\sss.txt")]
        public void SendFileTest(string path)
        {
            if (!SenderClientA.IsRegistered)
                RegisterTest();
            if (SenderClientA.SymmetricEncrypter == null)
                GetSessionKeyTest();
            var fileInfo = new FileInfo(path);
            if (CoreClientB.FilesSending.ContainsKey(fileInfo.Name))
                return;

            if (SenderClientB.SymmetricEncrypter == null)
            {
                if (SenderClientB.AsymmetricEncrypter == null)
                {
                    SenderClientB.AuthenticateAsync().Wait();
                    SenderClientB.GetPublicKeyAsync().Wait();
                    SenderClientB.SetPublicKey().Wait();

                    //SenderClientB.GetPublicKeyAsync().Wait();
                    //SenderClientB.SetPublicKey().Wait();
                }

                SenderClientB.GetSessionKey().Wait();
            }
            if (!SenderClientB.IsAuth)
                SenderClientB.AuthenticateAsync().Wait();
            if (!SenderClientB.IsRegistered)
                SenderClientB.RegisterAsync().Wait();
            var remoteFileStream = new RemoteFileStreamMole(SenderClientA, fileInfo.Name, fileInfo.Length);
            remoteFileStream.InitializeAsync().Wait();
            Func<object, RecieveOfFileTransferRequestEventArgs, Task> func = (o, args) =>
            {
                args.AllowContinueRegistration(true, remoteFileStream);
                return Task.CompletedTask;
            };
            CoreClientB.RecieveOfFileTransferRequestEvent += func;
            SenderClientB.SendFileRecieveRequest(fileInfo).Wait();

            using (var fileStream = new FileStream(@"C:\MoleFileSavingB\" + fileInfo.Name, FileMode.Create,
                FileAccess.ReadWrite, FileShare.Read))
            {
                remoteFileStream.CopyToAsync(fileStream).Wait();
            }
        }

        //private PassedTestFixture _passedTestFixture;
        //private ClientFixture _clientFixture;


        //public MoleClientSenderTests(ClientFixture clientFixture, PassedTestFixture passedTestFixture)
        //{
        //    clientFixture.RunClient(new IPEndPoint(IPAddress.Parse("192.168.65.129"), 10346),
        //        new IPEndPoint(IPAddress.Parse("192.168.65.129"), 10347), CancellationToken.None);
        //    while (clientFixture.Sender == null)
        //        Thread.Sleep(1000);
        //    Thread.Sleep(100);

        //    _clientFixture = clientFixture;
        //    _passedTestFixture = passedTestFixture;
        //}


        //[Fact]
        //public async void InicializeTest()
        //{
        //    await InicializeTestAsync();
        //}
        //[Fact]
        //public async void AuthenticateTest()
        //{
        //    await AuthenticateTestAsync();
        //}
        //[Fact]
        //public async void RegisterTest()
        //{
        //    await RegisterTestAsync();
        //}
        //[Fact]
        //public async void SendTextTest()
        //{
        //    await SendTextTestAsync();
        //}
        //[Fact]
        //public async void SendFileRecieveRequestTest()
        //{
        //    await SendFileRecieveRequestTestAsync();
        //}
        //[Fact]
        //public async void CopyFileTest()
        //{
        //    await CopyFileTestAsync();
        //}


        //public async Task CopyFileTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("CopyFileTestAsync()"))
        //        return;
        //    if (!_passedTestFixture.PassedTest.Contains("SendFileRecieveRequestTest()"))
        //        await SendFileRecieveRequestTestAsync();

        //    using (
        //        var stream = new FileStream(@"C:\Users\jaby\Desktop\" + _clientFixture.FileRecPrepArgs.Name,
        //            FileMode.Create, FileAccess.ReadWrite))
        //    {
        //        await _clientFixture.FileRecPrepArgs.Stream.CopyToAsync(stream);
        //    }
        //    _passedTestFixture.PassedTest.Add("CopyFileTestAsync()");
        //}
        //public async Task SendFileRecieveRequestTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("SendFileRecieveRequestTest()"))
        //        return;
        //    if (!_passedTestFixture.PassedTest.Contains("SendTextTest()"))
        //        await SendTextTestAsync();

        //    await
        //        _clientFixture.Sender.SendFileRecieveRequest(
        //            new FileInfo(@"C:\Users\jaby\Desktop\Новая папка\corefx-master.zip"));
        //    _passedTestFixture.PassedTest.Add("SendFileRecieveRequestTest()");
        //}
        //public async Task SendTextTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("SendTextTest()"))
        //        return;
        //    if (!_passedTestFixture.PassedTest.Contains("RegisterTest()"))
        //        await RegisterTestAsync();

        //    await _clientFixture.Sender.SendText("Hi! 1234");
        //    _passedTestFixture.PassedTest.Add("SendTextTest()");
        //}
        //public async Task RegisterTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("RegisterTest()"))
        //        return;
        //    if (!_passedTestFixture.PassedTest.Contains("AuthenticateTest()"))
        //        await AuthenticateTestAsync();

        //    await _clientFixture.Sender.RegisterAsync();
        //    _passedTestFixture.PassedTest.Add("RegisterTest()");
        //}
        //public async Task AuthenticateTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("AuthenticateTest()"))
        //        return;
        //    if (!_passedTestFixture.PassedTest.Contains("InicializeTest()"))
        //        await InicializeTestAsync();

        //    await _clientFixture.Sender.AuthenticateAsync();
        //    _passedTestFixture.PassedTest.Add("AuthenticateTest()");
        //}
        //private async Task InicializeTestAsync()
        //{
        //    if (_passedTestFixture.PassedTest.Contains("InicializeTest()"))
        //        return;

        //    await _clientFixture.Sender.Inicialize();
        //    _passedTestFixture.PassedTest.Add("InicializeTest()");
        //}
    }
}
