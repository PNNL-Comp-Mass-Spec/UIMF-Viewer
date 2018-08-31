using System;
using System.Text;
using System.Threading;
using System.IO;
using System.ComponentModel;
using UIMFLibrary;
using System.Collections.Generic;
using BelovTransform;
using System.Windows.Forms;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Linq;
using System.Drawing;

// This class decodes multiplexed frames in a .UIMF file, writing the results to a new .UIMF file
//
// -------------------------------------------------------------------------------
// Written by William Danielson for the Department of Energy (PNNL, Richland, WA)
// -------------------------------------------------------------------------------
//

namespace UIMF_BelovTransform
{
    public enum eProcessingStatus
    {
        Unstarted = 0,
        Initializing = 1,
        DuplicatingUIMF = 2,
        DeletingFrameScans = 3,
        BelovTransforming = 4,
        Finalizing = 5,
        Complete = 6,
        Failed = 7
    }

#if false
    struct udtGlobalParameters
    {
        public String InstrumentName;
        public int TimeOffset;
        public int NumTOFBins;
        public double BinWidth;
        public float TOFCorrectionTime;
    }
#endif
#if BELOV_TRANSFORM

    public class UIMF_BelovTransform
    {
        public const int DEFAULT_DEMUX_MIN_NUMBER_OF_IONS = 3;
        public const int DEFAULT_NOISE_DATA_SCAN_BOUNDARY = 50;
        public const int DEFAULT_NOISE_DATA_ARRIVAL_TIME_BOUNDARY = 200000;
        public const int DEFAULT_THREAD_COUNT = 1;

        public const int DEFAULT_MAX_CHECKPOINT_FRAME_INTERVAL = 50;
        public const int DEFAULT_MAX_CHECKPOINT_WRITE_FREQUENCY_MINUTES = 20;

        protected const int TYPICAL_POINTS_PER_SCAN = 2000;
        protected const int TYPICAL_MAX_POINTS_PER_SCAN = 3000;

        // The BelovTransforming code expects arrival times 10x the arrival time values stored in the .UIMF file
        protected const int ARRIVAL_TIME_SCALAR = 10;

        // Belov has this hard-coded to 10; units are nanoseconds
        protected const int TIME_SCALE = 10;

        // Class-wide variables
        protected string mOutputFileNameOverride = String.Empty;                // If empty, then is auto-defined based on the input file
        protected int mDemuxMinNumberOfIons = DEFAULT_DEMUX_MIN_NUMBER_OF_IONS;
        protected int mDemuxThreadsToUse = DEFAULT_THREAD_COUNT;        // Number of simultaneous BelovTransforming events to allow
        protected int mDemuxNoiseDataScanBoundary = DEFAULT_NOISE_DATA_SCAN_BOUNDARY;
        protected int mDemuxNoiseDataArrivalTimeBoundary = DEFAULT_NOISE_DATA_ARRIVAL_TIME_BOUNDARY;

        protected bool mPreviewMode = false;                            // When true, then BelovTransformes the data, but does not create a new .UIMF file

        protected int mFrameFirst = 0;                                  // If non-zero, then will start BelovTransforming at this frame
        protected int mFrameLast = 0;                                   // If non-zero, then will finish BelovTransforming with this frame

        protected bool mResumeBelovTransforming = false;                   // If true, then try to resume BelovTransforming by appending new data to an existing .uimf.tmp file

        protected long mThreadsRunning = 0;                             // Keep track of the number of running threads
        protected long mFramesWritten = 0;
        protected bool mUIMFFileCopySuccess = false;

        protected string mInputFilePathCached = String.Empty;
        protected string mOutputFilePathTempCached = String.Empty;

        protected Mutex mUIMFWriterMutex = new Mutex(false);

        // Checkpoint variables
        protected bool mCreateCheckpointFiles = false;                  // When true, then copies the .tmp decoded .uimf file to the folder specified by mCheckpointTargetFolder every mCheckpointFrameIntervalMax frames or mCheckpointWriteFrequencyMinutesMax minutes
        protected int mCheckpointFrameIntervalMax = DEFAULT_MAX_CHECKPOINT_FRAME_INTERVAL;
        protected int mCheckpointWriteFrequencyMinutesMax = DEFAULT_MAX_CHECKPOINT_WRITE_FREQUENCY_MINUTES;
        protected string mCheckpointTargetFolder = string.Empty;

        protected int m_LastCheckpointFrameNum;
        protected DateTime m_LastCheckpointTime;

        // Cached global parameters
        //udtGlobalParameters mCachedGlobalParams;

        protected eProcessingStatus mProcessingStatus = eProcessingStatus.Unstarted;
        protected string mErrorMessage = String.Empty;

        UIMFLibrary.DataReader uimf_Reader;

        private Thread thread_BelovTransform;
        private string Multiplexed_Filename;
        private int current_frame;

        public bool flag_Stopped = false;

        UIMF_File.Utilities.progress_Processing pb_DecodeProgress;

        public UIMF_BelovTransform(UIMFLibrary.DataReader uimf_reader, string filename, UIMF_File.Utilities.progress_Processing ptr_frame_progress, bool flag_auto)
        {
            this.Multiplexed_Filename = filename;
            this.uimf_Reader = uimf_reader;

            this.flag_Stopped = false;

            this.pb_DecodeProgress = ptr_frame_progress;
            this.pb_DecodeProgress.Caption = "Decoding " + Path.GetFileName(this.Multiplexed_Filename) + "...";
            // this.pb_DecodeProgress.SetValue(0, 0);
            this.pb_DecodeProgress.add_Status("Constructor complete", false);
            this.pb_DecodeProgress.Show();

            if (flag_auto)
            {
                this.thread_BelovTransform = new Thread(new ThreadStart(this.process_BelovTransform));
                this.thread_BelovTransform.Priority = System.Threading.ThreadPriority.Lowest;
                this.thread_BelovTransform.Start();
            }
        }

#if false
        private static void RunUIMFTest()
        {
            const int segmentLength = 15;
            const int numSegments = 24;

            const string matrixString = "100110101111000";

            DenseMatrix multiplierMatrix = MatrixCreator.CreateMatrixForDemultiplexing(matrixString);
            DenseMatrix scaledMatrix = (DenseMatrix)multiplierMatrix.Multiply(1.0 / 8.0);
            //DenseMatrix inversedMatrix = (DenseMatrix)multiplierMatrix.Inverse();
            DenseMatrix inversedScaledMatrix = (DenseMatrix)scaledMatrix.Inverse();

            //MatrixCreator.PrintMatrix(multiplierMatrix, "matrix.csv");
            //MatrixCreator.PrintMatrix(inversedMatrix, "matrix_inversed.csv");
            //MatrixCreator.PrintMatrix(scaledMatrix, "matrix_scaled.csv");
            //MatrixCreator.PrintMatrix(inversedScaledMatrix, "matrix_scaled_inversed.csv");

            //DenseMatrix inversedScaledMatrix = new DenseMatrix(segmentLength, segmentLength);

            //for (int i = 0; i < segmentLength; i++)
            //{
            //    for (int j = 0; j < segmentLength; j++)
            //    {
            //        if (i == j) inversedScaledMatrix[i, j] = 1;
            //        else inversedScaledMatrix[i, j] = 0;
            //    }
            //}

            DataReader uimfReader = new UIMFLibrary.DataReader();
            if (!uimfReader.OpenUIMF("Sarc_P09_C04_0796_089_22Jul11_Cheetah_11-05-32_encoded.uimf"))
            //if (!uimfReader.OpenUIMF("QCShew_Mux_FPGA_IMS6_WatersLC_65min_110816_001.UIMF"))
            {
                throw new FileNotFoundException("Could not find UIMF file.");
            }

            GlobalParameters globalParameters = uimfReader.GetGlobalParameters();
            double binWidth = globalParameters.BinWidth;
            int frameType = 1;
            int[] frameNumbers = uimfReader.GetFrameNumbers(frameType);
            int numFrames = frameNumbers.Length;
            Console.WriteLine("Total Data Frames = " + numFrames);

            const string newFileName = "QCShew_Mux_FPGA_IMS6_WatersLC_65min_110816_001_inversed.UIMF";
            DataWriter uimfWriter = new DataWriter();
            if (File.Exists(newFileName)) File.Delete(newFileName);
            uimfWriter.OpenUIMF(newFileName);
            uimfWriter.CreateTables(globalParameters.DatasetType);
            globalParameters.NumFrames = 1;
            uimfWriter.InsertGlobal(globalParameters);

            // Setup Filters
            FilterControl filterControl = new FilterControl();
            //filterControl.DoBoxCarFilter = false;
            //filterControl.DoSpuriousNoiseRemoval = false;
            //filterControl.DoSpuriousPeakRemoval = false;
            //filterControl.MinIntensityToProcessSegment = 1;

            FrameParameters firstFrameParameters = uimfReader.GetFrameParameters(1);
            double averageTOFLength = firstFrameParameters.AverageTOFLength;

            BelovTransform.BelovTransform demultiplexer = new BelovTransform.BelovTransform(inversedScaledMatrix, numSegments, filterControl, binWidth);
            Dictionary<int, int> scanToIndexMap = demultiplexer.BuildScanToIndexMap();

            //for (int currentFrameNumber = 1; currentFrameNumber < 2; currentFrameNumber++)
            for (int currentFrameNumber = 600; currentFrameNumber < 601; currentFrameNumber++)
            //foreach (int currentFrameNumber in frameNumbers)
            {
                if (currentFrameNumber == 1)
                    continue;

                int currentFrameIndex = uimfReader.GetFrameIndex(currentFrameNumber);
                Console.WriteLine("Processing Frame " + currentFrameIndex);

                // Get the data of the frame from the UIMF File
                double[][] arrayOfIntensityArrays = uimfReader.GetIntensityBlockForDemultiplexing(currentFrameIndex, frameType, segmentLength, scanToIndexMap);

                // Demultiplex the frame, which updates the array
                IEnumerable<ScanData> scanDataEnumerable = demultiplexer.DemultiplexFrame(arrayOfIntensityArrays, true);
                arrayOfIntensityArrays = null;

                // Setup the frame in the UIMFWriter
                FrameParameters frameParameters = uimfReader.GetFrameParameters(currentFrameIndex);
                uimfWriter.InsertFrame(frameParameters);

                var sortByScanNumberQuery = from scanData in scanDataEnumerable
                                            orderby scanData.ScanNumber ascending
                                            select scanData;

                foreach (ScanData scanData in sortByScanNumberQuery)
                {
                    uimfWriter.InsertScan(frameParameters, scanData.ScanNumber, scanData.BinsToIntensitiesMap.Keys.ToList(), scanData.BinsToIntensitiesMap.Values.ToList(), binWidth, 0);
                }
            }

            uimfReader.CloseUIMF();
            uimfWriter.CloseUIMF();
        }
#endif

#if BELOV_TRANSFORM
        public unsafe void process_BelovTransform()
        {
            int count = 0;

            List<int> list_Bins = new List<int>();
            List<int> list_Intensity = new List<int>();

            Stopwatch stop_watch = new Stopwatch();
            BelovTransform.BelovTransform belovTransform;

            GlobalParameters gp = this.uimf_Reader.GetGlobalParameters();
            FrameParameters fp;

            const int segmentLength = 15;
            const int numSegments = 24;

            const string matrixString = "100110101111000";

            DenseMatrix multiplierMatrix = MatrixCreator.CreateMatrixForDemultiplexing(matrixString);
            DenseMatrix scaledMatrix = (DenseMatrix)multiplierMatrix.Multiply(1.0 / 8.0);
            DenseMatrix inversedScaledMatrix = (DenseMatrix)scaledMatrix.Inverse();

            // create new UIMF File
            string UIMF_filename = Path.Combine(Path.GetDirectoryName(this.Multiplexed_Filename), Path.GetFileNameWithoutExtension(this.Multiplexed_Filename) + "_decoded.UIMF");
            if (File.Exists(UIMF_filename))
            {
                if (MessageBox.Show("File Exists", "File Exists, Replace?", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    File.Delete(UIMF_filename);
                else
                    return;
            }
            UIMFLibrary.DataWriter UIMF_Writer = new DataWriter();
            UIMF_Writer = new UIMFLibrary.DataWriter();
            UIMF_Writer.OpenUIMF(UIMF_filename);
            UIMF_Writer.CreateTables(null);
            UIMF_Writer.InsertGlobal(gp);

            FilterControl filterControl = new FilterControl();
            //filterControl.DoBoxCarFilter = false;
            belovTransform = new BelovTransform.BelovTransform(inversedScaledMatrix, numSegments, filterControl, gp.BinWidth);

            Dictionary<int, int> scanToIndexMap = belovTransform.BuildScanToIndexMap();

            bool flag_success = false;
            //byte[] bytes_SequenceName;
            //sbyte* sbytes_SequenceName;
            int max_time = 0;

            this.pb_DecodeProgress.Min = 0;
            this.pb_DecodeProgress.Max = gp.NumFrames;
            this.pb_DecodeProgress.Show();
            this.pb_DecodeProgress.Update();
            this.pb_DecodeProgress.Initialize();

            int[] frame_numbers = this.uimf_Reader.GetFrameNumbers(this.uimf_Reader.GetFrameTypeForFrame(0));
            for (this.current_frame = 0; ((this.current_frame < frame_numbers.Length) && !this.flag_Stopped && !this.pb_DecodeProgress.flag_Stop); this.current_frame++)
            {
                this.pb_DecodeProgress.SetValue(this.current_frame, (int)stop_watch.ElapsedMilliseconds);

                stop_watch.Reset();
                stop_watch.Start();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                try
                {
                    // Get the data of the frame from the UIMF File
                    fp = this.uimf_Reader.GetFrameParameters(current_frame);
                    double[][] arrayOfIntensityArrays = this.uimf_Reader.GetIntensityBlockForDemultiplexing(current_frame, fp.FrameType, segmentLength, scanToIndexMap);

                    // Demultiplex the frame, which updates the array
                    IEnumerable<ScanData> scanDataEnumerable = belovTransform.DemultiplexFrame(arrayOfIntensityArrays, true, fp.AverageTOFLength);
                    arrayOfIntensityArrays = null;

                    // Setup the frame in the UIMFWriter
                    UIMF_Writer.InsertFrame(fp);

                    var sortByScanNumberQuery = from scanData in scanDataEnumerable
                                                orderby scanData.ScanNumber ascending
                                                select scanData;

                    count = 0;
                    foreach (ScanData scanData in sortByScanNumberQuery)
                    {
                        count++;
                        UIMF_Writer.InsertScan(fp, scanData.ScanNumber, scanData.BinsToIntensitiesMap.Keys.ToList(), scanData.BinsToIntensitiesMap.Values.ToList(), gp.BinWidth, 0);
                    }

                    this.pb_DecodeProgress.add_Status(count.ToString(), false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            if (this.pb_DecodeProgress.Success())
            {
                pb_DecodeProgress.Hide();
            }

            UIMF_Writer.FlushUIMF();
            UIMF_Writer.CloseUIMF();
        }
#endif
        private static int ConvertArrivalTimeDemuxToInt(double dArrivalTimeDemux, double dAvgTOFLength)
        {
            // Note that dArrivalTimeDemux is likely in units of ns/10
            float arrival_time_ceil;
            float arrival_time_floor;
            float arrival_time_float;

            arrival_time_ceil = (float)Math.Ceiling((dArrivalTimeDemux - dAvgTOFLength) * TIME_SCALE);
            arrival_time_floor = (float)Math.Floor((dArrivalTimeDemux - dAvgTOFLength) * TIME_SCALE);
            arrival_time_float = (float)((dArrivalTimeDemux - dAvgTOFLength) * TIME_SCALE);

            if (Math.Abs(arrival_time_ceil - arrival_time_float) < Math.Abs(arrival_time_floor - arrival_time_float))
                return (int)arrival_time_ceil;
            else
            {
                if (Math.Abs(arrival_time_ceil - arrival_time_float) == Math.Abs(arrival_time_floor - arrival_time_float))
                    return (int)arrival_time_float;
                else
                    return (int)arrival_time_floor;
            }
        }

        private int ConvertArrivalTimeToBin(int iArrivalTime, double bin_width)
        {
            // Note: Do not add mCachedGlobalParams.TimeOffset at this time
            // The offset will be added within InsertScan in the UIMFWriter

            // Original code; this may have had an integer division bug
            // return (int)(iArrivalTime / mCachedGlobalParams.BinWidth / ARRIVAL_TIME_SCALAR);

            // Mid-term code, which led to bin values one number too small most of the time
            // return (int)(iArrivalTime / (mCachedGlobalParams.BinWidth * (double)ARRIVAL_TIME_SCALAR));

            // New code implemented 5/18/2011
            return (int)Math.Ceiling(iArrivalTime / (bin_width * (double)ARRIVAL_TIME_SCALAR));
        }

#if false
        public unsafe int** ToPointer(int[][] array)
        {
            fixed (int* arrayPtr = array[0])
            {
                int*[] ptrArray = new int*[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    fixed (int* ptr = array[i])
                        ptrArray[i] = ptr;
                }

                fixed (int** ptr = ptrArray)
                {
                    return ptr;
                }
            }
        }
#endif
    }
#endif
}
