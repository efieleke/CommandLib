using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// Implements <see cref="ICommandMonitor"/> by writing diagnostic information to a log file that can be parsed
    /// and dynamically displayed by the included CommandLogViewer application. Optionally writes to the debug stream
    /// as well.
    /// </summary>
    public class CommandLogger : ICommandMonitor
    {
        /// <summary>
        /// Constructs a CommandLogger object
        /// </summary>
        /// <param name="filename">Name of log file. Will be overwritten if it exists</param>
        /// <param name="logToDebugStream">if true, this will also log to the debug stream just like <see cref="CommandTracer"/></param>
        public CommandLogger(String filename, bool logToDebugStream)
        {
            if (logToDebugStream)
            {
                tracer = new CommandTracer();
            }

            writer = new System.IO.StreamWriter(filename, false);
            writer.AutoFlush = true;
        }

        /// <summary>
        /// Logs command starting info to file
        /// </summary>
        /// <param name="command">The command that is starting execution</param>
        public void CommandStarting(Command command)
        {
            if (tracer != null)
            {
                tracer.CommandStarting(command);
            }

            // Changing the format of this output will break the CommandLogViewer app.
            String header = FormHeader(command, "Starting");
            WriteMessage(command, header);
        }

        /// <summary>
        /// Logs command finished info to file
        /// </summary>
        /// <param name="command">The command that finished</param>
        /// <param name="exc">Will be null if the command succeeded. Otherwise a CommandAbortedException or some other Exception type</param>
        public void CommandFinished(Command command, Exception exc)
        {
            if (tracer != null)
            {
                tracer.CommandFinished(command, exc);
            }

            String message = null;

            if (exc == null)
            {
                message = FormHeader(command, "Completed");
            }
            else if (exc is CommandAbortedException)
            {
                message = FormHeader(command, "Aborted");
            }
            else
            {
                message = FormHeader(command, "Failed");
                message = String.Format("{0} Reason: {1}", message, exc.Message);
            }

            WriteMessage(command, message);
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
            if (!disposed)
            {
                if (disposing)
                {
                    if (tracer != null)
                    {
                        tracer.Dispose();
                    }

                    writer.Dispose();
                }

                disposed = true;
            }
        }

        static private String FormHeader(Command command, String action)
        {
            long parentId = command.Parent == null ? 0 : command.Parent.Id;
            String spaces = new String(' ', command.Depth);
            DateTime time = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);                  
            return String.Format("{0} {1}{2}({3}) {4} {5}", time.ToString("o"), spaces, command.Id, parentId, action, command.GetType().FullName);
        }

        private void WriteMessage(Command command, String message)
        {
            String extendedInfo = command.ExtendedDescription();

            if (!String.IsNullOrWhiteSpace(extendedInfo))
            {
                message += " [" + extendedInfo + "]";
            }

            writer.WriteLine(message);
        }

        private bool disposed = false;
        private System.IO.StreamWriter writer;
        private CommandTracer tracer;
    }
}
