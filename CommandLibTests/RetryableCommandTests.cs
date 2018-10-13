using System;
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
                this.maxRetries = maxRetries;
                this.waitTime = waitTime;
            }

            public bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime)
            {
                waitTime = this.waitTime;
                return failNumber <= maxRetries;
            }

            private int maxRetries;
            private TimeSpan waitTime;
        }

        [TestMethod]
        public void RetryableCommand_TestAbort()
        {
            using (RetryableCommand retryableCmd = new RetryableCommand(
                new FailingCommand(),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))))
            {
                AbortTest.Run(retryableCmd, null, 10);
            }

            using (RetryableCommand retryableCmd = new RetryableCommand(
                new PauseCommand(TimeSpan.FromDays(1)),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))))
            {
                AbortTest.Run(retryableCmd, null, 10);
            }
        }

        [TestMethod]
        public void RetryableCommand_TestHappyPath()
        {
            using (RetryableCommand retryableCmd = new RetryableCommand(
                new AddCommand(6),
                new RetryCallback(int.MaxValue, TimeSpan.FromMilliseconds(1))))
            {
                HappyPathTest.Run(retryableCmd, 2, 8);
            }
        }

        [TestMethod]
        public void RetryableCommand_TestFail()
        {
            using (RetryableCommand retryableCmd = new RetryableCommand(
                new FailingCommand(),
                new RetryCallback(5, TimeSpan.FromMilliseconds(1))))
            {
                FailTest.Run<FailingCommand.FailException>(retryableCmd, null);
            }
        }
    }
}
