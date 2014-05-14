using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotCommand : CommandLib.ParallelCommands
    {
        internal MoveRobotCommand(Robot robot, int x, int y, CommandLib.Command owner) : base(true, owner)
        {
            base.Add(new MoveRobotOnAxisCommand(robot, x, true, null));
            base.Add(new MoveRobotOnAxisCommand(robot, y, false, null));
        }
    }
}
