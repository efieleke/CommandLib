using System;

namespace Sophos.Commands
{
    /// <summary>
    /// Implements <see cref="ICommandMonitor"/> by writing diagnostic information to a log file that can be parsed
    /// and dynamically displayed by the included CommandLogViewer application.
    /// </summary>
    public class CommandLogger : ICommandMonitor
    {
        /// <summary>
        /// Constructs a CommandLogger object
        /// </summary>
        /// <param name="filename">Name of log file. Will be overwritten if it exists</param>
        public CommandLogger(string filename)
        {
            System.IO.FileStream stream = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
	        _writer = new System.IO.StreamWriter(stream) {AutoFlush = true};
        }

        /// <summary>
        /// Logs command starting info to file
        /// </summary>
        /// <param name="commandInfo">Information about the command that is starting execution</param>
        public void CommandStarting(ICommandInfo commandInfo)
        {
            // Changing the format of this output will break the CommandLogViewer app.
            string header = FormHeader(commandInfo, "Starting");
            WriteMessage(commandInfo, header);
        }

        /// <summary>
        /// Logs command finished info to file
        /// </summary>
        /// <param name="commandInfo">Information about the command that is finishing</param>
        /// <param name="exc">Will be null if the command succeeded. Otherwise a CommandAbortedException or some other Exception type</param>
        public void CommandFinished(ICommandInfo commandInfo, Exception exc)
        {
            // Changing the format of this output will break the CommandLogViewer app.
            string message;

            switch (exc)
            {
	            case null:
		            message = FormHeader(commandInfo, "Completed");
		            break;
	            case CommandAbortedException _:
		            message = FormHeader(commandInfo, "Aborted");
		            break;
	            default:
		            message = FormHeader(commandInfo, "Failed");
		            message = $"{message} Reason: {exc.Message}";
		            break;
            }

            WriteMessage(commandInfo, message);
        }


        /// <summary>
        /// Dispose implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// The finalizer
        /// </summary>
        ~CommandLogger()
        {
            Dispose(false);
        }

        /// <summary>
        /// If you inherit from this class, override this member and be sure to call the base class
        /// </summary>
        /// <param name="disposing">Whether this call was a result of a call to IDisposable.Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
	                // ReSharper disable once InconsistentlySynchronizedField
	                _writer.Dispose();
                }

                _disposed = true;
            }
        }

        private static string FormHeader(ICommandInfo commandInfo, string action)
        {
            long parentId = commandInfo.ParentInfo?.Id ?? 0;
            var spaces = new string(' ', commandInfo.Depth);
            DateTime time = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);                  
            return $"{time:o} {spaces}{commandInfo.Id}({parentId}) {action} {commandInfo.GetType().FullName}";
        }

        private void WriteMessage(ICommandInfo commandInfo, string message)
        {
            string extendedInfo = commandInfo.ExtendedDescription();

            if (!string.IsNullOrWhiteSpace(extendedInfo))
            {
                message += " [" + extendedInfo + "]";
            }

            lock (_criticalSection)
            {
                _writer.WriteLine(message);
            }
        }

        private bool _disposed;
        private readonly System.IO.StreamWriter _writer;
        private readonly object _criticalSection = new object();
    }
}
