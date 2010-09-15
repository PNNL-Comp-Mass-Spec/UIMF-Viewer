using System;
using System.IO;

namespace DataIO
{
    public class clsBinaryInput
    {
        private BinaryReader mReader = null;

        public void OpenForReading(string fPath)
        {
            mReader = new BinaryReader(File.Open(fPath, FileMode.Open));
        }

        public void Close()
        {
            try
            {
                mReader.Close();
            }
            catch { }
        }

        public double[] ReadDoubles(int count)
        {
            try
            {
                double [] d = new double [count];

                for (int i=0; i<count; i++)
                {
                    d[i] = mReader.ReadDouble();
                }

                return d;
            }
            catch {}
            return null;
        }

        public ushort[] ReadUshortTBuffer(int count)
        {
            try
            {
                ushort[] d = new ushort[count];

                for (int i = 0; i < count; i++)
                {
                    d[i] = (ushort)mReader.ReadInt16();
                }

                return d;
            }
            catch { }
            return null;
        }
    }
}
