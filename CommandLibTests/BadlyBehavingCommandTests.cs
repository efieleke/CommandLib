using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class BadlyBehavingCommandTests
    {
        [TestMethod]
        public void TestAbortGrandchild()
        {
            using (var cmd = new BadlyBehavingCommand())
            {
                try
                {
                    cmd.AbortGrandchild();
                    Assert.Fail("Did not expect to get here");
                }
                catch (ArgumentException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void TestResetGrandchildAbortEvent()
        {
            using (var cmd = new BadlyBehavingCommand())
            {
                try
                {
                    cmd.ResetGrandchildAbortEvent();
                    Assert.Fail("Did not expect to get here");
                }
                catch (ArgumentException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void TestTakeOwnershipOfOwnedCommand()
        {
            using (var cmd = new BadlyBehavingCommand())
            {
                try
                {
                    cmd.TakeOwnershipOfOwnedCommand();
                    Assert.Fail("Did not expect to get here");
                }
                catch (InvalidOperationException)
                {
                    // expected
                }
            }
        }

        private class BadlyBehavingCommand : SyncCommand
        {
            internal BadlyBehavingCommand() : base(null)
            {
            }

            protected override object SyncExecuteImpl(object runtimeArg)
            {
                throw new NotImplementedException();
            }

            internal void AbortGrandchild()
            {
                using (var cmdSequence = new SequentialCommands())
                {
                    cmdSequence.Add(new PauseCommand(TimeSpan.Zero));
                    TakeOwnership(cmdSequence);

                    try
                    {
                        AbortChildCommand(cmdSequence.Commands.First());
                    }
                    finally
                    {
                        RelinquishOwnership(cmdSequence);
                    }
                }
            }

            internal void ResetGrandchildAbortEvent()
            {
                using (var cmdSequence = new SequentialCommands())
                {
                    cmdSequence.Add(new PauseCommand(TimeSpan.Zero));
                    TakeOwnership(cmdSequence);

                    try
                    {
                        ResetChildAbortEvent(cmdSequence.Commands.First());
                    }
                    finally
                    {
                        RelinquishOwnership(cmdSequence);
                    }
                }
            }

            internal void TakeOwnershipOfOwnedCommand()
            {
                using (var cmdSequence = new SequentialCommands(this))
                {
                    TakeOwnership(cmdSequence);
                }
            }
        }
    }
}
