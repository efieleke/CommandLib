using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal static class AbortTest
    {
        internal static void Run(CommandLib.Command cmd, Object runtimeArg, int maxDelayTime)
        {
            String tempFile = System.IO.Path.GetTempFileName();

            try
            {
                CommandLib.Command.Monitors = new LinkedList<CommandLib.ICommandMonitor>();
                CommandLib.Command.Monitors.AddLast(new CommandLib.CommandLogger(tempFile));
                CommandLib.Command.Monitors.AddLast(new CommandLib.CommandTracer());
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
                    catch (CommandLib.CommandAbortedException exc)
                    {
                        Assert.IsTrue(CommandLib.Command.GetAttachedErrorInfo(exc) == null);
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
                foreach (CommandLib.ICommandMonitor monitor in CommandLib.Command.Monitors)
                {
                    monitor.Dispose();
                }

                CommandLib.Command.Monitors = null;
                System.IO.File.Delete(tempFile);
            }
        }
    }
}
