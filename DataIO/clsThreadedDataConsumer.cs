using System;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

namespace DataIO
{
    public class clsThreadedDataConsumer: clsDataConsumer
    {

        //list of all threaded data consumers 
        //used for cleanup
        private static List<clsThreadedDataConsumer> mThreadList = new List<clsThreadedDataConsumer>();

        private Thread mConsumeThread = null;

        private int mTimeOut = 500; //millseconds

        //thread state
        private bool mKillThread = false;  //flag to stop the thread from running
        private bool mStopping = false;     // trying to stop the thread, accepts no new input
        private bool mRunning = false;
        private bool mAborting = false;

        //default constructor starts the consuming thread spinning
        public clsThreadedDataConsumer()
        {
            lock (typeof(clsThreadedDataConsumer))
            {
                mThreadList.Add(this);
            }

            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                //App.Info("Start");
                this.Start();
            }
        }

        public clsThreadedDataConsumer(bool startSpinning)
        {
            lock (typeof(clsThreadedDataConsumer))
            {
                mThreadList.Add(this);
            }

            if (startSpinning)
            {
                if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
                {
                    //App.Info("Start");
                    this.Start();
                }
            }
        }

        public static void DisposeAll()
        {
            try
            {
                lock (typeof(clsThreadedDataConsumer))
                {
                    foreach (clsThreadedDataConsumer consumer in mThreadList)
                    {
                        consumer.HardStop();
                    }
                }
                mThreadList.Clear();
            }
            catch(Exception e)
            {
            }
        }

        private void ResetThreadState()
        {
            lock (mQ.SyncRoot)
            {
                mKillThread = false;
                mRunning = false;
                mStopping = false;
                mAborting = false;
            }
        }

        //use this to set the state variables if not in a locked section
        private void SetState(ref bool stateVariable, bool value)
        {
            lock (mQ.SyncRoot)
            {
                stateVariable  = value;
            }
        }

        //use this to get the state variables if not in a locked section
        private bool GetState(ref bool stateVariable)
        {
            bool val = false;

            lock (mQ.SyncRoot)
            {
                val = stateVariable;
            }

            return val;
        }

        public  void Start()
        {
            //don't want to access anything before we are spun up
            lock (mQ.SyncRoot)
            {
                //create and start the new consuming thread
                if (mConsumeThread == null)
                {
                    ResetThreadState();
                    mConsumeThread = new Thread(this.Consume);
                    mConsumeThread.Start();
                    mConsumeThread.Priority = ThreadPriority.Lowest;

                    //wait for thread to spin up, otherwise feeds will not make it onto the queue
                    while (!mRunning)
                    {
                        bool timedOut = !Monitor.Wait(mQ.SyncRoot, 10, true);
                    }
                }
            }
        }

        public virtual void Consume()
        {
            try
            {
                lock (mQ.SyncRoot)
                {
                    if (mRunning)
                    {
                        return;
                    }
                    mRunning = true;
                }
               object obj= null;

                while (true)
                {
                    lock (mQ.SyncRoot)
                    {
                        if (mKillThread) return;

                        while (mQ.Count == 0)
                        {
                            //blocks thread until pulsed
                            //lock is released, data may be added to the queue by another thread at this point
                            //if the thread is not pumped before the timeout, we drop out and check the kill status
                            bool timedOut = ! Monitor.Wait(mQ.SyncRoot, mTimeOut, true);

                            if (mKillThread) return;
                        }

                        if (mKillThread) return;
                        obj = mQ.Dequeue();
                    }

                    //lock is released, data may be added to the queue by another thread at this point
                    if (obj != null)
                    {
                        ProcessData(obj);
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            finally
            {
            }
        }

        public void Pulse()
        {
            Monitor.Pulse(mQ.SyncRoot);
        }

        private void BeginStop()
        {
            lock (mQ.SyncRoot)
            {
                mKillThread = true;
                mStopping = true;
            }

            if (mConsumeThread != null)
            {
                mConsumeThread.Interrupt();
                mConsumeThread.Join();
            }
            mConsumeThread = null;
            
        }

        public void SoftStop()
        {
            BeginStop();

            //flush the queue on the calling thread
            this.Flush();

            ResetThreadState();
        }

        public void HardStop()
        {
            BeginStop();

            //clear the queue
            lock (mQ.SyncRoot)
            {
                mQ.Clear();
            }

            ResetThreadState();
        }

        public void Abort()
        {
            lock (mQ.SyncRoot)
            {
                mKillThread = true;
                mAborting = true;
            }

            if (mConsumeThread != null)
            {
                mConsumeThread.Abort();
                mConsumeThread.Join();
            }
            mConsumeThread = null;
            ResetThreadState();
        }

        public override void FeedInput(object obj)
        {
            // this function has the potential for an infinite loop if called on the same thread,
            // or if it loads a thread that calls back to this thread.  Carefully check for this.
          
            // this will block indefinitely if it can't obtain the lock 
            // find out how to set a timeout here.
            lock (mQ.SyncRoot)
            {
                if (!mRunning) 
                    return;

                if (mStopping) 
                    return;

                if (mAborting) 
                    return;

                mQ.Enqueue(obj);
                //wake the consuming thread up to get the new data
                Monitor.Pulse(mQ.SyncRoot);
            }
        }

    }
}
