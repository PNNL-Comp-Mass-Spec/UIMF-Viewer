using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using UIMFLibrary;

namespace UIMFViewer
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

        public IntSafeArray ArrayFrameNum { get; private set; } = new IntSafeArray(0);

        public UIMFLibrary.MzCalibrator MzCalibration { get; private set; }

        public Dictionary<int, FrameType> FrameTypeDict { get; private set; }

        public UIMFDataWrapper(string uimfFile)
            : base(uimfFile)
        {
            this.UimfDataFile = uimfFile;

            FrameTypeDict = this.GetMasterFrameList();

            // Load initial information
            this.SetCurrentFrameType(ReadFrameType.AllFrames, true);

            this.CurrentFrameIndex = 0;

            this.FrameWidth = 1;
        }

        /// <summary>
        /// Returns the currently selected frame type
        /// </summary>
        /// <returns></returns>
        public ReadFrameType CurrentFrameType { get; private set; }

        public FrameType CurrentFrameNumFrameType => FrameTypeDict[CurrentFrameNum];

        public string FrameTypeDescription(int frameType)
        {
            if (frameType == 0)
                return "All Frames";
            else
                return FrameTypeDescription((FrameType)frameType);
        }

        public int MapBinCalibration(int currentBin, double newSlope, double newIntercept)
        {
            double mz = this.MzCalibration.TOFtoMZ(((double) currentBin) * this.TenthsOfNanoSecondsPerBin);
            double r = (Math.Sqrt(mz));

            return (int)((((r / newSlope) + newIntercept) * this.mGlobalParameters.BinWidth * 10000.0 / this.TenthsOfNanoSecondsPerBin) + .5); // .5 for rounding
        }

        /// <summary>
        /// Update the calibration coefficients for a single frame
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <param name="autoCalibrating"></param>
        public new void UpdateCalibrationCoefficients(int frameIndex, double slope, double intercept, bool autoCalibrating = false)
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
        public new void UpdateAllCalibrationCoefficients(double slope, double intercept, bool autoCalibrating = false)
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
            this.mCachedFrameParameters.Clear();
        }


        /// <summary>
        /// Returns the frame index for the given frame number
        /// Only searches frame numbers of the current frame type <see cref="CurrentFrameType"/>
        /// </summary>
        /// <param name="frameNumber"></param>
        /// <returns>Frame Index if found; otherwise; a negative number if not a valid frame number</returns>
        public int GetFrameIndex(int frameNumber)
        {
            return ArrayFrameNum.BinarySearch(frameNumber);
        }

        /// <summary>
        /// Set the frame type
        /// </summary>
        /// <param name="frameType">Frame type to set; see FrameType for types</param>
        /// <param name="forceReload">True to force a re-load of the data from the Frame_Parameters table</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int SetCurrentFrameType(ReadFrameType frameType, bool forceReload = false)
        {
            // If the frame type is already correct, then we don't need to re-query the database
            if ((CurrentFrameType == frameType) && !forceReload)
                return ArrayFrameNum.Length;

            CurrentFrameType = frameType;

            if (CurrentFrameType != ReadFrameType.AllFrames)
            {
                var frameNums = GetFrameNumbers((FrameType) frameType);
                if (frameNums.Length == 0)
                {
                    ArrayFrameNum = new IntSafeArray(0);
                    return 0;
                }

                ArrayFrameNum = new IntSafeArray(frameNums);
            }
            else
            {
                FrameTypeDict = GetMasterFrameList();
                if (FrameTypeDict.Count == 0)
                {
                    ArrayFrameNum = new IntSafeArray(0);
                    return 0;
                }

                ArrayFrameNum = new IntSafeArray(FrameTypeDict.Select(x => x.Key).OrderBy(x => x).ToArray());
            }

            this.CurrentFrameNum = this.ArrayFrameNum[0];
            this.UimfFrameParams = this.GetFrameParams(this.ArrayFrameNum[0]);
            this.MzCalibration = this.GetMzCalibrator(this.UimfFrameParams);

            return ArrayFrameNum.Count;
        }

        public int LoadFrame(int frameIndex)
        {
            if ((frameIndex < this.ArrayFrameNum.Length) && (frameIndex >= 0))
            {
                if (ArrayFrameNum[frameIndex] == CurrentFrameNum)
                {
                    return ArrayFrameNum[frameIndex];
                }

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
        public int GetNumberOfFrames(ReadFrameType frameType)
        {
            return this.SetCurrentFrameType(frameType);
        }

        public new double GetBinForPixel(int pixel)
        {
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

        /// <summary>
        /// Retrieves a given frame (or frames) and sums them in order to be viewed on a heatmap view or other 2D representation visually.
        /// </summary>
        /// <param name="startFrameNumber">
        /// </param>
        /// <param name="endFrameNumber">
        /// </param>
        /// <param name="flagTOF">
        /// </param>
        /// <param name="startScan">
        /// </param>
        /// <param name="scanCount">
        /// </param>
        /// <param name="startBin">
        /// </param>
        /// <param name="binCount">
        /// </param>
        /// <param name="yCompression">
        /// </param>
        /// <param name="frameData"></param>
        /// <param name="maxMzBin"></param>
        /// <param name="zeroOutData"></param>
        /// <param name="xCompression">
        /// </param>
        /// <param name="minMzBin"></param>
        /// <returns>
        /// Frame data to be utilized in visualization as a multidimensional array
        /// </returns>
        /// <remarks>
        /// This function is used by the UIMF Viewer and by Atreyu
        /// </remarks>
        public int[][] AccumulateFrameDataByCount(int startFrameNumber, int endFrameNumber, bool flagTOF, int startScan, int scanCount, int startBin, int binCount,
            int yCompression = -1, int[][] frameData = null, int minMzBin = -1, int maxMzBin = -1, bool zeroOutData = true, int xCompression = -1)
        {
            var endScan = startScan + scanCount - 1;
            var endBin = startBin + binCount - 1;
            if (yCompression >= 0)
            {
                endBin = startBin - 1 + binCount * yCompression;
            }

            if (xCompression >= 0)
            {
                endScan = startScan - 1 + scanCount * xCompression;
            }
            return AccumulateFrameData(startFrameNumber, endFrameNumber, flagTOF, startScan, endScan, startBin, endBin, yCompression, frameData,
                minMzBin, maxMzBin, zeroOutData, xCompression);
        }

        /// <summary>
        /// Retrieves a given frame (or frames) and sums them in order to be viewed on a heatmap view or other 2D representation visually.
        /// </summary>
        /// <param name="startFrameNumber">
        /// </param>
        /// <param name="endFrameNumber">
        /// </param>
        /// <param name="flagTOF">
        /// </param>
        /// <param name="startScan">
        /// </param>
        /// <param name="endScan">
        /// </param>
        /// <param name="startBin">
        /// </param>
        /// <param name="endBin">
        /// </param>
        /// <param name="yCompression">
        /// </param>
        /// <param name="frameData"></param>
        /// <param name="maxMzBin"></param>
        /// <param name="zeroOutData"></param>
        /// <param name="xCompression">
        /// </param>
        /// <param name="minMzBin"></param>
        /// <returns>
        /// Frame data to be utilized in visualization as a multidimensional array
        /// </returns>
        /// <remarks>
        /// This function is used by the UIMF Viewer and by Atreyu
        /// </remarks>
        public int[][] AccumulateFrameData(int startFrameNumber, int endFrameNumber, bool flagTOF, int startScan, int endScan, int startBin, int endBin,
            int yCompression = -1, int[][] frameData = null, int minMzBin = -1, int maxMzBin = -1, bool zeroOutData = true, int xCompression = -1)
        {
            if (endFrameNumber - startFrameNumber < 0)
            {
                throw new ArgumentException("Start frame cannot be greater than end frame", nameof(endFrameNumber));
            }

            var width = endScan - startScan + 1;
            var height = endBin - startBin + 1;
            if (yCompression > 1)
            {
                height = (int)Math.Round((double)height / yCompression);
            }

            if (xCompression > 1)
            {
                width = (int) Math.Round((double) width / xCompression);
            }
            else
            {
                xCompression = 1; // For math simplicity
            }

            if (frameData == null || width != frameData.Length || height != frameData[0].Length)
            {
                try
                {
                    frameData = new int[width][];
                    for (var i = 0; i < width; i++)
                    {
                        frameData[i] = new int[height];
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    throw new OutOfMemoryException("2D frameData array is too large with dimensions " + width + " by " + height, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception instantiating 2D frameData array of size " + width + " by " + height + ": " + ex.Message, ex);
                }
            }
            else if (zeroOutData)
            {
                for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                {
                    frameData[i][j] = 0;
                }
            }

            if (minMzBin < 0)
            {
                minMzBin = 0;
            }

            if (maxMzBin < 0)
            {
                maxMzBin = this.mGlobalParameters.Bins;
            }

            if (maxMzBin < minMzBin)
            {
                maxMzBin = minMzBin;
            }

            // ensure the correct Frame parameters are set
            if (endFrameNumber != this.CurrentFrameNum)
            {
                this.CurrentFrameNum = endFrameNumber;
                this.UimfFrameParams = this.GetFrameParams(endFrameNumber);
                this.MzCalibration = this.GetMzCalibrator(this.UimfFrameParams);
            }

            for (var currentFrameNumber = startFrameNumber; currentFrameNumber <= endFrameNumber; currentFrameNumber++)
            {

                // Create a calibration lookup table -- for speed
                calibrationTable = new double[height];
                if (flagTOF)
                {
                    for (var i = 0; i < height; i++)
                    {
                        calibrationTable[i] = startBin + (i * (double)(endBin - startBin) / height);
                    }
                }
                else
                {
                    var frameParams = GetFrameParams(currentFrameNumber);
                    var mzCalibrator = GetMzCalibrator(frameParams);

                    if (Math.Abs(frameParams.CalibrationSlope) < float.Epsilon)
                        Console.WriteLine(" ... Warning, CalibrationSlope is 0 for frame " + currentFrameNumber);

                    var mzMin = mzCalibrator.BinToMZ(startBin);
                    var mzMax = mzCalibrator.BinToMZ(endBin);

                    for (var i = 0; i < height; i++)
                    {
                        calibrationTable[i] = mzCalibrator.MZtoBin(mzMin + (i * (mzMax - mzMin) / height));
                    }
                }

                // This function extracts intensities from selected scans and bins in a single frame
                // and returns a two-dimensional array intensities[scan][bin]
                // frameNum is mandatory and all other arguments are optional
                using (var dbCommand = mDbConnection.CreateCommand())
                {
                    // The ScanNum cast here is required to support UIMF files that list the ScanNum field as SMALLINT yet have scan number values > 32765
                    dbCommand.CommandText = "SELECT Cast(ScanNum as Integer) AS ScanNum, Intensities " +
                                            "FROM Frame_Scans " +
                                            "WHERE FrameNum = " + currentFrameNumber +
                                            " AND ScanNum >= " + startScan +
                                            " AND ScanNum <= " + (startScan + (width * xCompression) - 1);

                    using (var reader = dbCommand.ExecuteReader())
                    {

                        // accumulate the data into the plot_data
                        if (yCompression <= 1)
                        {
                            AccumulateFrameDataNoYCompression(reader, width, startScan, startBin, endBin, ref frameData, minMzBin, maxMzBin, xCompression);
                        }
                        else
                        {
                            AccumulateFrameDataWithYCompression(reader, width, height, startScan, startBin, endBin, ref frameData, minMzBin, maxMzBin, xCompression);
                        }
                    }
                }
            }

            return frameData;
        }

        private void AccumulateFrameDataNoYCompression(IDataReader reader, int width, int startScan, int startBin, int endBin,
            ref int[][] frameData, int minMzBin, int maxMzBin, int xCompression)
        {
            for (var scansData = 0; (scansData / xCompression < width) && reader.Read(); scansData++)
            {
                var scanNum = GetInt32(reader, "ScanNum");
                ValidateScanNumber(scanNum);

                var currentScan = scanNum - startScan;
                var compressedBinIntensity = (byte[])(reader["Intensities"]);

                if (compressedBinIntensity.Length == 0)
                {
                    continue;
                }

                var compressedScan = currentScan;
                if (xCompression > 1)
                {
                    compressedScan = currentScan / xCompression;
                }

                var binIntensities = IntensityConverterCLZF.Decompress(compressedBinIntensity, out int _);

                foreach (var binIntensity in binIntensities)
                {
                    var binIndex = binIntensity.Item1;
                    if (binIndex < startBin || binIndex < minMzBin)
                    {
                        continue;
                    }
                    if (binIndex > endBin || binIndex > maxMzBin)
                    {
                        break;
                    }
                    frameData[compressedScan][binIndex - startBin] += binIntensity.Item2;
                }
            }
        }

        private void AccumulateFrameDataWithYCompression(IDataReader reader, int width, int height, int startScan, int startBin,
            int endBin, ref int[][] frameData, int minMzBin, int maxMzBin, int xCompression)
        {
            // each pixel accumulates more than 1 bin of data
            for (var scansData = 0; scansData / xCompression < width && reader.Read(); scansData++)
            {
                var scanNum = GetInt32(reader, "ScanNum");
                ValidateScanNumber(scanNum);

                var currentScan = scanNum - startScan;
                var compressedBinIntensity = (byte[])(reader["Intensities"]);

                if (compressedBinIntensity.Length == 0)
                {
                    continue;
                }

                var compressedScan = currentScan;
                if (xCompression > 1)
                {
                    compressedScan = currentScan / xCompression;
                }

                var pixelY = 1;

                var binIntensities = IntensityConverterCLZF.Decompress(compressedBinIntensity, out int _);

                foreach (var binIntensity in binIntensities)
                {
                    var binIndex = binIntensity.Item1;
                    if (binIndex < startBin || binIndex < minMzBin)
                    {
                        continue;
                    }
                    if (binIndex > endBin || binIndex > maxMzBin)
                    {
                        break;
                    }

                    double calibratedBin = binIndex;

                    for (var j = pixelY; j < height; j++)
                    {
                        if (calibrationTable[j] > calibratedBin)
                        {
                            pixelY = j;
                            frameData[compressedScan][pixelY] += binIntensity.Item2;
                            break;
                        }
                    }
                }
            }
        }

        private void ValidateScanNumber(int scanNum)
        {
            if (scanNum < 0)
            {
                // The .UIMF file was created with an old version of the writer that used SMALLINT for the ScanNum field in the Frame_Params table, thus limiting the scan range to 0 to 32765
                // In May 2016 we switched to a 32-bit integer for ScanNum
                var msg = "Scan number larger than 32765 for file with the ScanNum field as a SMALLINT; change the field type to INTEGER";
                throw new Exception(msg);
            }
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
            return GetDriftChromatogram(frameIndex, 0, this.mGlobalParameters.Bins);
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

        public class IntSafeArray : IEnumerable<int>
        {
            private int[] backingArray;

            public IntSafeArray(int count)
            {
                backingArray = new int[count];
                Count = count;
            }

            public IntSafeArray(int[] data)
            {
                backingArray = data;
                Count = data.Length;
            }

            public int Count { get; }

            public int Length => backingArray.Length;

            public IEnumerator<int> GetEnumerator()
            {
                return (IEnumerator<int>)backingArray.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int this[int index]
            {
                get
                {
                    if (index < 0)
                    {
                        return backingArray[0];
                    }

                    if (index >= Count)
                    {
                        return backingArray[Count - 1];
                    }

                    return backingArray[index];
                }
                set
                {
                    if (index < 0 || index >= Count)
                    {
                        throw new IndexOutOfRangeException($"'{index}' is not in range '0-{Count - 1}'!");
                    }

                    backingArray[index] = value;
                }
            }

            public int BinarySearch(int value)
            {
                return Array.BinarySearch(backingArray, value);
            }
        }

        /// <summary>
        /// Frame type. A copy of UIMFData.FrameType, with a enum type for '0'
        /// </summary>
        public enum ReadFrameType
        {
            /// <summary>
            /// Any frame type
            /// </summary>
            [Description("All Frames")]
            AllFrames = 0,

            /// <summary>
            /// MS1
            /// </summary>
            [Description("MS")]
            MS1 = 1,

            /// <summary>
            /// MS2
            /// </summary>
            [Description("MS/MS")]
            MS2 = 2,

            /// <summary>
            /// Calibration
            /// </summary>
            [Description("Calibration")]
            Calibration = 3,

            /// <summary>
            /// Prescan
            /// </summary>
            [Description("Prescan")]
            Prescan = 4
        }
    }
}
