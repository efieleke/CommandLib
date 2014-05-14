using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This application chronicles the story of two lovestruck robots attempting to meet each other at the origin (0,0).
// It demonstrates how to author an AsyncCommand-derived class, and makes use of ParallelCommands, SequentialCommands,
// PeriodicCommand, TimeLimitedCommand and RetryableCommand.
namespace CommandLibSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Robot robotOne = new Robot("George", 100, 126);
            Robot robotTwo = new Robot("Martha", 97, 80);

            // Create a command that will concurrently move both robots to position 0,0
            CommandLib.ParallelCommands moveRobotsCmd = new CommandLib.ParallelCommands(true);
            moveRobotsCmd.Add(new MoveRobotCommand(robotOne, 0, 0, null));
            moveRobotsCmd.Add(new MoveRobotCommand(robotTwo, 0, 0, null));

            // Create commands that will periodically report robot positions until they both reach their destination (0,0)
            CommandLib.PeriodicCommand reportRobotOnePositionCmd = new CommandLib.PeriodicCommand(
                new ReportPositionCommand(robotOne, null), // the command to execute
                long.MaxValue, // no fixed upper limit on repetitions
                TimeSpan.FromSeconds(1), // execute the command every second
                CommandLib.PeriodicCommand.IntervalType.PauseBefore, // wait a second before executing the command the first time
                true, // the second to wait is inclusive of the time it actually takes to report the position
                moveRobotsCmd.DoneEvent); // stop when this event is signaled (in other words, when both robots reach 0,0)

            CommandLib.PeriodicCommand reportRobotTwoPositionCmd = new CommandLib.PeriodicCommand(
                new ReportPositionCommand(robotTwo, null), // the command to execute
                long.MaxValue, // no fixed upper limit on repetitions
                TimeSpan.FromSeconds(1), // execute the command every second
                CommandLib.PeriodicCommand.IntervalType.PauseBefore, // wait a second before executing the command the first time
                true, // the second to wait is inclusive of the time it actually takes to report the position
                moveRobotsCmd.DoneEvent); // stop when this event is signaled (in other words, when both robots reach 0,0)

            // Create a command that will move the robots and periodically report at the same time
            CommandLib.ParallelCommands moveAndReportCmd = new CommandLib.ParallelCommands(true);
            moveAndReportCmd.Add(moveRobotsCmd);
            moveAndReportCmd.Add(reportRobotOnePositionCmd);
            moveAndReportCmd.Add(reportRobotTwoPositionCmd);

            // Create a command that will first report the starting positions, then perform the simulataneous moves
            // and position reporting, then report their final positions, and last of all, greet each other.
            CommandLib.SequentialCommands moveAndGreetCmd = new CommandLib.SequentialCommands();
            moveAndGreetCmd.Add(new ReportPositionCommand(robotOne, null));
            moveAndGreetCmd.Add(new ReportPositionCommand(robotTwo, null));
            moveAndGreetCmd.Add(moveAndReportCmd);
            moveAndGreetCmd.Add(new ReportPositionCommand(robotOne, null));
            moveAndGreetCmd.Add(new ReportPositionCommand(robotTwo, null));
            moveAndGreetCmd.Add(new EmitGreetingCommand(robotOne, null));
            moveAndGreetCmd.Add(new EmitGreetingCommand(robotTwo, null));

            // Wrap the above command in a command that throw a TimeoutException if it takes longer than 15 seconds.
            CommandLib.TimeLimitedCommand timeLimitedCmd = new CommandLib.TimeLimitedCommand(moveAndGreetCmd, 20000);

            // Allow retries, because we will time out, and maybe the end user actually wants to see if there are any
            // sparks between George and Martha.
            CommandLib.RetryableCommand retryableCmd = new CommandLib.RetryableCommand(timeLimitedCmd, new RetryHandler());

            // Execute our top-level command. Every command created by this app is a ultimately owned by this command.
            try
            {
                retryableCmd.SyncExecute();
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.Message);
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
                Console.Out.Write("Would you like to give George and Martha more time to find each other (y/n)? ");
                ConsoleKeyInfo keyInfo = Console.ReadKey(false);
                Console.WriteLine("");
                return keyInfo.KeyChar == 'y';
            }

            return false;
        }
    }
}
