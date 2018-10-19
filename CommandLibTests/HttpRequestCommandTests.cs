using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class HttpRequestCommandTests
    {
        internal class TestRequestGenerator : HttpRequestCommand.IRequestGenerator
        {
            internal TestRequestGenerator(System.Net.Http.HttpMethod method, string url)
                : this(method, url, null)
            {
            }

            internal TestRequestGenerator(System.Net.Http.HttpMethod method, string url, byte[] body)
            {
                _method = method;
                _uri = new Uri(url);
                _body = body;
            }

            public System.Net.Http.HttpRequestMessage GenerateRequest()
            {
                System.Net.Http.HttpRequestMessage msg = new System.Net.Http.HttpRequestMessage(_method, _uri);

                if (_body != null)
                {
                    msg.Content = new System.Net.Http.ByteArrayContent(_body);
                }

                return msg;
            }

	        private readonly System.Net.Http.HttpMethod _method;
            private readonly Uri _uri;
            private readonly byte[] _body;
        }

        private const string TestServer = "http://httpbin.org";

        private int CompareResults(object expected, object actual)
        {
            using (System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual)
            {
				return response.Content.ReadAsStringAsync().Result.Contains(expected.ToString()) ? 0 : 1;
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestHappyPath()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(
                    cmd,
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/get"),
                    "http://httpbin.org/get", CompareResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/get"), new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, "http://httpbin.org/get", CompareResults);
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestFail()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                FailTest.Run<System.Net.Http.HttpRequestException>(
                    cmd,
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/status/404"));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestAbort()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                AbortTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/delay/5"), 100);
            }
        }

        private int CompareUploadResults(object expected, object actual)
        {
            using (System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual)
            {
                byte[] inBytes = (byte[])expected;
                byte[] outBytes = response.Content.ReadAsByteArrayAsync().Result;
                string actualText = System.Text.Encoding.UTF8.GetString(outBytes);
                string searchFor = $"\"Content-Length\": \"{inBytes.Length}\"";
                return actualText.Contains(searchFor) ? 0 : 1;
            }
        }

        [TestMethod]
        public void UploadDataCommand_TestHappyPath()
        {
            byte[] data = new byte[] { 0xa, 0xb };

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data), data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data),
                new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TestServer + "/post", data), data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TestServer + "/post", data),
                new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, data, CompareUploadResults);
            }
        }
        
        [TestMethod]
        public void UploadDataCommand_TestFail()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                FailTest.Run<System.Net.Http.HttpRequestException>(
                    cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer, new byte[] { 0xa, 0xb }));
            }
        }
        
        [TestMethod]
        public void UploadDataCommand_TestAbort()
        {
            byte[] data = new byte[1024 * 1024];

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                AbortTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data), 5);
            }
        }
    }
}
