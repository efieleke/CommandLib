using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotArmCommand : CommandLib.ParallelCommands, CommandLib.RetryableCommand.IRetryCallback
    {
        internal MoveRobotArmCommand(RobotArm robotArm, int x, int y, int z)
            : base(true, null)
        {
            base.Add(
                new CommandLib.RetryableCommand(
                    new MoveRobotArmOnAxisCommand(robotArm, x, RobotArm.Axis.X, null),
                    this)
            );

            base.Add(
                new CommandLib.RetryableCommand(
                    new MoveRobotArmOnAxisCommand(robotArm, y, RobotArm.Axis.Y, null),
                    this)
            );

            base.Add(
                new CommandLib.RetryableCommand(
                    new MoveRobotArmOnAxisCommand(robotArm, z, RobotArm.Axis.Z, null),
                    this)
            );
        }

        public bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime)
        {
            Console.Out.WriteLine(reason.Message);

            if (reason is RobotArm.OverheatedException)
            {
                waitTime = TimeSpan.FromSeconds(5);
                Console.WriteLine("Will retry moving that axis after waiting 5 seconds...");
                return true;
            }

            waitTime = TimeSpan.FromTicks(0);
            return false;
        }
    }
}
