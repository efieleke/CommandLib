﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    internal static class FailTest
    {
        internal static void Run<T>(Command cmd, Object runtimeArg) where T : Exception
        {
            String tempFile = System.IO.Path.GetTempFileName();

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
                    String cmdContext = Command.GetAttachedErrorInfo(exc);
                    Assert.IsFalse(String.IsNullOrWhiteSpace(cmdContext));
                }
                catch (Exception exc)
                {
                    Assert.Fail("Caught unexpected type of exception: " + exc.ToString());
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
