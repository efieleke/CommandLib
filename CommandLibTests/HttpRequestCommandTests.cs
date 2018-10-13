﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class HttpRequestCommandTests
    {
        internal class TestRequestGenerator : HttpRequestCommand.IRequestGenerator
        {
            internal TestRequestGenerator(System.Net.Http.HttpMethod method, String url)
                : this(method, url, null)
            {
            }

            internal TestRequestGenerator(System.Net.Http.HttpMethod method, String url, byte[] body)
            {
                this.method = method;
                uri = new Uri(url);
                this.body = body;
            }

            public System.Net.Http.HttpRequestMessage GenerateRequest()
            {
                System.Net.Http.HttpRequestMessage msg = new System.Net.Http.HttpRequestMessage(method, uri);

                if (body != null)
                {
                    msg.Content = new System.Net.Http.ByteArrayContent(body);
                }

                return msg;
            }

            System.Net.Http.HttpMethod method;
            private Uri uri;
            private byte[] body;
        }

        private const String TEST_SERVER = "http://httpbin.org";

        private int CompareResults(object expected, object actual)
        {
            using (System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual)
            {
                return HttpRequestCommand.ContentAsString(response.Content).Result.Contains(expected.ToString()) ? 0 : 1;
            }
        }

        private int CompareFileResults(object expected, object actual)
        {
            using (System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual)
            {
                String outputFileName = System.IO.Path.GetTempFileName();
                System.IO.File.Delete(outputFileName);
                HttpRequestCommand.WriteContentToFile(response.Content, outputFileName).Wait();
                System.IO.File.Delete(outputFileName);
                return 0;
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestHappyPath()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(
                    cmd,
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TEST_SERVER + "/get"),
                    "http://httpbin.org/get", new Comparison<object>(CompareResults));
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TEST_SERVER + "/get"), new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, "http://httpbin.org/get", new Comparison<object>(CompareResults));
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TEST_SERVER + "/get"), new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, "http://httpbin.org/get", new Comparison<object>(CompareFileResults));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestFail()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                FailTest.Run<System.Net.Http.HttpRequestException>(
                    cmd,
                    new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TEST_SERVER + "/status/404"));
            }
        }

        [TestMethod]
        public void DownloadStringCommand_TestAbort()
        {
            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                AbortTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Get, TEST_SERVER + "/delay/5"), 100);
            }
        }

        private int CompareUploadResults(object expected, object actual)
        {
            using (System.Net.Http.HttpResponseMessage response = (System.Net.Http.HttpResponseMessage)actual)
            {
                byte[] inBytes = (byte[])expected;
                byte[] outBytes = response.Content.ReadAsByteArrayAsync().Result;
                String actualText = System.Text.Encoding.UTF8.GetString(outBytes);
                String searchFor = String.Format("\"Content-Length\": \"{0}\"", inBytes.Length);
                return actualText.Contains(searchFor) ? 0 : 1;
            }
        }

        [TestMethod]
        public void UploadDataCommand_TestHappyPath()
        {
            byte[] data = new byte[] { 0xa, 0xb };

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TEST_SERVER + "/put", data), data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TEST_SERVER + "/put", data),
                new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, null, data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                HappyPathTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TEST_SERVER + "/post", data), data, CompareUploadResults);
            }

            using (HttpRequestCommand cmd = new HttpRequestCommand(
                new TestRequestGenerator(System.Net.Http.HttpMethod.Post, TEST_SERVER + "/post", data),
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
                    cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TEST_SERVER, new byte[] { 0xa, 0xb }));
            }
        }
        
        [TestMethod]
        public void UploadDataCommand_TestAbort()
        {
            byte[] data = new byte[1024 * 1024];

            using (HttpRequestCommand cmd = new HttpRequestCommand(null, new HttpRequestCommand.EnsureSuccessStatusCodeResponseChecker()))
            {
                AbortTest.Run(cmd, new TestRequestGenerator(System.Net.Http.HttpMethod.Put, TEST_SERVER + "/put", data), 5);
            }
        }
    }
}
