using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibTests
{
    internal class FailingCommand : CommandLib.SyncCommand
    {
        [Serializable]
        internal class FailException : Exception
        {
        }

        internal FailingCommand() : this(null)
        {
        }

        internal FailingCommand (CommandLib.Command owner) : base(owner)
        {
        }

        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            throw new FailException();
        }
    }
}
