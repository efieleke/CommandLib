using Sophos.Commands;

namespace CommandLibTests
{
    internal class NeverEndingAsyncCommand : AsyncCommand
    {
        internal NeverEndingAsyncCommand() : base(null)
        {
        }

        internal NeverEndingAsyncCommand(Command owner) : base (owner)
        {
        }

        protected sealed override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            new System.Threading.Thread(() =>
            {
                _abortEvent.WaitOne();
                _abortEvent.Reset();
                listener.CommandAborted();
            }).Start();
        }

        protected sealed override void AbortImpl()
        {
            _abortEvent.Set();
        }

        private readonly System.Threading.ManualResetEvent _abortEvent = new System.Threading.ManualResetEvent(false);
    }
}
