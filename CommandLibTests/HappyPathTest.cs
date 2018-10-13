using System;
using System.Collections.Generic;
using Sophos.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal static class HappyPathTest
    {
        internal static void Run(Command cmd, Object runtimeArg, Object expectedResult, Comparison<Object> compare = null)
        {
            Assert.IsTrue(Command.Monitors == null);
            String tempFile = System.IO.Path.GetTempFileName();

            try
            {
                Command.Monitors = new LinkedList<ICommandMonitor>();
                Command.Monitors.AddLast(new CommandTracer());
                Command.Monitors.AddLast(new CommandLogger(tempFile));
                CmdListener listener = new CmdListener(CmdListener.CallbackType.Succeeded, expectedResult, compare);
                cmd.Abort(); // should be a no-op
                cmd.AsyncExecute(listener, runtimeArg);
                cmd.Wait();
                listener.Check();
                cmd.Abort(); // should be a no-op
                Object result;

                // Only doing this hooey-booey for code coverage reasons
                if (runtimeArg == null)
                {
                    result = cmd.SyncExecute<Object>();
                }
                else
                {
                    result = cmd.SyncExecute<Object>(runtimeArg);
                }

                if (compare == null)
                {
                    Assert.AreEqual(expectedResult, result);
                }
                else if (compare(expectedResult, result) != 0)
                {
                    Assert.Fail(String.Format("expectedResult != result ({0} != {1}", expectedResult, result));
                }

                cmd.Wait(); // should be a no-op
                cmd.AbortAndWait(); // should be a no-op
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
