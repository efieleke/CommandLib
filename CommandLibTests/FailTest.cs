using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal static class FailTest
    {
        internal static void Run<T>(CommandLib.Command cmd, Object runtimeArg) where T : Exception
        {
            String tempFile = System.IO.Path.GetTempFileName();

            try
            {
                CommandLib.Command.Monitors = new LinkedList<CommandLib.ICommandMonitor>();
                CommandLib.Command.Monitors.AddLast(new CommandLib.CommandTracer());
                CommandLib.Command.Monitors.AddLast(new CommandLib.CommandLogger(tempFile));
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Failed, null);
                cmd.AsyncExecute(listener, runtimeArg);
                cmd.Wait();
                listener.Check();

                try
                {
                    cmd.SyncExecute(runtimeArg);
                    Assert.Fail("Command completed successfully when it was expected to fail");
                }
                catch (CommandLib.CommandAbortedException exc)
                {
                    Assert.Fail(exc.ToString());
                }
                catch (T exc)
                {
                    String cmdContext = CommandLib.Command.GetAttachedErrorInfo(exc);
                    Assert.IsFalse(String.IsNullOrWhiteSpace(cmdContext));
                }
                catch (Exception exc)
                {
                    Assert.Fail("Caught unexpected type of exception: " + exc.ToString());
                }
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
