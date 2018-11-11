using System;
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
            AbortTest.Run(new DelayCommand(TimeSpan.FromDays(1)), null, 10);
        }

        [TestMethod]
        public void DelayCommand_TestHappyPath()
        {
            HappyPathTest.Run(new DelayCommand(TimeSpan.FromMilliseconds(1)), null, true);

            using (var shortDelay = new DelayCommand(TimeSpan.FromMilliseconds(100)))
            {
                shortDelay.AsyncExecute(new CmdListener(CmdListener.CallbackType.Succeeded, null));
                HappyPathTest.Run(new PauseCommand(TimeSpan.MaxValue, shortDelay.DoneEvent), null, null);
            }
        }
    }
}
