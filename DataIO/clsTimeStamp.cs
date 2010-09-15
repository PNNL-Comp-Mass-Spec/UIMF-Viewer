using System;
using System.Globalization; 

namespace DataIO
{
    public class clsTimeStamp
    {
        private static string[] mDataDelimeter = { " " };

        public static string TransferString(DateTime dt)
        {
            return (dt.ToString("G", DateTimeFormatInfo.InvariantInfo) + "." + dt.Millisecond.ToString("000"));
        }

        private string mDateString = "";
        public string DateString
        {
            get { return mDateString; }
        }
        
        private string mTimeString = "";
        public string TimeString
        {
            get { return mTimeString; }
        }

        public void Split(DateTime dt)
        {
            string timeStr = TransferString(dt);
            string[] splitStr = timeStr.Split(mDataDelimeter, StringSplitOptions.None);
            mDateString = splitStr[0];
            mTimeString = splitStr[1];
        }
    }
}
