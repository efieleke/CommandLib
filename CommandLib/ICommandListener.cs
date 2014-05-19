using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// An object implementing this interface is required as a parameter to <see cref="Command.AsyncExecute(ICommandListener)"/>.
    /// Exactly one of its methods will eventually be called when a command is executed asynchronously, and it is guaranteed that the
    /// call will be on a thread different from the thread AsyncExecute was called from.
    /// </summary>
    /// <remarks>
    /// The <see cref="Command"/> is in the last stage of execution when making these callbacks, so do not re-execute the command from within
    /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> from within your handler, as that will cause deadlock.
    /// </remarks>
    public interface ICommandListener
    {
        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> succeeds.
        /// </summary>
        /// <param name="result">
        /// The resulting data that executing the <see cref="Command"/> produced. The actual type of data is up to the concrete <see cref="Command"/>
        /// class, and should be documented by that class. If the command produces no data, this will be null. Otherwise, 'result' may be cast to the
        /// expected type.
        /// </param>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// </remarks>
        void CommandSucceeded(Object result);

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> was aborted.
        /// </summary>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// </remarks>
        void CommandAborted();

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> fails.
        /// </summary>
        /// <param name="exc">The reason for failure</param>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// </remarks>
        void CommandFailed(Exception exc);
    }
}
