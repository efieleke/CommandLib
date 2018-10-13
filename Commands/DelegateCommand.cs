using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sophos.Commands
{
	/// <summary>
	/// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
	/// In order for this command to be responsive to abort requests, the delegate method must check
	/// the <see cref="Command.AbortRequested"/> on the command that is the owner of this command.
	/// </summary>
	/// <typeparam name="TResult"></typeparam>
	public class DelegateCommand<TResult> : TaskCommand<TResult>
	{
		/// <summary>
		/// Constructs a command that will run the provided function, with no owner
		/// </summary>
		/// <param name="function">The function to run when this command is executed</param>
		public DelegateCommand(Func<TResult> function) : this(function, null) { }

		/// <summary>
		/// Constructs a command that will run the provided function, with no owner
		/// </summary>
		/// <param name="function">The function to run when this command is executed</param>
		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
		/// Owned commands respond to abort requests made of their owner. Also, owned commands are
		/// disposed of when the owner is disposed.
		/// </param>
		public DelegateCommand(Func<TResult> function, Command owner) : base(owner)
		{
			func = arg => function();
		}

		/// <summary>
		/// Constructs a command that will run the provided function, with no owner
		/// </summary>
		/// <param name="function">The function to run when this command is executed</param>
		public DelegateCommand(Func<object, TResult> function) : this(function, null) { }

		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
		/// Owned commands respond to abort requests made of their owner. Also, owned commands are
		/// disposed of when the owner is disposed.
		/// </param>
		public DelegateCommand(Func<object, TResult> function, Command owner) : base(owner)
		{
			func = function;
		}

		protected sealed override Task<TResult> CreateTask(object runtimeArg)
		{
			return new Task<TResult>(func, runtimeArg);
		}

		private readonly Func<object, TResult> func;
	}
}
