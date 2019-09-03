﻿using System;

namespace Sophos.Commands
{
    /// <summary>
    /// A <see cref="Command"/> wrapper that, in addition to responding to normal <see cref="Command.Abort()"/> requests, also aborts in response to either
    /// 1) a request to abort a different, specified <see cref="Command"/> instance, or 2) the signaling of a specified wait handle
    /// (typically an event). 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required as the underlying command to run.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns, and the
    /// 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public sealed class AbortSignaledCommand : SyncCommand
    {
        /// <summary>
        /// Constructs an AbortSignaledCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have an owner.
        /// The passed command will be disposed when this AbortSignaledCommand object is disposed.
        /// </param>
        /// <param name="abortEvent">
        /// When signaled, the command to run will be aborted. This object does not take ownership of this parameter.
        /// </param>
        public AbortSignaledCommand(Command commandToRun, System.Threading.WaitHandle abortEvent)
            : base(null)
        {
            _commandToRun = commandToRun;
            _abortEvent = abortEvent;
            TakeOwnership(commandToRun);
        }

        /// <summary>
        /// Constructs an AbortSignaledCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="commandToRun">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have an owner.
        /// The passed command will be disposed when this AbortSignaledCommand object is disposed.
        /// </param>
        /// <param name="commandToWatch">
        /// When this 'commandToWatch' is aborted, the command to run will also be aborted.
        /// </param>
        public AbortSignaledCommand(Command commandToRun, Command commandToWatch)
            : base(null)
        {
            _commandToRun = commandToRun;
            _commandToWatch = commandToWatch;
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
                return _commandToWatch;
            }
        }

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            _commandToRun.AsyncExecute(new Listener(this), runtimeArg);
            int waitResult = System.Threading.WaitHandle.WaitAny(new[] { _commandToRun.DoneEvent, ExternalAbortEvent });

            if (waitResult == 1)
            {
                AbortChildCommand(_commandToRun);
            }

            _commandToRun.Wait();

            if (_lastException != null)
            {
                throw _lastException;
            }

            return _result;
        }

        private System.Threading.WaitHandle ExternalAbortEvent
        {
            get { return _abortEvent ?? _commandToWatch.AbortEvent; }
        }

        private class Listener : ICommandListener
        {
            public Listener(AbortSignaledCommand command)
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

            private readonly AbortSignaledCommand _command;
        }

        private readonly Command _commandToRun;
        private readonly System.Threading.WaitHandle _abortEvent;
        private readonly Command _commandToWatch;
        private volatile Exception _lastException;
        private volatile object _result;
    }
}