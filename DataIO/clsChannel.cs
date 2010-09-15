using System;
using System.Collections.Generic;
using IDLTools;

namespace DataIO
{
    public class clsChannelList : clsFileIO
    {
        public List<clsChannel> Channels = new List<clsChannel>();
    }

   public class clsChannel 
    {
        public delegate void ChannelDelegate(clsDataPacket packet);

       //provide a nice general way to call back on the UI thread to process and visiualize data
       private ChannelDelegate mUIDelegate = null;
       public ChannelDelegate UIDelegate
       {
           get { return mUIDelegate; }
           set { mUIDelegate = value; }
       }

        private static int mChannelCount = 0;
       public static int  ChannelCount
        {
            get { return mChannelCount; }
            set { mChannelCount = value; }
        }

        private Type mType = null;
        [Persist]
        public Type ChannelType
        {
            get { return mType; }
            set { mType = value; }
        }

        private int mIndex = 0;
       public int Index
        {
            get { return mIndex; }
            set { mIndex = value; }
        }

        string mID = "";
        [Persist]
        public string ID
        {
            get { return mID; }
            set { mID = value; }
        }
        
        string mFormat = "";
        [Persist]
        public string Format
        {
            get { return mFormat; }
            set { mFormat = value; }
        }

        bool mVisualizationEnabled = false;
        [Persist]
       public bool VisualizationEnabled
        {
            get { return mVisualizationEnabled; }
            set { mVisualizationEnabled = value; }
        }

       public clsChannel() { }

       public clsChannel(Type t, string id)
       {
           mType = t;
           mID = id;
           mIndex = mChannelCount;
           mChannelCount++;
       }

       public clsChannel(Type t, string id, ChannelDelegate uiDelegate)
       {
           mType = t;
           mID = id;
           mUIDelegate = uiDelegate;
           // if we have a ui delegate, enable visualization by default
           VisualizationEnabled = true;
           mIndex = mChannelCount;
           mChannelCount++;
       }

       public clsChannel(int index, Type t, string id, string format)
       {
           mType = t;
           mID = id;
           mFormat = format;
           mIndex = index;
           mChannelCount++;
       }

       public clsChannel(Type t, string id, string format)
       {
           mType = t;
           mID = id;
           mFormat = format;
           mIndex = mChannelCount;
           mChannelCount++;
       }

    }
}
