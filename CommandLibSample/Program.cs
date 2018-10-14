using System;
using System.Collections.Generic;
using Sophos.Commands;

// This application moves a robot arm to 0,0 and tries to grab the toy at that location with its clamp.
// If it succeeds, it deposits the toy down the chute. Otherwise, it just returns to its home position.
//
// It demonstrates how to author an AsyncCommand-derived class, and makes use of ParallelCommands,
// SequentialCommands, PeriodicCommand, TimeLimitedCommand and RetryableCommand.
namespace CommandLibSample
{
	internal class Program
    {
        static void Main()
        {
            // Output all the command activity to a file in the temp directory. This is a simple text file, and it
            // can be viewed using CommandLogViewer.
            string tempFile = System.IO.Path.GetTempFileName();
            Command.Monitors = new LinkedList<ICommandMonitor>();
            Command.Monitors.AddLast(new CommandTracer());
            Command.Monitors.AddLast(new CommandLogger(tempFile));

            RobotArm robotArm = new RobotArm();

            using (GetToyCommand getToyCmd = new GetToyCommand(robotArm))
            {
                // Execute our top-level command. Every command created by this app is ultimately owned by this command.
                try
                {
                    getToyCmd.SyncExecute();
                    Console.WriteLine("Operation complete.");
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine(err.Message);
                }
            }
            
            foreach(ICommandMonitor monitor in Command.Monitors)
            {
                monitor.Dispose();
            }

            Console.Out.Write($"Delete generated log file ({tempFile} (y/n)? ");
            ConsoleKeyInfo keyInfo = Console.ReadKey(false);
            Console.WriteLine("");

            if (keyInfo.KeyChar != 'n')
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }
}
