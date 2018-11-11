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
            HappyPathTest.Run(new SequentialCommands(), null, null);
            var seqCmds = new SequentialCommands();
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

            seqCmds = new SequentialCommands();

            for (int i = 0; i < count; ++i)
            {
                seqCmds.Add(new AddCommand(1));
            }

            seqCmds.Clear();
            HappyPathTest.Run(seqCmds, 1, null);

            seqCmds = new SequentialCommands();
            seqCmds.Clear();
            seqCmds.Add(new PauseCommand(TimeSpan.FromTicks(1)));
            seqCmds.Add(new DelayCommand(TimeSpan.FromTicks(1)));
            seqCmds.Add(new PauseCommand(TimeSpan.FromTicks(1)));
            HappyPathTest.Run(seqCmds, 0, null);
        }

        [TestMethod]
        public void SequentialCommand_TestAbort()
        {
            var seqCmds = new SequentialCommands();
            seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
            seqCmds.Add(new NeverEndingAsyncCommand());
            AbortTest.Run(seqCmds, null, 20);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new NeverEndingAsyncCommand());
            seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
            AbortTest.Run(seqCmds, null, 20);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new DeafAsyncCommand(100));
            seqCmds.Add(new DeafSyncCommand(100000));
            AbortTest.Run(seqCmds, null, 5);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new DeafSyncCommand(100));
            seqCmds.Add(new DeafAsyncCommand(100000));
            AbortTest.Run(seqCmds, null, 5);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new DeafAsyncCommand(0));
            seqCmds.Add(new DeafSyncCommand(100));
            seqCmds.Add(new DeafSyncCommand(100000));
            AbortTest.Run(seqCmds, null, 5);
        }

        [TestMethod]
        public void SequentialCommand_TestFail()
        {
            var seqCmds = new SequentialCommands();
            seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            seqCmds.Add(new FailingCommand());
            FailTest.Run<FailingCommand.FailException>(seqCmds, null);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new FailingCommand());
            seqCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            FailTest.Run<FailingCommand.FailException>(seqCmds, null);

            seqCmds = new SequentialCommands();
            seqCmds.Add(new DelayCommand(TimeSpan.FromMilliseconds(0)));
            seqCmds.Add(new FailingCommand());
            FailTest.Run<FailingCommand.FailException>(seqCmds, null);
        }

        private class DeafSyncCommand : SyncCommand
        {
            public DeafSyncCommand(int milliseconds) : base(null)
            {
                _milliseconds = milliseconds;
            }

            protected override object SyncExeImpl(object runtimeArg)
            {
                System.Threading.Thread.Sleep(_milliseconds);
                return null;
            }

            private readonly int _milliseconds;
        }

        private class DeafAsyncCommand : AsyncCommand
        {
            public DeafAsyncCommand(int milliseconds) : base(null)
            {
                _milliseconds = milliseconds;
            }

            protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
            {
                var cmd = new DelegateCommand<object>(o =>
                {
                    System.Threading.Thread.Sleep(_milliseconds);
                    return o;
                }, this);

                cmd.AsyncExecute(listener, runtimeArg);
            }

            private readonly int _milliseconds;
        }
    }
}
