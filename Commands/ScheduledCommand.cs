using System;
using System.Globalization;

namespace Sophos.Commands
{
    /// <summary>
    /// Represents a <see cref="Command"/> that executes at a given time. When a ScheduledCommand is executed, it will enter an
    /// efficient wait state until the time arrives at which to execute the underlying command.
    /// </summary>
    /// <remarks>
    /// The 'runtimeArg' value to pass to <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// should be of the same type required as the underlying command to run.
    /// <para>
    /// This command returns from synchronous execution the same value that the underlying command to run returns,
    /// and the 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will be set in similar fashion.
    /// </para>
    /// </remarks>
    public class ScheduledCommand : SyncCommand
    {
        /// <summary>
        /// Constructs a ScheduledCommand as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this ScheduledCommand object is disposed.
        /// </param>
        /// <param name="timeOfExecution">
        /// The time at which to execute the command to run. Note that unless this ScheduledCommand object is actually executed, the command to run will never execute.
        /// </param>
        /// <param name="runImmediatelyIfTimeIsPast">
        /// If, when this ScheduledCommand is executed, the time of execution is in the past, it will execute immediately if this parameter is set to true
        /// (otherwise it will throw an InvalidOperation exception).
        /// </param>
        public ScheduledCommand(
            Command command,
            DateTime timeOfExecution,
            bool runImmediatelyIfTimeIsPast)
            : this(command, timeOfExecution, runImmediatelyIfTimeIsPast, null)
        {
        }

        /// <summary>
        /// Constructs a ScheduledCommand
        /// </summary>
        /// <param name="command">
        /// The command to run. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this ScheduledCommand object is disposed.
        /// </param>
        /// <param name="timeOfExecution">
        /// The time at which to execute the command to run. Note that unless this ScheduledCommand object is actually executed, the command to run will never execute.
        /// </param>
        /// <param name="runImmediatelyIfTimeIsPast">
        /// If, when this ScheduledCommand is executed, the time of execution is in the past, it will execute immediately if this parameter is set to true
        /// (otherwise it will throw an InvalidOperation exception).
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public ScheduledCommand(
            Command command,
            DateTime timeOfExecution,
            bool runImmediatelyIfTimeIsPast,
            Command owner)
            : base(owner)
        {
            _command = command;
            TakeOwnership(_command);
            _runImmediatelyIfTimeIsPast = runImmediatelyIfTimeIsPast;
            _pauseCmd = new PauseCommand(TimeSpan.FromTicks(0), null, this);
            _timeOfExecution = timeOfExecution;
        }

        /// <summary>The time at which to execute the command to run</summary>
        /// <remarks>
        /// It is safe to change this property while this command is executing, although if the underlying command
        /// to run has already begun execution, it will have no effect.
        /// </remarks>
        public DateTime TimeOfExecution
        {
            get
            {
                CheckDisposed();

                lock (_criticalSection)
                {
                    return _timeOfExecution;
                }
            }

            set
            {
                CheckDisposed();
                TimeSpan newInterval = value - DateTime.Now;

                if (newInterval.Ticks < 0 && !_runImmediatelyIfTimeIsPast)
                {
                    throw new InvalidOperationException(
	                    $"{Description} was scheduled to run at {value.ToString(CultureInfo.InvariantCulture)}, which is in the past");
                }

                lock(_criticalSection)
                {
                    _timeOfExecution = value;
                }

                if (newInterval.Ticks >= 0)
                {
                    _pauseCmd.Duration = newInterval;
                    _pauseCmd.Reset();
                }
                else
                {
                    _pauseCmd.CutShort();
                }
            }
        }

        /// <summary>Skips the current wait time before the execution of the underlying command and executes it immediately</summary>
        /// <remarks>This is a no-op if this ScheduledCommand object is not currently executing</remarks>
        public void SkipWait()
        {
            CheckDisposed();
            _pauseCmd.CutShort();
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the time to execute as well as whether to run immediately if the scheduled time is in the past.
        /// </returns>
        public override string ExtendedDescription()
        {
            return
	            $"Time to execute: {TimeOfExecution}; Run immediately if time is in the past? {_runImmediatelyIfTimeIsPast}";
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            TimeSpan waitTime = TimeOfExecution - DateTime.Now;

            if (waitTime.Ticks >= 0)
            {
                _pauseCmd.Duration = waitTime;
                _pauseCmd.SyncExecute();
            }
            else if (!_runImmediatelyIfTimeIsPast)
            {
                throw new InvalidOperationException(
	                $"{Description} was scheduled to run at {TimeOfExecution.ToString(CultureInfo.InvariantCulture)}, which is in the past");
            }

            return _command.SyncExecute(runtimeArg);
        }

        private readonly PauseCommand _pauseCmd;
        private readonly Command _command;
        private readonly bool _runImmediatelyIfTimeIsPast;
        private DateTime _timeOfExecution;
        private readonly object _criticalSection = new object();
    }
}
