using System;
using System.Collections.Generic;
using System.Text;

namespace DataIO
{
    public class clsDoubleBuffer: clsMessageFeeder
    {

        //double buffer incoming data so that we can parse process data one cycle at a time.
        List<clsDataPacket> mRunningBuffer = new List<clsDataPacket>();
        List<clsDataPacket> mReadyBuffer = new List<clsDataPacket>();

        DateTime mLatchTime = DateTime.Now;
        public DateTime LatchTime
        {
            get {
                DateTime dt;

                lock (mReadyBuffer)
                {
                    dt = mLatchTime;
                }

                return dt;
            }
            set 
            {
                lock (mReadyBuffer)
                {
                    mLatchTime = value;
                }               
            }
        }

        bool mBufferStarted = false;
        private bool mBufferReady = false;
        public bool BufferReady
        {
            get
            {
                bool ready;

                lock (mReadyBuffer)
                {
                    ready = mBufferReady;
                }

                return ready;
            }
            set
            {
                lock (mReadyBuffer)
                {
                    mBufferReady = value;
                }
            }
        }

        public clsDoubleBuffer()
        {
        }

        // the goal here is to to latch data to the ready buffer while still being responsive to 
        // incoming data
        public override void ProcessData(object obj)
        {
           clsDataPacket dp =  obj as clsDataPacket;

            lock (mRunningBuffer)
            {
                mRunningBuffer.Add(dp);

                // if we are starting to latch the buffer, we need to clear the ready buffer
                if (mRunningBuffer[0].timestamp < mLatchTime && mBufferStarted == false)
                {
                    mBufferStarted = true;
                    mReadyBuffer.Clear();
                }

                while (mRunningBuffer[0].timestamp < mLatchTime )
                {              
                       dp = mRunningBuffer[0];
                       mRunningBuffer.RemoveAt(0);
                       mReadyBuffer.Add(dp);
                       BufferReady = false;
                }

                if (mRunningBuffer.Count > 0)
                {
                    BufferReady = true;
                    mBufferStarted = false;
                }
            }
        }

        public void Flush()
        {
            if (mBufferReady != true) return;

            lock (mReadyBuffer)
            {
                for (int i = 0; i < mReadyBuffer.Count; i++)
                {
                    FeedOutputList(mReadyBuffer[i]);
                }

                mReadyBuffer.Clear();
            }
        }

    }
}
