using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// This Command encapsulates a Task. The static Create() methods might suffice for simple Task wrappers.
    /// Concrete classes must implement the abstract method that creates the Task. If your implementation is
    /// naturally asynchronous but does not make use of Tasks (i.e. the Task class), inherit directly from
    /// AsyncCommand instead.
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
    /// <typeparam name="TArg">The type of argument passed to the method that generates the task</typeparam>
    public abstract class TaskCommand<TArg, TResult> : AsyncCommand
    {
        /// <summary>
        /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
        /// </summary>
        /// <param name="func">
        /// The delegate that returns a Task. This will be run when the command is executed. This function
        /// is passed a cancellation token that can be passed to methods that return tasks.
        /// </param>
        /// <returns>The created command. It ignores the runtimeArg passed to SyncExecute() or AsyncExecute()</returns>
        public static TaskCommand<TArg, TResult> Create(Func<CancellationToken, Task<TResult>> func)
        {
            return Create(func, null);
        }

        /// <summary>
        /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
        /// </summary>
        /// <param name="func">
        /// The delegate that returns a Task. This will be run when the command is executed. This function
        /// is passed a cancellation token that can be passed to methods that return tasks.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
        /// Owned commands are disposed of when the owner is disposed.
        /// </param>
        /// <returns>The created command. It ignores the runtimeArg passed to SyncExecute() or AsyncExecute()</returns>
        public static TaskCommand<TArg, TResult> Create(Func<CancellationToken, Task<TResult>> func, Command owner)
        {
            return new DelegatedTaskCommand(func, owner);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.SyncExecute()"/>
        /// </summary>
        /// <returns>the same value that the underlying task returns</returns>
        /// <remarks>See <see cref="Command.SyncExecute()"/> for further details.</remarks>
        public new TResult SyncExecute()
        {
            return (TResult)base.SyncExecute();
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.SyncExecute(object)"/>
        /// </summary>
        /// <param name="runtimeArg">this is passed to the task instantiation method</param>
        /// <returns>the same value that the underlying task returns</returns>
        /// <remarks>See <see cref="Command.SyncExecute(object)"/> for further details.</remarks>
        public new TResult SyncExecute(object runtimeArg)
        {
            return (TResult)base.SyncExecute(runtimeArg);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.SyncExecute(object)"/>
        /// </summary>
        /// <param name="runtimeArg">this is passed to the task instantiation method</param>
        /// <returns>the same value that the underlying task returns</returns>
        /// <remarks>See <see cref="Command.SyncExecute(object)"/> for further details.</remarks>
        public TResult SyncExecute(TArg runtimeArg)
        {
            return (TResult)base.SyncExecute(runtimeArg);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.SyncExecute(object, Command)"/>
        /// </summary>
        /// <param name="runtimeArg">this is passed to the task instantiation method</param>
        /// <param name="owner">
        /// If you want this command to pay attention to abort requests of a different command, set this value to that command.
        /// Note that if this Command is already assigned an owner, passing a non-null value will raise an exception. Also note
        /// that the owner assignment is only in effect during the scope of this call. Upon return, this command will revert to
        /// having no owner.
        /// </param>
        /// <returns>the same value that the underlying task returns</returns>
        /// <remarks>See <see cref="Command.SyncExecute(object, Command)"/> for further details.</remarks>
        public new TResult SyncExecute(object runtimeArg, Command owner)
        {
            return (TResult)base.SyncExecute(runtimeArg, owner);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.SyncExecute(object, Command)"/>
        /// </summary>
        /// <param name="runtimeArg">this is passed to the task instantiation method</param>
        /// <param name="owner">
        /// If you want this command to pay attention to abort requests of a different command, set this value to that command.
        /// Note that if this Command is already assigned an owner, passing a non-null value will raise an exception. Also note
        /// that the owner assignment is only in effect during the scope of this call. Upon return, this command will revert to
        /// having no owner.
        /// </param>
        /// <returns>the same value that the underlying task returns</returns>
        /// <remarks>See <see cref="Command.SyncExecute(object, Command)"/> for further details.</remarks>
        public TResult SyncExecute(TArg runtimeArg, Command owner)
        {
            return (TResult)base.SyncExecute(runtimeArg, owner);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.AsyncExecute(Action{object}, Action, Action{Exception})"/>
        /// </summary>
        /// <param name="onSuccess">Callback for successful operation</param>
        /// <param name="onAbort">Callback for aborted operation</param>
        /// <param name="onFail">Callback for failed operation</param>
        /// <remarks>See <see cref="Command.AsyncExecute(Action{object}, Action, Action{Exception})"/> for further details</remarks>
        public void AsyncExecute(Action<TResult> onSuccess, Action onAbort, Action<Exception> onFail)
        {
            AsyncExecute(new DelegateCommandListener<TResult>(onSuccess, onAbort, onFail));
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.AsyncExecute(ICommandListener)"/>
        /// </summary>
        /// <param name="listener">One of this member's methods will be called upon completion of the command</param>
        /// <remarks>See <see cref="Command.AsyncExecute(ICommandListener)"/> for further details</remarks>
        public void AsyncExecute(ICommandListener<TResult> listener)
        {
            base.AsyncExecute(new CovariantListener<TResult>(listener));
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.AsyncExecute(Action{object}, Action, Action{Exception}, object)"/>
        /// </summary>
        /// <param name="onSuccess">Callback for successful operation</param>
        /// <param name="onAbort">Callback for aborted operation</param>
        /// <param name="onFail">Callback for failed operation</param>
        /// <param name="runtimeArg">This value is passed to the method that instantiates the task</param>
        /// <remarks>See <see cref="Command.AsyncExecute(Action{object}, Action, Action{Exception}, object)"/> for further details</remarks>
        public void AsyncExecute(Action<TResult> onSuccess, Action onAbort, Action<Exception> onFail, TArg runtimeArg)
        {
            AsyncExecute(new DelegateCommandListener<TResult>(onSuccess, onAbort, onFail), runtimeArg);
        }

        /// <summary>
        /// Covariant implementation of <see cref="Command.AsyncExecute(ICommandListener, object)"/>
        /// </summary>
        /// <param name="listener">One of this member's methods will be called upon completion of the command</param>
        /// <param name="runtimeArg">This value is passed to the method that instantiates the task</param>
        /// <remarks>See <see cref="Command.AsyncExecute(ICommandListener, object)"/> for further details</remarks>
        public void AsyncExecute(ICommandListener<TResult> listener, TArg runtimeArg)
        {
            base.AsyncExecute(new CovariantListener<TResult>(listener), runtimeArg);
        }

        /// <summary>
        /// Constructor for a top-level command
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
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="listener">Not applicable</param>
        /// <param name="runtimeArg">This is passed on to the underlying Task creation method.</param>
        protected sealed override async void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            Task<TResult> task;

            try
            {
                task = CreateTask(runtimeArg == null ? default(TArg) : (TArg)runtimeArg, CancellationToken);
            }
            catch (Exception e)
            {
                // We failed synchronously. This is most likely due to an exception occuring before
                // the first await. Let's be consistent about this and make the callback on the listener.
                // That must be done on a different thread.
                task = new Task<TResult>(() => throw e);
            }

            using (task)
            {
                if (task.Status == TaskStatus.Created)
                {
                    task.Start();
                }

                TResult result = default(TResult);
                Exception error = null;

                try
                {
                    result = await task;
                }
                catch (Exception exc)
                {
                    error = exc is OperationCanceledException ? new CommandAbortedException() : exc;
                }

                switch (error)
                {
                    case null:
                        listener.CommandSucceeded(result);
                        break;
                    case CommandAbortedException _:
                        listener.CommandAborted();
                        break;
                    default:
                        listener.CommandFailed(error);
                        break;
                }
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
        /// <param name="cancellationToken">
        /// If your implementation will create tasks that require a cancellation token to be responsive
        /// to abort requests, pass this token along.
        /// </param>
        /// <returns>
        /// This value is passed along as the return value of synchronous execution routines, or the 'result' parameter
        /// of <see cref="ICommandListener.CommandSucceeded"/> for asynchronous execution routines.
        /// </returns>
        protected abstract Task<TResult> CreateTask(TArg runtimeArg, CancellationToken cancellationToken);

        private class DelegatedTaskCommand : TaskCommand<TArg, TResult>
        {
            internal DelegatedTaskCommand(Func<CancellationToken, Task<TResult>> func, Command owner) : base(owner)
            {
                _func = func;
            }

            protected sealed override Task<TResult> CreateTask(TArg _, CancellationToken cancellationToken)
            {
                return _func(cancellationToken);
            }

            private readonly Func<CancellationToken, Task<TResult>> _func;
        }
    }

    /// <summary>
    /// This Command encapsulates a Task. The static Create() method might suffice for simple Task wrappers.
    /// Concrete classes must implement the abstract method that creates the Task. If your implementation is
    /// naturally asynchronous but does not make use of Tasks (i.e. the Task class), inherit directly from
    /// AsyncCommand instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/> will accept 
    /// an object for the 'runtimeArg'. This is passed on to the abstract <see cref="CreateTask" /> method.
    /// </para>
    /// <para>
    /// This command returns from synchronous execution the bool value true. The 'result' parameter of
    /// <see cref="ICommandListener.CommandSucceeded"/> will be set to true as well. 
    /// </para>
    /// </remarks>
    /// <typeparam name="TArg">The type of argument passed to method that creates the task</typeparam>
    public abstract class TaskCommand<TArg> : TaskCommand<TArg, bool>
    {
        /// <summary>
        /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
        /// </summary>
        /// <param name="func">
        /// The delegate that returns a Task. This will be run when the command is executed. This function
        /// is passed a cancellation token that can be passed to methods that return tasks.
        /// </param>
        /// <returns>The created command. It ignores the runtimeArg passed to SyncExecute() or AsyncExecute()</returns>
        public static TaskCommand<TArg> Create(Func<CancellationToken, Task> func)
        {
            return Create(func, null);
        }

        /// <summary>
        /// This class wraps a delegate as a Command. When the command is executed, the delegate is run.
        /// </summary>
        /// <param name="func">
        /// The delegate that returns a Task. This will be run when the command is executed. This function
        /// is passed a cancellation token that can be passed to methods that return tasks.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'.
        /// Owned commands are disposed of when the owner is disposed.
        /// </param>
        /// <returns>The created command. It ignores the runtimeArg passed to SyncExecute() or AsyncExecute()</returns>
        public static TaskCommand<TArg> Create(Func<CancellationToken, Task> func, Command owner)
        {
            return new DelegatedTaskCommand(func, owner);
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
        /// Concrete classes must implement this by returning a Task. If the delegate method takes significant
        /// time, it is advisable to have it be responsive to abort requests by checking
        /// <see cref="Command.AbortRequested"/> or calling <see cref="Command.CheckAbortFlag"/>.
        /// </summary>
        /// <param name="runtimeArg">
        /// Concrete implementations decide what to do with this. This value is passed on from the runtimeArg
        /// that was provided to the synchronous or asynchronous execution methods.
        /// </param>
        /// <param name="cancellationToken">
        /// If your implementation will create tasks that require a cancellation token to be responsive
        /// to abort requests, pass this token along.
        /// </param>
        /// <returns>
        /// This value is passed along as the return value of synchronous execution routines, or the 'result' parameter
        /// of <see cref="ICommandListener.CommandSucceeded"/> for asynchronous execution routines.
        /// </returns>
        protected abstract Task CreateTaskNoResult(TArg runtimeArg, CancellationToken cancellationToken);

        /// <inheritdoc />
        protected sealed override async Task<bool> CreateTask(TArg runtimeArg, CancellationToken cancellationToken)
        {
            using (Task task = CreateTaskNoResult(runtimeArg, cancellationToken))
            {
                await task;
                return true;
            }
        }

        private class DelegatedTaskCommand : TaskCommand<TArg>
        {
            internal DelegatedTaskCommand(Func<CancellationToken, Task> func, Command owner) : base(owner)
            {
                _func = func;
            }

            protected sealed override Task CreateTaskNoResult(TArg _, CancellationToken cancelToken)
            {
                return _func(cancelToken);
            }

            private readonly Func<CancellationToken, Task> _func;
        }
    }
}
