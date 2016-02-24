using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// A <see cref="Command"/> wrapper for <see cref="System.Net.WebClient.DownloadStringAsync(System.Uri)"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object of type System.Uri for the'runtimeArg', If not null, that will be used instead of the System.Uri passed to the constructor.
    /// <para>
    /// This command returns from synchronous execution a String that represents the server response from  the HTTP
    /// operation. The 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class DownloadStringCommand : CommandLib.AsyncCommand
    {
        /// <summary>
        /// Constructs a DownloadStringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        public DownloadStringCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Constructs a DownloadStringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="address">The location of the String to download. If null, be certain to pass a non-null value to the execution routine.</param>
        public DownloadStringCommand(System.Uri address)
            : this(address, new System.Net.WebClient())
        {
            disposeWebClient = true;
        }

        /// <summary>
        /// Constructs a DownloadStringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="address">The location of the String to download. If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a WebClient object).
        /// </param>
        public DownloadStringCommand(System.Uri address, System.Net.WebClient webClient)
            : this(address, webClient, null)
        {
        }

        /// <summary>
        /// Constructs a DownloadStringCommand object
        /// </summary>
        /// <param name="address">The location of the String to download. If null, be certain to pass a non-null value to the execution routine.</param>
        /// <param name="webClient">
        /// The WebClient instance to use for the operation. If null, one will be created. If a non-null webClient is passed,
        /// understand that it is not a thread-safe object, so be careful how that object is shared with other code (including
        /// passing it to multiple instances of this class, or other Command objects that accept a WebClient object).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public DownloadStringCommand(System.Uri address, System.Net.WebClient webClient, Command owner)
            : base(owner)
        {
            this.address = address;
            this.webClient = webClient;
            webClient.DownloadStringCompleted += DownloadCompleted;
        }

        /// <summary>
        /// Returns the underlying WebClient instance used to download the string
        /// </summary>
        public System.Net.WebClient WebClient
        {
            get { return webClient; }
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
                    webClient.DownloadStringCompleted -= DownloadCompleted;

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
            this.listener = listener;
            webClient.DownloadStringAsync(runtimeArg == null ? address : (System.Uri)runtimeArg, this);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        protected sealed override void AbortImpl()
        {
            // According to docs, this is a no-op if no operation is in progress
            webClient.CancelAsync();
        }

        private void DownloadCompleted(Object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            DownloadStringCommand cmd = (DownloadStringCommand)e.UserState;

            // Because we registered this event handler with the WebClient member, all instances
            // of DownloadStringCommand will receive this very event. Filter out all the others, so
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

        private System.Uri address = null;
        private System.Net.WebClient webClient = null;
        private ICommandListener listener = null;
        private bool disposeWebClient = false;
    }
}
