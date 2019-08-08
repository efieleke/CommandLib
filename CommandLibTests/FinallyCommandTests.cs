using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class FinallyCommandTests
    {
        [TestMethod]
        public void FinallyCommand_TestAbort()
        {
            AbortTest.Run(new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), null, 10);
            AbortTest.Run(new FinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), null, 10);
            AbortTest.Run(new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Abort), false), null, 10);
            AbortTest.Run(new FinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Abort), false), null, 10);
            AbortTest.Run(new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Fail), false), null, 10);
            AbortTest.Run(new FinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), new CleanupCommand(CleanupCommand.Behavior.Fail), false), null, 10);

            var cleanupCmd = new CleanupCommand(CleanupCommand.Behavior.Succeed);

            using (var cmd = new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), cleanupCmd, false))
            {
                cmd.AsyncExecute(o => { }, () => { }, e => { });
                cmd.AbortAndWait();
                Assert.IsFalse(cleanupCmd.Executed);
            }

            cleanupCmd = new CleanupCommand(CleanupCommand.Behavior.Succeed);

            using (var cmd = new FinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), cleanupCmd, false))
            {
                cmd.AsyncExecute(o => { }, () => { }, e => { });
                cmd.AbortAndWait();
                Assert.IsFalse(cleanupCmd.Executed);
            }

            cleanupCmd = new CleanupCommand(CleanupCommand.Behavior.Succeed);

            using (var cmd = new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), cleanupCmd, true))
            {
                cmd.AsyncExecute(o => { }, () => { }, e => { });
                cmd.AbortAndWait();
                Assert.IsTrue(cleanupCmd.Executed);
            }

            cleanupCmd = new CleanupCommand(CleanupCommand.Behavior.Succeed);

            using (var cmd = new FinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), cleanupCmd, true))
            {
                cmd.AsyncExecute(o => { }, () => { }, e => { });
                cmd.AbortAndWait();
                Assert.IsTrue(cleanupCmd.Executed);
            }
        }

        [TestMethod]
        public void FinallyCommand_TestHappyPath()
        {
            HappyPathTest.Run(new FinallyCommand(new AddCommand(6), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), 2, 8);
            HappyPathTest.Run(new FinallyCommand(new PauseCommand(TimeSpan.Zero), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), null, null);
            HappyPathTest.Run(new FinallyCommand(new DelayCommand(TimeSpan.Zero), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), null, true);
        }

        [TestMethod]
        public void FinallyCommand_TestFail()
        {
            FailTest.Run<FailingCommand.FailException>(new FinallyCommand(new FailingCommand(), new CleanupCommand(CleanupCommand.Behavior.Succeed), false), null);
            FailTest.Run<AggregateException>(new FinallyCommand(new FailingCommand(), new CleanupCommand(CleanupCommand.Behavior.Fail), false), null);
            FailTest.Run<AggregateException>(new FinallyCommand(new FailingCommand(), new CleanupCommand(CleanupCommand.Behavior.Abort), false), null);
            FailTest.Run<MockException>(new FinallyCommand(new PauseCommand(TimeSpan.Zero), new CleanupCommand(CleanupCommand.Behavior.Fail), false), null);
            FailTest.Run<MockException>(new FinallyCommand(new DelayCommand(TimeSpan.Zero), new CleanupCommand(CleanupCommand.Behavior.Fail), false), null);
        }

        private class MockException : Exception {}

        private class CleanupCommand : SyncCommand
        {
            internal enum Behavior { Succeed, Fail, Abort }

            public CleanupCommand(Behavior behavior) : base(null)
            {
                _behavior = behavior;
            }

            internal bool Executed { get; set; }

            protected override object SyncExecuteImpl(object runtimeArg)
            {
                CheckAbortFlag();
                Executed = true;

                switch (_behavior)
                {
                    case Behavior.Succeed:
                        return true;
                    case Behavior.Abort:
                        throw new CommandAbortedException();
                    case Behavior.Fail:
                        throw new MockException();
                    default:
                        Assert.Fail("Should not get here");
                        return false;
                }
            }

            private readonly Behavior _behavior;
        }
    }
}
