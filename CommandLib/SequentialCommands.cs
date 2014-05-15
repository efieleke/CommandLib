using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// Represents a collection of <see cref="Command"/> objects that execute in sequence, wrapped in a <see cref="Command"/> object.
    /// </summary>
    /// <remarks>
    /// The 'runtimeArg' parameter passed to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// will be passed to the first command in the collection when it executes. The return value of the first command is passed as
    /// 'runtimeArg' to the next command in the collection, and so on.
    /// <para>
    /// Synchronous execution will return the value the last command in the collection returned, and the 'result'
    /// parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class SequentialCommands : SyncCommand
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
        public SequentialCommands(Command owner)
            : base(owner)
        {
        }

        /// <summary>Adds a <see cref="Command"/> to the collection to execute.</summary>
        /// <param name="command">The command to add</param>
        /// <remarks>
        /// This object takes ownership of any commands that are added, so the passed command must not already have an owner.
        /// The passed command will be disposed when this SequentialCommands object is disposed.
        /// <para>Behavior is undefined if you add a command while this SeqentialCommands object is executing</para>
        /// </remarks>
        public void Add(Command command)
        {
            CheckDisposed();
            TakeOwnership(command);
            commands.AddLast(command);
        }

        /// <summary>
        /// Empties all commands from the collection. Behavior is undefined if you call this while this command is executing.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            foreach (Command cmd in commands)
            {
                RelinquishOwnership(cmd);
                cmd.Dispose();
            }

            commands.Clear();
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the number of commands in the collection
        /// </returns>
        public override string ExtendedDescription()
        {
            return String.Format("Number of commands: {0}", commands.Count);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            foreach (Command cmd in commands)
            {
                CheckAbortFlag();
                runtimeArg = cmd.SyncExecute(runtimeArg);
            }

            return runtimeArg;
        }

        private System.Collections.Generic.LinkedList<Command> commands = new LinkedList<Command>();
    }
}
