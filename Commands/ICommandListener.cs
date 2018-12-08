using System;

namespace Sophos.Commands
{
	/// <summary>
	/// An object implementing this interface is required as a parameter to <see cref="Command.AsyncExecute(ICommandListener)"/>.
	/// Exactly one of its methods will eventually be called when a command is executed asynchronously, and it is guaranteed that the
	/// call will be on a thread different from the thread AsyncExecute was called from.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="Command"/> is in the last stage of execution when making these callbacks, so do not re-execute the command from within
	/// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> from within your handler, as that will cause deadlock.
	/// </para>
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
        /// Implementations must not throw.
        /// </remarks>
        void CommandSucceeded(object result);

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> was aborted.
        /// </summary>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// Implementations must not throw.
        /// </remarks>
        void CommandAborted();

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> fails.
        /// </summary>
        /// <param name="exc">The reason for failure</param>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// Implementations must not throw.
        /// </remarks>
        void CommandFailed(Exception exc);
    }

    /// <summary>
    /// An object implementing this interface is required as a parameter to certain overloads of <see cref="Command.AsyncExecute(ICommandListener)"/>.
    /// Exactly one of its methods will eventually be called when a command is executed asynchronously, and it is guaranteed that the
    /// call will be on a thread different from the thread AsyncExecute was called from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Command"/> is in the last stage of execution when making these callbacks, so do not re-execute the command from within
    /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> from within your handler, as that will cause deadlock.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type of resulting data tha executing the related command produces</typeparam>
    public interface ICommandListener<in TResult>
    {
        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> succeeds.
        /// </summary>
        /// <param name="result">The resulting data that executing the <see cref="Command"/> produced. </param>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// Implementations must not throw.
        /// </remarks>
        void CommandSucceeded(TResult result);

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> was aborted.
        /// </summary>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// Implementations must not throw.
        /// </remarks>
        void CommandAborted();

        /// <summary>
        /// Called when a <see cref="Command"/> launched via <see cref="Command.AsyncExecute(ICommandListener)"/> fails.
        /// </summary>
        /// <param name="exc">The reason for failure</param>
        /// <remarks>
        /// The <see cref="Command"/> is in the last stage of execution when making this callback, so do not re-execute the command from within
        /// your handler. Also, do not call the executing command's <see cref="Command.Wait()"/> method from within your handler, as that will cause deadlock.
        /// Implementations must not throw.
        /// </remarks>
        void CommandFailed(Exception exc);
    }

    /// <summary>
    /// Converts an see<see cref="ICommandListener{TResult}"/> to an <see cref="ICommandListener"/>"/>
    /// </summary>
    /// <typeparam name="TResult">The type of resulting data tha executing the related command produces</typeparam>
    public class CovariantListener<TResult> : ICommandListener
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener">The listener to convert</param>
        public CovariantListener(ICommandListener<TResult> listener)
        {
            _listener = listener;
        }

        /// <inheritdoc />
        public void CommandSucceeded(object result)
        {
            _listener.CommandSucceeded((TResult)result);
        }

        /// <inheritdoc />
        public void CommandAborted()
        {
            _listener.CommandAborted();
        }

        /// <inheritdoc />
        public void CommandFailed(Exception exc)
        {
            _listener.CommandFailed(exc);
        }

        private readonly ICommandListener<TResult> _listener;
    }
}
