using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class SequentialCommandsTests
    {
        [TestMethod]
        public void SequentialCommand_TestHappyPath()
        {
            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                HappyPathTest.Run(seqCmds, null, null);
                const int count = 10;

                for (int i = 0; i < count; ++i)
                {
                    seqCmds.Add(new AddCommand(1));
                }

                HappyPathTest.Run(seqCmds, 0, count);
                seqCmds.Clear();
                HappyPathTest.Run(seqCmds, 1, 1);
                seqCmds.Clear();
            }
        }

        [TestMethod]
        public void SequentialCommand_TestAbort()
        {
            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                seqCmds.Add(new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(10)));
                seqCmds.Add(new NeverEndingAsyncCommand());
                AbortTest.Run(seqCmds, null, 20);
            }

            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                seqCmds.Add(new NeverEndingAsyncCommand());
                seqCmds.Add(new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(10)));
                AbortTest.Run(seqCmds, null, 20);
            }
        }

        [TestMethod]
        public void SequentialCommand_TestFail()
        {
            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                seqCmds.Add(new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(0)));
                seqCmds.Add(new FailingCommand());
                FailTest.Run<FailingCommand.FailException>(seqCmds, null);
            }

            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                seqCmds.Add(new FailingCommand());
                seqCmds.Add(new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(0)));
                FailTest.Run<FailingCommand.FailException>(seqCmds, null);
            }

            using (CommandLib.SequentialCommands seqCmds = new CommandLib.SequentialCommands())
            {
                seqCmds.Add(new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(0)));
                seqCmds.Add(new AddCommand(1)); // won't get the right type passed in
                FailTest.Run<InvalidCastException>(seqCmds, new Object());
            }
        }
    }
}
