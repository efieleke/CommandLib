using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal static class AbortTest
    {
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

	            using (Task<object> task = cmd.AsTask<object>(true, runtimeArg))
	            {
		            try
		            {
						System.Threading.Thread.Sleep(maxDelayTime); // give time for the command to start executing
			            cmd.Abort(); // there is no way to directly abort the task other than by aborting the underlying command
			            task.Wait();
			            Assert.Fail("Command succeeded when it was expected to be aborted");
					}
					catch (AggregateException e)
		            {
			            Exception inner = e.InnerException;
						Assert.IsNotNull(inner);

			            if (inner is CommandAbortedException)
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

	            bool succeeded = false;
	            bool aborted = false;
	            bool failed = false;
	            cmd.AsyncExecute(r => succeeded = true, () => aborted = true, e => failed = true, runtimeArg);
	            System.Threading.Thread.Sleep(maxDelayTime); // give time for the command to start executing
	            cmd.AbortAndWait();
				Assert.IsFalse(succeeded);
	            Assert.IsTrue(aborted);
				Assert.IsFalse(failed);
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
    }
}
