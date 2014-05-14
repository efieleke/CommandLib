using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class VariableCommandTest
    {
        [TestMethod]
        public void TestHappyPath()
        {
            using (TestAdd2Command test = new TestAdd2Command(1, null))
            {
                HappyPathTest.Run(test, 1, 3);
            }
        }

        [TestMethod]
        public void TestFailPath()
        {
            using (TestFailCommand test = new TestFailCommand(null, null))
            {
                FailTest.Run<FailingCommand.FailException>(test, null);
            }

            try
            {
                new CommandLib.VariableCommand(null);
            }
            catch(ArgumentNullException)
            {
            }
        }

        [TestMethod]
        public void TestAbortPath()
        {
            using (TestNeverEndCommand test = new TestNeverEndCommand(null, null))
            {
                AbortTest.Run(test, null, 10);
            }
        }

        abstract private class TestCommand : CommandLib.Command
        {
            internal TestCommand(Object runtimeArg, CommandLib.Command owner) : base(owner)
            {
                variableCommand = new CommandLib.VariableCommand(this);
                Assert.AreEqual(variableCommand.CommandToRun, null);
            }

            public abstract CommandLib.Command GenerateCommand();

            protected sealed override object SyncExecuteImpl(object runtimeArg)
            {
                SetupCommand();
                return variableCommand.SyncExecute(runtimeArg);
            }

            protected sealed override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
            {
                SetupCommand();
                variableCommand.AsyncExecute(listener, runtimeArg);
            }

            private void SetupCommand()
            {
                CommandLib.Command cmd = GenerateCommand();
                variableCommand.CommandToRun = cmd;
                Assert.AreNotEqual(variableCommand.CommandToRun, null);
                variableCommand.CommandToRun = cmd;
                variableCommand.CommandToRun = GenerateCommand();
            }

            private CommandLib.VariableCommand variableCommand;
        }

        private class TestAdd2Command : TestCommand
        {
            internal TestAdd2Command(Object runtimeArg, CommandLib.Command owner) : base(runtimeArg, owner)
            {
            }

            public sealed override CommandLib.Command GenerateCommand()
            {
                return new AddCommand(2);
            }
        }

        private class TestFailCommand : TestCommand
        {
            internal TestFailCommand(Object runtimeArg, CommandLib.Command owner)
                : base(runtimeArg, owner)
            {
            }

            public sealed override CommandLib.Command GenerateCommand()
            {
                return new FailingCommand();
            }
        }

        private class TestNeverEndCommand : TestCommand
        {
            internal TestNeverEndCommand(Object runtimeArg, CommandLib.Command owner)
                : base(runtimeArg, owner)
            {
            }

            public sealed override CommandLib.Command GenerateCommand()
            {
                return new NeverEndingAsyncCommand();
            }
        }
    }
}
