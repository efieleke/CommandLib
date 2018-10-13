using System;
using Sophos.Commands;

namespace CommandLibSample
{
    class ReportPositionCommand : SyncCommand
    {
        internal ReportPositionCommand(RobotArm robotArm) : base(null)
        {
            this.robotArm = robotArm;
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            int x, y, z;
            robotArm.GetPosition(out x, out y, out z);
            Console.Out.WriteLine(String.Format("Robot arm is at position {0},{1},{2}", x, y, z));
            return null;
        }

        private RobotArm robotArm;
    }
}
