using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLibSample
{
    class Robot
    {
        internal Robot (String name, int xPos, int yPos)
        {
            this.name = name;
            this.xPos = xPos;
            this.yPos = yPos;
        }

        internal interface IAbortableAsyncResult : IAsyncResult, IDisposable
        {
            Object UserData { get;  }
            bool Aborted { get; }
            void Abort();
        }

        internal delegate void OperationCompleteEventHandler(object sender, IAbortableAsyncResult result);
        internal event OperationCompleteEventHandler MoveXCompleteEvent;
        internal event OperationCompleteEventHandler MoveYCompleteEvent;
        internal event OperationCompleteEventHandler WaitForRobotCompleteEvent;

        internal IAbortableAsyncResult MoveX(int destination, Object userData)
        {
            Operation moveOp = new Operation(userData);

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                bool aborted = false;

                while (!aborted)
                {
                    lock (criticalSection)
                    {
                        if (xPos > destination)
                        {
                            --xPos;
                        }
                        else if (xPos < destination)
                        {
                            ++xPos;
                        }
                        else
                        {
                            break;
                        }
                    }

                    aborted = moveOp.abortEvent.WaitOne(250);
                }

                moveOp.aborted = aborted;
                moveOp.doneEvent.Set();

                if (MoveXCompleteEvent != null)
                {
                    MoveXCompleteEvent(this, moveOp);
                }
            });

            thread.Start();
            return moveOp;
        }

        internal IAbortableAsyncResult MoveY(int destination, Object userData)
        {
            // Very similar to MoveXToZero. Consider refactoring.
            Operation moveOp = new Operation(userData);

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                bool aborted = false;

                while (!aborted)
                {
                    lock (criticalSection)
                    {
                        if (yPos > destination)
                        {
                            --yPos;
                        }
                        else if (yPos < destination)
                        {
                            ++yPos;
                        }
                        else
                        {
                            break;
                        }
                    }

                    aborted = moveOp.abortEvent.WaitOne(250);
                }

                moveOp.aborted = aborted;
                moveOp.doneEvent.Set();

                if (MoveYCompleteEvent != null)
                {
                    MoveYCompleteEvent(this, moveOp);
                }
            });

            thread.Start();
            return moveOp;
        }

        internal void GetPosition(out int x, out int y)
        {
            lock (criticalSection)
            {
                x = xPos;
                y = yPos;
            }
        }

        internal IAbortableAsyncResult WaitForRobot(Robot robot, Object userData)
        {
            Operation waitOp = new Operation(userData);

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                bool aborted = false;

                while (!aborted)
                {
                    int x, y;
                    robot.GetPosition(out x, out y);

                    lock (criticalSection)
                    {
                        if (x == xPos && y == yPos)
                        {
                            break;
                        }
                    }

                    aborted = waitOp.abortEvent.WaitOne(100);
                }

                waitOp.aborted = aborted;
                waitOp.doneEvent.Set();

                if (WaitForRobotCompleteEvent != null)
                {
                    WaitForRobotCompleteEvent(this, waitOp);
                }
            });

            thread.Start();
            return waitOp;
        }

        internal String Name
        {
            get { return name;  }
        }

        internal String Greeting
        {
            get {  return "Hello. My name is " + Name + "."; }
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

        private String name;
        private int xPos;
        private int yPos;
        private Object criticalSection = new Object();
    }
}
