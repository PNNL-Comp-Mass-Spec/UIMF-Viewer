using System;
using System.Collections;

namespace DataIO
{
    [Serializable]
    public class clsDataConsumer
    {
        protected Queue mQ = new Queue();

        public virtual void ProcessData(object obj)
        {
        }

        //since we may be adding data faster than we are consuming it
        public void Flush()
        {
            lock (mQ.SyncRoot)
            {
                while (mQ.Count > 0)
                {
                    object obj = mQ.Dequeue();
                    ProcessData(obj);
                }
            }
        }

        public int QCount
        {
            get
            {
                int count = -1;
                lock (mQ.SyncRoot)
                {
                    count = mQ.Count;
                }
                return count;
            }
        }

        public virtual void FeedInput(object obj)
        {
        }

    }
}
