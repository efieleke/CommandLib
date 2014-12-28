using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This application moves a robot arm to 0,0 and tries to grab the toy at that location with its clamp.
// If it succeeds, it deposits the toy down the chute. Otherwise, it just returns to its home position.
//
// It demonstrates how to author an AsyncCommand-derived class, and makes use of ParallelCommands,
// SequentialCommands, PeriodicCommand, TimeLimitedCommand and RetryableCommand.
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

            RobotArm robotArm = new RobotArm();
            GetToyCommand getToyCmd = new GetToyCommand(robotArm);

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
}
