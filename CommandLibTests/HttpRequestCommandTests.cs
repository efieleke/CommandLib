using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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
            HappyPathTest.Run(
                new HttpRequestCommand(null,new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/get"),
                "://httpbin.org/get",
                CompareResults);

            HappyPathTest.Run(
                new HttpRequestCommand(
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/get"),
                    new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                null,
                "://httpbin.org/get",
                CompareResults);

            using (var httpRequestCommand = new HttpRequestCommand())
            {
                System.Net.Http.HttpResponseMessage result = httpRequestCommand.SyncExecute(new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/get"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestFail()
        {
            FailTest.Run<System.Net.Http.HttpRequestException>(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/status/404"));
        }

        [TestMethod]
        public void DownloadStringCommand_TestAbort()
        {
            AbortTest.Run(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/delay/5"),
                100);
        }

        [TestMethod]
        public void DownloadStringCommand_TestTimeout()
        {
            using (var requestCommand = new HttpRequestCommand(new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TestServer + "/delay/5")))
            {
                requestCommand.Client.Timeout = TimeSpan.FromMilliseconds(1);

                try
                {
                    requestCommand.SyncExecute();
                    Assert.Fail("Did not expect to get here.");
                }
                catch (TimeoutException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void TestStatusExceptionSerialization()
        {
            var exception = new HttpRequestCommand.HttpStatusException
            {
                StatusCode = HttpStatusCode.BadRequest,
                ResponseBody = "Response Body"
            };

            var serializer = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, exception);
                stream.Seek(0, SeekOrigin.Begin);
                HttpRequestCommand.HttpStatusException exc2 = (HttpRequestCommand.HttpStatusException)serializer.Deserialize(stream);
                Assert.AreEqual(exception.Message, exc2.Message);
                Assert.AreEqual(exception.Source, exc2.Source);
                Assert.AreEqual(exception.ResponseBody, exc2.ResponseBody);
                Assert.AreEqual(exception.StatusCode, exc2.StatusCode);

                try
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    exception.GetObjectData(null, new StreamingContext());
                    Assert.Fail("Should not get here.");
                }
                catch (ArgumentNullException)
                {
                    // expected
                }
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

            HappyPathTest.Run(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data),
                data,
                CompareUploadResults);

            HappyPathTest.Run(
                new HttpRequestCommand(
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data),
                    new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                null,
                data,
                CompareUploadResults);

            HappyPathTest.Run(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TestServer + "/post", data),
                data,
                CompareUploadResults);

            HappyPathTest.Run(
                new HttpRequestCommand(
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TestServer + "/post", data),
                    new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                null,
                data,
                CompareUploadResults);
        }
        
        [TestMethod]
        public void UploadDataCommand_TestFail()
        {
            FailTest.Run<System.Net.Http.HttpRequestException>(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer, new byte[] { 0xa, 0xb }));
        }
        
        [TestMethod]
        public void UploadDataCommand_TestAbort()
        {
            byte[] data = new byte[1024 * 1024];

            AbortTest.Run(
                new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()),
                new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TestServer + "/put", data),
                5);
        }
    }
}
