using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class DownloadStringCommandTests
    {
        private const String TEST_SERVER = "http://<YOUR TEST WEB SERVER URL>";

        private int CompareResults(object expected, object actual)
        {
            return actual.ToString().Contains(expected.ToString()) ? 0 : 1;
        }

        //[TestMethod]
        public void DownloadStringCommand_TestHappyPath()
        {
            using (CommandLib.DownloadStringCommand cmd = new CommandLib.DownloadStringCommand())
            {
                HappyPathTest.Run(cmd, new Uri(TEST_SERVER + "/<YOUR GET STRING>"), "<SUBSTRING THAT MUST EXIST IN RESULT>", new Comparison<object>(CompareResults));
            }

            using (CommandLib.DownloadStringCommand cmd = new CommandLib.DownloadStringCommand(new Uri(TEST_SERVER + "/<YOUR GET STRING>")))
            {
                HappyPathTest.Run(cmd, null, "<SUBSTRING THAT MUST EXIST IN RESULT>", new Comparison<object>(CompareResults));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestFail()
        {
            using (CommandLib.DownloadStringCommand cmd = new CommandLib.DownloadStringCommand())
            {
                FailTest.Run<System.Net.WebException>(cmd, new Uri("http://www.adslgkggkuytjgdlkhfbjf.com"));
            }
        }

        //[TestMethod]
        public void DownloadStringCommand_TestAbort()
        {
            using (CommandLib.DownloadStringCommand cmd = new CommandLib.DownloadStringCommand())
            {
                AbortTest.Run(cmd, new Uri(TEST_SERVER + "/<YOUR GET STRING WITH DELAY>"), 100);
            }
        }
    }
}
