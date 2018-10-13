using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class TimeLimitedCommandTests
    {
        [TestMethod]
        public void TimeLimitedCommand_TestAbort()
        {
            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(
                new PauseCommand(TimeSpan.FromDays(1)),
                int.MaxValue))
            {
                AbortTest.Run(timeLimitedCmd, null, 10);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestHappyPath()
        {
            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(new AddCommand(4), int.MaxValue))
            {
                HappyPathTest.Run(timeLimitedCmd, 10, 14);
            }

            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(
                new PauseCommand(TimeSpan.FromMilliseconds(10)),
                100))
            {
                HappyPathTest.Run(timeLimitedCmd, null, null);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestFail()
        {
            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(new FailingCommand(), int.MaxValue))
            {
                FailTest.Run<FailingCommand.FailException>(timeLimitedCmd, null);
            }

            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(
                new PauseCommand(TimeSpan.FromMilliseconds(100)),
                10))
            {
                FailTest.Run<TimeoutException>(timeLimitedCmd, null);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestTimeout()
        {
            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(
                new PauseCommand(TimeSpan.FromDays(1)), 10))
            {
                try
                {
                    timeLimitedCmd.SyncExecute();
                    Assert.Fail("Command should have thrown a timeout exception");
                }
                catch (TimeoutException)
                {
                }
            }

            TimeLimitedCommand innerCmd = new TimeLimitedCommand(
                new PauseCommand(TimeSpan.FromDays(1)), 10);

            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(innerCmd, int.MaxValue))
            {
                try
                {
                    timeLimitedCmd.SyncExecute();
                    Assert.Fail("Command should have thrown a timeout exception");
                }
                catch (TimeoutException)
                {
                }
            }

            innerCmd = new TimeLimitedCommand(new PauseCommand(TimeSpan.FromDays(1)), int.MaxValue);

            using (TimeLimitedCommand timeLimitedCmd = new TimeLimitedCommand(innerCmd, 10))
            {
                try
                {
                    timeLimitedCmd.SyncExecute();
                    Assert.Fail("Command should have thrown a timeout exception");
                }
                catch (TimeoutException)
                {
                }
            }
        }
    }
}
