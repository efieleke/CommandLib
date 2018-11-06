using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sophos.Commands
{
    /// <summary>
    /// Wraps Task.Delay() into a Command object. <see cref="PauseCommand"/> is more efficient when
    /// run synchronously (SyncExecute), but this class is more efficient when run asynchronously
    /// (AsyncExecute). PauseCommand offers a few more features than this class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Command.SyncExecute(object)"/> and <see cref="Command.AsyncExecute(ICommandListener, object)"/>
    /// ignore the 'runtimeArg' value that is passed in
    /// </para>
    /// </remarks>
    public class DelayCommand : TaskCommand<TimeSpan?>
    {
        /// <summary>
        /// Constructor for a top-level command
        /// </summary>
        /// <param name="duration">The delay duration</param>
        public DelayCommand(TimeSpan duration) : this(duration, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="duration">The delay duration</param>
        /// <param name="owner">
        /// Specify null to indicate a top-level command. Otherwise, this command will be owned by 'owner'. Owned commands respond to
        /// abort requests made of their owner. Also, owned commands are disposed of when the owner is disposed.
        /// </param>
        public DelayCommand(TimeSpan duration, Command owner) : base(owner)
        {
            Duration = duration;
        }

        /// <summary>
        /// The amount of time to pause
        /// </summary>
        /// <remarks>It is safe to change this property while the command is executing, but doing so will have no effect until the next time it is executed.</remarks>
        public TimeSpan Duration
        {
            get
            {
                lock (_criticalSection)
                {
                    // Copy for thread safety
                    return new TimeSpan(_duration.Ticks);
                }
            }
            set
            {
                lock (_criticalSection)
                {
                    _duration = value;
                }
            }
        }

        /// <inheritdoc />
        protected override Task CreateTaskNoResult(TimeSpan? runtimeArg, CancellationToken cancellationToken)
        {
            return Task.Delay(runtimeArg ?? Duration, cancellationToken);
        }

        private TimeSpan _duration;
        private readonly object _criticalSection = new object();
    }
}
