using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
    [TestClass]
    public class BadAbortTests
    {
        [TestMethod]
        public void BadAbort_TestAbortChild()
        {
            using (CommandLib.SequentialCommands seqCmd = new CommandLib.SequentialCommands())
            {
                AddCommand addCmd = new AddCommand(0);
                seqCmd.Add(addCmd);

                try
                {
                    addCmd.Abort();
                    Assert.Fail("AbortEventedCommand was given an owner");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
