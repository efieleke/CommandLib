using System;
using Sophos.Commands;

namespace CommandLibSample
{
    class MoveRobotArmOnAxisCommand : AsyncCommand
    {
        internal MoveRobotArmOnAxisCommand(RobotArm robotArm, int destination, RobotArm.Axis axis, Command owner)
            : base(owner)
        {
            this.robotArm = robotArm;
            this.destination = destination;
            this.axis = axis;

            robotArm.MoveCompleteEvent += MoveCompleted;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                robotArm.MoveCompleteEvent -= MoveCompleted;

                if (asyncResult != null)
                {
                    asyncResult.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            this.listener = listener;
            RobotArm.IAbortableAsyncResult result = robotArm.Move(axis, destination, this);

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
                else if (e.Failure != null)
                {
                    listener.CommandFailed(e.Failure);
                }
                else
                {
                    listener.CommandSucceeded(null);
                }
            }
        }

        private readonly RobotArm robotArm;
        private readonly int destination;
        private readonly RobotArm.Axis axis;
        private RobotArm.IAbortableAsyncResult asyncResult;
        private ICommandListener listener;
        private readonly Object criticalSection = new Object();
    }
}
