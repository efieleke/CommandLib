using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sophos.Commands;

// This application prepares a spaghetti and salad dinner.
namespace CommandLibSample
{
	internal class Program
	{
		private static readonly PrepareDinnerCmd MakeDinnerCmd = new PrepareDinnerCmd();

		private static void Main()
		{
			// Trap Ctrl-C in to provide an example of aborting a command (see implementation
			// of ConsoleCtrlCheck below)
			SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

			// Output all the command activity to a file in the temp directory. This is a simple text file,
			// and it can be viewed using CommandLogViewer.
			string tempFile = System.IO.Path.GetTempFileName();
			Command.Monitors = new LinkedList<ICommandMonitor>();
			Command.Monitors.AddLast(new CommandTracer());
			Command.Monitors.AddLast(new CommandLogger(tempFile));

			try
			{
				MakeDinnerCmd.SyncExecute();
				Console.Out.WriteLine("Dinner is ready!");
			}
			catch (CommandAbortedException)
			{
				Console.Out.WriteLine("Dinner preparation aborted. Let's order pizza instead.");
			}

			foreach (ICommandMonitor monitor in Command.Monitors)
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

            MakeDinnerCmd.Dispose();
		}

        // This class is a ParallelCommands-derived type that adds Command objects to its
        // collection. When the PrepareDinnerCommand instance is executed, its contained
        // commands will execute concurrently (those two commands are preparing the spaghetti
        // and preparing the salad; see the last two statements in the constructor).
		private class PrepareDinnerCmd : ParallelCommands
		{
			internal PrepareDinnerCmd() : base(Behavior.AbortUponFailure)
			{
				// Preparing the noodles consists of three operations that must be performed in sequence.
				var prepareNoodlesCmd = new SequentialCommands();
				prepareNoodlesCmd.Add(new BoilWaterCmd());
				prepareNoodlesCmd.Add(new BoilNoodlesCmd());
				prepareNoodlesCmd.Add(new DrainNoodlesCmd());

				// The garlic must be sauteed before being added to the sauce, thus these
				// operations are placed into a SequentialCommands instance.
				var prepareGarlicCmd = new SequentialCommands();
				prepareGarlicCmd.Add(new SauteGarlicCmd());
				prepareGarlicCmd.Add(new AddGarlicToHeatingSauceCmd());

				// The following operations can be done in tandem (none are dependent upon any
				// of the others being complete before they are initiated), so they are placed into a
				// ParallelCommands instance.
				var prepareNoodlesAndSauceCmd = new ParallelCommands(Behavior.AbortUponFailure);
				prepareNoodlesAndSauceCmd.Add(new HeatSauceCmd());
				prepareNoodlesAndSauceCmd.Add(prepareGarlicCmd);
				prepareNoodlesAndSauceCmd.Add(prepareNoodlesCmd);

				// The noodles and sauce preparation must be complete before the sauce is
				// added to the noodles.
				var prepareSpaghettiCmd = new SequentialCommands();
				prepareSpaghettiCmd.Add(prepareNoodlesAndSauceCmd);
				prepareSpaghettiCmd.Add(new AddSauceToNoodlesCmd());

				// The lettuce doesn't have to be rinsed before the veggies are chopped. (Perhaps
				// there are two chefs in the kitchen, or the one chef likes to alternate rinsing
				// and chopping until both tasks are done.)
				var rinseLettuceAndChopVeggiesCmd = new ParallelCommands(Behavior.AbortUponFailure);
				rinseLettuceAndChopVeggiesCmd.Add(new RinseLettuceCmd());
				rinseLettuceAndChopVeggiesCmd.Add(new ChopVeggiesCmd());

				// The salad ingredients need to be ready before it is tossed, so these operations
				// are sequential.
				var prepareSaladCmd = new SequentialCommands();
				prepareSaladCmd.Add(rinseLettuceAndChopVeggiesCmd);
				prepareSaladCmd.Add(new TossSaladCmd());

				// This class inherits from ParallelCommands. We now call its Add() method to prepare
				// the spaghetti and prepare the salad. Neither task has operations that the other
				// depends upon, so they can be done in parallel.
				Add(prepareSpaghettiCmd);
				Add(prepareSaladCmd);
			}
		}

		private class BoilWaterCmd : PretendCmd
		{
			internal BoilWaterCmd() : base(TimeSpan.FromSeconds(5), "boiling water") { }
		}

		private class BoilNoodlesCmd : PretendCmd
		{
			internal BoilNoodlesCmd() : base(TimeSpan.FromSeconds(5), "boiling noodles") { }
		}

		private class SauteGarlicCmd : PretendCmd
		{
			internal SauteGarlicCmd() : base(TimeSpan.FromSeconds(2), "sauteing garlic") { }
		}

		private class HeatSauceCmd : PretendCmd
		{
			internal HeatSauceCmd() : base(TimeSpan.FromSeconds(5), "heating sauce") { }
		}

		private class AddGarlicToHeatingSauceCmd : PretendCmd
		{
			internal AddGarlicToHeatingSauceCmd() : base(TimeSpan.FromSeconds(1), "adding garlic to sauce") { }
		}

		private class DrainNoodlesCmd : PretendCmd
		{
			internal DrainNoodlesCmd() : base(TimeSpan.FromSeconds(1), "draining noodles") { }
		}

		private class AddSauceToNoodlesCmd : PretendCmd
		{
			internal AddSauceToNoodlesCmd() : base(TimeSpan.FromSeconds(1), "adding sauce to noodles") { }
		}

		private class ChopVeggiesCmd : PretendCmd
		{
			internal ChopVeggiesCmd() : base(TimeSpan.FromSeconds(5), "chopping veggies") { }
		}

		private class RinseLettuceCmd : PretendCmd
		{
			internal RinseLettuceCmd() : base(TimeSpan.FromSeconds(2), "rinsing lettuce") { }
		}

		private class TossSaladCmd : PretendCmd
		{
			internal TossSaladCmd() : base(TimeSpan.FromSeconds(2), "tossing salad") {}
		}

		private class PretendCmd : SequentialCommands
		{
			internal PretendCmd(TimeSpan duration, string desc) : base(null)
			{
                // The first and last commands added accomplish the same thing, but in different ways.

                // This command wraps an asynchronous Task
			    Add(TaskCommand<string>.Create(c => Console.Out.WriteLineAsync($"Started {desc}")));

                Add(new DelayCommand(duration));

                // This command wraps a synchronous function. Same end result as the async version above.
                // Only did it both ways for demonstration purposes.
			    Add(new DelegateCommand(() => Console.Out.WriteLine($"Finished {desc}")));
			}
		}

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

		// A delegate type to be used as the handler routine 
		// for SetConsoleCtrlHandler.
		public delegate bool HandlerRoutine(int ctrlType);

		private static bool ConsoleCtrlCheck(int ctrlType)
		{
			if (ctrlType == 0) // Ctrl-C
			{
				MakeDinnerCmd.AbortAndWait();
				return true;
			}

			return false;
		}
	}
}
