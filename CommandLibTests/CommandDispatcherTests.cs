using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class CommandDispatcherTests
    {
	    private int _completed;
        private int _aborted;
        private int _failed;

        internal void dispatcher_CommandFinishedEvent(object sender, CommandDispatcher.CommandFinishedEventArgs e)
        {
	        switch (e.Error)
	        {
		        case null:
			        System.Threading.Interlocked.Increment(ref _completed);
			        break;
		        case CommandAbortedException _:
			        System.Threading.Interlocked.Increment(ref _aborted);
			        break;
		        default:
			        System.Threading.Interlocked.Increment(ref _failed);
			        break;
	        }
        }

        [TestMethod]
        public void CommandDispatcher_TestAbort()
        {
            CommandDispatcher dispatcher = new CommandDispatcher(2);
            dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
            _aborted = 0;
            _completed = 0;
            _failed = 0;
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            dispatcher.Dispatch(new PauseCommand(TimeSpan.FromDays(1)));
            System.Threading.Thread.Sleep(100);
            dispatcher.AbortAndWait();
            Assert.AreEqual(2, _completed);
            Assert.AreEqual(0, _failed);
            Assert.AreEqual(2, _aborted);
        }

        [TestMethod]
        public void CommandDispatcher_TestHappyPath()
        {
            using (CommandDispatcher dispatcher = new CommandDispatcher(2))
            {
                dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
                _aborted = 0;
                _completed = 0;
                _failed = 0;
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(100)));
                dispatcher.Dispatch(new FailingCommand());
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(0)));
            }

            Assert.AreEqual(5, _completed);
            Assert.AreEqual(1, _failed);
            Assert.AreEqual(0, _aborted);
        }

        [TestMethod]
        public void CommandDispatcher_TestBumAsyncCommand()
        {
            using (CommandDispatcher dispatcher = new CommandDispatcher(1))
            {
                dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
                _aborted = 0;
                _completed = 0;
                _failed = 0;
                dispatcher.Dispatch(new PauseCommand(TimeSpan.FromMilliseconds(10)));
                dispatcher.Dispatch(new BumAsyncCommand());
                dispatcher.Dispatch(new BumAsyncCommand());
            }

            Assert.AreEqual(1, _completed);
            Assert.AreEqual(2, _failed);
            Assert.AreEqual(0, _aborted);

            using (CommandDispatcher dispatcher = new CommandDispatcher(1))
            {
                dispatcher.CommandFinishedEvent += dispatcher_CommandFinishedEvent;
                _aborted = 0;
                _completed = 0;
                _failed = 0;

                try
                {
                    dispatcher.Dispatch(new BumAsyncCommand());
                    Assert.Fail("Did not expect to get here");
                }
                catch (NotImplementedException)
                {
                }
            }

            Assert.AreEqual(0, _completed);
            Assert.AreEqual(0, _failed);
            Assert.AreEqual(0, _aborted);
        }

        [TestMethod]
        public void CommandDispatcher_TestBadArgs()
        {
            try
            {
                var _ = new CommandDispatcher(0);
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

        private class BumAsyncCommand : AsyncCommand
        {
            public BumAsyncCommand() : base(null) {}

            protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
            {
                throw new NotImplementedException();
            }
        }
    }
}
