using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class RecurringCommandTests
    {
        private class RecurCallback : RecurringCommand.IExecutionTimeCallback
        {
            internal RecurCallback(TimeSpan intervalBeforeFirst, TimeSpan intervalBeforeRest, int repetitions)
            {
                this.intervalBeforeFirst = intervalBeforeFirst;
                this.intervalBeforeRest = intervalBeforeRest;
                this.repetitions = repetitions;
            }

            public bool GetFirstExecutionTime(out DateTime time)
            {
                time = DateTime.Now + intervalBeforeFirst;
                return --repetitions >= 0;
            }

            public bool GetNextExecutionTime(ref DateTime time)
            {
                if (--repetitions >= 0)
                {
                    time = DateTime.Now + intervalBeforeRest;
                    return true;
                }

                return false;
            }

            private TimeSpan intervalBeforeFirst;
            private TimeSpan intervalBeforeRest;
            int repetitions;
        }

        [TestMethod]
        public void RecurringCommand_TestAbort()
        {
            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1), TimeSpan.FromDays(1), 7)))
            {
                AbortTest.Run(recurringCmd, 2, 20);
            }

            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1), 7)))
            {
                AbortTest.Run(recurringCmd, 3, 20);
            }

            using (RecurringCommand recurringCmd = new RecurringCommand(
                new PauseCommand(TimeSpan.FromDays(1)),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1).Negate(), 7)))
            {
                AbortTest.Run(recurringCmd, 1, 20);
            }
        }

        [TestMethod]
        public void RecurringCommand_TestHappyPath()
        {
            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7)))
            {
                HappyPathTest.Run(recurringCmd, 2, null);
            }

            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1).Negate(), 7)))
            {
                HappyPathTest.Run(recurringCmd, 2, null);
            }
        }

        [TestMethod]
        public void RecurringCommand_TestFail()
        {
            using (RecurringCommand recurringCmd = new RecurringCommand(
                new FailingCommand(),
                new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7)))
            {
                FailTest.Run<FailingCommand.FailException>(recurringCmd, 2);
            }
        }

        [TestMethod]
        public void RecurringCommand_TestSkipCurrentWait()
        {
            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1), TimeSpan.FromDays(1), 2)))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                recurringCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give the async routine a moment to get going
                recurringCmd.SkipCurrentWait();
                Assert.IsFalse(recurringCmd.Wait(TimeSpan.FromMilliseconds(100)));
                recurringCmd.SkipCurrentWait();
                recurringCmd.Wait();
                listener.Check();
            }
        }

        [TestMethod]
        public void RecurringCommand_TestSetNextExecutionTime()
        {
            using (RecurringCommand recurringCmd = new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1), TimeSpan.FromDays(1), 1)))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                recurringCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give the async routine a moment to get going
                Assert.IsFalse(recurringCmd.Wait(TimeSpan.FromMilliseconds(100)));
                recurringCmd.SetNextExecutionTime(DateTime.Now);
                recurringCmd.Wait();
                listener.Check();
            }
        }
    }
}
