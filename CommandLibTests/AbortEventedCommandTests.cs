using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                using (CommandLib.AbortEventedCommand abortEventedPauseCmd = new CommandLib.AbortEventedCommand(new NeverEndingAsyncCommand(), abortEvent))
                {
                    try
                    {
                        abortEventedPauseCmd.SyncExecute();
                        Assert.Fail();
                    }
                    catch (CommandLib.CommandAbortedException)
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
                        catch (CommandLib.CommandAbortedException)
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
                using (CommandLib.AbortEventedCommand abortEventedPauseCmd = new CommandLib.AbortEventedCommand(new NeverEndingAsyncCommand(), abortEvent))
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
                CommandLib.PauseCommand pauseCmd = new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(10));
                using (CommandLib.AbortEventedCommand abortEventedPauseCmd = new CommandLib.AbortEventedCommand(pauseCmd, abortEvent))
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
                CommandLib.AbortEventedCommand abortEventedCmd = new CommandLib.AbortEventedCommand(new FailingCommand(), abortEvent);
                FailTest.Run<FailingCommand.FailException>(abortEventedCmd, null);
                abortEventedCmd.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times"), TestMethod]
        public void AbortEventedCommand_TestMustBeTopLevel()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                CommandLib.PauseCommand pauseCmd = new CommandLib.PauseCommand(TimeSpan.FromMilliseconds(10));

                using (CommandLib.AbortEventedCommand abortEventedPauseCmd = new CommandLib.AbortEventedCommand(pauseCmd, abortEvent))
                {
                    using (CommandLib.SequentialCommands seqCmd = new CommandLib.SequentialCommands())
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

                try
                {
                    pauseCmd.Dispose();
                    Assert.Fail("Double dispose allowed");
                }
                catch(ObjectDisposedException)
                {
                }
            }
        }
    }
}
