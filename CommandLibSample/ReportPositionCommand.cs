using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class ReportPositionCommand : CommandLib.SyncCommand
    {
        internal ReportPositionCommand(Robot robot, CommandLib.Command owner) : base(owner)
        {
            this.robot = robot;
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            int x, y;
            robot.GetPosition(out x, out y);
            Console.Out.WriteLine(String.Format("{0} is at position {1},{2}", robot.Name, x, y));
            return null;
        }

        private Robot robot;
    }
}
