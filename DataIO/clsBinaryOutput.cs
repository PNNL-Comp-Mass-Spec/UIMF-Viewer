using System;
using System.IO;

namespace DataIO
{
    public class clsBinaryOutput : clsFileIO
    {
        private BinaryWriter mBinaryWriter = null;

        public override void OpenForWriting(string fPath)
        {
            mBinaryWriter = new BinaryWriter(File.Create(fPath));
            //spin up consuming thread
            base.Start();
            mWritable = true;
        }

        public override void CloseWrite()
        {
            //wait for the queue to flush before closing the file
            base.SoftStop();

            mBinaryWriter.Close();

            mWritable = false;
        }

        public override void ProcessData(object obj)
        {
            double[] d = obj as double[];

            if (d != null)
            {
                WriteDoubleArray(d);
            }
        }

        private void WriteDoubleArray(double[] d)
        {
            int max = d.Length;
            for (int i = 0; i < max; i++)
            {
                mBinaryWriter.Write(d[i]);
            }
        }
    }
}
