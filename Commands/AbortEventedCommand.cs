using System;

namespace Sophos.Commands
{
    /// <summary>
    /// A <see cref="Command"/> wrapper that, in addition to responding to normal <see cref="Command.Abort"/> requests, also aborts in response to either
    /// 1) a request to abort a different, specified <see cref="Command"/> instance, or 2) the signaling  of a specified wait handle
    /// (typically an event). 
    /// </summary>
    /// <remarks>
    /// AbortEventedCommand objects must be top level. Any attempt by another <see cref="Command"/> to take ownership of an AbortEventedCommand
    /// will raise an exception. For example, adding this type to a <see cref="SequentialCommands"/> will raise an exception because
    /// <see cref="SequentialCommands"/> would attempt to assume ownership.
    /// <para>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required as the underlying command to run.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns, and the
    /// 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public sealed class AbortEventedCommand : SyncCommand
    {
        /// <summary>
        /// Constructs an AbortEventedCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have an owner.
        /// The passed command will be disposed when this AbortEventedCommand object is disposed.
        /// </param>
        /// <param name="abortEvent">
        /// When signaled, the command to run will be aborted. This object does not take ownership of this parameter.
        /// </param>
        public AbortEventedCommand(Command commandToRun, System.Threading.WaitHandle abortEvent)
            : base(null)
        {
            this.commandToRun = commandToRun;
            this.abortEvent = abortEvent;
            TakeOwnership(commandToRun);
        }

        /// <summary>
        /// Constructs an AbortEventedCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have an owner.
        /// The passed command will be disposed when this AbortEventedCommand object is disposed.
        /// </param>
        /// <param name="commandToWatch">
        /// When this 'commandToWatch' is aborted, the command to run will also be aborted.
        /// </param>
        public AbortEventedCommand(Command commandToRun, Command commandToWatch)
            : base(null)
        {
            this.commandToRun = commandToRun;
            this.commandToWatch = commandToWatch;
            TakeOwnership(commandToRun);
        }

        /// <summary>
        /// This is for diagnostic purposes. Will be null if this command is not linked to another command.
        /// </summary>
        public Command CommandToWatch
        {
            get
            {
                CheckDisposed();
                return commandToWatch;
            }
        }

        /// <summary>
        /// Gets the underlying command to run.
        /// </summary>
        public Command CommandToRun
        {
            get
            {
                CheckDisposed();
                return commandToRun;
            }
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            commandToRun.AsyncExecute(new Listener(this), runtimeArg);
            int waitResult = System.Threading.WaitHandle.WaitAny(new System.Threading.WaitHandle[] { commandToRun.DoneEvent, ExternalAbortEvent });

            if (waitResult == 1)
            {
                Abort();
            }

            commandToRun.Wait();

            if (lastException != null)
            {
                throw lastException;
            }

            return result;
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <returns>Not applicable</returns>
        protected sealed override bool MustBeTopLevel()
        {
            return true;
        }

        private System.Threading.WaitHandle ExternalAbortEvent
        {
            get
            {
                if (abortEvent == null)
                {
                    return commandToWatch.AbortEvent;
                }

                return abortEvent;
            }
        }

        private class Listener : ICommandListener
        {
            public Listener(AbortEventedCommand command)
            {
                this.command = command;
            }

            public void CommandSucceeded(Object result)
            {
                command.result = result;
                command.lastException = null;
            }

            public void CommandAborted()
            {
                command.lastException = new CommandAbortedException();
            }

            public void CommandFailed(Exception exc)
            {
                command.lastException = exc;
            }

            private AbortEventedCommand command;
        }

        private Command commandToRun;
        private System.Threading.WaitHandle abortEvent = null;
        private Command commandToWatch = null;
        private Exception lastException = null;
        private Object result = null;
    }
}
