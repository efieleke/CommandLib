using System;
using System.Collections.Generic;
using System.Linq;

namespace Sophos.Commands
{
	/// <summary>Represents a collection of <see cref="Command"/> objects that execute in parallel, wrapped in a <see cref="Command"/> object</summary>
	/// <remarks>
	/// <para>
	/// The 'runtimeArg' parameter passed to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
	/// is ignored.
	/// </para>
	/// <para>
	/// Synchronous execution will return null, and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will also be set to null.
	/// </para>
	/// </remarks>
	public class ParallelCommands : AsyncCommand
    {
        /// <summary>
        /// Describes behaviors for a <see cref="ParallelCommands"/> instance
        /// </summary>
        [Flags]
        public enum Behavior
        {
            /// <summary>
            /// If set, any sub-command run via this instance that fails will cause an abort request to be issued
            /// to the still-running commands.
            /// </summary>
            AbortUponFailure = 1,

            /// <summary>
            /// If set, and errors occur, the exception raised will be an AggregateException, containing the exceptions
            /// for every failed sub-command. Otherwise the exception raised will be the one raised by the first
            /// command to fail (if any).
            /// </summary>
            AggregateErrors = 2
        }

        /// <summary>
        /// Constructs a ParallelCommands object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="behavior">
        /// Flags describing certain behaviors of this instance
        /// </param>
        public ParallelCommands(Behavior behavior)
            : this(behavior, null)
        {
        }

        /// <summary>
        /// Constructs a ParallelCommands object
        /// </summary>
        /// <param name="behavior">
        /// Flags describing certain behaviors of this instance
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public ParallelCommands(Behavior behavior, Command owner)
            : base(owner)
        {
            _behavior = behavior;
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

            if (_behavior.HasFlag(Behavior.AbortUponFailure))
            {
                // Because we need to abort running commands in case one of them fails,
                // and we don't want the topmost command to abort as well, we keep these commands 
                // wrapped in topmost AbortSignaledCommand objects. These top level objects will
                // still respond to abort requests to this ParallelCommands object via the
                // 'this' pointer we pass as an argument.
                _commands.Add(CreateAbortSignaledCommand(command));
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
                if (!_behavior.HasFlag(Behavior.AbortUponFailure))
                {
                    RelinquishOwnership(cmd);
                }

                cmd.Dispose();
            }

            _commands.Clear();
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the number of commands in the collection, as well as whether abort upon failure is set or not
        /// </returns>
        public override string ExtendedDescription()
        {
            return $"Number of commands: {_commands.Count}; Flags: {_behavior}";
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
                    if (_behavior.HasFlag(Behavior.AbortUponFailure))
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
            if (_commands.Count == 0)
            {
                var dummyCmd = new AbortSignaledCommand(new DelegateCommand<object>(o => null), this);
                dummyCmd.AsyncExecute(listener);
            }
            else
            {
                Listener eventHandler = new Listener(this, listener);

                foreach (var command in _commands)
                {
                    command.AsyncExecute(eventHandler);
                }
            }
        }

        private class Listener : ICommandListener
        {
            public Listener(ParallelCommands command, ICommandListener listener)
            {
                _command = command;
                _listener = listener;
                _remaining = command._commands.Count;
                _errors = new Exception[command._commands.Count];
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
                int priorCount = System.Threading.Interlocked.Increment(ref _failCount) - 1;
                _errors[priorCount] = exc;

                if (priorCount == 0 && _command._behavior.HasFlag(Behavior.AbortUponFailure))
                {
                    foreach (Command cmd in _command._commands)
                    {
                        cmd.Abort();
                    }
                }

                OnCommandFinished();
            }

            private void OnCommandFinished()
            {
                if (System.Threading.Interlocked.Decrement(ref _remaining) == 0)
                {
                    if (_failCount > 0)
                    {
                        if (_command._behavior.HasFlag(Behavior.AggregateErrors))
                        {
                            // Return a list of errors, including the aborts.
                            for (int i = 0; i < _abortCount; ++i)
                            {
                                _errors[_failCount + i] = new CommandAbortedException();
                            }

                            // There may have been some successes, which is why Take() is used instead
                            // of returning all of _errors (which may end with null elements).
                            _listener.CommandFailed(new AggregateException(_errors.Take(_failCount + _abortCount)));
                        }
                        else
                        {
                            _listener.CommandFailed(_errors[0]);
                        }
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
            private volatile int _failCount;
            private volatile int _abortCount;
            private volatile int _remaining;
            private readonly Exception[] _errors; // array instead of List for thread safety
        }

        private readonly List<Command> _commands = new List<Command>();
	    private readonly Behavior _behavior;
    }
}
