using Sophos.Commands;

namespace CommandLibTests
{
    internal class AddCommand : SyncCommand
    {
        internal AddCommand(int amount) : this(amount, null)
        {
        }

        internal AddCommand(int amount, Command owner) : base(owner)
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
