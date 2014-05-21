﻿using System;
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

            System.IO.FileStream stream = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
            writer = new System.IO.StreamWriter(stream);
            writer.AutoFlush = true;
        }

        /// <summary>
        /// Logs command starting info to file
        /// </summary>
        /// <param name="commandInfo">Information about the command that is starting execution</param>
        public void CommandStarting(ICommandInfo commandInfo)
        {
            if (tracer != null)
            {
                tracer.CommandStarting(commandInfo);
            }

            // Changing the format of this output will break the CommandLogViewer app.
            String header = FormHeader(commandInfo, "Starting");
            WriteMessage(commandInfo, header);
        }

        /// <summary>
        /// Logs command finished info to file
        /// </summary>
        /// <param name="commandInfo">Information about the command that is finishing</param>
        /// <param name="exc">Will be null if the command succeeded. Otherwise a CommandAbortedException or some other Exception type</param>
        public void CommandFinished(ICommandInfo commandInfo, Exception exc)
        {
            if (tracer != null)
            {
                tracer.CommandFinished(commandInfo, exc);
            }

            String message = null;

            if (exc == null)
            {
                message = FormHeader(commandInfo, "Completed");
            }
            else if (exc is CommandAbortedException)
            {
                message = FormHeader(commandInfo, "Aborted");
            }
            else
            {
                message = FormHeader(commandInfo, "Failed");
                message = String.Format("{0} Reason: {1}", message, exc.Message);
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

        static private String FormHeader(ICommandInfo commandInfo, String action)
        {
            long parentId = commandInfo.ParentInfo == null ? 0 : commandInfo.ParentInfo.Id;
            String spaces = new String(' ', commandInfo.Depth);
            DateTime time = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);                  
            return String.Format("{0} {1}{2}({3}) {4} {5}", time.ToString("o"), spaces, commandInfo.Id, parentId, action, commandInfo.GetType().FullName);
        }

        private void WriteMessage(ICommandInfo commandInfo, String message)
        {
            String extendedInfo = commandInfo.ExtendedDescription();

            if (!String.IsNullOrWhiteSpace(extendedInfo))
            {
                message += " [" + extendedInfo + "]";
            }

            lock (criticalSection)
            {
                writer.WriteLine(message);
            }
        }

        private bool disposed = false;
        private System.IO.StreamWriter writer;
        private CommandTracer tracer;
        private Object criticalSection = new Object();
    }
}
