using System;
using Sophos.Commands;

namespace CommandLibSample
{
	internal class MoveRobotArmCommand : ParallelCommands, RetryableCommand.IRetryCallback
    {
        internal MoveRobotArmCommand(RobotArm robotArm, int x, int y, int z)
            : base(true, null)
        {
            Add(
                new RetryableCommand(
                    new MoveRobotArmOnAxisCommand(robotArm, x, RobotArm.Axis.X, null),
                    this)
            );

            Add(
                new RetryableCommand(
                    new MoveRobotArmOnAxisCommand(robotArm, y, RobotArm.Axis.Y, null),
                    this)
            );

            Add(
                new RetryableCommand(
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
