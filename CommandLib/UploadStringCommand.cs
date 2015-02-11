using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>A <see cref="Command"/> wrapper for <see cref="System.Net.WebClient.UploadStringAsync(System.Uri, string, string)"/>.</summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept an
    /// object of type <see cref="UploadArgs"/> for the'runtimeArg', If not null, that will be used instead of the <see cref="UploadArgs"/>
    /// passed to the constructor.
    /// <para>
    /// This command returns from synchronous execution a String that represents the server response from  the HTTP operation.
    /// 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class UploadStringCommand : CommandLib.AsyncCommand
    {
        /// <summary>
        /// Specifies necessary arguments about the data to upload
        /// </summary>
        public class UploadArgs
        {
            /// <summary>
            /// Constructs an UploadArgs object
            /// </summary>
            /// <param name="uri">The location to upload the data to</param>
            /// <param name="method">The method used to upload the data (for example, "DELETE")</param>
            /// <param name="data">The data to send</param>
            public UploadArgs(System.Uri uri, String method, String data)
            {
                this.uri = uri;
                this.method = method;
                this.data = data;
            }

            /// <summary>
            /// The location to which the data should be uploaded. Changing this during execution will cause undefined behavior.
            /// </summary>
            public System.Uri URI { get { return uri; } }

            /// <summary>
            /// The method used to upload the data (for example, "DELETE"). Changing this during execution will cause undefined behavior.
            /// </summary>
            public String Method { get { return method; } }

            /// <summary>
            /// The data to send.  Changing this during execution will cause undefined behavior.
            /// </summary>
            public String Data { get { return data; } }

            private System.Uri uri;
            private String method;
            private String data;
        }

        /// <summary>
        /// Constructs an UploadStringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        public UploadStringCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Constructs an UploadDataCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="args">Parameters for the upload. If null, be certain to pass a non-null value to the execution routine.</param>
        public UploadStringCommand(UploadArgs args)
            : this(args, new System.Net.WebClient())
        {
            disposeWebClient = true;
        }

        /// <summary>
        /// Constructs an UploadDataCommand object as a top-level <see cref="Command"/> (that is, a <see cref="Command"/> with no owner)
        /// </summary>
        /// <param name="args">Parameters for the upload. If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a WebClient object).
        /// </param>
        public UploadStringCommand(UploadArgs args, System.Net.WebClient webClient)
            : this(args, webClient, null)
        {
        }

        /// <summary>Constructs an UploadDataCommands object</summary>
        /// <param name="args">Parameters for the upload  If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other <see cref="Command"/> objects that accept a WebClient object).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public UploadStringCommand(UploadArgs args, System.Net.WebClient webClient, Command owner)
            : base(owner)
        {
            uploadArgs = args;
            this.webClient = webClient;
            webClient.UploadStringCompleted += UploadCompleted;
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
                    webClient.UploadStringCompleted -= UploadCompleted;

                    if (disposeWebClient)
                    {
                        webClient.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="listener">Not applicable</param>
        /// <param name="runtimeArg">Not applicable</param>
        protected sealed override void AsyncExecuteImpl(CommandLib.ICommandListener listener, Object runtimeArg)
        {
            UploadArgs args = runtimeArg == null ? uploadArgs : (UploadArgs)runtimeArg;
            this.listener = listener;
            webClient.UploadStringAsync(args.URI, args.Method, args.Data, this);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        protected sealed override void AbortImpl()
        {
            // According to docs, this is a no-op if no operation is in progress
            webClient.CancelAsync();
        }

        private void UploadCompleted(Object sender, System.Net.UploadStringCompletedEventArgs e)
        {
            UploadStringCommand cmd = (UploadStringCommand)e.UserState;

            // Because we registered this event handler with the WebClient member, all instances
            // of UploadStringCommand will receive this very event. Filter out all the others, so
            // that we raise exactly one listener callback.
            if (cmd == this)
            {
                if (e.Cancelled)
                {
                    cmd.listener.CommandAborted();
                }
                else if (e.Error == null)
                {
                    cmd.listener.CommandSucceeded(e.Result);
                }
                else
                {
                    cmd.listener.CommandFailed(e.Error);
                }
            }
        }

        private UploadArgs uploadArgs = null;
        private System.Net.WebClient webClient = null;
        private ICommandListener listener = null;
        private bool disposeWebClient = false;
    }
}
