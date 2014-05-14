using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// A <see cref="Command"/> which is a simple holder for another <see cref="Command"/> object. It's only useful purpose is to aid in preventing
    /// many temporary <see cref="Command"/> objects from collecting in memory during the lifetime of their owner.
    /// </summary>
    /// <remarks>
    /// If you find that you are generating a temporary <see cref="Command"/> object within the execution method of an owning
    /// <see cref="Command"/>, it's best to not specify the creator as the owner of this temporary command. Owned commands are
    /// not disposed until the owner is disposed, so if the owner is executed many times before it is disposed,
    /// it's possible for resource usage to grow unbounded. The better approach is to pass this temporary command
    /// to a VariableCommand object, which would be a member variable of the owner. Assigning to the <see cref="CommandToRun"/>
    /// property will take care of disposing any previously assigned command.
    /// <para>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required as the underlying command to run.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns,
    /// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public sealed class VariableCommand : CommandLib.Command
    {
        /// <summary>
        /// Constructs a VariableCommand object
        /// </summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public VariableCommand(CommandLib.Command owner)
            : base(owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("Variable command objects offer no value if they are not owned.");
            }
        }

        /// <summary>The underlying command to run.</summary>
        /// <remarks>
        /// <para>
        /// This object takes ownership of any command assigned to this property, so the passed command must not already have
        /// an owner. The command to run will be disposed when this VariableCommand object is disposed.
        /// </para>
        /// <para>
        /// It is acceptable to assign null, but a runtime error will occur if this is null and this VariableCommand instance
        /// is executed.
        /// </para>
        /// <para>Behavior is undefined if this property is changed while this command is executing.</para>
        /// </remarks>
        public CommandLib.Command CommandToRun
        {
            get
            {
                CheckDisposed();
                return commandToRun;
            }
            set
            {
                CheckDisposed();

                if (commandToRun == value)
                {
                    return;
                }

                if (commandToRun != null)
                {
                    RelinquishOwnership(commandToRun);
                    commandToRun.Dispose();
                }

                if (value != null)
                {
                    TakeOwnership(value);
                }

                commandToRun = value;
            }
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            return commandToRun.SyncExecute(runtimeArg);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="listener">Not applicable</param>
        /// <param name="runtimeArg">Not applicable</param>
        protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            commandToRun.AsyncExecute(listener, runtimeArg);
        }

        private CommandLib.Command commandToRun = null;
    }
}
