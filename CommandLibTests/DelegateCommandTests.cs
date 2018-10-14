using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
	[TestClass]
	public class DelegateCommandTests
	{
		[TestMethod]
		public void TestDelegateCommand()
		{
			using (var cmd = new DelegateCommand<int>(() => 7))
			{
				HappyPathTest.Run(cmd, null, 7);
			}

			using (var cmd = new DelegateCommand<int>(i => (int)i))
			{
				HappyPathTest.Run(cmd, 5, 5);
			}
		}
	}
}
