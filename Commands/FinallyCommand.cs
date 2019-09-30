using System;

namespace Sophos.Commands
{
    /// <summary>
    /// This <see cref="Command"/> wraps another command, and runs a client-specified command upon either success
    /// of failure, and optionally upon abortion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// is ignored.
    /// </para>
    /// <para>
    /// This command returns null from synchronous execution, and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/>
    /// will be set to null.
    /// </para>
    /// </remarks>
    public class FinallyCommand : SyncCommand
    {
        /// <summary>
        /// Constructs a FinallyCommand object as a top level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this FinallyCommand object is disposed.
        /// </param>
        /// <param name="uponCompletionCommand">
        /// The command to run upon success or failure (and optionally upon abortion). This command must not have an owner. It will
        /// be disposed when this FinallyCommand is disposed.
        /// </param>
        /// <param name="evenUponAbort">
        /// If this value is true, uponCompletionCommand will be run even upon abortion, and uponCompletionCommand will not be responsive to abort requests of this object.
        /// If false, uponCompletionCommand will not be run upon abortion, and if it is running while an abort request is made of this object, it will be aborted.
        /// </param>
        public FinallyCommand(Command commandToRun, Command uponCompletionCommand, bool evenUponAbort) : this(commandToRun, uponCompletionCommand, evenUponAbort, null)
        {
        }

        /// <summary>
        /// Constructs a FinallyCommand object
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this FinallyCommand object is disposed.
        /// </param>
        /// <param name="uponCompletionCommand">
        /// The command to run upon success or failure (and optionally upon abortion). This command must not have an owner. It will
        /// be disposed when this FinallyCommand is disposed.
        /// </param>
        /// <param name="evenUponAbort">
        /// If this value is true, uponCompletionCommand will be run even upon abortion, and uponCompletionCommand will not be responsive to abort requests of this object.
        /// If false, uponCompletionCommand will not be run upon abortion, and if it is running while an abort request is made of this object, it will be aborted.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public FinallyCommand(Command commandToRun, Command uponCompletionCommand, bool evenUponAbort, Command owner) : base(owner)
        {
            _errorTrappingCommand = new ErrorTrappingCommand(commandToRun, evenUponAbort, this);
            _uponCompletionCommand = uponCompletionCommand;
            TakeOwnership(_uponCompletionCommand);
        }

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            object result = _errorTrappingCommand.SyncExecute(runtimeArg);

            if (_errorTrappingCommand.Exception is CommandAbortedException)
            {
                ResetChildAbortEvent(_uponCompletionCommand);
            }

            try
            {
                _uponCompletionCommand.SyncExecute(null);
            }
            catch (Exception e)
            {
                if (_errorTrappingCommand.Exception != null)
                {
                    throw new AggregateException(_errorTrappingCommand.Exception, e);
                }

                throw;
            }

            if (_errorTrappingCommand.Exception != null)
            {
                throw _errorTrappingCommand.Exception;
            }

            return result;
        }

        private class ErrorTrappingCommand : SyncCommand
        {
            public ErrorTrappingCommand(Command commandToRun, bool trapAbort, Command owner) : base(owner)
            {
                _commandToRun = commandToRun;
                TakeOwnership(_commandToRun);
                _trapAbort = trapAbort;
            }

            protected override object SyncExecuteImpl(object runtimeArg)
            {
                try
                {
                    Exception = null;
                    return _commandToRun.SyncExecute(runtimeArg);
                }
                catch (CommandAbortedException e)
                {
                    if (_trapAbort)
                    {
                        Exception = e;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Exception = e;
                }

                return null;
            }

            internal Exception Exception { get; private set; }
            private readonly Command _commandToRun;
            private readonly bool _trapAbort;
        }

        private readonly ErrorTrappingCommand _errorTrappingCommand;
        private readonly Command _uponCompletionCommand;
    }
}
