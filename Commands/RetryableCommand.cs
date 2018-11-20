using System;

namespace Sophos.Commands
{
	/// <summary>
	/// This <see cref="Command"/> wraps another command, allowing the command to be retried upon failure, up to any number of times.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
	/// should be of the same type required as the underlying command to run.
	/// </para>
	/// <para>
	/// This command returns from synchronous execution the same value that the underlying command to run returns,
	/// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
	/// </para>
	/// </remarks>
	public class RetryableCommand : SyncCommand
    {
        /// <summary>
        /// Interface that defines aspects of retry behavior
        /// </summary>
        public interface IRetryCallback
        {
            /// <summary>
            /// Callback for when a command to be retried fails
            /// </summary>
            /// <param name="failNumber">The number of times the command has failed (including this time)</param>
            /// <param name="reason">The reason for failure. Modifications to this object will be ignored.</param>
            /// <param name="waitTime">
            /// The amount of time to wait before retrying. This value is ignored if the method returns false.
            /// </param>
            /// <returns>false if the command should not be retried (which will propagate the exception). Otherwise true to perform a retry after the specified wait time</returns>
            bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime);
        }


        /// <summary>
        /// Callback for when a command to be retried fails
        /// </summary>
        /// <param name="failNumber">The number of times the command has failed (including this time)</param>
        /// <param name="reason">The reason for failure. Modifications to this object will be ignored.</param>
        /// <param name="waitTime">
        /// The amount of time to wait before retrying. This value is ignored if the method returns false.
        /// </param>
        /// <returns>false if the command should not be retried (which will propagate the exception). Otherwise true to perform a retry after the specified wait time</returns>
        public delegate bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime);

        /// <summary>
        /// Constructs a RetryableCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RetryableCommand object is disposed.
        /// </param>
        /// <param name="callback">This object defines aspects of retry behavior</param>
        public RetryableCommand(Command command, IRetryCallback callback) : this(command, callback, null)
        {
        }

        /// <summary>
        /// Constructs a RetryableCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RetryableCommand object is disposed.
        /// </param>
        /// <param name="callback">This defines aspects of retry behavior</param>
        public RetryableCommand(Command command, OnCommandFailed callback) : this(command, callback, null)
        {
        }

        /// <summary>
        /// Constructs a RetryableCommands object
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RetryableCommand object is disposed.
        /// </param>
        /// <param name="callback">This object defines aspects of retry behavior</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public RetryableCommand(Command command, IRetryCallback callback, Command owner)
            : base(owner)
        {
            _command = command;
            TakeOwnership(command);
            _pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(0), null, this);
            _callback = callback;
        }

        /// <summary>
        /// Constructs a RetryableCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RetryableCommand object is disposed.
        /// </param>
        /// <param name="callback">This defines aspects of retry behavior</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public RetryableCommand(Command command, OnCommandFailed callback, Command owner)
            : this(command, new RetryCallback(callback), owner)
        {
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            int i = 0;

            while(true)
            {
                try
                {
                    CheckAbortFlag();
                    return _command.SyncExecute(runtimeArg);
                }
                catch(CommandAbortedException)
                {
                    throw;
                }
                catch(Exception exc)
                {
	                if (!_callback.OnCommandFailed(++i, exc, out TimeSpan waitTime))
                    {
                        throw;
                    }

                    _pauseCmd.Duration = waitTime;
                    _pauseCmd.SyncExecute();
                }
            }
        }

        private class RetryCallback : IRetryCallback
        {
            internal RetryCallback(OnCommandFailed retryCallback)
            {
                _retryCallback = retryCallback;
            }

            public bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime)
            {
                return _retryCallback(failNumber, reason, out waitTime);
            }

            private readonly OnCommandFailed _retryCallback;
        }

        private readonly Command _command;
        private readonly PauseCommand _pauseCmd;
        private readonly IRetryCallback _callback;
    }
}
