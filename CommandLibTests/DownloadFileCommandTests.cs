using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class DownloadFileCommandTests
    {
        [TestMethod]
        public void DownloadFileCommand_TestHappyPath()
        {
            String inputFileName = System.IO.Path.GetTempFileName();
            String outputFileName = System.IO.Path.GetTempFileName();

            try
            {
                System.IO.FileStream inFile = new System.IO.FileStream(inputFileName, System.IO.FileMode.Append);
                inFile.Write(new byte[] { 0xa, 0xb }, 0, 2);
                inFile.Close();
                CommandLib.DownloadFileCommand.DownloadArgs downloadArgs = new CommandLib.DownloadFileCommand.DownloadArgs(new System.Uri(inputFileName), outputFileName);

                using (CommandLib.DownloadFileCommand cmd = new CommandLib.DownloadFileCommand())
                {
                    HappyPathTest.Run(cmd, downloadArgs, null);
                    byte[] inBytes = System.IO.File.ReadAllBytes(inputFileName);
                    byte[] outBytes = System.IO.File.ReadAllBytes(outputFileName);
                    Assert.AreEqual(inBytes.Length, outBytes.Length);
                    Assert.AreEqual(inBytes.Length, 2);
                    Assert.IsTrue(System.Linq.Enumerable.SequenceEqual(inBytes, outBytes));
                    System.IO.File.Delete(outputFileName);
                }

                using (CommandLib.DownloadFileCommand cmd = new CommandLib.DownloadFileCommand(downloadArgs))
                {
                    HappyPathTest.Run(cmd, null, null);
                    byte[] inBytes = System.IO.File.ReadAllBytes(inputFileName);
                    byte[] outBytes = System.IO.File.ReadAllBytes(outputFileName);
                    Assert.AreEqual(inBytes.Length, outBytes.Length);
                    Assert.AreEqual(inBytes.Length, 2);
                    Assert.IsTrue(System.Linq.Enumerable.SequenceEqual(inBytes, outBytes));
                }
            }
            finally
            {
                System.IO.File.Delete(inputFileName);
                System.IO.File.Delete(outputFileName);
            }
        }

        [TestMethod]
        public void DownloadFileCommand_TestFail()
        {
            String inputFileName = System.IO.Path.GetTempFileName();
            System.IO.File.Delete(inputFileName);
            String outputFileName = System.IO.Path.GetTempFileName();
            System.IO.File.Delete(outputFileName);

            using (CommandLib.DownloadFileCommand cmd = new CommandLib.DownloadFileCommand())
            {
                FailTest.Run<System.Net.WebException>(
                    cmd, new CommandLib.DownloadFileCommand.DownloadArgs(new System.Uri(inputFileName), outputFileName));
            }
        }

        //[TestMethod]
        public void DownloadFileCommand_TestAbort()
        {
            String outputFileName = System.IO.Path.GetTempFileName();

            try
            {
                using (CommandLib.DownloadFileCommand cmd = new CommandLib.DownloadFileCommand())
                {
                    AbortTest.Run(cmd, new CommandLib.DownloadFileCommand.DownloadArgs(new Uri("<YOUR TEST SERVER DOWNLOAD FILE URL>"), outputFileName), 0);
                }
            }
            finally
            {
                System.IO.File.Delete(outputFileName);
            }
        }
    }
}
