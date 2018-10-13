using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal static class AbortTest
    {
        internal static void Run(Command cmd, Object runtimeArg, int maxDelayTime)
        {
            String tempFile = System.IO.Path.GetTempFileName();

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

                System.Threading.Thread thread = new System.Threading.Thread(() =>
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
                        String msg = "Expected aborted exception, but instead got this exception: " + exc.ToString();
                        Assert.Fail(msg);
                    }
                });

                thread.Start();
                System.Threading.Thread.Sleep(20); // give time for the command to start executing
                cmd.AbortAndWait();
                thread.Join();
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
