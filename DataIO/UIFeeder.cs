using System;
using System.Collections.Generic;
using System.Text;


using System.Windows.Forms;

namespace DataIO
{
    public class clsUIFeeder : clsUnthreadedDataConsumer
    {

        public override void ProcessData(object obj)
        {
            clsDataPacket packet = obj as clsDataPacket;
            if (packet == null) return;

            for (int i = 0; i < packet.elements.Count; i++)
            {
                clsChannel chan = packet.elements[i].channel;
                object data = packet.elements[i].data;

                if (chan != null && chan.VisualizationEnabled && chan.UIDelegate != null)
                {
                    chan.UIDelegate(new clsDataPacket(packet.timestamp, packet.elements[i]));
                }
            }
        }
    }
}
