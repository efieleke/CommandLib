using System;

namespace Sophos.Commands
{
    /// <summary>
    /// Represents a <see cref="Command"/> which is most naturally asynchronous in its implementation. If you inherit from this class, you
    /// are responsible for implementing <see cref="Command.AsyncExecuteImpl"/>. This class  implements <see cref="SyncExecuteImpl"/>.
    /// If your implementation makes use of asynchronous Tasks (i.e. the Task class), inherit from TaskCommand instead.
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
                    _doneEvent.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            _doneEvent.Reset();
            AsyncExecuteImpl(new Listener(this), runtimeArg);
            _doneEvent.WaitOne();

            if (_lastException != null)
            {
                throw _lastException;
            }

            return _result;
        }

        private class Listener : ICommandListener
        {
            public Listener(AsyncCommand command)
            {
                _command = command;
            }

            public void CommandSucceeded(object result)
            {
                _command._lastException = null;
                _command._result = result;
                _command._doneEvent.Set();
            }

            public void CommandAborted()
            {
                _command._lastException = new CommandAbortedException();
                _command._doneEvent.Set();
            }

            public void CommandFailed(Exception exc)
            {
                _command._lastException = exc;
                _command._doneEvent.Set();
            }

            private readonly AsyncCommand _command;
        }

        private readonly System.Threading.ManualResetEvent _doneEvent = new System.Threading.ManualResetEvent(false);
        private Exception _lastException;
        private object _result;
    }
}
