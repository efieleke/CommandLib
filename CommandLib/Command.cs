using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLib
{
    /// <summary>
    /// CommandLib contains a set of classes that can be used to easily coordinate synchronous and asynchronous activities in
    /// complex ways. Most classes in this library inherit from <see cref="Command"/>, which represents an action. Any
    /// <see cref="Command"/> can be run synchronously or asynchronously, and may be aborted.
    /// <para>
    /// Using <see cref="ParallelCommands"/>, you can run a collection of <see cref="Command"/> objects concurrently, and using
    /// <see cref="SequentialCommands"/>, you can run a collection of <see cref="Command"/> objects in sequence. Any command
    /// can be added to these two types (including <see cref="ParallelCommands"/> and <see cref="SequentialCommands"/> themselves,
    /// because they are <see cref="Command"/> objects), so it's possible to create a deep nesting of coordinated activities.
    /// </para>
    /// <para>
    /// <see cref="PeriodicCommand"/> repeats its action at a given interval, <see cref="ScheduledCommand"/> runs once at a specific
    /// time, and <see cref="RecurringCommand"/> runs at times that are provided via a callback.
    /// </para>
    /// <para>
    /// <see cref="RetryableCommand"/> provides the option to keep retrying a failed command until the caller decides enough is enough,
    /// and <see cref="TimeLimitedCommand"/> fails with a timeout exception if a given duration elapses before the command finishes
    /// execution.
    /// </para>
    /// <para>
    /// <see cref="CommandDispatcher"/> provides the capability to set up a pool for command execution.
    /// </para>
    /// <para>
    /// All of the above <see cref="Command"/> classes are simply containers for <see cref="Command"/> objects that presumably do
    /// something of interest. CommandLib includes a few <see cref="Command"/> classes that might be commonly useful, including
    /// <see cref="PauseCommand"/>, <see cref="DownloadFileCommand"/>, <see cref="DownloadStringCommand"/> and
    /// <see cref="UploadDataCommand"/>, but it is expected that users of this library will create their own
    /// <see cref="Command"/>-derived classes.
    /// </para>
    /// <para>
    /// Documentation for <see cref="Command"/>, <see cref="AsyncCommand"/> and <see cref="SyncCommand"/> should be read before
    /// developing a <see cref="Command"/>-derived class. <see cref="VariableCommand"/> and <see cref="AbortEventedCommand"/> might also
    /// serve as aids in the development of a <see cref="Command"/>.
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }

    /// <summary>
    /// Informational data that is part of a <see cref="Command"/> instance
    /// </summary>
    public interface ICommandInfo
    {
        /// <summary>
        /// Returns the unique identifier for this command.
        /// </summary>
        long Id
        {
            get;
        }

        /// <summary>
        /// Returns the owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).
        /// </summary>
        ICommandInfo ParentInfo
        {
            get;
        }

        /// <summary>
        /// Counts the number of parents until the top level command is reached
        /// </summary>
        /// <remarks>A parent is considered an owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).</remarks>
        int Depth
        {
            get;
        }

        /// <summary>A description of the Command</summary>
        /// <remarks>
        /// This is the name of the concrete class of this command, preceded by the names of the classes of each parent,
        /// up to the top-level parent. This is followed with this command's unique id (for example, 'SequentialCommands=>PauseCommand(23)').
        /// The description ends with details of about the current state of the command, if available.
        /// <para>
        /// A parent is considered the owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).
        /// </para>
        /// </remarks>
        String Description
        {
            get;
        }

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
        String ExtendedDescription();
    }

    /// <summary>Represents an action that can be run synchronously or asynchronously.</summary>
    /// <remarks>
    /// Commands are abortable. Even a synchronously running command can be aborted from a separate thread.
    /// <para>
    /// Commands all inherit from IDisposable, but Dispose() should only be called on top level commands. A top-level
    /// command is a Command that is not owned by another Command. Some Command-derived classes will take ownership
    /// of Command objects passed as arguments to certain methods. Such behavior is documented.
    /// </para>
    /// <para>
    /// When developing a Command subclass, be sure to inherit from either <see cref="SyncCommand"/> (if your command is naturally
    /// synchronous in its implementation) or <see cref="AsyncCommand"/>. Those classes take care of the unnatural implementations
    /// (<see cref="SyncCommand"/> implements <see cref="AsyncExecuteImpl"/>, and <see cref="AsyncCommand"/> implements
    /// <see cref="SyncExecuteImpl"/>).
    /// </para>
    /// <para>
    /// Also, when developing a Command subclass, make sure that any member variables that are Commands are properly
    /// owned by passing 'this' as the owner field when constructing the command. The main advantages of doing this
    /// are 1) owned commands will automatically respond to abort requests issued to the owner, and 2) the owner
    /// will automatically call Dispose() on its owned commands when it itself is disposed.
    /// </para>
    /// <para>
    /// If you write a method that accepts a Command as an argument, you may wish to assume ownership of that Command.
    /// <see cref="TakeOwnership"/> allows you to do this. The <see cref="SequentialCommands.Add"/> member of
    /// <see cref="SequentialCommands"/> is an example of this behavior.
    /// </para>
    /// <para>
    /// If you find that you are generating a temporary Command object within the execution method of a parent
    /// command, it's best to not specify the parent as the owner of this temporary command. Owned commands are
    /// not disposed until the parent is disposed, so if this parent is executed many times before it is disposed,
    /// it's possible for resource usage to grow unbounded. The better approach is to assign this temporary command
    /// to a <see cref="VariableCommand"/> object, which would be a member variable of the parent. The assignment will take
    /// care of disposing any previously assigned command.
    /// </para>
    /// <para>
    /// If you would like to create a top level command that responds to abort requests to a different command,
    /// call <see cref="CreateAbortLinkedCommand"/>. The use cases of this would be rare, but it can help when command
    /// objects must be more loosely coupled.
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
        public static String GetAttachedErrorInfo(Exception exc)
        {
            if (exc.Data.Contains("CommandContext"))
            {
                return (String)exc.Data["CommandContext"];
            }

            return null;
        }

#region ICommandInfo
        /// <summary>
        /// Returns the unique identifier for this command.
        /// </summary>
        public long Id
        {
            get { return id; }
        }

        /// <summary>
        /// Returns the owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).
        /// </summary>
        public ICommandInfo ParentInfo
        {
            get
            {
                return Parent;
            }
        }

        /// <summary>
        /// Counts the number of parents until the top level command is reached
        /// </summary>
        /// <remarks>A parent is considered an owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).</remarks>
        public int Depth
        {
            get
            {
                CheckDisposed();
                int result = 0;

                for (Command parent = GetParent(this); parent != null; parent = GetParent(parent))
                {
                    ++result;
                }

                return result;
            }
        }

        /// <summary>A description of the Command</summary>
        /// <remarks>
        /// This is the name of the concrete class of this command, preceded by the names of the classes of each parent,
        /// up to the top-level parent. This is followed with this command's unique id (for example, 'SequentialCommands=>PauseCommand(23)').
        /// The description ends with details of about the current state of the command, if available.
        /// <para>
        /// A parent is considered the owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).
        /// </para>
        /// </remarks>
        public String Description
        {
            get
            {
                CheckDisposed();
                String result = GetType().Name;

                for (Command parent = GetParent(this); parent != null; parent = GetParent(parent))
                {
                    result = parent.GetType().Name + "=>" + result;
                }

                result += "(" + id.ToString() + ")";
                String extendedDescription = ExtendedDescription();

                if (!String.IsNullOrWhiteSpace(extendedDescription))
                {
                    result += " " + extendedDescription;
                }

                return result;
            }
        }

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
        public virtual String ExtendedDescription()
        {
            return "";
        }
#endregion

        /// <summary>
        /// Call to dispose this command and release any resources that it holds. Only call this on top-level commands (i.e. commands that have no owner)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            // This is the line that generated CA1063. But I like having this because I want to know if I'm doubly disposing objects.
            // I'm open to being convinced that this is a bad idea, however. Perhaps most objects should be allowed to be
            // doubly disposed, and they would just do the right thing. (I find it simpler to guard against that up front.)
            CheckDisposed();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// Generic methods are also provided to automatically cast the return value to the expected type.
        /// </returns>
        /// <exception cref="CommandLib.CommandAbortedException">Thrown when execution is aborted</exception>
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
        public Object SyncExecute()
        {
            return BaseSyncExecute(null);
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
        /// <exception cref="CommandLib.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </remarks>
        public Object SyncExecute(Object runtimeArg)
        {
            return BaseSyncExecute(runtimeArg);
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <typeparam name="R">The expected return type. An exception will be thrown if the actual return type cannot be cast to this type.</typeparam>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// </returns>
        /// <exception cref="CommandLib.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// <para>
        /// Some commands expect an argument be passed to SyncExecute. See the concrete command of interest for details.
        /// If an argument is expected, call one of the overloaded SyncExecute() methods instead.
        /// </para>
        /// </remarks>
        public R SyncExecute<R>()
        {
            return (R)BaseSyncExecute(null);
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <typeparam name="R">The expected return type. An exception will be thrown if the actual return type cannot be cast to this type.</typeparam>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this parameter.
        /// See the concrete command class of interest for details.
        /// </param>
        /// <returns>
        /// Concrete commands may return a value of interest. See the concrete command class for details.
        /// </returns>
        /// <exception cref="CommandLib.CommandAbortedException">Thrown when execution is aborted</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if called after this object has been disposed</exception>
        /// <exception cref="System.Exception">
        /// Thrown if execution does not complete successfully. Call <see cref="GetAttachedErrorInfo"/> to retrieve context information
        /// about the command that was running at the time the exception was thrown.
        /// </exception>
        /// <remarks>
        /// It is safe to call this any number of times, but it will cause undefined behavior to re-execute a
        /// command that is already executing.
        /// </remarks>
        public R SyncExecute<R>(Object runtimeArg)
        {
            return (R)BaseSyncExecute(runtimeArg);
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
        /// <para>
        /// If the execution of the command expects an argument, call <see cref="AsyncExecute(ICommandListener, object)"/> instead.
        /// </para>
        /// </remarks>
        /// <param name="listener">
        /// One of the methods of this interface will be called upon completion, on a separate thread. See the
        /// <see cref="ICommandListener"/> documentation for details.
        /// </param>
        public void AsyncExecute(ICommandListener listener)
        {
            // I'm considering having this method return an IAsyncResult, but I'm not sure doing so would really offer anything of value.
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
        /// <param name="listener">
        /// One of the methods of this interface will be called upon completion, on a separate thread. See the
        /// <see cref="ICommandListener"/> documentation for details.
        /// </param>
        /// <param name="runtimeArg">
        /// Some commands may expect some sort of argument at the time of execution, and some commands may ignore this.
        /// See the concrete command class of interest for details.
        /// </param>
        public void AsyncExecute(ICommandListener listener, Object runtimeArg)
        {
            // I'm considering having this method return an IAsyncResult, but I'm not sure doing so would really offer anything of value.
            CheckDisposed();
            
            if (listener == null)
            {
                throw new ArgumentNullException("listener must not be null");
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

                AsyncExecuteImpl(new ListenerProxy(this, listener), runtimeArg);
            }
            catch (Exception exc)
            {
                DecrementExecuting(null, null, exc);
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
        /// It is an error to call Abort() on anything other than a top level command.
        /// </para>
        /// </remarks>
        public void Abort()
        {
            try
            {
                CheckDisposed();

                // Calling Abort on a child command makes no sense, because children follow the parent in this regard.
                if (owner != null)
                {
                    throw new InvalidOperationException("Abort can only be called on top-level commands");
                }

                abortEvent.Set();

                lock (childLock)
                {
                    AbortImplAllDescendents(this);
                }

                AbortImpl();
            }
            catch (Exception exc)
            {
                AttachErrorInfo(exc);
                throw;
            }
        }

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
            return doneEvent.WaitOne(duration);
        }

        /// <summary>
        /// The exact same effect as a call to <see cref="Abort"/> immediately followed by a call to <see cref="Wait()"/>
        /// </summary>
        public void AbortAndWait()
        {
            AbortAndWait(TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// The exact same effect as a call to <see cref="Abort"/> immediately followed by a call to <see cref="Wait(TimeSpan)"/>
        /// </summary>
        /// <param name="duration">The maximum amount of time to wait</param>
        /// <returns>true if the the command completed within 'duration', false otherwise</returns>
        public bool AbortAndWait(TimeSpan duration)
        {
            Abort();
            return Wait(duration);
        }

        /// <summary>Creates a top-level command that nevertheless responds to abort requests made of this command.</summary>
        /// <param name="commandToLink">
        /// The command to run. The returned <see cref="AbortEventedCommand"/> object takes ownership of this argument, so the passed
        /// command must not already have an owner. The passed command will be disposed when the <see cref="AbortEventedCommand"/> is
        /// disposed.
        /// </param>
        /// <returns>An AbortEventedCommand.</returns>
        /// <remarks>
        /// Note that the linked command is only linked with regards to abort requests (not execution requests, or anything else),
        /// and that the link is one-way. If <see cref="Abort"/> is called on the linked command, this Command object will not respond to that.
        /// </remarks>
        public AbortEventedCommand CreateAbortLinkedCommand(Command commandToLink)
        {
            CheckDisposed();
            return new AbortEventedCommand(commandToLink, this);
        }

        /// <summary>
        /// Returns the owner, or the command that an <see cref="AbortEventedCommand"/> is linked to (if any).
        /// </summary>
        public Command Parent
        {
            get
            {
                CheckDisposed();
                return GetParent(this);
            }
        }

        /// <summary>
        /// Signaled when this command is to be aborted. Note that this event is only reset when the command next begins execution.
        /// </summary>
        public System.Threading.WaitHandle AbortEvent
        {
            get
            {
                CheckDisposed();
                return abortEvent;
            }
        }

        /// <summary>
        /// Signaled when this command has finished execution, regardless of whether it succeeded, failed or was aborted.
        /// </summary>
        public System.Threading.WaitHandle DoneEvent
        {
            get
            {
                CheckDisposed();
                return doneEvent;
            }
        }

        /// <summary>Executes the command and does not return until it finishes.</summary>
        /// <param name="runtimeArg">The implementation of the command defines what this value should be (if it's interested).</param>
        /// <returns>The implementation of the command defines what this value will be</returns>
        /// <remarks>
        /// If a command is aborted, implementations should throw a <see cref="CommandAbortedException"/>. Implementations may do so by either periodically
        /// calling <see cref="CheckAbortFlag"/>, or by implementating this method via calls to owned commands. In rare cases, <see cref="AbortImpl"/>
        /// may need to be overridden.
        /// <para>
        /// If a concrete Command class is most naturally implemented in asynchronous fashion, it should inherit from <see cref="AsyncCommand"/>.
        /// That class takes care of implementing SyncExecuteImpl().
        /// </para>
        /// </remarks>
        protected abstract Object SyncExecuteImpl(Object runtimeArg);

        /// <summary>Starts executing the command and returns immediately.</summary>
        /// <param name="listener">One of the methods of the listener will be called upon command completion, on a separate thread.</param>
        /// <param name="runtimeArg">The implementation of the command defines what this value should be (if it's interested).</param>
        /// <remarks>
        /// Implementions must invoke one of the listener methods on a thread other than the one this method was called from when execution finishes.
        /// Also, implementations will likely need to override <see cref="AbortImpl"/> in order to respond to abort requests in a timely manner.
        /// <para>
        /// If a concrete Command class is most naturally implemented in synchronous fashion, it should inherit from <see cref="SyncCommand"/>.
        /// That class takes care of implementing AsyncExecuteImpl().
        /// </para>
        /// </remarks>
        protected abstract void AsyncExecuteImpl(ICommandListener listener, Object runtimeArg);

        /// <summary>Implementations should override if there's something in particular they can do to more effectively respond to an abort request.</summary>
        /// <remarks>
        /// Note that <see cref="AsyncCommand"/>-derived classes are likely to need to override this method. <see cref="SyncCommand"/>-derived classes will
        /// typically not need to override this method, instead calling <see cref="CheckAbortFlag"/> periodically, and/or passing work off to owned commands,
        /// which themselves will respond to abort requests.
        /// <para>
        /// Implementations of this method must be asynchronous. Do not wait for the command to fully abort, or a deadlock possibility will arise.
        /// </para>
        /// </remarks>
        protected virtual void AbortImpl()
        {
        }

        /// <summary>Construct a Command</summary>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        protected Command(Command owner)
        {
            if (owner == null)
            {
                abortEvent = new System.Threading.ManualResetEvent(false);
            }
            else
            {
                owner.TakeOwnership(this);
            }

            id = System.Threading.Interlocked.Increment(ref nextId);
        }

        /// <summary>Make this command the owner of the command passed as an argument.</summary>
        /// <param name="orphan">
        /// The command to be owned. Only un-owned commands can take a new owner. Allowing
        /// other types of owner transfer would invite misuse and the bad behavior that results
        /// (e.g. adding the same Command instance to <see cref="SequentialCommands"/> and <see cref="ParallelCommands"/>).
        /// </param>
        protected void TakeOwnership(Command orphan)
        {
            if (orphan.MustBeTopLevel())
            {
                throw new InvalidOperationException(String.Format("{0} objects may only be top level", orphan.GetType().FullName));
            }

            lock (childLock)
            {
                if (orphan.owner != null)
                {
                    throw new InvalidOperationException("Attempt to assume ownership of a command that already has an owner.");
                }

                // Only top level owners have the abort event. All of its children share the
                // same single event. Besides saving a bit on resources, it really helps make
                // synchronization easier around aborts.

                // Get rid of the extraneous abort event if one has been created.
                if (orphan.abortEvent != null)
                {
                    orphan.abortEvent.Dispose();
                }

                // Make the orphan and its children share this same abort event.
                SetAbortEvent(orphan);

                // This creates a circular reference, which would be a problem in a language
                // like C++, but garbage collection does not use reference counting, so we
                // should be good. Maintaining children and owner simplifies management
                // of abort and wait operations.
                orphan.owner = this;
                children.Add(orphan);
            }
        }

        /// <summary>Makes what used to be an owned command a top-level command.</summary>
        /// <remarks>The caller of this method must be responsible for ensuring that the relinquished command is properly disposed.</remarks>
        /// <param name="command">The command to relinquish ownership. Note that it must currently be a direct child command of this object (not a grandchild, for example)</param>
        protected void RelinquishOwnership(Command command)
        {
            lock (childLock)
            {
                bool removed = children.Remove(command);

                if (!removed)
                {
                    throw new InvalidOperationException("Attempt to relinquish ownership of a command that is not directly owned by this object.");
                }

                command.owner = null;
                command.abortEvent = new System.Threading.ManualResetEvent(false);

                foreach (Command child in command.children)
                {
                    command.SetAbortEvent(child);
                }
            }
        }

        /// <summary>
        /// Throws a <see cref="CommandAbortedException"/> if an abort is pending. Synchronous implementations may find this useful in
        /// order to respond to an abort request in a timely manner.
        /// </summary>
        protected void CheckAbortFlag()
        {
            if (abortEvent.WaitOne(0))
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
            if (!disposed)
            {
                if (disposing)
                {
                    if (owner != null)
                    {
                        System.Diagnostics.Debug.Print("Dispose() called for an un-owned Command. Dispose() should only be called for top-level commands. " +
                            "If you need a temporary Command that responds to abort requests of a different Command, consider using an AbortEventedCommand.");

                        return;
                    }

                    // Even though this command may have informed us that it is done by now, it still may not have signaled its done
                    // event. That signal must be complete for this command to be considered truly done and disposable.
                    doneEvent.WaitOne();

                    foreach (Command child in children)
                    {
                        // Avoid the above exception, and avoid doubly disposing abortEvent (because only the parent has the "real" copy)
                        child.owner = null;
                        child.abortEvent = null;
                        child.Dispose();
                    }

                    if (abortEvent != null)
                    {
                        abortEvent.Dispose();
                    }

                    doneEvent.Dispose();
                }

                disposed = true;
            }
        }

        /// <summary>Helper property for implementations that need to override <see cref="Dispose(bool)"/></summary>
        protected bool Disposed { get { return disposed; } }

        /// <summary>Throws an <see cref="ObjectDisposedException"/> if this object has been disposed.</summary>
        protected void CheckDisposed()
        {
            if (disposed)
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

        /// <summary>
        /// Implementations should override to return false if their Command class must never be owned by another Command.
        /// This is expected to be a rare restriction. Within CommandLib, only <see cref="AbortEventedCommand"/> has this restriction.
        /// </summary>
        /// <returns>true if the Command subclass must be top level. Default is false.</returns>
        protected virtual bool MustBeTopLevel()
        {
            return false;
        }

        private static Command GetParent(Command command)
        {
            if (command.owner != null)
            {
                return command.owner;
            }

            if (command is AbortEventedCommand)
            {
                AbortEventedCommand abortEventedCommand = (AbortEventedCommand)command;
                return abortEventedCommand.CommandToWatch;
            }

            return null;
        }

        private void SetAbortEvent(Command target)
        {
            target.abortEvent = abortEvent;

            foreach (Command child in target.children)
            {
                target.SetAbortEvent(child);
            }
        }

        private static void AbortImplAllDescendents(Command command)
        {
            foreach (Command child in command.children)
            {
                AbortImplAllDescendents(child);
                child.AbortImpl();
            }
        }

        private Object BaseSyncExecute(Object runtimeArg)
        {
            CheckDisposed();
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

                Object result = SyncExecuteImpl(runtimeArg);
                DecrementExecuting(null, null, null);
                return result;
            }
            catch (Exception exc)
            {
                DecrementExecuting(null, null, exc);
                throw;
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
            doneEvent.WaitOne();
            doneEvent.Reset();

            // Don't reset the abort event for launched child commands
            if (owner == null)
            {
                abortEvent.Reset();
            }

            System.Threading.Interlocked.Increment(ref executing);
        }

        private void DecrementExecuting(ICommandListener listener, Object result, Exception exc)
        {
            int refCount = System.Threading.Interlocked.Decrement(ref executing);

            if (refCount < 0)
            {
                System.Threading.Interlocked.Increment(ref executing);
                throw new InvalidOperationException("Attempt to inform an idle command that it is no longer executing. Command: " + Description);
            }

            if (refCount == 0)
            {
                bool aborted = exc != null && exc is CommandAbortedException;
                
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

                doneEvent.Set();
            }
        }

        private class ListenerProxy : ICommandListener
        {
            internal ListenerProxy(Command command, ICommandListener listener)
            {
                this.command = command;
                this.listener = listener;
                asyncExeThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }

            public void CommandSucceeded(object result)
            {
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == asyncExeThreadId)
                {
                    throw new InvalidOperationException("ICommandListener.CommandSucceeded called on same thread as AsyncExecute()");
                }

                command.DecrementExecuting(listener, result, null);
            }

            public void CommandAborted()
            {
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == asyncExeThreadId)
                {
                    throw new InvalidOperationException("ICommandListener.CommandAborted called on same thread as AsyncExecute()");
                }

                command.DecrementExecuting(listener, null, new CommandAbortedException());
            }

            public void CommandFailed(Exception exc)
            {
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == asyncExeThreadId)
                {
                    throw new InvalidOperationException("ICommandListener.CommandFailed called on same thread as AsyncExecute()");
                }

                command.DecrementExecuting(listener, null, exc);
            }

            private Command command;
            private ICommandListener listener;
            private int asyncExeThreadId;
        }

        private readonly long id;
        private Command owner = null;
        private HashSet<Command> children = new HashSet<Command>();
        private int executing = 0;
        private System.Threading.ManualResetEvent doneEvent = new System.Threading.ManualResetEvent(true);
        private System.Threading.ManualResetEvent abortEvent = null;
        private bool disposed = false;
        private Object childLock = new Object();

        static long nextId = 0;
    }
}
