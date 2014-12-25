using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class MoveRobotArmOnAxisCommand : CommandLib.AsyncCommand
    {
        internal MoveRobotArmOnAxisCommand(RobotArm robotArm, int destination, bool xAxis, CommandLib.Command owner)
            : base(owner)
        {
            this.robotArm = robotArm;
            this.destination = destination;
            this.xAxis = xAxis;

            if (xAxis)
            {
                robotArm.MoveXCompleteEvent += MoveCompleted;
            }
            else
            {
                robotArm.MoveYCompleteEvent += MoveCompleted;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (xAxis)
                {
                    robotArm.MoveXCompleteEvent -= MoveCompleted;
                }
                else
                {
                    robotArm.MoveYCompleteEvent -= MoveCompleted;
                }

                if (asyncResult != null)
                {
                    asyncResult.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override void AsyncExecuteImpl(CommandLib.ICommandListener listener, object runtimeArg)
        {
            this.listener = listener;
            RobotArm.IAbortableAsyncResult result;

            if (xAxis)
            {
                result = robotArm.MoveX(destination, this);
            }
            else
            {
                result = robotArm.MoveY(destination, this);
            }

            lock (criticalSection)
            {
                if (asyncResult != null)
                {
                    asyncResult.Dispose();
                }

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

        private void MoveCompleted(Object sender, RobotArm.IAbortableAsyncResult e)
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

        private readonly RobotArm robotArm;
        private readonly int destination;
        private readonly bool xAxis;
        private RobotArm.IAbortableAsyncResult asyncResult;
        private CommandLib.ICommandListener listener;
        private Object criticalSection = new Object();
    }
}
