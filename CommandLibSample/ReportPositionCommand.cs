using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class ReportPositionCommand : CommandLib.SyncCommand
    {
        internal ReportPositionCommand(RobotArm robotArm) : base(null)
        {
            this.robotArm = robotArm;
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            int x, y;
            robotArm.GetPosition(out x, out y);
            Console.Out.WriteLine(String.Format("Robot arm is at position {0},{1}", x, y));
            return null;
        }

        private RobotArm robotArm;
    }
}
