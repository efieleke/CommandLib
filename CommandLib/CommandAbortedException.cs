using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLib
{
    /// <summary>
    /// This is thrown from Command.SyncExecute() when a command is aborted.
    /// </summary>
    [SerializableAttribute]
    public class CommandAbortedException : Exception
    {
    }
}
