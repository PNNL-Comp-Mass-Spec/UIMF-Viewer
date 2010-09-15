using System;
using System.Collections.Generic;
using DataIO;
using IDLTools;

namespace DataIO
{
    public class clsDoubleParser: clsDataFeeder
    {

        private  static string mPacketDelimeter =  "\n";
        private static string[] mDataDelimeter = { "," };

        private List<clsChannel> mChannels = new List<clsChannel>();
        public List<clsChannel> Channels
        {
            get { return mChannels; }
            set { mChannels = value; }
        }

        protected string mInputStr = "";

        public static string CreateMessageString(DateTime t, params double[] list)
        {
            string msg = "";

            for (int i=0; i<list.Length; i++)
            {
                msg += list[i].ToString() + mDataDelimeter[0] ;
            }

            msg +=  clsTimeStamp.TransferString(t) + mPacketDelimeter;

            return msg;
        }

        private string ParseMessage()
        {
            int position = mInputStr.IndexOf(mPacketDelimeter);
            if (position >= 0)
            {
                int clipLength = position + mPacketDelimeter.Length;
                string tokenStr = mInputStr.Substring(0, position);

                if (mInputStr.Length > clipLength)
                {
                    mInputStr = mInputStr.Substring(clipLength, mInputStr.Length - clipLength);
                }
                else
                {
                    mInputStr = "";
                }

                return tokenStr;
            }

            return "";
        }

        public override void ProcessData(object obj)
        {
            //build message string by concatenation
            mInputStr += obj as string;

            bool newMessageFound = true; //set to true so we run the loop once

            while (newMessageFound)
            {
                string tokenStr = ParseMessage();

                if (tokenStr == "")
                {
                    newMessageFound = false;
                }
                else
                {
                    try
                    {
                        clsDataPacket packet = new clsDataPacket();

                        string[] splitStr = tokenStr.Split(mDataDelimeter, StringSplitOptions.None);

                        for (int i = 0; i < splitStr.Length - 1; i++)
                        {
                            double d = Convert.ToDouble(splitStr[i]);
                            clsChannel chan = mChannels[i];
                            packet.elements.Add(new clsDataPacketElement(chan, d));
                        }

                        packet.timestamp = Convert.ToDateTime(splitStr[splitStr.Length - 1]);
                        FeedOutputList(packet);
                    }
                    catch
                    {
                        App.Error("Double Parser failed on:  " + tokenStr);
                    }
                }
            }
        }

    }
}
