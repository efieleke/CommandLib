using System;

namespace Sophos.Commands
{
    /// <summary>
    /// This is thrown from Command.SyncExecute() when a command is aborted.
    /// </summary>
    [SerializableAttribute]
    public class CommandAbortedException : Exception
    {
        /// <summary>
        /// Constructs a CommandAbortedException object
        /// </summary>
        public CommandAbortedException()
        {
        }

        /// <summary>
        /// Constructs a CommandAbortedException object
        /// </summary>
        /// <param name="message">See <see cref="Exception"/> documentation</param>
        public CommandAbortedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a CommandAbortedException object
        /// </summary>
        /// <param name="message">See <see cref="Exception"/> documentation</param>
        /// <param name="innerException">>See <see cref="Exception"/> documentation</param>
        public CommandAbortedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructs a CommandAbortedException object
        /// </summary>
        /// <param name="info">>See <see cref="Exception"/> documentation</param>
        /// <param name="context">>See <see cref="Exception"/> documentation</param>
        public CommandAbortedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
