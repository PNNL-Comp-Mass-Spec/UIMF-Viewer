using System;
using System.Collections.Generic;

namespace DataIO
{
    public class clsDataPacketIO : clsFileIO
    {
        private clsTimeStamp TimeStamp = new clsTimeStamp();

        protected bool mUseChannelIndex = false;
        public bool UseChannelIndex
        {
            get { return mUseChannelIndex; }
            set { mUseChannelIndex = value; }
        }

        protected bool mUseTimeStamp = true;
        public bool UseTimeStamp
        {
            get { return mUseTimeStamp; }
            set { mUseTimeStamp = value; }
        }

        protected string mCoupletDelimeter = ",\t";  //separates the channel index from the data
        public string CoupletDelimeter
        {
            get { return mCoupletDelimeter; }
            set { mCoupletDelimeter = value; }
        }

        protected string mChannelDelimeter = ",\t";  //separates timestamp and channel channel couplets 
        public string ChannelDelimeter
        {
            get { return mChannelDelimeter; }
            set { mChannelDelimeter = value; }
        }
 
        public override void ProcessData(object obj)
        {
            clsDataPacket row = obj as clsDataPacket;
            if (row != null)
            {
                WriteRow(row);
            }
        }

        public override void WriteLine(string line)
        {
            if (mWritable)
            {
                mFileWriter.WriteLine(line);
            }
        }

        public void WriteHeader(clsDataPacket row)
        {
            string line = "";

            if (mUseTimeStamp)
            {
                line = "DATE" + mCoupletDelimeter + "OBSERVATION TIME" + mChannelDelimeter;
            }

            for (int i = 0; i < row.elements.Count; i++)
            {
               line +=  row.elements[i].channel.ID;

                if (i < row.elements.Count - 1)
                {
                    line += mChannelDelimeter;
                }
            }

            mFileWriter.WriteLine(line);

            mLineCount++;
        }

        public virtual void WriteRow(clsDataPacket row)
        {
            if (!mWritable) return;

            string line = "";

            if (mUseTimeStamp)
            {
                TimeStamp.Split(row.timestamp);
                line = TimeStamp.DateString + mCoupletDelimeter + TimeStamp.TimeString + mCoupletDelimeter;
            }

            for (int i = 0; i < row.elements.Count; i++)
            {
                object obj = row.elements[i].data;
                if (mUseChannelIndex)
                {
                    line += row.elements[i].channel.Index.ToString() + mCoupletDelimeter + obj.ToString();
                }
                else
                {
                    line +=  obj.ToString();
                }

                if (i<row.elements.Count -1 )
                {
                    line += mChannelDelimeter;
                }
            }
            
            mFileWriter.WriteLine(line);

            mFileWriter.Flush();

            mLineCount++;
        }

        public void WriteRowAsync(clsDataPacket row)
        {
            if (!mWritable) return;

            base.FeedInput(row);
        }

        public List<object> ReadRow(List<clsChannel> channelList)
        {
            return null;
        }
    }
}
