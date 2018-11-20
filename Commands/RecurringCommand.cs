using System;

namespace Sophos.Commands
{
	/// <summary>Represents a <see cref="Command"/> that repeatedly executes at times specified by the caller</summary>
	/// <remarks>
	/// <para>
	/// The runtimeArg passed to the execution methods will be passed to the command that executes, every time that it executes.
	/// The runtimeArg should be of the same type required as the underlying command to run.
	/// </para>
	/// <para>
	/// This command returns null from synchronous execution, and sets the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> to null.
	/// </para>
	/// <para>
	/// If the interval between execution times is fixed, it would be simpler to use <see cref="PeriodicCommand"/> instead.
	/// </para>
	/// </remarks>
	public class RecurringCommand : SyncCommand
    {
        /// <summary>
        /// Defines at what times the underlying command executes
        /// </summary>
        public interface IExecutionTimeCallback
        {
            /// <summary>
            /// Called when a <see cref="RecurringCommand"/> needs to know the first time to execute its underlying command to run.
            /// </summary>
            /// <param name="time">
            /// Implementations should set this to the first time to execute. If a time in the past is specified, the command to run
            /// will execute immediately. However, if this method returns false, the value set here will be ignored.
            /// </param>
            /// <returns>
            /// Implementations should return true to indicate that the command to run should be executed at the provided time. Returning false causes the
            /// RecurringCommand to finish execution.
            /// </returns>
            bool GetFirstExecutionTime(out DateTime time);

            /// <summary>
            /// Called when a <see cref="RecurringCommand"/> needs to know the next time to execute its underlying command to run.
            /// </summary>
            /// <param name="time">
            /// This will be initialized to the last time the command to run was set to begin execution. Implementations
            /// should set this to the next time to execute. If a time in the past is specified, the command to run will execute
            /// immediately. However, if this method returns false, the value set here will be ignored.
            /// </param>
            /// <returns>
            /// Implementations should return true to indicate that the command to run should be executed at the provided time.
            /// Returning false causes the RecurringCommand to finish execution.
            /// </returns>
            bool GetNextExecutionTime(ref DateTime time);
        }

        /// <summary>
        /// Called when a <see cref="RecurringCommand"/> needs to know the first time to execute its underlying command to run.
        /// </summary>
        /// <param name="time">
        /// Implementations should set this to the first time to execute. If a time in the past is specified, the command to run
        /// will execute immediately. However, if this method returns false, the value set here will be ignored.
        /// </param>
        /// <returns>
        /// Implementations should return true to indicate that the command to run should be executed at the provided time. Returning false causes the
        /// RecurringCommand to finish execution.
        /// </returns>
        public delegate bool GetFirstExecutionTime(out DateTime time);

        /// <summary>
        /// Called when a <see cref="RecurringCommand"/> needs to know the next time to execute its underlying command to run.
        /// </summary>
        /// <param name="time">
        /// This will be initialized to the last time the command to run was set to begin execution. Implementations
        /// should set this to the next time to execute. If a time in the past is specified, the command to run will execute
        /// immediately. However, if this method returns false, the value set here will be ignored.
        /// </param>
        /// <returns>
        /// Implementations should return true to indicate that the command to run should be executed at the provided time.
        /// Returning false causes the RecurringCommand to finish execution.
        /// </returns>
        public delegate bool GetNextExecutionTime(ref DateTime time);

        /// <summary>
        /// Constructs a RecurringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RecurringCommand object is disposed.
        /// </param>
        /// <param name="callback">Defines at what times the underlying command executes</param>
        public RecurringCommand(Command command, IExecutionTimeCallback callback)
            : this(command, callback, null)
        {
        }

        /// <summary>
        /// Constructs a RecurringCommand object as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RecurringCommand object is disposed.
        /// </param>
        /// <param name="getFirstExecutionTime">
        /// Defines if and when the underlying command will first execute
        /// </param>
        /// <param name="getNextExecutionTime">
        /// Defines if and when the underlying command will next execute. This is called after
        /// every completion of the underlying command.
        /// </param>
        public RecurringCommand(Command command, GetFirstExecutionTime getFirstExecutionTime, GetNextExecutionTime getNextExecutionTime)
            : this(command, getFirstExecutionTime, getNextExecutionTime, null)
        {
        }

        /// <summary>
        /// Constructs a RecurringCommand object
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RecurringCommand object is disposed.
        /// </param>
        /// <param name="callback">Defines at what times the underlying command executes</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public RecurringCommand(Command command, IExecutionTimeCallback callback, Command owner)
            : base(owner)
        {
            _callback = callback;
            _scheduledCmd = new ScheduledCommand(command, DateTime.Now, true, this);
        }

        /// <summary>
        /// Constructs a RecurringCommand object
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this RecurringCommand object is disposed.
        /// </param>
        /// <param name="getFirstExecutionTime">
        /// Defines if and when the underlying command will first execute
        /// </param>
        /// <param name="getNextExecutionTime">
        /// Defines if and when the underlying command will next execute. This is called after
        /// every completion of the underlying command.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public RecurringCommand(Command command, GetFirstExecutionTime getFirstExecutionTime, GetNextExecutionTime getNextExecutionTime, Command owner)
            : this(command, new ExecutionTimeCallbackFromDelegates(getFirstExecutionTime, getNextExecutionTime), owner)
        {
        }

        /// <summary>If currently waiting until the time to next execute the command to run, skip the wait and execute the command right away.</summary>
        /// <remarks>This is a no-op if this ScheduledCommand object is not currently executing</remarks>
        public void SkipCurrentWait()
        {
            CheckDisposed();
            _scheduledCmd.SkipWait();
        }

        /// <summary>
        /// If currently waiting until the time to next execute the command to run, resets that time to the time specified.
        /// </summary>
        /// <param name="time">
        /// The time to execute the command to run. If a time in the past is specified, the command to run will execute immediately.
        /// </param>
        /// <remarks>
        /// This is a no-op if this RecurringCommand object is not currently executing.
        /// </remarks>
        public void SetNextExecutionTime(DateTime time)
        {
            CheckDisposed();
            _scheduledCmd.TimeOfExecution = time;
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
	        bool keepGoing = _callback.GetFirstExecutionTime(out DateTime executionTime);

            while (keepGoing)
            {
                _scheduledCmd.TimeOfExecution = executionTime;
                CheckAbortFlag();
                _scheduledCmd.SyncExecute(runtimeArg);
                executionTime = _scheduledCmd.TimeOfExecution; // in case it was changed
                keepGoing = _callback.GetNextExecutionTime(ref executionTime);
            }

            return null;
        }

        private readonly ScheduledCommand _scheduledCmd;
        private readonly IExecutionTimeCallback _callback;

        private class ExecutionTimeCallbackFromDelegates : IExecutionTimeCallback
        {
            internal ExecutionTimeCallbackFromDelegates(
                GetFirstExecutionTime getFirstExecutionTime, GetNextExecutionTime getNextExecutionTime)
            {
                _getFirstExecutionTime = getFirstExecutionTime ?? throw new ArgumentNullException(nameof(getFirstExecutionTime));
                _getNextExecutionTime = getNextExecutionTime ?? throw new ArgumentNullException(nameof(getNextExecutionTime));
            }

            public bool GetFirstExecutionTime(out DateTime time)
            {
                return _getFirstExecutionTime(out time);
            }

            public bool GetNextExecutionTime(ref DateTime time)
            {
                return _getNextExecutionTime(ref time);
            }

            private readonly GetFirstExecutionTime _getFirstExecutionTime;
            private readonly GetNextExecutionTime _getNextExecutionTime;
        }
    }
}
