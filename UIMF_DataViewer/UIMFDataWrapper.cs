using System;
using System.Data.SQLite;
using System.Linq;
using UIMFLibrary;

namespace UIMF_File
{
    public class UIMFDataWrapper : UIMFLibrary.DataReader
    {
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // Viewer functionality
        //
        // William Danielson
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        //
        private double[] calibrationTable;

        public string UimfDataFile { get; }
        public UIMFLibrary.GlobalParams UimfGlobalParams => GetGlobalParams();
        public UIMFLibrary.FrameParams UimfFrameParams { get; private set; }
        public int CurrentFrameNum { get; private set; }

        public int CurrentFrameIndex;
        public int FrameWidth;

        private int currentFrameType;
        public int[] ArrayFrameNum { get; private set; } = new int[0];

        private double[] defaultFragVoltages = (double[]) null;

        public UIMFLibrary.MzCalibrator MzCalibration { get; private set; }

        public UIMFDataWrapper(string uimfFile)
            : base(uimfFile)
        {
            this.UimfDataFile = uimfFile;

            this.defaultFragVoltages = this.GetDefaultFragVoltages();

            this.currentFrameType = this.get_FrameType();
            this.CurrentFrameIndex = 0;

            this.FrameWidth = 1;
        }

        public string FrameTypeDescription(int frameType)
        {
            if (frameType == 0)
                return "All Frames";
            else
                return FrameTypeDescription((FrameType)frameType);
        }

        public double[] GetDefaultFragVoltages()
        {
            if (defaultFragVoltages == null)
            {
                // get the MS1 frames and use the fragmentation voltages on that.
                int current_frame_type = get_FrameType();
                int num_frames = this.set_FrameType(1);

                // if there are MS1 frames, set the default voltages.
                if (num_frames > 0)
                {
                    this.LoadFrame(0); // will be a MS frame

                    if (this.UimfFrameParams.HasParameter(FrameParamKeyType.FragmentationProfile))
                    {
                        var fragSequence = FrameParamUtilities.ConvertByteArrayToFragmentationSequence(Convert.FromBase64String(this.UimfFrameParams.GetValueString(FrameParamKeyType.FragmentationProfile)));
                        if (fragSequence.Length == 4)
                            defaultFragVoltages = fragSequence;
                        else
                            defaultFragVoltages = (double[]) null;
                    }
                }

                // reset the frame type.
                this.set_FrameType(current_frame_type);
            }

            return defaultFragVoltages;
        }

        public int MapBinCalibration(int currentBin, double newSlope, double newIntercept)
        {
            double mz = this.MzCalibration.TOFtoMZ(((double) currentBin) * this.TenthsOfNanoSecondsPerBin);
            double r = (Math.Sqrt(mz));

            return (int)((((r / newSlope) + newIntercept) * this.m_globalParameters.BinWidth * 10000.0 / this.TenthsOfNanoSecondsPerBin) + .5); // .5 for rounding
        }

        /// <summary>
        /// Update the calibration coefficients for a single frame
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <param name="autoCalibrating"></param>
        public void UpdateCalibrationCoefficients(int frameIndex, float slope, float intercept, bool autoCalibrating = false)
        {
            if (frameIndex > this.ArrayFrameNum.Length)
                return;

            using (var writer = new DataWriter(this.UimfDataFile))
            {
                writer.UpdateCalibrationCoefficients(this.ArrayFrameNum[frameIndex], slope, intercept, autoCalibrating);
            }

            // Make sure the mz_Calibration object is up-to-date
            // These values will likely also get updated via the call to reset_FrameParameters (which then calls GetFrameParameters)
            //this.MzCalibration.K = slope / 10000.0;
            //this.MzCalibration.T0 = intercept * 10000.0;

            this.ReloadFrameParameters();
        }

        /// <summary>
        /// /// Update the calibration coefficients for all frames
        /// </summary>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <param name="autoCalibrating"></param>
        public void UpdateAllCalibrationCoefficients(float slope, float intercept, bool autoCalibrating = false)
        {
            using (var writer = new DataWriter(this.UimfDataFile))
            {
                writer.UpdateAllCalibrationCoefficients(slope, intercept, autoCalibrating);
            }

            this.ReloadFrameParameters();
        }

        public void ReloadFrameParameters()
        {
            ClearFrameParametersCache();
            this.UimfFrameParams = this.GetFrameParams(this.ArrayFrameNum[this.CurrentFrameIndex]);
            this.MzCalibration = GetMzCalibrator(UimfFrameParams);
        }

        public void ClearFrameParametersCache()
        {
            this.m_CachedFrameParameters.Clear();
        }


        /// <summary>
        /// Returns the frame index for the given frame number
        /// Only searches frame numbers of the current frame type (get_FrameType)
        /// </summary>
        /// <param name="frameNumber"></param>
        /// <returns>Frame Index if found; otherwise; a negative number if not a valid frame number</returns>
        public int GetFrameIndex(int frameNumber)
        {
            return Array.BinarySearch(this.ArrayFrameNum, frameNumber);
        }

        /// <summary>
        /// Returns the current frame type
        /// </summary>
        /// <returns></returns>
        public int get_FrameType()
        {
            return this.currentFrameType;
        }

        /// <summary>
        /// Set the frame type (using enum FrameType)
        /// </summary>
        /// <param name="eFrameType">Frame type to set; see FrameType for types</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int set_FrameType(int eFrameType)
        {
            bool force_reload = false;
            return set_FrameType(eFrameType, force_reload);
        }

        /// <summary>
        /// Set the frame type
        /// </summary>
        /// <param name="frame_type">Frame type to set; see FrameType for types</param>
        /// <param name="force_reload">True to force a re-load of the data from the Frame_Parameters table</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int set_FrameType(int frame_type, bool force_reload)
        {
            int frame_count;
            int i;

            // If the frame type is already correct, then we don't need to re-query the database
            if ((this.currentFrameType == frame_type) && !force_reload)
                return this.ArrayFrameNum.Length;

            currentFrameType = frame_type;

            var preparedStatement = this.m_dbConnection.CreateCommand();
            if (frame_type == 0)
                preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters";
            else
                preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters WHERE FrameType = " + this.currentFrameType.ToString();
            var sqliteDataReader = preparedStatement.ExecuteReader();

            sqliteDataReader.Read();
            frame_count = Convert.ToInt32(sqliteDataReader[0]);
            sqliteDataReader.Dispose();

            if (frame_count == 0)
            {
                this.ArrayFrameNum = new int[0];
                return 0;
            }

            // build an array of frame numbers for instant referencing.
            this.ArrayFrameNum = new int[frame_count];
            preparedStatement.Dispose();

            preparedStatement = this.m_dbConnection.CreateCommand();
            if (frame_type == 0)
                preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters ORDER BY FrameNum ASC";
            else
                preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters WHERE FrameType = " + this.currentFrameType.ToString() + " ORDER BY FrameNum ASC";

            sqliteDataReader = preparedStatement.ExecuteReader();

            i = 0;
            while (sqliteDataReader.Read())
            {
                this.ArrayFrameNum[i] = Convert.ToInt32(sqliteDataReader[0]);
                i++;
            }

            this.CurrentFrameNum = this.ArrayFrameNum[0];
            this.UimfFrameParams = this.GetFrameParams(this.ArrayFrameNum[0]);
            this.MzCalibration = this.GetMzCalibrator(this.UimfFrameParams);

            preparedStatement.Dispose();

            //MessageBox.Show(frame_count.ToString());
            return frame_count;
        }

        public int LoadFrame(int frameIndex)
        {
            if ((frameIndex < this.ArrayFrameNum.Length) && (frameIndex > 0))
            {
                this.CurrentFrameNum = this.ArrayFrameNum[frameIndex];
                this.UimfFrameParams = this.GetFrameParams(this.ArrayFrameNum[frameIndex]);
                this.MzCalibration = this.GetMzCalibrator(this.UimfFrameParams);

                return this.ArrayFrameNum[frameIndex];
            }
            else
                return -1;
        }

        /// <summary>
        /// Note that calling this function will auto-update the current frame type to frametype
        /// </summary>
        /// <param name="frameType"></param>
        /// <returns></returns>
        public int get_NumFrames(int frameType)
        {
            return this.set_FrameType(frameType);
        }

        public new double GetBinForPixel(int pixel)
        {
            // TODO: Renamed to "GetBinForPixel", and the UIMFLibrary version returns -1 on error...
            if (this.calibrationTable == null)
            {
                // Random, likely safe return value...
                return 100000;
            }
            if (pixel < this.calibrationTable.Length)
                return this.calibrationTable[pixel];
            else
                return this.calibrationTable[this.calibrationTable.Length - 1]; // return maximum bin
        }

        public int[][] AccumulateFrameDataUncompressed(int frameIndex, bool flagTOF, int startScan, int startBin, int[][] frameData)
        {
            return this.AccumulateFrameData(frameIndex, flagTOF, startScan, startBin, 0, this.m_globalParameters.Bins, frameData, -1);
        }

        public int[][] AccumulateFrameData(int frameIndex, bool flagTOF, int startScan, int startBin, int minMzBin, int maxMzBin, int[][] frameData, int yCompression)
        {
            if ((frameIndex < 0) || (frameIndex >= this.ArrayFrameNum.Length))
                return frameData;

            int i;

            int dataWidth = frameData.Length;
            int dataHeight = frameData[0].Length;

            byte[] compressedBinIntensity;
            byte[] streamBinIntensity = new byte[this.m_globalParameters.Bins * 4];
            int scansData;
            int indexCurrentBin;
            int intBinIntensity;
            int decompressLength;
            int pixelY = 0;
            int currentScan;
            int endBin;

            // this.UIMF_FrameParameters = this.GetFrameParameters(this.array_FrameNum[frame_index]);

            if (yCompression > 0)
                endBin = startBin + (dataHeight * yCompression);
            else if (yCompression < 0)
                endBin = startBin + dataHeight - 1;
            else
            {
                throw new Exception("UIMFLibrary accumulate_PlotData: Compression == 0");
            }

            // ensure the correct Frame parameters are set
            if (this.ArrayFrameNum[frameIndex] != this.CurrentFrameNum)
            {
                this.CurrentFrameNum = this.ArrayFrameNum[frameIndex];
                this.UimfFrameParams = this.GetFrameParams(this.ArrayFrameNum[frameIndex]);
                this.MzCalibration = this.GetMzCalibrator(this.UimfFrameParams);
            }

            // Create a calibration lookup table -- for speed
            this.calibrationTable = new double[dataHeight];
            if (flagTOF)
            {
                for (i = 0; i < dataHeight; i++)
                    this.calibrationTable[i] = startBin + ((double)i * (double)(endBin - startBin) / (double)dataHeight);
            }
            else
            {
                double mz_min = (double)this.MzCalibration.TOFtoMZ((float)((startBin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));
                double mz_max = (double)this.MzCalibration.TOFtoMZ((float)((endBin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));

                for (i = 0; i < dataHeight; i++)
                    this.calibrationTable[i] = (double)this.MzCalibration.MZtoTOF(mz_min + ((double)i * (mz_max - mz_min) / (double)dataHeight)) * this.m_globalParameters.BinWidth / (double)TenthsOfNanoSecondsPerBin;
            }

            // This function extracts intensities from selected scans and bins in a single frame
            // and returns a two-dimetional array intensities[scan][bin]
            // frameNum is mandatory and all other arguments are optional
            var preparedStatement = this.m_dbConnection.CreateCommand();
            preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.ArrayFrameNum[frameIndex].ToString() + " AND ScanNum >= " + startScan.ToString() + " AND ScanNum <= " + (startScan + dataWidth - 1).ToString();
            var sqliteDataReader = preparedStatement.ExecuteReader();
            preparedStatement.Dispose();

            // accumulate the data into the plot_data
            if (yCompression < 0)
            {
                pixelY = 1;

                //MessageBox.Show(start_bin.ToString() + " " + end_bin.ToString());

                for (scansData = 0; ((scansData < dataWidth) && sqliteDataReader.Read()); scansData++)
                {
                    currentScan = Convert.ToInt32(sqliteDataReader["ScanNum"]) - startScan;
                    compressedBinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                    if (compressedBinIntensity.Length == 0)
                        continue;

                    indexCurrentBin = 0;
                    //decompress_length = CLZF2.Decompress(compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                    decompressLength = CLZF2.Decompress(compressedBinIntensity, ref streamBinIntensity);

                    int previousValue = 0;
                    int binData;
                    for (binData = 0; (binData < decompressLength) && (indexCurrentBin <= endBin); binData += 4)
                    {
                        intBinIntensity = BitConverter.ToInt32(streamBinIntensity, binData);

                        if (intBinIntensity < 0)
                        {
                            indexCurrentBin += -intBinIntensity;   // concurrent zeros
                        }
                        else if (intBinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                        {
                            // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                            // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                            // consecutive zeroes to hit the underflow limit
                            // Really, the encoding we are using should never output a zero.
                        }
                        else if ((indexCurrentBin < minMzBin) || (indexCurrentBin < startBin))
                            indexCurrentBin++;
                        else if (indexCurrentBin > maxMzBin)
                            break;
                        else
                        {
                            frameData[currentScan][indexCurrentBin - startBin] += intBinIntensity;
                            indexCurrentBin++;
                        }
                        previousValue = intBinIntensity;
                    }
                }
            }
            else    // each pixel accumulates more than 1 bin of data
            {
                for (scansData = 0; ((scansData < dataWidth) && sqliteDataReader.Read()); scansData++)
                {
                    currentScan = Convert.ToInt32(sqliteDataReader["ScanNum"]) - startScan;

                    compressedBinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                    if (compressedBinIntensity.Length == 0)
                        continue;

                    indexCurrentBin = 0;
                    //decompress_length = LZFCompressionUtil.Decompress(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                    decompressLength = CLZF2.Decompress(compressedBinIntensity, ref streamBinIntensity);

                    pixelY = 1;

                    double calibrated_bin = 0;
                    int previousValue = 0;
                    int bin_value;
                    for (bin_value = 0; (bin_value < decompressLength) && (indexCurrentBin < endBin); bin_value += 4)
                    {
                        intBinIntensity = BitConverter.ToInt32(streamBinIntensity, bin_value);

                        if (intBinIntensity < 0)
                        {
                            indexCurrentBin += -intBinIntensity; // concurrent zeros
                        }
                        else if (intBinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                        {
                            // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                            // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                            // consecutive zeroes to hit the underflow limit
                            // Really, the encoding we are using should never output a zero.
                        }
                        else if ((indexCurrentBin < minMzBin) || (indexCurrentBin < startBin))
                            indexCurrentBin++;
                        else if (indexCurrentBin > maxMzBin)
                            break;
                        else
                        {
                            calibrated_bin = (double)indexCurrentBin;

                            for (i = pixelY; i < dataHeight; i++)
                            {
                                if (this.calibrationTable[i] > calibrated_bin)
                                {
                                    pixelY = i;
                                    frameData[currentScan][pixelY] += intBinIntensity;
                                    break;
                                }
                            }
                            indexCurrentBin++;
                        }
                        previousValue = intBinIntensity;
                    }
                }
            }

            sqliteDataReader.Close();
            return frameData;
        }

        /// <summary>
        /// Get the summed mass spectrum (as bins) for the range of scans
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="startScan"></param>
        /// <param name="endScan"></param>
        /// <returns></returns>
        public int[] GetSumScans(int frameIndex, int startScan, int endScan)
        {
            if ((frameIndex < 0) || (frameIndex >= this.ArrayFrameNum.Length))
                return (int[])null;

            // ensure the correct Frame parameters are set
            if (this.ArrayFrameNum[frameIndex] != this.CurrentFrameNum)
            {
                LoadFrame(frameIndex);
            }

            var tofArray = this.GetSpectrumAsBins(CurrentFrameNum, CurrentFrameNum, this.UimfFrameParams.FrameType, startScan, endScan);

            return tofArray;
        }

        public int[] GetDriftChromatogram(int frameIndex)
        {
            return GetDriftChromatogram(frameIndex, 0, this.m_globalParameters.Bins);
        }

        /// <summary>
        /// Get the drift chromatogram for the specified frame, but only for bins between minBin and maxBin, inclusive.
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="minBin"></param>
        /// <param name="maxBin"></param>
        /// <returns></returns>
        public int[] GetDriftChromatogram(int frameIndex, int minBin, int maxBin)
        {
            this.LoadFrame(frameIndex);
            int[] driftChromatogram = new int[this.UimfFrameParams.Scans];

            for (var driftScan = 0; driftScan < this.UimfFrameParams.Scans; driftScan++)
            {
                var data = this.GetSpectrumAsBinsNz(CurrentFrameNum, UimfFrameParams.FrameType, driftScan, out _);

                foreach (var bin in data.Where(x => minBin <= x.Item1 && x.Item1 <= maxBin))
                {
                    driftChromatogram[driftScan] += bin.Item2;
                }
            }

            return driftChromatogram;
        }
    }
}
