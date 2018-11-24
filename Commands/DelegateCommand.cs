using System;

namespace Sophos.Commands
{
    /// <summary>
    /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
    /// In order for this command to be responsive to abort requests, the delegate method must check
    /// the <see cref="Command.AbortRequested"/> flag on the command that is the owner of this command.
    /// </summary>
    /// <typeparam name="TResult">The type returned by the delegate</typeparam>
	public sealed class DelegateCommand<TResult> : SyncCommand
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
			_func = arg => function();
		}

		/// <summary>
		/// Constructs a command that will run the provided function, with no owner
		/// </summary>
		/// <param name="function">The function to run when this command is executed</param>
		public DelegateCommand(Func<object, TResult> function) : this(function, null) { }

		/// <summary>
		/// Constructs a command that will run the provided function, with no owner
		/// </summary>
		/// <param name="function">The function to run when this command is executed</param>
		/// <param name="owner">
		/// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
		/// Owned commands respond to abort requests made of their owner. Also, owned commands are
		/// disposed of when the owner is disposed.
		/// </param>
		public DelegateCommand(Func<object, TResult> function, Command owner) : base(owner)
		{
			_func = function;
		}

	    /// <inheritdoc />
		protected override object SyncExecuteImpl(object runtimeArg)
		{
			return _func(runtimeArg);
		}

		private readonly Func<object, TResult> _func;
	}
    /// <summary>
    /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
    /// In order for this command to be responsive to abort requests, the delegate method must check
    /// the <see cref="Command.AbortRequested"/> flag on the command that is the owner of this command.
    /// </summary>
    public sealed class DelegateCommand : SyncCommand
    {
        /// <summary>
        /// Constructs a command that will run the provided action, with no owner
        /// </summary>
        /// <param name="action">The action to run when this command is executed</param>
        public DelegateCommand(Action action) : this(action, null) { }

        /// <summary>
        /// Constructs a command that will run the provided action, with no owner
        /// </summary>
        /// <param name="action">The action to run when this command is executed</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
        /// Owned commands respond to abort requests made of their owner. Also, owned commands are
        /// disposed of when the owner is disposed.
        /// </param>
        public DelegateCommand(Action action, Command owner) : base(owner)
        {
            _action = arg => action();
        }

        /// <summary>
        /// Constructs a command that will run the provided action, with no owner
        /// </summary>
        /// <param name="action">The action to run when this command is executed</param>
        public DelegateCommand(Action<object> action) : this(action, null) { }

        /// <summary>
        /// Constructs a command that will run the provided action, with no owner
        /// </summary>
        /// <param name="action">The action to run when this command is executed</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
        /// Owned commands respond to abort requests made of their owner. Also, owned commands are
        /// disposed of when the owner is disposed.
        /// </param>
        public DelegateCommand(Action<object> action, Command owner) : base(owner)
        {
            _action = action;
        }

        /// <inheritdoc />
        protected override object SyncExecuteImpl(object runtimeArg)
        {
            _action(runtimeArg);
            return null;
        }

        private readonly Action<object> _action;
    }

}
