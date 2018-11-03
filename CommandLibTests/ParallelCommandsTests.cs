using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class ParallelCommandsTests
    {
        [TestMethod]
        public void ParallelCommands_TestHappyPath()
        {
            TestHappyPath(false);
            TestHappyPath(true);
        }

        private void TestHappyPath(bool abortUponFailure)
        {
            ParallelCommands.Behavior behavior = abortUponFailure
                ? ParallelCommands.Behavior.AbortUponFailure | ParallelCommands.Behavior.AggregateErrors
                : ParallelCommands.Behavior.AggregateErrors;

            var parallelCmds = new ParallelCommands(behavior);
            HappyPathTest.Run(parallelCmds, null, null);
            parallelCmds.Dispose();
            parallelCmds = new ParallelCommands(behavior);
            const int count = 10;

            for (int i = 0; i < count; ++i)
            {
                parallelCmds.Add(new AddCommand(1));
            }

            HappyPathTest.Run(parallelCmds, 0, null);
            parallelCmds.Clear();
            HappyPathTest.Run(parallelCmds, 0, null);
            parallelCmds.Dispose();
        }

        [TestMethod]
        public void ParallelCommands_TestAbort()
        {
            TestAbort(false);
            TestAbort(true);
        }

        private void TestAbort(bool abortUponFailure)
        {
            ParallelCommands.Behavior behavior = abortUponFailure
                ? ParallelCommands.Behavior.AbortUponFailure | ParallelCommands.Behavior.AggregateErrors
                : ParallelCommands.Behavior.AggregateErrors;

            using (var parallelCmds = new ParallelCommands(behavior))
            {
                parallelCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                parallelCmds.Add(new NeverEndingAsyncCommand());
                AbortTest.Run(parallelCmds, null, 5);
            }

            using (var parallelCmds = new ParallelCommands(behavior))
            {
                parallelCmds.Add(new NeverEndingAsyncCommand());
                parallelCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                AbortTest.Run(parallelCmds, null, 20);
            }

            using (var parallelCmds = new ParallelCommands(behavior))
            {
                parallelCmds.Add(new NeverEndingAsyncCommand());
                parallelCmds.Add(new NeverEndingAsyncCommand());
                AbortTest.Run(parallelCmds, null, 20);
            }
        }

        [TestMethod]
        public void ParallelCommands_TestFail()
        {
            TestFail(false);
            TestFail(true);
        }

        private void TestFail(bool abortUponFailure)
        {
            ParallelCommands.Behavior behavior = abortUponFailure
                ? ParallelCommands.Behavior.AbortUponFailure | ParallelCommands.Behavior.AggregateErrors
                : ParallelCommands.Behavior.AggregateErrors;

            var parallelCmds = new ParallelCommands(behavior);
            parallelCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            parallelCmds.Add(new FailingCommand());
            FailTest.Run<AggregateException>(parallelCmds, null);
            //FailTest.Run<FailingCommand.FailException>(parallelCmds, null);
            parallelCmds.Dispose();

            parallelCmds = new ParallelCommands(behavior);
            parallelCmds.Add(new FailingCommand());
            parallelCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            FailTest.Run<AggregateException>(parallelCmds, null);
            parallelCmds.Dispose();

            parallelCmds = new ParallelCommands(behavior);
            parallelCmds.Add(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            parallelCmds.Add(new FailingCommand());
            FailTest.Run<AggregateException>(parallelCmds, null);
            parallelCmds.Dispose();
        }
    }
}
