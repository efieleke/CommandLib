using System;
using System.Threading.Tasks;
using CommandLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLibTests
{
	[TestClass]
	public class TaskCommandTests
	{
		[TestMethod]
		public void Command_TestFromCommand()
		{
			using (var cmd = new DoNothingCommand())
			{
				using (Task<int> task = Command.CreateTask<int>(cmd, 7))
				{
					task.Start();
					Assert.AreEqual(7, task.Result);
				}

				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					task.Start();
					Assert.AreEqual(5, task.Result);
				}
			}
		}

		[TestMethod]
		public void TaskCommand_TestSuccess()
		{
			using (var cmd = new DoNothingCommand())
			{
				Assert.AreEqual(5, cmd.SyncExecute(5));
				Assert.AreEqual(4, cmd.SyncExecute(4));
			}
		}

		[TestMethod]
		public void TaskCommand_TestAsync()
		{
			using (var cmd = new DoNothingCommand())
			{
				cmd.AsyncExecute(new DoNothingListener(1), 1);
				cmd.Wait();
				cmd.AsyncExecute(new DoNothingListener(2), 2);
				cmd.Wait();
			}
		}

		[TestMethod]
		public void TaskCommand_TestError()
		{
			using (var cmd = new DoNothingCommand(true))
			{
				try
				{
					cmd.SyncExecute();
					Assert.Fail("Did not expect to get here");
				}
				catch(Exception e)
				{
					Assert.AreEqual("boo hoo", e.Message);
				}
			}
		}

		private class DoNothingListener : ICommandListener
		{
			internal DoNothingListener(object expectedResult) { this.expectedResult = expectedResult;  }
			public void CommandAborted() { return; }
			public void CommandFailed(Exception exc) { return; }
			public void CommandSucceeded(object result)	{ Assert.AreEqual(expectedResult, result);  return; }
			private readonly object expectedResult;
		}

		private class DoNothingCommand : TaskCommand<int>
		{
			internal DoNothingCommand() : this(false) { }
			internal DoNothingCommand(bool fail) : base(null) { this.fail = fail; }

			protected override Task<int> CreateTask(object runtimeArg)
			{
				if (fail) { throw new Exception("boo hoo"); }
				return Task.FromResult((int)runtimeArg);
			}

			private readonly bool fail;
		}
	}
}
