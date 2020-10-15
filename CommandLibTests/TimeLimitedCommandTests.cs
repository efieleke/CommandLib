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
            AbortTest.Run(new TimeLimitedCommand(new PauseCommand(TimeSpan.FromDays(1)), int.MaxValue), null, 10);
        }

        [TestMethod]
        public void TimeLimitedCommand_TestHappyPath()
        {
            HappyPathTest.Run(new TimeLimitedCommand(new AddCommand(4), int.MaxValue), 10, 14);
            HappyPathTest.Run(new TimeLimitedCommand(new PauseCommand(TimeSpan.FromMilliseconds(10)), 100), null, null);
        }

        [TestMethod]
        public void TimeLimitedCommand_TestFail()
        {
            FailTest.Run<FailingCommand.FailException>(new TimeLimitedCommand(new FailingCommand(), int.MaxValue), null);
            FailTest.Run<TimeoutException>(new TimeLimitedCommand(new PauseCommand(TimeSpan.FromMilliseconds(100)), 10), null);
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

        [TestMethod]
        public void TimeLimitedCommand_TestAbortReset()
        {
            var timeLimitedCmd = new TimeLimitedCommand(new PauseCommand(TimeSpan.FromDays(1)), 10);
            int count = 0;

            using (var recurringCmd = new RecurringCommand(
                new TimeoutEatingCmd(timeLimitedCmd),
                (RecurringCommand cmd, out DateTime t) =>
                {
                    t = DateTime.MinValue;
                    return true;
                },
                (RecurringCommand cmd, ref DateTime t) =>
                {
                    t = DateTime.MinValue;
                    return ++count < 3;
                }))
            {
                recurringCmd.SyncExecute();
            }
        }

        private class TimeoutEatingCmd : Command
        {
            public TimeoutEatingCmd(Command commandToRun) : base(null)
            {
                _commandToRun = commandToRun;
                TakeOwnership(_commandToRun);
            }

            public override bool IsNaturallySynchronous() => _commandToRun.IsNaturallySynchronous();

            protected override object SyncExecuteImpl(object runtimeArg)
            {
                try
                {
                    _commandToRun.SyncExecute(runtimeArg);
                }
                catch (TimeoutException)
                {
                }

                return null;
            }

            protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
            {
                _commandToRun.AsyncExecute(
                    listener.CommandSucceeded,
                    listener.CommandAborted,
                    e =>
                    {
                        if (e is TimeoutException)
                        {
                            listener.CommandSucceeded(null);
                        }
                        else
                        {
                            listener.CommandFailed(e);
                        }
                    });
            }

            private readonly Command _commandToRun;
        }
    }
}
