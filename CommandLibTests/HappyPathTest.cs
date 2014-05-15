using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    internal static class HappyPathTest
    {
        internal static void Run(CommandLib.Command cmd, Object runtimeArg, Object expectedResult, Comparison<Object> compare = null)
        {
            Assert.IsTrue(CommandLib.Command.Monitor != null);

            try
            {
                CommandLib.Command.Monitor = null;
                Assert.Fail("Command.Monitor was successfully set to null");
            }
            catch (ArgumentNullException)
            {
            }

            CommandLib.Command.Monitor = new TestCommandMonitor();
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
    }
}
