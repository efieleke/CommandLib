using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;

namespace CommandLib
{
    /// <summary>
    /// A <see cref="Command"/> wrapper for <see cref="System.Net.Http.HttpClient.SendAsync(System.Net.Http.HttpRequestMessage)"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object of type IHttpRequestGenerator for the'runtimeArg', If not null, that will be used instead of the
    /// IHttpRequestGenerator passed to the constructor.
    /// <para>
    /// This command returns from synchronous execution a System.Net.Http.HttpResponseMessage that represents the server response from  the HTTP
    /// operation. The 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class HttpRequestCommand : SyncCommand
    {
        /// <summary>
        /// Users of HttpRequestCommand must implement this interface and pass an instance to either the contructor or SyncExecute.
        /// </summary>
        public interface IHttpRequestGenerator
        {
            /// <summary>
            /// Every time this is called, it should return a new object, because requests cannot be reused.
            /// </summary>
            /// <returns>the request to send</returns>
            System.Net.Http.HttpRequestMessage GenerateRequest();
        }

        /// <summary>
        /// Helper method to convert an HttpContent object to a String. This can be used on the Content
        /// property of the HttpResponseMessage object that is returned from the HttpGetCommand's execution.
        /// </summary>
        /// <param name="content">The content to covert</param>
        /// <returns>The content as a string</returns>
        public static String ContentAsString(System.Net.Http.HttpContent content)
        {
            using (System.Threading.Tasks.Task<String> task = content.ReadAsStringAsync())
            {
                task.Wait();
                return task.Result;
            }
        }

        /// <summary>
        /// Helper method to write an HttpContent object to a file. This can be used on the Content
        /// property of the HttpResponseMessage object that is returned from the HttpGetCommand's execution.
        /// The file will be either created or overwritten.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        public static void WriteContentToFile(System.Net.Http.HttpContent content, String fileName)
        {
            using (var fileStream = System.IO.File.Create(fileName))
            {
                using (System.Threading.Tasks.Task task = content.CopyToAsync(fileStream))
                {
                    task.Wait();
                }
            }
        }

        /// <summary>
        /// Constructs a HttpRequestCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="ensureSuccessStatusCode">If true, and the status code indicates failure, this command will fail with an HttpRequestException</param>
        public HttpRequestCommand(bool ensureSuccessStatusCode)
            : this(ensureSuccessStatusCode, null)
        {
        }

        /// <summary>
        /// Constructs a HttpGetCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="ensureSuccessStatusCode">If true, and the status code indicates failure, this command will fail with an HttpRequestException</param>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        public HttpRequestCommand(bool ensureSuccessStatusCode, IHttpRequestGenerator requestGenerator)
            : this(ensureSuccessStatusCode, requestGenerator, new System.Net.Http.HttpClient())
        {
            disposeHttpClient = true;
        }

        /// <summary>
        /// Constructs a HttpGetCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="ensureSuccessStatusCode">If true, and the status code indicates failure, this command will fail with an HttpRequestException</param>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="httpClient">
        /// The HttpClient instance to use for the operation. Be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a HttpClient object).
        /// </param>
        public HttpRequestCommand(bool ensureSuccessStatusCode, IHttpRequestGenerator requestGenerator, System.Net.Http.HttpClient httpClient)
            : this(ensureSuccessStatusCode, requestGenerator, httpClient, null)
        {
        }

        /// <summary>
        /// Constructs a HttpGetCommand object
        /// </summary>
        /// <param name="ensureSuccessStatusCode">If true, and the status code indicates failure, this command will fail with an HttpRequestException</param>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="httpClient">
        /// The HttpClient instance to use for the operation. Be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a HttpClient object).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public HttpRequestCommand(bool ensureSuccessStatusCode, IHttpRequestGenerator requestGenerator, System.Net.Http.HttpClient httpClient, Command owner)
            : base(owner)
        {
            this.ensureSuccessStatusCode = ensureSuccessStatusCode;
            this.requestGenerator = requestGenerator;
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Returns the underlying HttpClient instance used to download the data
        /// </summary>
        public System.Net.Http.HttpClient Client
        {
            get { return httpClient; }
        }

        /// <summary>
        /// Implementations should override only if they contain members that must be disposed. Remember to invoke the base class implementation from within any override.
        /// </summary>
        /// <param name="disposing">Will be true if this was called as a direct result of the object being explicitly disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    cancelTokenSource.Dispose();

                    if (disposeHttpClient)
                    {
                        httpClient.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">see base class documentation</param>
        protected override void PrepareExecute(Object runtimeArg)
        {
            cancelTokenSource.Dispose();
            cancelTokenSource = new System.Threading.CancellationTokenSource();
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>The System.Net.Http.HttpResponseMessage</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            IHttpRequestGenerator generator = runtimeArg == null ? requestGenerator : (IHttpRequestGenerator)runtimeArg;

            // The same request message cannot be sent more than once, so we have to contruct a new one every time.
            using (System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> task =
                httpClient.SendAsync(
                    generator.GenerateRequest(), System.Net.Http.HttpCompletionOption.ResponseContentRead, cancelTokenSource.Token))
            {
                try
                {
                    task.Wait();

                    if (ensureSuccessStatusCode)
                    {
                        task.Result.EnsureSuccessStatusCode();
                    }

                    return task.Result;
                }
                catch (System.AggregateException e)
                {
                    if (e.GetBaseException() is System.OperationCanceledException)
                    {
                        throw new CommandAbortedException();
                    }

                    throw e.GetBaseException();
                }
                catch (System.OperationCanceledException)
                {
                    throw new CommandAbortedException();
                }
            }
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        protected sealed override void AbortImpl()
        {
            cancelTokenSource.Cancel();
        }

        private bool ensureSuccessStatusCode;
        private IHttpRequestGenerator requestGenerator = null;
        private System.Net.Http.HttpClient httpClient = null;
        private bool disposeHttpClient = false;
        private System.Threading.CancellationTokenSource cancelTokenSource = new System.Threading.CancellationTokenSource();
    }
}
