using System;

namespace Sophos.Commands
{
    /// <summary>
    /// Implements <see cref="ICommandMonitor"/> by writing diagnostic information to the debug stream.
    /// </summary>
    public class CommandTracer : ICommandMonitor
    {
        /// <summary>
        /// Writes information about the command that is starting to the debug stream
        /// </summary>
        /// <param name="commandInfo">information about the command that is starting</param>
        public void CommandStarting(ICommandInfo commandInfo)
        {
            string parentId = commandInfo.ParentInfo == null ? "none" : commandInfo.ParentInfo.Id.ToString();
            string spaces = new string(' ', commandInfo.Depth);
            string message = $"{spaces}{commandInfo.GetType().FullName}({commandInfo.Id.ToString()}) started. Parent Id: {parentId}";
            PrintMessage(commandInfo, message);
        }

        /// <summary>
        /// Writes information about the command that finished to the debug stream
        /// </summary>
        /// <param name="commandInfo">information about the command is finishing</param>
        /// <param name="exc">If the command did not succeed, this indicates the reason</param>
        public void CommandFinished(ICommandInfo commandInfo, Exception exc)
        {
            string parentId = commandInfo.ParentInfo == null ? "none" : commandInfo.ParentInfo.Id.ToString();
            string spaces = new string(' ', commandInfo.Depth);
            string message;

            switch (exc)
            {
	            case null:
		            message = $"{spaces}{commandInfo.GetType().FullName}({commandInfo.Id.ToString()}) succeeded. Parent Id: {parentId}";
		            break;
	            case CommandAbortedException _:
		            message = $"{spaces}{commandInfo.GetType().FullName}({commandInfo.Id.ToString()}) aborted. Parent Id: {parentId}";
		            break;
	            default:
		            message = $"{spaces}{commandInfo.GetType().FullName}({commandInfo.Id.ToString()}) failed. Parent Id: {parentId}. Reason: {exc.Message}";
		            break;
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

        private static void PrintMessage(ICommandInfo commandInfo, string message)
        {
	        // ReSharper disable once RedundantAssignment
	        string extendedInfo = commandInfo.ExtendedDescription();

	        System.Diagnostics.Debug.Print(
	            string.IsNullOrWhiteSpace(extendedInfo) ? message : $"{message}[{extendedInfo}]");
        }
    }
}
