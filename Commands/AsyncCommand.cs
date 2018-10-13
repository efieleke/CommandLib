using System;

namespace Sophos.Commands
{
    /// <summary>
    /// Represents a <see cref="Command"/> which is most naturally asynchronous in its implementation. If you inherit from this class, you
    /// are responsible for implementing <see cref="Command.AsyncExecuteImpl"/>. This class  implements <see cref="SyncExecuteImpl"/>.
    /// </summary>
    public abstract class AsyncCommand : Command
    {
        /// <summary>
        /// Constructs and AsyncCommand object.
        /// </summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        protected AsyncCommand(Command owner)
            : base(owner)
        {
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
                    doneEvent.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExecuteImpl(Object runtimeArg)
        {
            doneEvent.Reset();
            AsyncExecuteImpl(new Listener(this), runtimeArg);
            doneEvent.WaitOne();

            if (lastException != null)
            {
                throw lastException;
            }

            return result;
        }

        private class Listener : ICommandListener
        {
            public Listener(AsyncCommand command)
            {
                this.command = command;
            }

            public void CommandSucceeded(Object result)
            {
                command.lastException = null;
                command.result = result;
                command.doneEvent.Set();
            }

            public void CommandAborted()
            {
                command.lastException = new CommandAbortedException();
                command.doneEvent.Set();
            }

            public void CommandFailed(Exception exc)
            {
                command.lastException = exc;
                command.doneEvent.Set();
            }

            private AsyncCommand command;
        }

        private System.Threading.ManualResetEvent doneEvent = new System.Threading.ManualResetEvent(false);
        private Exception lastException = null;
        private Object result = null;
    }
}
