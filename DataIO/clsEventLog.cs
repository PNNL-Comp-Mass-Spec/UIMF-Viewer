using System;
using System.Collections.Generic;
using System.Text;
using IDLTools;

namespace DataIO
{
    public class clsEventLog: clsDataPacketIO
    {
        private clsDataPacket mPacket = null;

        public clsEventLog()
        {
            this.ChannelDelimeter = ",";
            this.CoupletDelimeter = ",";
            this.UseChannelIndex = false;
            this.UseTimeStamp = true;

            mPacket = new clsDataPacket(DateTime.Now, 
                new clsDataPacketElement(new clsChannel(typeof(string), "ID"), null),
                new clsDataPacketElement(new clsChannel(typeof(string), "event"), null));
        }

        public void PutEvent(string IDStr,  string eventStr)
        {
            if (mFileWriter == null)
            {
                OpenLog();
            }
            mPacket.elements[0].data = IDStr;
            mPacket.elements[1].data = "\"" + eventStr + "\"";
            mPacket.timestamp = DateTime.Now;

            this.ProcessData(mPacket);
        }

        public void OpenLog()
        {
            if (mFileWriter == null)
            {
                string fName = DateTime.Now.ToString();
                fName = fName.Replace("/", "_");
                fName = fName.Replace(":", "_");
                fName = "Event Log " + fName + ".txt";

                if (this.Directory == "")
                {
                    this.Directory = App.HomeDir;
                }
                string fPath = this.Directory + "\\" + fName;
                this.OpenForWriting(fPath);
            }
        }
    }
}
