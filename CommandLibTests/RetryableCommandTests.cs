﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class RetryableCommandTests
    {
        private class RetryCallback : RetryableCommand.IRetryCallback
        {
            internal RetryCallback(int maxRetries, TimeSpan waitTime)
            {
                _maxRetries = maxRetries;
                _waitTime = waitTime;
            }

            public bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime)
            {
                waitTime = _waitTime;
                return failNumber <= _maxRetries;
            }

            private readonly int _maxRetries;
            private readonly TimeSpan _waitTime;
        }

        [TestMethod]
        public void RetryableCommand_TestAbort()
        {
            AbortTest.Run(new RetryableCommand(
                new FailingCommand(),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))),
                null,
                10);

            AbortTest.Run(new RetryableCommand(
                new PauseCommand(TimeSpan.FromDays(1)),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))),
                null,
                10);

            var retryCallback = new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1));

            AbortTest.Run(new RetryableCommand(
                new FailingCommand(),
                (int failNumber, Exception reason, out TimeSpan waitTime) => retryCallback.OnCommandFailed(failNumber, reason, out waitTime)),
                null,
                10);
        }

        [TestMethod]
        public void RetryableCommand_TestHappyPath()
        {
            HappyPathTest.Run(new RetryableCommand(
                new AddCommand(6),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))),
                2,
                8);

            var retryCallback = new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1));

            HappyPathTest.Run(new RetryableCommand(
                new AddCommand(6),
                (int failNumber, Exception reason, out TimeSpan waitTime) => retryCallback.OnCommandFailed(failNumber, reason, out waitTime)),
                2,
                8);
        }

        [TestMethod]
        public void RetryableCommand_TestFail()
        {
            FailTest.Run<FailingCommand.FailException>(new RetryableCommand(
                new FailingCommand(), new RetryCallback(5, TimeSpan.FromMilliseconds(1))), null);

            var retryCallback = new RetryCallback(5, TimeSpan.FromMilliseconds(1));

            FailTest.Run<FailingCommand.FailException>(new RetryableCommand(
                new FailingCommand(),
                (int failNumber, Exception reason, out TimeSpan waitTime) => retryCallback.OnCommandFailed(failNumber, reason, out waitTime)),
                null);
        }
    }
}
