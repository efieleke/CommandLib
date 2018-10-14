﻿using System;
using System.Net.Http;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// A <see cref="Command"/> wrapper for <see cref="HttpClient.SendAsync(HttpRequestMessage)"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object of type IHttpRequestGenerator for the'runtimeArg', If not null, that will be used instead of the
    /// IHttpRequestGenerator passed to the constructor.
    /// <para>
    /// This command returns from synchronous execution a .HttpResponseMessage that represents the server response from  the HTTP
    /// operation. The 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion. It is the caller's
    /// responsibility to dispose of this response object.
    /// </para>
    /// </remarks>
    public class HttpRequestCommand : TaskCommand<HttpResponseMessage>
    {
        /// <summary>
        /// Users of HttpRequestCommand must implement this interface and pass an instance to either the contructor or SyncExecute.
        /// </summary>
        public interface IRequestGenerator
        {
            /// <summary>
            /// Every time this is called, it should return a new object, because requests cannot be reused.
            /// </summary>
            /// <returns>the request to send</returns>
            HttpRequestMessage GenerateRequest();
        }

        /// <summary>
        /// Implement this interface if you wish to force command execution to fail depending upon the response (e.g. error status codes)
        /// </summary>
        public interface IResponseChecker
        {
            /// <summary>
            /// Throw an exception from this method if the response is deemed to be a failure that should cause the command to fail.
            /// </summary>
            /// <param name="response">The response to evalulate. Note that implementors must *not* dispose this parameter.</param>
            Task CheckResponse(HttpResponseMessage response);
        }

        /// <summary>
        /// This is used as the inner exception for the exception thrown by EnsureSuccessStatusCodeResponseChecker
        /// </summary>
        [SerializableAttribute]
        public class HttpStatusException : Exception
        {
            /// <summary>
            /// Constructs a HttpStatusException object
            /// </summary>
            public HttpStatusException()
            {
            }

            /// <summary>
            /// Constructs a HttpStatusException object
            /// </summary>
            /// <param name="message">See <see cref="Exception"/> documentation</param>
            public HttpStatusException(String message) : base(message)
            {
            }

            /// <summary>
            /// Constructs a HttpStatusException object
            /// </summary>
            /// <param name="message">See <see cref="Exception"/> documentation</param>
            /// <param name="innerException">>See <see cref="Exception"/> documentation</param>
            public HttpStatusException(String message, Exception innerException)
                : base(message, innerException)
            {
            }

            /// <summary>
            /// Constructs a HttpStatusException object
            /// </summary>
            /// <param name="info">>See <see cref="Exception"/> documentation</param>
            /// <param name="context">>See <see cref="Exception"/> documentation</param>
            protected HttpStatusException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
                : base(info, context)
            {
            }

            /// <summary>
            /// The status code returned by the server
            /// </summary>
            public System.Net.HttpStatusCode StatusCode { get; set; }

            /// <summary>
            /// The body of the response returned by the server
            /// </summary>
            public String ResponseBody { get; set; }

            /// <summary>
            /// Exists to support serialization
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
            public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("info");
                }

                info.AddValue("StatusCode", this.StatusCode);
                info.AddValue("ResponseBody", this.ResponseBody);
                base.GetObjectData(info, context);
            }
        }

        /// <summary>
        /// Implementation of IResposeChecker that throws an HttpRequestException if the status code
		/// represents an error, with an inner exception with more detail.
        /// </summary>
        public class EnsureSuccessStatusCodeResponseChecker : IResponseChecker
        {
            /// <summary>
            /// Throws an HttpRequestException if the status code represents an error
            /// </summary>
            /// <param name="response">The response that is evaluated</param>
            public async Task CheckResponse(HttpResponseMessage response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    HttpStatusException reason = new HttpStatusException() { StatusCode = response.StatusCode };

                    if (response.Content != null)
                    {
                        try
                        {
                            reason.ResponseBody = await HttpRequestCommand.ContentAsString(response.Content);
                        }
                        catch(Exception)
                        {
                            // We have no guarantee that the server will give us a response body that can be
                            // serialized as a string. So just eat any failure here and instead propogate
                            // the real error.
                        }
                    }

                    throw new HttpRequestException(response.ReasonPhrase, reason);
                }
            }
        }

        /// <summary>
        /// Helper method to convert an HttpContent object to a String. This can be used on the Content
        /// property of the HttpResponseMessage object that is returned from the HttpGetCommand's execution.
        /// </summary>
        /// <param name="content">The content to covert</param>
        /// <returns>The content as a string</returns>
        public static Task<String> ContentAsString(HttpContent content)
        {
			return content.ReadAsStringAsync();
        }

        /// <summary>
        /// Helper method to write an HttpContent object to a file. This can be used on the Content
        /// property of the HttpResponseMessage object that is returned from the HttpGetCommand's execution.
        /// The file will be either created or overwritten.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        public static async Task WriteContentToFile(HttpContent content, String fileName)
        {
            using (var fileStream = System.IO.File.Create(fileName))
            {
				await content.CopyToAsync(fileStream);
            }
        }

        /// <summary>
        /// Constructs a HttpRequestCommand object as a top-level <see cref="Command"/>
        /// </summary>
        public HttpRequestCommand() : this(null)
        {
        }

        /// <summary>
        /// Constructs a HttpGetCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        public HttpRequestCommand(IRequestGenerator requestGenerator) : this(requestGenerator, null)
        {
        }

        /// <summary>
        /// Constructs a HttpGetCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="responseChecker">If not null, responses will be checked for failure using this object. If
        /// null, error status codes are not treated as a failure to execute the command. EnsureSuccessStatusCodeResponseChecker
        /// is provided as an implementation.
        /// </param>
        public HttpRequestCommand(IRequestGenerator requestGenerator, IResponseChecker responseChecker)
            : this(requestGenerator, responseChecker, new HttpClient())
        {
            disposeHttpClient = true;
        }

        /// <summary>
        /// Constructs a HttpGetCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="responseChecker">If not null, responses will be checked for failure using this object. If
        /// null, error status codes are not treated as a failure to execute the command. EnsureSuccessStatusCodeResponseChecker
        /// is provided as an implementation.
        /// </param>
        /// <param name="httpClient">
        /// The HttpClient instance to use for the operation. Be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a HttpClient object).
        /// </param>
        public HttpRequestCommand(IRequestGenerator requestGenerator, IResponseChecker responseChecker, HttpClient httpClient)
            : this(requestGenerator, responseChecker, httpClient, null)
        {
        }

        /// <summary>
        /// Constructs a HttpGetCommand object
        /// </summary>
        /// <param name="requestGenerator">If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="responseChecker">If not null, responses will be checked for failure using this object. If
        /// null, error status codes are not treated as a failure to execute the command. EnsureSuccessStatusCodeResponseChecker
        /// is provided as an implementation.
        /// </param>
        /// <param name="httpClient">
        /// The HttpClient instance to use for the operation. Be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a HttpClient object).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public HttpRequestCommand(IRequestGenerator requestGenerator, IResponseChecker responseChecker, HttpClient httpClient, Command owner)
            : base(owner)
        {
            this.requestGenerator = requestGenerator;
            this.responseChecker = responseChecker;
            Client = httpClient;
        }

        /// <summary>
        /// Returns the underlying HttpClient instance used to download the data
        /// </summary>
        public HttpClient Client { get; }

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
                        Client.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        protected sealed override void AbortImpl()
        {
            _aborted = true;
            cancelTokenSource.Cancel();
        }

		/// <summary>
		/// Do not call this method from a derived class. It is called by the framework.
		/// </summary>
		/// <param name="runtimeArg"></param>
		/// <returns></returns>
		protected sealed override async Task<HttpResponseMessage> CreateTask(object runtimeArg)
		{
			// Dispose of the prior cancel token source after the request is performed. We need to keep
			// the prior one until then for thread safety reasons
			using (var previousCancelTokenSource = cancelTokenSource)
			{
				cancelTokenSource = new System.Threading.CancellationTokenSource();
				_aborted = false;
				IRequestGenerator generator = runtimeArg == null ? requestGenerator : (IRequestGenerator)runtimeArg;

				using (HttpRequestMessage request = generator.GenerateRequest())
				{
					// The same request message cannot be sent more than once, so we have to contruct a new one every time.
					Task<HttpResponseMessage> task = Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelTokenSource.Token);

					try
					{
						HttpResponseMessage message = await task;

						if (responseChecker != null)
						{
							try
							{
								await responseChecker.CheckResponse(message);
							}
							catch (Exception)
							{
								message.Dispose();
								task.Dispose();
								throw;
							}
						}

						return message;
					}
					catch (System.OperationCanceledException)
					{
						if (_aborted)
						{
							// We got here because Abort() was called
							throw new CommandAbortedException();
						}

						// We got here because the request timed out
						throw new TimeoutException();
					}
				}
			}
		}

		private readonly IRequestGenerator requestGenerator = null;
        private IResponseChecker responseChecker = null;
        private readonly bool disposeHttpClient = false;
        private bool _aborted = false;
        private System.Threading.CancellationTokenSource cancelTokenSource = new System.Threading.CancellationTokenSource();
    }
}
