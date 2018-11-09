using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// SequentialCommands is a <see cref="Command"/> object which contains a collection of commands which are run in sequence.
    /// Each command is run in its most natural form: commands inherited from <see cref="SyncCommand"/> are executed synchronously,
    /// and other commands are run asynchronously (but still in sequence).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The 'runtimeArg' parameter passed to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// is ignored.
    /// </para>
    /// <para>
    /// Synchronous execution will return null, and the 'result'
    /// parameter of <see cref="ICommandListener.CommandSucceeded"/> will be null.
    /// </para>
    /// </remarks>
    public class SequentialCommands : Command
    {
        /// <summary>
        /// Constructs a SequentialCommands object as a top-level <see cref="Command"/>
        /// </summary>
        public SequentialCommands() : this(null)
        {
        }

        /// <summary>
        /// Constructs a SequentialCommands object
        /// </summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public SequentialCommands(Command owner) : base(owner)
        {
        }

        /// <summary>Adds a <see cref="Command"/> to the collection to execute.</summary>
        /// <param name="command">The command to add</param>
        /// <remarks>
        /// This object takes ownership of any commands that are added, so the passed command must not already have an owner.
        /// The passed command will be disposed when this SequentialCommands object is disposed.
        /// <para>Behavior is undefined if you add a command while this SequentialCommands object is executing</para>
        /// </remarks>
        public void Add(Command command)
        {
            CheckDisposed();
            TakeOwnership(command);
            _commands.AddLast(command);
        }

        /// <summary>
        /// Empties all commands from the collection. Behavior is undefined if you call this while this command is executing.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            foreach (Command cmd in _commands)
            {
                RelinquishOwnership(cmd);
                cmd.Dispose();
            }

            _commands.Clear();
        }

        /// <summary>
        /// The commands that have been added to this collection
        /// </summary>
        public IEnumerable<Command> Commands
        {
            get
            {
                return _commands;
            }
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the number of commands in the collection
        /// </returns>
        public override string ExtendedDescription()
        {
            return $"Number of commands: {_commands.Count}";
        }

        /// <inheritdoc />
        public sealed override Task<TResult> AsTask<TResult>(object runtimeArg, Command owner)
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>();

            if (_commands.All(c => c is SyncCommand))
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        SyncExecute(runtimeArg, owner);
                        taskCompletionSource.SetResult(default(TResult));
                    }
                    catch (CommandAbortedException)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                });
            }
            else
            {
                Command cmd = owner == null ? (Command)this : new AbortSignaledCommand(this, owner);

                Task.Factory.StartNew(() => cmd.AsyncExecute(
                    o => taskCompletionSource.SetResult(default(TResult)),
                    () => taskCompletionSource.SetCanceled(),
                    e => taskCompletionSource.SetException(e),
                    runtimeArg));
            }

            return taskCompletionSource.Task;
        }

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            if (_commands.All(c => c is SyncCommand))
            {
                // If every one of the commands is synchronous, we have a special case
                // where we can optimize by not waiting on an event handle to know
                // when we are done.
                foreach (Command cmd in _commands)
                {
                    CheckAbortFlag();
                    cmd.SyncExecute();
                }
            }
            else
            {
                var resetEvent = new ManualResetEvent(false);
                Exception error = null;

                AsyncExecuteImpl(new DelegateCommandListener<object>( 
                    // ReSharper disable once AccessToDisposedClosure
                    o => resetEvent.Set(),
                    // ReSharper disable once AccessToDisposedClosure
                    () => { error = new CommandAbortedException(); resetEvent.Set(); },
                    // ReSharper disable once AccessToDisposedClosure
                    e => { error = e; resetEvent.Set(); }),
                    null);

                resetEvent.WaitOne();
                resetEvent.Dispose();

                if (error != null)
                {
                    throw error;
                }
            }

            return null;
        }
        
        /// <inheritdoc />
        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            LinkedListNode<Command> node = _commands.First;

            if (node == null)
            {
                // No commands in the collection. We must still notify the caller on a separate thread.
                _asyncWrapperCmd = new DelegateCommand<object>(o => o, this);
                _asyncWrapperCmd.AsyncExecute(listener);
                return;
            }

            // We have to execute at least one command asynchronously in order to finish on a different thread.
            if (!_commands.All(c => c is SyncCommand))
            {
                try
                {
                    // ReSharper disable once PossibleNullReferenceException
                    while (node.Value is SyncCommand)
                    {
                        CheckAbortFlag();
                        node.Value.SyncExecute();
                        node = node.Next;
                    }
                }
                catch (Exception e)
                {
                    // need to call back on the listener from a different thread
                    _asyncWrapperCmd = new DelegateCommand<object>(o => throw e, this);
                    _asyncWrapperCmd.AsyncExecute(listener);
                    return;
                }
            }

            // The code above guarantees that when we reach this point, 'node' will not be null.
            _listener.ExternalListener = listener;
            _listener.CurrentNode = node;
            node.Value.AsyncExecute(_listener);
        }

        private class Listener : ICommandListener
        {
            internal LinkedListNode<Command> CurrentNode
            {
                set { _currentNode = value; }
            }

            internal ICommandListener ExternalListener { private get; set; }

            public void CommandSucceeded(object result)
            {
                _currentNode = _currentNode.Next;

                if (_currentNode == null)
                {
                    ExternalListener.CommandSucceeded(null);
                }
                else if (_currentNode.Value.AbortEvent.WaitOne(0))
                {
                    ExternalListener.CommandAborted();
                }
                else if (_currentNode.Value is SyncCommand)
                {
                    try
                    {
                        // Recursive call
                        CommandSucceeded(_currentNode.Value.SyncExecute());
                    }
                    catch (CommandAbortedException)
                    {
                        ExternalListener.CommandAborted();
                    }
                    catch (Exception e)
                    {
                        ExternalListener.CommandFailed(e);
                    }
                }
                else
                {
                    _currentNode.Value.AsyncExecute(this);
                }
            }

            public void CommandAborted()
            {
                ExternalListener.CommandAborted();
            }

            public void CommandFailed(Exception exc)
            {
                ExternalListener.CommandFailed(exc);
            }

            private volatile LinkedListNode<Command> _currentNode;
        }

        private readonly LinkedList<Command> _commands = new LinkedList<Command>();
        private readonly Listener _listener = new Listener();
        private volatile DelegateCommand<object> _asyncWrapperCmd;
    }
}
