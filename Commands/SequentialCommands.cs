using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Sophos.Commands
{
    /// <summary>
    /// SequentialCommands is a <see cref="Command"/> object which contains a collection of commands which are run in sequence.
    /// When possible, each command is run in its most natural form: commands inherited from <see cref="SyncCommand"/> are executed synchronously,
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

        /// <inheritdoc />
        public sealed override bool IsNaturallySynchronous()
        {
            return _commands.All(c => c.IsNaturallySynchronous());
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
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            LinkedListNode<Command> node = _commands.First;

            while (node != null && node.Value.IsNaturallySynchronous())
            {
                CheckAbortFlag();
                node.Value.SyncExecute();
                node = node.Next;
            }

            if (node != null)
            {
                // We encountered a command that is asynchronous in nature.
                CheckAbortFlag();
                var resetEvent = new ManualResetEvent(false);
                Exception error = null;

                DoAsyncExecute(
                    new DelegateCommandListener<object>( 
                        // ReSharper disable once AccessToDisposedClosure
                        o => resetEvent.Set(),
                        // ReSharper disable once AccessToDisposedClosure
                        () => { error = new CommandAbortedException(); resetEvent.Set(); },
                        // ReSharper disable once AccessToDisposedClosure
                        e => { error = e; resetEvent.Set(); }),
                    node);

                resetEvent.WaitOne();
                resetEvent.Dispose();

                if (error != null)
                {
                    ExceptionDispatchInfo.Capture(error).Throw();
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

            // We execute the first command asynchronously regardless of whether it is naturally asynchronous, because this call must not block.
            DoAsyncExecute(listener, node);
        }

        private void DoAsyncExecute(ICommandListener listener, LinkedListNode<Command> node)
        {
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
                else if (_currentNode.Value.IsNaturallySynchronous())
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
