using System;

namespace Sophos.Commands
{
	/// <summary>A <see cref="Command"/> that efficiently does nothing for a specified duration.</summary>
	/// <remarks>
	/// <para>
	/// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
	/// ignore the 'runtimeArg' value that is passed in (except that it is used for the return value)
	/// </para>
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
            _duration = duration;
            _externalCutShortEvent = stopEvent;
        }

        /// <summary>
        /// If currently executing, finishes the pause now. Does *not* cause this command to be aborted.
        /// </summary>
        public void CutShort()
        {
            CheckDisposed();
            _cutShortEvent.Set();
        }

        /// <summary>
        /// If currently executing, starts the pause all over again, with the currently set duration value
        /// </summary>
        public void Reset()
        {
            CheckDisposed();
            _resetEvent.Set();
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

                lock (_criticalSection)
                {
                    // Copy for thread safety
                    return new TimeSpan(_duration.Ticks);
                }
            }
            set
            {
                CheckDisposed();

                lock (_criticalSection)
                {
                    _duration = value;
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
            return $"Duration: {Duration}; External stop event? {_externalCutShortEvent != null}";
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
                    _resetEvent.Dispose();
                    _cutShortEvent.Dispose();
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
            _cutShortEvent.Reset();
            _resetEvent.Reset();
        }

        /// <summary>
        /// Do not call this method from a derived class. It is called by the framework.
        /// </summary>
        /// <param name="runtimeArg">Not applicable</param>
        /// <returns>Not applicable</returns>
        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            int result = WaitForDuration();

            while (result == 1)
            {
                _resetEvent.Reset();
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
                _externalCutShortEvent == null ?
                    new[] { AbortEvent, _resetEvent, _cutShortEvent } :
                    new[] { AbortEvent, _resetEvent, _cutShortEvent, _externalCutShortEvent };

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

        private TimeSpan _duration;
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);
        private readonly System.Threading.ManualResetEvent _cutShortEvent = new System.Threading.ManualResetEvent(false);
        private readonly System.Threading.WaitHandle _externalCutShortEvent;
        private readonly object _criticalSection = new object();
    }
}
