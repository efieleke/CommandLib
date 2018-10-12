using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommandLib
{
	/// <summary>
	/// This Command encapsulates a Task. Unlike other commands, a TaskCommand can only be executed once,
	/// because Tasks can only be run once. This command ignores abort requests, and the runtime arg value
	/// that can be passed as an argument to the execution methods is ignored.
	/// </summary>
	public class TaskCommand<TResult> : AsyncCommand
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="task">
		/// The underlying task to run. It must not be disposed before this command is executed.
		/// </param>
		public TaskCommand(Task<TResult> task) : this(task, null)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="task">
		/// The underlying task to run. It must not be disposed before this command is executed.
		/// </param>
		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
		/// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
		/// </param>
		public TaskCommand(Task<TResult> task, Command owner) : base(owner)
		{
			this.task = task;
		}

		/// <summary>
		/// Do not call this method from a derived class. It is called by the framework.
		/// </summary>
		/// <param name="listener">Not applicable</param>
		/// <param name="runtimeArg">Not applicable</param>
		protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
		{
			int startingThreadId = Thread.CurrentThread.ManagedThreadId;

			task.ContinueWith(t =>
			{
				// This always seems to be on a different thread, but the documentation does
				// not guarantee that will always be the case.
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
			});

			task.Start();
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

		private readonly Task<TResult> task;
	}
}
