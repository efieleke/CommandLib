using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class EmitGreetingCommand : CommandLib.SyncCommand
    {
        internal EmitGreetingCommand(Robot robot, CommandLib.Command owner) : base(owner)
        {
            this.robot = robot;
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            Console.Out.WriteLine(robot.Greeting);
            return null;
        }

        private Robot robot;
    }
}
