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
                abortEvent.WaitOne();
                abortEvent.Reset();
                listener.CommandAborted();
            }).Start();
        }

        protected sealed override void AbortImpl()
        {
            abortEvent.Set();
        }

        private System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false);
    }
}
