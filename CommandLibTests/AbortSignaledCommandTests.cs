﻿using System;
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

        [TestMethod]
        public void AbortSignaledCommand_TestLinkToOtherCommand()
        {
            using (var pauseCmd = new PauseCommand(TimeSpan.FromDays(1)))
            {
                using (var abortSignaledCmd = new AbortSignaledCommand(new PauseCommand(TimeSpan.FromDays(1)), pauseCmd))
                {
                    Assert.IsTrue(ReferenceEquals(abortSignaledCmd.CommandToWatch, pauseCmd));
                    bool aborted = false;
                    abortSignaledCmd.AsyncExecute(o => { }, () => { aborted = true; }, e => throw e);
                    pauseCmd.AsyncExecute(o => { }, () => { }, e => throw e);
                    System.Threading.Thread.Sleep(100);
                    pauseCmd.AbortAndWait();
                    abortSignaledCmd.Wait();
                    Assert.IsTrue(aborted);
                }
            }
        }

        [TestMethod]
        public void AbortSignaledCommand_TestAsChild()
        {
            using (System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false))
            {
                var pauseCmd = new PauseCommand(TimeSpan.FromDays(1));

                using (AbortSignaledCommand abortSignaledPauseCmd = new AbortSignaledCommand(pauseCmd, abortEvent))
                {
                    Assert.IsNull(abortSignaledPauseCmd.CommandToWatch);

                    using (SequentialCommands seqCmd = new SequentialCommands())
                    {
                        bool aborted = false;
                        seqCmd.Add(abortSignaledPauseCmd);

                        seqCmd.AsyncExecute(o => { }, () => { aborted = true; }, e => throw e);
                        System.Threading.Thread.Sleep(100);
                        abortEvent.Set();
                        seqCmd.Wait();
                        Assert.IsTrue(aborted);
                    }
                }

                // Double-dispose should be allowed.
                pauseCmd.Dispose();
            }
        }
    }
}
