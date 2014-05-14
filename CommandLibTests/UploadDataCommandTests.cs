using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class UploadDataCommandTests
    {
        private const String TEST_SERVER = "http://<YOUR TEST WEB SERVER URL>";

        private int CompareResults(object expected, object actual)
        {
            byte[] inBytes = (byte[])expected;
            byte[] outBytes = (byte[])actual;
            String actualText = System.Text.Encoding.UTF8.GetString(outBytes);
            String searchFor = String.Format("\"Content-Length\": \"{0}\"", inBytes.Length);
            return actualText.Contains(searchFor) ? 0 : 1;
        }

        //[TestMethod]
        public void UploadDataCommand_TestHappyPath()
        {
            byte[] data = new byte[] { 0xa, 0xb };

            CommandLib.UploadDataCommand.UploadArgs args = new CommandLib.UploadDataCommand.UploadArgs(
                new Uri(TEST_SERVER + "/<YOUR PUT>"), "PUT", data);

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand())
            {
                HappyPathTest.Run(cmd, args, data, CompareResults);
            }

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand(args))
            {
                HappyPathTest.Run(cmd, null, data, CompareResults);
            }

            args = new CommandLib.UploadDataCommand.UploadArgs(new Uri(TEST_SERVER + "/<YOUR POST>"), "POST", data);

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand())
            {
                HappyPathTest.Run(cmd, args, data, CompareResults);
            }

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand(args))
            {
                HappyPathTest.Run(cmd, null, data, CompareResults);
            }
        }

        [TestMethod]
        public void UploadDataCommand_TestFail()
        {
            CommandLib.UploadDataCommand.UploadArgs args = new CommandLib.UploadDataCommand.UploadArgs(
                new Uri("http://www.adslgkggkuytjgdlkhfbjf.com"), "PUT", new byte[] { 0xa, 0xb });

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand())
            {
                FailTest.Run<System.Net.WebException>(cmd, args);
            }
        }

        //[TestMethod]
        public void UploadDataCommand_TestAbort()
        {
            byte[] bytes = new byte[1024 * 1024];

            CommandLib.UploadDataCommand.UploadArgs args = new CommandLib.UploadDataCommand.UploadArgs(
                new Uri(TEST_SERVER + "/<YOUR PUT>"), "PUT", bytes);

            using (CommandLib.UploadDataCommand cmd = new CommandLib.UploadDataCommand())
            {
                AbortTest.Run(cmd, args, 5);
            }
        }
    }
}
