using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class CommandDispatcherTests
    {
        private int completed;
        private int aborted;
        private int failed;

        internal void dispatcher_CommandFinishedEvent(Object sender, CommandDispatcher.CommandFinishedEventArgs e)
        {
            if (e.Error == null)
            {
                System.Threading.Interlocked.Increment(ref completed);
            }
            else if (e.Error is CommandAbortedException)
            {
                System.Threading.Interlocked.Increment(ref aborted);
            }
            else
            {
                System.Threading.Interlocked.Increment(ref failed);
            }
        }

        [TestMethod]
        public void CommandDispatcher_TestAbort()
        {
            CommandDispatcher dispatcher = new CommandDispatcher(2);
            dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
            aborted = 0;
            completed = 0;
            failed = 0;
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            System.Threading.Thread.Sleep(100);
            dispatcher.AbortAndWait();
            Assert.AreEqual(2, completed);
            Assert.AreEqual(0, failed);
            Assert.AreEqual(2, aborted);
        }

        [TestMethod]
        public void CommandDispatcher_TestHappyPath()
        {
            using (CommandDispatcher dispatcher = new CommandDispatcher(2))
            {
                dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
                aborted = 0;
                completed = 0;
                failed = 0;
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(100)));
                dispatcher.Dispatch(new FailingCommand());
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            }

            Assert.AreEqual(5, completed);
            Assert.AreEqual(1, failed);
            Assert.AreEqual(0, aborted);
        }

        [TestMethod]
        public void CommandDispatcher_TestBadArgs()
        {
            try
            {
                CommandDispatcher dispatcher = new CommandDispatcher(0);
                Assert.Fail("Dispatcher with 0 pool size constructed.");
            }
            catch(ArgumentException)
            {
            }

            using (CommandDispatcher dispatcher = new CommandDispatcher(2))
            {
                PauseCommand pause = new PauseCommand(TimeSpan.FromDays(1));
                
                using (SequentialCommands seq = new SequentialCommands())
                {
                    seq.Add(pause);

                    try
                    {
                        dispatcher.Dispatch(pause);
                        Assert.Fail("Dispatched a child command.");
                    }
                    catch(ArgumentException)
                    {
                    }
                }
            }
        }
    }
}
