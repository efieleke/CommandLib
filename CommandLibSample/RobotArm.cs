using System;

namespace CommandLibSample
{
    /// <summary>
    /// A robot arm can move along X and Y axes, report its current position, open and close its clamp,
    /// and report whether it has successfully grabbed something.
	/// 
	/// This class is contrived in that it simulates being asynchronous via worker threads and idle waits.
	/// For that reason there is probably not much learning to be gained by comprehending anything more
	/// than its public interface.
    /// </summary>
    internal class RobotArm
    {
        [SerializableAttribute]
        public class OverheatedException : Exception
        {
            public OverheatedException(string message)
                : base(message)
            {
            }
        }

        internal enum Axis
        {
            X,
            Y,
            Z
        }

        /// <summary>
        /// The object returned by the RobotArm move methods.
        /// </summary>
        internal interface IAbortableAsyncResult : IAsyncResult, IDisposable
        {
            object UserData { get;  }
            Axis MoveAxis { get; }
            bool Aborted { get; }
            Exception Failure { get; }
            void Abort();
        }

        internal delegate void OperationCompleteEventHandler(object sender, IAbortableAsyncResult result);
        internal event OperationCompleteEventHandler MoveCompleteEvent;

        internal RobotArm()
        {
            _random = new Random();
        }

	    /// <summary>
	    /// Begin moving along an axis.
	    /// </summary>
	    /// <param name="axis">The axis on which to move</param>
	    /// <param name="destination">The target to move to</param>
	    /// <param name="userData">Caller defined data that will be passed to the completion callback</param>
	    /// <returns></returns>
	    internal IAbortableAsyncResult Move(Axis axis, int destination, object userData)
        {
            Operation moveOp = new Operation(axis, userData);

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                try
                {
                    bool aborted = false;

                    while (!aborted)
                    {
                        if (!AdjustPosition(axis, destination))
                        {
                            break;
                        }

                        aborted = moveOp.AbortEvent.WaitOne(125);
                    }

                    moveOp.aborted = aborted;
                }
                catch (Exception e)
                {
                    moveOp.Error = e;
                }

                moveOp.DoneEvent.Set();
				MoveCompleteEvent?.Invoke(this, moveOp);
			});

            thread.Start();
            return moveOp;
        }

	    /// <summary>
	    /// The current position of this RobotArm
	    /// </summary>
	    /// <param name="x">x coordinate</param>
	    /// <param name="y">y coordinate</param>
	    /// <param name="z">coordinate</param>
	    internal void GetPosition(out int x, out int y, out int z)
        {
            lock (_criticalSection)
            {
                x = _xPos;
                y = _yPos;
                z = _zPos;
            }
        }

        /// <summary>
        /// Opens the robot clamp
        /// </summary>
        internal void OpenClamp()
        {
            _clampIsOpen = true;
        }

        /// <summary>
        /// Closes the clamp
        /// </summary>
        /// <returns>true, if the clamp actually grabbed something with this close operation</returns>
        internal bool CloseClamp()
        {
            if (_clampIsOpen)
            {
                _clampIsOpen = false;
                return _random.Next() % 5 == 0;
            }

            return false;
        }

        private bool AdjustPosition(Axis axis, int destination)
        {
            if (_random.Next() % 100 == 0)
            {
                throw new OverheatedException($"Error: {axis} axis motor has overheated.");
            }

            lock (_criticalSection)
            {
                // Yuck! Wish I could pass an int by reference to this method.
                switch(axis)
                {
                    case Axis.X:
                        if (_xPos == destination)
                        {
                            return false;
                        }

                        if (_xPos < destination)
                        {
                            ++_xPos;
                        }
                        else
                        {
                            --_xPos;
                        }

                        break;
                    case Axis.Y:
                        if (_yPos == destination)
                        {
                            return false;
                        }

                        if (_yPos < destination)
                        {
                            ++_yPos;
                        }
                        else
                        {
                            --_yPos;
                        }

                        break;
                    default:
                        if (_zPos == destination)
                        {
                            return false;
                        }

                        if (_zPos < destination)
                        {
                            ++_zPos;
                        }
                        else
                        {
                            --_zPos;
                        }

                        break;
                }

                return true;
            }
        }

        private class Operation : IAbortableAsyncResult
        {
            internal Operation (Axis axis, object userData)
            {
                MoveAxis = axis;
                UserData = userData;
            }

            ~Operation()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            public object AsyncState
            {
                get { throw new NotImplementedException(); }
            }

            public System.Threading.WaitHandle AsyncWaitHandle
            {
                get { return DoneEvent; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return DoneEvent.WaitOne(0); }
            }

            public Axis MoveAxis { get; }

	        public object UserData { get; }

	        public bool Aborted
            {
                get { return aborted;  }
            }

            public Exception Failure
            {
                get { return Error;  }
            }

            public void Abort()
            {
                AbortEvent.Set();
            }

            private void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    if (disposing)
                    {
                        DoneEvent.Dispose();
                        AbortEvent.Dispose();
                    }
                }
            }

	        // ReSharper disable once InconsistentNaming
	        internal bool aborted;
            internal Exception Error;
            internal readonly System.Threading.ManualResetEvent DoneEvent = new System.Threading.ManualResetEvent(false);
            internal readonly System.Threading.ManualResetEvent AbortEvent = new System.Threading.ManualResetEvent(false);
	        private bool _disposed;
        }

        private readonly Random _random;
        private int _xPos;
        private int _yPos;
	    private int _zPos;
        private bool _clampIsOpen;
        private readonly object _criticalSection = new object();
    }
}
