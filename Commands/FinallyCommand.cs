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
    /// should be of the same type required as the underlying command to run.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns,
    /// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class FinallyCommand : Command
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
            _commandToRun = commandToRun;
            _uponCompletionCommand = uponCompletionCommand;
            _evenUponAbort = evenUponAbort;
            TakeOwnership(_commandToRun);

            if (uponCompletionCommand.Parent != null)
            {
                throw new ArgumentException(nameof(uponCompletionCommand), $"{nameof(uponCompletionCommand)} must not have an owner");
            }

            if (!evenUponAbort)
            {
                TakeOwnership(_uponCompletionCommand);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_evenUponAbort)
            {
                _uponCompletionCommand.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override bool IsNaturallySynchronous()
        {
            return _commandToRun.IsNaturallySynchronous();
        }

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            object result;

            try
            {
                result = _commandToRun.SyncExecute(runtimeArg);
            }
            catch (CommandAbortedException)
            {
                if (_evenUponAbort)
                {
                    _uponCompletionCommand.SyncExecute();
                }

                throw;
            }
            catch (Exception e)
            {
                try
                {
                    _uponCompletionCommand.SyncExecute();
                }
                catch (Exception exception)
                {
                    throw new AggregateException(e, exception);
                }

                throw;
            }

            _uponCompletionCommand.SyncExecute();
            return result;
        }

        /// <inheritdoc />
        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            _commandToRun.AsyncExecute(new Listener(listener, this), runtimeArg);
        }

        private class Listener : ICommandListener
        {
            internal Listener(ICommandListener listener, FinallyCommand finallyCommand)
            {
                _listener = listener;
                _finallyCommand = finallyCommand;
            }

            public void CommandSucceeded(object result)
            {
                if (_finallyCommand._uponCompletionCommand.IsNaturallySynchronous())
                {
                    try
                    {
                        _finallyCommand._uponCompletionCommand.SyncExecute();
                        _listener.CommandSucceeded(result);
                    }
                    catch (CommandAbortedException)
                    {
                        _listener.CommandAborted();
                    }
                    catch (Exception e)
                    {
                        _listener.CommandFailed(e);
                    }
                }
                else
                {
                    _finallyCommand._uponCompletionCommand.AsyncExecute(
                        o => _listener.CommandSucceeded(result),
                        () => _listener.CommandAborted(),
                        e => _listener.CommandFailed(e));
                }
            }

            public void CommandAborted()
            {
                if (_finallyCommand._evenUponAbort)
                {
                    if (_finallyCommand._uponCompletionCommand.IsNaturallySynchronous())
                    {
                        try
                        {
                            _finallyCommand._uponCompletionCommand.SyncExecute();
                            _listener.CommandAborted();
                        }
                        catch (Exception e)
                        {
                            _listener.CommandFailed(e);
                        }
                    }
                    else
                    {
                        _finallyCommand._uponCompletionCommand.AsyncExecute(
                            o => _listener.CommandAborted(),
                            () => _listener.CommandFailed(new CommandAbortedException()), // this would be strange
                            e => _listener.CommandFailed(e));
                    }
                }
                else
                {
                    _listener.CommandAborted();
                }
            }

            public void CommandFailed(Exception exc)
            {
                if (_finallyCommand._uponCompletionCommand.IsNaturallySynchronous())
                {
                    try
                    {
                        _finallyCommand._uponCompletionCommand.SyncExecute();
                        _listener.CommandFailed(exc);
                    }
                    catch (Exception e)
                    {
                        _listener.CommandFailed(new AggregateException(exc, e));
                    }
                }
                else
                {
                    _finallyCommand._uponCompletionCommand.AsyncExecute(
                        o => _listener.CommandFailed(exc),
                        () => _listener.CommandFailed(new AggregateException(exc, new CommandAbortedException())),
                        e => _listener.CommandFailed(new AggregateException(exc, e)));
                }
            }

            private readonly ICommandListener _listener;
            private readonly FinallyCommand _finallyCommand;
        }

        private readonly Command _commandToRun;
        private readonly Command _uponCompletionCommand;
        private readonly bool _evenUponAbort;
    }
}
