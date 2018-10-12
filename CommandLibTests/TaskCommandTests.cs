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
				using (Task<Object> task = Command.CreateTask<Object>(cmd))
				{
					task.Start();
					Assert.IsNull(task.Result);
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
				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					var taskCmd = new TaskCommand<int>(task);
					Assert.AreEqual(5, taskCmd.SyncExecute());
				}

				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					var taskCmd = new TaskCommand<int>(task);
					Assert.AreEqual(5, taskCmd.SyncExecute(4));// arg to SyncExecute is ignored
				}
			}
		}

		[TestMethod]
		public void TaskCommand_TestAsync()
		{
			using (var cmd = new DoNothingCommand())
			{
				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					var taskCmd = new TaskCommand<int>(task);
					taskCmd.AsyncExecute(new DoNothingListener(5), 1); // runtime arg is ignored
					taskCmd.Wait();
				}
			}
		}

		[TestMethod]
		public void TaskCommand_TestAbort()
		{
			using (var cmd = new PauseCommand(TimeSpan.FromDays(1)))
			{
				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					var taskCmd = new TaskCommand<int>(task);
					taskCmd.AsyncExecute(new DoNothingListener(4));
					cmd.Abort();
					taskCmd.Wait();
				}
			}
		}

		[TestMethod]
		public void TaskCommand_TestError()
		{
			using (var cmd = new DoNothingCommand(true))
			{
				using (Task<int> task = Command.CreateTask<int>(cmd, 5))
				{
					var taskCmd = new TaskCommand<int>(task);

					try
					{
						taskCmd.SyncExecute();
						Assert.Fail("Did not expect to get here");
					}
					catch(Exception e)
					{
						Assert.AreEqual("boo hoo", e.Message);
					}
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

		private class DoNothingCommand : SyncCommand
		{
			internal DoNothingCommand() : this(false) { }
			internal DoNothingCommand(bool fail) : base(null) { this.fail = fail; }

			protected override object SyncExeImpl(object runtimeArg)
			{
				if (fail) { throw new Exception("boo hoo"); }
				return runtimeArg;
			}

			private readonly bool fail;
		}
	}
}
