using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class PauseCommandTests
    {
        [TestMethod]
        public void PauseCommand_TestAbort()
        {
            AbortTest.Run(new PauseCommand(TimeSpan.FromDays(1)), null, 10);
        }

        [TestMethod]
        public void PauseCommand_TestHappyPath()
        {
            HappyPathTest.Run(new PauseCommand(TimeSpan.FromMilliseconds(1)), null, null);

            using (var shortPause = new PauseCommand(TimeSpan.FromMilliseconds(100)))
            {
                shortPause.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));
                HappyPathTest.Run(new PauseCommand(TimeSpan.MaxValue, shortPause.DoneEvent), null, null);
            }

            var disposed = new PauseCommand(TimeSpan.FromMilliseconds(9));
            disposed.Dispose();

            try
            {
                disposed.SyncExecute();
                Assert.Fail("Executed a disposed command");
            }
            catch(ObjectDisposedException)
            {
            }
        }

        [TestMethod]
        public void PauseCommand_TestReset()
        {
            using (var pauseCmd = new PauseCommand(TimeSpan.FromDays(1)))
            {
                var listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                pauseCmd.AsyncExecute(listener);
                System.Threading.Thread.Sleep(10); // give time for the command to start
                pauseCmd.Duration = TimeSpan.FromMilliseconds(1);
                Assert.IsFalse(pauseCmd.Wait(TimeSpan.FromMilliseconds(10)));
                pauseCmd.Reset();
                Assert.IsTrue(pauseCmd.Wait(TimeSpan.FromMilliseconds(100)));
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Aborted, null);
                pauseCmd.Duration = TimeSpan.FromMilliseconds(100);
                pauseCmd.AsyncExecute(listener);
                System.Threading.Thread.Sleep(20); // give the async routine a moment to get going
                pauseCmd.Duration = TimeSpan.FromDays(1);
                pauseCmd.Reset();
                Assert.IsFalse(pauseCmd.Wait(TimeSpan.FromMilliseconds(100)));
                pauseCmd.AbortAndWait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Succeeded, null);
                pauseCmd.Duration = TimeSpan.FromDays(1);
                pauseCmd.AsyncExecute(listener);
                System.Threading.Thread.Sleep(10); // give time for the command to start
                pauseCmd.Reset();
                pauseCmd.CutShort();
                pauseCmd.Wait();
                listener.Check();
            }
        }

        [TestMethod]
        public void PauseCommand_TestCutShort()
        {
            using (var pauseCmd = new PauseCommand(TimeSpan.FromDays(1)))
            {
                var listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                pauseCmd.AsyncExecute(listener);
                System.Threading.Thread.Sleep(20); // give the async routine a moment to get going
                pauseCmd.CutShort();
                pauseCmd.Wait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Aborted, null);
                pauseCmd.CutShort(); // no-op
                pauseCmd.AsyncExecute(listener);
                Assert.IsFalse(pauseCmd.Wait(TimeSpan.FromMilliseconds(10)));
                pauseCmd.AbortAndWait();
            }
        }
    }
}
