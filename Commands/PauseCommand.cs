using System;

namespace Sophos.Commands
{
    /// <summary>A <see cref="Command"/> that efficiently does nothing for a specified duration.</summary>
    /// <remarks>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// ignore the 'runtimeArg' value that is passed in (except that it is used for the return value)
    /// <para>
    /// Synchronous execution will return the same runtimeArg value as was passed in, and the
    /// 'result' parameter of <see cref="ICommandListener.CommandSucceeded"/> will also be set to the same runtimeArg value.
    /// </para>
    /// </remarks>
    public class PauseCommand : SyncCommand
    {
        /// <summary>Constructs a PauseCommand object as a top-level <see cref="Command"/></summary>
        /// <param name="duration">The amount of time to pause</param>
        public PauseCommand(TimeSpan duration)
            : this(duration, null, null)
        {
        }

        /// <summary>Constructs a PauseCommand object as a top-level <see cref="Command"/></summary>
        /// <param name="duration">The amount of time to pause</param>
        /// <param name="stopEvent">
        /// Optional event to indicate that the PauseCommand should stop. Raising this event is equivalent to calling <see cref="CutShort"/>
        /// </param>
        public PauseCommand(TimeSpan duration, System.Threading.WaitHandle stopEvent)
            : this(duration, stopEvent, null)
        {
        }

        /// <summary>Constructs a PauseCommand object</summary>
        /// <param name="duration">The amount of time to pause</param>
        /// <param name="stopEvent">
        /// Optional event to indicate that the PauseCommand should stop. Raising this event is equivalent to calling <see cref="CutShort"/>
        /// </param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public PauseCommand(TimeSpan duration, System.Threading.WaitHandle stopEvent, Command owner)
            : base(owner)
        {
            this.duration = duration;
            this.externalCutShortEvent = stopEvent;
        }

        /// <summary>
        /// If currently executing, finishes the pause now. Does *not* cause this command to be aborted.
        /// </summary>
        public void CutShort()
        {
            CheckDisposed();
            cutShortEvent.Set();
        }

        /// <summary>
        /// If currently executing, starts the pause all over again, with the currently set duration value
        /// </summary>
        public void Reset()
        {
            CheckDisposed();
            resetEvent.Set();
        }

        /// <summary>
        /// The amount of time to pause
        /// </summary>
        /// <remarks>It is safe to change this property while the command is executing, but doing so will have no effect until the next time it is executed.</remarks>
        public TimeSpan Duration
        {
            get
            {
                CheckDisposed();

                lock (criticalSection)
                {
                    // Copy for thread safety
                    return new TimeSpan(duration.Ticks);
                }
            }
            set
            {
                CheckDisposed();

                lock (criticalSection)
                {
                    this.duration = value;
                }
            }
        }

        /// <summary>
        /// Returns diagnostic information about this object's state
        /// </summary>
        /// <returns>
        /// The returned text includes the duration, as well as whether an external stop event is defined
        /// </returns>
        public override string ExtendedDescription()
        {
            return String.Format("Duration: {0}; External stop event? {1}", Duration, externalCutShortEvent != null);
        }

        /// <summary>
        /// Implementations should override only if they contain members that must be disposed. Remember to invoke the base class implementation from within any override.
        /// </summary>
        /// <param name="disposing">Will be true if this was called as a direct result of the object being explicitly disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    resetEvent.Dispose();
                    cutShortEvent.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        protected sealed override void PrepareExecute(object runtimeArg)
        {
            cutShortEvent.Reset();
            resetEvent.Reset();
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override Object SyncExeImpl(Object runtimeArg)
        {
            int result = WaitForDuration();

            while (result == 1)
            {
                resetEvent.Reset();
                result = WaitForDuration();
            }

            if (result == 0)
            {
                throw new CommandAbortedException();
            }

            return runtimeArg;
        }

        private int WaitForDuration()
        {
            System.Threading.WaitHandle[] handles =
                externalCutShortEvent == null ?
                    new System.Threading.WaitHandle[] { AbortEvent, resetEvent, cutShortEvent } :
                    new System.Threading.WaitHandle[] { AbortEvent, resetEvent, cutShortEvent, externalCutShortEvent };

            double totalMilliseconds = Duration.TotalMilliseconds;
            int waitTime = totalMilliseconds >= int.MaxValue ? int.MaxValue : (int)totalMilliseconds;
            int result;

            do
            {
                result = System.Threading.WaitHandle.WaitAny(handles, waitTime);
                totalMilliseconds -= int.MaxValue;
                waitTime = totalMilliseconds >= int.MaxValue ? int.MaxValue : (int)totalMilliseconds;
            }
            while (result == System.Threading.WaitHandle.WaitTimeout && totalMilliseconds > 0);

            return result;
        }

        private TimeSpan duration;
        private System.Threading.ManualResetEvent resetEvent = new System.Threading.ManualResetEvent(false);
        private System.Threading.ManualResetEvent cutShortEvent = new System.Threading.ManualResetEvent(false);
        private readonly System.Threading.WaitHandle externalCutShortEvent = null;
        private readonly Object criticalSection = new Object();
    }
}
