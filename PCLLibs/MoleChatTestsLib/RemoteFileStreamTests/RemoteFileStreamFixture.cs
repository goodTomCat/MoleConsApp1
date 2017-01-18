using System;
using System.Collections.Generic;
using System.IO;
using MoleClientLib.RemoteFileStream;

namespace MoleChatTestsLib.RemoteFileStreamTests
{
    public class RemoteFileStreamFixture : IDisposable
    {
        public const string FilePathTrue = @"C:\Users\jaby\Desktop\Новая папка\corefx-master.zip";
        public const string PathForMiddleFile = @"C:\Users\jaby\Desktop\Новая папка\MoleTest\TempFile\corefx-master.zip";


        public RemoteFileStreamFixture()
        {
            //var filePathTrue = @"C:\Users\jaby\Desktop\Новая папка\sss.txt";
            //var pathForMiddleFile = @"C:\Users\jaby\Desktop\Новая папка\MoleTest\TempFile\sss.txt";
            //var infoOfTrueFile = new FileInfo(FilePathTrue);
            //var streamForSavingFile = File.Create(PathForMiddleFile);
            //streamForSavingFile.SetLength(infoOfTrueFile.Length);
            //RemoteStream = new RemoteFileStreamForTests(streamForSavingFile, infoOfTrueFile.Name,
            //    infoOfTrueFile.OpenRead());
            //RemoteStream = new RemoteFileStreamForTests(infoOfTrueFile.Name, infoOfTrueFile.Length,
            //    infoOfTrueFile.OpenRead());
            //FileTrueInfo = infoOfTrueFile;
        }


        public List<string> TestsFinished { get; } = new List<string>(7);
        public FileInfo FileTrueInfo { get; protected set; }
        public RemoteFileStreamBase RemoteStream { get; set; }


        public void Initialize(bool useMiddleFile)
        {
            var info = new FileInfo(FilePathTrue);
            var middleFile = File.Create(PathForMiddleFile);
            middleFile.SetLength(info.Length);
            Initialize(info, useMiddleFile ? middleFile : null);
        }

        public void Initialize(string pathOfFileTrue, bool useMiddleFile)
        {
            var info = new FileInfo(pathOfFileTrue);
            if (useMiddleFile)
            {
                var middleFile = File.Create(PathForMiddleFile);
                middleFile.SetLength(info.Length);
                Initialize(info, middleFile);
            }
            else
                Initialize(info, null);
        }

        public void Initialize(string pathOfFileTrue, string pathOfMiddleFile)
        {
            var info = new FileInfo(pathOfFileTrue);
            var middleFile = File.Create(PathForMiddleFile);
            middleFile.SetLength(info.Length);
            Initialize(info, middleFile);
        }
        public void Dispose()
        {
            RemoteStream.Dispose();
            RemoteStream = null;
            FileTrueInfo = null;
        }


        protected virtual void Initialize(FileInfo infoOfTrueFile, Stream streamAsMiddleFile)
        {
            RemoteStream = streamAsMiddleFile == null
                ? new RemoteFileStreamForTests(infoOfTrueFile.Name, infoOfTrueFile.Length,
                    infoOfTrueFile.OpenRead())
                : new RemoteFileStreamForTests(streamAsMiddleFile, infoOfTrueFile.Name, infoOfTrueFile.OpenRead());
            FileTrueInfo = infoOfTrueFile;
        }
    }
}
