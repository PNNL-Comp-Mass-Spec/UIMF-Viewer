using System;
using System.IO;

namespace UIMFViewer.Utilities
{
    /// <summary>
    /// Summary description for TextExport.
    /// </summary>
    public class TextExport
    {
        public TextExport()
        {
        }

        public void Export(string file_path, string col_header, int[][] Array2D, double[] drift_axis, double[] tof_axis)
        {
            int i;
            int j;
            int width = drift_axis.Length;
            int height = tof_axis.Length;

            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                // Write the data
                fs = new FileStream(file_path, FileMode.Create);
                sw = new StreamWriter(fs);

                sw.Write(col_header);

                // first column of data is the TOF Tic
                // first row of data is the drift Tic
                for (i = 0; i < width; i++)
                    sw.Write(", " + drift_axis[i].ToString("0.0000"));
                sw.Write("\n");

                // dump the data with the first column TOF Tic
                for (i = 0; i < height; i++)
                {
                    sw.Write(tof_axis[i].ToString("0.0000"));
                    for (j = 0; j < width; j++)
                        sw.Write("," + Array2D[j][i]);
                    sw.Write("\n");
                }
                sw.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                if (sw != null)
                    sw.Close();
                if (fs != null)
                    fs.Close();
                throw new Exception(ex.ToString());
            }
        }

        public void Export(string file_path, string col_header, double[][] Array2D, double[] drift_axis, double[] tof_axis)
        {
            int i;
            int j;
            int width = drift_axis.Length;
            int height = tof_axis.Length;

            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                // Write the data
                fs = new FileStream(file_path, FileMode.Create);
                sw = new StreamWriter(fs);

                sw.Write(col_header);

                // first column of data is the TOF Tic
                // first row of data is the drift Tic
                for (i = 0; i < width; i++)
                    sw.Write(", " + drift_axis[i].ToString("0.0000"));
                sw.Write("\n");

                // dump the data with the first column TOF Tic
                for (i = 0; i < height; i++)
                {
                    sw.Write(tof_axis[i].ToString("0.0000"));
                    for (j = 0; j < width; j++)
                        sw.Write("," + Array2D[j][i]);
                    sw.Write("\n");
                }
                sw.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                if (sw != null)
                    sw.Close();
                if (fs != null)
                    fs.Close();
                throw new Exception(ex.ToString());
            }
        }
#if false
        public void Export(string file_path, float [,] Array2D)
        {
            FileStream fs=null;
            StreamWriter sw=null;
            try
            {
                // Write the data
                fs = new FileStream(file_path, FileMode.Create);
                sw = new StreamWriter(fs);
                for(int i=0; i<=Array2D.GetUpperBound(1); i++)
                {
                    int j=0;
                    for(; j<=Array2D.GetUpperBound(0)-1; j++)
                        sw.Write(Array2D[j,i] + ",");
                    sw.Write(Array2D[j,Array2D.GetUpperBound(1)] + "\n");
                }
                sw.Close();
                fs.Close();
            }
            catch(Exception ex)
            {
                if(sw != null)
                    sw.Close();
                if(fs != null)
                    fs.Close();
                throw new Exception(ex.ToString());
            }
        }
#endif
    }
}
