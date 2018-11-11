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
            HappyPathTest.Run(new DelegateCommand<int>(() => 7), null, 7);
            HappyPathTest.Run(new DelegateCommand<int>(i => (int)i), 5, 5);
		}
	}
}
