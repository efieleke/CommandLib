using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    class CmdListener : ICommandListener
    {
        internal enum CallbackType
        {
            Succeeded,
            Aborted,
            Failed,
            None
        }

        internal CmdListener(CallbackType expectedCallback, object expectedResult, Comparison<object> compare = null)
        {
            _compare = compare;
            Reset(expectedCallback, expectedResult);
        }

        internal void Reset(CallbackType expectedCallback, object expectedResult)
        {
            System.Diagnostics.Debug.Assert(expectedCallback != CallbackType.None);
            _expectedCallback = expectedCallback;
            _expectedResult = expectedResult;
            _actualCallback = CallbackType.None;
            _actualResult = null;
            _error = null;
        }

        public void CommandSucceeded(object result)
        {
            _actualResult = result;
            _actualCallback = CallbackType.Succeeded;
        }

        public void CommandAborted()
        {
            _actualCallback = CallbackType.Aborted;
        }

        public void CommandFailed(Exception exc)
        {
            _actualCallback = CallbackType.Failed;
            _error = exc;
        }

        internal void Check()
        {
            if (_actualCallback == CallbackType.Failed)
            {
                Assert.AreEqual(_expectedCallback, _actualCallback, _error.ToString());
            }
            else
            {
                Assert.AreEqual(_expectedCallback, _actualCallback);
            }

            if (_actualCallback == CallbackType.Succeeded)
            {
                if (_compare == null)
                {
                    Assert.AreEqual(_expectedResult, _actualResult);
                }
                else if (_compare(_expectedResult, _actualResult) != 0)
                {
                    Assert.Fail($"expectedResult != result ({_expectedResult} != {_actualResult}");
                }
            }
        }

        private CallbackType _expectedCallback;
        private object _expectedResult;
        private CallbackType _actualCallback = CallbackType.None;
        private object _actualResult;
        private Exception _error;
        private readonly Comparison<object> _compare;
    }
}
