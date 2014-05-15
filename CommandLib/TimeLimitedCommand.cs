using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// This <see cref="Command"/> wraps another <see cref="Command"/>, throwing a <see cref="TimeoutException"/> if a
    /// specified interval elapses before the underlying command finishes execution.
    /// </summary>
    /// <remarks>
    /// The underlying command to execute must be responsive to abort requests in order for the timeout interval to be honored.
    /// <para>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required by the underlying command to run.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns,
    /// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class TimeLimitedCommand : SyncCommand
    {

        /// <summary>
        /// Constructs a TimeLimitedCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this TimeLimitedCommand object is disposed.
        /// </param>
        /// <param name="timeoutMS">
        /// The timeout interval, in milliseconds. The countdown does not begin until this command is executed.
        /// </param>
        public TimeLimitedCommand(Command commandToRun, int timeoutMS)
            : this(commandToRun, timeoutMS, null)
        {
        }

        /// <summary>
        /// Constructs a TimeLimitedCommand object
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this TimeLimitedCommand object is disposed.
        /// </param>
        /// <param name="timeoutMS">
        /// The timeout interval, in milliseconds. The countdown does not begin until this command is executed.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public TimeLimitedCommand(Command commandToRun, int timeoutMS, Command owner)
            : base(owner)
        {
            this.timeoutMS = timeoutMS;
            this.commandToRun = CreateAbortLinkedCommand(commandToRun);
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the timeout duration.
        /// </returns>
        public override string ExtendedDescription()
        {
            return String.Format("Timeout MS: {0}", timeoutMS);
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
                    commandToRun.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            commandToRun.AsyncExecute(new Listener(this), runtimeArg);
            bool finished = commandToRun.DoneEvent.WaitOne(timeoutMS);

            if (!finished)
            {
                commandToRun.AbortAndWait();
                throw new TimeoutException(String.Format("Timed out after waiting {0}ms for command '{1}' to finish", timeoutMS, commandToRun.Description));
            }

            if (lastException != null)
            {
                throw lastException;
            }

            return result;
        }

        private class Listener : ICommandListener
        {
            public Listener(TimeLimitedCommand command)
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

            private TimeLimitedCommand command;
        }

        private Command commandToRun;
        private int timeoutMS;
        private Object result;
        private Exception lastException;
    }
}
