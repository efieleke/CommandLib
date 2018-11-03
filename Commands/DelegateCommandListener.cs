using System;

namespace Sophos.Commands
{
	/// <summary>
	/// Implements ICommandListener by using provided delegates.
	/// </summary>
	/// <typeparam name="TResult">The type returned by the function delegate</typeparam>
	public class DelegateCommandListener<TResult> : ICommandListener
	{
		/// <summary>
		/// Instantiates an ICommandListener that can be passed to Command.AsyncExecute()
		/// </summary>
		/// <param name="onSuccess">Method to be called when a <see cref="Command"/> succeeds synchronously</param>
		/// <param name="onAbort">Method to be called when a <see cref="Command"/> is aborted</param>
		/// <param name="onFail">Method to be called when a <see cref="Command"/> fails</param>
		public DelegateCommandListener(Action<TResult> onSuccess, Action onAbort, Action<Exception> onFail)
		{
			_onSuccess = onSuccess?? throw new ArgumentNullException(nameof(onSuccess));
			_onAbort = onAbort ?? throw new ArgumentNullException(nameof(onAbort));
			_onFail = onFail ?? throw new ArgumentNullException(nameof(onFail));
		}

		/// <inheritdoc />
		public void CommandSucceeded(object result)
		{
			_onSuccess((TResult)result);
		}

		/// <inheritdoc />
		public void CommandAborted()
		{
			_onAbort();
		}

		/// <inheritdoc />
		public void CommandFailed(Exception exc)
		{
			_onFail(exc);
		}

		private readonly Action<TResult> _onSuccess;
		private readonly Action _onAbort;
		private readonly Action<Exception> _onFail;
	}
}
