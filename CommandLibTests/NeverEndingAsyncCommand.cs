using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibTests
{
    internal class NeverEndingAsyncCommand : CommandLib.AsyncCommand
    {
        internal NeverEndingAsyncCommand() : base(null)
        {
        }

        internal NeverEndingAsyncCommand(CommandLib.Command owner) : base (owner)
        {
        }

        protected sealed override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
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
