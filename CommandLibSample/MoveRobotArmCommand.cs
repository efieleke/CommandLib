using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotArmCommand : CommandLib.ParallelCommands
    {
        internal MoveRobotArmCommand(RobotArm robotArm, int x, int y, int z)
            : base(true, null)
        {
            base.Add(new MoveRobotArmOnAxisCommand(robotArm, x, RobotArm.Axis.X, null));
            base.Add(new MoveRobotArmOnAxisCommand(robotArm, y, RobotArm.Axis.Y, null));
            base.Add(new MoveRobotArmOnAxisCommand(robotArm, z, RobotArm.Axis.Z, null));
        }
    }
}
