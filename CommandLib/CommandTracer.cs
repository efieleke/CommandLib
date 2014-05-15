using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// Implements <see cref="ICommandMonitor"/> by writing diagnostic information to the debug stream.
    /// </summary>
    public class CommandTracer : ICommandMonitor
    {
        /// <summary>
        /// Constructs a CommandTracer object
        /// </summary>
        public CommandTracer()
        {
        }

        /// <summary>
        /// Writes information about the command that is starting to the debug stream
        /// </summary>
        /// <param name="command">The command that is starting</param>
        public void CommandStarting(Command command)
        {
            String parentId = command.Parent == null ? "none" : command.Parent.Id.ToString();
            String spaces = new String(' ', command.Depth);
            System.Diagnostics.Debug.Print("{0}Command {1}({2}) started. Parent Id: {3}", spaces, command.GetType().FullName, command.Id.ToString(), parentId);
        }

        /// <summary>
        /// Writes information about the command that finished to the debug stream
        /// </summary>
        /// <param name="command">The command that finished</param>
        /// <param name="exc">If the command did not succeed, this indicates the reason</param>
        public void CommandFinished(Command command, Exception exc)
        {
            String parentId = command.Parent == null ? "none" : command.Parent.Id.ToString();
            String spaces = new String(' ', command.Depth);

            if (exc == null)
            {
                System.Diagnostics.Debug.Print("{0}Command {1}({2}) succeeded. Parent Id: {3}", spaces, command.GetType().FullName, command.Id.ToString(), parentId);
            }
            else if (exc is CommandAbortedException)
            {
                System.Diagnostics.Debug.Print("{0}Command {1}({2}) aborted. Parent Id: {3}", spaces, command.GetType().FullName, command.Id.ToString(), parentId);
            }
            else
            {
                System.Diagnostics.Debug.Print("{0}Command {1}({2}) failed. Parent Id: {3}. Reason: {4}", spaces, command.GetType().FullName, command.Id.ToString(), parentId, exc.Message);
            }
        }
    }
}
