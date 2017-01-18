using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MoleChatTestsLib.RemoteFileStreamTests
{
    public class RemoteFileStreamTests : IClassFixture<RemoteFileStreamFixture>
    {
        protected RemoteFileStreamFixture Fixture;
        protected List<string> TestsFinished;


        public RemoteFileStreamTests(RemoteFileStreamFixture fixture)
        {
            Fixture = fixture;
            TestsFinished = Fixture.TestsFinished;
        }


        [Fact]
        public async void InitializeAsyncTest()
        {
            await InitializeAsyncTestAsync();
        }

        [Fact]
        public async void CopyFileTest()
        {
            await CopyFileTestAsync();
        }

        [Fact]
        public async void CopyFileByPartTest()
        {
            await CopyFileByPartTestAsync();
        }

        [Fact]
        public async void FlushTest()
        {
            await FlushTestAsync();
        }


        protected async Task FlushTestAsync()
        {
            if (TestsFinished.Contains("FlushTest()"))
                return;
            if (!TestsFinished.Contains("InitializeAsyncTest()"))
                await InitializeAsyncTestAsync();

            long readedAll;
            var bytesFromRemoteStream = new byte[8000];
            var bytesOfTrueFileRead = new byte[8000];
            Fixture.Initialize(true);
            using (Fixture.RemoteStream)
            {
                await Fixture.RemoteStream.InitializeAsync();

                await Fixture.RemoteStream.FlushAsync().ConfigureAwait(false);
                var streamOfTrueFileRead = Fixture.FileTrueInfo.OpenRead();
                var readed = 0;
                readed = await streamOfTrueFileRead.ReadAsync(bytesOfTrueFileRead, 0, bytesOfTrueFileRead.Length);
                readedAll = readed;
                while (readed != 0)
                {
                    bytesFromRemoteStream = new byte[8000];
                    await Fixture.RemoteStream.ReadAsync(bytesFromRemoteStream, 0, bytesFromRemoteStream.Length);
                    Assert.True(bytesOfTrueFileRead.SequenceEqual(bytesFromRemoteStream));
                    bytesOfTrueFileRead = new byte[8000];
                    readed = await streamOfTrueFileRead.ReadAsync(bytesOfTrueFileRead, 0, bytesOfTrueFileRead.Length);
                    readedAll += readed;
                }
            }
            TestsFinished.Add("FlushTest()");

        }
        protected async Task CopyFileByPartTestAsync()
        {
            if (TestsFinished.Contains("CopyFileByPartTest()"))
                return;
            if (!TestsFinished.Contains("InitializeAsyncTest()"))
                await InitializeAsyncTestAsync();

            Fixture.Initialize(true);
            using (Fixture.RemoteStream)
            {
                await Fixture.RemoteStream.InitializeAsync();
                Fixture.RemoteStream.Seek(0, SeekOrigin.Begin);
                using (var fileStream =
                    File.Create($@"C:\Users\jaby\Desktop\Новая папка\MoleTest\FileSaving\{Fixture.RemoteStream.Name}"))
                {
                    var buf = new byte[2];
                    var tasksWrite = new Task[6];
                    for (int i = 0; i < 6; i++)
                    {
                        var iTemp = i;
                        await Fixture.RemoteStream.ReadAsync(buf, 0, buf.Length);
                        fileStream.Seek(iTemp * 2, SeekOrigin.Begin);
                        tasksWrite[iTemp] = fileStream.WriteAsync(buf, 0, buf.Length);
                    }
                    await Task.WhenAll(tasksWrite);
                }
                TestsFinished.Add("CopyFileByPartTest()");
            }
            
        }
        protected async Task CopyFileTestAsync()
        {
            if (TestsFinished.Contains("CopyFileTest()"))
                return;
            if (!TestsFinished.Contains("InitializeAsyncTest()"))
                await InitializeAsyncTestAsync();

            Fixture.Initialize(true);
            using (Fixture.RemoteStream)
            {
                await Fixture.RemoteStream.InitializeAsync();
                Fixture.RemoteStream.Seek(0, SeekOrigin.Begin);
                using (var fileStream =
                    File.Create($@"C:\Users\jaby\Desktop\Новая папка\MoleTest\FileSaving\{Fixture.RemoteStream.Name}"))
                {
                    await Fixture.RemoteStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
            TestsFinished.Add("CopyFileTest()");
        }
        protected async Task InitializeAsyncTestAsync()
        {
            if (TestsFinished.Contains("InitializeAsyncTest()"))
                return;

            Fixture.Initialize(true);
            using (Fixture.RemoteStream)
            {
                await Fixture.RemoteStream.InitializeAsync();
            }
            
            TestsFinished.Add("InitializeAsyncTest()");
        }
    }
}
