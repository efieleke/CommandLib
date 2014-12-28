﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    /// <summary>
    /// A robot arm can move along X and Y axes, and return its current position
    /// </summary>
    class RobotArm
    {
        internal RobotArm (int xPos, int yPos)
        {
            this.xPos = xPos;
            this.yPos = yPos;
        }

        /// <summary>
        /// The object returned by the RobotArm move methods.
        /// </summary>
        internal interface IAbortableAsyncResult : IAsyncResult, IDisposable
        {
            Object UserData { get;  }
            bool Aborted { get; }
            void Abort();
        }

        internal delegate void OperationCompleteEventHandler(object sender, IAbortableAsyncResult result);
        internal event OperationCompleteEventHandler MoveXCompleteEvent;
        internal event OperationCompleteEventHandler MoveYCompleteEvent;

        /// <summary>
        /// Begin moving along the X axis. Rate is four units per second. 
        /// </summary>
        /// <param name="destination">The target to move to</param>
        /// <param name="userData">Caller defined data that will be passed to the completion callback</param>
        /// <returns></returns>
        internal IAbortableAsyncResult MoveX(int destination, Object userData)
        {
            return Move(destination, userData, true);
        }

        /// <summary>
        /// Begin moving along the Y axis. Rate is four units per second. 
        /// </summary>
        /// <param name="destination">The target to move to</param>
        /// <param name="userData">Caller defined object that will be passed to the completion callback</param>
        /// <returns></returns>
        internal IAbortableAsyncResult MoveY(int destination, Object userData)
        {
            return Move(destination, userData, false);
        }

        /// <summary>
        /// The current position of this RobotArm
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        internal void GetPosition(out int x, out int y)
        {
            lock (criticalSection)
            {
                x = xPos;
                y = yPos;
            }
        }

        private IAbortableAsyncResult Move(int destination, Object userData, bool xAxis)
        {
            Operation moveOp = new Operation(userData);

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                bool aborted = false;

                while (!aborted)
                {
                    if (!AdjustPosition(destination, xAxis))
                    {
                        break;
                    }

                    aborted = moveOp.abortEvent.WaitOne(125);
                }

                moveOp.aborted = aborted;
                moveOp.doneEvent.Set();
                OperationCompleteEventHandler handler = xAxis ? MoveXCompleteEvent : MoveYCompleteEvent;

                if (handler != null)
                {
                    handler(this, moveOp);
                }
            });

            thread.Start();
            return moveOp;
        }

        private bool AdjustPosition(int destination, bool xAxis)
        {
            lock (criticalSection)
            {
                if (xAxis)
                {
                    if (xPos < destination)
                    {
                        ++xPos;
                    }
                    else if (xPos > destination)
                    {
                        --xPos;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (yPos < destination)
                    {
                        ++yPos;
                    }
                    else if (yPos > destination)
                    {
                        --yPos;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private class Operation : IAbortableAsyncResult
        {
            internal Operation (Object userData)
            {
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

            public Object UserData
            {
                get { return userData;  }
            }

            public bool Aborted
            {
                get { return aborted;  }
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
            internal System.Threading.ManualResetEvent doneEvent = new System.Threading.ManualResetEvent(false);
            internal System.Threading.ManualResetEvent abortEvent = new System.Threading.ManualResetEvent(false);
            internal Object userData;
            private bool disposed = false;
        }

        private int xPos;
        private int yPos;
        private Object criticalSection = new Object();
    }
}
