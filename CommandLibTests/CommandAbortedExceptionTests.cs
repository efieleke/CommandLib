using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sophos.Commands;

namespace CommandLibTests
{
    [TestClass]
    public class CommandAbortedExceptionTests
    {
        [TestMethod]
        public void TestConstructors()
        {
            var exc1 = new CommandAbortedException("This is a test");
            Assert.AreEqual("This is a test", exc1.Message);
            var exc2 = new CommandAbortedException("This is another test", exc1);
            Assert.AreEqual("This is another test", exc2.Message);
            Assert.IsTrue(ReferenceEquals(exc1, exc2.InnerException));

            var serializer = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, exc2);
                stream.Seek(0, SeekOrigin.Begin);
                CommandAbortedException exc3 = (CommandAbortedException)serializer.Deserialize(stream);
                Assert.AreEqual(exc2.Message, exc3.Message);
                Assert.AreEqual(exc2.InnerException?.Message, exc3.InnerException?.Message);
            }
        }
    }
}
