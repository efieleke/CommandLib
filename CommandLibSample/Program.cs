using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sophos.Commands;

// This application prepares a spaghetti and salad dinner with a chef who is an expert multitasker.
namespace CommandLibSample
{
	internal class Program
	{
		private static readonly PrepareDinnerCmd MakeDinnerCmd = new PrepareDinnerCmd();

		private static void Main()
		{
			// Trap Ctrl-C in to provide an example of aborting a command
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
		}

		private class PrepareDinnerCmd : ParallelCommands
		{
			internal PrepareDinnerCmd() : base(abortUponFailure: true)
			{
				var sauteGarlicAndHeatSauceCmd = new ParallelCommands(abortUponFailure: true);
				sauteGarlicAndHeatSauceCmd.Add(new SauteGarlicCmd());
				sauteGarlicAndHeatSauceCmd.Add(new HeatSauceCmd());

				var prepareSauceCmd = new SequentialCommands();
				prepareSauceCmd.Add(sauteGarlicAndHeatSauceCmd);
				prepareSauceCmd.Add(new AddGarlicToHeatingSauceCmd());

				var prepareNoodlesCmd = new SequentialCommands();
				prepareNoodlesCmd.Add(new BoilWaterCmd());
				prepareNoodlesCmd.Add(new BoilNoodlesCmd());
				prepareNoodlesCmd.Add(new DrainNoodlesCmd());

				var prepareNoodlesAndSauceCmd = new ParallelCommands(abortUponFailure: true);
				prepareNoodlesAndSauceCmd.Add(prepareSauceCmd);
				prepareNoodlesAndSauceCmd.Add(prepareNoodlesCmd);

				var prepareSpaghettiCmd = new SequentialCommands();
				prepareSpaghettiCmd.Add(prepareNoodlesAndSauceCmd);
				prepareSpaghettiCmd.Add(new AddSauceToNoodlesCmd());

				var prepareSaladCmd = new SequentialCommands();
				prepareSaladCmd.Add(new RinseLettuceCmd());
				prepareSaladCmd.Add(new ChopVeggiesCmd());
				prepareSaladCmd.Add(new TossSaladCmd());

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

		private class PretendCmd : SyncCommand
		{
			internal PretendCmd(TimeSpan duration, string desc) : base(null)
			{
				_pauseCmd = new PauseCommand(duration, null, this);
				_desc = desc;
			}

			protected override object SyncExeImpl(object runtimeArg)
			{
				Console.Out.WriteLine("Started " + _desc);
				_pauseCmd.SyncExecute();
				Console.Out.WriteLine("Finished " + _desc);
				return null;
			}

			private readonly PauseCommand _pauseCmd;
			private readonly string _desc;
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
