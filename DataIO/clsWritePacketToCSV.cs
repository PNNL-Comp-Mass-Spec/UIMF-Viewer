using System;

namespace DataIO
{
    public class clsWritePacketToCSV: clsDataPacketIO
    {
        public clsWritePacketToCSV()
        {
            this.Extension = ".csv";
            mChannelDelimeter = ",";
            mCoupletDelimeter = ",";
            mUseTimeStamp = true;
            mUseChannelIndex = false;
        }
    }
}
