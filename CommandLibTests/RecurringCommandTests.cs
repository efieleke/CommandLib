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
                _intervalBeforeFirst = intervalBeforeFirst;
                _intervalBeforeRest = intervalBeforeRest;
                _repetitions = repetitions;
            }

            public bool GetFirstExecutionTime(out DateTime time)
            {
                time = DateTime.Now + _intervalBeforeFirst;
                return --_repetitions >= 0;
            }

            public bool GetNextExecutionTime(ref DateTime time)
            {
                if (--_repetitions >= 0)
                {
                    time = DateTime.Now + _intervalBeforeRest;
                    return true;
                }

                return false;
            }

            private readonly TimeSpan _intervalBeforeFirst;
            private readonly TimeSpan _intervalBeforeRest;
	        private int _repetitions;
        }

        [TestMethod]
        public void RecurringCommand_TestAbort()
        {
            AbortTest.Run(new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1), TimeSpan.FromDays(1), 99)),
                2,
                20);

            AbortTest.Run(new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1), 99)),
                3,
                20);

            AbortTest.Run(new RecurringCommand(
                new PauseCommand(TimeSpan.FromDays(1)),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1).Negate(), 99)),
                1,
                20);

            var recurCallback = new RecurCallback(TimeSpan.FromDays(1), TimeSpan.FromDays(1), 99);

            AbortTest.Run(new RecurringCommand(
                new AddCommand(0),
                (out DateTime d) => recurCallback.GetFirstExecutionTime(out d), (ref DateTime d) => recurCallback.GetNextExecutionTime(ref d)),
                2,
                20);
        }

        [TestMethod]
        public void RecurringCommand_TestHappyPath()
        {
            HappyPathTest.Run(new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7)),
                2,
                null);

            HappyPathTest.Run(new RecurringCommand(
                new AddCommand(0),
                new RecurCallback(TimeSpan.FromDays(1).Negate(), TimeSpan.FromDays(1).Negate(), 7)),
                2,
                null);

            var recurCallback = new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7);

            HappyPathTest.Run(new RecurringCommand(
                new AddCommand(0),
                (out DateTime d) => recurCallback.GetFirstExecutionTime(out d), (ref DateTime d) => recurCallback.GetNextExecutionTime(ref d)),
                2,
                null);
        }

        [TestMethod]
        public void RecurringCommand_TestFail()
        {
            FailTest.Run<FailingCommand.FailException>(new RecurringCommand(
                new FailingCommand(),
                new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7)),
                2);

            var recurCallback = new RecurCallback(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 7);

            FailTest.Run<FailingCommand.FailException>(new RecurringCommand(
                new FailingCommand(),
                (out DateTime d) => recurCallback.GetFirstExecutionTime(out d), (ref DateTime d) => recurCallback.GetNextExecutionTime(ref d)),
                2);
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
