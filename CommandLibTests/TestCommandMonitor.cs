using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLibTests
{
    internal class TestCommandMonitor : CommandLib.ICommandMonitor
    {
        public void CommandStarting(CommandLib.Command command)
        {
            tracer.CommandStarting(command);
        }

        public void CommandFinished(CommandLib.Command command, Exception exc)
        {
            tracer.CommandFinished(command, exc);
        }

        private CommandLib.CommandTracer tracer = new CommandLib.CommandTracer();
    }
}
