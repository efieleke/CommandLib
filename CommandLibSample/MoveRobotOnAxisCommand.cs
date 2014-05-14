using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotOnAxisCommand : CommandLib.AsyncCommand
    {
        internal MoveRobotOnAxisCommand(Robot robot, int destination, bool xAxis, CommandLib.Command owner) : base(owner)
        {
            this.robot = robot;
            this.destination = destination;
            this.xAxis = xAxis;

            if (xAxis)
            {
                robot.MoveXCompleteEvent += MoveCompleted;
            }
            else
            {
                robot.MoveYCompleteEvent += MoveCompleted;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (xAxis)
                {
                    robot.MoveXCompleteEvent -= MoveCompleted;
                }
                else
                {
                    robot.MoveYCompleteEvent -= MoveCompleted;
                }
            }

            base.Dispose(disposing);
        }

        protected override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
        {
            this.listener = listener;
            Robot.IAbortableAsyncResult result;

            if (xAxis)
            {
                result = robot.MoveX(destination, this);
            }
            else
            {
                result = robot.MoveY(destination, this);
            }

            lock (criticalSection)
            {
                asyncResult = result;
            }
        }

        protected override void AbortImpl()
        {
            lock (criticalSection)
            {
                if (asyncResult != null)
                {
                    asyncResult.Abort();
                }
            }
        }

        private void MoveCompleted(Object sender, Robot.IAbortableAsyncResult e)
        {
            if (e.UserData == this)
            {
                if (e.Aborted)
                {
                    listener.CommandAborted();
                }
                else
                {
                    listener.CommandSucceeded(null);
                }
            }
        }

        private readonly Robot robot;
        private readonly int destination;
        private readonly bool xAxis;
        private Robot.IAbortableAsyncResult asyncResult;
        private CommandLib.ICommandListener listener;
        private Object criticalSection = new Object();
    }
}
