using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal static class AbortTest
    {
        // This call takes ownership of 'cmd' (it disposes of it)
        internal static void Run(Command cmd, object runtimeArg, int maxDelayTime)
        {
            string tempFile = System.IO.Path.GetTempFileName();

            try
            {
                Command.Monitors = new LinkedList<ICommandMonitor>();
                Command.Monitors.AddLast(new CommandLogger(tempFile));
                Command.Monitors.AddLast(new CommandTracer());
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Aborted, null);
                cmd.AsyncExecute(listener, runtimeArg);
                cmd.AbortAndWait();
                listener.Check();

                listener.Reset(CmdListener.CallbackType.Aborted, null);
                cmd.AsyncExecute(listener, runtimeArg);
                System.Threading.Thread.Sleep(maxDelayTime);
                cmd.AbortAndWait();
                listener.Check();

                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        cmd.SyncExecute(runtimeArg);
                        Assert.Fail("Command succeeded when it was expected to be aborted");
                    }
                    catch (CommandAbortedException exc)
                    {
                        Assert.IsTrue(Command.GetAttachedErrorInfo(exc) == null);
                    }
                    catch (Exception exc)
                    {
                        string msg = "Expected aborted exception, but instead got this exception: " + exc;
                        Assert.Fail(msg);
                    }
                });

                thread.Start();
                System.Threading.Thread.Sleep(maxDelayTime); // give time for the command to start executing
                cmd.AbortAndWait();
                thread.Join();

                bool succeeded = false;
                bool aborted = false;
                bool failed = false;
                cmd.AsyncExecute(r => succeeded = true, () => aborted = true, e => failed = true, runtimeArg);
                System.Threading.Thread.Sleep(maxDelayTime); // give time for the command to start executing
                cmd.AbortAndWait();
                Assert.IsFalse(succeeded);
                Assert.IsTrue(aborted);
                Assert.IsFalse(failed);

                cmd = new AbsolutelyAsyncCommand(cmd);

                using (Task<object> task = cmd.RunAsTask<object>(runtimeArg))
	            {
		            try
		            {
						System.Threading.Thread.Sleep(maxDelayTime); // give time for the command to start executing

		                try
		                {
                            cmd.Abort();
		                }
		                catch (ObjectDisposedException)
		                {
		                    // AsTask took ownership of cmd, so we cannot assume it has not already been disposed.
		                    // If we get an object disposed exception, we can infer that the task was already complete.
		                }

		                task.Wait();
			            Assert.Fail("Command succeeded when it was expected to be aborted");
					}
					catch (AggregateException e)
		            {
			            Exception inner = e.InnerException;
						Assert.IsNotNull(inner);

			            if (inner is TaskCanceledException)
			            {
				            Assert.IsTrue(Command.GetAttachedErrorInfo(inner) == null);
						}
			            else
			            {
				            string msg = "Expected aborted exception, but instead got this exception: " + inner;
				            Assert.Fail(msg);
						}
					}
	            }
			}
			finally
            {
                foreach (ICommandMonitor monitor in Command.Monitors)
                {
                    monitor.Dispose();
                }

                Command.Monitors = null;
                System.IO.File.Delete(tempFile);
            }
        }

        private class AbsolutelyAsyncCommand : AsyncCommand
        {
            internal AbsolutelyAsyncCommand(Command command) : base(null)
            {
                _cmd = command;
                TakeOwnership(_cmd);
            }

            protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
            {
                Task.Run(() =>
                {
                    try
                    {
                        object result = _cmd.SyncExecute(runtimeArg);
                        listener.CommandSucceeded(result);
                    }
                    catch (CommandAbortedException)
                    {
                        listener.CommandAborted();
                    }
                    catch (Exception e)
                    {
                        listener.CommandFailed(e);
                    }
                });
            }

            private readonly Command _cmd;
        }
    }
}
