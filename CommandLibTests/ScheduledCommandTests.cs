using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(0), Tomorrow(), false))
            {
                AbortTest.Run(scheduledCmd, null, 20);
            }

            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new NeverEndingAsyncCommand(), Yesterday(), true))
            {
                AbortTest.Run(scheduledCmd, null, 20);
            }
        }

        [TestMethod]
        public void ScheduledCommand_TestHappyPath()
        {
            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(3), Yesterday(), true))
            {
                HappyPathTest.Run(scheduledCmd, 0, 3);
            }

            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(2), RealSoon(), true))
            {
                HappyPathTest.Run(scheduledCmd, 0, 2);
            }

            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(2), RealSoon(), false))
            {
                Assert.AreEqual(scheduledCmd.SyncExecute(3), 5);
            }
        }

        [TestMethod]
        public void ScheduledCommand_TestFail()
        {
            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(0), Yesterday(), false))
            {
                FailTest.Run<InvalidOperationException>(scheduledCmd, 0);
            }

            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new FailingCommand(), RealSoon(), true))
            {
                FailTest.Run<FailingCommand.FailException>(scheduledCmd, 0);
            }
        }

        [TestMethod]
        public void ScheduledCommand_TestSkipCurrentWait()
        {
            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(0), Tomorrow(), false))
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
            using (CommandLib.ScheduledCommand scheduledCmd = new CommandLib.ScheduledCommand(new AddCommand(0), Tomorrow(), false))
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
