using System;
using Sophos.Commands;

namespace CommandLibTests
{
    internal class FailingCommand : SyncCommand
    {
        [Serializable]
        internal class FailException : Exception
        {
        }

        internal FailingCommand() : this(null)
        {
        }

        internal FailingCommand (Command owner) : base(owner)
        {
        }

        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            throw new FailException();
        }
    }
}
