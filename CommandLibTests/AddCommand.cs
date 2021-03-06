﻿using Sophos.Commands;

namespace CommandLibTests
{
    internal class AddCommand : SyncCommand
    {
        internal AddCommand(int amount) : this(amount, null)
        {
        }

        internal AddCommand(int amount, Command owner) : base(owner)
        {
            _amount = amount;
        }

        protected sealed override object SyncExecuteImpl(object runtimeArg)
        {
            return (int?) runtimeArg + _amount ?? _amount;
        }

        private readonly int _amount;
    }
}
