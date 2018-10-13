using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
	/// <summary>
	/// This Command encapsulates a Task. This command ignores abort requests. Concrete classes must implement the abstract method
	/// that creates the Task.
	/// </summary>
	/// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object for the'runtimeArg'. This is passed on to the abstract <see cref="CreateTask(object)"/> method.
	/// <para>
	/// This command returns from synchronous execution the value of type TResult that the undering Task returns. The 'result' parameter of
	/// <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion. It is the caller's responsibility to dispose of this
	/// response object if needed.
	/// </para>
	/// </remarks>
	public abstract class TaskCommand<TResult> : AsyncCommand
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public TaskCommand() : this(null)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
		/// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
		/// </param>
		public TaskCommand(Command owner) : base(owner)
		{
		}

		/// <summary>
		/// Implementations should override only if they contain members that must be disposed. Remember to invoke the base class implementation from within any override.
		/// </summary>
		/// <param name="disposing">Will be true if this was called as a direct result of the object being explicitly disposed.</param>
		protected override void Dispose(bool disposing)
		{
			if (!Disposed)
			{
				if (disposing)
				{
					if (task != null)
					{
						task.Dispose();
						task = null;
					}
				}
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Do not call this method from a derived class. It is called by the framework.
		/// </summary>
		/// <param name="listener">Not applicable</param>
		/// <param name="runtimeArg">This is passed on to the underlying Task creation method.</param>
		protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
		{
			int startingThreadId = Thread.CurrentThread.ManagedThreadId;

			if (task != null) { task.Dispose(); }
			task = CreateTask(runtimeArg);

			task.ContinueWith(t =>
			{
				// Check to see if CreateTask() returned a Task that executed synchronously.
				// That would be poor usage of this class, but it is supported.
				if (Thread.CurrentThread.ManagedThreadId == startingThreadId)
				{
					// We must call the listener back asynchronously. That's the contract.
					var thread = new Thread(ExecuteAsyncRoutine)
					{
						Name = Description + ": TaskCommand.ExecuteAsyncRoutine"
					};

					try
					{
						thread.Start(new AsyncThreadArg(listener, t.Result));
					}
					catch (AggregateException exc)
					{
						thread.Start(new AsyncThreadArg(listener, exc.InnerException));
					}
				}
				else
				{
					try
					{
						listener.CommandSucceeded(t.Result);
					}
					catch (AggregateException exc)
					{
						listener.CommandFailed(exc.InnerException);
					}
				}
			},
			TaskContinuationOptions.ExecuteSynchronously);

			if (task.Status == TaskStatus.Created)
			{
				task.Start();
			}
		}

		private class AsyncThreadArg
		{
			internal AsyncThreadArg(ICommandListener listener, TResult result)
			{
				Listener = listener;
				Result = result;
			}

			internal AsyncThreadArg(ICommandListener listener, Exception exception)
			{
				Listener = listener;
				Exception = exception;
			}

			internal ICommandListener Listener { get; }
			internal TResult Result { get; }
			internal Exception Exception { get; }
		}

		private void ExecuteAsyncRoutine(Object arg)
		{
			AsyncThreadArg threadArg = (AsyncThreadArg)arg;

			if (threadArg.Exception == null)
			{
				threadArg.Listener.CommandSucceeded(threadArg.Result);
			}
			else
			{
				threadArg.Listener.CommandFailed(threadArg.Exception);
			}
		}

		/// <summary>
		/// Concrete classes must implement this by returning a Task.
		/// </summary>
		/// <param name="runtimeArg">
		/// Concrete implementations decide what to do with this. This value is passed on from the runtimeArg
		/// that was provided to the synchronous or asynchronous execution methods.
		/// </param>
		/// <returns></returns>
		protected abstract Task<TResult> CreateTask(object runtimeArg);

		private Task<TResult> task;
	}
}
