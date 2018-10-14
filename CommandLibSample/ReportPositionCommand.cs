using System;
using Sophos.Commands;

namespace CommandLibSample
{
	internal class ReportPositionCommand : SyncCommand
    {
        internal ReportPositionCommand(RobotArm robotArm) : base(null)
        {
            _robotArm = robotArm;
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
	        _robotArm.GetPosition(out int x, out int y, out int z);
            Console.Out.WriteLine($"Robot arm is at position {x},{y},{z}");
            return null;
        }

        private readonly RobotArm _robotArm;
    }
}
