using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class SequentialCommandsTests
    {
        [TestMethod]
        public void SequentialCommand_TestHappyPath()
        {
            using (SequentialCommands seqCmds = new SequentialCommands())
            {
                HappyPathTest.Run(seqCmds, null, null);
                Assert.IsFalse(seqCmds.Commands.GetEnumerator().MoveNext());
                const int count = 10;

                for (int i = 0; i < count; ++i)
                {
                    seqCmds.Add(new AddCommand(1));
                }

                int addedCount = 0;

                foreach (Command cmd in seqCmds.Commands)
                {
                    Assert.IsTrue(cmd is AddCommand);
                    ++addedCount;
                }

                Assert.AreEqual(addedCount, count);
                HappyPathTest.Run(seqCmds, 0, null);
                seqCmds.Clear();
                HappyPathTest.Run(seqCmds, 1, null);
                seqCmds.Clear();

                seqCmds.Add(new PauseCommand(TimeSpan.FromTicks(1)));
                seqCmds.Add(new DelayCommand(TimeSpan.FromTicks(1)));
                seqCmds.Add(new PauseCommand(TimeSpan.FromTicks(1)));
                HappyPathTest.Run(seqCmds, 0, null);
            }
        }

        [TestMethod]
        public void SequentialCommand_TestAbort()
        {
            using (SequentialCommands seqCmds = new SequentialCommands())
            {
                seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                seqCmds.Add(new NeverEndingAsyncCommand());
                AbortTest.Run(seqCmds, null, 20);
            }

            using (SequentialCommands seqCmds = new SequentialCommands())
            {
                seqCmds.Add(new NeverEndingAsyncCommand());
                seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                AbortTest.Run(seqCmds, null, 20);
            }
        }

        [TestMethod]
        public void SequentialCommand_TestFail()
        {
            using (SequentialCommands seqCmds = new SequentialCommands())
            {
                seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                seqCmds.Add(new FailingCommand());
                FailTest.Run<FailingCommand.FailException>(seqCmds, null);
            }

            using (SequentialCommands seqCmds = new SequentialCommands())
            {
                seqCmds.Add(new FailingCommand());
                seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                FailTest.Run<FailingCommand.FailException>(seqCmds, null);
            }
        }
    }
}
