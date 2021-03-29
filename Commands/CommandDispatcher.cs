using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
	/// <summary>
	/// Dispatches <see cref="Command"/> objects for asynchronous execution. The dispatcher assumes
	/// responsibility for disposing dispatched commands.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class can be useful when commands are dynamically generated at runtime, and must be dynamically executed upon generation
	/// (for example, asynchronous handling of requests sent over a data stream).
	/// </para>
	/// <para>
	/// Users of the class should remember to call Dispose() when they are done with this object. That will wait until all dispatched
	/// commands finish execution, and also dispose the dispatched <see cref="Command"/> objects. For a faster shutdown, you may wish to call
	/// <see cref="CommandDispatcher.Abort()"/> before disposing the dispatcher.
	/// </para>
	/// </remarks>
	public class CommandDispatcher : IDisposable
    {
        /// <summary>
        /// Defines the event callback parameter for dispatched commands that finish execution.
        /// </summary>
        public class CommandFinishedEventArgs : EventArgs
        {
            /// <summary>
            ///  Constructor for the event args
            /// </summary>
            /// <param name="command">The command that finished execution</param>
            /// <param name="result">The result of the execution, if successful. Otherwise this will always be null. The actual content of this value is defined by the concrete <see cref="Command"/>.</param>
            /// <param name="exc">
            /// This will be null if the command completed successfully. If the command was aborted, this will be a <see cref="CommandAbortedException"/>.
            /// Otherwise, this will indicate the reason for failure.
            /// </param>
            public CommandFinishedEventArgs(Command command, object result, Exception exc)
            {
                Cmd = command;
                Result = result;
                Error = exc;
            }

            /// <summary>
            /// The command that finished execution.
            /// </summary>
            public Command Cmd { get; }

	        /// <summary>
            /// The result of the execution, if successful. Otherwise this will always be null. The actual content of this value is defined by the concrete <see cref="Command"/>.
            /// </summary>
            public object Result { get; }

	        /// <summary>
            /// This will be null if the command completed successfully. If the command was aborted, this will be a <see cref="CommandAbortedException"/>.
            /// Otherwise, this will indicate the reason for failure.
            /// </summary>
            public Exception Error { get; }
        }

        /// <summary>
        /// Defines the event callback interface for dispatched commands that finish execution.
        /// </summary>
        /// <param name="sender">The object that raised this event</param>
        /// <param name="e">Information about the finished command</param>
        public delegate void DispatchedCommandFinished(object sender, CommandFinishedEventArgs e);

        /// <summary>
        /// Event router for command finished events.
        /// </summary>
        /// <seealso cref="DispatchedCommandFinished"/>
        public event DispatchedCommandFinished CommandFinishedEvent;

        /// <summary>
        /// Constructs a CommandDispatcher object
        /// </summary>
        /// <param name="maxConcurrent">
        /// The maximum number of commands that can be executed concurrently by this dispatcher. If this
        /// limit is reached, commands will be queued and only executed when enough prior dispatched commands
        /// finish execution.
        /// </param>
        public CommandDispatcher(int maxConcurrent)
        {
            if (maxConcurrent <= 0)
            {
                throw new ArgumentException($"{nameof(maxConcurrent)} must be greater than 0", nameof(maxConcurrent));
            }

            _maxConcurrent = maxConcurrent;
        }

        /// <summary>
        /// If fewer commands than the maximum concurrent are currently executing, asynchronously executes the command immediately. Otherwise, places the command in a
        /// queue for processing when room becomes available.
        /// </summary>
        /// <param name="command">
        /// The command to execute as soon as there is room. This object will assume responsibility for disposing of
        /// this command. The command must be top-level (that is, it must have no parent).
        /// <para>
        /// Note that it will cause undefined behavior to dispatch a <see cref="Command"/> object that is currently executing,
        /// or that has already been dispatched but has not yet executed.
        /// </para>
        /// </param>
        /// <remarks>
        /// When the command eventually finishes execution, the <see cref="CommandFinishedEvent"/> subscribers will be notified on
        /// a different thread.
        /// </remarks>
        public void Dispatch(Command command)
        {
            using (var counter = new ReentrancyCounter(_reentrantCount))
            {
                if (counter.Count > 1)
                {
                    Task.Run(() => Dispatch(command));
                    return;
                }

                if (command.ParentInfo != null)
                {
                    throw new ArgumentException("Only top-level commands can be dispatched");
                }

                bool shouldExecute = false;
                lock (_criticalSection)
                {
                    _nothingToDoEvent.Reset();

                    foreach (Command cmd in _finishedCommands)
                    {
                        cmd.Dispose();
                    }

                    _finishedCommands.Clear();

                    if (_runningCommands.Count == _maxConcurrent)
                    {
                        _commandBacklog.Enqueue(command);
                    }
                    else
                    {
                        _runningCommands.Add(command);
                        shouldExecute = true;
                    }
                }

                if (shouldExecute)
                {
                    command.AsyncExecute(new Listener(this, command));
                }
            }
        }

        /// <summary>
        /// Aborts all dispatched commands, and empties the queue of not-yet executed commands.
        /// </summary>
        public void Abort()
        {
            using (var counter = new ReentrancyCounter(_reentrantCount))
            {
                if (counter.Count > 1)
                {
                    Task.Run(() => Abort());
                    return;
                }

                lock (_criticalSection)
                {
                    foreach (Command cmd in _commandBacklog)
                    {
                        cmd.Dispose();
                    }

                    _commandBacklog.Clear();

                    foreach (Command cmd in _runningCommands)
                    {
                        cmd.Abort();
                    }
                }
            }
        }

        /// <summary>
        /// Waits for all dispatched commands to finish execution
        /// </summary>
        public void Wait()
        {
            _nothingToDoEvent.WaitOne();
        }

        /// <summary>
        /// Exact same effect as calling <see cref="Abort"/> followed immediately by a call to <see cref="Wait"/>.
        /// </summary>
        public void AbortAndWait()
        {
            Abort();
            Wait();
        }

        /// <summary>
        /// Implementation of IDisposable.Dispose()
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The finalizer
        /// </summary>
        ~CommandDispatcher()
        {
            Dispose(false);
        }

        /// <summary>
        /// Derived implementations should override if they have work to do when disposing
        /// </summary>
        /// <param name="disposing">True if this was called as a result of a call to Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Wait();

                    foreach (Command cmd in _finishedCommands)
                    {
                        cmd.Dispose();
                    }

                    _nothingToDoEvent.Dispose();
                }

                _disposed = true;
            }
        }

        private void OnCommandFinished(Command command, object result, Exception exc)
        {
            using (var counter = new ReentrancyCounter(_reentrantCount))
            {
                if (counter.Count > 1)
                {
                    Task.Run(() => OnCommandFinished(command, result, exc));
                    return;
                }

                CommandFinishedEvent?.Invoke(this, new CommandFinishedEventArgs(command, result, exc));
                bool shouldReleaseLock = false;

                try
                {
                    Monitor.Enter(_criticalSection, ref shouldReleaseLock);
                    _runningCommands.Remove(command);

                    // We cannot dispose of this command here, because it's not quite done executing yet.
                    _finishedCommands.AddLast(command);

                    if (_commandBacklog.Count == 0)
                    {
                        if (_runningCommands.Count == 0)
                        {
                            Monitor.Exit(_criticalSection);
                            shouldReleaseLock = false;
                            _nothingToDoEvent.Set();
                        }
                    }
                    else
                    {
                        Command nextInLine = _commandBacklog.Dequeue();
                        _runningCommands.Add(nextInLine);
                        Monitor.Exit(_criticalSection);
                        shouldReleaseLock = false;
                        nextInLine.AsyncExecute(new Listener(this, nextInLine));
                    }
                }
                finally
                {
                    if (shouldReleaseLock)
                    {
                        Monitor.Exit(_criticalSection);
                    }
                }
            }
        }

        private class Listener : ICommandListener
        {
            internal Listener(CommandDispatcher dispatcher, Command command)
            {
                _dispatcher = dispatcher;
                _command = command;
            }

            public void CommandSucceeded(object result)
            {
                _dispatcher.OnCommandFinished(_command, result, null);
            }

            public void CommandAborted()
            {
                _dispatcher.OnCommandFinished(_command, null, new CommandAbortedException());
            }

            public void CommandFailed(Exception exc)
            {
                _dispatcher.OnCommandFinished(_command, null, exc);
            }

            private readonly Command _command;
            private readonly CommandDispatcher _dispatcher;
        }

        private class ReentrancyCounter : IDisposable
        {
            internal ReentrancyCounter(ThreadLocal<int> counter)
            {
                _counter = counter;
                ++counter.Value;
            }

            internal int Count => _counter.Value;

            public void Dispose()
            {
                --_counter.Value;
            }

            private readonly ThreadLocal<int> _counter;
        }

        private readonly int _maxConcurrent;
        private readonly List<Command> _runningCommands = new List<Command>();
        private readonly Queue<Command> _commandBacklog = new Queue<Command>();
        private readonly LinkedList<Command> _finishedCommands = new LinkedList<Command>();
        private readonly object _criticalSection = new object();
        private readonly ManualResetEvent _nothingToDoEvent = new ManualResetEvent(true);
        private readonly ThreadLocal<int> _reentrantCount = new ThreadLocal<int>(() => 0);
        private volatile bool _disposed;
    }
}
