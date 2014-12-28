using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This application moves a robot arm to the origin (0,0). It demonstrates how to author an AsyncCommand-derived class,
// and makes use of ParallelCommands, SequentialCommands, PeriodicCommand, TimeLimitedCommand and RetryableCommand.
namespace CommandLibSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Output all the command activity to a file in the temp directory. This is a simple text file, and it
            // can be viewed using CommandLogViewer.
            String tempFile = System.IO.Path.GetTempFileName();
            CommandLib.Command.Monitors = new LinkedList<CommandLib.ICommandMonitor>();
            CommandLib.Command.Monitors.AddLast(new CommandLib.CommandTracer());
            CommandLib.Command.Monitors.AddLast(new CommandLib.CommandLogger(tempFile));

            RobotArm robotArm = new RobotArm(100, 126);

            // Create a command that will concurrently move the robot arm to position 0,0
            MoveRobotArmCommand moveCmd = new MoveRobotArmCommand(robotArm, 0, 0);

            // Create a commands that will periodically report robot arm position until it reaches the destination (0,0)
            CommandLib.PeriodicCommand reportPositionCmd = new CommandLib.PeriodicCommand(
                new ReportPositionCommand(robotArm), // the command to execute
                long.MaxValue, // no fixed upper limit on repetitions
                TimeSpan.FromMilliseconds(500), // execute the command twice a second
                CommandLib.PeriodicCommand.IntervalType.PauseBefore, // wait a second before executing the command the first time
                true, // the second to wait is inclusive of the time it actually takes to report the position
                moveCmd.DoneEvent); // stop when this event is signaled (in other words, when the arm reaches 0,0)

            // Create a command that will move the robot arm and periodically report at the same time
            CommandLib.ParallelCommands moveAndReportCmd = new CommandLib.ParallelCommands(true);
            moveAndReportCmd.Add(moveCmd);
            moveAndReportCmd.Add(reportPositionCmd);

            // Create a command that will first report the starting position, then perform the simulataneous move
            // and position reporting, then report the final position.
            CommandLib.SequentialCommands framedMoveAndReportCmd = new CommandLib.SequentialCommands();
            framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));
            framedMoveAndReportCmd.Add(moveAndReportCmd);
            framedMoveAndReportCmd.Add(new ReportPositionCommand(robotArm));

            // Wrap the above command in a command that throws a TimeoutException if it takes longer than 20 seconds.
            CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(framedMoveAndReportCmd, 10000);

            // Allow retries, because we will time out
            CommandLib.RetryableCommand retryableCmd = new CommandLib.RetryableCommand(timeLimitedCmd, new RetryHandler());

            // Execute our top-level command. Every command created by this app is a ultimately owned by this command.
            try
            {
                retryableCmd.SyncExecute();
                Console.WriteLine("Successfully moved robot arm to the origin");
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.Message);
            }
            
            foreach(CommandLib.ICommandMonitor monitor in CommandLib.Command.Monitors)
            {
                monitor.Dispose();
            }

            Console.Out.Write(String.Format("Delete generated log file ({0} (y/n)? ", tempFile));
            ConsoleKeyInfo keyInfo = Console.ReadKey(false);
            Console.WriteLine("");

            if (keyInfo.KeyChar != 'n')
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }

    class RetryHandler : CommandLib.RetryableCommand.IRetryCallback
    {
        public bool OnCommandFailed(int failNumber, Exception reason, out TimeSpan waitTime)
        {
            waitTime = TimeSpan.FromTicks(0);

            if (reason is TimeoutException)
            {
                Console.Out.Write("Would you like to give the robot arm more time to reach position 0,0 (y/n)? ");
                ConsoleKeyInfo keyInfo = Console.ReadKey(false);
                Console.WriteLine("");
                return keyInfo.KeyChar == 'y';
            }

            return false;
        }
    }
}
