using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using IDLTools;

namespace DataIO
{
    class clsFIOProperties
    {
        [Persist]
        private string mDirectory = "C:\\";
        public string Directory 
        {
            get { return mDirectory; }
            set { mDirectory = value; }
        }

        [Persist]
        private string mFilename = "file";
        public string Filename
        {
            get { return mFilename; }
            set { mFilename = value; }
        }

        [Persist]
        private string mExtension = ".txt";
        public string Extension
        {
            get { return mExtension; }
            set { mExtension = value; }
        }
    }
}
