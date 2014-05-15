using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>Represents a <see cref="Command"/> that repeats periodically at a specified interval</summary>
    /// <remarks>
    /// The runtimeArg passed to the execution methods will be passed to the command that executes, every time that it executes.
    /// The runtimeArg should be of the same type required as the underlying command to run.
    /// <para>
    /// This command returns null from synchronous execution, and sets the 'result' parameter of 
    /// <see cref="ICommandListener.CommandSucceeded"/> to null.
    /// </para>
    /// <para>
    /// If more dynamic control is needed around the period of time between executions, use <see cref="RecurringCommand"/> instead.
    /// </para>
    /// </remarks>
    public class PeriodicCommand : SyncCommand
    {
        /// <summary>
        /// Defines how the interval between command executions is performed within a <see cref="PeriodicCommand"/>
        /// </summary>
        public enum IntervalType
        {
            /// <summary>
            /// Pause first, then run the command, and repeat as many times as specified
            /// </summary>
            PauseBefore,

            /// <summary>
            /// Run the command first, then pause, and repeat as many times as specified. There is no pause after the last
            /// execution of the command.
            /// </summary>
            PauseAfter
        }

        /// <summary>
        /// Constructs a top-level PeriodicCommand as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run periodically. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this PeriodicCommand object is disposed.
        /// </param>
        /// <param name="repeatCount">The number of times to repeat the command</param>
        /// <param name="interval">The interval of time between repetitions</param>
        /// <param name="intervalType">Specifies whether the pause interval occurs before or after the command executes</param>
        /// <param name="intervalIsInclusive">
        /// If false, the interval means the time between when the command finishes and when it starts next.
        /// If true, the interval means the time between the start of successive command executions (in this case, if the
        /// command execution takes longer than the interval, the next command will start immediately).
        /// </param>
        public PeriodicCommand(
            Command command,
            long repeatCount,
            TimeSpan interval,
            IntervalType intervalType,
            bool intervalIsInclusive)
            : this(command, repeatCount, interval, intervalType, intervalIsInclusive, null)
        {
        }

        /// <summary>
        /// Constructs a top-level PeriodicCommand as a top-level <see cref="Command"/>
        /// </summary>
        /// <param name="command">
        /// The command to run periodically. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this PeriodicCommand object is disposed.
        /// </param>
        /// <param name="repeatCount">The number of times to repeat the command</param>
        /// <param name="interval">The interval of time between repetitions</param>
        /// <param name="intervalType">Specifies whether the pause interval occurs before or after the command executes</param>
        /// <param name="intervalIsInclusive">
        /// If false, the interval means the time between when the command finishes and when it starts next.
        /// If true, the interval means the time between the start of successive command executions (in this case, if the
        /// command execution takes longer than the interval, the next command will start immediately).
        /// </param>
        /// <param name="stopEvent">
        /// Optional event to indicate that the perdiodic command should stop. Raising this event is equivalent to calling <see cref="Stop"/>
        /// You can specify the <see cref="Command.DoneEvent"/> of a different <see cref="Command"/> as the stop event, which will cause this
        /// periodic command to stop when the other command finishes, but be sure that the other command begins execution before this command
        /// if you choose to do this.
        /// </param>
        public PeriodicCommand(
            Command command,
            long repeatCount,
            TimeSpan interval,
            IntervalType intervalType,
            bool intervalIsInclusive,
            System.Threading.WaitHandle stopEvent)
            : this(command, repeatCount, interval, intervalType, intervalIsInclusive, stopEvent, null)
        {
        }

        /// <summary>
        /// Constructs a PeriodicCommand
        /// </summary>
        /// <param name="command">
        /// The command to run periodically. This object takes ownership of the command, so the passed command must not already have
        /// an owner. The passed command will be disposed when this PeriodicCommand object is disposed.
        /// </param>
        /// <param name="repeatCount">The number of times to repeat the command (Int.Max is the maximum)</param>
        /// <param name="interval">The interval of time between repetitions</param>
        /// <param name="intervalType">Specifies whether the pause interval occurs before or after the command executes</param>
        /// <param name="intervalIsInclusive">
        /// If false, the interval means the time between when the command finishes and when it starts next.
        /// If true, the interval means the time between the start of successive command executions (in this case, if the
        /// command execution takes longer than the interval, the next command will start immediately).
        /// </param>
        /// <param name="stopEvent">
        /// Optional event to indicate that the perdiodic command should stop. Raising this event is equivalent to calling <see cref="Stop"/>
        /// You can specify the <see cref="Command.DoneEvent"/> of a different <see cref="Command"/> as the stop event, which will cause this
        /// periodic command to stop when the other command finishes, but be sure that the other command begins execution before this command
        /// if you choose to do this.
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public PeriodicCommand(
            Command command,
            long repeatCount,
            TimeSpan interval,
            IntervalType intervalType,
            bool intervalIsInclusive,
            System.Threading.WaitHandle stopEvent,
            Command owner)
            : base(owner)
        {
            this.stopEvent = stopEvent;
            initialPause = new PauseCommand(interval, stopEvent, this);
            pause = new PauseCommand(interval, stopEvent);

            if (intervalIsInclusive)
            {
                switch (intervalType)
                {
                    case IntervalType.PauseBefore:
                        startWithPause = true;
                        break;
                    case IntervalType.PauseAfter:
                        startWithPause = false;
                        break;
                    default:
                        initialPause.Dispose();
                        pause.Dispose();
                        throw new ArgumentException(String.Format("Unknown interval type {0}", intervalType), "intervalType");
                }

                ParallelCommands parallelCmds = new ParallelCommands(true, this);
                parallelCmds.Add(command);
                parallelCmds.Add(pause);
                collectionCmd = parallelCmds;
            }
            else
            {
                switch (intervalType)
                {                
                    case IntervalType.PauseAfter:
                        SequentialCommands seqAfter = new SequentialCommands(this);
                        seqAfter.Add(command);
                        seqAfter.Add(pause);
                        collectionCmd = seqAfter;
                        break;
                    case IntervalType.PauseBefore:
                        SequentialCommands seqBefore = new SequentialCommands(this);
                        seqBefore.Add(pause);
                        seqBefore.Add(command);
                        collectionCmd = seqBefore;
                        break;
                    default:
                        initialPause.Dispose();
                        pause.Dispose();
                        throw new ArgumentException(String.Format("Unknown interval type {0}", intervalType), "intervalType");
                }
            }

            this.repeatCount = repeatCount;
        }

        /// <summary>
        /// The interval of time between command executions.
        /// </summary>
        /// <remarks>It is safe to change this property while the command is executing</remarks>
        public TimeSpan Interval
        {
            get
            {
                CheckDisposed();
                return pause.Duration;
            }
            set
            {
                CheckDisposed();
                initialPause.Duration = value;
                pause.Duration = value;
            }
        }

        /// <summary>
        /// Changes the number of times the command to run will execute. If the command to run is currently executing when this is
        /// called, it will be allowed to finish, even if the repeat count is set to a number lower than the number of times
        /// already executed.
        /// </summary>
        /// <remarks>It is safe to change this property while this command is executing</remarks>
        public long RepeatCount
        {
            get
            {
                CheckDisposed();
                return System.Threading.Interlocked.Read(ref repeatCount);
            }
            set
            {
                CheckDisposed();
                System.Threading.Interlocked.Exchange(ref repeatCount, value);
            }
        }

        /// <summary>
        /// Causes the command to stop repeating. This will not cause the command to be aborted. If the command to run
        /// is currently executing when this is called, it will be allowed to finish.
        /// </summary>
        /// <remarks>This is a no-op if this PeriodicCommand instance is not currently executing</remarks>
        public void Stop()
        {
            CheckDisposed();
            RepeatCount = 0;
            SkipCurrentWait();
        }

        /// <summary>
        /// If currently in the interval between command executions, skip the wait and execute the command right away.
        /// This only skips the current wait. It will not skip subsequent waits.
        /// </summary>
        public void SkipCurrentWait()
        {
            CheckDisposed();
            initialPause.CutShort();
            pause.CutShort();
        }

        /// <summary>
        /// Rewinds the current pause to its full duration.
        /// </summary>
        public void Reset()
        {
            CheckDisposed();
            initialPause.Reset();
            pause.Reset();
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the repetition count, the duration between executions, whether to start with a pause,
        /// as well as whether an external stop event is defined
        /// </returns>
        public override string ExtendedDescription()
        {
            return String.Format("Repetitions: {0}; Interval: {1}; Start with pause? {2}; External stop event? {3}", RepeatCount, Interval, startWithPause, stopEvent != null);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            if (startWithPause && RepeatCount > 0)
            {
                initialPause.SyncExecute();
            }

            for (int i = 0; i < RepeatCount; ++i)
            {
                if (stopEvent != null && stopEvent.WaitOne(0))
                {
                    return null;
                }

                CheckAbortFlag();

                if (i == RepeatCount - 1)
                {
                    // Don't pause for the last execution
                    TimeSpan prevInterval = pause.Duration;
                    pause.Duration = TimeSpan.FromTicks(0);

                    try
                    {
                        collectionCmd.SyncExecute<Object>(runtimeArg);
                    }
                    finally
                    {
                        pause.Duration = prevInterval;
                    }
                }
                else
                {
                    collectionCmd.SyncExecute<Object>(runtimeArg);
                }
            }

            return null;
        }

        private PauseCommand pause;
        private PauseCommand initialPause;
        private Command collectionCmd;
        private long repeatCount;
        private bool startWithPause;
        private System.Threading.WaitHandle stopEvent;
    }
}
