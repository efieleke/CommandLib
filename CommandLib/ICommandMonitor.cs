using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// This is a callback interface for <see cref="Command"/> starting and finishing events. Its intended use is for logging and diagnostics.
    /// </summary>
    /// <remarks>
    /// <see cref="CommandTracer"/> and <see cref="CommandLogger"/> are available implementations.
    /// You may set a monitor via the static <see cref="Command.Monitor"/> property of <see cref="Command"/>.
    /// </remarks>
    public interface ICommandMonitor : IDisposable
    {
        /// <summary>
        /// Invoked by the framework whenever a <see cref="Command"/> (including owned commands) starts execution
        /// </summary>
        /// <param name="command">
        /// The command that is starting execution. Implementations should treat this as a non-modifiable object, in the last stage of execution (notifying listeners).
        /// Also, do not call the command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// </param>
        /// <remarks>
        /// Implementations of this method must not throw.
        /// </remarks>
        void CommandStarting(Command command);

        /// <summary>
        /// Invoked by the framework whenever a <see cref="Command"/> (including owned commands) is finishing execution, for whatever reason (success, fail, or abort).
        /// </summary>
        /// <param name="command">
        /// The command that is finishing execution. Implementations should treat this as a non-modifiable object, in the last stage of execution (notifying listeners).
        /// Also, do not call the command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// </param>
        /// <param name="exc">
        /// Will be null if the command succeeded. Otherwise will be a <see cref="CommandAbortedException"/> if the command was aborted, or some other
        /// Exception type if the command failed.
        /// </param>
        /// <remarks>
        /// Implementations of this method must not throw.
        /// </remarks>
        void CommandFinished(Command command, Exception exc);
    }
}
