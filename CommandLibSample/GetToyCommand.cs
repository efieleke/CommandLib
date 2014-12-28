using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibSample
{
    class GetToyCommand : CommandLib.SyncCommand
    {
        internal GetToyCommand(RobotArm robotArm) : base(null)
        {
            this.robotArm = robotArm;

            // Notice that 'this' is passed as the owning command to each of these child commands.
            // If we didn't do that, abort requests would not be honored by these children (and we'd
            // also have a resource leak because we never dispose them).
            moveToToyCmd = new MoveAndReportPositionCommand(robotArm, 45, 55, -60, this);
            moveToChuteCmd = new MoveAndReportPositionCommand(robotArm, 100, 100, 0, this);
            moveToHomeCmd = new MoveAndReportPositionCommand(robotArm, 0, 0, 0, this);
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            Console.WriteLine("Attempting to grab a toy with the robot arm...");
            Console.Out.WriteLine("Opening clamp.");
            robotArm.OpenClamp();

            // Run the command that will move the robot arm to 45,55,60 (where we hope it will grab a toy)
            // and periodically report at the same time
            moveToToyCmd.SyncExecute();

            Console.Out.WriteLine("Closing clamp on toy.");

            if (robotArm.CloseClamp())
            {
                Console.Out.WriteLine("Got a toy! Will now move to the chute and drop it.");
                moveToChuteCmd.SyncExecute();

                // Drop the toy
                Console.Out.WriteLine("Opening clamp.");
                robotArm.OpenClamp();
                Console.Out.WriteLine("Dropped the toy down the chute!");
                Console.Out.WriteLine("Closing clamp.");
                robotArm.CloseClamp();
            }
            else
            {
                Console.Out.WriteLine("Too bad. Failed to grab a toy.");
            }

            Console.Out.WriteLine("Now returning to the home position.");
            moveToHomeCmd.SyncExecute();
            return null;
        }

        private RobotArm robotArm;
        private MoveAndReportPositionCommand moveToToyCmd;
        private MoveAndReportPositionCommand moveToChuteCmd;
        private MoveAndReportPositionCommand moveToHomeCmd;
    }
}
