﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// This library contains a set of classes that can be used to coordinate synchronous and asynchronous activities.
    /// Most classes in this library inherit from <see cref="Command"/>, which represents an action. Any
    /// <see cref="Command"/> can be run synchronously or asynchronously, and may be aborted.
    /// <para></para>
    /// <para>
    /// Using <see cref="ParallelCommands"/>, you can run a collection of <see cref="Command"/> objects concurrently, and using
    /// <see cref="SequentialCommands"/>, you can run a collection of <see cref="Command"/> objects in sequence. Any command
    /// can be added to these two types (including <see cref="ParallelCommands"/> and <see cref="SequentialCommands"/> themselves,
    /// (because they are <see cref="Command"/> objects), so it's possible to create deep levels of coordinated activities.
    /// </para>
    /// <para>
    /// Commands works both with tasks and with non-task-based asynchronous operations. <see cref="TaskCommand{TArg, TResult}"/>,
    /// <see cref="DelegateCommand{TResult}"/>,  and <see cref="Command.RunAsTask{TResult}(object, Command)"/> offer easy
    /// integration with Tasks and delegates.
    /// </para>
    /// <para>
    /// <see cref="PeriodicCommand"/> repeats its action at a given interval, <see cref="ScheduledCommand"/> runs once at a specific
    /// time, and <see cref="RecurringCommand"/> runs at times that are provided via a callback.
    /// </para>
    /// <para>
    /// <see cref="RetryableCommand"/> provides the option to conditionally keep retrying a failed command, <see cref="FinallyCommand"/>
    /// ensures a cleanup routine is run regardless of whether a command succeeds or fails, and <see cref="TimeLimitedCommand"/>
    /// fails with a timeout exception if a given duration elapses before the command finishes execution.
    /// </para>
    /// <para>
    /// <see cref="CommandDispatcher"/> manages asynchronous execution of dynamically generated commands.
    /// </para>
    /// <para>
    /// All of the above <see cref="Command"/> classes are simply containers for <see cref="Command"/> objects that presumably do
    /// something of interest. It is expected that users of this library will create their own <see cref="Command"/>-derived classes.
    /// </para>
    /// <para>
    /// Guidelines for developing your own Command-derived class:
    /// </para>
    /// <para>
    /// - If the implementation of your command is naturally synchronous, inherit from <see cref="SyncCommand"/>
    /// </para>
    /// <para>
    /// - If the implementation of your command is naturally asynchronous and makes use of Tasks (i.e. the Task class), inherit from <see cref="TaskCommand{TArg, TResult}"/>
    /// </para>
    /// <para>
    /// - If the implementation of your command is naturally asynchronous but does not make use of tasks, inherit from <see cref="AsyncCommand"/>
    /// </para>
    /// <para>
    /// - Make your implementation responsive to abort requests if it could take more than a trivial amount of time. To do this, make occasional calls to Command.CheckAbortFlag() or Command.AbortRequested.
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Informational data that is part of a <see cref="Command"/> instance.
    /// </summary>
    public interface ICommandInfo
    {
        /// <summary>
        /// Returns the unique identifier for the command.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// Returns the owner, or null if this <see cref="Command"/> is top level.
        /// </summary>
        ICommandInfo ParentInfo { get; }

        /// <summary>
        /// Returns the list of commands that are owned by this command. Will return an empty list if there are none.
        /// </summary>
        IList<ICommandInfo> ChildInfo { get; }

        /// <summary>
        /// Counts the number of parents until the top level command is reached
        /// </summary>
        /// <remarks>A parent is considered an owner.</remarks>
        int Depth { get; }

        /// <summary>A description of the Command</summary>
        /// <remarks>
        /// This is the name of the concrete class of this command, preceded by the names of the classes of each parent,
        /// up to the top-level parent. This is followed with this command's unique id (for example, 'SequentialCommands=>PauseCommand(23)').
        /// The description ends with details of about the current state of the command, if available.
        /// <para>
        /// A parent is considered the owner.
        /// </para>
        /// </remarks>
        string Description { get; }

        /// <summary>
        /// Information about the command (beyond its type and id), if available, for diagnostic purposes.
        /// </summary>
        /// <returns>
        /// Implementations should return information about the current state of the Command, if available. Return an empty string
        /// or null if there is no useful state information to report.
        /// </returns>
        /// <remarks>
        /// This information is included as part of the <see cref="Description"/> property. It is meant for diagnostic purposes.
        /// <para>
        /// Implementations must be thread safe, and they must not not throw.
        /// </para>
        /// </remarks>
        string ExtendedDescription();
    }

    /// <summary>Represents an action that can be run synchronously or asynchronously.</summary>
    /// <remarks>
    /// <para>
    /// Commands may be aborted. Even a synchronously running command can be aborted from a separate thread.
    /// </para>
    /// <para>
    /// Commands all inherit from IDisposable, but Dispose() need only be called on top level commands. A top-level
    /// command is a Command that is not owned by another Command. Some Command-derived classes will take ownership
    /// of Command objects passed as arguments to certain methods. Such behavior is documented.
    /// </para>
    /// <para>
    /// When developing a Command subclass, inherit from <see cref="T:Sophos.Commands.SyncCommand" /> (if your command is naturally
    /// synchronous in its implementation), <see cref="T:Sophos.Commands.TaskCommand`1" /> (if your asynchronous command uses Tasks in its
    /// implementation), or <see cref="T:Sophos.Commands.AsyncCommand" /> if your implementation is otherwise asynchronous (that is, it
    /// naturally completes on a thread that is different from the one it started on, but is not in the form of a Task).
    /// <see cref="M:Sophos.Commands.Command.SyncExecuteImpl(System.Object)" />).
    /// </para>
    /// <para>
    /// Also, when developing a Command subclass, make sure that any member variables that are Commands are properly
    /// owned by passing 'this' as the owner field when constructing the command. The main advantages of doing this
    /// are 1) owned commands will automatically respond to abort requests issued to the owner, and 2) the owner
    /// will automatically call Dispose() on its owned commands when it itself is disposed.
    /// </para>
    /// <para>
    /// If you write a method that accepts a Command as an argument, you may wish to assume ownership of that Command.
    /// <see cref="M:Sophos.Commands.Command.TakeOwnership(Sophos.Commands.Command)" /> allows you to do this. The <see cref="M:Sophos.Commands.SequentialCommands.Add(Sophos.Commands.Command)" /> member of
    /// <see cref="T:Sophos.Commands.SequentialCommands" /> is an example of this behavior.
    /// </para>
    /// <para>
    /// If you find that you need to create a Command object within the execution method of its owning command
    /// (perhaps because the way to create the Command depends upon runtime conditions), there are some things to
    /// consider. Owned commands are not disposed until the owner is disposed. If the owner is executed many times
    /// before it is disposed, and you create a new child command with the same owner upon every execution, resource usage
    /// will grow unbounded. The better approach is to not assign an owner to the locally created command, but instead
    /// have it run within the context of the launching command using <see cref="M:Sophos.Commands.Command.SyncExecute(System.Object,Sophos.Commands.Command)" />.
    /// </para>
    /// <para>
    /// Generally speaking, when authoring Commands, it's best to make them as granular as possible. That makes it much easier
    /// to reuse them while composing command structures. Also, ensure that your commands are responsive to abort requests if
    /// they take a noticeable amount of time to complete (whether or not they are synchronous in nature).
    /// </para>
    /// </remarks>
    public abstract class Command : IDisposable, ICommandInfo
    {
        /// <summary>
        /// The objects that define command monitoring behavior. Monitoring is meant for logging and diagnostic purposes.
        /// </summary>
        /// <remarks>
        /// This property is not thread-safe. Be sure not to change it while any commands are executing.
        /// <para>
        /// There are no default monitors. <see cref="CommandTracer"/> and <see cref="CommandLogger"/> are implementations of
        /// <see cref="ICommandMonitor"/> that can be used.
        /// </para>
        /// <para>
        /// The caller is responsible for calling Dispose() on any monitors added to this collection. Changing it will not dispose of the previously
        /// set values.
        /// </para>
        /// </remarks>
        public static LinkedList<ICommandMonitor> Monitors { get; set; }

        /// <summary>
        /// Returns Command context information related to an exception, if present
        /// </summary>
        /// <param name="exc">The exception object of interest</param>
        /// <returns>Text that describes the command that raised the exception during its execution</returns>
        public static string GetAttachedErrorInfo(Exception exc)
        {
            if (exc.Data.Contains("CommandContext"))
            {
                return (string)exc.Data["CommandContext"];
            }

            return null;
        }

        #region ICommandInfo
        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public ICommandInfo ParentInfo => GetParent(this);

        /// <inheritdoc />
        public IList<ICommandInfo> ChildInfo
        {
            get
            {
                lock (_childLock)
                {
                    return new List<ICommandInfo>(_children);
                }
            }
        }

        /// <inheritdoc />
        public int Depth
        {
            get
            {
                CheckDisposed();
                Command parent = GetParent(this);
                return parent?.Depth + 1 ?? 0;
            }
        }

        /// <inheritdoc />
        public string Description
        {
            get
            {
                CheckDisposed();
                string result = GetType().Name;

                for (Command parent = GetParent(this); parent != null; parent = GetParent(parent))
                {
                    result = parent.GetType().Name + "=>" + result;
                }

                result += "(" + Id + ")";
                string extendedDescription = ExtendedDescription();

                if (!string.IsNullOrWhiteSpace(extendedDescription))
                {
                    result += " " + extendedDescription;
                }

                return result;
            }
        }

        /// <inheritdoc />
        public virtual string ExtendedDescription()
        {
            return "";
        }
        #endregion

        #region Tasks

        /// <summary>
        /// Returns a task that executes this command. Note that this command must
        /// not be disposed, because it will be auto-disposed when the task completes. Also, this operation will fail if this
        /// command is not a top-level command (in other words, it must not have parents). If you wish to run the Task
        /// as a child of another Command (and thus make it responsive to abort requests of that parent), pass a
        /// non-null owner to <see cref="Command.RunAsTask{TResult}(object, Command)"/>
        /// Also, note that behavior is undefined if this command is executing at the time this method is called.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type the command returns from SynExecute. If you don't care about the return value, it is safe to
        /// specify Object as the type.
        /// </typeparam>
        /// <returns>
        /// The Task, which will have been started.
        /// </returns>
        public Task<TResult> RunAsTask<TResult>()
        {
            return RunAsTask<TResult>(null);
        }

        /// <summary>
        /// Returns a task that executes this command. Note that this command must
        /// not be disposed, because it will be auto-disposed when the task completes. Also, this operation will fail if this
        /// command is not a top-level command (in other words, it must not have parents). If you wish to run the Task
        /// as a child of another Command (and thus make it responsive to abort requests of that parent), pass a
        /// non-null owner to <see cref="Command.RunAsTask{TResult}(object, Command)"/>
        /// Also, note that behavior is undefined if this command is executing at the time this method is called.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type the command returns from SynExecute. If you don't care about the return value, it is safe to
        /// specify Object as the type.
        /// </typeparam>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands do not.
        /// See the concrete command class of interest for details.
        /// </param>
        /// <returns>
        /// The Task, which will have been started.
        /// </returns>
        public Task<TResult> RunAsTask<TResult>(object runtimeArg)
        {
            return RunAsTask<TResult>(runtimeArg, null);
        }

        /// <summary>
        /// Returns a task that executes this command. Note that this command must
        /// not be disposed, because it will be auto-disposed when the task completes. Also, this operation will fail if this
        /// command is not a top-level command (in other words, it must not have parents). If you wish to run the Task
        /// as a child of another Command (and thus make it responsive to abort requests of that parent), pass a
        /// non-null owner.
        /// Also, note that behavior is undefined if this command is executing at the time this method is called.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type the command returns from SynExecute. If you don't care about the return value, it is safe to
        /// specify Object as the type.
        /// </typeparam>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands do not.
        /// See the concrete command class of interest for details.
        /// </param>
        /// <param name="owner">
        /// If you want this Command to pay attention to abort requests of a different command while running the returned Task,
        /// set this value to that command. Note that the owner assignment is only in effect during the scope of this call. Upon return,
        /// this command will revert to having no owner.
        /// </param>
        /// <returns>
        /// The Task, which will have been started.
        /// </returns>
        public async Task<TResult> RunAsTask<TResult>(object runtimeArg, Command owner)
        {
            CheckDisposed();
            if (ParentInfo != null) { throw new InvalidOperationException("Only top level commands can be run as a Task"); }
            owner?.TakeOwnership(this);

            try
            {
                var taskCompletionSource = new TaskCompletionSource<TResult>();

                if (AbortRequested || (owner != null && owner.AbortRequested))
                {
                    taskCompletionSource.SetCanceled();
                }
                else
                {
                    AsyncExecute(
                        o => taskCompletionSource.SetResult((TResult)o),
                        () => taskCompletionSource.SetCanceled(),
                        e => taskCompletionSource.SetException(e),
                        runtimeArg);
                }

                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                owner?.RelinquishOwnership(this);

                // This may occur on the same thread that the async callback occured on.
                // We must dispose asynchronously.
                await Task.Run(() => { Dispose(); }).ConfigureAwait(false);
            }
        }
        #endregion

        /// <summary>
        /// Returns true if this <see cref="Command"/>'s most efficient form of execution is synchronous.
        /// This information is used on occasion to determine how to best execute a command.
        /// </summary>
        /// <returns>true, if this command is most efficient when run synchronously</returns>
        public abstract bool IsNaturallySynchronous();

        /// <summary>
        /// Call to dispose this command and release any resources that it holds. Only call this on top-level commands (i.e. commands that have no owner)
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// Generic methods are also provided to automatically cast the return value to the expected type.
        /// </returns>
        /// <exception cref="Commands.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// <para>
        /// Some commands expect an argument be passed to SyncExecute(). See the concrete command of interest for details.
        /// If an argument is expected, call one of the overloaded SyncExecute() methods instead.
        /// </para>
        /// </remarks>
        public object SyncExecute()
        {
            return BaseSyncExecute(null, null);
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this parameter.
        /// See the concrete command class of interest for details.
        /// </param>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// Generic methods are also provided to automatically cast the return value to the expected type.
        /// </returns>
        /// <exception cref="Commands.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </remarks>
        public object SyncExecute(object runtimeArg)
        {
            return BaseSyncExecute(runtimeArg, null);
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this parameter.
        /// See the concrete command class of interest for details.
        /// </param>
        /// <param name="owner">
        /// If you want this command to pay attention to abort requests of a different command, set this value to that command.
        /// Note that if this Command is already assigned an owner, passing a non-null value will raise an exception. Also note
        /// that the owner assignment is only in effect during the scope of this call. Upon return, this command will revert to
        /// having no owner.
        /// </param>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// Generic methods are also provided to automatically cast the return value to the expected type.
        /// </returns>
        /// <exception cref="Commands.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </remarks>
        public object SyncExecute(object runtimeArg, Command owner)
        {
            return BaseSyncExecute(runtimeArg, owner);
        }

        /// <summary>
        /// Starts executing the command and returns immediately.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Wait()"/> if you need to block until the command finishes.
        /// <para>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </para>
        /// </remarks>
        /// <param name="onSuccess">
        /// This delegate will be called upon successful completion, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandSucceeded(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        /// <param name="onAbort">
        /// This delegate will be called if this command is aborted, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandAborted(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        /// <param name="onFail">
        /// This delegate will be called if this command fails, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandFailed(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        public void AsyncExecute(Action<object> onSuccess, Action onAbort, Action<Exception> onFail)
        {
            AsyncExecute(onSuccess, onAbort, onFail, null);
        }

        /// <summary>
        /// Starts executing the command and returns immediately.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Wait()"/> if you need to block until the command finishes.
        /// <para>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </para>
        /// </remarks>
        /// <param name="listener">
        /// One of the methods of this interface will be called upon completion, on a separate thread. See the
        /// <see cref="ICommandListener"/> documentation for details.
        /// </param>
        public void AsyncExecute(ICommandListener listener)
        {
            AsyncExecute(listener, null);
        }

        /// <summary>
        /// Starts executing the command and returns immediately.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Wait()"/> if you need to block until the command finishes.
        /// <para>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </para>
        /// </remarks>
        /// <param name="onSuccess">
        /// This delegate will be called upon successful completion, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandSucceeded(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        /// <param name="onAbort">
        /// This delegate will be called if this command is aborted, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandAborted(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        /// <param name="onFail">
        /// This delegate will be called if this command fails, on a separate thread. This delegate
        /// corresponds exactly with ICommandListener.CommandFailed(). See the <see cref="ICommandListener"/>
        /// documentation for details.
        /// </param>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this.
        /// See the concrete command class of interest for details.
        /// </param>
        public void AsyncExecute(Action<object> onSuccess, Action onAbort, Action<Exception> onFail, object runtimeArg)
        {
            AsyncExecute(new DelegateCommandListener<object>(onSuccess, onAbort, onFail), runtimeArg);
        }

        /// <summary>
        /// Starts executing the command and returns immediately.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Wait()"/> if you need to block until the command finishes.
        /// <para>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </para>
        /// </remarks>
        /// <param name="listener">
        /// One of the methods of this interface will be called upon completion, on a separate thread. See the
        /// <see cref="ICommandListener"/> documentation for details.
        /// </param>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this.
        /// See the concrete command class of interest for details.
        /// </param>
        public void AsyncExecute(ICommandListener listener, object runtimeArg)
        {
            CheckDisposed();

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            PreExecute();

            try
            {
                if (Monitors != null)
                {
                    foreach (ICommandMonitor monitor in Monitors)
                    {
                        monitor.CommandStarting(this);
                    }
                }

                var listenerProxy = new ListenerProxy(this, listener);

                try
                {
                    AsyncExecuteImpl(listenerProxy, runtimeArg);
                }
                catch (Exception e)
                {
                    listenerProxy.CommandFailed(e); // deals properly with abort exceptions
                }
            }
            catch (Exception exc)
            {
                DecrementExecuting(null, null, exc, null, null);
                throw;
            }
        }

        /// <summary>Aborts a running command</summary>
        /// <remarks>
        /// This method will have no effect on a command that is not running (nor will it cause a future execution of this command to abort).
        /// Synchronous execution will throw a <see cref="CommandAbortedException"/> if aborted, and asynchronous execution will invoke
        /// <see cref="ICommandListener.CommandAborted"/> on the listener if aborted. Note that if a command is near completion, it may finish
        /// successfully (or fail) before an abort request is processed.
        /// <para>
        /// It is an error to call Abort() on anything other than a top level command. If implementing a Command-derived class and
        /// you need to explicitly abort an owned command (this is unusual), your implementation may call AbortChildCommand().
        /// </para>
        /// </remarks>
        public void Abort() => Abort(mustBeTopLevel:true);

        /// <summary>
        /// Waits for a running command to complete. Will return immediately if the command is not currently executing.
        /// </summary>
        public void Wait()
        {
            Wait(TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Waits a specified duration for a running command to complete. Will return immediately if the command is not currently executing.
        /// </summary>
        /// <param name="duration">The maximum amount of time to wait</param>
        /// <returns>true if the the command completed within 'duration', false otherwise</returns>
        public bool Wait(TimeSpan duration)
        {
            CheckDisposed();
            return _doneEvent.WaitOne(duration);
        }

        /// <summary>
        /// The exact same effect as a call to <see cref="Abort()"/> immediately followed by a call to <see cref="Wait()"/>
        /// </summary>
        public void AbortAndWait()
        {
            AbortAndWait(TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// The exact same effect as a call to <see cref="Abort()"/> immediately followed by a call to <see cref="Wait(TimeSpan)"/>
        /// </summary>
        /// <param name="duration">The maximum amount of time to wait</param>
        /// <returns>true if the the command completed within 'duration', false otherwise</returns>
        public bool AbortAndWait(TimeSpan duration)
        {
            Abort();
            return Wait(duration);
        }

        /// <summary>
        /// Signaled when an abort request has been made. The state of this handle must not be altered
        /// by anything but the framework.
        /// </summary>
        public WaitHandle AbortEvent
        {
            get
            {
                CheckDisposed();
                return _cancelEvent;
            }
        }

        /// <summary>
        /// Signaled when this command has finished execution, regardless of whether it succeeded, failed or was aborted.
        /// The state of this handle must not be altered by anything but the framework.
        /// </summary>
        public WaitHandle DoneEvent
        {
            get
            {
                CheckDisposed();
                return _doneEvent;
            }
        }

        /// <summary>
        /// Resets the abort event of a command that is owned by this command. Derived implementations may need to call this
        /// execute a child command regardless of whether its owner was aborted. This method only exists for special cases.
        /// </summary>
        /// <param name="childCommand">The owned command. This must be an immediate child (not a grandchild, for example).</param>
        protected void ResetChildAbortEvent(Command childCommand)
        {
            if (!ReferenceEquals(childCommand.ParentInfo, this))
            {
                throw new ArgumentException("Only immediate children may be passed as an argument to ResetChildAbortEvent", nameof(childCommand));
            }

            childCommand._cancelEvent.Reset();

            lock (childCommand._childLock)
            {
                foreach (Command cmd in childCommand._children)
                {
                    childCommand.ResetChildAbortEvent(cmd);
                }
            }
        }

        /// <summary>
        /// Aborts a command that is owned by this command. Derived implementations may need to call this to halt an owned
        /// command's execution without effecting the execution state of the owning command. Note that when a command is
        /// aborted via normal means (via <see cref="Command.Abort()"/>), all of its owned commands are also aborted. This
        /// method only exists for special cases.
        /// </summary>
        /// <param name="childCommand">The owned command. This must be an immediate child (not a grandchild, for example).</param>
        protected void AbortChildCommand(Command childCommand)
        {
            if (!ReferenceEquals(childCommand.ParentInfo, this))
            {
                throw new ArgumentException("Only immediate children may be passed as an argument to AbortChildCommand", nameof(childCommand));
            }

            childCommand.Abort(false);
        }

        /// <summary>
        /// Returns whether an abort request has been made
        /// </summary>
        /// <returns>true, if an abort request has been made.</returns>
        protected bool AbortRequested => AbortEvent.WaitOne(0);

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <param name="runtimeArg">The implementation of the command defines what this value should be (if it's interested).</param>
        /// <returns>The implementation of the command defines what this value will be</returns>
        /// <remarks>
        /// If a command is aborted, implementations should throw a <see cref="CommandAbortedException"/>. Implementations may do so by either periodically
        /// calling <see cref="CheckAbortFlag"/>, or by implementing this method via calls to owned commands. In rare cases, <see cref="AbortImpl"/>
        /// may need to be overridden.
        /// <para>
        /// If a concrete Command class is most naturally implemented in asynchronous fashion, it should inherit from <see cref="AsyncCommand"/>.
        /// That class takes care of implementing SyncExecuteImpl().
        /// </para>
        /// </remarks>
        protected abstract object SyncExecuteImpl(object runtimeArg);

        /// <summary>Starts executing the command and returns immediately.</summary>
        /// <param name="listener">One of the methods of the listener will be called upon command completion, on a separate thread.</param>
        /// <param name="runtimeArg">The implementation of the command defines what this value should be (if it's interested).</param>
        /// <remarks>
        /// Implementations will likely need to override <see cref="AbortImpl"/> in order to respond to abort requests in a timely manner.
        /// <para>
        /// If a concrete Command class is most naturally implemented in synchronous fashion, it should inherit from <see cref="SyncCommand"/>.
        /// That class takes care of implementing AsyncExecuteImpl().
        /// </para>
        /// </remarks>
        protected abstract void AsyncExecuteImpl(ICommandListener listener, object runtimeArg);

        /// <summary>Implementations should override if there's something in particular they can do to more effectively respond to an abort request.</summary>
        /// <remarks>
        /// Note that <see cref="AsyncCommand"/>-derived classes are most-likely to need to override this method. <see cref="SyncCommand"/>-derived classes will
        /// typically not need to override this method, instead calling <see cref="CheckAbortFlag"/> periodically, and/or passing work off to owned commands,
        /// which themselves will respond to abort requests.
        /// <para>
        /// Implementations of this method must be asynchronous, and must not throw. Do not wait for the command to fully abort,
        /// or a deadlock possibility will arise.
        /// </para>
        /// </remarks>
        protected virtual void AbortImpl()
        {
        }

        /// <summary>Construct a Command</summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// <para>Note that it is also possible to set the owner at a later time via <see cref="Command.TakeOwnership"/></para>
        /// </param>
        protected Command(Command owner)
        {
            owner?.TakeOwnership(this);
            Id = Interlocked.Increment(ref _nextId);
        }

        /// <summary>
        /// Make this command the owner of the command passed as an argument. An owner is responsible
        /// for disposing its owned commands. An owned command honors abort requests made of its parent.
        /// </summary>
        /// <param name="orphan">
        /// The command to be owned. Only un-owned commands can take a new owner. Allowing
        /// other types of owner transfer would invite misuse and the bad behavior that results
        /// (e.g. adding the same Command instance to <see cref="SequentialCommands"/> and <see cref="ParallelCommands"/>).
        /// </param>
        protected void TakeOwnership(Command orphan)
        {
            lock (_childLock)
            {
                if (orphan._owner != null)
                {
                    throw new InvalidOperationException("Attempt to assume ownership of a command that already has an owner.");
                }

                // This creates a circular reference, which would be a problem in a language
                // like C++, but garbage collection does not use reference counting, so we
                // should be good. Maintaining children and owner simplifies management
                // of abort and wait operations.
                orphan._owner = this;
                _children.Add(orphan);
            }
        }

        /// <summary>Makes what used to be an owned command a top-level command.</summary>
        /// <remarks>The caller of this method must be responsible for ensuring that the relinquished command is properly disposed.</remarks>
        /// <param name="command">The command to relinquish ownership. Note that it must currently be a direct child command of this object (not a grandchild, for example)</param>
        protected void RelinquishOwnership(Command command)
        {
            lock (_childLock)
            {
                bool removed = _children.Remove(command);

                if (!removed)
                {
                    throw new InvalidOperationException("Attempt to relinquish ownership of a command that is not directly owned by this object.");
                }

                command._owner = null;
            }
        }

        /// <summary>
        /// Throws a <see cref="CommandAbortedException"/> if an abort is pending. Synchronous implementations may find this useful in
        /// order to respond to an abort request in a timely manner.
        /// </summary>
        protected void CheckAbortFlag()
        {
            if (_cancelEvent.WaitOne(0))
            {
                throw new CommandAbortedException();
            }
        }

        /// <summary>
        /// Implementations should override only if they contain members that must be disposed. Remember to invoke the base class implementation from within any override.
        /// </summary>
        /// <param name="disposing">Will be true if this was called as a direct result of the object being explicitly disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    if (_owner != null)
                    {
                        System.Diagnostics.Debug.Print("Dispose() called for an owned Command. Dispose() should only be called for top-level commands.");
                        return;
                    }

                    // Even though this command may have informed us that it is done by now, it still may not have signaled its done
                    // event. That signal must be complete for this command to be considered truly done and disposable.
                    _doneEvent.WaitOne();

                    // ReSharper disable once InconsistentlySynchronizedField
                    foreach (Command child in _children)
                    {
                        // Avoid the above exception
                        child._owner = null;
                        child._cancelEvent.Dispose();
                        child.Dispose();
                    }

                    _cancelEvent.Dispose();
                    _doneEvent.Dispose();
                    _abortThreadId.Dispose();
                }

                Disposed = true;
            }
        }

        /// <summary>Helper property for implementations that need to override <see cref="Dispose(bool)"/></summary>
        protected bool Disposed { get; private set; }

        /// <summary>Throws an <see cref="ObjectDisposedException"/> if this object has been disposed.</summary>
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~Command()
        {
            Dispose(false);
        }

        private static Command GetParent(Command command)
        {
            return command._owner;
        }

        private static void AbortImplAllDescendents(Command command)
        {
            lock (command._childLock)
            {
                foreach (Command child in command._children)
                {
                    child._cancelEvent.Set();
                    AbortImplAllDescendents(child);
                    child.AbortImpl();
                }
            }
        }

        private static void ResetCancelEvents(Command command)
        {
            command._cancelEvent.Reset();

            lock (command._childLock)
            {
                foreach (Command child in command._children)
                {
                    ResetCancelEvents(child);
                }
            }
        }

        private void Abort(bool mustBeTopLevel)
        {
            CheckDisposed();

            try
            {
                if (mustBeTopLevel && _owner != null)
                {
                    throw new InvalidOperationException("Abort may only be called on a top level command.");
                }

                try
                {
                    _abortThreadId.Value = Thread.CurrentThread.ManagedThreadId;
                    _cancelEvent.Set();
                    AbortImplAllDescendents(this);
                    AbortImpl();
                }
                finally
                {
                    _abortThreadId.Value = null;
                }
            }
            catch (Exception exc)
            {
                AttachErrorInfo(exc);
                throw;
            }
        }

        private object BaseSyncExecute(object runtimeArg, Command owner)
        {
            CheckDisposed();
            PreExecute();
            owner?.TakeOwnership(this);

            try
            {
                if (Monitors != null)
                {
                    foreach (ICommandMonitor monitor in Monitors)
                    {
                        monitor.CommandStarting(this);
                    }
                }

                owner?.CheckAbortFlag();
                object result = SyncExecuteImpl(runtimeArg);
                DecrementExecuting(null, null, null, null, null);
                return result;
            }
            catch (Exception exc)
            {
                DecrementExecuting(null, null, exc, null, null);
                throw;
            }
            finally
            {
                owner?.RelinquishOwnership(this);
            }
        }

        private void AttachErrorInfo(Exception exc)
        {
            if (!exc.Data.Contains("CommandContext"))
            {
                exc.Data.Add("CommandContext", Description);
            }
        }

        private void PreExecute()
        {
            // Asynchronously launched commands inform their listener that they are done just before they signal the done event.
            // Owner commands may trigger off these callbacks to help determine when it itself is done (ParallelCommands is an example).
            // When the owner command is done, it will raise its done event, thus signaling the user that it may be relaunched or
            // even disposed. However, the children might not have gotten around yet to signaling their own done events. Thus
            // we take care of that wiggle room here.
            _doneEvent.WaitOne();
            _doneEvent.Reset();

            // Only reset the cancel events when the top level command is executed. Otherwise, child commands that are eventually
            // run as part of the top level command could have their cancel events reset after the top level operation was aborted.
            if (_owner == null)
            {
                ResetCancelEvents(this);
            }

            Interlocked.Increment(ref _executing);
        }

        private void DecrementExecuting(
            ICommandListener listener,
            object result,
            Exception exc,
            int? asyncExeThreadId,
            ThreadLocal<int?> abortThreadId)
        {
            int refCount = Interlocked.Decrement(ref _executing);

            if (refCount < 0)
            {
                Interlocked.Increment(ref _executing);
                throw new InvalidOperationException("Attempt to inform an idle command that it is no longer executing. Command: " + Description);
            }

            if (refCount == 0)
            {
                if ((asyncExeThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == asyncExeThreadId.Value) ||
                    (abortThreadId != null && abortThreadId.IsValueCreated && abortThreadId.Value != null && Thread.CurrentThread.ManagedThreadId == abortThreadId.Value.Value))
                {
                    // Not ideal usage, but permitted.
                    // We must call the listener back asynchronously. That's the contract.
                    ThreadPool.QueueUserWorkItem(ExecuteAsyncRoutine, new AsyncThreadArg(listener, result, exc));
                }
                else
                {
                    InformDone(listener, result, exc);
                }
            }
        }

        private class AsyncThreadArg
        {
            internal AsyncThreadArg(ICommandListener listener, object result, Exception exc)
            {
                Listener = listener;
                Result = result;
                Exception = exc;
            }

            internal ICommandListener Listener { get; }
            internal object Result { get; }
            internal Exception Exception { get; }
        }

        private void ExecuteAsyncRoutine(object arg)
        {
            AsyncThreadArg threadArg = (AsyncThreadArg)arg;
            InformDone(threadArg.Listener, threadArg.Result, threadArg.Exception);
        }

        private void InformDone(ICommandListener listener, object result, Exception exc)
        {
            bool aborted = exc is CommandAbortedException;

            if (exc != null && !aborted)
            {
                AttachErrorInfo(exc);
            }
            
            if (Monitors != null)
            {
                foreach (ICommandMonitor monitor in Monitors)
                {
                    monitor.CommandFinished(this, exc);
                }
            }

            if (listener != null)
            {
                if (exc == null)
                {
                    listener.CommandSucceeded(result);
                }
                else if (aborted)
                {
                    listener.CommandAborted();
                }
                else
                {
                    listener.CommandFailed(exc);
                }
            }

            _doneEvent.Set();
        }

        private class ListenerProxy : ICommandListener
        {
            internal ListenerProxy(Command command, ICommandListener listener)
            {
                _command = command;
                _listener = listener;
                _asyncExeThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public void CommandSucceeded(object result)
            {
                _command.DecrementExecuting(_listener, result, null, _asyncExeThreadId, _command._abortThreadId);
            }

            public void CommandAborted()
            {
                _command.DecrementExecuting(_listener, null, new CommandAbortedException(), _asyncExeThreadId, _command._abortThreadId);
            }

            public void CommandFailed(Exception exc)
            {
                _command.DecrementExecuting(_listener, null, exc, _asyncExeThreadId, _command._abortThreadId);
            }

            private readonly Command _command;
            private readonly ICommandListener _listener;
            private readonly int _asyncExeThreadId;
        }

        private volatile Command _owner;
        private readonly HashSet<Command> _children = new HashSet<Command>();
        private volatile int _executing;
        private readonly ThreadLocal<int?> _abortThreadId = new ThreadLocal<int?>();
        private readonly ManualResetEvent _doneEvent = new ManualResetEvent(true);
        private readonly ManualResetEvent _cancelEvent = new ManualResetEvent(false);
        private readonly object _childLock = new object();

        private static volatile int _nextId;
    }
}
