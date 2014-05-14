using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibTests
{
    internal class AddCommand : CommandLib.SyncCommand
    {
        internal AddCommand(int amount) : this(amount, null)
        {
        }

        internal AddCommand(int amount, CommandLib.Command owner) : base(owner)
        {
            this.amount = amount;
        }

        protected sealed override object SyncExeImpl(object runtimeArg)
        {
            return (int)runtimeArg + amount;
        }

        private int amount;
    }
}
