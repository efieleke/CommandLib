using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// This Command encapsulates a Task. Concrete classes must implement the abstract method
    /// that creates the Task. If your implementation is naturally asynchronous but does not make use
    /// of Tasks (i.e. the Task class), inherit directly from AsyncCommand instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object for the 'runtimeArg'. This is passed on to the abstract <see cref="CreateTask" /> method.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the value of type TResult that the underlying Task returns. The 'result' parameter of
    /// <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion. It is the caller's responsibility to dispose of this
    /// response object if needed.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type returned with the Task</typeparam>
	public abstract class TaskCommand<TResult> : AsyncCommand
	{
		/// <summary>
		/// Constructor
		/// </summary>
		protected TaskCommand() : this(null)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
		/// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
		/// </param>
		protected TaskCommand(Command owner) : base(owner)
		{
		}

		/// <summary>
		/// Implementations should override only if they contain members that must be disposed. Remember to invoke the base class implementation from within any override.
		/// </summary>
		/// <param name="disposing">Will be true if this was called as a direct result of the object being explicitly disposed.</param>
		protected override void Dispose(bool disposing)
		{
			if (!Disposed && disposing && _task != null)
			{
				_task.Dispose();
				_task = null;
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

			_task?.Dispose();

			try
			{
				_task = CreateTask(runtimeArg);
			}
			catch(Exception e)
			{
				// We failed synchronously. This is most likely due to an exception occuring before
				// the first await. Let's be consistent about this and make the callback on the listener.
				// That must be done on a different thread.
				_task = new Task<TResult>(() => throw e);
			}

			_task.ContinueWith(t =>
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
					    switch (exc.InnerException)
					    {
					        case CommandAbortedException _:
					        case TaskCanceledException _:
					            listener.CommandAborted();
					            break;
					        default:
					            listener.CommandFailed(exc.InnerException);
					            break;
					    }
					}
				}
			},
			TaskContinuationOptions.ExecuteSynchronously);

			if (_task.Status == TaskStatus.Created)
			{
				_task.Start();
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

		private void ExecuteAsyncRoutine(object arg)
		{
			AsyncThreadArg threadArg = (AsyncThreadArg)arg;

			switch (threadArg.Exception)
			{
				case null:
					threadArg.Listener.CommandSucceeded(threadArg.Result);
					break;
				case CommandAbortedException _:
                case TaskCanceledException _:
					threadArg.Listener.CommandAborted();
					break;
				default:
					threadArg.Listener.CommandFailed(threadArg.Exception);
					break;
			}
		}

		/// <summary>
		/// Concrete classes must implement this by returning a Task. If the delegate method takes significant
		/// time, it is advisable to have it be responsive to abort requests by checking
		/// <see cref="Command.AbortRequested"/> or calling <see cref="Command.CheckAbortFlag"/>.
		/// </summary>
		/// <param name="runtimeArg">
		/// Concrete implementations decide what to do with this. This value is passed on from the runtimeArg
		/// that was provided to the synchronous or asynchronous execution methods.
		/// </param>
		/// <returns>
		/// This value is passed along as the return value of synchronous execution routines, or the 'result' parameter
		/// of <see cref="ICommandListener.CommandSucceeded"/> for asynchronous execution routines.
		/// </returns>
		protected abstract Task<TResult> CreateTask(object runtimeArg);

		private Task<TResult> _task;
	}
}
