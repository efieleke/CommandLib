﻿
namespace Sophos.Commands
{
    /// <summary>
    /// Represents a <see cref="Command"/> which is most naturally synchronous in its implementation. If you inherit from this class,
    /// you are responsible for implementing <see cref="SyncExeImpl"/>. This class implements <see cref="AsyncExecuteImpl"/>.
    /// </summary>
    public abstract class SyncCommand : Command
    {
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
		/// This will be called just before command execution, on the same thread from which SyncExecute or AsyncExecute
		/// was called.
		/// </summary>
		/// <param name="runtimeArg">This will be the same value that was passed to <see cref="Command.SyncExecute(object)"/> or
		/// <see cref="Command.AsyncExecute(ICommandListener, object)"/>
		/// </param>
		/// <remarks>
		/// Implementations may initialize values that need to be set just before the command runs (e.g. resetting event
		/// handles and such). Doing so here will prevent timing issues when a command is asynchronously executed followed by
		/// an immediate operation upon the command that is dependent upon it being in the executed state.
		/// </remarks>
		protected virtual void PrepareExecute(object runtimeArg)
        {
        }
        
	    /// <summary>Executes the command and does not return until it finishes.</summary>
		/// <remarks>
		/// Implementations that take noticeable time should be responsive to abort requests, if possible, by either periodically
		/// calling <see cref="Command.CheckAbortFlag"/>, or by implementing this method via calls to owned commands. In  rare cases,
		/// <see cref="Command.AbortImpl"/> may need to be overridden.
		/// </remarks>
		/// <param name="runtimeArg">The implementation of the command defines what this value should be (if it's interested).</param>
		/// <returns>The implementation of the command defines what this value will be.</returns>
		protected abstract object SyncExeImpl(object runtimeArg);

		/// <summary>
		/// Do not call this method from a derived class. It is called by the framework.
		/// </summary>
		/// <param name="listener">Not applicable</param>
		/// <param name="runtimeArg">Not applicable</param>
		protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
		{
			_asyncWrapperCmd = new DelegateCommand<object>(SyncExecuteImpl, this);
			_asyncWrapperCmd.AsyncExecute(listener, runtimeArg);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            PrepareExecute(runtimeArg);
            return SyncExeImpl(runtimeArg);
        }

		private DelegateCommand<object> _asyncWrapperCmd;
	}
}