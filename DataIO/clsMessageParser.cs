using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using IDLTools;

/*  
 * Base class for parsing messages from an incoming char stream.
 */


namespace DataIO
{
    public class clsMessageParser: clsDataFeeder
    {
   
        protected string mDelimeter = "\n";
        public string Delimeter
        {
            get { return mDelimeter; }
            set { mDelimeter = value; }
        }
        
        /// <summary>
        /// message stream to this point
        /// </summary>
        protected string mInputStr = ""; 

        protected string ParseMessage()
        {
                int position = mInputStr.IndexOf(mDelimeter);
                if (position >= 0)
                {
                    int clipLength = position + mDelimeter.Length;
                    string tokenStr = mInputStr.Substring(0, position);

                    if (mInputStr.Length >clipLength)
                    {
                        mInputStr = mInputStr.Substring(tokenStr.Length, mInputStr.Length - clipLength);
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
            bool newMessageFound = true; //set to true so we run the loop once

            //build message string by concatenation
            mInputStr += obj as string;

            while (newMessageFound)
            {
                string tokenStr = ParseMessage();

                if (tokenStr == "")
                {
                    newMessageFound = false;
                }
                else
                {
                    FeedOutputList(tokenStr);                    
                }
            }
        }
    }
}
