using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotArmCommand : CommandLib.ParallelCommands
    {
        internal MoveRobotArmCommand(RobotArm robotArm, int x, int y)
            : base(true, null)
        {
            base.Add(new MoveRobotArmOnAxisCommand(robotArm, x, true, null));
            base.Add(new MoveRobotArmOnAxisCommand(robotArm, y, false, null));
        }
    }
}
