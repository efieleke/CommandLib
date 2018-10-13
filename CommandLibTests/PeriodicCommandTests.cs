using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class PeriodicCommandTests
    {
        [TestMethod]
        public void PeriodicCommand_TestAbort()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new PauseCommand(TimeSpan.FromMilliseconds(10)),
                int.MaxValue,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseAfter,
                false))
            {
                AbortTest.Run(periodicCmd, null, 20);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new PauseCommand(TimeSpan.FromMilliseconds(10)),
                int.MaxValue,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseBefore,
                false))
            {
                AbortTest.Run(periodicCmd, null, 20);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
				new PauseCommand(TimeSpan.FromMilliseconds(10)),
                int.MaxValue,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseAfter,
                true))
            {
                AbortTest.Run(periodicCmd, null, 20);
            }
        }

        [TestMethod]
        public void PeriodicCommand_TestHappyPath()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                5,
                TimeSpan.FromMilliseconds(1),
                PeriodicCommand.IntervalType.PauseAfter,
                false))
            {
                HappyPathTest.Run(periodicCmd, 0, null);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(2),
                5,
                TimeSpan.FromMilliseconds(1),
                PeriodicCommand.IntervalType.PauseBefore,
                false))
            {
                HappyPathTest.Run(periodicCmd, 0, null);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(2),
                5,
                TimeSpan.FromMilliseconds(10),
                PeriodicCommand.IntervalType.PauseAfter,
                true))
            {
                HappyPathTest.Run(periodicCmd, 0, null);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                0,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseBefore,
                true))
            {
                HappyPathTest.Run(periodicCmd, 0, null);
            }

            try
            {
                using (PeriodicCommand periodicCmd = new PeriodicCommand(
                    new AddCommand(1),
                    0,
                    TimeSpan.FromDays(1),
                    (PeriodicCommand.IntervalType)27,
                    true))
                {
                    Assert.Fail("Invalid interval type was allowed");
                }
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        public void PeriodicCommand_TestFail()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new FailingCommand(),
                5,
                TimeSpan.FromMilliseconds(1),
                PeriodicCommand.IntervalType.PauseAfter,
                false))
            {
                FailTest.Run<FailingCommand.FailException>(periodicCmd, null);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new FailingCommand(),
                5,
                TimeSpan.FromMilliseconds(1),
                PeriodicCommand.IntervalType.PauseBefore,
                false))
            {
                FailTest.Run<FailingCommand.FailException>(periodicCmd, null);
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new FailingCommand(),
                5,
                TimeSpan.FromMilliseconds(1),
                PeriodicCommand.IntervalType.PauseAfter,
                true))
            {
                FailTest.Run<FailingCommand.FailException>(periodicCmd, null);
            }
        }

        [TestMethod]
        public void PeriodicCommand_TestSkipCurrentWait()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                3,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseAfter,
                false))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give the async routine a moment to get going

                for(int i = 0; i < 2; ++i)
                {
                    periodicCmd.SkipCurrentWait();
                    System.Threading.Thread.Sleep(100); // give a little time for the next repetition to start
                }

                periodicCmd.Wait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.SkipCurrentWait(); // no-op
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(20); // give the async routine a moment to get going

                for (int i = 0; i < 1; ++i)
                {
                    periodicCmd.SkipCurrentWait();
                    System.Threading.Thread.Sleep(20); // give a little time for the next repetition to start
                }

                Assert.IsFalse(periodicCmd.Wait(TimeSpan.FromMilliseconds(10)));
                periodicCmd.SkipCurrentWait();
                periodicCmd.Wait();
                listener.Check();
            }
        }

        [TestMethod]
        public void PeriodicCommand_TestReset()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                2,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseBefore,
                true))
            {
                Assert.AreEqual(TimeSpan.FromDays(1), periodicCmd.Interval);
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(200); // give time for the command to start
                periodicCmd.Interval = TimeSpan.FromMilliseconds(1);
                Assert.IsFalse(periodicCmd.Wait(TimeSpan.FromMilliseconds(10)));
                periodicCmd.Reset();
                Assert.IsTrue(periodicCmd.Wait(TimeSpan.FromMilliseconds(1000)));
                listener.Check();
                Assert.AreEqual(TimeSpan.FromMilliseconds(1), periodicCmd.Interval);

                listener.Reset(CmdListener.CallbackType.Aborted, null);
                periodicCmd.Interval = TimeSpan.FromMilliseconds(100);
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(20); // give the async routine a moment to get going
                periodicCmd.Interval = TimeSpan.FromDays(1);
                periodicCmd.Reset();
                Assert.IsFalse(periodicCmd.Wait(TimeSpan.FromMilliseconds(100)));
                periodicCmd.AbortAndWait();
                listener.Check();
            }

            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                1,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseBefore,
                true))
            {
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(100); // give time for the command to start
                periodicCmd.Reset();
                periodicCmd.SkipCurrentWait();
                periodicCmd.Wait();
                listener.Check();
            }
        }

        [TestMethod]
        public void PeriodicCommand_TestStop()
        {
            using (PeriodicCommand periodicCmd = new PeriodicCommand(
                new AddCommand(1),
                2,
                TimeSpan.FromDays(1),
                PeriodicCommand.IntervalType.PauseBefore,
                true))
            {
                periodicCmd.Stop();
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 0);
                System.Threading.Thread.Sleep(10); // give time for the command to start
                periodicCmd.Stop();
                periodicCmd.Wait();
                listener.Check();
                periodicCmd.Stop();

                listener.Reset(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 2);
                periodicCmd.Stop();
                periodicCmd.Wait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Succeeded, null);
                periodicCmd.AsyncExecute(listener, 2);
                periodicCmd.RepeatCount = long.MaxValue;
                periodicCmd.Interval = TimeSpan.FromTicks(0);
                periodicCmd.SkipCurrentWait();
                System.Threading.Thread.Sleep(10); // give time for the command to execute many times
                periodicCmd.Stop();
                periodicCmd.Wait();
                listener.Check();
            }

            using (System.Threading.ManualResetEvent stopEvent = new System.Threading.ManualResetEvent(false))
            {
                using (PeriodicCommand periodicCmd = new PeriodicCommand(
                    new AddCommand(1),
                    2,
                    TimeSpan.FromDays(1),
                    PeriodicCommand.IntervalType.PauseBefore,
                    true,
                    stopEvent))
                {
                    CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, null);
                    periodicCmd.AsyncExecute(listener, 0);
                    System.Threading.Thread.Sleep(10); // give time for the command to start
                    stopEvent.Set();
                    periodicCmd.Wait();
                    listener.Check();

                    listener.Reset(CmdListener.CallbackType.Succeeded, null);
                    periodicCmd.AsyncExecute(listener, 2);
                    periodicCmd.Wait();
                    listener.Check();

                    stopEvent.Reset();
                    listener.Reset(CmdListener.CallbackType.Succeeded, null);
                    periodicCmd.AsyncExecute(listener, 2);
                    periodicCmd.RepeatCount = long.MaxValue;
                    periodicCmd.Interval = TimeSpan.FromTicks(0);
                    periodicCmd.SkipCurrentWait();
                    System.Threading.Thread.Sleep(10); // give time for the command to execute many times
                    stopEvent.Set();
                    periodicCmd.Wait();
                    listener.Check();
                }
            }
        }
    }
}
