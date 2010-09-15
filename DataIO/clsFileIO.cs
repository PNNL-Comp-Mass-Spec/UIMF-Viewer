using System;
using System.Windows.Forms;
using System.IO;
using IDLTools;

namespace DataIO
{
    public class clsFileIO: clsThreadedDataConsumer
    {
        //defaults to text input and output
        protected StreamWriter mFileWriter = null;
        protected bool mWritable = false;

        protected StreamReader mFileReader = null;
        protected bool mReadable = false;

        public bool IsOpenForWriting
        {
            get { return mWritable; }
        }

        //only sping consuming thread when writing data
        public clsFileIO()
            : base(false)
        {
        }

        protected int mLineCount = 0;
        public int LineCount
        {
            get { return mLineCount; }
            set { mLineCount = value; }
        }

        protected string mDirectory = "";
        [Persist]
        public string Directory
        {
            get 
            {
                if (mDirectory == "")
                {
                    mDirectory = App.HomeDir;
                }
                return mDirectory; 
            }
            set { mDirectory = value; }
        }

        protected string mFilename = "file";
        [Persist]
        public string Filename
        {
            get { return mFilename; }
            set { mFilename = value; }
        }

        protected string mExtension = ".txt";
        [Persist]
        public string Extension
        {
            get { return mExtension; }
            set { mExtension = value; }
        }

        //default is to write channel index with data. 
        private bool mWriteChannelIndex = true;
        public bool WriteChannelIndex
        {
            get { return mWriteChannelIndex; }
            set { mWriteChannelIndex = value; }
        }

        public void BrowseDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

             dialog.RootFolder = Environment.SpecialFolder.Desktop;
             dialog.SelectedPath = Directory;

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                Directory = dialog.SelectedPath;
            }
        }

        public void BrowseOpenFilename()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.DefaultExt = mExtension;
            dialog.InitialDirectory = Directory;

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                this.Filename = dialog.FileName;
            }
            else
            {
                this.Filename = "";
            }
        }

        public void BrowseOpen()
        {
            BrowseOpenFilename();
            if (this.Filename != "")
            {
                OpenForReading(this.Filename);
            }
        }

        public void BrowseSaveFilename()
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.DefaultExt = mExtension;
            dialog.InitialDirectory = Directory;

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                this.Filename = dialog.FileName;
            }
            else
            {
                this.Filename = "";
            }
        }

        public void BrowseSave()
        {
            BrowseSaveFilename();

            if (this.Filename != "")
            {
                OpenForWriting(this.Filename);
            }
        }


        public virtual void OpenForWriting(string fPath)
        {
                mFileWriter = new StreamWriter(fPath);
                mLineCount = 0;

                //spin up consuming thread
                base.Start();
                mWritable = true;
        }

        public virtual void WriteLine(string line)
        {
            if (mWritable)
            {
                mFileWriter.WriteLine(line);
            }
        }

        public virtual void CloseWrite()
        {
            //wait for the queue to flush before closing the file
            base.SoftStop();

            mFileWriter.Close();

            mWritable = false;
        }

        public virtual void OpenForReading(string fPath)
        {
            try
            {
                mFileReader = new StreamReader(fPath);
                mReadable = true;
            }
            catch
            {
                mReadable = false;
                throw (new Exception("Unable to open:  " + fPath));
            }
        }

        public string ReadLine()
        {
            if (!mReadable)
            {
                return null;
            }
            string line = mFileReader.ReadLine();
            if (line == null)
            {
                CloseRead();
            }
            return line;
        }

        public string ReadAll()
        {
            if (!mReadable)
            {
                return null;
            }

            string line = mFileReader.ReadToEnd();
            return line;
        }


        public virtual void CloseRead()
        {
            mReadable = false;
            mFileReader.Close();
        }

        public  string GenerateFilePath()
        {
            return (mDirectory + "\\" + mFilename + mExtension);
        }

        public  string GenerateTimestampedFilePath()
        {
            mFilename = clsTimeStamp.TransferString(DateTime.Now);
            mFilename = mFilename.Replace("/", "_");
            mFilename = mFilename.Replace(":", "_");
            return this.GenerateFilePath();
        }

        public string GenerateTimestampedFilePath(string prefix)
        {
            mFilename = clsTimeStamp.TransferString(DateTime.Now);
            mFilename = mFilename.Replace("/", "_");
            mFilename = mFilename.Replace(":", "_");
            mFilename = prefix + mFilename;
            return this.GenerateFilePath();
        }

        public string GenerateTimestampedFilePath(DateTime dt, string prefix)
        {
            mFilename = clsTimeStamp.TransferString(dt);
            mFilename = mFilename.Replace("/", "_");
            mFilename = mFilename.Replace(":", "_");
            mFilename = prefix + mFilename;
            return this.GenerateFilePath();
        }

        public override void ProcessData(object obj)
        {
            string line = obj as string;
            if (line != null)
            {
                WriteLine(line);
            }
        }
      
    }
}
