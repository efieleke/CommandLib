using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class ComplexCommandTest
    {
        [TestMethod]
        public void ComplexCommand_TestHappyPath()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                using (CommandLib.Command test = GenerateComplexCommand(abortEvent, 1, false))
                {
                    HappyPathTest.Run(test, null, null);
                }
            }
        }

        [TestMethod]
        public void ComplexCommand_TestFail()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                using (CommandLib.Command test = GenerateComplexCommand(abortEvent, 1, true))
                {
                    FailTest.Run<FailingCommand.FailException>(test, null);
                }
            }
        }

        [TestMethod]
        public void ComplexCommand_TestNestedCommandAbort()
        {
            CommandLib.SequentialCommands outerSeq = new CommandLib.SequentialCommands();
            CommandLib.SequentialCommands innerSeq = new CommandLib.SequentialCommands();
            innerSeq.Add(new NeverEndingAsyncCommand());
            outerSeq.Add(innerSeq);
            outerSeq.AsyncExecute(new CmdListener(CmdListener.CallbackType.Aborted, null));
            System.Threading.Thread.Sleep(10);
            outerSeq.AbortAndWait();
        }

        [TestMethod]
        public void ComplexCommand_TestAbort()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                using (CommandLib.Command test = GenerateComplexCommand(abortEvent, 1, false))
                {
                    CmdListener listener = new CmdListener(CmdListener.CallbackType.Aborted, null);
                    test.AsyncExecute(listener, null);
                    abortEvent.Set();
                    test.Wait();
                    abortEvent.Reset();
                    listener.Check();

                    listener.Reset(CmdListener.CallbackType.Aborted, null);
                    test.AsyncExecute(listener, null);
                    System.Threading.Thread.Sleep(10);
                    abortEvent.Set();
                    test.Wait();
                    listener.Check();

                    AbortTest.Run(test, null, 10);
                }
            }
        }

        private CommandLib.Command GenerateComplexCommand(System.Threading.ManualResetEvent abortEvent, int maxPauseMS, bool insertFailure)
        {
            return new CommandLib.AbortEventedCommand(new ComplexCommand(maxPauseMS, insertFailure), abortEvent);
        }


        private class ComplexCommand : CommandLib.SyncCommand
        {
            internal ComplexCommand(int maxPauseMS, bool insertFailure) : base(null)
            {
                container = new CommandLib.VariableCommand(this);
                CommandLib.ParallelCommands parallel = GenerateParallelCommands(maxPauseMS, insertFailure);
                CommandLib.SequentialCommands seq = GenerateSequentialCommands(maxPauseMS, insertFailure);
                parallel.Add(GenerateSequentialCommands(maxPauseMS, insertFailure));
                parallel.Add(GenerateParallelCommands(maxPauseMS, insertFailure));
                seq.Add(GenerateParallelCommands(maxPauseMS, insertFailure));
                seq.Add(GenerateSequentialCommands(maxPauseMS, insertFailure));
                CommandLib.ParallelCommands combined = new CommandLib.ParallelCommands(false);
                combined.Add(seq);
                combined.Add(parallel);

                container.CommandToRun = new CommandLib.PeriodicCommand(
                    combined, 3, TimeSpan.FromMilliseconds(maxPauseMS), CommandLib.PeriodicCommand.IntervalType.PauseAfter, true);
            }

            protected sealed override object SyncExeImpl(object runtimeArg)
            {
                RelinquishOwnership(container);

                try
                {
                    RelinquishOwnership(container);
                }
                catch (InvalidOperationException)
                {
                }

                TakeOwnership(container);
                return container.SyncExecute(runtimeArg);
            }

            private static CommandLib.ParallelCommands GenerateParallelCommands(int maxPauseMS, bool insertFailure)
            {
                CommandLib.ParallelCommands cmds = new CommandLib.ParallelCommands(false);

                foreach (CommandLib.Command cmd in GenerateCommands(maxPauseMS, insertFailure))
                {
                    cmds.Add(cmd);
                }

                return cmds;
            }

            private static CommandLib.SequentialCommands GenerateSequentialCommands(int maxPauseMS, bool insertFailure)
            {
                CommandLib.SequentialCommands cmds = new CommandLib.SequentialCommands();

                foreach (CommandLib.Command cmd in GenerateCommands(maxPauseMS, insertFailure))
                {
                    cmds.Add(cmd);
                }

                return cmds;
            }

            private static CommandLib.Command[] GenerateCommands(int maxPauseMS, bool insertFailure)
            {
                return new CommandLib.Command[]
                {
                    new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(maxPauseMS)),
                    new NoOpCommand(),
                    new CommandLib.PeriodicCommand(
                        insertFailure ? (CommandLib.Command)new FailingCommand() : (CommandLib.Command)new NoOpCommand(),
                        5, TimeSpan.FromMilliseconds(maxPauseMS),
                        CommandLib.PeriodicCommand.IntervalType.PauseBefore,
                        true)
                };
            }

            private CommandLib.VariableCommand container;
        }
    }

    internal class NoOpCommand : CommandLib.SyncCommand
    {
        internal NoOpCommand()
            : this(null)
        {
        }

        internal NoOpCommand(CommandLib.Command owner)
            : base(owner)
        {
        }

        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            return null;
        }
    }
}
