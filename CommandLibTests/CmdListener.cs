using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    class CmdListener : CommandLib.ICommandListener
    {
        internal enum CallbackType
        {
            Succeeded,
            Aborted,
            Failed,
            None
        }

        internal CmdListener(CallbackType expectedCallback, Object expectedResult, Comparison<Object> compare = null)
        {
            this.compare = compare;
            Reset(expectedCallback, expectedResult);
        }

        internal void Reset(CallbackType expectedCallback, Object expectedResult)
        {
            System.Diagnostics.Debug.Assert(expectedCallback != CallbackType.None);
            this.expectedCallback = expectedCallback;
            this.expectedResult = expectedResult;
            actualCallback = CallbackType.None;
            actualResult = null;
            error = null;
        }

        public void CommandSucceeded(object result)
        {
            actualResult = result;
            actualCallback = CallbackType.Succeeded;
        }

        public void CommandAborted()
        {
            actualCallback = CallbackType.Aborted;
        }

        public void CommandFailed(Exception exc)
        {
            actualCallback = CallbackType.Failed;
            error = exc;
        }

        internal void Check()
        {
            if (actualCallback == CallbackType.Failed)
            {
                Assert.AreEqual(expectedCallback, actualCallback, error.ToString());
            }
            else
            {
                Assert.AreEqual(expectedCallback, actualCallback);
            }

            if (actualCallback == CallbackType.Succeeded)
            {
                if (compare == null)
                {
                    Assert.AreEqual(expectedResult, actualResult);
                }
                else if (compare(expectedResult, actualResult) != 0)
                {
                    Assert.Fail(String.Format("expectedResult != result ({0} != {1}", expectedResult, actualResult));
                }
            }
        }

        private CallbackType expectedCallback;
        private Object expectedResult;
        private CallbackType actualCallback = CallbackType.None;
        private Object actualResult = null;
        private Exception error = null;
        private Comparison<Object> compare;
    }
}
