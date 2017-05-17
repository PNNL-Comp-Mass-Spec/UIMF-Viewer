using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Lzf;

namespace UIMFLibrary
{
	class BinCentricTableCreation
	{
		private const int BIN_SIZE = 200;
		public const string INSERT_BIN_INTENSITIES = "INSERT INTO Bin_Intensities (MZ_BIN, INTENSITIES) VALUES(:MZ_BIN, :INTENSITIES)";
		public const string CREATE_BINS_TABLE = "CREATE TABLE Bin_Intensities (MZ_BIN int(11), INTENSITIES BLOB);";
		public const string CREATE_BINS_INDEX = "CREATE INDEX Bin_Intensities_MZ_BIN_IDX ON Bin_Intensities(MZ_BIN);";

		public static void CreateBinCentricTable(SQLiteConnection uimfWriterConnection, DataReader uimfReader)
		{
			// Create the temporary database
			string temporaryDatabaseLocation = CreateTemporaryDatabase(uimfReader);

			string connectionString = "Data Source=" + temporaryDatabaseLocation + ";";
			using (SQLiteConnection temporaryDatabaseConnection = new SQLiteConnection(connectionString))
			{
				temporaryDatabaseConnection.Open();

				// Write the bin centric tables to UIMF file
				InsertBinCentricData(uimfWriterConnection, temporaryDatabaseConnection, uimfReader);
			}

			// Delete the temporary database
			//File.Delete(temporaryDatabaseLocation);
		}

		private static void InsertBinCentricData(SQLiteConnection uimfWriterConnection, SQLiteConnection temporaryDatabaseConnection, DataReader uimfReader)
		{
			int numBins = uimfReader.GetGlobalParameters().Bins;
			int numImsScans = uimfReader.GetFrameParameters(1).Scans;

			// Create new table in the UIMF file that will be used to store bin-centric data
			CreateBinIntensitiesTable(uimfWriterConnection);

			using (SQLiteCommand insertCommand = new SQLiteCommand(INSERT_BIN_INTENSITIES, uimfWriterConnection))
			{
				insertCommand.Prepare();

				for (int i = 0; i <= numBins; i++)
				{
					SortDataForBin(temporaryDatabaseConnection, insertCommand, i, numImsScans);
				}
			}

			CreateBinIntensitiesIndex(uimfWriterConnection);
		}

		private static string CreateTemporaryDatabase(DataReader uimfReader)
		{
			FileInfo uimfFileInfo = new FileInfo(uimfReader.UimfFilePath);

			// Get location of new SQLite file
			string sqliteFileName = uimfFileInfo.Name.Replace(".UIMF", "_temporary.db3").Replace(".uimf", "_temporary.db3");
			FileInfo sqliteFile = new FileInfo(sqliteFileName);

			Console.WriteLine(DateTime.Now + " - Writing " + sqliteFile.FullName);

			// Create new SQLite file
			if (File.Exists(sqliteFile.FullName)) File.Delete(sqliteFile.FullName);
			string connectionString = "Data Source=" + sqliteFile.FullName + ";";

			// Get global UIMF information
			GlobalParameters globalParameters = uimfReader.GetGlobalParameters();
			int numFrames = globalParameters.NumFrames;
			int numBins = globalParameters.Bins;

			int tablesCreated = CreateBlankDatabase(sqliteFile.FullName, numBins);

			using (SQLiteConnection connection = new SQLiteConnection(connectionString))
			{
				connection.Open();

				Dictionary<int, SQLiteCommand> commandDictionary = new Dictionary<int, SQLiteCommand>();

				for (int i = 0; i <= numBins; i += BIN_SIZE)
				{
					string query = GetInsertIntensityQuery(i);
					SQLiteCommand sqlCommand = new SQLiteCommand(query, connection);
					sqlCommand.Prepare();
					commandDictionary.Add(i, sqlCommand);
				}

				using (SQLiteTransaction transaction = connection.BeginTransaction())
				{
					for (int frameNumber = 1; frameNumber <= numFrames; frameNumber++)
					{
						Console.WriteLine(DateTime.Now + " - Processing Frame: " + frameNumber);

						FrameParameters frameParameters = uimfReader.GetFrameParameters(frameNumber);
						int numScans = frameParameters.Scans;

						// Get data from UIMF file
						Dictionary<int, int>[] frameBinData = uimfReader.GetIntensityBlockOfFrame(frameNumber);

						for (int scanNumber = 0; scanNumber < numScans; scanNumber++)
						{
							Dictionary<int, int> scanData = frameBinData[scanNumber];

							foreach (KeyValuePair<int, int> kvp in scanData)
							{
								int binNumber = kvp.Key;
								int intensity = kvp.Value;
								int modValue = binNumber % BIN_SIZE;
								int minBin = binNumber - modValue;

								SQLiteCommand sqlCommand = commandDictionary[minBin];
								SQLiteParameterCollection parameters = sqlCommand.Parameters;
								parameters.Clear();
								parameters.Add(new SQLiteParameter(":MZ_BIN", binNumber));
								parameters.Add(new SQLiteParameter(":SCAN_LC", frameNumber));
								parameters.Add(new SQLiteParameter(":SCAN_IMS", scanNumber));
								parameters.Add(new SQLiteParameter(":INTENSITY", intensity));
								sqlCommand.ExecuteNonQuery();
							}
						}
					}

					transaction.Commit();
				}
			}

			Console.WriteLine(DateTime.Now + " - Indexing " + tablesCreated + " tables");

			CreateIndexes(sqliteFile.FullName, numBins);

			Console.WriteLine(DateTime.Now + " - Done");

			return sqliteFile.FullName;
		}

		private static int CreateBlankDatabase(string locationForNewDatabase, int numBins)
		{
			// Create new SQLite file
			FileInfo sqliteFile = new FileInfo(locationForNewDatabase);
			if (sqliteFile.Exists) sqliteFile.Delete();
			string connectionString = "Data Source=" + sqliteFile.FullName + ";";

			int tablesCreated = 0;

			using (SQLiteConnection connection = new SQLiteConnection(connectionString))
			{
				connection.Open();

				for (int i = 0; i <= numBins; i += BIN_SIZE)
				{
					using (SQLiteCommand sqlCommand = new SQLiteCommand(GetCreateIntensitiesTableQuery(i), connection))
					{
						sqlCommand.ExecuteNonQuery();
					}
					tablesCreated++;
				}
			}

			return tablesCreated;
		}

		private static void CreateIndexes(string locationForNewDatabase, int numBins)
		{
			FileInfo sqliteFile = new FileInfo(locationForNewDatabase);
			string connectionString = "Data Source=" + sqliteFile.FullName + ";";

			using (SQLiteConnection connection = new SQLiteConnection(connectionString))
			{
				connection.Open();

				for (int i = 0; i <= numBins; i += BIN_SIZE)
				{
					using (SQLiteCommand sqlCommand = new SQLiteCommand(GetCreateIndexesQuery(i), connection))
					{
						sqlCommand.ExecuteNonQuery();
					}
				}
			}
		}

		private static void SortDataForBin(SQLiteConnection inConnection, SQLiteCommand insertCommand, int binNumber, int numImsScans)
		{
			List<int> runLengthZeroEncodedData = new List<int>();
			insertCommand.Parameters.Clear();

			string query = GetReadSingleBinQuery(binNumber);

			using (SQLiteCommand readCommand = new SQLiteCommand(query, inConnection))
			{
				using (SQLiteDataReader reader = readCommand.ExecuteReader())
				{
					int previousLocation = 0;

					while (reader.Read())
					{
						int scanLc = Convert.ToInt32(reader[1]);
						int scanIms = Convert.ToInt32(reader[2]);
						int intensity = Convert.ToInt32(reader[3]);

						int newLocation = (scanLc * numImsScans) + scanIms;
						int difference = newLocation - previousLocation - 1;

						// Add the negative difference if greater than 0 to represent a number of scans without data
						if(difference > 0)
						{
							runLengthZeroEncodedData.Add(-difference);
						}

						// Add the intensity value for this particular scan
						runLengthZeroEncodedData.Add(intensity);

						previousLocation = newLocation;
					}
				}
			}

			int dataCount = runLengthZeroEncodedData.Count;

			if (dataCount > 0)
			{
				//byte[] compressedRecord = new byte[dataCount * 4 * 5];
				byte[] byteBuffer = new byte[dataCount * 4];
				Buffer.BlockCopy(runLengthZeroEncodedData.ToArray(), 0, byteBuffer, 0, dataCount * 4);
				//int nlzf = LZFCompressionUtil.Compress(ref byteBuffer, dataCount * 4, ref compressedRecord, compressedRecord.Length);
				//byte[] spectra = new byte[nlzf];
				//Array.Copy(compressedRecord, spectra, nlzf);

				insertCommand.Parameters.Add(new SQLiteParameter(":MZ_BIN", binNumber));
				insertCommand.Parameters.Add(new SQLiteParameter(":INTENSITIES", byteBuffer));

				insertCommand.ExecuteNonQuery();
			}
		}

		private static string GetInsertIntensityQuery(int binNumber)
		{
			int minBin, maxBin;
			GetMinAndMaxBin(binNumber, out minBin, out maxBin);

			return "INSERT INTO Bin_Intensities_" + minBin + "_" + maxBin + " (MZ_BIN, SCAN_LC, SCAN_IMS, INTENSITY)" +
				"VALUES (:MZ_BIN, :SCAN_LC, :SCAN_IMS, :INTENSITY);";
		}

		private static string GetCreateIntensitiesTableQuery(int binNumber)
		{
			int minBin, maxBin;
			GetMinAndMaxBin(binNumber, out minBin, out maxBin);

			return "CREATE TABLE Bin_Intensities_" + minBin + "_" + maxBin + " (" +
				"MZ_BIN    int(11)," +
				"SCAN_LC    int(11)," +
				"SCAN_IMS   int(11)," +
				"INTENSITY  int(11));";
		}

		private static string GetCreateIndexesQuery(int binNumber)
		{
			int minBin, maxBin;
			GetMinAndMaxBin(binNumber, out minBin, out maxBin);

			return "CREATE INDEX Bin_Intensities_" + minBin + "_" + maxBin + "_MZ_BIN_SCAN_LC_SCAN_IMS_IDX ON Bin_Intensities_" + minBin + "_" + maxBin + " (MZ_BIN, SCAN_LC, SCAN_IMS);";
		}

		private static string GetReadSingleBinQuery(int binNumber)
		{
			int minBin, maxBin;
			GetMinAndMaxBin(binNumber, out minBin, out maxBin);

			return "SELECT * FROM Bin_Intensities_" + minBin + "_" + maxBin + " WHERE MZ_BIN = " + binNumber + " ORDER BY SCAN_LC, SCAN_IMS;";
		}

		private static void GetMinAndMaxBin(int binNumber, out int minBin, out int maxBin)
		{
			int modValue = binNumber % BIN_SIZE;
			minBin = binNumber - modValue;
			maxBin = binNumber + (BIN_SIZE - modValue - 1);
		}

		private static void CreateBinIntensitiesTable(SQLiteConnection uimfWriterConnection)
		{
			using (SQLiteCommand command = new SQLiteCommand(CREATE_BINS_TABLE, uimfWriterConnection))
			{
				command.ExecuteNonQuery();
			}
		}

		private static void CreateBinIntensitiesIndex(SQLiteConnection uimfWriterConnection)
		{
			using (SQLiteCommand command = new SQLiteCommand(CREATE_BINS_INDEX, uimfWriterConnection))
			{
				command.ExecuteNonQuery();
			}
		}
	}
}
