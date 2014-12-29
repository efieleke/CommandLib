using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibSample
{
    class MoveAndReportPositionCommand : CommandLib.SyncCommand
    {
        internal MoveAndReportPositionCommand(RobotArm robotArm, int x, int y, int z, CommandLib.Command owner) : base(owner)
        {
            MoveRobotArmCommand moveCmd = new MoveRobotArmCommand(robotArm, x, y, z);

            // Create a commands that will periodically report robot arm position until it reaches the destination (x,y,z)
            CommandLib.PeriodicCommand reportPositionCmd = new CommandLib.PeriodicCommand(
                new ReportPositionCommand(robotArm), // the command to execute
                long.MaxValue, // no fixed upper limit on repetitions
                TimeSpan.FromMilliseconds(500), // execute the command twice a second
                CommandLib.PeriodicCommand.IntervalType.PauseBefore, // wait a second before executing the command the first time
                true, // the second to wait is inclusive of the time it actually takes to report the position
                moveCmd.DoneEvent); // stop when this event is signaled (in other words, when the arm reaches 0,0)

            // Create the command that will move the robot arm and periodically report at the same time
            CommandLib.ParallelCommands moveAndReportCmd = new CommandLib.ParallelCommands(true);
            moveAndReportCmd.Add(moveCmd);
            moveAndReportCmd.Add(reportPositionCmd);

            // Create a command that will first report the starting position, then perform the simultaneous move
            // and position reporting, then report the final position.
            //
            // Notice that 'this' is passed as the owning command to this child command.
            // If we didn't do that, abort requests would not be honored (and we'd have a
            // resource leak).
            // also have a resource leak because we never dispose this object).
            framedMoveAndReportCmd = new CommandLib.SequentialCommands(this);
            framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));
            framedMoveAndReportCmd.Add(moveAndReportCmd);
            framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            framedMoveAndReportCmd.SyncExecute();
            return null;
        }

        private CommandLib.SequentialCommands framedMoveAndReportCmd;
    }
}
