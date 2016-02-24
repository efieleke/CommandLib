using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>A <see cref="Command"/> wrapper for <see cref="System.Net.WebClient.DownloadFileAsync(System.Uri, String)"/>.</summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will
    /// accept an object of type <see cref="DownloadArgs"/> for the'runtimeArg', If not
    /// null, that object will be used instead of the <see cref="DownloadArgs"/> passed to the constructor.
    /// <para>
    /// This command returns null from synchronous execution, and sets the 'result' parameter of
    /// <see cref="ICommandListener.CommandSucceeded"/> to null.
    /// </para>
    /// </remarks>
    public class DownloadFileCommand : CommandLib.AsyncCommand
    {
        /// <summary>
        /// This is the type of argument that may be passed as 'runtimeArg' for <see cref="Command.SyncExecute(object)"/> or
        /// <see cref="Command.AsyncExecute(ICommandListener, object)"/> of a <see cref="DownloadFileCommand"/> object.
        /// </summary>
        public class DownloadArgs
        {
            /// <summary>
            /// Constructs a DownloadArgs object
            /// </summary>
            /// <param name="uri">The location of the file to download</param>
            /// <param name="destination"> The location to write the downloaded file. If a file already exists at this location, it will be overwritten.</param>
            public DownloadArgs(System.Uri uri, String destination)
            {
                this.uri = uri;
                this.destination = destination;
            }

            /// <summary>
            /// The URI of the file to download
            /// </summary>
            public System.Uri URI { get { return uri; } }

            /// <summary>
            /// The location to write the downloaded file. If a file already exists at this location, it will be overwritten.
            /// </summary>
            public String Destination { get { return destination;  } }
            private System.Uri uri;

            private String destination;
        }

        /// <summary>
        /// Constructs a DownloadFileCommand object as a top-level <see cref="Command"/>
        /// </summary>
        public DownloadFileCommand() : this(null)
        {
        }

        /// <summary>
        /// Constructs a DownloadFileCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="args">Info about the download. If null, be certain to pass a non-null value to the execution routine.</param>
        public DownloadFileCommand(DownloadArgs args)
            : this(args, new System.Net.WebClient())
        {
            disposeWebClient = true;
        }

        /// <summary>
        /// Constructs a DownloadFileCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="args">Info about the download. If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a WebClient object).
        /// </param>
        public DownloadFileCommand(DownloadArgs args, System.Net.WebClient webClient)
            : this(args, webClient, null)
        {
        }

        /// <summary>
        /// Constructs a DownloadFileCommand object
        /// </summary>
        /// <param name="args">Info about the download. If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a WebClient object).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public DownloadFileCommand(DownloadArgs args, System.Net.WebClient webClient, Command owner)
            : base(owner)
        {
            downloadArgs = args;
            this.webClient = webClient;
            webClient.DownloadFileCompleted += DownloadCompleted;
        }

        /// <summary>
        /// Returns the underlying WebClient instance used to download the file
        /// </summary>
        public System.Net.WebClient WebClient
        {
            get { return webClient; }
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="listener">Not applicable</param>
        /// <param name="runtimeArg">Not applicable</param>
        protected sealed override void AsyncExecuteImpl(CommandLib.ICommandListener listener, Object runtimeArg)
        {
            DownloadArgs args = runtimeArg == null ? downloadArgs : (DownloadArgs)runtimeArg;
            this.listener = listener;
            webClient.DownloadFileAsync(args.URI, args.Destination, this);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        protected sealed override void AbortImpl()
        {
            // According to docs, this is a no-op if no operation is in progress
            webClient.CancelAsync();
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
                    webClient.DownloadFileCompleted -= DownloadCompleted;

                    if (disposeWebClient)
                    {
                        webClient.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void DownloadCompleted(Object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            DownloadFileCommand cmd = (DownloadFileCommand)e.UserState;

            // Because we registered this event handler with the WebClient member, all instances
            // of DownloadFileCommand will receive this very event. Filter out all the others, so
            // that we raise exactly one listener callback.
            if (cmd == this)
            {
                if (e.Cancelled)
                {
                    cmd.listener.CommandAborted();
                }
                else if (e.Error == null)
                {
                    cmd.listener.CommandSucceeded(null);
                }
                else
                {
                    cmd.listener.CommandFailed(e.Error);
                }
            }
        }

        private DownloadArgs downloadArgs = null;
        private System.Net.WebClient webClient = null;
        private ICommandListener listener = null;
        private bool disposeWebClient = false;
    }
}
