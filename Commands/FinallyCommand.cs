using System;

namespace Sophos.Commands
{
    /// <summary>
    /// This <see cref="Command"/> wraps another command, and runs a client-specified operation upon completion.
    /// This operation is run if the command completes successfully or if it fails, but it is not run the if the
    /// if the command is aborted.
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
    public abstract class FinallyCommand : Command
    {
        /// <summary>
        /// Constructs a FinallyCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this FinallyCommand object is disposed.
        /// </param>
        protected FinallyCommand(Command command) : this(command, null)
        {
        }

        /// <summary>
        /// Constructs a FinallyCommand object
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this FinallyCommand object is disposed.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        protected FinallyCommand(Command command, Command owner) : base(owner)
        {
            _command = command;
            TakeOwnership(command);
        }

        /// <inheritdoc />
        public override bool IsNaturallySynchronous()
        {
            return _command.IsNaturallySynchronous();
        }

        /// <summary>
        /// This method is called after command success or failure, but before any of the ICommandListener callbacks are invoked
        /// (in the case of asynchronous execution). Note that this is not called if this command is aborted.
        /// </summary>
        protected abstract void Finally();

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            object result;

            try
            {
                result = _command.SyncExecute(runtimeArg);
            }
            catch (CommandAbortedException)
            {
                throw;
            }
            catch (Exception e)
            {
                try
                {
                    Finally();
                }
                catch (Exception exception)
                {
                    throw new AggregateException(e, exception);
                }

                throw;
            }

            Finally();
            return result;
        }

        /// <inheritdoc />
        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            _command.AsyncExecute(
                result =>
                {
                    if (AbortRequested)
                    {
                        listener.CommandAborted();
                        return;
                    }

                    try
                    {
                        Finally();
                    }
                    catch (Exception e)
                    {
                        listener.CommandFailed(e);
                        return;
                    }

                    listener.CommandSucceeded(result);
                },
                listener.CommandAborted,
                e =>
                {
                    try
                    {
                        Finally();
                    }
                    catch (Exception exception)
                    {
                        listener.CommandFailed(new AggregateException(e, exception));
                        return;
                    }

                    listener.CommandFailed(e);
                },
                runtimeArg);
        }

        private readonly Command _command;
    }
}
