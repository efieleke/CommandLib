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
            AbortTest.Run(new MockFinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Succeed), null, 10);
            AbortTest.Run(new MockFinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Succeed), null, 10);
            AbortTest.Run(new MockFinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Abort), null, 10);
            AbortTest.Run(new MockFinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Abort), null, 10);
            AbortTest.Run(new MockFinallyCommand(new PauseCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Fail), null, 10);
            AbortTest.Run(new MockFinallyCommand(new DelayCommand(TimeSpan.FromDays(1)), MockFinallyCommand.Behavior.Fail), null, 10);
            AbortTest.Run(new MockFinallyCommand(new PauseCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Abort), null, 10);
            AbortTest.Run(new MockFinallyCommand(new DelayCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Abort), null, 10);
        }

        [TestMethod]
        public void FinallyCommand_TestHappyPath()
        {
            HappyPathTest.Run(new MockFinallyCommand(new AddCommand(6), MockFinallyCommand.Behavior.Succeed), 2, 8);
            HappyPathTest.Run(new MockFinallyCommand(new PauseCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Succeed), null, null);
            HappyPathTest.Run(new MockFinallyCommand(new DelayCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Succeed), null, true);
        }

        [TestMethod]
        public void FinallyCommand_TestFail()
        {
            FailTest.Run<FailingCommand.FailException>(new MockFinallyCommand(new FailingCommand(), MockFinallyCommand.Behavior.Succeed), null);
            FailTest.Run<AggregateException>(new MockFinallyCommand(new FailingCommand(), MockFinallyCommand.Behavior.Fail), null);
            FailTest.Run<AggregateException>(new MockFinallyCommand(new FailingCommand(), MockFinallyCommand.Behavior.Abort), null);
            FailTest.Run<MockException>(new MockFinallyCommand(new PauseCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Fail), null);
            FailTest.Run<MockException>(new MockFinallyCommand(new DelayCommand(TimeSpan.Zero), MockFinallyCommand.Behavior.Fail), null);
        }

        private class MockException : Exception {}

        private class MockFinallyCommand : FinallyCommand
        {
            internal enum Behavior { Succeed, Fail, Abort }

            public MockFinallyCommand(Command command, Behavior behavior) : base(command)
            {
                _behavior = behavior;
            }

            protected override void Finally()
            {
                switch (_behavior)
                {
                    case Behavior.Succeed:
                        return;
                    case Behavior.Abort:
                        throw new CommandAbortedException();
                    case Behavior.Fail:
                        throw new MockException();
                    default:
                        Assert.Fail("Shouldn't ever get here");
                        return;
                }
            }

            private readonly Behavior _behavior;
        }
    }
}
