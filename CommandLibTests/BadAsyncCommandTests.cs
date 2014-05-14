using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal class BadAsyncCommand : CommandLib.AsyncCommand
    {
        internal enum FinishType { Succeed, Fail, Abort }

        internal BadAsyncCommand(FinishType finishType) : base(null)
        {
            this.finishType = finishType;
        }

        protected override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
        {
            switch(finishType)
            {
                case FinishType.Abort:
                    listener.CommandAborted();
                    break;
                case FinishType.Fail:
                    listener.CommandFailed(new Exception("boo hoo"));
                    break;
                case FinishType.Succeed:
                    listener.CommandSucceeded(null);
                    break;
            }
        }

        private FinishType finishType;
    }

    internal class DoublyDoneAsyncCommand : CommandLib.AsyncCommand
    {
        internal DoublyDoneAsyncCommand()
            : base(null)
        {
        }

        protected override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                listener.CommandSucceeded(null);

                try
                {
                    listener.CommandSucceeded(null);
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception exc)
                {
                    String msg = "Expected InvalidOperationException, but instead got this: " + exc.ToString();
                    Assert.Fail(msg);
                }
            });

            thread.Start();
        }
    }

    [TestClass]
    public class BadAsyncCommandTests
    {
        [TestMethod]
        public void DoublyDoneAsyncCommand_TestDoubleSuccess()
        {
            using (DoublyDoneAsyncCommand test = new DoublyDoneAsyncCommand())
            {
                test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));
                test.Wait();
            }
        }

        [TestMethod]
        public void BadAsyncCommand_TestBadSuccess()
        {
            using (BadAsyncCommand test = new BadAsyncCommand(BadAsyncCommand.FinishType.Succeed))
            {
                try
                {
                    test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));
                    Assert.Fail("Called back on same thread");
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    test.AsyncExecute(null);
                    Assert.Fail("Used a null listener");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [TestMethod]
        public void BadAsyncCommand_TestBadFail()
        {
            using (BadAsyncCommand test = new BadAsyncCommand(BadAsyncCommand.FinishType.Fail))
            {
                try
                {
                    test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Failed, null));
                    Assert.Fail("Called back on same thread");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        [TestMethod]
        public void BadAsyncCommand_TestBadAbort()
        {
            using (BadAsyncCommand test = new BadAsyncCommand(BadAsyncCommand.FinishType.Abort))
            {
                try
                {
                    test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Aborted, null));
                    Assert.Fail("Called back on same thread");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
