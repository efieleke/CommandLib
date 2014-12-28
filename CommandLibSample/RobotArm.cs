using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    /// <summary>
    /// A robot arm can move along X and Y axes, report its current position, open and close its clamp,
    /// and report whether it has successfully grabbed something.
    /// </summary>
    class RobotArm
    {
        [SerializableAttribute]
        public class OverheatedException : Exception
        {
            public OverheatedException(String message)
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
            Object UserData { get;  }
            Axis MoveAxis { get; }
            bool Aborted { get; }
            Exception Failure { get; }
            void Abort();
        }

        internal delegate void OperationCompleteEventHandler(object sender, IAbortableAsyncResult result);
        internal event OperationCompleteEventHandler MoveCompleteEvent;

        internal RobotArm()
        {
            random = new Random();
        }

        /// <summary>
        /// Begin moving along an axis.
        /// </summary>
        /// <param name="destination">The target to move to</param>
        /// <param name="userData">Caller defined data that will be passed to the completion callback</param>
        /// <returns></returns>
        internal IAbortableAsyncResult Move(Axis axis, int destination, Object userData)
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

                        aborted = moveOp.abortEvent.WaitOne(125);
                    }

                    moveOp.aborted = aborted;
                }
                catch (Exception e)
                {
                    moveOp.error = e;
                }

                moveOp.doneEvent.Set();

                if (MoveCompleteEvent != null)
                {
                    MoveCompleteEvent(this, moveOp);
                }
            });

            thread.Start();
            return moveOp;
        }

        /// <summary>
        /// The current position of this RobotArm
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        internal void GetPosition(out int x, out int y, out int z)
        {
            lock (criticalSection)
            {
                x = xPos;
                y = yPos;
                z = zPos;
            }
        }

        /// <summary>
        /// Opens the robot clamp
        /// </summary>
        internal void OpenClamp()
        {
            clampIsOpen = true;
        }

        /// <summary>
        /// Closes the clamp
        /// </summary>
        /// <returns>true, if the clamp actually grabbed something with this close operation</returns>
        internal bool CloseClamp()
        {
            if (clampIsOpen)
            {
                return random.Next() % 5 == 0;
            }

            return false;
        }

        private bool AdjustPosition(Axis axis, int destination)
        {
            if (random.Next() % 100 == 0)
            {
                throw new OverheatedException(String.Format("Error: {0} axis motor has overheated.", axis));
            }

            lock (criticalSection)
            {
                // Yuck! Wish I could pass an int by reference to this method.
                switch(axis)
                {
                    case Axis.X:
                        if (xPos == destination)
                        {
                            return false;
                        }

                        if (xPos < destination)
                        {
                            ++xPos;
                        }
                        else
                        {
                            --xPos;
                        }

                        break;
                    case Axis.Y:
                        if (yPos == destination)
                        {
                            return false;
                        }

                        if (yPos < destination)
                        {
                            ++yPos;
                        }
                        else
                        {
                            --yPos;
                        }

                        break;
                    default:
                        if (zPos == destination)
                        {
                            return false;
                        }

                        if (zPos < destination)
                        {
                            ++zPos;
                        }
                        else
                        {
                            --zPos;
                        }

                        break;
                }

                return true;
            }
        }

        private class Operation : IAbortableAsyncResult
        {
            internal Operation (Axis axis, Object userData)
            {
                this.axis = axis;
                this.userData = userData;
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
                get { return doneEvent; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return doneEvent.WaitOne(0); }
            }

            public Axis MoveAxis
            {
                get { return axis; }
            }

            public Object UserData
            {
                get { return userData;  }
            }

            public bool Aborted
            {
                get { return aborted;  }
            }

            public Exception Failure
            {
                get { return error;  }
            }

            public void Abort()
            {
                abortEvent.Set();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    disposed = true;

                    if (disposing)
                    {
                        doneEvent.Dispose();
                        abortEvent.Dispose();
                    }
                }
            }

            internal bool aborted = false;
            internal Exception error;
            internal System.Threading.ManualResetEvent doneEvent = new System.Threading.ManualResetEvent(false);
            internal System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false);
            internal Axis axis;
            internal Object userData;
            private bool disposed = false;
        }

        private Random random;
        private int xPos = 0;
        private int yPos = 0;
        private int zPos = 0;
        private bool clampIsOpen = false;
        private Object criticalSection = new Object();
    }
}
