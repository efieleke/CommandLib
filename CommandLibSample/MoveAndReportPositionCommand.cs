using System;
using Sophos.Commands;

namespace CommandLibSample
{
	internal class MoveAndReportPositionCommand : SyncCommand
    {
        internal MoveAndReportPositionCommand(RobotArm robotArm, int x, int y, int z, Command owner) : base(owner)
        {
            var moveCmd = new MoveRobotArmCommand(robotArm, x, y, z);

            // Create a command that will periodically report robot arm position until it reaches the destination (x,y,z)
            var reportPositionCmd = new PeriodicCommand(
                new ReportPositionCommand(robotArm), // the command to execute
                repeatCount: long.MaxValue, // no fixed upper limit on repetitions
                interval: TimeSpan.FromMilliseconds(500), // execute the command twice a second
                intervalType: PeriodicCommand.IntervalType.PauseBefore, // wait half a second before executing the command the first time
                intervalIsInclusive: true, // the half second to wait is inclusive of the time it actually takes to report the position
                stopEvent: moveCmd.DoneEvent); // stop when this event is signaled (in other words, when the arm reaches 0,0)

            // Create the command that will move the robot arm and periodically report at the same time
            var moveAndReportCmd = new ParallelCommands(abortUponFailure:true);
            moveAndReportCmd.Add(moveCmd);
            moveAndReportCmd.Add(reportPositionCmd);

            // Create a command that will first report the starting position, then perform the simultaneous move
            // and position reporting, then report the final position.
            //
            // Notice that 'this' is passed as the owning command to this child command.
            // If we didn't do that, abort requests would not be honored (and we'd have a
            // resource leak).
            _framedMoveAndReportCmd = new SequentialCommands(this);
            _framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));
            _framedMoveAndReportCmd.Add(moveAndReportCmd);
            _framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));
        }

        protected override object SyncExeImpl(object runtimeArg)
        {
            _framedMoveAndReportCmd.SyncExecute();
            return null;
        }

        private readonly SequentialCommands _framedMoveAndReportCmd;
    }
}
