using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
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
            this.abortUponFailure = abortUponFailure;

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

            if (abortUponFailure)
            {
                // Because we need to abort running commands in case one of them fails,
                // and we don't want the topmost command to abort as well, we keep these commands 
                // wrapped in topmost AbortEventedCommand objects. These top level objects will
                // still respond to abort requests to this ParallelCommands object via the
                // 'this' pointer we pass as an argument.
                commands.Add(CreateAbortLinkedCommand(command));
            }
            else
            {
                TakeOwnership(command);
                commands.Add(command);
            }
        }

        /// <summary>Empties all commands from the collection.</summary>
        /// <remarks>Behavior is undefined if you call this while this command is executing.</remarks>
        public void Clear()
        {
            CheckDisposed();

            foreach (Command cmd in commands)
            {
                if (!abortUponFailure)
                {
                    RelinquishOwnership(cmd);
                }

                cmd.Dispose();
            }

            commands.Clear();

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
                if (commands.Count > 1)
                {
                    if (abortUponFailure)
                    {
                        List<Command> result = new List<Command>(commands.Count - 1);

                        for (int i = 1; i < commands.Count; ++i )
                        {
                            AbortEventedCommand abortEventedCmd = (AbortEventedCommand)commands[i];
                            result.Add(abortEventedCmd.CommandToRun);
                        }

                        return result;
                    }

                    return commands.GetRange(1, commands.Count - 1);
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
            return String.Format("Number of commands: {0}; Abort upon failure? {1}", commands.Count - 1, abortUponFailure);
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
                    if (abortUponFailure)
                    {
                        foreach (Command cmd in commands)
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
        protected sealed override void AsyncExecuteImpl(ICommandListener listener, Object runtimeArg)
        {
            int startIndex = (commands.Count == 1 ? 0 : 1);
            Listener eventHandler = new Listener(this, listener);

            for (int i = startIndex; i < commands.Count; ++i)
            {
                commands[i].AsyncExecute(eventHandler, runtimeArg);
            }
        }

        private class Listener : ICommandListener
        {
            public Listener(ParallelCommands command, ICommandListener listener)
            {
                this.command = command;
                this.listener = listener;
                remaining = command.commands.Count;

                if (remaining > 1)
                {
                    --remaining; // we don't run the dummy command unless it's the only one
                }
            }

            public void CommandSucceeded(Object result)
            {
                OnCommandFinished();
            }

            public void CommandAborted()
            {
                System.Threading.Interlocked.Increment(ref abortCount);
                OnCommandFinished();
            }

            public void CommandFailed(Exception exc)
            {
                if (System.Threading.Interlocked.Increment(ref failCount) == 1)
                {
                    error = exc;

                    if (command.abortUponFailure)
                    {
                        foreach (Command cmd in command.commands)
                        {
                            cmd.Abort();
                        }
                    }
                }

                OnCommandFinished();
            }

            private void OnCommandFinished()
            {
                if (System.Threading.Interlocked.Decrement(ref remaining) == 0)
                {
                    if (error != null)
                    {
                        listener.CommandFailed(error);
                    }
                    else if (abortCount > 0)
                    {
                        listener.CommandAborted();
                    }
                    else
                    {
                        listener.CommandSucceeded(null);
                    }
                }
            }

            private ICommandListener listener;
            private ParallelCommands command;
            private long failCount = 0;
            private long abortCount = 0;
            private long remaining;
            private Exception error = null;
        }

        private class DummyCommand : SyncCommand
        {
            public DummyCommand() : base(null) { }

            protected sealed override object SyncExeImpl(object runtimeArg)
            {
                return null;
            }
        }

        private System.Collections.Generic.List<Command> commands = new List<Command>();
        bool abortUponFailure;
    }
}
