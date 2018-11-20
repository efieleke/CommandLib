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
                HappyPathTest.Run(GenerateComplexCommand(abortEvent, 1, false), null, null);
            }
        }

        [TestMethod]
        public void ComplexCommand_TestFail()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                FailTest.Run<FailingCommand.FailException>(GenerateComplexCommand(abortEvent, 1, true), null);
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
                Command test = GenerateComplexCommand(abortEvent, 1, false);
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

        private Command GenerateComplexCommand(System.Threading.ManualResetEvent abortEvent, int maxPauseMS, bool insertFailure)
        {
            return new AbortSignaledCommand(new ComplexCommand(maxPauseMS, insertFailure), abortEvent);
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
                var combined = new ParallelCommands(0);
                combined.Add(seq);
                combined.Add(parallel);
                _cmd = new PeriodicCommand(
                    combined, 3, TimeSpan.FromMilliseconds(maxPauseMS), PeriodicCommand.IntervalType.PauseAfter, true, null, this);

                // For code coverage. Also, gives us an opportunity to try the third overload of SyncExecute.
                RelinquishOwnership(_cmd);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _cmd.Dispose();
                }

                base.Dispose(disposing);
            }

            protected sealed override object SyncExecuteImpl(object runtimeArg)
            {
                try
                {
                    RelinquishOwnership(_cmd);
                }
                catch (InvalidOperationException)
                {
                }

                using (PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromTicks(0)))
                {
                    string _ = "test";
                    
                    if ((string)pauseCmd.SyncExecute(_, this) != _)
                    {
                        throw new Exception("Unexpected return value from pause command execution");
                    }
                }

                return _cmd.SyncExecute(runtimeArg, this);
            }

            private static ParallelCommands GenerateParallelCommands(int maxPauseMS, bool insertFailure)
            {
                var cmds = new ParallelCommands(0);

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
                        insertFailure ? new FailingCommand() : (Command)new NoOpCommand(),
                        5, TimeSpan.FromMilliseconds(maxPauseMS),
                        PeriodicCommand.IntervalType.PauseBefore,
                        true)
                };
            }

            private readonly Command _cmd;
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

        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            return null;
        }
    }
}
