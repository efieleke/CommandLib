using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class HttpGetCommandTests
    {
        private const String TEST_SERVER = "http://httpbin.org";

        private int CompareResults(object expected, object actual)
        {
            System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual;
            return CommandLib.HttpGetCommand.ContentAsString(response.Content).Contains(expected.ToString()) ? 0 : 1;
        }

        private int CompareFileResults(object expected, object actual)
        {
            System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual;
            String outputFileName = System.IO.Path.GetTempFileName();
            System.IO.File.Delete(outputFileName);
            CommandLib.HttpGetCommand.WriteContentToFile(response.Content, outputFileName);
            System.IO.File.Delete(outputFileName);
            return 0;
        }

        [TestMethod]
        public void DownloadStringCommand_TestHappyPath()
        {
            using (CommandLib.HttpGetCommand cmd = new CommandLib.HttpGetCommand())
            {
                HappyPathTest.Run(cmd, new Uri(TEST_SERVER + "/get"), "http://httpbin.org/get", new Comparison<object>(CompareResults));
            }

            using (CommandLib.HttpGetCommand cmd = new CommandLib.HttpGetCommand(new Uri(TEST_SERVER + "/get")))
            {
                HappyPathTest.Run(cmd, null, "http://httpbin.org/get", new Comparison<object>(CompareResults));
            }

            using (CommandLib.HttpGetCommand cmd = new CommandLib.HttpGetCommand(new Uri(TEST_SERVER + "/get")))
            {
                HappyPathTest.Run(cmd, null, "http://httpbin.org/get", new Comparison<object>(CompareFileResults));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestFail()
        {
            using (CommandLib.HttpGetCommand cmd = new CommandLib.HttpGetCommand())
            {
                FailTest.Run<System.Net.Http.HttpRequestException>(cmd, new Uri("http://www.adslgkggkuytjgdlkhfbjf.com"));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestAbort()
        {
            using (CommandLib.HttpGetCommand cmd = new CommandLib.HttpGetCommand())
            {
                AbortTest.Run(cmd, new Uri(TEST_SERVER + "/delay/5"), 100);
            }
        }
    }
}
