using System;
using System.IO;

namespace IonMobility
{
	/// <summary>
	/// Write currently displayed frame information to a file for visualization with DX
	/// </summary>
	public class CDXFileWrite
	{
		public CDXFileWrite()
		{
		}

		public void Write(string file_path, int [,] Array2D, int minSpectra, int minTOFValue, int stepSpectra, int stepTOFValue)
		{
			// Write the DX header as a text file.
			FileStream fs = new FileStream(file_path, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			WriteHeader(sw, Array2D.GetUpperBound(0)+1, Array2D.GetUpperBound(1)+1,
				minSpectra, minTOFValue, stepSpectra, stepTOFValue);
			sw.Close();
			fs.Close();

			// Write the data as binary
			fs = new FileStream(file_path, FileMode.Append);			
			BinaryWriter w = new BinaryWriter(fs);
			for(int i=0; i<=Array2D.GetUpperBound(0); i++)
				for(int j=0; j<=Array2D.GetUpperBound(1); j++)
					w.Write(Array2D[i,j]);			
			w.Close();
			fs.Close();
		}

		private void WriteHeader(StreamWriter sw, int sizeSpectra, int sizeTOFValue, int minSpectra, int minTOFValue, int stepSpectra, int stepTOFValue)
		{
			sw.Write("size_spectra = ");
			sw.WriteLine(sizeSpectra);
			sw.Write("size_TOFValue = ");
			sw.WriteLine(sizeTOFValue);
			sw.Write("origin_spectra = ");
			sw.WriteLine(minSpectra);
			sw.Write("origin_TOFValue = ");
			sw.WriteLine(minTOFValue);
			sw.Write("step_spectra = ");
			sw.WriteLine(stepSpectra);
			sw.Write("step_TOFValue = ");
			sw.WriteLine(stepTOFValue);	
			//sw.WriteLine(1);
		}		
	}
}
