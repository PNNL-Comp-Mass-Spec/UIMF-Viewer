using System;
using System.Collections.Generic;

namespace DataIO
{
    public class clsDataPacketElement
    {
        public clsChannel channel;
        public object data;

        public clsDataPacketElement(clsChannel chan, object dat)
        {
            channel = chan;
            data = dat;
        }
    }

    public class clsDataPacket
    {
        public DateTime timestamp;

        public List<clsDataPacketElement> elements = new List<clsDataPacketElement>();

        public clsDataPacket()
        {
        }

        public clsDataPacket (DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public clsDataPacket (DateTime timestamp, params clsDataPacketElement[] data)
        {
            this.timestamp = timestamp;

            for (int i=0; i<data.Length; i++)
            {
                elements.Add(data[i]);
            }
        }
    }
}
