using System;
using System.Collections.Generic;
using System.Text;

namespace DataIO
{
    class clsViewerFeeder: clsThreadedDataConsumer 
    {
        DataIO.ctlMessageViewer mViewer = null;

        public clsViewerFeeder(ctlMessageViewer viewer)
        {
            mViewer = viewer;
        }

        public override void ProcessData(object obj)
        {            
            string str = obj as string;
            if (mViewer != null)
            {
                mViewer.MsgText = str;
            }
        }

    }
}
