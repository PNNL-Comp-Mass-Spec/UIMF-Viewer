/////////////////////////////////////////////////////////////////////////////////////
// This file includes a library of functions to retrieve data from a UIMF format file
// Author: Yan Shi, PNNL, December 2008
// Updates by:
//          Anuj Shaw
//          William Danielson
//          Yan Shi
//          Gordon Slysz
//          Matthew Monroe
//			Kevin Crowell
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Lzf;

namespace UIMFLibrary
{
    public class DataReader : IDisposable
    {
        #region "Constants and Enums"
            public enum FrameType
            {
                MS1 = 1,
                MS2 = 2,
                Calibration = 3,
                Prescan = 4
            }

			public enum ToleranceType
			{
				PPM = 1,
				Thomson = 2
			}

            private const int DATASIZE = 4; //All intensities are stored as 4 byte quantities

            // No longer used: private const int MAXMZ = 5000;

            private const string BPI = "BPI";
            private const string TIC = "TIC";

        #endregion

        #region "Class-wide variables"

            protected SQLiteConnection m_uimfDatabaseConnection;
			protected SQLiteDataReader m_sqliteDataReader;

            // v1.2 prepared statements
			private SQLiteCommand m_getCountPerSpectrumCommand;
			private SQLiteCommand m_getCountPerFrameCommand;
			private SQLiteCommand m_getFileBytesCommand;
			private SQLiteCommand m_getFrameNumbers;
			private SQLiteCommand m_getFrameParametersCommand;
			private SQLiteCommand m_getFramesAndScanByDescendingIntensityCommand;
			private SQLiteCommand m_getSpectrumCommand;
			private SQLiteCommand m_sumVariableScansPerFrameCommand;
			protected SQLiteCommand m_preparedStatement;
			private SQLiteCommand m_checkForBinCentricTableCommand;
			private SQLiteCommand m_getBinDataCommand;

			protected FrameParameters[] m_frameParametersCache;
            protected GlobalParameters m_globalParameters;
            private double[] m_calibrationTable;
			private string m_uimfFilePath;
    		private bool m_doesContainBinCentricData;

    		private int m_frameTypeMs;

			private static int m_errMessageCounter;

    		private List<SpectrumCache> m_spectrumCacheList;
    		private IDictionary<FrameType, FrameTypeInfo> m_frameTypeInfo;

        #endregion

        #region "Properties"

        public bool PressureIsMilliTorr { get; set; }


		public double TenthsOfNanoSecondsPerBin
			{
				get { return m_globalParameters.BinWidth * 10.0; }
			}

		public string UimfFilePath { get { return m_uimfFilePath; } }

        #endregion

		public DataReader(string fileName)
		{
			m_errMessageCounter = 0;
			m_calibrationTable = new double[0];
			m_spectrumCacheList = new List<SpectrumCache>();
			m_frameTypeInfo = new Dictionary<FrameType, FrameTypeInfo>();

            FileSystemInfo uimfFileInfo = new FileInfo(fileName);

			if (uimfFileInfo.Exists)
			{
				string connectionString = "Data Source = " + uimfFileInfo.FullName + "; Version=3; DateTimeFormat=Ticks;";
				m_uimfDatabaseConnection = new SQLiteConnection(connectionString);

				try
				{
					m_uimfDatabaseConnection.Open();
					m_uimfFilePath = uimfFileInfo.FullName;

					// Populate the global parameters object
					m_globalParameters = GetGlobalParametersFromTable(m_uimfDatabaseConnection);

					// Initialize the frame parameters cache
					m_frameParametersCache = new FrameParameters[m_globalParameters.NumFrames + 1];

					LoadPrepStmts();

					// Lookup whether the pressure columns are in torr or mTorr
					DeterminePressureUnits();

					// Find out if the MS1 Frames are labeled as 0 or 1.
					DetermineFrameTypes();

					// Discover and store info about each frame type
					FillOutFrameInfo();

					m_doesContainBinCentricData = DoesContainBinCentricData();
				}
				catch (Exception ex)
				{
					throw new Exception("Failed to open UIMF file: " + ex);
				}
			}
			else
			{
				throw new FileNotFoundException("UIMF file not found: " + uimfFileInfo.FullName);
			}
		}

		public int[][] AccumulateFrameData(int frameNumber, bool flagTOF, int startScan, int startBin, int minMZBin, int maxMZBin, int[][] frameData, int yCompression)
		{
			int dataWidth = frameData.Length;
			int dataHeight = frameData[0].Length;

			byte[] compressedBinIntensity;
			byte[] streamBinIntensity = new byte[m_globalParameters.Bins * 4];
			int endBin;

			if (yCompression > 0)
			{
				endBin = startBin + (dataHeight*yCompression);
			}
			else if (yCompression < 0)
			{
				endBin = startBin + dataHeight - 1;
			}
			else
			{
				throw new Exception("UIMFLibrary accumulate_PlotData: Compression == 0");
			}

			// Create a calibration lookup table -- for speed
			m_calibrationTable = new double[dataHeight];
			if (flagTOF)
			{
				for (int i = 0; i < dataHeight; i++)
				{
					m_calibrationTable[i] = startBin + (i * (double) (endBin - startBin) / dataHeight);
				}
			}
			else
			{
				FrameParameters frameparameters = GetFrameParameters(frameNumber);
				MZ_Calibrator mzCalibrator = GetMzCalibrator(frameparameters);

				double mzMin = mzCalibrator.TOFtoMZ((float)((startBin / m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));
				double mzMax = mzCalibrator.TOFtoMZ((float)((endBin / m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));

				for (int i = 0; i < dataHeight; i++)
				{
					m_calibrationTable[i] = mzCalibrator.MZtoTOF(mzMin + (i * (mzMax - mzMin) / dataHeight)) * m_globalParameters.BinWidth / TenthsOfNanoSecondsPerBin;
				}
			}

			// This function extracts intensities from selected scans and bins in a single frame 
			// and returns a two-dimensional array intensities[scan][bin]
			// frameNum is mandatory and all other arguments are optional
			m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
			m_preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + frameNumber + " AND ScanNum >= " + startScan + " AND ScanNum <= " + (startScan + dataWidth - 1);

			m_sqliteDataReader = m_preparedStatement.ExecuteReader();
			m_preparedStatement.Dispose();

			// accumulate the data into the plot_data
			if (yCompression < 0)
			{
				//MessageBox.Show(start_bin.ToString() + " " + end_bin.ToString());

				for (int scansData = 0; ((scansData < dataWidth) && m_sqliteDataReader.Read()); scansData++)
				{
					int currentScan = Convert.ToInt32(m_sqliteDataReader["ScanNum"]) - startScan;
					compressedBinIntensity = (byte[])(m_sqliteDataReader["Intensities"]);

					if (compressedBinIntensity.Length == 0)
						continue;

					int indexCurrentBin = 0;
					int decompressLength = LZFCompressionUtil.Decompress(ref compressedBinIntensity, compressedBinIntensity.Length, ref streamBinIntensity, m_globalParameters.Bins * 4);

					for (int binData = 0; (binData < decompressLength) && (indexCurrentBin <= endBin); binData += 4)
					{
						int intBinIntensity = BitConverter.ToInt32(streamBinIntensity, binData);

						if (intBinIntensity < 0)
						{
							indexCurrentBin += -intBinIntensity;   // concurrent zeros
						}
						else if ((indexCurrentBin < minMZBin) || (indexCurrentBin < startBin))
							indexCurrentBin++;
						else if (indexCurrentBin > maxMZBin)
							break;
						else
						{
							frameData[currentScan][indexCurrentBin - startBin] += intBinIntensity;
							indexCurrentBin++;
						}
					}
				}
			}
			else    // each pixel accumulates more than 1 bin of data
			{
				for (int scansData = 0; ((scansData < dataWidth) && m_sqliteDataReader.Read()); scansData++)
				{
					int currentScan = Convert.ToInt32(m_sqliteDataReader["ScanNum"]) - startScan;
					// if (current_scan >= data_width)
					//     break;

					compressedBinIntensity = (byte[])(m_sqliteDataReader["Intensities"]);

					if (compressedBinIntensity.Length == 0)
						continue;

					int indexCurrentBin = 0;
					int decompressLength = LZFCompressionUtil.Decompress(ref compressedBinIntensity, compressedBinIntensity.Length, ref streamBinIntensity, m_globalParameters.Bins * 4);

					int pixelY = 1;

					for (int binValue = 0; (binValue < decompressLength) && (indexCurrentBin < endBin); binValue += 4)
					{
						int intBinIntensity = BitConverter.ToInt32(streamBinIntensity, binValue);

						if (intBinIntensity < 0)
						{
							indexCurrentBin += -intBinIntensity; // concurrent zeros
						}
						else if ((indexCurrentBin < minMZBin) || (indexCurrentBin < startBin))
						{
							indexCurrentBin++;
						}
						else if (indexCurrentBin > maxMZBin)
						{
							break;
						}
						else
						{
							double calibratedBin = indexCurrentBin;

							for (int i = pixelY; i < dataHeight; i++)
							{
								if (m_calibrationTable[i] > calibratedBin)
								{
									pixelY = i;
									frameData[currentScan][pixelY] += intBinIntensity;
									break;
								}
							}
							indexCurrentBin++;
						}
					}
				}
			}

			m_sqliteDataReader.Close();
			return frameData;
		}

    	/// <summary>
    	/// Clones this database, but doesn't copy data in tables sTablesToSkipCopyingData.
    	/// If a table is skipped, data will still copy for the frame types specified in eFrameScanFrameTypeDataToAlwaysCopy.
    	/// </summary>
    	/// <param name="targetDBPath">The desired path of the newly cloned UIMF file.</param>
    	/// <param name="tablesToSkip">A list of table names (e.g. Frame_Scans) that should not be copied.</param>
		/// <param name="frameTypesToAlwaysCopy">
		/// A list of FrameTypes that should ALWAYS be copied. 
		///		e.g. If "Frame_Scans" is passed into tablesToSkip, data will still be inserted into "Frame_Scans" for these Frame Types.
		/// </param>
    	/// <returns>True if success, false if a problem</returns>
    	public bool CloneUIMF(string targetDBPath, List<string> tablesToSkip, List<FrameType> frameTypesToAlwaysCopy)
        {
            string sCurrentTable = string.Empty;
			
            try
            {
                // Get list of tables in source DB
                Dictionary<string, string> dctTableInfo = CloneUIMFGetObjects("table");

                // Delete the "sqlite_sequence" database from dctTableInfo if present
				if (dctTableInfo.ContainsKey("sqlite_sequence"))
				{
					dctTableInfo.Remove("sqlite_sequence");
				}

            	// Get list of indices in source DB
                Dictionary<string, string> dctIndexInfo = CloneUIMFGetObjects("index");

				if (File.Exists(targetDBPath))
				{
					File.Delete(targetDBPath);
				}

            	try
                {
                    string sTargetConnectionString = "Data Source = " + targetDBPath + "; Version=3; DateTimeFormat=Ticks;";
                    SQLiteConnection cnTargetDB = new SQLiteConnection(sTargetConnectionString);
	           
		            cnTargetDB.Open();
                    SQLiteCommand cmdTargetDB = cnTargetDB.CreateCommand();
                        
                    // Create each table
					foreach (KeyValuePair<string, string> kvp in dctTableInfo)
                	{
						if (!String.IsNullOrEmpty(kvp.Value))
						{
							sCurrentTable = string.Copy(kvp.Key);
							cmdTargetDB.CommandText = kvp.Value;
							cmdTargetDB.ExecuteNonQuery();
						}
                	}

					foreach (KeyValuePair<string, string> kvp in dctIndexInfo)
					{
						if (!String.IsNullOrEmpty(kvp.Value))
						{
							sCurrentTable = kvp.Key + " (create index)";
							cmdTargetDB.CommandText = kvp.Value;
							cmdTargetDB.ExecuteNonQuery();
						}
					}

                    try
                    {
                        cmdTargetDB.CommandText = "ATTACH DATABASE '" + m_uimfFilePath + "' AS SourceDB;";
                        cmdTargetDB.ExecuteNonQuery();

                        // Populate each table
						foreach (KeyValuePair<string, string> kvp in dctTableInfo)
						{
							sCurrentTable = string.Copy(kvp.Key);

                            if (!tablesToSkip.Contains(sCurrentTable))
                            {
                                string sSql = "INSERT INTO main." + sCurrentTable + " SELECT * FROM SourceDB." + sCurrentTable + ";";

                                cmdTargetDB.CommandText = sSql;
                                cmdTargetDB.ExecuteNonQuery();
                            }
                            else
                            {
                                if (sCurrentTable.ToLower() == "Frame_Scans".ToLower() && 
                                    frameTypesToAlwaysCopy != null && 
                                    frameTypesToAlwaysCopy.Count > 0)
                                {
                                    // Explicitly copy data for the frame types defined in eFrameScanFrameTypeDataToAlwaysCopy
                                    for (int i = 0; i < frameTypesToAlwaysCopy.Count; i++)
                                    {
                                        string sSql = "INSERT INTO main." + sCurrentTable + 
                                                      " SELECT * FROM SourceDB." + sCurrentTable +
                                                      " WHERE FrameNum IN (SELECT FrameNum FROM Frame_Parameters " +
													  "WHERE FrameType = " + (frameTypesToAlwaysCopy[i].Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameTypesToAlwaysCopy[i]) + ");";

                                        cmdTargetDB.CommandText = sSql;
                                        cmdTargetDB.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        sCurrentTable = "(DETACH DATABASE)";

                        // Detach the source DB
                        cmdTargetDB.CommandText = "DETACH DATABASE 'SourceDB';";
                        cmdTargetDB.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error copying data into cloned database, table " + sCurrentTable, ex);
                    }

                    cmdTargetDB.Dispose();
                    cnTargetDB.Close();

                }
                catch (Exception ex)
                {
                    throw new Exception("Error initializing cloned database, table " + sCurrentTable, ex);
                }
                      
            }
            catch (Exception ex)
            {
                throw new Exception("Error cloning database: " + ex.Message, ex);
            }

            return true;
        }

		public static double ConvertBinToMZ(double slope, double intercept, double binWidth, double correctionTimeForTOF, int bin)
		{
			double t = bin * binWidth / 1000;
			double term1 = slope * ((t - correctionTimeForTOF / 1000 - intercept));
			return term1 * term1;
		}

        public string FrameTypeDescription(FrameType frameType)
        {
            switch (frameType)
            {
				case FrameType.MS1:
                    return "MS";
				case FrameType.MS2:
                    return "MS/MS";
				case FrameType.Calibration:
                    return "Calibration";
				case FrameType.Prescan:
                    return "Prescan";
                default:
                    throw new InvalidCastException("Invalid frame type: " + frameType);
            }
        }

		/// <summary>
		/// Returns the x,y,z arrays needed for a surface plot for the elution of the species in both the LC and drifttime dimensions
		/// </summary>
		/// <param name="startFrameNumber"></param>
		/// <param name="endFrameNumber"></param>
		/// <param name="frameType"></param>
		/// <param name="startScan"></param>
		/// <param name="endScan"></param>
		/// <param name="targetMZ"></param>
		/// <param name="toleranceInMZ"></param>
		/// <param name="frameValues"></param>
		/// <param name="scanValues"></param>
		/// <param name="intensities"></param>
		public void Get3DElutionProfile(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScan, int endScan, double targetMZ, double toleranceInMZ, out int[] frameValues, out int[] scanValues, out int[] intensities)
		{

			if ((startFrameNumber > endFrameNumber) || (startFrameNumber < 0))
			{
				throw new ArgumentException("Failed to get LCProfile. Input startFrame was greater than input endFrame. start_frame=" + startFrameNumber.ToString() + ", end_frame=" + endFrameNumber.ToString());
			}

			if (startScan > endScan)
			{
				throw new ArgumentException("Failed to get 3D profile. Input startScan was greater than input endScan");
			}

			int lengthOfOutputArrays = (endFrameNumber - startFrameNumber + 1) * (endScan - startScan + 1);

			frameValues = new int[lengthOfOutputArrays];
			scanValues = new int[lengthOfOutputArrays];
			intensities = new int[lengthOfOutputArrays];


			int[] lowerUpperBins = GetUpperLowerBinsFromMz(startFrameNumber, targetMZ, toleranceInMZ);

			int[][][] frameIntensities = GetIntensityBlock(startFrameNumber, endFrameNumber, frameType, startScan, endScan, lowerUpperBins[0], lowerUpperBins[1]);

			int counter = 0;

			for (int frameNumber = startFrameNumber; frameNumber <= endFrameNumber; frameNumber++)
			{
				for (int scan = startScan; scan <= endScan; scan++)
				{
					int sumAcrossBins = 0;
					for (int bin = lowerUpperBins[0]; bin <= lowerUpperBins[1]; bin++)
					{
						int binIntensity = frameIntensities[frameNumber - startFrameNumber][scan - startScan][bin - lowerUpperBins[0]];
						sumAcrossBins += binIntensity;
					}
					frameValues[counter] = frameNumber;
					scanValues[counter] = scan;
					intensities[counter] = sumAcrossBins;
					counter++;
				}
			}
		}

        /// <summary>
        /// Extracts BPI from startFrame to endFrame and startScan to endScan and returns an array
        /// </summary>
        /// <param name="frameType"></param>
        /// <param name="startFrameNumber"></param>
        /// <param name="endFrameNumber"></param>
        /// <param name="startScan"></param>
        /// <param name="endScan"></param>
        public double[] GetBPI(FrameType frameType, int startFrameNumber, int endFrameNumber, int startScan, int endScan)
        {
            return GetTicOrBpi(frameType, startFrameNumber, endFrameNumber, startScan, endScan, BPI);
        }

		/// <summary>
		/// Extracts BPI from startFrame to endFrame and startScan to endScan and returns a dictionary for all frames
		/// </summary>
		/// <param name="startFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="endFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="startScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="endScan">If startScan and endScan are 0, then uses all scans</param>
		/// <returns>Dictionary where keys are frame number and values are the BPI value</returns>
		public Dictionary<int, double> GetBPIByFrame(int startFrameNumber, int endFrameNumber, int startScan, int endScan)
		{
			return GetTicOrBpiByFrame(startFrameNumber, endFrameNumber, startScan, endScan, BPI, filterByFrameType: false, frameType: FrameType.MS1);
		}

		/// <summary>
		/// Extracts BPI from startFrame to endFrame and startScan to endScan and returns a dictionary of the specified frame type
		/// </summary>
		/// <param name="startFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="endFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="startScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="endScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="frameType">FrameType to return</param>
		/// <returns>Dictionary where keys are frame number and values are the BPI value</returns>
		public Dictionary<int, double> GetBPIByFrame(int startFrameNumber, int endFrameNumber, int startScan, int endScan, FrameType frameType)
		{
			return GetTicOrBpiByFrame(startFrameNumber, endFrameNumber, startScan, endScan, BPI, filterByFrameType: true, frameType: frameType);
		}

        public List<string> GetCalibrationTableNames()
        {
        	SQLiteCommand cmd = new SQLiteCommand(m_uimfDatabaseConnection) {CommandText = "SELECT NAME FROM Sqlite_master WHERE type='table' ORDER BY NAME"};
        	List<string> calibrationTableNames = new List<string>();
            try
            {
				using (SQLiteDataReader reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						string tableName = Convert.ToString(reader["Name"]);
						if (tableName.StartsWith("Calib_"))
						{
							calibrationTableNames.Add(tableName);
						}
					}
				}
            }
            catch (Exception ex)
            {
                throw new Exception("Exception finding calibration table names: " + ex);
            }

            return calibrationTableNames;
        }

        public int GetCountPerFrame(int frameNumber)
        {
            int countPerFrame = 0;
			m_getCountPerFrameCommand.Parameters.Add(new SQLiteParameter(":FrameNum", frameNumber));

            try
            {
                SQLiteDataReader reader = m_getCountPerFrameCommand.ExecuteReader();
                while (reader.Read())
                {
					countPerFrame = reader.IsDBNull(0) ? 1 : Convert.ToInt32(reader[0]);
                }
                m_getCountPerFrameCommand.Parameters.Clear();
                reader.Dispose();
                reader.Close();
            }
            catch (Exception ex)
            {
				Console.WriteLine("Exception looking up sum(nonzerocount) for frame " + frameNumber + ": " + ex.Message);
                countPerFrame = 1;
            }

            return countPerFrame;
        }

		public void GetDriftTimeProfile(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScan, int endScan, double targetMZ, double toleranceInMZ, ref int[] imsScanValues, ref int[] intensities)
		{
			if ((startFrameNumber > endFrameNumber) || (startFrameNumber < 0))
			{
				throw new ArgumentException("Failed to get DriftTime profile. Input startFrame was greater than input endFrame. start_frame=" + startFrameNumber.ToString() + ", end_frame=" + endFrameNumber.ToString());
			}

			if ((startScan > endScan) || (startScan < 0))
			{
				throw new ArgumentException("Failed to get LCProfile. Input startScan was greater than input endScan. startScan=" + startScan + ", endScan=" + endScan);
			}

			int lengthOfScanArray = endScan - startScan + 1;
			imsScanValues = new int[lengthOfScanArray];
			intensities = new int[lengthOfScanArray];

			int[] lowerAndUpperBinBoundaries = GetUpperLowerBinsFromMz(startFrameNumber, targetMZ, toleranceInMZ);

			int[][][] intensityBlock = GetIntensityBlock(startFrameNumber, endFrameNumber, frameType, startScan, endScan, lowerAndUpperBinBoundaries[0], lowerAndUpperBinBoundaries[1]);

			for (int scanIndex = startScan; scanIndex <= endScan; scanIndex++)
			{
				int frameSum = 0;
				for (int frameIndex = startFrameNumber; frameIndex <= endFrameNumber; frameIndex++)
				{
					int binSum = 0;
					for (int bin = lowerAndUpperBinBoundaries[0]; bin <= lowerAndUpperBinBoundaries[1]; bin++)
					{
						binSum += intensityBlock[frameIndex - startFrameNumber][scanIndex - startScan][bin - lowerAndUpperBinBoundaries[0]];
					}
					frameSum += binSum;

				}

				intensities[scanIndex - startScan] = frameSum;
				imsScanValues[scanIndex - startScan] = scanIndex;
			}
		}

        /// <summary>
        /// Method to provide the bytes from tables that store metadata files 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public byte[] GetFileBytesFromTable(string tableName)
        {
            SQLiteDataReader reader = null;
            byte[] byteBuffer = null;

            try
            {
                m_getFileBytesCommand.CommandText = "SELECT FileText from " + tableName;

                if (TableExists(tableName))
                {
                    reader = m_getFileBytesCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        byteBuffer = (byte[])(reader["FileText"]);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return byteBuffer;
        }
        
        public Stack<int[]> GetFrameAndScanListByDescendingIntensity()
        {
            FrameParameters fp = GetFrameParameters(0);
            Stack<int[]> tuples = new Stack<int[]>(m_globalParameters.NumFrames * fp.Scans);

        	m_sqliteDataReader = m_getFramesAndScanByDescendingIntensityCommand.ExecuteReader();
            while (m_sqliteDataReader.Read())
            {
                int[] values = new int[3];
                values[0] = Convert.ToInt32(m_sqliteDataReader[0]);
                values[1] = Convert.ToInt32(m_sqliteDataReader[1]);
                values[2] = Convert.ToInt32(m_sqliteDataReader[2]);

                tuples.Push(values);
            }
            m_sqliteDataReader.Close();
            return tuples;
        }

        /// <summary>
        /// Returns the frame numbers for the specified frame_type
        /// </summary>
        /// <returns></returns>
        public int[] GetFrameNumbers(FrameType frameType)
        {
			List<int> frameNumberList = new List<int>();

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT DISTINCT(FrameNum) FROM Frame_Parameters WHERE FrameType = :FrameType ORDER BY FrameNum";
				dbCmd.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));
				dbCmd.Prepare();
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					while (reader.Read())
					{
						frameNumberList.Add(Convert.ToInt32(reader["FrameNum"]));
					}
				}
			}

        	return frameNumberList.ToArray();
        }

		public FrameParameters GetFrameParameters(int frameNumber)
		{
			if (frameNumber < 0)
			{
				throw new ArgumentOutOfRangeException("FrameNumber should be greater than or equal to zero.");
			}

			FrameParameters frameParameters = m_frameParametersCache[frameNumber];

			// Check in cache first
			if (frameParameters == null)
			{
				frameParameters = new FrameParameters();

				// Parameters are not yet cached; retrieve and cache them
				if (m_uimfDatabaseConnection != null)
				{
					m_getFrameParametersCommand.Parameters.Add(new SQLiteParameter("FrameNum", frameNumber));

					SQLiteDataReader reader = m_getFrameParametersCommand.ExecuteReader();
					if (reader.Read())
					{
						PopulateFrameParameters(frameParameters, reader);
					}

					// Store the frame parameters in the cache
					m_frameParametersCache[frameNumber] = frameParameters;
					m_getFrameParametersCommand.Parameters.Clear();

					reader.Close();
				}
			}

			return frameParameters;
		}

		public MZ_Calibrator GetMzCalibrator(FrameParameters frameParameters)
		{
			return new MZ_Calibrator(frameParameters.CalibrationSlope / 10000.0, frameParameters.CalibrationIntercept * 10000.0);
		}

        /// <summary>
        /// Returns the key frame pressure value that is used in the calculation of drift time 
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <returns>Frame pressure used in drift time calc</returns>
        public double GetFramePressureForCalculationOfDriftTime(int frameIndex)
        {

            /*
             * [gord, April 2011] A little history..
             * Earlier UIMF files have the column 'PressureBack' but not the 
             * newer 'RearIonFunnelPressure' or 'IonFunnelTrapPressure'
             * 
             * So, will first check for old format
             * if there is a value there, will use it.  If not,
             * look for newer columns and use these values. 
             */

            FrameParameters fp = GetFrameParameters(frameIndex);
            double pressure = fp.PressureBack;

            if (Math.Abs(pressure - 0) < float.Epsilon) pressure = fp.RearIonFunnelPressure;
			if (Math.Abs(pressure - 0) < float.Epsilon) pressure = fp.IonFunnelTrapPressure;

            return pressure;

        }

		public int[][] GetFramesAndScanIntensitiesForAGivenMz(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScan, int endScan, double targetMZ, double toleranceInMZ)
		{
			if ((startFrameNumber > endFrameNumber) || (startFrameNumber < 0))
			{
				throw new ArgumentException("Failed to get 3D profile. Input startFrame was greater than input endFrame");
			}

			if (startScan > endScan || startScan < 0)
			{
				throw new ArgumentException("Failed to get 3D profile. Input startScan was greater than input endScan");
			}

			int[][] intensityValues = new int[endFrameNumber - startFrameNumber + 1][];
			int[] lowerUpperBins = GetUpperLowerBinsFromMz(startFrameNumber, targetMZ, toleranceInMZ);

			int[][][] frameIntensities = GetIntensityBlock(startFrameNumber, endFrameNumber, frameType, startScan, endScan, lowerUpperBins[0], lowerUpperBins[1]);


			for (int frameNumber = startFrameNumber; frameNumber <= endFrameNumber; frameNumber++)
			{
				intensityValues[frameNumber - startFrameNumber] = new int[endScan - startScan + 1];
				for (int scan = startScan; scan <= endScan; scan++)
				{
					int sumAcrossBins = 0;
					for (int bin = lowerUpperBins[0]; bin <= lowerUpperBins[1]; bin++)
					{
						int binIntensity = frameIntensities[frameNumber - startFrameNumber][scan - startScan][bin - lowerUpperBins[0]];
						sumAcrossBins += binIntensity;
					}

					intensityValues[frameNumber - startFrameNumber][scan - startScan] = sumAcrossBins;
				}
			}

			return intensityValues;
		}

        /// <summary>
        /// Populate the global parameters object, m_globalParameters
        /// </summary>
        /// <remarks>We want to make sure that this method is only called once</remarks>
        /// <returns></returns>
        public GlobalParameters GetGlobalParameters()
        {
            if (m_globalParameters == null)
            {
                //Retrieve it from the database
                if (m_uimfDatabaseConnection == null)
                {
                    //this means that you've called this method without opening the UIMF file.
                    //should throw an exception saying UIMF file not opened here
                    //for now, let's just set an error flag
                    //success = false;
                    //the custom exception class has to be defined as yet
                }
                else
                {
                    m_globalParameters = GetGlobalParametersFromTable(m_uimfDatabaseConnection);                    
                }
            }

            return m_globalParameters;
        }

        /// <summary>
        /// Returns the saturation level (maximum intensity value) for a single unit of measurement
        /// </summary>
        /// <returns>saturation level</returns>
        public int GetSaturationLevel()
        {
            int prescanAccumulations;
            if (m_globalParameters == null) prescanAccumulations = GetGlobalParameters().Prescan_Accumulations;
            else prescanAccumulations = m_globalParameters.Prescan_Accumulations;

            return prescanAccumulations*255;
        }

        public static GlobalParameters GetGlobalParametersFromTable(SQLiteConnection oUimfDatabaseConnection)
        {
            GlobalParameters oGlobalParameters = new GlobalParameters();

            SQLiteCommand dbCmd = oUimfDatabaseConnection.CreateCommand();
            dbCmd.CommandText = "SELECT * FROM Global_Parameters";

            SQLiteDataReader reader = dbCmd.ExecuteReader();
            while (reader.Read())
            {
                try
                {

                    oGlobalParameters.BinWidth = Convert.ToDouble(reader["BinWidth"]);
                    oGlobalParameters.DateStarted = Convert.ToString(reader["DateStarted"]);
                    oGlobalParameters.NumFrames = Convert.ToInt32(reader["NumFrames"]);
                    oGlobalParameters.TimeOffset = Convert.ToInt32(reader["TimeOffset"]);
                    oGlobalParameters.BinWidth = Convert.ToDouble(reader["BinWidth"]);
                    oGlobalParameters.Bins = Convert.ToInt32(reader["Bins"]);
                    try
                    {
                        oGlobalParameters.TOFCorrectionTime = Convert.ToSingle(reader["TOFCorrectionTime"]);
                    }
                    catch
                    {
                        m_errMessageCounter++;
                        Console.WriteLine("Warning: this UIMF file is created with an old version of IMF2UIMF (TOFCorrectionTime is missing from the Global_Parameters table), please get the newest version from \\\\floyd\\software");
                    }
                    oGlobalParameters.Prescan_TOFPulses = Convert.ToInt32(reader["Prescan_TOFPulses"]);
                    oGlobalParameters.Prescan_Accumulations = Convert.ToInt32(reader["Prescan_Accumulations"]);
                    oGlobalParameters.Prescan_TICThreshold = Convert.ToInt32(reader["Prescan_TICThreshold"]);
                    oGlobalParameters.Prescan_Continuous = Convert.ToBoolean(reader["Prescan_Continuous"]);
                    oGlobalParameters.Prescan_Profile = Convert.ToString(reader["Prescan_Profile"]);
                    oGlobalParameters.FrameDataBlobVersion = (float)Convert.ToDouble((reader["FrameDataBlobVersion"]));
                    oGlobalParameters.ScanDataBlobVersion = (float)Convert.ToDouble((reader["ScanDataBlobVersion"]));
                    oGlobalParameters.TOFIntensityType = Convert.ToString(reader["TOFIntensityType"]);
                    oGlobalParameters.DatasetType = Convert.ToString(reader["DatasetType"]);
                    try
                    {
                        oGlobalParameters.InstrumentName = Convert.ToString(reader["Instrument_Name"]);
                    }
                    catch (Exception)
                    {
                        //ignore since it may not be present in all previous versions
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to get global parameters " + ex);
                }
            }

            dbCmd.Dispose();
            reader.Close();

            return oGlobalParameters;
        }

		public double[][] GetIntensityBlockForDemultiplexing(int frameNumber, FrameType frameType, int segmentLength, Dictionary<int, int> scanToIndexMap)
		{
			FrameParameters frameParameters = GetFrameParameters(frameNumber);

			int numBins = m_globalParameters.Bins;
			int numScans = frameParameters.Scans;

			// The number of scans has to be divisible by the given segment length
			if (numScans % segmentLength != 0)
			{
				throw new Exception("Number of scans of " + numScans + " is not divisible by the given segment length of " + segmentLength);
			}

			// Initialize the intensities 2-D array
			double[][] intensities = new double[numBins][];
			for (int i = 0; i < numBins; i++)
			{
				intensities[i] = new double[numScans];
			}

			// Now setup queries to retrieve data
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum1", frameParameters.FrameNum));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum2", frameParameters.FrameNum));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum1", -1));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum2", numScans));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));

			byte[] decompSpectraRecord = new byte[m_globalParameters.Bins * DATASIZE];

			using (m_sqliteDataReader = m_getSpectrumCommand.ExecuteReader())
			{
				while (m_sqliteDataReader.Read())
				{
					int binIndex = 0;

					byte[] spectra = (byte[])(m_sqliteDataReader["Intensities"]);
					int scanNumber = Convert.ToInt32(m_sqliteDataReader["ScanNum"]);

					if (spectra.Length > 0)
					{
						int outputLength = LZFCompressionUtil.Decompress(ref spectra, spectra.Length, ref decompSpectraRecord, m_globalParameters.Bins * DATASIZE);
						int numReturnedBins = outputLength / DATASIZE;
						for (int i = 0; i < numReturnedBins; i++)
						{
							int decodedIntensityValue = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);

							if (decodedIntensityValue < 0)
							{
								binIndex += -decodedIntensityValue;
							}
							else
							{
								intensities[binIndex][scanToIndexMap[scanNumber]] = decodedIntensityValue;
								binIndex++;
							}
						}
					}
				}
			}

			m_getSpectrumCommand.Parameters.Clear();

			return intensities;
		}

		public void GetLCProfile(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScan, int endScan, double targetMZ, double toleranceInMZ, out int[] frameValues, out int[] intensities)
		{
			if ((startFrameNumber > endFrameNumber) || (startFrameNumber < 0))
			{
				throw new ArgumentException("Failed to get LCProfile. Input startFrame was greater than input endFrame. start_frame=" + startFrameNumber.ToString() + ", end_frame=" + endFrameNumber.ToString());
			}

			frameValues = new int[endFrameNumber - startFrameNumber + 1];

			int[] lowerAndUpperBinBoundaries = GetUpperLowerBinsFromMz(startFrameNumber, targetMZ, toleranceInMZ);
			intensities = new int[endFrameNumber - startFrameNumber + 1];

			int[][][] frameIntensities = GetIntensityBlock(startFrameNumber, endFrameNumber, frameType, startScan, endScan, lowerAndUpperBinBoundaries[0], lowerAndUpperBinBoundaries[1]);
			for (int frameNumber = startFrameNumber; frameNumber <= endFrameNumber; frameNumber++)
			{
				int scanSum = 0;
				for (int scan = startScan; scan <= endScan; scan++)
				{
					int binSum = 0;
					for (int bin = lowerAndUpperBinBoundaries[0]; bin <= lowerAndUpperBinBoundaries[1]; bin++)
					{
						binSum += frameIntensities[frameNumber - startFrameNumber][scan - startScan][bin - lowerAndUpperBinBoundaries[0]];
					}
					scanSum += binSum;
				}

				intensities[frameNumber - startFrameNumber] = scanSum;
				frameValues[frameNumber - startFrameNumber] = frameNumber;
			}
		}

        public SortedList<int, LogEntry> GetLogEntries(string entryType, string postedBy)
        {
			SortedList<int, LogEntry> lstLogEntries = new SortedList<int, LogEntry>();

            if (TableExists("Log_Entries"))
            {
				using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
				{

					string sSql = "SELECT Entry_ID, Posted_By, Posting_Time, Type, Message FROM Log_Entries";
					string sWhere = String.Empty;

					if (!String.IsNullOrEmpty(entryType))
					{
						sWhere = "WHERE Type = '" + entryType + "'";
					}

					if (!String.IsNullOrEmpty(postedBy))
					{
						if (sWhere.Length == 0)
						{
							sWhere = "WHERE";
						}
						else
						{
							sWhere += " AND";
						}

						sWhere += " Posted_By = '" + postedBy + "'";
					}

					if (sWhere.Length > 0)
					{
						sSql += " " + sWhere;
					}

					sSql += " ORDER BY Entry_ID;";

					dbCmd.CommandText = sSql;

					using (SQLiteDataReader reader = dbCmd.ExecuteReader())
					{
						while (reader.Read())
						{
							try
							{
								LogEntry logEntry = new LogEntry();

								int iEntryID = Convert.ToInt32(reader["Entry_ID"]);
								logEntry.PostedBy = Convert.ToString(reader["Posted_By"]);

								string sPostingTime = Convert.ToString(reader["Posting_Time"]);
								DateTime postingTime;
								DateTime.TryParse(sPostingTime, out postingTime);
								logEntry.PostingTime = postingTime;

								logEntry.Type = Convert.ToString(reader["Type"]);
								logEntry.Message = Convert.ToString(reader["Message"]);

								lstLogEntries.Add(iEntryID, logEntry);

							}
							catch (Exception ex)
							{
								throw new Exception("Failed to get global parameters " + ex);
							}
						}
					}
				}
            }

            return lstLogEntries;
        }

        /// <summary>
        /// Constructs a dictionary that has the frame numbers as the key and the frame type as the value.
        /// </summary>
        /// <returns>Returns a dictionary object that has frame number as the key and frame type as the value.</returns>
		public Dictionary<int, FrameType> GetMasterFrameList()
        {
			Dictionary<int, FrameType> masterFrameDictionary = new Dictionary<int, FrameType>();

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT DISTINCT(FrameNum), FrameType FROM Frame_Parameters";
				dbCmd.Prepare();
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					while (reader.Read())
					{
						int frameNumber = Convert.ToInt32(reader["FrameNum"]);
						int frameType = Convert.ToInt32(reader["FrameType"]);

						// If the frame type is 0, then we are dealing with an old UIMF file where the MS1 frames were labeled as 0
						if (frameType == 0) frameType = 1;

						masterFrameDictionary.Add(frameNumber, (FrameType) frameType);
					}
				}
			}

        	return masterFrameDictionary;
        }

        /// <summary>
        /// Utility method to return the Frame Type for a particular frame number
        /// </summary>
        /// <param name="frameNumber"></param>
        /// <returns></returns>
        public FrameType GetFrameTypeForFrame(int frameNumber)
        {
        	int frameTypeInt = -1;

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT FrameType FROM Frame_Parameters WHERE FrameNum = :FrameNumber";
				dbCmd.Parameters.Add(new SQLiteParameter("FrameNumber", frameNumber));
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					if (reader.Read())
					{
						frameTypeInt = Convert.ToInt32(reader["FrameType"]);
					}
				}
			}

			// If the frame type is 0, then this is an older UIMF file where the MS1 frames were labeled as 0
			if(frameTypeInt == 0)
			{
				return FrameType.MS1;
			}
			
			return (FrameType) frameTypeInt;
        }

		/// <summary>
		/// </summary>
		/// <param name="frameType"></param>
		/// <returns></returns>
		public int GetNumberOfFrames(FrameType frameType)
		{
			int count = 0;

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT COUNT(DISTINCT(FrameNum)) AS FrameCount FROM Frame_Parameters WHERE FrameType IN (:FrameType)";
				dbCmd.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? "0,1" : ((int)frameType).ToString())));
				dbCmd.Prepare();
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					if (reader.Read())
					{
						count = Convert.ToInt32(reader["FrameCount"]);
					}
				}
			}

			return count;
		}

		public double GetPixelMZ(int bin)
		{
			if ((m_calibrationTable != null) && (bin < m_calibrationTable.Length))
			{
				return m_calibrationTable[bin];
			}

			return -1;
		}

		private SpectrumCache GetOrCreateSpectrumCache(int startFrameNumber, int endFrameNumber, FrameType frameType)
		{
			foreach (SpectrumCache possibleSpectrumCache in m_spectrumCacheList)
			{
				if (possibleSpectrumCache.StartFrameNumber == startFrameNumber && possibleSpectrumCache.EndFrameNumber == endFrameNumber)
				{
					return possibleSpectrumCache;
				}
			}

			// Initialize List of arrays that will be used for the cache
			int numScansInFrame = GetFrameParameters(startFrameNumber).Scans;
			IList<IDictionary<int, int>> listOfIntensityDictionaries = new List<IDictionary<int, int>>(numScansInFrame);
			Dictionary<int, int> summedIntensityDictionary = new Dictionary<int, int>();

			// Initialize each array that will be used for the cache
			for (int i = 0; i < numScansInFrame; i++)
			{
				listOfIntensityDictionaries.Add(new Dictionary<int, int>());
			}

			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum1", startFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum2", endFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum1", 1));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum2", numScansInFrame));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));

			using (SQLiteDataReader reader = m_getSpectrumCommand.ExecuteReader())
			{
				byte[] decompSpectraRecord = new byte[m_globalParameters.Bins*DATASIZE];

				while (reader.Read())
				{
					int binIndex = 0;
					byte[] spectraRecord = (byte[]) (reader["Intensities"]);
					if (spectraRecord.Length > 0)
					{
						int scanNum = Convert.ToInt32(reader["ScanNum"]);

						int outputLength = LZFCompressionUtil.Decompress(ref spectraRecord, spectraRecord.Length, ref decompSpectraRecord,
							                                                m_globalParameters.Bins*DATASIZE);
						int numBins = outputLength/DATASIZE;

						IDictionary<int, int> currentIntensityDictionary = listOfIntensityDictionaries[scanNum];

						for (int i = 0; i < numBins; i++)
						{
							int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i*DATASIZE);
							if (decodedSpectraRecord < 0)
							{
								binIndex += -decodedSpectraRecord;
							}
							else
							{
								int currentValue;
								if (currentIntensityDictionary.TryGetValue(binIndex, out currentValue))
								{
									currentIntensityDictionary[binIndex] += decodedSpectraRecord;
									summedIntensityDictionary[binIndex] += decodedSpectraRecord;
								}
								else
								{
									currentIntensityDictionary.Add(binIndex, decodedSpectraRecord);

									// Check the summed dictionary
									if(summedIntensityDictionary.TryGetValue(binIndex, out currentValue))
									{
										summedIntensityDictionary[binIndex] += decodedSpectraRecord;
									}
									else
									{
										summedIntensityDictionary.Add(binIndex, decodedSpectraRecord);
									}
								}

								binIndex++;
							}
						}
					}
				}
			}

			// Create the new spectrum cache
			SpectrumCache spectrumCache = new SpectrumCache(startFrameNumber, endFrameNumber, listOfIntensityDictionaries, summedIntensityDictionary);

			if(m_spectrumCacheList.Count >= 10)
			{
				m_spectrumCacheList.RemoveAt(0);
			}

			m_spectrumCacheList.Add(spectrumCache);

			m_getSpectrumCommand.Parameters.Clear();

			return spectrumCache;
		}

    	/// <summary>
    	/// Extracts m/z values and intensities from given frame number and scan number.
    	/// Each entry into mzArray will be the m/z value that contained a non-zero intensity value.
    	/// The index of the m/z value in mzArray will match the index of the corresponding intensity value in intensityArray.
    	/// </summary>
    	/// <param name="frameNumber">The frame number of the desired spectrum.</param>
    	/// <param name="frameType">The frame type to consider.</param>
    	/// <param name="scanNumber">The scan number of the desired spectrum.</param>
    	/// <param name="mzArray">The m/z values that contained non-zero intensity values.</param>
    	/// <param name="intensityArray">The corresponding intensity values of the non-zero m/z value.</param>
    	/// <returns>The number of non-zero m/z values found in the resulting spectrum.</returns>
    	public int GetSpectrum(int frameNumber, FrameType frameType, int scanNumber, out double[] mzArray, out int[] intensityArray)
		{
			return GetSpectrum(frameNumber, frameNumber, frameType, scanNumber, scanNumber, out mzArray, out intensityArray);
		}

		/// <summary>
		/// Extracts m/z values and intensities from given frame range and scan range.
		/// The intensity values of each m/z value are summed across the frame range. The result is a spectrum for a single frame.
		/// Each entry into mzArray will be the m/z value that contained a non-zero intensity value.
		/// The index of the m/z value in mzArray will match the index of the corresponding intensity value in intensityArray.
		/// </summary>
		/// <param name="startFrameNumber">The start frame number of the desired spectrum.</param>
		/// <param name="endFrameNumber">The end frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
		/// <param name="startScanNumber">The start scan number of the desired spectrum.</param>
		/// <param name="endScanNumber">The end scan number of the desired spectrum.</param>
		/// <param name="mzArray">The m/z values that contained non-zero intensity values.</param>
		/// <param name="intensityArray">The corresponding intensity values of the non-zero m/z value.</param>
		/// <returns>The number of non-zero m/z values found in the resulting spectrum.</returns>
		public int GetSpectrum(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScanNumber, int endScanNumber, out double[] mzArray, out int[] intensityArray)
		{
			int nonZeroCount = 0;

			SpectrumCache spectrumCache = GetOrCreateSpectrumCache(startFrameNumber, endFrameNumber, frameType);

			FrameParameters frameParams = GetFrameParameters(startFrameNumber);

			// Allocate the maximum possible for these arrays. Later on we will strip out the zeros.
			// Adding 1 to the size to fix a bug in some old IMS data where the bin index could exceed the maximum bins by 1
			mzArray = new double[m_globalParameters.Bins + 1];
			intensityArray = new int[m_globalParameters.Bins + 1];

			IList<IDictionary<int, int>> cachedListOfIntensityDictionaries = spectrumCache.ListOfIntensityDictionaries;

			// If we are summing all scans together, then we can use the summed version of the spectrum cache
			if (endScanNumber - startScanNumber + 1 == frameParams.Scans)
			{
				IDictionary<int, int> currentIntensityDictionary = spectrumCache.SummedIntensityDictionary;

				foreach (KeyValuePair<int, int> kvp in currentIntensityDictionary)
				{
					int binIndex = kvp.Key;
					int intensity = kvp.Value;

					if (intensityArray[binIndex] == 0)
					{
						mzArray[binIndex] = ConvertBinToMZ(frameParams.CalibrationSlope, frameParams.CalibrationIntercept, m_globalParameters.BinWidth, m_globalParameters.TOFCorrectionTime, binIndex);
						nonZeroCount++;
					}

					intensityArray[binIndex] += intensity;
				}
			}
			else
			{
				// Get the data out of the cache, making sure to sum across scans if necessary
				for (int scanIndex = startScanNumber; scanIndex <= endScanNumber; scanIndex++)
				{
					IDictionary<int, int> currentIntensityDictionary = cachedListOfIntensityDictionaries[scanIndex];

					foreach (KeyValuePair<int, int> kvp in currentIntensityDictionary)
					{
						int binIndex = kvp.Key;
						int intensity = kvp.Value;

						if (intensityArray[binIndex] == 0)
						{
							mzArray[binIndex] = ConvertBinToMZ(frameParams.CalibrationSlope, frameParams.CalibrationIntercept, m_globalParameters.BinWidth, m_globalParameters.TOFCorrectionTime, binIndex);
							nonZeroCount++;
						}

						intensityArray[binIndex] += intensity;
					}
				}
			}
			

			StripZerosFromArrays(nonZeroCount, ref mzArray, ref intensityArray);

			return nonZeroCount;
		}

		/// <summary>
		/// Extracts m/z values and intensities from given frame range and scan range and m/z range.
		/// The intensity values of each m/z value are summed across the frame range. The result is a spectrum for a single frame.
		/// Each entry into mzArray will be the m/z value that contained a non-zero intensity value.
		/// The index of the m/z value in mzArray will match the index of the corresponding intensity value in intensityArray.
		/// </summary>
		/// <param name="startFrameNumber">The start frame number of the desired spectrum.</param>
		/// <param name="endFrameNumber">The end frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
		/// <param name="startScanNumber">The start scan number of the desired spectrum.</param>
		/// <param name="endScanNumber">The end scan number of the desired spectrum.</param>
		/// <param name="startMz">The start m/z value of the desired spectrum.</param>
		/// <param name="endMz">The end m/z value of the desired spectrum.</param>
		/// <param name="mzArray">The m/z values that contained non-zero intensity values.</param>
		/// <param name="intensityArray">The corresponding intensity values of the non-zero m/z value.</param>
		/// <returns>The number of non-zero m/z values found in the resulting spectrum.</returns>
		public int GetSpectrum(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScanNumber, int endScanNumber, double startMz, double endMz, out double[] mzArray, out int[] intensityArray)
		{
			FrameParameters frameParameters = GetFrameParameters(startFrameNumber);

			double slope = frameParameters.CalibrationSlope;
			double intercept = frameParameters.CalibrationIntercept;
			double binWidth = m_globalParameters.BinWidth;
			float tofCorrectionTime = m_globalParameters.TOFCorrectionTime;

			int startBin = (int)Math.Floor(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, startMz)) - 1;
			int endBin = (int)Math.Ceiling(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, endMz)) + 1;

			if (startBin < 0 || endBin > m_globalParameters.Bins)
			{
				// If the start or end bin is outside a normal range, then just grab everything
				return GetSpectrum(startFrameNumber, endFrameNumber, frameType, startScanNumber, endScanNumber, out mzArray, out intensityArray);
			}

			int numFrames = endFrameNumber - startFrameNumber + 1;
			int numScans = endScanNumber - startScanNumber + 1;
			int numBins = endBin - startBin + 1;

			if((numFrames * numScans) < numBins || !m_doesContainBinCentricData)
			{
			    return GetSpectrum(startFrameNumber, endFrameNumber, frameType, startScanNumber, endScanNumber, startBin, endBin, out mzArray, out intensityArray);
			}
			else
			{
				return GetSpectrumBinCentric(startFrameNumber, endFrameNumber, frameType, startScanNumber, endScanNumber, startBin, endBin, out mzArray, out intensityArray);
			}
		}

		/// <summary>
		/// Extracts m/z values and intensities from given frame range and scan range and bin range.
		/// The intensity values of each m/z value are summed across the frame range. The result is a spectrum for a single frame.
		/// Each entry into mzArray will be the m/z value that contained a non-zero intensity value.
		/// The index of the m/z value in mzArray will match the index of the corresponding intensity value in intensityArray.
		/// </summary>
		/// <param name="startFrameNumber">The start frame number of the desired spectrum.</param>
		/// <param name="endFrameNumber">The end frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
		/// <param name="startScanNumber">The start scan number of the desired spectrum.</param>
		/// <param name="endScanNumber">The end scan number of the desired spectrum.</param>
		/// <param name="startBin">The start bin index of the desired spectrum.</param>
		/// <param name="endBin">The end bin index of the desired spectrum.</param>
		/// <param name="mzArray">The m/z values that contained non-zero intensity values.</param>
		/// <param name="intensityArray">The corresponding intensity values of the non-zero m/z value.</param>
		/// <returns>The number of non-zero m/z values found in the resulting spectrum.</returns>
		public int GetSpectrumBinCentric(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScanNumber, int endScanNumber, int startBin, int endBin, out double[] mzArray, out int[] intensityArray)
		{
			//Console.WriteLine("LC " + startFrameNumber + " - " + endFrameNumber + "\t IMS " + startScanNumber + " - " + endScanNumber + "\t Bin " + startBin + " - " + endBin);

			List<double> mzList = new List<double>();
			List<int> intensityList = new List<int>();

			FrameParameters frameParams = GetFrameParameters(startFrameNumber);
			int numImsScans = frameParams.Scans;

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", startBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", endBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				//int maxDecompressedSPectraSize = m_globalParameters.NumFrames * frameParams.Scans * DATASIZE;
				//byte[] decompSpectraRecord = new byte[maxDecompressedSPectraSize];

				while (reader.Read())
				{
					int binNumber = Convert.ToInt32(reader["MZ_BIN"]);
					int intensity = 0;
					int entryIndex = 0;
					//int numEntries = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					//if (spectraRecord.Length > 0)
					//{
					//    int outputLength = LZFCompressionUtil.Decompress(ref spectraRecord, spectraRecord.Length, ref decompSpectraRecord, maxDecompressedSPectraSize);
					//    numEntries = outputLength / DATASIZE;
					//}

					for (int i = 0; i < decompSpectraRecord.Length; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numImsScans, out scanLc, out scanIms);

							// If we pass the LC Scan number we are interested in, then go ahead and quit
							if (scanLc > endFrameNumber) break;

							// Only add to the intensity if it is within the specified range
							if (scanLc >= startFrameNumber && scanIms >= startScanNumber && scanIms <= endScanNumber)
							{
								// Only consider the FrameType that was given
								if (GetFrameParameters(scanLc).FrameType == frameType)
								{
									intensity += decodedSpectraRecord;
								}
							}
						}
					}

					if (intensity > 0)
					{
						double mz = ConvertBinToMZ(frameParams.CalibrationSlope, frameParams.CalibrationIntercept, m_globalParameters.BinWidth, m_globalParameters.TOFCorrectionTime, binNumber);
						mzList.Add(mz);
						intensityList.Add(intensity);
					}
				}
			}

			mzArray = mzList.ToArray();
			intensityArray = intensityList.ToArray();

			m_getBinDataCommand.Parameters.Clear();

			return mzList.Count;
		}

		/// <summary>
		/// Extracts m/z values and intensities from given frame range and scan range and bin range.
		/// The intensity values of each m/z value are summed across the frame range. The result is a spectrum for a single frame.
		/// Each entry into mzArray will be the m/z value that contained a non-zero intensity value.
		/// The index of the m/z value in mzArray will match the index of the corresponding intensity value in intensityArray.
		/// </summary>
		/// <param name="startFrameNumber">The start frame number of the desired spectrum.</param>
		/// <param name="endFrameNumber">The end frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
		/// <param name="startScanNumber">The start scan number of the desired spectrum.</param>
		/// <param name="endScanNumber">The end scan number of the desired spectrum.</param>
		/// <param name="startBin">The start bin index of the desired spectrum.</param>
		/// <param name="endBin">The end bin index of the desired spectrum.</param>
		/// <param name="mzArray">The m/z values that contained non-zero intensity values.</param>
		/// <param name="intensityArray">The corresponding intensity values of the non-zero m/z value.</param>
		/// <returns>The number of non-zero m/z values found in the resulting spectrum.</returns>
		public int GetSpectrum(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScanNumber, int endScanNumber, int startBin, int endBin, out double[] mzArray, out int[] intensityArray)
		{
			int nonZeroCount = 0;
			int numBinsToConsider = endBin - startBin + 1;
			int intensity = 0;

			// Allocate the maximum possible for these arrays. Later on we will strip out the zeros.
			mzArray = new double[numBinsToConsider];
			intensityArray = new int[numBinsToConsider];

			SpectrumCache spectrumCache = GetOrCreateSpectrumCache(startFrameNumber, endFrameNumber, frameType);
			FrameParameters frameParams = GetFrameParameters(startFrameNumber);
			IList<IDictionary<int, int>> cachedListOfIntensityDictionaries = spectrumCache.ListOfIntensityDictionaries;

			// If we are summing all scans together, then we can use the summed version of the spectrum cache
			if (endScanNumber - startScanNumber + 1 == frameParams.Scans)
			{
				IDictionary<int, int> summedIntensityDictionary = spectrumCache.SummedIntensityDictionary;

				for (int binIndex = 0; binIndex < numBinsToConsider; binIndex++)
				{
					int binNumber = binIndex + startBin;
					if (!summedIntensityDictionary.TryGetValue(binNumber, out intensity)) continue;

					if (intensityArray[binIndex] == 0)
					{
						mzArray[binIndex] = ConvertBinToMZ(frameParams.CalibrationSlope, frameParams.CalibrationIntercept, m_globalParameters.BinWidth, m_globalParameters.TOFCorrectionTime, binNumber);
						nonZeroCount++;
					}

					intensityArray[binIndex] += intensity;
				}
			}
			else
			{
				// Get the data out of the cache, making sure to sum across scans if necessary
				for (int scanIndex = startScanNumber; scanIndex <= endScanNumber; scanIndex++)
				{
					IDictionary<int, int> currentIntensityDictionary = cachedListOfIntensityDictionaries[scanIndex];

					// No need to move on if the dictionary is empty
					if (currentIntensityDictionary.Count == 0) continue;

					for (int binIndex = 0; binIndex < numBinsToConsider; binIndex++)
					{
						int binNumber = binIndex + startBin;
						if (!currentIntensityDictionary.TryGetValue(binNumber, out intensity)) continue;

						if (intensityArray[binIndex] == 0)
						{
							mzArray[binIndex] = ConvertBinToMZ(frameParams.CalibrationSlope, frameParams.CalibrationIntercept, m_globalParameters.BinWidth, m_globalParameters.TOFCorrectionTime, binNumber);
							nonZeroCount++;
						}

						intensityArray[binIndex] += intensity;
					}
				}
			}

			StripZerosFromArrays(nonZeroCount, ref mzArray, ref intensityArray);

			m_getSpectrumCommand.Parameters.Clear();

			return nonZeroCount;
		}

		/// <summary>
		/// Extracts intensities from given frame range and scan range.
		/// The intensity values of each bin are summed across the frame range. The result is a spectrum for a single frame.
		/// </summary>
		/// <param name="frameNumber">The frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
		/// <param name="scanNumber">The scan number of the desired spectrum.</param>
		/// <returns>The number of non-zero bins found in the resulting spectrum.</returns>
		public int[] GetSpectrumAsBins(int frameNumber, FrameType frameType, int scanNumber)
		{
			return GetSpectrumAsBins(frameNumber, frameNumber, frameType, scanNumber, scanNumber);
		}

    	/// <summary>
    	/// Extracts intensities from given frame range and scan range.
    	/// The intensity values of each bin are summed across the frame range. The result is a spectrum for a single frame.
    	/// </summary>
    	/// <param name="startFrameNumber">The start frame number of the desired spectrum.</param>
		/// <param name="endFrameNumber">The end frame number of the desired spectrum.</param>
		/// <param name="frameType">The frame type to consider.</param>
    	/// <param name="startScanNumber">The start scan number of the desired spectrum.</param>
		/// <param name="endScanNumber">The end scan number of the desired spectrum.</param>
    	/// <returns>An array containing an intensity value for each bin location, even if the intensity value is 0.</returns>
    	public int[] GetSpectrumAsBins(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScanNumber, int endScanNumber)
		{
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum1", startFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum2", endFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum1", startScanNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum2", endScanNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));

			// Adding 1 to the number of bins to fix a bug in some old IMS data where the bin index could exceed the maximum bins by 1
			int[] intensityArray = new int[m_globalParameters.Bins + 1];

			using (SQLiteDataReader reader = m_getSpectrumCommand.ExecuteReader())
			{
				byte[] decompSpectraRecord = new byte[m_globalParameters.Bins * DATASIZE];
				
				while (reader.Read())
				{
					int binIndex = 0;
					byte[] spectraRecord = (byte[])(reader["Intensities"]);

					if (spectraRecord.Length > 0)
					{
						int outputLength = LZFCompressionUtil.Decompress(ref spectraRecord, spectraRecord.Length, ref decompSpectraRecord, m_globalParameters.Bins * DATASIZE);
						int numBins = outputLength / DATASIZE;

						for (int i = 0; i < numBins; i++)
						{
							int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
							if (decodedSpectraRecord < 0)
							{
								binIndex += -decodedSpectraRecord;
							}
							else
							{
								intensityArray[binIndex] += decodedSpectraRecord;
								binIndex++;
							}
						}
					}
				}
			}

			m_getSpectrumCommand.Parameters.Clear();

			return intensityArray;
		}

    	/// <summary>
    	/// Extracts TIC from startFrame to endFrame and startScan to endScan and returns an array
    	/// </summary>
    	/// <param name="frameType"></param>
    	/// <param name="startFrameNumber"></param>
    	/// <param name="endFrameNumber"></param>
    	/// <param name="startScan"></param>
    	/// <param name="endScan"></param>
    	public double[] GetTIC(FrameType frameType, int startFrameNumber, int endFrameNumber, int startScan, int endScan)
        {
            return GetTicOrBpi(frameType, startFrameNumber, endFrameNumber, startScan, endScan, TIC);
        }

        /// <summary>
        /// Extracts TIC from frameNum and scanNum
        /// </summary>
        /// <param name="frameNumber"></param>
        /// <param name="scanNum"></param>
        /// <returns></returns>
        public double GetTIC(int frameNumber, int scanNum)
        {
            double tic = 0;

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT TIC FROM Frame_Scans WHERE FrameNum = " + frameNumber + " AND ScanNum = " + scanNum;
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					if (reader.Read())
					{
						tic = Convert.ToDouble(reader["TIC"]);
					}
				}
			}

            return tic;
        }
		
		/// <summary>
		/// Extracts TIC from startFrame to endFrame and startScan to endScan and returns a dictionary for all frames
		/// </summary>
		/// <param name="startFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="endFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="startScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="endScan">If startScan and endScan are 0, then uses all scans</param>
		/// <returns>Dictionary where keys are frame number and values are the TIC value</returns>
		public Dictionary<int, double> GetTICByFrame(int startFrameNumber, int endFrameNumber, int startScan, int endScan)
		{
			return GetTicOrBpiByFrame(startFrameNumber, endFrameNumber, startScan, endScan, TIC, filterByFrameType:false, frameType:FrameType.MS1);
		}

		/// <summary>
		/// Extracts TIC from startFrame to endFrame and startScan to endScan and returns a dictionary of the specified frame type
		/// </summary>
		/// <param name="startFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="endFrameNumber">If startFrameNumber and endFrameNumber are 0, then returns all frames</param>
		/// <param name="startScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="endScan">If startScan and endScan are 0, then uses all scans</param>
		/// <param name="frameType">FrameType to return</param>
		/// <returns>Dictionary where keys are frame number and values are the TIC value</returns>
		public Dictionary<int, double> GetTICByFrame(int startFrameNumber, int endFrameNumber, int startScan, int endScan, FrameType frameType)
		{
			return GetTicOrBpiByFrame(startFrameNumber, endFrameNumber, startScan, endScan, TIC, filterByFrameType:true, frameType:frameType);
		}

        /// <summary>
        /// Method to check if this dataset has any MSMS data
        /// </summary>
        /// <returns>True if MSMS frames are present</returns>
        public bool HasMSMSData()
        {
			int count = 0;

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT COUNT(DISTINCT(FrameNum)) AS FrameCount FROM Frame_Parameters WHERE FrameType = :FrameType";
				dbCmd.Parameters.Add(new SQLiteParameter("FrameType", (int) FrameType.MS2));
				dbCmd.Prepare();
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					if (reader.Read())
					{
						count = Convert.ToInt32(reader["FrameCount"]);
					}
				}
			}

        	return count > 0;
        }

        /// <summary>
        /// Returns True if all frames with frame types 0 through 3 have CalibrationDone > 0 in frame_parameters
        /// </summary>
        /// <returns></returns>
        public bool IsCalibrated()
        {
            return IsCalibrated(FrameType.Calibration);
        }

        /// <summary>
        /// Returns True if all frames have CalibrationDone > 0 in frame_parameters
        /// </summary>
        /// <param name="iMaxFrameTypeToExamine">Maximum frame type to examine when checking for calibrated frames</param>
        /// <returns></returns>
		public bool IsCalibrated(FrameType iMaxFrameTypeToExamine)
        {
            bool bIsCalibrated = false;

			int iFrameTypeCount = -1;
			int iFrameTypeCountCalibrated = -2;

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT FrameType, COUNT(*)  AS FrameCount, SUM(IFNULL(CalibrationDone, 0)) AS FramesCalibrated " +
				                    "FROM frame_parameters " +
				                    "GROUP BY FrameType;";
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					while (reader.Read())
					{
						int iFrameType = -1;
						try
						{
							iFrameType = Convert.ToInt32(reader[0]);
							int iFrameCount = Convert.ToInt32(reader[1]);
							int iCalibratedFrameCount = Convert.ToInt32(reader[2]);

							if (iMaxFrameTypeToExamine < 0 || iFrameType <= (int) iMaxFrameTypeToExamine)
							{
								iFrameTypeCount += 1;
								if (iFrameCount == iCalibratedFrameCount)
									iFrameTypeCountCalibrated += 1;
							}

						}
						catch (Exception ex)
						{
							Console.WriteLine("Exception determing if all frames are calibrated; error occurred with FrameType " + iFrameType + ": " + ex.Message);
						}
					}
				}
			}

			if (iFrameTypeCount == iFrameTypeCountCalibrated)
			{
				bIsCalibrated = true;
			}

        	return bIsCalibrated;
        }

		/// <summary>
		/// Post a new log entry to table Log_Entries
		/// </summary>
		/// <param name="entryType">Log entry type (typically Normal, Error, or Warning)</param>
		/// <param name="message">Log message</param>
		/// <param name="postedBy">Process or application posting the log message</param>
		/// <remarks>The Log_Entries table will be created if it doesn't exist</remarks>
		public void PostLogEntry(string entryType, string message, string postedBy)
		{
			DataWriter.PostLogEntry(m_uimfDatabaseConnection, entryType, message, postedBy);
		}

        public bool TableExists(string tableName)
        {
            return TableExists(m_uimfDatabaseConnection, tableName);
        }

        public static bool TableExists(SQLiteConnection oConnection, string tableName)
        {
			bool hasRows;

			using (SQLiteCommand cmd = new SQLiteCommand(oConnection) { CommandText = "SELECT name FROM sqlite_master WHERE type='table' And name = '" + tableName + "'" })
			{
				using (SQLiteDataReader rdr = cmd.ExecuteReader())
				{
					hasRows = rdr.HasRows;
				}
			}

        	return hasRows;
        }

        public bool TableHasColumn(string tableName, string columnName)
        {
            return TableHasColumn(m_uimfDatabaseConnection, tableName, columnName);
        }

        public static bool TableHasColumn(SQLiteConnection oConnection, string tableName, string columnName)
        {
			bool hasColumn;

			using (SQLiteCommand cmd = new SQLiteCommand(oConnection) { CommandText = "Select * From '" + tableName + "' Limit 1;" })
			{
				using (SQLiteDataReader rdr = cmd.ExecuteReader())
				{
					hasColumn = rdr.GetOrdinal(columnName) >= 0;
				}
			}

        	return hasColumn;
        }

    	/// <summary>
    	/// /// Update the calibration coefficients for all frames
    	/// </summary>
    	/// <param name="slope">The slope value for the calibration.</param>
    	/// <param name="intercept">The intercept for the calibration.</param>
		/// <param name="isAutoCalibrating">Optional argument that should be set to true if calibration is automatic. Defaults to false.</param>
    	public void UpdateAllCalibrationCoefficients(float slope, float intercept, bool isAutoCalibrating = false)
		{
			m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
			m_preparedStatement.CommandText = "UPDATE Frame_Parameters " +
											 "SET CalibrationSlope = " + slope + ", " +
												 "CalibrationIntercept = " + intercept;
			if (isAutoCalibrating)
			{
				m_preparedStatement.CommandText += ", CalibrationDone = 1";
			}

    		m_preparedStatement.ExecuteNonQuery();
			m_preparedStatement.Dispose();

			foreach (FrameParameters frameParameters in m_frameParametersCache)
			{
				frameParameters.CalibrationSlope = slope;
				frameParameters.CalibrationIntercept = intercept;
			}
		}

		/// <summary>
		/// Update the calibration coefficients for a single frame
		/// </summary>
		/// <param name="frameNumber">The frame number to update.</param>
		/// <param name="slope">The slope value for the calibration.</param>
		/// <param name="intercept">The intercept for the calibration.</param>
		/// <param name="isAutoCalibrating">Optional argument that should be set to true if calibration is automatic. Defaults to false.</param>
		public void UpdateCalibrationCoefficients(int frameNumber, float slope, float intercept, bool isAutoCalibrating = false)
		{
			m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
			m_preparedStatement.CommandText = "UPDATE Frame_Parameters " +
											 "SET CalibrationSlope = " + slope + ", " +
												 "CalibrationIntercept = " + intercept;
			if (isAutoCalibrating)
			{
				m_preparedStatement.CommandText += ", CalibrationDone = 1";
			}

			m_preparedStatement.CommandText += " WHERE FrameNum = " + frameNumber.ToString();

			m_preparedStatement.ExecuteNonQuery();
			m_preparedStatement.Dispose();

			FrameParameters frameParameters = GetFrameParameters(frameNumber);
			frameParameters.CalibrationSlope = slope;
			frameParameters.CalibrationIntercept = intercept;
		}

		/// <summary>
        /// Convert the array of bytes defining a fragmentation sequence to an array of doubles
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        private static double[] ArrayFragmentationSequence(byte[] blob)
        {
            double[] frag = new double[blob.Length / 8];

			for (int i = 0; i < frag.Length; i++)
			{
				frag[i] = BitConverter.ToDouble(blob, i*8);
			}

			return frag;
        }

		/// <summary>
		/// Calculates the LC and IMS scans of an encoded index.
		/// </summary>
		/// <param name="encodedIndex">The encoded index.</param>
		/// <param name="numImsScansInFrame">The number of IMS scans.</param>
		/// <param name="scanLc">The resulting LC Scan number.</param>
		/// <param name="scanIms">The resulting IMS Scan number.</param>
		private void CalculateFrameAndScanForEncodedIndex(int encodedIndex, int numImsScansInFrame, out int scanLc, out int scanIms)
		{
			scanLc = encodedIndex / numImsScansInFrame;
			scanIms = encodedIndex % numImsScansInFrame;
		}

		/// <summary>
		/// Runs a query to see if the bin centric data exists in this UIMF file
		/// </summary>
		/// <returns>true if the binc entric data exists, false otherwise</returns>
		private bool DoesContainBinCentricData()
		{
			using (SQLiteDataReader reader = m_checkForBinCentricTableCommand.ExecuteReader())
			{
				return reader.HasRows;
			}
		}

		private Dictionary<string, string> CloneUIMFGetObjects(string sObjectType)
		{
			Dictionary<string, string> sObjects = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

			SQLiteCommand cmd = new SQLiteCommand(m_uimfDatabaseConnection) {CommandText = "SELECT name, sql FROM main.sqlite_master WHERE type='" + sObjectType + "' ORDER BY NAME"};

			using (SQLiteDataReader reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					sObjects.Add(Convert.ToString(reader["Name"]), Convert.ToString(reader["sql"]));
				}
			}

			return sObjects;
		}

    	private static bool ColumnIsMilliTorr(SQLiteCommand cmd, string tableName, string columnName)
		{
			bool isMillitorr = false;
			try
			{
                cmd.CommandText = "SELECT Avg(Pressure) AS AvgPressure FROM (SELECT " + columnName + " AS Pressure FROM " +
                                  tableName + " WHERE IFNULL(" + columnName + ", 0) > 0 ORDER BY FrameNum LIMIT 25) SubQ";

				object objResult = cmd.ExecuteScalar();
				if (objResult != null && objResult != DBNull.Value)
				{
					if (Convert.ToSingle(objResult) > 100)
					{
						isMillitorr = true;
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception examining pressure column " + columnName + " in table " + tableName + ": " + ex.Message);
			}

			return isMillitorr;
		}

		/// <summary>
		/// Examines the pressure columns to determine whether they are in torr or mTorr
		/// </summary>
		internal void DeterminePressureUnits()
		{
			try
			{
				PressureIsMilliTorr = false;

				SQLiteCommand cmd = new SQLiteCommand(m_uimfDatabaseConnection);

				bool isMilliTorr = ColumnIsMilliTorr(cmd, "Frame_Parameters", "HighPressureFunnelPressure");
				if (isMilliTorr)
				{
				    PressureIsMilliTorr = true;
				    return;
				}

                isMilliTorr = ColumnIsMilliTorr(cmd, "Frame_Parameters", "PressureBack");
                if (isMilliTorr)
                {
                    PressureIsMilliTorr = true;
                    return;
                }

              
				isMilliTorr = ColumnIsMilliTorr(cmd, "Frame_Parameters", "IonFunnelTrapPressure");
				if (isMilliTorr)
				{
				    PressureIsMilliTorr = true;
				    return;
				}

				isMilliTorr = ColumnIsMilliTorr(cmd, "Frame_Parameters", "RearIonFunnelPressure");
				if (isMilliTorr) PressureIsMilliTorr = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception determining whether pressure columns are in milliTorr: " + ex.Message);
			}
		}

		/// <summary>
		/// Determines if the MS1 Frames of this file are labeled as 0 or 1. 
		/// Note that MS1 frames should recorded as '1'. But we need to
		/// support legacy UIMF files which have values of '0' for MS1. 
		/// The determined value is stored in a class-wide variable for later use.
		/// Exception is thrown if both 0 and 1 are found.
		/// </summary>
		private void DetermineFrameTypes()
		{
			List<int> frameTypeList = new List<int>();

			using (SQLiteCommand dbCmd = m_uimfDatabaseConnection.CreateCommand())
			{
				dbCmd.CommandText = "SELECT DISTINCT(FrameType) FROM Frame_Parameters";
				dbCmd.Prepare();
				using (SQLiteDataReader reader = dbCmd.ExecuteReader())
				{
					while (reader.Read())
					{
						frameTypeList.Add(Convert.ToInt32(reader["FrameType"]));
					}
				}
			}

			if(frameTypeList.Contains(0))
			{
				if(frameTypeList.Contains(1))
				{
					throw new Exception("FrameTypes of 0 and 1 found. Not a valid UIMF file.");
				}

				m_frameTypeMs = 0;
			}
			else
			{
				m_frameTypeMs = 1;
			}
		}

		/// <summary>
		/// This will fill out information about each frame type
		/// </summary>
		private void FillOutFrameInfo()
		{
			if (m_frameTypeInfo.Any()) return;

			int[] ms1FrameNumbers = GetFrameNumbers(FrameType.MS1);
			FrameTypeInfo ms1FrameTypeInfo = new FrameTypeInfo(m_globalParameters.NumFrames);
			foreach (int ms1FrameNumber in ms1FrameNumbers)
			{
				ms1FrameTypeInfo.AddFrame(ms1FrameNumber);
			}

			int[] ms2FrameNumbers = GetFrameNumbers(FrameType.MS2);
			FrameTypeInfo ms2FrameTypeInfo = new FrameTypeInfo(m_globalParameters.NumFrames);
			foreach (int ms2FrameNumber in ms2FrameNumbers)
			{
				ms2FrameTypeInfo.AddFrame(ms2FrameNumber);
			}

			m_frameTypeInfo.Add(FrameType.MS1, ms1FrameTypeInfo);
			m_frameTypeInfo.Add(FrameType.MS2, ms2FrameTypeInfo);
		}

		/// <summary>
		/// Returns the bin value that corresponds to an m/z value.  
		/// NOTE: this may not be accurate if the UIMF file uses polynomial calibration values  (eg.  FrameParameter A2)
		/// </summary>
		/// <param name="slope"></param>
		/// <param name="intercept"></param>
		/// <param name="binWidth"></param>
		/// <param name="correctionTimeForTOF"></param>
		/// <param name="targetMZ"></param>
		/// <returns></returns>
		public static double GetBinClosestToMZ(double slope, double intercept, double binWidth, double correctionTimeForTOF, double targetMZ)
		{
			//NOTE: this may not be accurate if the UIMF file uses polynomial calibration values  (eg.  FrameParameter A2)
			double binCorrection = (correctionTimeForTOF / 1000) / binWidth;
			double bin = (Math.Sqrt(targetMZ) / slope + intercept) / binWidth * 1000;
			//TODO:  have a test case with a TOFCorrectionTime > 0 and verify the binCorrection adjustment
			return bin + binCorrection;
		}

		public Dictionary<int, int>[] GetIntensityBlockOfFrame(int frameNumber)
		{
			FrameParameters frameParameters = GetFrameParameters(frameNumber);
			int numScans = frameParameters.Scans;
			FrameType frameType = frameParameters.FrameType;

			Dictionary<int, int>[] dictionaryArray = new Dictionary<int, int>[numScans];
			for (int i = 0; i < numScans; i++)
			{
				dictionaryArray[i] = new Dictionary<int, int>();
			}

			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum1", frameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum2", frameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum1", -1));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum2", numScans - 1));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));

			using (SQLiteDataReader reader = m_getSpectrumCommand.ExecuteReader())
			{
				byte[] decompSpectraRecord = new byte[m_globalParameters.Bins * DATASIZE];

				while (reader.Read())
				{
					int binIndex = 0;

					byte[] spectra = (byte[])(reader["Intensities"]);
					int scanNum = Convert.ToInt32(reader["ScanNum"]);

					Dictionary<int, int> currentBinDictionary = dictionaryArray[scanNum];

					//get frame number so that we can get the frame calibration parameters
					if (spectra.Length > 0)
					{
						int outputLength = LZFCompressionUtil.Decompress(ref spectra, spectra.Length, ref decompSpectraRecord, m_globalParameters.Bins * DATASIZE);
						int numBins = outputLength / DATASIZE;
						for (int i = 0; i < numBins; i++)
						{
							int decodedIntensityValue = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
							if (decodedIntensityValue < 0)
							{
								binIndex += -decodedIntensityValue;
							}
							else
							{
								currentBinDictionary.Add(binIndex, decodedIntensityValue);
								binIndex++;
							}
						}
					}
				}
			}

			m_getSpectrumCommand.Parameters.Clear();

			return dictionaryArray;
		}

		public int[][][] GetIntensityBlock(int startFrameNumber, int endFrameNumber, FrameType frameType, int startScan, int endScan, int startBin, int endBin)
		{
			if (startBin < 0)
			{
				startBin = 0;
			}

			if (endBin > m_globalParameters.Bins)
			{
				endBin = m_globalParameters.Bins;
			}

			int lengthOfFrameArray = (endFrameNumber - startFrameNumber + 1);

			int[][][] intensities = new int[lengthOfFrameArray][][];
			for (int i = 0; i < lengthOfFrameArray; i++)
			{
				intensities[i] = new int[endScan - startScan + 1][];
				for (int j = 0; j < endScan - startScan + 1; j++)
				{
					intensities[i][j] = new int[endBin - startBin + 1];
				}
			}

			//now setup queries to retrieve data (April 2011 Note: there is probably a better query method for this)
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum1", startFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameNum2", endFrameNumber));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum1", startScan));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("ScanNum2", endScan));
			m_getSpectrumCommand.Parameters.Add(new SQLiteParameter("FrameType", (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType)));
			
			using (SQLiteDataReader reader = m_getSpectrumCommand.ExecuteReader())
			{
				byte[] decompSpectraRecord = new byte[m_globalParameters.Bins * DATASIZE];

				while (reader.Read())
				{
					int frameNum = Convert.ToInt32(reader["FrameNum"]);
					int binIndex = 0;

					byte[] spectra = (byte[])(reader["Intensities"]);
					int scanNum = Convert.ToInt32(reader["ScanNum"]);

					//get frame number so that we can get the frame calibration parameters
					if (spectra.Length > 0)
					{
						int outputLength = LZFCompressionUtil.Decompress(ref spectra, spectra.Length, ref decompSpectraRecord, m_globalParameters.Bins * DATASIZE);
						int numBins = outputLength / DATASIZE;
						for (int i = 0; i < numBins; i++)
						{
							int decodedIntensityValue = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
							if (decodedIntensityValue < 0)
							{
								binIndex += -decodedIntensityValue;
							}
							else
							{
								if (startBin <= binIndex && binIndex <= endBin)
								{
									intensities[frameNum - startFrameNumber][scanNum - startScan][binIndex - startBin] = decodedIntensityValue;
								}
								binIndex++;
							}
						}
					}
				}
			}

			m_getSpectrumCommand.Parameters.Clear();

			return intensities;
		}

		public double[,] GetXicAsArray(double targetMz, double tolerance, FrameType frameType, ToleranceType toleranceType)
		{
			FrameParameters frameParameters = GetFrameParameters(1);
			double slope = frameParameters.CalibrationSlope;
			double intercept = frameParameters.CalibrationIntercept;
			double binWidth = m_globalParameters.BinWidth;
			float tofCorrectionTime = m_globalParameters.TOFCorrectionTime;
			int numScans = frameParameters.Scans;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int numFrames = frameTypeInfo.NumFrames;
			int[] frameIndexes = frameTypeInfo.FrameIndexes;

			double[,] result = new double[numFrames, numScans];

			double mzTolerance = toleranceType == ToleranceType.Thomson ? tolerance : (targetMz / 1000000 * tolerance);
			double lowMz = targetMz - mzTolerance;
			double highMz = targetMz + mzTolerance;

			int startBin = (int)Math.Floor(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
			int endBin = (int)Math.Ceiling(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", startBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", endBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScans, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Add intensity to the result
							int frameIndex = frameIndexes[scanLc];
							result[frameIndex, scanIms] += decodedSpectraRecord;
						}
					}
				}
			}

			return result;
		}

		public double[,] GetXicAsArray(double targetMz, double tolerance, int frameIndexMin, int frameIndexMax, int scanMin, int scanMax, FrameType frameType, ToleranceType toleranceType)
		{
			FrameParameters frameParameters = GetFrameParameters(frameIndexMin);
			double slope = frameParameters.CalibrationSlope;
			double intercept = frameParameters.CalibrationIntercept;
			double binWidth = m_globalParameters.BinWidth;
			float tofCorrectionTime = m_globalParameters.TOFCorrectionTime;
			int numScansInFrame = frameParameters.Scans;
			int numScans = scanMax - scanMin + 1;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int[] frameIndexes = frameTypeInfo.FrameIndexes;
			int numFrames = frameIndexMax - frameIndexMin + 1;

			double[,] result = new double[numFrames, numScans];

			double mzTolerance = toleranceType == ToleranceType.Thomson ? tolerance : (targetMz / 1000000 * tolerance);
			double lowMz = targetMz - mzTolerance;
			double highMz = targetMz + mzTolerance;

			int startBin = (int)Math.Floor(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
			int endBin = (int)Math.Ceiling(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", startBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", endBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScansInFrame, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Get the frame index
							int frameIndex = frameIndexes[scanLc];

							// We can stop after we get past the max frame number given
							if (frameIndex > frameIndexMax) break;

							// Skip all frames and scans that we do not care about
							if (frameIndex < frameIndexMin || scanIms < scanMin || scanIms > scanMax) continue;

							// Add intensity to the result
							result[frameIndex - frameIndexMin, scanIms - scanMin] += decodedSpectraRecord;
						}
					}
				}
			}

			return result;
		}

		public double[,] GetXicAsArray(int targetBin, FrameType frameType)
		{
			FrameParameters frameParameters = GetFrameParameters(1);
			int numScans = frameParameters.Scans;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int numFrames = frameTypeInfo.NumFrames;
			int[] frameIndexes = frameTypeInfo.FrameIndexes;

			double[,] result = new double[numFrames, numScans];

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", targetBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", targetBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScans, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Add intensity to the result
							int frameIndex = frameIndexes[scanLc];
							result[frameIndex, scanIms] += decodedSpectraRecord;
						}
					}
				}
			}

			return result;
		}

		public List<IntensityPoint> GetXic(int targetBin, FrameType frameType)
		{
			FrameParameters frameParameters = GetFrameParameters(1);
			int numScans = frameParameters.Scans;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int[] frameIndexes = frameTypeInfo.FrameIndexes;

			List<IntensityPoint> intensityList = new List<IntensityPoint>();

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", targetBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", targetBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScans, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Add intensity to the result
							int frameIndex = frameIndexes[scanLc];
							intensityList.Add(new IntensityPoint(frameIndex, scanIms, decodedSpectraRecord));
						}
					}
				}
			}

			return intensityList;
		}

		public List<IntensityPoint> GetXic(double targetMz, double tolerance, FrameType frameType, ToleranceType toleranceType)
		{
			FrameParameters frameParameters = GetFrameParameters(1);
			double slope = frameParameters.CalibrationSlope;
			double intercept = frameParameters.CalibrationIntercept;
			double binWidth = m_globalParameters.BinWidth;
			float tofCorrectionTime = m_globalParameters.TOFCorrectionTime;
			int numScans = frameParameters.Scans;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int[] frameIndexes = frameTypeInfo.FrameIndexes;

			double mzTolerance = toleranceType == ToleranceType.Thomson ? tolerance : (targetMz / 1000000 * tolerance);
			double lowMz = targetMz - mzTolerance;
			double highMz = targetMz + mzTolerance;

			int startBin = (int)Math.Floor(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
			int endBin = (int)Math.Ceiling(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

			Dictionary<IntensityPoint, IntensityPoint> pointDictionary = new Dictionary<IntensityPoint, IntensityPoint>();
			IntensityPoint dictionaryValue;

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", startBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", endBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScans, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Add intensity to the result
							int frameIndex = frameIndexes[scanLc];
							IntensityPoint newPoint = new IntensityPoint(frameIndex, scanIms, decodedSpectraRecord);

							if (pointDictionary.TryGetValue(newPoint, out dictionaryValue))
							{
								dictionaryValue.Intensity += decodedSpectraRecord;
							}
							else
							{
								pointDictionary.Add(newPoint, newPoint);
							}
						}
					}
				}
			}

			return pointDictionary.Values.OrderBy(x => x.ScanLc).ThenBy(x => x.ScanIms).ToList();
		}

		public List<IntensityPoint> GetXic(double targetMz, double tolerance, int frameIndexMin, int frameIndexMax, int scanMin, int scanMax, FrameType frameType, ToleranceType toleranceType)
		{
			FrameParameters frameParameters = GetFrameParameters(1);
			double slope = frameParameters.CalibrationSlope;
			double intercept = frameParameters.CalibrationIntercept;
			double binWidth = m_globalParameters.BinWidth;
			float tofCorrectionTime = m_globalParameters.TOFCorrectionTime;
			int numScans = frameParameters.Scans;

			FrameTypeInfo frameTypeInfo = m_frameTypeInfo[frameType];
			int[] frameIndexes = frameTypeInfo.FrameIndexes;

			double mzTolerance = toleranceType == ToleranceType.Thomson ? tolerance : (targetMz / 1000000 * tolerance);
			double lowMz = targetMz - mzTolerance;
			double highMz = targetMz + mzTolerance;

			int startBin = (int)Math.Floor(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
			int endBin = (int)Math.Ceiling(GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

			Dictionary<IntensityPoint, IntensityPoint> pointDictionary = new Dictionary<IntensityPoint, IntensityPoint>();
			IntensityPoint dictionaryValue;

			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMin", startBin));
			m_getBinDataCommand.Parameters.Add(new SQLiteParameter("BinMax", endBin));

			using (SQLiteDataReader reader = m_getBinDataCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					int entryIndex = 0;
					int scanLc = 0;
					int scanIms = 0;

					byte[] decompSpectraRecord = (byte[])(reader["INTENSITIES"]);
					int numPossibleRecords = decompSpectraRecord.Length / DATASIZE;

					for (int i = 0; i < numPossibleRecords; i++)
					{
						int decodedSpectraRecord = BitConverter.ToInt32(decompSpectraRecord, i * DATASIZE);
						if (decodedSpectraRecord < 0)
						{
							entryIndex += -decodedSpectraRecord;
						}
						else
						{
							// Increment the entry index BEFORE storing the data so that we use the correct index (instead of having all indexes off by 1)
							entryIndex++;

							// Calculate LC Scan and IMS Scan of this entry
							CalculateFrameAndScanForEncodedIndex(entryIndex, numScans, out scanLc, out scanIms);

							// Skip FrameTypes that do not match the given FrameType
							if (GetFrameParameters(scanLc).FrameType != frameType) continue;

							// Get the frame index
							int frameIndex = frameIndexes[scanLc];

							// We can stop after we get past the max frame number given
							if (frameIndex > frameIndexMax) break;

							// Skip all frames and scans that we do not care about
							if (frameIndex < frameIndexMin || scanIms < scanMin || scanIms > scanMax) continue;

							IntensityPoint newPoint = new IntensityPoint(frameIndex, scanIms, decodedSpectraRecord);

							if (pointDictionary.TryGetValue(newPoint, out dictionaryValue))
							{
								dictionaryValue.Intensity += decodedSpectraRecord;
							}
							else
							{
								pointDictionary.Add(newPoint, newPoint);
							}
						}
					}
				}
			}

			return pointDictionary.Values.OrderBy(x => x.ScanLc).ThenBy(x => x.ScanIms).ToList();
		}

		/// <summary>
		/// Get TIC or BPI for scans of given frame type in given frame range
		/// Optionally filter on scan range
		/// </summary>
		/// <param name="frameType"></param>
		/// <param name="startFrameNumber"></param>
		/// <param name="endFrameNumber"></param>
		/// <param name="startScan"></param>
		/// <param name="endScan"></param>
		/// <param name="fieldName"></param>
		private double[] GetTicOrBpi(FrameType frameType, int startFrameNumber, int endFrameNumber, int startScan, int endScan, string fieldName)
		{
			Dictionary<int, double> dctTicOrBPI = GetTicOrBpiByFrame(startFrameNumber, endFrameNumber, startScan, endScan, fieldName, filterByFrameType: true, frameType:frameType);

			double[] data = new double[dctTicOrBPI.Count];
			
			int index = 0;
			foreach (double Value in dctTicOrBPI.Values)
			{
				data[index] = Value;
				index++;
			}

			return data;
		}

		/// <summary>
		/// Get TIC or BPI for scans of given frame type in given frame range
		/// Optionally filter on scan range
		/// </summary>
		/// <param name="frameType"></param>
		/// <param name="startFrameNumber"></param>
		/// <param name="endFrameNumber"></param>
		/// <param name="startScan"></param>
		/// <param name="endScan"></param>
		/// <param name="fieldName"></param>
		/// <returns>Dictionary where keys are frame number and values are the TIC or BPI value</returns>
		private Dictionary<int, double> GetTicOrBpiByFrame(int startFrameNumber, int endFrameNumber, int startScan, int endScan, string fieldName, bool filterByFrameType, FrameType frameType)
		{
			// Make sure endFrame is valid
			if (endFrameNumber < startFrameNumber)
			{
				endFrameNumber = startFrameNumber;
			}

			Dictionary<int, double> dctTicOrBPI = new Dictionary<int, double>();

			// Construct the SQL
			string sql = " SELECT Frame_Scans.FrameNum, Sum(Frame_Scans." + fieldName + ") AS Value " +
						 " FROM Frame_Scans INNER JOIN Frame_Parameters ON Frame_Scans.FrameNum = Frame_Parameters.FrameNum ";

			string whereClause = string.Empty;

			if (!(startFrameNumber == 0 && endFrameNumber == 0))
			{
				// Filter by frame number
				whereClause = "Frame_Parameters.FrameNum >= " + startFrameNumber + " AND " + "Frame_Parameters.FrameNum <= " + endFrameNumber;
			}

			if (filterByFrameType)
			{
				// Filter by frame type
				if (!string.IsNullOrEmpty(whereClause)) whereClause += " AND ";
				whereClause += "Frame_Parameters.FrameType = " + (frameType.Equals(FrameType.MS1) ? m_frameTypeMs : (int)frameType);
			}

			if (!(startScan == 0 && endScan == 0))
			{
				// Filter by scan number
				if (!string.IsNullOrEmpty(whereClause)) whereClause += " AND ";
				whereClause += "Frame_Scans.ScanNum >= " + startScan + " AND Frame_Scans.ScanNum <= " + endScan;
			}

			if (!string.IsNullOrEmpty(whereClause))
			{
				sql += " WHERE " + whereClause;
			}

			sql += " GROUP BY Frame_Scans.FrameNum ORDER BY Frame_Scans.FrameNum";

			using (SQLiteCommand dbcmdUIMF = m_uimfDatabaseConnection.CreateCommand())
			{
				dbcmdUIMF.CommandText = sql;
				using (SQLiteDataReader reader = dbcmdUIMF.ExecuteReader())
				{					
					while (reader.Read())
					{
						dctTicOrBPI.Add(Convert.ToInt32(reader["FrameNum"]), Convert.ToDouble(reader["Value"]));
					}
				}
			}

			return dctTicOrBPI;
		}

		private int[] GetUpperLowerBinsFromMz(int frameNumber, double targetMZ, double toleranceInMZ)
		{
			int[] bins = new int[2];
			double lowerMZ = targetMZ - toleranceInMZ;
			double upperMZ = targetMZ + toleranceInMZ;
			FrameParameters fp = GetFrameParameters(frameNumber);
			GlobalParameters gp = GetGlobalParameters();
			bool polynomialCalibrantsAreUsed = (Math.Abs(fp.a2 - 0) > float.Epsilon || 
												Math.Abs(fp.b2 - 0) > float.Epsilon || 
												Math.Abs(fp.c2 - 0) > float.Epsilon || 
												Math.Abs(fp.d2 - 0) > float.Epsilon || 
												Math.Abs(fp.e2 - 0) > float.Epsilon || 
												Math.Abs(fp.f2 - 0) > float.Epsilon);
			if (polynomialCalibrantsAreUsed)
			{
				//note: the reason for this is that we are trying to get the closest bin for a given m/z.  But when a polynomial formula is used to adjust the m/z, it gets
				// much more complicated.  So someone else can figure that out  :)
				throw new NotImplementedException("DriftTime profile extraction hasn't been implemented for UIMF files containing polynomial calibration constants.");
			}

			double lowerBin = GetBinClosestToMZ(fp.CalibrationSlope, fp.CalibrationIntercept, gp.BinWidth, gp.TOFCorrectionTime, lowerMZ);
			double upperBin = GetBinClosestToMZ(fp.CalibrationSlope, fp.CalibrationIntercept, gp.BinWidth, gp.TOFCorrectionTime, upperMZ);
			bins[0] = (int)Math.Round(lowerBin, 0);
			bins[1] = (int)Math.Round(upperBin, 0);
			return bins;
		}

		private void LoadPrepStmts()
		{
			m_getFileBytesCommand = m_uimfDatabaseConnection.CreateCommand();

			m_getFrameNumbers = m_uimfDatabaseConnection.CreateCommand();
			m_getFrameNumbers.CommandText = "SELECT FrameNum FROM Frame_Parameters WHERE FrameType = :FrameType";
			m_getFrameNumbers.Prepare();

			m_getFrameParametersCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getFrameParametersCommand.CommandText = "SELECT * FROM Frame_Parameters WHERE FrameNum = :FrameNum"; // FrameType not necessary
			m_getFrameParametersCommand.Prepare();

			// Table: Frame_Scans
			m_sumVariableScansPerFrameCommand = m_uimfDatabaseConnection.CreateCommand();

			m_getFramesAndScanByDescendingIntensityCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getFramesAndScanByDescendingIntensityCommand.CommandText = "SELECT FrameNum, ScanNum, BPI FROM Frame_Scans ORDER BY BPI";
			m_getFramesAndScanByDescendingIntensityCommand.Prepare();

			m_getSpectrumCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getSpectrumCommand.CommandText = "SELECT FS.ScanNum, FS.FrameNum, FS.Intensities FROM Frame_Scans FS JOIN Frame_Parameters FP ON (FS.FrameNum = FP.FrameNum) WHERE FS.FrameNum >= :FrameNum1 AND FS.FrameNum <= :FrameNum2 AND FS.ScanNum >= :ScanNum1 AND FS.ScanNum <= :ScanNum2 AND FP.FrameType = :FrameType";
			m_getSpectrumCommand.Prepare();

			m_getCountPerSpectrumCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getCountPerSpectrumCommand.CommandText = "SELECT NonZeroCount FROM Frame_Scans WHERE FrameNum = :FrameNum AND ScanNum = :ScanNum";
			m_getCountPerSpectrumCommand.Prepare();

			m_getCountPerFrameCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getCountPerFrameCommand.CommandText = "SELECT sum(NonZeroCount) FROM Frame_Scans WHERE FrameNum = :FrameNum AND NOT NonZeroCount IS NULL";
			m_getCountPerFrameCommand.Prepare();

			m_checkForBinCentricTableCommand = m_uimfDatabaseConnection.CreateCommand();
			m_checkForBinCentricTableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Bin_Intensities';";
			m_checkForBinCentricTableCommand.Prepare();

			m_getBinDataCommand = m_uimfDatabaseConnection.CreateCommand();
			m_getBinDataCommand.CommandText = "SELECT MZ_BIN, INTENSITIES FROM Bin_Intensities WHERE MZ_BIN >= :BinMin AND MZ_BIN <= :BinMax;";
			m_getBinDataCommand.Prepare();
		}

		private void PopulateFrameParameters(FrameParameters fp, SQLiteDataReader reader)
		{
			try
			{
				bool columnMissing;

				fp.FrameNum = Convert.ToInt32(reader["FrameNum"]);
				fp.StartTime = Convert.ToDouble(reader["StartTime"]);

				if (fp.StartTime > 1E+17)
				{
					// StartTime is stored as Ticks in this file
					// Auto-compute the correct start time
					DateTime dtRunStarted;
					if (DateTime.TryParse(m_globalParameters.DateStarted, out dtRunStarted))
					{
						Int64 lngTickDifference = (Int64)fp.StartTime - dtRunStarted.Ticks;
						if (lngTickDifference >= 0)
						{
							fp.StartTime = dtRunStarted.AddTicks(lngTickDifference).Subtract(dtRunStarted).TotalMinutes;
						}
					}
				}

				fp.Duration = Convert.ToDouble(reader["Duration"]);
				fp.Accumulations = Convert.ToInt32(reader["Accumulations"]);
				
				int frameTypeInt = Convert.ToInt16(reader["FrameType"]);

				// If the frametype is 0, then this is an older UIMF file where the MS1 frames were labeled as 0.
				if(frameTypeInt == 0)
				{
					fp.FrameType = FrameType.MS1;
				}
				else
				{
					fp.FrameType = (FrameType) frameTypeInt;
				}

				fp.Scans = Convert.ToInt32(reader["Scans"]);
				fp.IMFProfile = Convert.ToString(reader["IMFProfile"]);
				fp.TOFLosses = Convert.ToDouble(reader["TOFLosses"]);
				fp.AverageTOFLength = Convert.ToDouble(reader["AverageTOFLength"]);
				fp.CalibrationSlope = Convert.ToDouble(reader["CalibrationSlope"]);
				fp.CalibrationIntercept = Convert.ToDouble(reader["CalibrationIntercept"]);
				fp.Temperature = Convert.ToDouble(reader["Temperature"]);
				fp.voltHVRack1 = Convert.ToDouble(reader["voltHVRack1"]);
				fp.voltHVRack2 = Convert.ToDouble(reader["voltHVRack2"]);
				fp.voltHVRack3 = Convert.ToDouble(reader["voltHVRack3"]);
				fp.voltHVRack4 = Convert.ToDouble(reader["voltHVRack4"]);
				fp.voltCapInlet = Convert.ToDouble(reader["voltCapInlet"]);                // 14, Capillary Inlet Voltage

				fp.voltEntranceHPFIn = TryGetFrameParam(reader, "voltEntranceHPFIn", 0, out columnMissing); // 15, HPF In Voltage
				if (columnMissing)
				{
					// Legacy column names are present
					fp.voltEntranceHPFIn = TryGetFrameParam(reader, "voltEntranceIFTIn", 0);
					fp.voltEntranceHPFOut = TryGetFrameParam(reader, "voltEntranceIFTOut", 0);
				}
				else
				{
					fp.voltEntranceHPFOut = TryGetFrameParam(reader, "voltEntranceHPFOut", 0); // 16, HPF Out Voltage
				}

				fp.voltEntranceCondLmt = Convert.ToDouble(reader["voltEntranceCondLmt"]); // 17, Cond Limit Voltage
				fp.voltTrapOut = Convert.ToDouble(reader["voltTrapOut"]);                 // 18, Trap Out Voltage
				fp.voltTrapIn = Convert.ToDouble(reader["voltTrapIn"]);                   // 19, Trap In Voltage
				fp.voltJetDist = Convert.ToDouble(reader["voltJetDist"]);                 // 20, Jet Disruptor Voltage
				fp.voltQuad1 = Convert.ToDouble(reader["voltQuad1"]);                     // 21, Fragmentation Quadrupole Voltage
				fp.voltCond1 = Convert.ToDouble(reader["voltCond1"]);                     // 22, Fragmentation Conductance Voltage
				fp.voltQuad2 = Convert.ToDouble(reader["voltQuad2"]);                     // 23, Fragmentation Quadrupole Voltage
				fp.voltCond2 = Convert.ToDouble(reader["voltCond2"]);                     // 24, Fragmentation Conductance Voltage
				fp.voltIMSOut = Convert.ToDouble(reader["voltIMSOut"]);                   // 25, IMS Out Voltage

				fp.voltExitHPFIn = TryGetFrameParam(reader, "voltExitHPFIn", 0, out columnMissing); // 26, HPF In Voltage
				if (columnMissing)
				{
					// Legacy column names are present
					fp.voltExitHPFIn = TryGetFrameParam(reader, "voltExitIFTIn", 0);
					fp.voltExitHPFOut = TryGetFrameParam(reader, "voltExitIFTOut", 0);
				}
				else
				{
					fp.voltExitHPFOut = TryGetFrameParam(reader, "voltExitHPFOut", 0);      // 27, HPF Out Voltage
				}

				fp.voltExitCondLmt = Convert.ToDouble(reader["voltExitCondLmt"]);           // 28, Cond Limit Voltage
				fp.PressureFront = Convert.ToDouble(reader["PressureFront"]);
				fp.PressureBack = Convert.ToDouble(reader["PressureBack"]);
				fp.MPBitOrder = Convert.ToInt16(reader["MPBitOrder"]);
				fp.FragmentationProfile = ArrayFragmentationSequence((byte[])(reader["FragmentationProfile"]));

				fp.HighPressureFunnelPressure = TryGetFrameParam(reader, "HighPressureFunnelPressure", 0, out columnMissing);
				if (columnMissing)
				{
					if (m_errMessageCounter < 5)
					{
						Console.WriteLine("Warning: this UIMF file is created with an old version of IMF2UIMF (HighPressureFunnelPressure is missing from the Frame_Parameters table); please get the newest version from \\\\floyd\\software");
						m_errMessageCounter++;
					}
				}
				else
				{
					fp.IonFunnelTrapPressure = TryGetFrameParam(reader, "IonFunnelTrapPressure", 0);
					fp.RearIonFunnelPressure = TryGetFrameParam(reader, "RearIonFunnelPressure", 0);
					fp.QuadrupolePressure = TryGetFrameParam(reader, "QuadrupolePressure", 0);
					fp.ESIVoltage = TryGetFrameParam(reader, "ESIVoltage", 0);
					fp.FloatVoltage = TryGetFrameParam(reader, "FloatVoltage", 0);
					fp.CalibrationDone = TryGetFrameParamInt32(reader, "CalibrationDone", 0);
					fp.Decoded = TryGetFrameParamInt32(reader, "Decoded", 0);

					if (PressureIsMilliTorr)
					{
						// Divide each of the pressures by 1000 to convert from milliTorr to Torr
						fp.HighPressureFunnelPressure /= 1000.0;
						fp.IonFunnelTrapPressure /= 1000.0;
						fp.RearIonFunnelPressure /= 1000.0;
						fp.QuadrupolePressure /= 1000.0;
					}
				}


				fp.a2 = TryGetFrameParam(reader, "a2", 0, out columnMissing);
				if (columnMissing)
				{
					fp.b2 = 0;
					fp.c2 = 0;
					fp.d2 = 0;
					fp.e2 = 0;
					fp.f2 = 0;
					if (m_errMessageCounter < 5)
					{
						Console.WriteLine("Warning: this UIMF file is created with an old version of IMF2UIMF (b2 calibration column is missing from the Frame_Parameters table); please get the newest version from \\\\floyd\\software");
						m_errMessageCounter++;
					}
				}
				else
				{
					fp.b2 = TryGetFrameParam(reader, "b2", 0);
					fp.c2 = TryGetFrameParam(reader, "c2", 0);
					fp.d2 = TryGetFrameParam(reader, "d2", 0);
					fp.e2 = TryGetFrameParam(reader, "e2", 0);
					fp.f2 = TryGetFrameParam(reader, "f2", 0);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to access frame parameters table " + ex);
			}
		}

		private static void StripZerosFromArrays<T>(int nonZeroCount, ref T[] xDataArray, ref int[] yDataArray)
		{
			List<T> xArrayList = new List<T>(nonZeroCount);
			List<int> yArrayList = new List<int>(nonZeroCount);

			for (int i = 0; i < xDataArray.Length; i++)
			{
				int yDataPoint = yDataArray[i];

				if (yDataPoint > 0)
				{
					xArrayList.Add(xDataArray[i]);
					yArrayList.Add(yDataPoint);
				}
			}

			xDataArray = xArrayList.ToArray();
			yDataArray = yArrayList.ToArray();
		}

		private static double TryGetFrameParam(SQLiteDataReader reader, string columnName, double defaultValue)
		{
			bool columnMissing;
			return TryGetFrameParam(reader, columnName, defaultValue, out columnMissing);
		}

		private static double TryGetFrameParam(SQLiteDataReader reader, string columnName, double defaultValue, out bool columnMissing)
		{
			double result = defaultValue;
			columnMissing = false;

			try
			{
				result = !DBNull.Value.Equals(reader[columnName]) ? Convert.ToDouble(reader[columnName]) : defaultValue;
			}
			catch (IndexOutOfRangeException)
			{
				columnMissing = true;
			}

			return result;
		}

		private static int TryGetFrameParamInt32(SQLiteDataReader reader, string columnName, int defaultValue)
		{
			bool columnMissing;
			return TryGetFrameParamInt32(reader, columnName, defaultValue, out columnMissing);
		}

		private static int TryGetFrameParamInt32(SQLiteDataReader reader, string columnName, int defaultValue, out bool columnMissing)
		{
			int result = defaultValue;
			columnMissing = false;

			try
			{
				result = !DBNull.Value.Equals(reader[columnName]) ? Convert.ToInt32(reader[columnName]) : defaultValue;
			}
			catch (IndexOutOfRangeException)
			{
				columnMissing = true;
			}

			return result;
		}

		private void UnloadPrepStmts()
		{
			if (m_getCountPerSpectrumCommand != null) m_getCountPerSpectrumCommand.Dispose();

			if (m_getCountPerFrameCommand != null) m_getCountPerFrameCommand.Dispose();

			if (m_getFileBytesCommand != null) m_getFileBytesCommand.Dispose();

			if (m_getFrameNumbers != null) m_getFrameNumbers.Dispose();

			if (m_getFrameParametersCommand != null) m_getFrameParametersCommand.Dispose();

			if (m_getFramesAndScanByDescendingIntensityCommand != null) m_getFramesAndScanByDescendingIntensityCommand.Dispose();

			if (m_getSpectrumCommand != null) m_getSpectrumCommand.Dispose();

			if (m_sumVariableScansPerFrameCommand != null) m_sumVariableScansPerFrameCommand.Dispose();
		}

		#region IDisposable Members

		public void Dispose()
		{
			try
			{
				UnloadPrepStmts();

				if (m_uimfDatabaseConnection != null)
				{
					m_uimfDatabaseConnection.Close();
					m_uimfDatabaseConnection.Dispose();
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to close UIMF file " + ex);
			}
		}

		#endregion
	}

}
