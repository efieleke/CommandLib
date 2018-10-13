using System;

namespace Sophos.Commands
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
        /// <param name="commandInfo">information about the command that is starting</param>
        public void CommandStarting(ICommandInfo commandInfo)
        {
            String parentId = commandInfo.ParentInfo == null ? "none" : commandInfo.ParentInfo.Id.ToString();
            String spaces = new String(' ', commandInfo.Depth);
            String message = String.Format("{0}{1}({2}) started. Parent Id: {3}", spaces, commandInfo.GetType().FullName, commandInfo.Id.ToString(), parentId);
            PrintMessage(commandInfo, message);
        }

        /// <summary>
        /// Writes information about the command that finished to the debug stream
        /// </summary>
        /// <param name="commandInfo">information about the command is finishing</param>
        /// <param name="exc">If the command did not succeed, this indicates the reason</param>
        public void CommandFinished(ICommandInfo commandInfo, Exception exc)
        {
            String parentId = commandInfo.ParentInfo == null ? "none" : commandInfo.ParentInfo.Id.ToString();
            String spaces = new String(' ', commandInfo.Depth);
            String message;

            if (exc == null)
            {
                message = String.Format("{0}{1}({2}) succeeded. Parent Id: {3}", spaces, commandInfo.GetType().FullName, commandInfo.Id.ToString(), parentId);
            }
            else if (exc is CommandAbortedException)
            {
                message = String.Format("{0}{1}({2}) aborted. Parent Id: {3}", spaces, commandInfo.GetType().FullName, commandInfo.Id.ToString(), parentId);
            }
            else
            {
                message = String.Format("{0}{1}({2}) failed. Parent Id: {3}. Reason: {4}", spaces, commandInfo.GetType().FullName, commandInfo.Id.ToString(), parentId, exc.Message);
            }

            PrintMessage(commandInfo, message);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// The finalizer
        /// </summary>
        ~CommandTracer()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
        }

        static private void PrintMessage(ICommandInfo commandInfo, String message)
        {
            String extendedInfo = commandInfo.ExtendedDescription();

            if (!String.IsNullOrWhiteSpace(extendedInfo))
            {
                message += " [" + extendedInfo + "]";
            }

            System.Diagnostics.Debug.Print(message);
        }
    }
}
