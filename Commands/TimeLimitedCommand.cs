using System;

namespace Sophos.Commands
{
	/// <summary>
	/// This <see cref="Command"/> wraps another <see cref="Command"/>, throwing a <see cref="TimeoutException"/> if a
	/// specified interval elapses before the underlying command finishes execution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The underlying command to execute must be responsive to abort requests in order for the timeout interval to be honored.
	/// </para>
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
            _timeoutMS = timeoutMS;
            _commandToRun = commandToRun;
            TakeOwnership(_commandToRun);
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the timeout duration.
        /// </returns>
        public override string ExtendedDescription()
        {
            return $"Timeout MS: {_timeoutMS}";
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            _commandToRun.AsyncExecute(new Listener(this), runtimeArg);
            bool finished = _commandToRun.DoneEvent.WaitOne(_timeoutMS);

            if (!finished)
            {
                AbortChildCommand(_commandToRun);
                _commandToRun.Wait();
                ResetChildAbortEvent(_commandToRun);
                throw new TimeoutException($"Timed out after waiting {_timeoutMS}ms for command '{_commandToRun.Description}' to finish");
            }

            if (_lastException != null)
            {
                throw _lastException;
            }

            return _result;
        }

        private class Listener : ICommandListener
        {
            public Listener(TimeLimitedCommand command)
            {
                _command = command;
            }

            public void CommandSucceeded(object result)
            {
                _command._result = result;
                _command._lastException = null;
            }

            public void CommandAborted()
            {
                _command._lastException = new CommandAbortedException();
            }

            public void CommandFailed(Exception exc)
            {
                _command._lastException = exc;
            }

            private readonly TimeLimitedCommand _command;
        }

        private readonly Command _commandToRun;
        private readonly int _timeoutMS;
        private volatile object _result;
        private volatile Exception _lastException;
    }
}
