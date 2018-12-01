using System;

namespace Sophos.Commands
{
	/// <summary>
	/// This is a callback interface for <see cref="Command"/> starting and finishing events. Its intended use is for logging and diagnostics.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="CommandTracer"/> and <see cref="CommandLogger"/> are available implementations.
	/// You may add a monitor via the static <see cref="Command.Monitors"/> property of <see cref="Command"/>.
	/// </para>
	/// </remarks>
	public interface ICommandMonitor : IDisposable
    {
        /// <summary>
        /// Invoked by the framework whenever a <see cref="Command"/> (including owned commands) starts execution
        /// </summary>
        /// <param name="commandInfo">
        /// Information about the command that is starting execution. This may be safely cast to a Command
        /// object. The reason a Command object is not passed directly is to discourage invoking any method
        /// or property that could change the state of the command (which would cause undefined behavior).
        /// Implementations may safely call GetType() on this to determine the concrete command type.
        /// </param>
        /// <remarks>
        /// Implementations of this method must not throw.
        /// </remarks>
        void CommandStarting(ICommandInfo commandInfo);

        /// <summary>
        /// Invoked by the framework whenever a <see cref="Command"/> (including owned commands) is finishing execution, for whatever reason (success, fail, or abort).
        /// </summary>
        /// <param name="commandInfo">
        /// Information about the command that is finishing execution. This may be safely cast to a Command
        /// object. The reason a Command object is not passed directly is to discourage invoking any method
        /// or property that could change the state of the command (which would cause undefined behavior).
        /// Implementations may safely call GetType() on this to determine the concrete command type.
        /// </param>
        /// <param name="exc">
        /// Will be null if the command succeeded. Otherwise will be a <see cref="CommandAbortedException"/> if the command was aborted, or some other
        /// Exception type if the command failed.
        /// </param>
        /// <remarks>
        /// Implementations of this method must not throw.
        /// </remarks>
        void CommandFinished(ICommandInfo commandInfo, Exception exc);
    }
}
