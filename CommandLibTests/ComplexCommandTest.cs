using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

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
                using (Command test = GenerateComplexCommand(abortEvent, 1, false))
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
                using (Command test = GenerateComplexCommand(abortEvent, 1, true))
                {
                    FailTest.Run<FailingCommand.FailException>(test, null);
                }
            }
        }

        [TestMethod]
        public void ComplexCommand_TestNestedCommandAbort()
        {
            SequentialCommands outerSeq = new SequentialCommands();
            SequentialCommands innerSeq = new SequentialCommands();
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
                using (Command test = GenerateComplexCommand(abortEvent, 1, false))
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

        private Command GenerateComplexCommand(System.Threading.ManualResetEvent abortEvent, int maxPauseMS, bool insertFailure)
        {
            return new AbortEventedCommand(new ComplexCommand(maxPauseMS, insertFailure), abortEvent);
        }

        private class ComplexCommand : SyncCommand
        {
            internal ComplexCommand(int maxPauseMS, bool insertFailure) : base(null)
            {
                ParallelCommands parallel = GenerateParallelCommands(maxPauseMS, insertFailure);
                SequentialCommands seq = GenerateSequentialCommands(maxPauseMS, insertFailure);
                parallel.Add(GenerateSequentialCommands(maxPauseMS, insertFailure));
                parallel.Add(GenerateParallelCommands(maxPauseMS, insertFailure));
                seq.Add(GenerateParallelCommands(maxPauseMS, insertFailure));
                seq.Add(GenerateSequentialCommands(maxPauseMS, insertFailure));
                ParallelCommands combined = new ParallelCommands(false);
                combined.Add(seq);
                combined.Add(parallel);
                cmd = new PeriodicCommand(
                    combined, 3, TimeSpan.FromMilliseconds(maxPauseMS), PeriodicCommand.IntervalType.PauseAfter, true, null, this);

                // For code coverage. Also, gives us an opportunity to try the third overload of SyncExecute.
                RelinquishOwnership(cmd);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    cmd.Dispose();
                }

                base.Dispose(disposing);
            }

            protected sealed override object SyncExeImpl(object runtimeArg)
            {
                try
                {
                    RelinquishOwnership(cmd);
                }
                catch (InvalidOperationException)
                {
                }

                using (PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromTicks(0)))
                {
                    String dontCare = "test";
                    
                    if (pauseCmd.SyncExecute<String>(dontCare, this) != dontCare)
                    {
                        throw new Exception("Unexpected return value from pause command execution");
                    }
                }

                return cmd.SyncExecute(runtimeArg, this);
            }

            private static ParallelCommands GenerateParallelCommands(int maxPauseMS, bool insertFailure)
            {
                ParallelCommands cmds = new ParallelCommands(false);

                foreach (Command cmd in GenerateCommands(maxPauseMS, insertFailure))
                {
                    cmds.Add(cmd);
                }

                return cmds;
            }

            private static SequentialCommands GenerateSequentialCommands(int maxPauseMS, bool insertFailure)
            {
                SequentialCommands cmds = new SequentialCommands();

                foreach (Command cmd in GenerateCommands(maxPauseMS, insertFailure))
                {
                    cmds.Add(cmd);
                }

                return cmds;
            }

            private static Command[] GenerateCommands(int maxPauseMS, bool insertFailure)
            {
                return new Command[]
                {
                    new PauseCommand(TimeSpan.FromMilliseconds(maxPauseMS)),
                    new NoOpCommand(),
                    new PeriodicCommand(
                        insertFailure ? (Command)new FailingCommand() : (Command)new NoOpCommand(),
                        5, TimeSpan.FromMilliseconds(maxPauseMS),
                        PeriodicCommand.IntervalType.PauseBefore,
                        true)
                };
            }

            private Command cmd;
        }
    }

    internal class NoOpCommand : SyncCommand
    {
        internal NoOpCommand()
            : this(null)
        {
        }

        internal NoOpCommand(Command owner)
            : base(owner)
        {
        }

        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            return null;
        }
    }
}
