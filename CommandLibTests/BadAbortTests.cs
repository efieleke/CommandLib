using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class BadAbortTests
    {
        [TestMethod]
        public void BadAbort_TestAbortChild()
        {
            using (SequentialCommands seqCmd = new SequentialCommands())
            {
                AddCommand addCmd = new AddCommand(0);
                seqCmd.Add(addCmd);

                try
                {
                    addCmd.Abort();
                    Assert.Fail("Aborted command was given an owner");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
