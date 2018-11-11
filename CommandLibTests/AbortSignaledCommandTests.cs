using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class AbortSignaledCommandTests
    {
        [TestMethod]
        public void AbortSignaledCommand_TestAbortedPause()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(true))
            {
                using (AbortSignaledCommand abortSignaledPauseCmd = new AbortSignaledCommand(new NeverEndingAsyncCommand(), abortEvent))
                {
                    try
                    {
                        abortSignaledPauseCmd.SyncExecute();
                        Assert.Fail();
                    }
                    catch (CommandAbortedException)
                    {
                    }
                    catch (Exception)
                    {
                        Assert.Fail();
                    }

                    abortEvent.Reset();

                    System.Threading.Thread thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
							// The thread is joined below, so this is safe.
	                        // ReSharper disable once AccessToDisposedClosure
	                        abortSignaledPauseCmd.SyncExecute();
                            Assert.Fail();
                        }
                        catch (CommandAbortedException)
                        {
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                    });

                    thread.Start();
                    System.Threading.Thread.Sleep(20); // give time for the thread to start
                    abortEvent.Set();
                    abortSignaledPauseCmd.Wait();
                    thread.Join();

                    CmdListener listener = new CmdListener(CmdListener.CallbackType.Aborted, null);
                    abortSignaledPauseCmd.AsyncExecute(listener);
                    abortSignaledPauseCmd.Wait();
                    listener.Check();

                    abortEvent.Reset();
                    listener.Reset(CmdListener.CallbackType.Aborted, null);
                    abortSignaledPauseCmd.AsyncExecute(listener);
                    abortEvent.Set();
                    abortSignaledPauseCmd.Wait();
                    listener.Check();

                    abortEvent.Reset();
                    listener.Reset(CmdListener.CallbackType.Aborted, null);
                    abortSignaledPauseCmd.AsyncExecute(listener);
                    System.Threading.Thread.Sleep(10);
                    abortEvent.Set();
                    abortSignaledPauseCmd.Wait();
                    listener.Check();
                }
            }
        }

        [TestMethod]
        public void AbortSignaledCommand_TestAbort()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                AbortTest.Run(new AbortSignaledCommand(new NeverEndingAsyncCommand(), abortEvent), null, 10);
            }
        }

        [TestMethod]
        public void AbortSignaledCommand_TestHappyPath()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(10));
                HappyPathTest.Run(new AbortSignaledCommand(pauseCmd, abortEvent), null, null);
            }
        }

        [TestMethod]
        public void AbortSignaledCommand_TestFail()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                var abortSignaledCmd = new AbortSignaledCommand(new FailingCommand(), abortEvent);
                FailTest.Run<FailingCommand.FailException>(abortSignaledCmd, null);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times"), TestMethod]
        public void AbortSignaledCommand_TestMustBeTopLevel()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(10));

                using (AbortSignaledCommand abortSignaledPauseCmd = new AbortSignaledCommand(pauseCmd, abortEvent))
                {
                    using (SequentialCommands seqCmd = new SequentialCommands())
                    {
                        try
                        {
                            seqCmd.Add(abortSignaledPauseCmd);
                            Assert.Fail("AbortSignaledCommand was given an owner");
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        try
                        {
                            seqCmd.Add(pauseCmd);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }

                    // Ill-advised, but should not cause anything worse than a diagnostic warning message
                    pauseCmd.Dispose();
                }

                // Double-dispose should be allowed.
                pauseCmd.Dispose();
            }
        }
    }
}
