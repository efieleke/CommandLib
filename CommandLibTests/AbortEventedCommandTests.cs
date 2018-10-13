using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class AbortEventedCommandTests
    {
        [TestMethod]
        public void AbortEventedCommand_TestAbortedPause()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(true))
            {
                using (AbortEventedCommand abortEventedPauseCmd = new AbortEventedCommand(new NeverEndingAsyncCommand(), abortEvent))
                {
                    try
                    {
                        abortEventedPauseCmd.SyncExecute();
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
                            abortEventedPauseCmd.SyncExecute();
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
                    abortEventedPauseCmd.Wait();
                    thread.Join();

                    CmdListener listener = new CmdListener(CmdListener.CallbackType.Aborted, null);
                    abortEventedPauseCmd.AsyncExecute(listener);
                    abortEventedPauseCmd.Wait();
                    listener.Check();

                    abortEvent.Reset();
                    listener.Reset(CmdListener.CallbackType.Aborted, null);
                    abortEventedPauseCmd.AsyncExecute(listener);
                    abortEvent.Set();
                    abortEventedPauseCmd.Wait();
                    listener.Check();

                    abortEvent.Reset();
                    listener.Reset(CmdListener.CallbackType.Aborted, null);
                    abortEventedPauseCmd.AsyncExecute(listener);
                    System.Threading.Thread.Sleep(10);
                    abortEvent.Set();
                    abortEventedPauseCmd.Wait();
                    listener.Check();
                }
            }
        }

        [TestMethod]
        public void AbortEventedCommand_TestAbort()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                using (AbortEventedCommand abortEventedPauseCmd = new AbortEventedCommand(new NeverEndingAsyncCommand(), abortEvent))
                {
                    AbortTest.Run(abortEventedPauseCmd, null, 10);
                }
            }
        }

        [TestMethod]
        public void AbortEventedCommand_TestHappyPath()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(10));
                using (AbortEventedCommand abortEventedPauseCmd = new AbortEventedCommand(pauseCmd, abortEvent))
                {
                    HappyPathTest.Run(abortEventedPauseCmd, null, null);
                }
            }
        }

        [TestMethod]
        public void AbortEventedCommand_TestFail()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                AbortEventedCommand abortEventedCmd = new AbortEventedCommand(new FailingCommand(), abortEvent);
                FailTest.Run<FailingCommand.FailException>(abortEventedCmd, null);
                abortEventedCmd.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times"), TestMethod]
        public void AbortEventedCommand_TestMustBeTopLevel()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                PauseCommand pauseCmd = new PauseCommand(TimeSpan.FromMilliseconds(10));

                using (AbortEventedCommand abortEventedPauseCmd = new AbortEventedCommand(pauseCmd, abortEvent))
                {
                    using (SequentialCommands seqCmd = new SequentialCommands())
                    {
                        try
                        {
                            seqCmd.Add(abortEventedPauseCmd);
                            Assert.Fail("AbortEventedCommand was given an owner");
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
