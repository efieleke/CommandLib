using System;

namespace Sophos.Commands
{
    /// <summary>
    /// This <see cref="Command"/> wraps another command, allowing the command to be retried upon failure, up to any number of times.
    /// </summary>
    /// <remarks>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required as the underlying command to run.
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns,
    /// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class RetryableCommand : Commands.SyncCommand
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
            /// <returns>false if the command should not be retried (which will propogate the exception). Otherwise true to perform a retry after the specified wait time</returns>
            bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime);
        }

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
            this.command = command;
            TakeOwnership(command);
            pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(0), null, this);
            this.callback = callback;
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            int i = 0;

            while(true)
            {
                try
                {
                    CheckAbortFlag();
                    return command.SyncExecute(runtimeArg);
                }
                catch(CommandAbortedException)
                {
                    throw;
                }
                catch(Exception exc)
                {
                    TimeSpan waitTime;

                    if (!callback.OnCommandFailed(++i, exc, out waitTime))
                    {
                        throw;
                    }

                    pauseCmd.Duration = waitTime;
                    pauseCmd.SyncExecute();
                }
            }
        }

        private Command command;
        private PauseCommand pauseCmd;
        private IRetryCallback callback;
    }
}
