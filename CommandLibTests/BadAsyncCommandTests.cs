﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal class BadAsyncCommand : AsyncCommand
    {
        internal enum FinishType { Succeed, Fail, Abort }

        internal BadAsyncCommand(FinishType finishType) : base(null)
        {
            _finishType = finishType;
        }

        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            switch(_finishType)
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

        private readonly FinishType _finishType;
    }

    internal class DoublyDoneAsyncCommand : AsyncCommand
    {
        internal DoublyDoneAsyncCommand()
            : base(null)
        {
        }

        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
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
                    string msg = "Expected InvalidOperationException, but instead got this: " + exc;
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
                // Poor usage, but supported
                test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));
                test.Wait();

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
                // Called back on same thread, but should work anyway
                test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Failed, null));
            }
        }

        [TestMethod]
        public void BadAsyncCommand_TestBadAbort()
        {
            using (BadAsyncCommand test = new BadAsyncCommand(BadAsyncCommand.FinishType.Abort))
            {
                // Called back on same thread, but should work anyway
                test.AsyncExecute(new CmdListener(CmdListener.CallbackType.Aborted, null));
            }
        }

        [TestMethod]
        public void JunkyMonitorTest()
        {
            Command.Monitors = new LinkedList<ICommandMonitor>();
            Command.Monitors.AddFirst(new BadMonitor());

            using (var cmd = new PauseCommand(TimeSpan.Zero))
            {
                try
                {
                    cmd.AsyncExecute(o => { Assert.Fail("Shouldn't be here"); }, () => { Assert.Fail("Shouldn't be here"); }, e => throw e);
                    Assert.Fail("Shouldn't be here");
                }
                catch (NotImplementedException)
                {
                    // expected
                    foreach (ICommandMonitor monitor in Command.Monitors)
                    {
                        monitor.Dispose();
                    }

                    Command.Monitors = null;
                    cmd.AsyncExecute(o => {}, () => { Assert.Fail("Shouldn't be here"); }, e => throw e);
                    cmd.Wait();
                }
            }
        }

        private class BadMonitor : ICommandMonitor
        {
            public void Dispose()
            {
            }

            public void CommandStarting(ICommandInfo commandInfo)
            {
                throw new NotImplementedException();
            }

            public void CommandFinished(ICommandInfo commandInfo, Exception exc)
            {
            }
        }
    }
}
