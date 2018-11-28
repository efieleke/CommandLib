using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class TaskCommandTests
    {
        [TestMethod]
        public void Command_TestFromCommand()
        {
            var cmd = new DoNothingCommand();

            using (Task<int> task = cmd.RunAsTask<int>(7))
            {
                Assert.AreEqual(7, task.Result);
            }

            cmd = new DoNothingCommand();

            using (Task<int> task = cmd.RunAsTask<int>(5))
            {
                Assert.AreEqual(5, task.Result);
            }
        }

        [TestMethod]
        public void TaskCommand_TestSuccess()
        {
            HappyPathTest.Run(new DoNothingCommand(), null, 0);
            HappyPathTest.Run(new DoNothingCommand(), 5, 5);
            HappyPathTest.Run(new DoNothingCommand(), 4, 4);

            var cmd = new DoNothingCommand();
            Assert.AreEqual(0, cmd.SyncExecute());
            Assert.AreEqual(1, cmd.SyncExecute(1));
            Assert.AreEqual(4, cmd.SyncExecute((object)4));
            Assert.AreEqual(7, cmd.SyncExecute(7, null));
            Assert.AreEqual(7, cmd.SyncExecute((object)7, null));

            cmd.AsyncExecute(i => Assert.AreEqual(0, i), () => { }, e => { });
            cmd.Wait();
            cmd.AsyncExecute(i => Assert.AreEqual(2, i), () => { }, e => { }, 2);
            cmd.Wait();
            cmd.AsyncExecute(new TestCommandListener<int>(0));
            cmd.Wait();
            cmd.AsyncExecute(new TestCommandListener<int>(3), 3);
            cmd.Wait();

            HappyPathTest.Run(new DoNothingCommand(DoNothingCommand.Behavior.SucceedSynchronously), 5, 5);
            HappyPathTest.Run(new DoNothingCommand(DoNothingCommand.Behavior.SucceedSynchronously), 4, 4);
        }

        internal class TestCommandListener<TResult> : ICommandListener<TResult>
        {
            internal TestCommandListener(TResult expected)
            {
                _expected = expected;
            }

            public void CommandSucceeded(TResult result)
            {
                Assert.AreEqual(_expected, result);
            }

            public void CommandAborted()
            {
                throw new NotImplementedException();
            }

            public void CommandFailed(Exception exc)
            {
                throw exc;
            }

            private readonly TResult _expected;
        }
        [TestMethod]
        public void TaskCommand_TestError()
        {
            FailTest.Run<NotSupportedException>(new DoNothingCommand(DoNothingCommand.Behavior.FailUpFront), 7);
            FailTest.Run<InvalidOperationException>(new DoNothingCommand(DoNothingCommand.Behavior.FailInTask), 7);
        }

        [TestMethod]
        public void TaskCommand_TestAbort()
        {
            AbortTest.Run(new DoNothingCommand(DoNothingCommand.Behavior.Abort), 7, 0);
        }

        [TestMethod]
        public void FromTask_TestSuccess()
        {
            HappyPathTest.Run(TaskCommand<object, int>.Create(c => AddOneTask(1, null)), 0, 2);
            HappyPathTest.Run(TaskCommand<object, int>.Create(c => AddOneTask(1, null)), 0, 2);
        }

        [TestMethod]
        public void FromTask_TestAbort()
        {
            AbortTest.Run(TaskCommand<object, int>.Create(c => AddOneTask(1, c)), 0, 2);
            AbortTest.Run(TaskCommand<object, int>.Create(c => AddOneTask(1, c)), 0, 2);
        }

        [TestMethod]
        public void FromTask_TestFail()
        {
            FailTest.Run<InvalidOperationException>(TaskCommand<object, int>.Create(c => FailTask(true)), null);
            FailTest.Run<ArgumentException>(TaskCommand<object, int>.Create(c => FailTask(false)), null);
        }

        private static Task<int> AddOneTask(int input, CancellationToken? cancellationToken)
        {
            if (cancellationToken == null)
            {
                return new Task<int>(() =>
                {
                    Thread.Sleep(5);
                    return input + 1;
                });
            }

            return new Task<int>(() =>
            {
                cancellationToken.Value.ThrowIfCancellationRequested();
                Thread.Sleep(5);
                cancellationToken.Value.ThrowIfCancellationRequested();
                return input + 1;
            },
            cancellationToken.Value);
        }

        private static Task<int> FailTask(bool failImmediately)
        {
            return new Task<int>(() =>
            {
                if (failImmediately)
                {
                    throw new InvalidOperationException();
                }

                Thread.Sleep(5);
                throw new ArgumentException();
            });
        }

        private class DoNothingCommand : TaskCommand<int?, int>
        {
            internal enum Behavior
            {
                Succeed,
                SucceedSynchronously,
                FailUpFront,
                FailInTask,
                Abort
            }

            internal DoNothingCommand() : this(Behavior.Succeed)
            {
            }

            internal DoNothingCommand(Behavior behavior)
            {
                _behavior = behavior;
            }

            protected override Task<int> CreateTask(int? runtimeArg, CancellationToken cancellationToken)
            {
                switch (_behavior)
                {
                    case Behavior.FailUpFront:
                        throw new NotSupportedException();
                    case Behavior.SucceedSynchronously:
                        return Task.FromResult(runtimeArg ?? 0);
                    default:
                        return Task.Run(() =>
                        {
                            switch (_behavior)
                            {
                                case Behavior.FailInTask:
                                    throw new InvalidOperationException();
                                case Behavior.Abort when _abortEvent.WaitOne():
                                    throw new CommandAbortedException();
                                default:
                                    return runtimeArg ?? 0;
                            }
                        });
                }
            }

            protected override void AbortImpl()
            {
                _abortEvent.Set();
            }

            private readonly Behavior _behavior;
            private readonly ManualResetEvent _abortEvent = new ManualResetEvent(false);
        }
    }
}
