/////////////////////////////////////////////////////////////////////////
// This file includes a library of functions to create a UIMF format file
// Author: Yan Shi, PNNL, December 2008
// Modified by: 
//				William F. Danielson				
//				Anuj R. Shah, May 19th 2010
/////////////////////////////////////////////////////////////////////////

using System;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SQLite;

namespace UIMFLibrary
{
	public class DataWriter
	{
		private SQLiteConnection m_dbConn;
		private SQLiteCommand m_dbCmdUIMF;
		private SQLiteCommand m_dbCmdPrepareInsertScan;
		private SQLiteCommand m_dbCmdPrepareInsertFrame;
		private GlobalParameters m_GlobalParameters;

		public void OpenUIMF(string FileName)
		{
            string connectionString = "Data Source = " + FileName + "; Version=3; DateTimeFormat=Ticks;";
			m_dbConn = new SQLiteConnection(connectionString);
			try
			{
				m_dbConn.Open();
				m_dbCmdUIMF = m_dbConn.CreateCommand();
				m_dbCmdUIMF.CommandText = "PRAGMA synchronous=0;BEGIN TRANSACTION;";
				m_dbCmdUIMF.ExecuteNonQuery();
				PrepareInsertFrame();
				PrepareInsertScan();
				
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to open UIMF file " + ex.ToString());
			}
		}

		public bool CloseUIMF(string FileName)
		{
			try
			{
				if (m_dbConn != null)
				{
					m_dbCmdUIMF = m_dbConn.CreateCommand();
					m_dbCmdUIMF.CommandText = "END TRANSACTION;PRAGMA synchronous=1;";
					m_dbCmdUIMF.ExecuteNonQuery();
					m_dbCmdUIMF.Dispose();
					m_dbConn.Close();
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void CreateTables(string DataType)
		{

			// https://prismwiki.pnl.gov/wiki/IMS_Data_Processing

			// Create m_GlobalParameters Table  
			m_dbCmdUIMF = m_dbConn.CreateCommand();
			m_dbCmdUIMF.CommandText = "CREATE TABLE Global_Parameters ( " +
				"DateStarted STRING, " + // date experiment was started
				"NumFrames INT(4) NOT NULL, " + // Number of frames in dataset  
				"TimeOffset INT(4) NOT NULL, " + //  Offset from 0. All bin numbers must be offset by this amount  
				"BinWidth DOUBLE NOT NULL, " + // Width of TOF bins (in ns)  
				"Bins INT(4) NOT NULL, " + // Total TOF bins in a frame
				"TOFCorrectionTime FLOAT NOT NULL, " + //Instrument delay time
				"FrameDataBlobVersion FLOAT NOT NULL, " +// Version of FrameDataBlob in T_Frame  
				"ScanDataBlobVersion FLOAT NOT NULL, " + // Version of ScanInfoBlob in T_Frame  
				"TOFIntensityType TEXT NOT NULL, " + // Data type of intensity in each TOF record (ADC is int/TDC is short/FOLDED is float) 
				"DatasetType TEXT, " + 
			"Prescan_TOFPulses INT(4), " +
				"Prescan_Accumulations INT(4), " +
				"Prescan_TICThreshold INT(4), " +
				"Prescan_Continuous BOOL, " +
				"Prescan_Profile STRING);";
				m_dbCmdUIMF.ExecuteNonQuery();

			// Create Frame_parameters Table
			m_dbCmdUIMF.CommandText = "CREATE TABLE Frame_Parameters (" +
				"FrameNum INT(4) PRIMARY KEY, " + // 0, Contains the frame number 
				"StartTime DOUBLE, " + // 1, Start time of frame  
				"Duration DOUBLE, " + // 2, Duration of frame  
				"Accumulations INT(2), " + // 3, Number of collected and summed acquisitions in a frame 
				"FrameType SHORT, " + // 4, Bitmap: 0=MS (Regular); 1=MS/MS (Frag); 2=Prescan; 4=Multiplex 
				"Scans INT(4), " + // 5, Number of TOF scans  
				"IMFProfile STRING, " + 
				"TOFLosses DOUBLE, " + 
				"AverageTOFLength DOUBLE NOT NULL, " + // 6, Average time between TOF trigger pulses
				"CalibrationSlope DOUBLE, " + // 7, Value of k0  
				"CalibrationIntercept DOUBLE, " + // 8, Value of t0  
				"a2 DOUBLE, "+
				"b2 DOUBLE, "+
				"c2 DOUBLE, "+
				"d2 DOUBLE, "+
				"e2 DOUBLE, "+
				"f2 DOUBLE, "+
				"Temperature DOUBLE, " + // 9, Ambient temperature
				"voltHVRack1 DOUBLE, " + // 10, HVRack Voltage 
				"voltHVRack2 DOUBLE, " + // 11, HVRack Voltage 
				"voltHVRack3 DOUBLE, " + // 12, HVRack Voltage 
				"voltHVRack4 DOUBLE, " + // 13, HVRack Voltage 
				"voltCapInlet DOUBLE, " + // 14, Capilary Inlet Voltage
				"voltEntranceIFTIn DOUBLE, " + // 15, IFT In Voltage
				"voltEntranceIFTOut DOUBLE, " + // 16, IFT Out Voltage
				"voltEntranceCondLmt DOUBLE, " + // 17, Cond Limit Voltage
				"voltTrapOut DOUBLE, " + // 18, Trap Out Voltage
				"voltTrapIn DOUBLE, " + // 19, Trap In Voltage
				"voltJetDist DOUBLE, " +              // 20, Jet Disruptor Voltage
				"voltQuad1 DOUBLE, " +                // 21, Fragmentation Quadrupole Voltage
				"voltCond1 DOUBLE, " +                // 22, Fragmentation Conductance Voltage
				"voltQuad2 DOUBLE, " +                // 23, Fragmentation Quadrupole Voltage
				"voltCond2 DOUBLE, " +                // 24, Fragmentation Conductance Voltage
				"voltIMSOut DOUBLE, " +               // 25, IMS Out Voltage
				"voltExitIFTIn DOUBLE, " +            // 26, IFT In Voltage
				"voltExitIFTOut DOUBLE, " +           // 27, IFT Out Voltage
				"voltExitCondLmt DOUBLE, " +          // 28, Cond Limit Voltage
				"PressureFront DOUBLE, " + // 29, Pressure at front of Drift Tube 
				"PressureBack DOUBLE, " + // 30, Pressure at back of Drift Tube 
				"MPBitOrder INT(1), " + // 31, Determines original size of bit sequence 
				"FragmentationProfile BLOB);"; // Voltage profile used in fragmentation, Length number of Scans 
			m_dbCmdUIMF.ExecuteNonQuery();			    

			// Create Frame_Scans Table
			if (System.String.Equals(DataType, "double"))
			{
				m_dbCmdUIMF.CommandText = "CREATE TABLE Frame_Scans ( " +
					"FrameNum INT(4) NOT NULL, " + //  Contains the frame number
					"ScanNum INT(2) NOT NULL, " + //Scan number
                    "NonZeroCount INT(4) NOT NULL, " +
					"BPI DOUBLE NOT NULL, BPI_MZ DOUBLE NOT NULL, " + // base peak intensity and assocaited mz
					"TIC DOUBLE NOT NULL, " + //  Total Ion Chromatogram
					"Intensities BLOB);"; //  Intensities  
			}
			else if (System.String.Equals(DataType, "float"))
			{
				m_dbCmdUIMF.CommandText = "CREATE TABLE Frame_Scans ( " +
					"FrameNum INT(4) NOT NULL, " + //  Contains the frame number
					"ScanNum INT(2) NOT NULL, " + //Scan number
					"BPI FLOAT NOT NULL, BPI_MZ DOUBLE NOT NULL, " + // base peak intensity and assocaited mz
					"NonZeroCount INT(4) NOT NULL, " + 
					"TIC FLOAT NOT NULL, " + //  Total Ion Chromatogram
					"Intensities BLOB);"; //  Intensities  
			}
			else if (System.String.Equals(DataType, "short"))
			{
				m_dbCmdUIMF.CommandText = "CREATE TABLE Frame_Scans ( " +
					"FrameNum INT(4) NOT NULL, " + //  Contains the frame number
					"ScanNum INT(2) NOT NULL, " + //Scan number
                    "NonZeroCount INT(4) NOT NULL, " + //Non zero count
					"BPI INT(2) NOT NULL, BPI_MZ DOUBLE NOT NULL, " + // base peak intensity and assocaited mz
					"TIC INT(2) NOT NULL, " + //  Total Ion Chromatogram
					"Intensities BLOB);"; //  Intensities  
			}
			else
			{
				m_dbCmdUIMF.CommandText = "CREATE TABLE Frame_Scans ( " +
					"FrameNum INT(4) NOT NULL, " + //  Contains the frame number
					"ScanNum INT(2) NOT NULL, " + //Scan number
                    "NonZeroCount INT(4) NOT NULL, " + //non zero count
					"BPI INT(4) NOT NULL, BPI_MZ DOUBLE NOT NULL, " + // base peak intensity and assocaited mz
					"TIC INT(4) NOT NULL, " + //  Total Ion Chromatogram
					"Intensities BLOB);"; //  Intensities  
			}
			
			//ARS made this change to facilitate faster retrieval of scans/spectrums.
			m_dbCmdUIMF.CommandText += "CREATE UNIQUE INDEX pk_index on Frame_Scans(FrameNum, ScanNum);";
			//ARS change ends

			m_dbCmdUIMF.ExecuteNonQuery();
			m_dbCmdUIMF.Dispose();
		}

		public void InsertGlobal(GlobalParameters header)
		{
            m_GlobalParameters = header;
			m_dbCmdUIMF = m_dbConn.CreateCommand();
			m_dbCmdUIMF.CommandText = "INSERT INTO Global_Parameters " +
				"(DateStarted, NumFrames, TimeOffset, BinWidth, Bins, TOFCorrectionTime, FrameDataBlobVersion, ScanDataBlobVersion, " +
				"TOFIntensityType, DatasetType, Prescan_TOFPulses, Prescan_Accumulations, Prescan_TICThreshold, Prescan_Continuous, Prescan_Profile) " +
				"VALUES(:DateStarted, :NumFrames, :TimeOffset, :BinWidth, :Bins, :TOFCorrectionTime, :FrameDataBlobVersion, :ScanDataBlobVersion, " +
				":TOFIntensityType, :DatasetType, :Prescan_TOFPulses, :Prescan_Accumulations, :Prescan_TICThreshold, :Prescan_Continuous, :Prescan_Profile);";
            
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":DateStarted", header.DateStarted));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":NumFrames", header.NumFrames));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":TimeOffset", header.TimeOffset));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":BinWidth", header.BinWidth));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Bins", header.Bins));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":TOFCorrectionTime", header.TOFCorrectionTime));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":FrameDataBlobVersion", header.FrameDataBlobVersion));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":ScanDataBlobVersion", header.ScanDataBlobVersion));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":TOFIntensityType", header.TOFIntensityType));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":DatasetType", header.DatasetType));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Prescan_TOFPulses", header.Prescan_TOFPulses));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Prescan_Accumulations", header.Prescan_Accumulations));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Prescan_TICThreshold", header.Prescan_TICThreshold));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Prescan_Continuous", header.Prescan_Continuous));
			m_dbCmdUIMF.Parameters.Add(new SQLiteParameter(":Prescan_Profile", header.Prescan_Profile));
            
			m_dbCmdUIMF.ExecuteNonQuery();
			m_dbCmdUIMF.Parameters.Clear();
			m_dbCmdUIMF.Dispose();
		}
		
		//ARS modified insert frame to use a prepared statement instead of reconstructing the query for each and every frame 
		public void InsertFrame(FrameParameters fp)
		{
			if ( m_dbCmdPrepareInsertFrame == null)
			{
				PrepareInsertFrame();
			}

			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":FrameNum", fp.FrameNum));  // 0, Primary Key, Contains the frame number
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":StartTime", fp.StartTime)); // 1, Start time of frame
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":Duration", fp.Duration)); // 2, Duration of frame  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":Accumulations", fp.Accumulations)); // 3, Number of collected and summed acquisitions in a frame 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":FrameType", fp.FrameType)); // 4, Bitmap: 0=MS (Regular); 1=MS/MS (Frag); 2=Prescan; 4=Multiplex 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":Scans", fp.Scans)); // 5, Number of TOF scans  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":IMFProfile", fp.IMFProfile)); 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":TOFLosses", fp.TOFLosses)); 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":AverageTOFLength", fp.AverageTOFLength)); // 6, Average time between TOF trigger pulses
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":CalibrationSlope", fp.CalibrationSlope)); // 7, Value of k0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":CalibrationIntercept", fp.CalibrationIntercept)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":a2", fp.a2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":b2", fp.b2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":c2", fp.c2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":d2", fp.d2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":e2", fp.e2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":f2", fp.f2)); // 8, Value of t0  
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":Temperature", fp.Temperature)); // 9, Ambient temperature
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltHVRack1", fp.voltHVRack1)); // 10, HVRack Voltage 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltHVRack2", fp.voltHVRack2)); // 11, HVRack Voltage 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltHVRack3", fp.voltHVRack3)); // 12, HVRack Voltage 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltHVRack4", fp.voltHVRack4)); // 13, HVRack Voltage 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltCapInlet", fp.voltCapInlet)); // 14, Capilary Inlet Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltEntranceIFTIn", fp.voltEntranceIFTIn)); // 15, IFT In Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltEntranceIFTOut", fp.voltEntranceIFTOut)); // 16, IFT Out Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltEntranceCondLmt", fp.voltEntranceCondLmt)); // 17, Cond Limit Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltTrapOut", fp.voltTrapOut)); // 18, Trap Out Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltTrapIn", fp.voltTrapIn)); // 19, Trap In Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltJetDist", fp.voltJetDist));              // 20, Jet Disruptor Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltQuad1", fp.voltQuad1));                // 21, Fragmentation Quadrupole Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltCond1", fp.voltCond1));                // 22, Fragmentation Conductance Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltQuad2", fp.voltQuad2));                // 23, Fragmentation Quadrupole Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltCond2", fp.voltCond2));                // 24, Fragmentation Conductance Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltIMSOut", fp.voltIMSOut));               // 25, IMS Out Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltExitIFTIn", fp.voltExitIFTIn));            // 26, IFT In Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltExitIFTOut", fp.voltExitIFTOut));           // 27, IFT Out Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":voltExitCondLmt", fp.voltExitCondLmt));          // 28, Cond Limit Voltage
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":PressureFront", fp.PressureFront)); // 29, Pressure at front of Drift Tube 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":PressureBack", fp.PressureBack)); // 30, Pressure at back of Drift Tube 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":MPBitOrder", fp.MPBitOrder)); // 31, Determines original size of bit sequence 
			m_dbCmdPrepareInsertFrame.Parameters.Add(new SQLiteParameter(":FragmentationProfile", blob_FragmentationSequence(fp.FragmentationProfile)));
            
			m_dbCmdPrepareInsertFrame.ExecuteNonQuery();
			m_dbCmdPrepareInsertFrame.Parameters.Clear();
		}

		//This function should be called for each scan, intensities is an array including all zeros
		//TODO:: Deprecate this function since the bpi is calculation using an incorrect calibration function
		public int InsertScan(FrameParameters fp, int scanNum, int counter, double[] intensities, double bin_width)
		{	
			//RLZE - convert 0s to negative multiples as well as calculate TIC and BPI, BPI_MZ
			int nrlze = 0; 
			int zero_count = 0;
			double[] rlze_data = new double[intensities.Length];
			double tic_scan = 0;
			double bpi = 0;
			double bpi_mz = 0;
            int datatypeSize = 8;

			for ( int i = 0; i < intensities.Length; i++)
			{
				double x = intensities[i];
				if (x > 0)
				{
					
					//TIC is just the sum of all intensities
					tic_scan += intensities[i];
					if (intensities[i] > bpi)
					{
						   bpi = intensities[i] ; 
						   bpi_mz = convertBinToMz(i, bin_width, fp);
					}
					if(zero_count < 0)
					{
						rlze_data[nrlze++] = (double)zero_count;
						zero_count = 0;
					}
					rlze_data[nrlze++] = x;
				}
				else zero_count--;
			}

			//Compress intensities
            int nlzf = 0;
            byte[] compressed_record = new byte[nrlze * datatypeSize * 5];
            if (nrlze > 0)
            {
                byte[] byte_array = new byte[nrlze * datatypeSize];
                Buffer.BlockCopy(rlze_data, 0, byte_array, 0, nrlze * datatypeSize);
                nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, nrlze * datatypeSize, ref compressed_record, nrlze * datatypeSize * 5);
            }

            if (nlzf != 0)
            {
                byte[] spectra = new byte[nlzf];
                Array.Copy(compressed_record, spectra, nlzf);

                //Insert records
                insertScanAddParameters(fp.FrameNum, scanNum, counter, (int)bpi, bpi_mz, (int)tic_scan, spectra);
                m_dbCmdPrepareInsertScan.ExecuteNonQuery();
                m_dbCmdPrepareInsertScan.Parameters.Clear();
            }
			return nlzf;
		}

		//This should be the correct signature for the insert scan function
		//public int InsertScan(FrameParameters fp, int scanNum, int[] intensities, double bin_width)
		//{
		//}

		public int InsertScan(FrameParameters fp, int scanNum, int counter, float[] intensities, double bin_width)
		{			
			int nrlze = 0; 
			int zero_count = 0;
			float[] rlze_data = new float[intensities.Length];
			double tic_scan = 0;
			double bpi = 0;
			double bpi_mz = 0;
            int datatypeSize = 4;

			for ( int i = 0; i < intensities.Length; i++)
			{
				float x = intensities[i];
				if (x > 0)
				{
					
					//TIC is just the sum of all intensities
					tic_scan += intensities[i];
					if (intensities[i] > bpi)
					{
						bpi = intensities[i] ; 
						bpi_mz = convertBinToMz(i, bin_width, fp);
					}
					if(zero_count < 0)
					{
						rlze_data[nrlze++] = (float)zero_count;
						zero_count = 0;
					}
					rlze_data[nrlze++] = x;
				}
				else zero_count--;
			}


            int nlzf = 0;
            byte[] compressed_record = new byte[nrlze * datatypeSize * 5];
            if (nrlze > 0)
            {
                byte[] byte_array = new byte[nrlze * datatypeSize];
                Buffer.BlockCopy(rlze_data, 0, byte_array, 0, nrlze * datatypeSize);
                nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, nrlze * datatypeSize, ref compressed_record, nrlze * datatypeSize * 5);
            }

            if (nlzf != 0)
            {
                byte[] spectra = new byte[nlzf];
                Array.Copy(compressed_record, spectra, nlzf);

                //Insert records
                insertScanAddParameters(fp.FrameNum, scanNum, counter, (int)bpi, bpi_mz, (int)tic_scan, spectra);
                m_dbCmdPrepareInsertScan.ExecuteNonQuery();
                m_dbCmdPrepareInsertScan.Parameters.Clear();
            }
			return nlzf;
		}


        public int InsertScan(FrameParameters fp, int scanNum, System.Collections.Generic.List<int> bins, System.Collections.Generic.List<int> intensities, double bin_width, int timeOffset)
        {
            int nonZeroCount = 0;

            if (fp != null)
            {
                if (bins != null && intensities != null && bins.Count != 0 && intensities.Count != 0 && bins.Count == intensities.Count)
                {
                    //that is the total number of datapoints that are to be encoded
                    nonZeroCount = bins.Count;

                    int[] rlze = new int[bins.Count* 2]; //this is the maximum length required assuming that there are no continuous values

                    //now iterate through both arrays and attempt to run length zero encode the values
                    int tic = 0;
                    int bpi = 0;
                    int index = 0;
                    double bpi_mz = 0;
                    int datatypeSize = 4;

                    rlze[index++] = -(timeOffset + bins[0]);
                    for (int i = 0; i < bins.Count ; i++)
                    {
                        //the intensities will always be positive integers
                        tic += intensities[i];
                        if (bpi < intensities[i])
                        {
                            bpi = intensities[i];
                            bpi_mz = convertBinToMz(bins[i], bin_width, fp);
                        }


                        if (i != 0 && bins[i] != bins[i - 1] + 1)
                        {
                            //since the bin numbers are not continuous, add a negative index to the array
                            //and in some cases we have to add the offset from the previous index
                            rlze[index++] = bins[i - 1] - bins[i] + 1;
                        }


                        //copy the intensity value and increment the index.
                        rlze[index++] = intensities[i];
                    }

                    //so now we have a run length zero encoded array
                    byte[] compresedRecord = new byte[index * datatypeSize * 5];
                    byte[] byte_array = new byte[index * datatypeSize];
                    Buffer.BlockCopy(rlze, 0, byte_array, 0, index * datatypeSize);
                    int nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, index * datatypeSize, ref compresedRecord, compresedRecord.Length);
                    byte[] spectra = new byte[nlzf];

                    Array.Copy(compresedRecord, spectra, nlzf);

                    //Insert records
                    if (true)
                    {
                        insertScanAddParameters(fp.FrameNum, scanNum, bins.Count, bpi, bpi_mz, tic, spectra);
                        m_dbCmdPrepareInsertScan.ExecuteNonQuery();
                        m_dbCmdPrepareInsertScan.Parameters.Clear();
                    }

                }

            }

            return nonZeroCount;
            

        }
        //this method takes in a list of bin numbers and intensities and converts them to a run length encoded array
        //which is later compressed at the byte level for reduced size
        public int InsertScan(FrameParameters fp, int scanNum, int [] bins, int[] intensities, double bin_width, int timeOffset)
        {
            int nonZeroCount = 0;

            if (fp != null)
            {
                if (bins != null && intensities != null && bins.Length != 0 && intensities.Length != 0 && bins.Length == intensities.Length)
                {
                    //that is the total number of datapoints that are to be encoded
                    nonZeroCount = bins.Length;

                    int[] rlze = new int[bins.Length * 2]; //this is the maximum length required assuming that there are no continuous values

                    //now iterate through both arrays and attempt to run length zero encode the values
                    int tic = 0;
                    int bpi = 0;
                    int index = 0;
                    double bpi_mz = 0;
                    int datatypeSize = 4;

                    rlze[index++] = -(timeOffset + bins[0]);
                    for (int i = 0; i < bins.Length; i++)
                    {
                        //the intensities will always be positive integers
                        tic += intensities[i];
                        if (bpi < intensities[i])
                        {
                            bpi = intensities[i];
                            bpi_mz = convertBinToMz(bins[i], bin_width, fp);
                        }


                        if (i != 0 && bins[i] != bins[i - 1] + 1)
                        {
                            //since the bin numbers are not continuous, add a negative index to the array
                            //and in some cases we have to add the offset from the previous index
                            rlze[index++] = bins[i - 1] - bins[i] + 1;
                        }
                       

                        //copy the intensity value and increment the index.
                        rlze[index++] = intensities[i]; 
                    }

                    //so now we have a run length zero encoded array
                    byte[] compresedRecord = new byte[index * datatypeSize * 5];
                    byte[] byte_array = new byte[index * datatypeSize];
                    Buffer.BlockCopy(rlze, 0, byte_array, 0, index * datatypeSize);
                    int nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, index * datatypeSize, ref compresedRecord, compresedRecord.Length); 
                    byte[] spectra = new byte[nlzf];

                    Array.Copy(compresedRecord, spectra, nlzf);

                    //Insert records
					if (true)
					{
						insertScanAddParameters(fp.FrameNum, scanNum, bins.Length, bpi, bpi_mz, tic, spectra);
						m_dbCmdPrepareInsertScan.ExecuteNonQuery();
						m_dbCmdPrepareInsertScan.Parameters.Clear();
					}
	
                }
                    
            }

            return nonZeroCount;
        }


        public int InsertScan(FrameParameters fp, int scanNum, int counter, int[] intensities, double bin_width)
		{

			if (fp == null )
			{
				return -1;
			}


			int nrlze = 0; 
			int zero_count = 0;
			int[] rlze_data = new int[intensities.Length];
			int tic_scan = 0;
			int bpi = 0;
			double bpi_mz = 0;
            int datatypeSize = 4;

			//Calculate TIC and BPI
			for ( int i = 0; i < intensities.Length; i++)
			{
				int x = intensities[i];
				if (x > 0)
				{
					//TIC is just the sum of all intensities
					tic_scan += intensities[i];
					if (intensities[i] > bpi)
					{
						bpi = intensities[i] ; 
						bpi_mz = convertBinToMz(i, bin_width, fp);
					}
					if(zero_count < 0)
					{
						rlze_data[nrlze++] = zero_count;
						zero_count = 0;
					}
					rlze_data[nrlze++] = x;
				}
				else zero_count--;
			}

			//Compress intensities
            int nlzf = 0;

            byte[] compresedRecord = new byte[nrlze * datatypeSize * 5];
            if (nrlze > 0)
            {   
                byte[] byte_array = new byte[nrlze * datatypeSize];
                Buffer.BlockCopy(rlze_data, 0, byte_array, 0, nrlze * datatypeSize);
                nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, nrlze * datatypeSize, ref compresedRecord, compresedRecord.Length);
            }

            if (nlzf != 0)
            {
                byte[] spectra = new byte[nlzf];
                Array.Copy(compresedRecord, spectra, nlzf);

                //Insert records
				if (true)
				{
					insertScanAddParameters(fp.FrameNum, scanNum, counter, (int)bpi, bpi_mz, (int)tic_scan, spectra);
					m_dbCmdPrepareInsertScan.ExecuteNonQuery();
					m_dbCmdPrepareInsertScan.Parameters.Clear();
				}
				
            }

			return nlzf;
		}

		public int InsertScan(FrameParameters fp, int scanNum, int counter, short[] intensities, double bin_width)
		{
			int nrlze = 0; 
			int zero_count = 0;
			short[] rlze_data = new short[intensities.Length];
			double tic_scan = 0;
			double bpi = 0;
			double bpi_mz = 0;
			int nonZeroIntensities = 0;
            int datatypeSize = 2;

			//Calculate TIC and BPI
			for ( int i = 0; i < intensities.Length; i++)
			{
				short x = intensities[i];
				if (x > 0)
				{
					
					//TIC is just the sum of all intensities
					tic_scan += intensities[i];
					if (intensities[i] > bpi)
					{
						bpi = intensities[i] ; 
						bpi_mz = convertBinToMz(i, bin_width, fp);
					}
					if(zero_count < 0)
					{
						rlze_data[nrlze++] = (short)zero_count;
						zero_count = 0;
					}
					rlze_data[nrlze++] = x;
					nonZeroIntensities++;
				}
				else zero_count--;
			}

            int nlzf = 0;
            byte[] compressed_record = new byte[nrlze * datatypeSize * 5];
            if (nrlze > 0)
            {
                byte[] byte_array = new byte[nrlze * datatypeSize];
                Buffer.BlockCopy(rlze_data, 0, byte_array, 0, nrlze * datatypeSize);
                nlzf = IMSCOMP_wrapper.compress_lzf(ref byte_array, nrlze * datatypeSize, ref compressed_record, nrlze * datatypeSize * 5);
            }

            if (nlzf != 0)
            {
                byte[] spectra = new byte[nlzf];
                Array.Copy(compressed_record, spectra, nlzf);

                //Insert records
                insertScanAddParameters(fp.FrameNum, scanNum, counter, (int)bpi, bpi_mz, (int)tic_scan, spectra);
                m_dbCmdPrepareInsertScan.ExecuteNonQuery();
                m_dbCmdPrepareInsertScan.Parameters.Clear();
            }

			
			return nlzf;
		}


		public void UpdateCalibrationCoefficients(int frameNum, float slope, float intercept)
		{
			m_dbCmdUIMF = m_dbConn.CreateCommand();
			m_dbCmdUIMF.CommandText = "UPDATE Frame_Parameters SET CalibrationSlope = " + slope.ToString() +
				", CalibrationIntercept = " + intercept + " WHERE FrameNum = " + frameNum;
            
			m_dbCmdUIMF.ExecuteNonQuery();
			m_dbCmdUIMF.Dispose();
		}
		public void AddGlobalParameter(string ParameterName, string ParameterType, string ParameterValue)
		{
			try
			{
				m_dbCmdUIMF = m_dbConn.CreateCommand();
				m_dbCmdUIMF.CommandText = "Alter TABLE m_GlobalParameters Add " + ParameterName.ToString() + " " + ParameterType.ToString();
				m_dbCmdUIMF.CommandText += " UPDATE m_GlobalParameters SET " + ParameterName.ToString() + " = " + ParameterValue;
				this.m_dbCmdUIMF.ExecuteNonQuery();
				m_dbCmdUIMF.Dispose();
			}
			catch
			{
                m_dbCmdUIMF = m_dbConn.CreateCommand();
                m_dbCmdUIMF.CommandText = "UPDATE m_GlobalParameters SET " + ParameterName.ToString() + " = " + ParameterValue;
                this.m_dbCmdUIMF.ExecuteNonQuery();
                m_dbCmdUIMF.Dispose();
                Console.WriteLine("Parameter " + ParameterName + " already exists, its value will be updated to " + ParameterValue);
			}
		}
        public void AddFrameParameter(string paramName, string ParameterType)
		{
			try
			{
				m_dbCmdUIMF = m_dbConn.CreateCommand();
                m_dbCmdUIMF.CommandText = "Alter TABLE Frame_Parameters Add " + paramName.ToString() + " " + ParameterType.ToString();
				this.m_dbCmdUIMF.ExecuteNonQuery();
				m_dbCmdUIMF.Dispose();
			}
			catch
			{
                Console.WriteLine("Parameter " + paramName + " already exists, its value will be updated");
			}
		}
        public void UpdateGlobalParameter(string paramName, string val)
		{
			m_dbCmdUIMF = m_dbConn.CreateCommand();
            m_dbCmdUIMF.CommandText = "UPDATE m_GlobalParameters SET " + paramName.ToString() + " = " + val;
			this.m_dbCmdUIMF.ExecuteNonQuery();
			m_dbCmdUIMF.Dispose();
		}
		public void UpdateFrameParameter(int frameNum, string paramName, string val)
		{
			m_dbCmdUIMF = m_dbConn.CreateCommand();
			m_dbCmdUIMF.CommandText = "UPDATE Frame_Parameters SET " + paramName.ToString() + " = " + val + " WHERE FrameNum = " + frameNum;
			this.m_dbCmdUIMF.ExecuteNonQuery();
			m_dbCmdUIMF.Dispose();
		}

		private void PrepareInsertFrame()
		{
			m_dbCmdPrepareInsertFrame = m_dbConn.CreateCommand();
			m_dbCmdPrepareInsertFrame.CommandText = "INSERT INTO Frame_Parameters " +
				"(FrameNum, StartTime, Duration, Accumulations, FrameType, Scans, IMFProfile, TOFLosses, AverageTOFLength, " +
				"CalibrationSlope, CalibrationIntercept, a2, b2, c2, d2, e2, f2, Temperature, voltHVRack1, voltHVRack2, voltHVRack3, voltHVRack4, " + 
				"voltCapInlet, voltEntranceIFTIn, voltEntranceIFTOut, voltEntranceCondLmt, " + 
				"voltTrapOut, voltTrapIn, voltJetDist, voltQuad1, voltCond1, voltQuad2, voltCond2, " +
				"voltIMSOut, voltExitIFTIn, voltExitIFTOut, voltExitCondLmt, " +
				"PressureFront, PressureBack, MPBitOrder, FragmentationProfile) " +
				"VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,? );";
			m_dbCmdPrepareInsertFrame.Prepare();
		}

		private void PrepareInsertScan()
		{
			//This function should be called before looping through each frame and scan
			m_dbCmdPrepareInsertScan = m_dbConn.CreateCommand();
			m_dbCmdPrepareInsertScan.CommandText = "INSERT INTO Frame_Scans (FrameNum, ScanNum, NonZeroCount, BPI, BPI_MZ, TIC, Intensities) " +
				"VALUES(?,?,?,?,?,?, ?);";
			m_dbCmdPrepareInsertScan.Prepare();
			
		}

		private void insertScanAddParameters(int frameNum, int scanNum, int nonZeroCount, int bpi, double bpi_mz, int tic_scan, byte[]SpectraRecord)
		{
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("FrameNum", frameNum.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("ScanNum", scanNum.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("NonZeroCount", nonZeroCount.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("BPI", bpi.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("BPI_MZ", bpi_mz.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("TIC", tic_scan.ToString()));
			m_dbCmdPrepareInsertScan.Parameters.Add(new SQLiteParameter("Intensities", SpectraRecord));
		}


		private string date_time_string(DateTime dt)
		{
			//Convert DateTime to String yyyy-mm-dd hh:mm:ss
			string dt_string = dt.Year.ToString("0000") + "-" + dt.Month.ToString("00") + "-" + dt.Day.ToString("00") + " " + dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00") + ":" + dt.Second.ToString("00");

			return "'" + dt_string + "'";
		}
		private byte[] blob_FragmentationSequence(double[] frag)
		{
			// convert the fragmentation profile into an array of bytes
			int length_blob = frag.Length;
			byte[] blob_values = new byte[length_blob * 8];

			Buffer.BlockCopy(frag, 0, blob_values, 0, length_blob * 8);

			return blob_values;
		}

		
		private double convertBinToMz( int binNumber, double bin_width, FrameParameters fp)
		{
			double t = binNumber * bin_width/1000;
			double resMassErr = fp.a2*t + fp.b2 * System.Math.Pow(t,3)+ fp.c2 * System.Math.Pow(t,5) + fp.d2 * System.Math.Pow(t,7) + fp.e2 * System.Math.Pow(t,9) + fp.f2 * System.Math.Pow(t,11);
			double mz = (double)(fp.CalibrationSlope * ((double)(t - (double)m_GlobalParameters.TOFCorrectionTime/1000 - fp.CalibrationIntercept)));
			mz = (mz * mz) + resMassErr;
			return mz;
		}

	}
}
