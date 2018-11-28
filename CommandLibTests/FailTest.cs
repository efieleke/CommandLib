using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal static class FailTest
    {
        // This call takes ownership of 'cmd' (it disposes of it)
        internal static void Run<T>(Command cmd, object runtimeArg) where T : Exception
        {
            string tempFile = System.IO.Path.GetTempFileName();

            try
            {
                Command.Monitors = new LinkedList<ICommandMonitor>();
                Command.Monitors.AddLast(new CommandTracer());
                Command.Monitors.AddLast(new CommandLogger(tempFile));
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Failed, null);
                cmd.AsyncExecute(listener, runtimeArg);
                cmd.Wait();
                listener.Check();

                try
                {
                    cmd.SyncExecute(runtimeArg);
                    Assert.Fail("Command completed successfully when it was expected to fail");
                }
                catch (CommandAbortedException exc)
                {
                    Assert.Fail(exc.ToString());
                }
                catch (T exc)
                {
                    string cmdContext = Command.GetAttachedErrorInfo(exc);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cmdContext));
                }
                catch (Exception exc)
                {
                    Assert.Fail("Caught unexpected type of exception: " + exc);
                }

                bool succeeded = false;
                Exception failure = null;
                bool aborted = false;
                bool failed = false;

                cmd.AsyncExecute(
                    r => succeeded = true,
                    () => aborted = true,
                    e => { failed = true; failure = e; },
                    runtimeArg);

                cmd.Wait();
                Assert.IsFalse(succeeded);
                Assert.IsFalse(aborted);
                Assert.IsTrue(failed);
                Assert.IsTrue(failure is T);

                using (Task<object> task = cmd.RunAsTask<object>(runtimeArg))
	            {
		            try
		            {
			            task.Wait();
		            }
		            catch (AggregateException e)
		            {
						Assert.IsNotNull(e.InnerException);

						if (e.InnerException is T)
			            {
				            string cmdContext = Command.GetAttachedErrorInfo(e.InnerException);
				            Assert.IsFalse(string.IsNullOrWhiteSpace(cmdContext));
			            }
						else
						{
							Assert.Fail(e.InnerException.ToString());
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
    }
}
