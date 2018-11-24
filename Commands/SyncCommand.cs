
using System;
using System.Threading;

namespace Sophos.Commands
{
    /// <summary>
    /// Represents a <see cref="Command"/> which is most naturally synchronous in its implementation. If you inherit from this class,
    /// you are responsible for implementing <see cref="Command.SyncExecuteImpl"/>. This class implements <see cref="AsyncExecuteImpl"/>.
    /// </summary>
    public abstract class SyncCommand : Command
    {
        /// <inheritdoc />
        public sealed override bool IsNaturallySynchronous() { return true; }

        /// <summary>
        /// Constructs a SyncCommand object
        /// </summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        protected SyncCommand(Command owner)
            : base(owner)
        {
        }

		/// <summary>
		/// Do not call this method from a derived class. It is called by the framework.
		/// </summary>
		/// <param name="listener">Not applicable</param>
		/// <param name="runtimeArg">Not applicable</param>
		protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
		{
		    ThreadPool.QueueUserWorkItem(ExecuteAsyncRoutine, new AsyncThreadArg(this, runtimeArg, listener));
        }

        private class AsyncThreadArg
        {
            internal AsyncThreadArg(SyncCommand command, object runtimeArg, ICommandListener listener)
            {
                Command = command;
                RuntimeArg = runtimeArg;
                Listener = listener;
            }

            internal SyncCommand Command { get; }
            internal object RuntimeArg { get; }
            internal ICommandListener Listener { get; }
        }

        private void ExecuteAsyncRoutine(object arg)
        {
            AsyncThreadArg threadArg = (AsyncThreadArg)arg;

            try
            {
                object result = threadArg.Command.SyncExecuteImpl(threadArg.RuntimeArg);
                threadArg.Listener.CommandSucceeded(result);
            }
            catch (CommandAbortedException)
            {
                threadArg.Listener.CommandAborted();
            }
            catch (Exception exc)
            {
                threadArg.Listener.CommandFailed(exc);
            }
        }
	}
}
