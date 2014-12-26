using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// Dispatches <see cref="Command"/> objects to a pool for execution.
    /// </summary>
    /// <remarks>
    /// This class can be useful when commands are dynamically generated at runtime, and must be dynamically executed upon generation.
    /// (for example, asynchronous handling of requests sent over a data stream).
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
            public CommandFinishedEventArgs(Command command, Object result, Exception exc)
            {
                this.command = command;
                this.result = result;
                this.exc = exc;
            }

            /// <summary>
            /// The command that finished execution.
            /// </summary>
            public Command Cmd { get { return command; } }

            /// <summary>
            /// The result of the execution, if successful. Otherwise this will always be null. The actual content of this value is defined by the concrete <see cref="Command"/>.
            /// </summary>
            public Object Result { get { return result; } }

            /// <summary>
            /// This will be null if the command completed successfully. If the command was aborted, this will be a <see cref="CommandAbortedException"/>.
            /// Otherwise, this will indicate the reason for failure.
            /// </summary>
            public Exception Error { get { return exc; } }

            private Command command;
            private Object result;
            private Exception exc;
        }

        /// <summary>
        /// Defines the event callback interface for dispatched commands that finish execution.
        /// </summary>
        /// <param name="sender">The object that raised this event</param>
        /// <param name="e">Information about the finished command</param>
        public delegate void DispatchedCommandFinished(Object sender, CommandFinishedEventArgs e);

        /// <summary>
        /// Event router for command finished events.
        /// </summary>
        /// <seealso cref="DispatchedCommandFinished"/>
        public event DispatchedCommandFinished CommandFinishedEvent;

        /// <summary>
        /// Constructs a CommandDispatcher object
        /// </summary>
        /// <param name="poolSize">The maximum number of commands that can be executed concurrently by this dispatcher.</param>
        public CommandDispatcher(int poolSize)
        {
            if (poolSize <= 0)
            {
                throw new ArgumentException("poolSize must be greater than 0");
            }

            this.poolSize = poolSize;
        }

        /// <summary>
        /// If there is room in the pool, asynchronously executes the command immediately. Otherwise, places the command in a queue for processing when room in the pool becomes available.
        /// </summary>
        /// <param name="command">
        /// The command to execute as soon as there is room in the pool. This object will assume responsibility for disposing of this command. The command must be top-level
        /// (that is, it must have no parent).
        /// <para>
        /// Note that it will cause undefined behavior to dispatch a <see cref="Command"/> object that is currently executing, or that has already been dispatched but has not yet executed.
        /// </para>
        /// </param>
        /// <remarks>
        /// When the command evenutally finishes execution, the <see cref="CommandFinishedEvent"/> subscribers will be notified on a different thread.
        /// </remarks>
        public void Dispatch(Command command)
        {
            if (command.ParentInfo != null)
            {
                throw new ArgumentException("Only top-level commands can be dispatched");
            }

            nothingToDoEvent.Reset();

            lock (criticalSection)
            {
                foreach(Command cmd in finishedCommands)
                {
                    cmd.Dispose();
                }

                finishedCommands.Clear();

                if (runningCommands.Count() == poolSize)
                {
                    commandBacklog.Enqueue(command);
                }
                else
                {
                    runningCommands.Add(command);
                    command.AsyncExecute(new Listener(this, command));
                }
            }
        }

        /// <summary>
        /// Aborts all dispatched commands, and empties the queue of not yet executed commands.
        /// </summary>
        public void Abort()
        {
            lock(criticalSection)
            {
                foreach(Command cmd in commandBacklog)
                {
                    cmd.Dispose();
                }

                commandBacklog.Clear();

                foreach(Command cmd in runningCommands)
                {
                    cmd.Abort();
                }
            }
        }

        /// <summary>
        /// Waits for all dispatched commands to finish execution
        /// </summary>
        public void Wait()
        {
            nothingToDoEvent.WaitOne();
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
        /// Derived implemenations should override if they have work to do when disposing
        /// </summary>
        /// <param name="disposing">True if this was called as a result of a call to Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Wait();

                    foreach (Command cmd in finishedCommands)
                    {
                        cmd.Dispose();
                    }

                    nothingToDoEvent.Dispose();
                }

                disposed = true;
            }
        }

        private void OnCommandFinished(Command command, Object result, Exception exc)
        {
            if (CommandFinishedEvent != null)
            {
                CommandFinishedEvent(this, new CommandFinishedEventArgs(command, result, exc));
            }

            lock (criticalSection)
            {
                runningCommands.Remove(command);

                // We cannot dispose of this command here, because it's not quite done executing yet.
                finishedCommands.AddLast(command);

                if (commandBacklog.Count() == 0)
                {
                    if (runningCommands.Count == 0)
                    {
                        nothingToDoEvent.Set();
                    }
                }
                else
                {
                    Command nextInLine = commandBacklog.Dequeue();
                    runningCommands.Add(nextInLine);

                    try
                    {
                        nextInLine.AsyncExecute(new Listener(this, nextInLine));
                    }
                    catch (Exception e)
                    {
                        OnCommandFinished(nextInLine, null, e);
                    }
                }
            }
        }

        private class Listener : ICommandListener
        {
            internal Listener(CommandDispatcher dispatcher, Command command)
            {
                this.dispatcher = dispatcher;
                this.command = command;
            }

            public void CommandSucceeded(object result)
            {
                dispatcher.OnCommandFinished(command, result, null);
            }

            public void CommandAborted()
            {
                dispatcher.OnCommandFinished(command, null, new CommandAbortedException());
            }

            public void CommandFailed(Exception exc)
            {
                dispatcher.OnCommandFinished(command, null, exc);
            }

            private Command command;
            private CommandDispatcher dispatcher;
        }

        private readonly int poolSize;
        private List<Command> runningCommands = new List<Command>();
        private Queue<Command> commandBacklog = new Queue<Command>();
        private LinkedList<Command> finishedCommands = new LinkedList<Command>();
        private Object criticalSection = new Object();
        private System.Threading.ManualResetEvent nothingToDoEvent = new System.Threading.ManualResetEvent(true);
        private bool disposed = false;
    }
}
