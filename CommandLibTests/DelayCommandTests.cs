using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class DelayCommandTests
    {
        [TestMethod]
        public void DelayCommand_TestAbort()
        {
            using (var delayCmd = new DelayCommand(TimeSpan.FromDays(1)))
            {
                AbortTest.Run(delayCmd, null, 10);
            }
        }

        [TestMethod]
        public void DelayCommand_TestHappyPath()
        {
            using (var delayCmd = new DelayCommand(TimeSpan.FromMilliseconds(1)))
            {
                HappyPathTest.Run(delayCmd, null, true);
            }

            using (var shortDelay = new DelayCommand(TimeSpan.FromMilliseconds(100)))
            {
                shortDelay.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));

                using (PauseCommand pauseCmd = new PauseCommand(TimeSpan.MaxValue, shortDelay.DoneEvent))
                {
                    HappyPathTest.Run(pauseCmd, null, null);
                }
            }
        }
    }
}
