using System;
using System.Windows.Forms;
using System.IO;

namespace IonMobility
{
	/// <summary>
	/// Summary description for CReaderFloat.
	/// </summary>
	public class CReaderFloat : CReader
	{

        private const int FLOAT_RECORD_SIZE = 8; // int & float

        public CReaderFloat(IonMobilityProperties.CExperiment experiment)
            : base(experiment)
		{
		}

		// Get entire frame data, add a calibrator at some point
		public void ReadFrame(int frameNumber, out CFrameFloat frame)
		{
			int [] spectraStartByte;
			int [] spectraStartIndex;
			int [] spectraNumRecs;
			double [] spectraTIC;
			double [] TOFValues;
			double [] intensities;
			int dataStartByte;

            //MessageBox.Show("CReaderFloat Readframe");

			FileStream fs;
			if(_data_file != null)
			{
				fs = new FileStream(_data_file, FileMode.Open, FileAccess.Read);
			}
			else
			{
				fs = new FileStream(_experiment.AccumulationFile(frameNumber), FileMode.Open, FileAccess.Read);
			}
			BinaryReader r = new BinaryReader(fs);

			ReadIMFHeader(r, out dataStartByte, out spectraStartByte, out spectraNumRecs, out spectraTIC);
			ReadFrameData(r, spectraStartByte, spectraNumRecs, spectraTIC, 
                out spectraStartIndex, out TOFValues, out intensities);

			frame = new CFrameFloat(frameNumber, spectraStartIndex, spectraNumRecs, 
                spectraTIC, TOFValues, intensities, _experiment);
           //MessageBox.Show("FLOAT: "+frameNumber.ToString()+", "+spectraStartIndex.Length.ToString()+", "+spectraNumRecs.Length.ToString()+", "+spectraTIC.Length.ToString()+", "+TOFValues.Length.ToString()+", "+intensities.Length.ToString()+", "+_experiment.ToString());
			// 1, 620, 620, 620, 10686, 10686

            double sumint = 0;
            int i;
            int j;
            for (i = 0; i < spectraStartIndex.Length; i++)
            {
                sumint = 0;
                for (j = spectraStartIndex[i]; j < spectraStartIndex[i] + spectraNumRecs[i]; j++)
                    sumint += intensities[j];
                //if (sumint > 0)
                //    MessageBox.Show(i.ToString()+"  Sum intensities: " + sumint.ToString());
            }
            r.Close();
			fs.Close();
		}

		protected void ReadIMFHeader( BinaryReader r, 
			out int dataStartByte,
			out int [] spectraStartByte, 
			out int [] spectraNumRecs,
			out double [] spectraTIC )
		{
			// Read past the ESC position
			dataStartByte = GetESCPosition(r) + 1;
			
			// Because of GetESCPosition(), stream should be at loc of the IMF header start
			int numSpectra = r.ReadInt32();
			// numSpectra = 1270;
			int [] spectraLens = new int[numSpectra];
			spectraStartByte = new int[numSpectra];
			spectraNumRecs = new int[numSpectra];
			spectraTIC = new double[numSpectra];

			for(int i=0; i<numSpectra; i++)
			{
				spectraTIC[i] = r.ReadInt32();
				spectraNumRecs[i] = r.ReadInt32() / FLOAT_RECORD_SIZE; 
			}
			
			spectraStartByte[0] = 0;
			for(int i=1; i<numSpectra; i++)
				spectraStartByte[i] = spectraStartByte[i-1] + (spectraNumRecs[i-1] * FLOAT_RECORD_SIZE);
		}

		// Read all of the frame data.
		protected void ReadFrameData( BinaryReader r,
									int [] spectraByteLoc,
									int [] spectraNumRecs,
									double [] spectraTIC,
									out int [] spectraStartIndex,
									out double [] TOFValues,
									out double [] intensities )
		{			
			int bytesToRead;
			int numRecs;
			byte [] binData;
            double bins_TimeOffset = (int)((this._experiment.TimeOffset * 10) / this._experiment.BinWidth);

			bytesToRead = spectraByteLoc[spectraByteLoc.Length-1] + 
				(spectraNumRecs[spectraNumRecs.Length-1]* FLOAT_RECORD_SIZE) -
				spectraByteLoc[0];
			numRecs = bytesToRead / FLOAT_RECORD_SIZE;

			// Should already be in the correct position since ReadIMFHeader was prev. called.
			binData = r.ReadBytes(bytesToRead);
			
			MemoryStream ms = new MemoryStream(binData,0,binData.Length);

			// New binary reader to read the memory stream
			BinaryReader br = new BinaryReader(ms);

			// Prepare arrays for data
			TOFValues = new double[numRecs];
			intensities = new double[numRecs];

			int cnt=0;

			for(int i=0; i<numRecs; i++)
			{
                TOFValues[i] = ((double)br.ReadInt32()) / this._experiment.BinWidth; // wfd
                TOFValues[i] += bins_TimeOffset;

                intensities[i] = br.ReadSingle();
				if(intensities[i]!= 0.0f)
					cnt++;
			}

			Console.WriteLine("Counts: {0}",cnt);
			spectraStartIndex = new int[spectraByteLoc.Length];
			for(int i=0; i<spectraByteLoc.Length; i++)
				spectraStartIndex[i] = (spectraByteLoc[i] - spectraByteLoc[0]) / FLOAT_RECORD_SIZE;

			br.Close();
			ms.Close();
		}		
	}
}





















