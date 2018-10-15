﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sophos.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal static class HappyPathTest
    {
        internal static void Run(Command cmd, object runtimeArg, object expectedResult, Comparison<object> compare = null)
        {
            Assert.IsTrue(Command.Monitors == null);
            string tempFile = System.IO.Path.GetTempFileName();

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

	            // Only doing this hooey-booey for code coverage reasons
                object result = runtimeArg == null ? cmd.SyncExecute<object>() : cmd.SyncExecute<object>(runtimeArg);

                if (compare == null)
                {
                    Assert.AreEqual(expectedResult, result);
                }
                else if (compare(expectedResult, result) != 0)
                {
                    Assert.Fail($"expectedResult != result ({expectedResult} != {result}");
                }

                cmd.Wait(); // should be a no-op
                cmd.AbortAndWait(); // should be a no-op

	            using (Task<object> task = cmd.AsTask<object>(false, runtimeArg))
	            {
		            task.Start();
		            result = task.Result;

		            if (compare == null)
		            {
			            Assert.AreEqual(expectedResult, result);
		            }
		            else if (compare(expectedResult, result) != 0)
		            {
			            Assert.Fail($"expectedResult != result ({expectedResult} != {result}");
		            }
	            }
            }
			finally
            {
	            Assert.IsNotNull(Command.Monitors);

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
