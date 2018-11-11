using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class ScheduledCommandTests
    {
        private static DateTime Tomorrow()
        {
            return DateTime.Now + TimeSpan.FromDays(1);
        }

        private static DateTime Yesterday()
        {
            return DateTime.Now - TimeSpan.FromDays(1);
        }

        private static DateTime RealSoon()
        {
            return DateTime.Now + TimeSpan.FromMilliseconds(100);
        }

        [TestMethod]
        public void ScheduledCommand_TestAbort()
        {
            AbortTest.Run(new ScheduledCommand(new AddCommand(0), Tomorrow(), false), null, 20);
            AbortTest.Run(new ScheduledCommand(new NeverEndingAsyncCommand(), Yesterday(), true), null, 20);
        }

        [TestMethod]
        public void ScheduledCommand_TestHappyPath()
        {
            HappyPathTest.Run(new ScheduledCommand(new AddCommand(3), Yesterday(), true), 0, 3);
            HappyPathTest.Run(new ScheduledCommand(new AddCommand(2), RealSoon(), true), 0, 2);

            using (ScheduledCommand scheduledCmd = new ScheduledCommand(new AddCommand(2), RealSoon(), false))
            {
                Assert.AreEqual(scheduledCmd.SyncExecute(3), 5);
            }
        }

        [TestMethod]
        public void ScheduledCommand_TestFail()
        {
            FailTest.Run<InvalidOperationException>(new ScheduledCommand(new AddCommand(0), Yesterday(), false), 0);
            FailTest.Run<FailingCommand.FailException>(new ScheduledCommand(new FailingCommand(), RealSoon(), true), 0);
        }

        [TestMethod]
        public void ScheduledCommand_TestSkipCurrentWait()
        {
            using (ScheduledCommand scheduledCmd = new ScheduledCommand(new AddCommand(0), Tomorrow(), false))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, 0);
                scheduledCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give the async routine a moment to get going
                scheduledCmd.SkipWait();
                scheduledCmd.Wait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Succeeded, 0);
                scheduledCmd.SkipWait(); // no-op
                scheduledCmd.AsyncExecute(listener, 0);
                Assert.IsFalse(scheduledCmd.Wait(TimeSpan.FromMilliseconds(100)));
                scheduledCmd.SkipWait();
                scheduledCmd.Wait();
                listener.Check();
            }
        }

        [TestMethod]
        public void ScheduledCommand_TestChange()
        {
            using (ScheduledCommand scheduledCmd = new ScheduledCommand(new AddCommand(0), Tomorrow(), false))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, 0);
                scheduledCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give the async routine a moment to get going
                scheduledCmd.TimeOfExecution = RealSoon();
                scheduledCmd.Wait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Succeeded, 0);
                scheduledCmd.TimeOfExecution = RealSoon();
                scheduledCmd.AsyncExecute(listener, 0);
                Assert.IsFalse(scheduledCmd.Wait(TimeSpan.FromMilliseconds(10)));
                scheduledCmd.Wait();
                listener.Check();

                try
                {
                    scheduledCmd.TimeOfExecution = Yesterday();
                    Assert.Fail("Successfully set time of operation to the past");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
