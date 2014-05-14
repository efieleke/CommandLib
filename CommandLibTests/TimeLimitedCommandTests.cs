using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class TimeLimitedCommandTests
    {
        [TestMethod]
        public void TimeLimitedCommand_TestAbort()
        {
            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(
                new CommandLib.PauseCommand(TimeSpan.FromDays(1)),
                int.MaxValue))
            {
                AbortTest.Run(timeLimitedCmd, null, 10);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestHappyPath()
        {
            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(new AddCommand(4), int.MaxValue))
            {
                HappyPathTest.Run(timeLimitedCmd, 10, 14);
            }

            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(
                new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(10)),
                100))
            {
                HappyPathTest.Run(timeLimitedCmd, null, null);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestFail()
        {
            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(new FailingCommand(), int.MaxValue))
            {
                FailTest.Run<FailingCommand.FailException>(timeLimitedCmd, null);
            }

            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(
                new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(100)),
                10))
            {
                FailTest.Run<TimeoutException>(timeLimitedCmd, null);
            }
        }

        [TestMethod]
        public void TimeLimitedCommand_TestTimeout()
        {
            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(
                new CommandLib.PauseCommand(TimeSpan.FromDays(1)), 10))
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

            CommandLib.TimeLimitedCommand innerCmd = new CommandLib.TimeLimitedCommand(
                new CommandLib.PauseCommand(TimeSpan.FromDays(1)), 10);

            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(innerCmd, int.MaxValue))
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

            innerCmd = new CommandLib.TimeLimitedCommand(new CommandLib.PauseCommand(TimeSpan.FromDays(1)), int.MaxValue);

            using (CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(innerCmd, 10))
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
