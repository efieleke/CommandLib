using System;
using System.Collections.Generic;

namespace Sophos.Commands
{
    /// <summary>Represents a collection of <see cref="Command"/> objects that execute in parallel, wrapped in a <see cref="Command"/> object</summary>
    /// <remarks>
    /// The 'runtimeArg' parameter passed to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// will be passed to every command in the collection when it executes.
    /// <para>
    /// Synchronous execution will return null, and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will also be set to null.
    /// </para>
    /// </remarks>
    public class ParallelCommands : AsyncCommand
    {
        /// <summary>
        /// Constructs a ParallelCommands object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="abortUponFailure">
        /// If true, and any <see cref="Command"/> within the collection fails, the rest of the executing commands will immediately be aborted
        /// </param>
        public ParallelCommands(bool abortUponFailure)
            : this(abortUponFailure, null)
        {
        }

        /// <summary>
        /// Constructs a ParallelCommands object
        /// </summary>
        /// <param name="abortUponFailure">
        /// If true, and any command within the collection fails, the rest of the executing commands will immediately be aborted.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public ParallelCommands(bool abortUponFailure, Command owner)
            : base(owner)
        {
            _abortUponFailure = abortUponFailure;

            // We always need at least one command in the collection so that the 
            // finished callback occurs properly even if the user of the class didn't
            // add any commands.
            Add(new DummyCommand());
        }

        /// <summary>Adds a <see cref="Command"/> to the collection to execute.</summary>
        /// <param name="command">The command to add</param>
        /// <remarks>
        /// This object takes ownership of any commands that are added, so the passed command must not already have an owner.
        /// The passed command will be disposed when this ParallelCommands object is disposed.
        /// <para>Behavior is undefined if you add a command while this ParallelCommands object is executing</para>
        /// <para>If multiple commands fail, only the first failure reason is reported via <see cref="ICommandListener"/>.</para>
        /// </remarks>
        public void Add(Command command)
        {
            CheckDisposed();

            if (_abortUponFailure)
            {
                // Because we need to abort running commands in case one of them fails,
                // and we don't want the topmost command to abort as well, we keep these commands 
                // wrapped in topmost AbortSignaledCommand objects. These top level objects will
                // still respond to abort requests to this ParallelCommands object via the
                // 'this' pointer we pass as an argument.
                _commands.Add(CreateAbortLinkedCommand(command));
            }
            else
            {
                TakeOwnership(command);
                _commands.Add(command);
            }
        }

        /// <summary>Empties all commands from the collection.</summary>
        /// <remarks>Behavior is undefined if you call this while this command is executing.</remarks>
        public void Clear()
        {
            CheckDisposed();

            foreach (Command cmd in _commands)
            {
                if (!_abortUponFailure)
                {
                    RelinquishOwnership(cmd);
                }

                cmd.Dispose();
            }

            _commands.Clear();

            // We always need at least one command in the collection so that the 
            // finished callback occurs properly even if the user of the class didn't
            // add any commands.
            Add(new DummyCommand());
        }

        /// <summary>
        /// The commands that have been added to this collection
        /// </summary>
        public IEnumerable<Command> Commands
        {
            get
            {
                if (_commands.Count > 1)
                {
                    if (_abortUponFailure)
                    {
                        List<Command> result = new List<Command>(_commands.Count - 1);

                        for (int i = 1; i < _commands.Count; ++i )
                        {
                            AbortSignaledCommand abortSignaledCmd = (AbortSignaledCommand)_commands[i];
                            result.Add(abortSignaledCmd.CommandToRun);
                        }

                        return result;
                    }

                    return _commands.GetRange(1, _commands.Count - 1);
                }

                return new List<Command>(0);
            }
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the number of commands in the collection, as well as whether abort upon failure is set or not
        /// </returns>
        public override string ExtendedDescription()
        {
            return $"Number of commands: {_commands.Count - 1}; Abort upon failure? {_abortUponFailure}";
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
                    if (_abortUponFailure)
                    {
                        foreach (Command cmd in _commands)
                        {
                            cmd.Wait(); // because these were top level, we must make sure they're really done
                            cmd.Dispose();
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="listener">Not applicable</param>
        /// <param name="runtimeArg">Not applicable</param>
        protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            int startIndex = (_commands.Count == 1 ? 0 : 1);
            Listener eventHandler = new Listener(this, listener);

            for (int i = startIndex; i < _commands.Count; ++i)
            {
                _commands[i].AsyncExecute(eventHandler, runtimeArg);
            }
        }

        private class Listener : ICommandListener
        {
            public Listener(ParallelCommands command, ICommandListener listener)
            {
                _command = command;
                _listener = listener;
                _remaining = command._commands.Count;

                if (_remaining > 1)
                {
                    --_remaining; // we don't run the dummy command unless it's the only one
                }
            }

            public void CommandSucceeded(object result)
            {
                OnCommandFinished();
            }

            public void CommandAborted()
            {
                System.Threading.Interlocked.Increment(ref _abortCount);
                OnCommandFinished();
            }

            public void CommandFailed(Exception exc)
            {
                if (System.Threading.Interlocked.Increment(ref _failCount) == 1)
                {
                    _error = exc;

                    if (_command._abortUponFailure)
                    {
                        foreach (Command cmd in _command._commands)
                        {
                            cmd.Abort();
                        }
                    }
                }

                OnCommandFinished();
            }

            private void OnCommandFinished()
            {
                if (System.Threading.Interlocked.Decrement(ref _remaining) == 0)
                {
                    if (_error != null)
                    {
                        _listener.CommandFailed(_error);
                    }
                    else if (_abortCount > 0)
                    {
                        _listener.CommandAborted();
                    }
                    else
                    {
                        _listener.CommandSucceeded(null);
                    }
                }
            }

            private readonly ICommandListener _listener;
            private readonly ParallelCommands _command;
            private long _failCount;
            private long _abortCount;
            private long _remaining;
            private Exception _error;
        }

        private class DummyCommand : SyncCommand
        {
            public DummyCommand() : base(null) { }

            protected sealed override object SyncExeImpl(object runtimeArg)
            {
                return null;
            }
        }

        private readonly List<Command> _commands = new List<Command>();
	    private readonly bool _abortUponFailure;
    }
}
