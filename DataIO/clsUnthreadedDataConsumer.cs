using System;
using System.Windows.Forms;

namespace DataIO
{
    //processes on other threads feed this class
    //in order to update controls running on the UI thread
    public class clsUnthreadedDataConsumer: clsDataConsumer
    {
        bool mConsuming = false;

        private Control mControl = null;
        public Control Control
        {
            get { return mControl; }
            set { mControl = value; }
        }

        private void Consume()
        {
            try
            {
                int count = 0;
                object obj = null;

                //prevent reentrance
                lock (mQ.SyncRoot)
                {
                    if (mConsuming == true) return;
                    mConsuming = true;
                    count = mQ.Count;
                }

                while (count > 0)
                {
                    lock (mQ.SyncRoot)
                    {
                        obj = mQ.Dequeue();
                    }

                    //lock is released, data may be added to the queue by another thread at this point
                    if (obj != null)
                        ProcessData(obj);

                    lock (mQ.SyncRoot)
                    {
                        count = mQ.Count;
                    }
                }
            }
            finally
            {
                mConsuming = false;
            }
        }

        public override void FeedInput(object obj)
        {
            lock (mQ.SyncRoot)
            {
                if (mControl != null)
                {
                    mQ.Enqueue(obj);

                    //use a control associated with the UI thread to asynchronously start flushing the queue
                    mControl.BeginInvoke(new MethodInvoker(this.Consume));
                }
            }
        }

    }
}
