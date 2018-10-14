using System;
using System.Threading.Tasks;
using Sophos.Commands;
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
				using (Task<int> task = Command.AsTask<int>(cmd, 7))
				{
					task.Start();
					Assert.AreEqual(7, task.Result);
				}

				using (Task<int> task = Command.AsTask<int>(cmd, 5))
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
				HappyPathTest.Run(cmd, 5, 5);
				HappyPathTest.Run(cmd, 4, 4);
			}
		}

		[TestMethod]
		public void TaskCommand_TestError()
		{
			using (var cmd = new DoNothingCommand(DoNothingCommand.Behavior.FailUpFront))
			{
				FailTest.Run<NotSupportedException>(cmd, 7);
			}

			using (var cmd = new DoNothingCommand(DoNothingCommand.Behavior.FailInTask))
			{
				FailTest.Run<InvalidOperationException>(cmd, 7);
			}
		}

		[TestMethod]
		public void TaskCommand_TestAbort()
		{
			using (var cmd = new DoNothingCommand(DoNothingCommand.Behavior.Abort))
			{
				AbortTest.Run(cmd, 7, 0);
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
			internal enum Behavior {Succeed, FailUpFront, FailInTask, AbortUpFront, Abort };

			internal DoNothingCommand() : this(Behavior.Succeed) { }
			internal DoNothingCommand(Behavior behavior) : base(null) { this.behavior = behavior; }

			protected override Task<int> CreateTask(object runtimeArg)
			{
				if (behavior == Behavior.FailUpFront) { throw new NotSupportedException(); }

				return Task.Run<int>(() =>
				{	
					if (behavior == Behavior.FailInTask) { throw new InvalidOperationException(); }

					if (behavior == Behavior.Abort && abortEvent.WaitOne())
					{
						throw new CommandAbortedException();
					}

					return (int)runtimeArg;
				});
			}

			protected override void AbortImpl()
			{
				abortEvent.Set();
			}

			private readonly Behavior behavior;
			private readonly System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false);
		}
	}
}
