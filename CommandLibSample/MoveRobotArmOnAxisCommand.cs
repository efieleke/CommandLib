using Sophos.Commands;

namespace CommandLibSample
{
	internal class MoveRobotArmOnAxisCommand : AsyncCommand
    {
        internal MoveRobotArmOnAxisCommand(RobotArm robotArm, int destination, RobotArm.Axis axis, Command owner)
            : base(owner)
        {
            _robotArm = robotArm;
            _destination = destination;
            _axis = axis;

            robotArm.MoveCompleteEvent += MoveCompleted;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _robotArm.MoveCompleteEvent -= MoveCompleted;

	            // ReSharper disable once InconsistentlySynchronizedField
	            _asyncResult?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void AsyncExecuteImpl(ICommandListener listener, object runtimeArg)
        {
            _listener = listener;
            RobotArm.IAbortableAsyncResult result = _robotArm.Move(_axis, _destination, this);

            lock (_criticalSection)
            {
	            _asyncResult?.Dispose();
	            _asyncResult = result;
            }
        }

        protected override void AbortImpl()
        {
            lock (_criticalSection)
            {
	            _asyncResult?.Abort();
            }
        }

        private void MoveCompleted(object sender, RobotArm.IAbortableAsyncResult e)
        {
            if (e.UserData == this)
            {
                if (e.Aborted)
                {
                    _listener.CommandAborted();
                }
                else if (e.Failure != null)
                {
                    _listener.CommandFailed(e.Failure);
                }
                else
                {
                    _listener.CommandSucceeded(null);
                }
            }
        }

        private readonly RobotArm _robotArm;
        private readonly int _destination;
        private readonly RobotArm.Axis _axis;
        private RobotArm.IAbortableAsyncResult _asyncResult;
        private ICommandListener _listener;
        private readonly object _criticalSection = new object();
    }
}
