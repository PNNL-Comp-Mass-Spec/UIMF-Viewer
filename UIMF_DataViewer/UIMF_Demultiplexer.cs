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

// This class decodes multiplexed frames in a .UIMF file, writing the results to a new .UIMF file
//
// -------------------------------------------------------------------------------
// Written by William Danielson for the Department of Energy (PNNL, Richland, WA)
// -------------------------------------------------------------------------------
// 

namespace UIMF_Demultiplexer
{
    public enum eProcessingStatus
    {
        Unstarted = 0,
        Initializing = 1,
        DuplicatingUIMF = 2,
        DeletingFrameScans = 3,
        Demultiplexing = 4,
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

    public class UIMF_Demultiplexer
    {
        public const int DEFAULT_DEMUX_MIN_NUMBER_OF_IONS = 3;
        public const int DEFAULT_NOISE_DATA_SCAN_BOUNDARY = 50;
        public const int DEFAULT_NOISE_DATA_ARRIVAL_TIME_BOUNDARY = 200000;
        public const int DEFAULT_THREAD_COUNT = 1;

        public const int DEFAULT_MAX_CHECKPOINT_FRAME_INTERVAL = 50;
        public const int DEFAULT_MAX_CHECKPOINT_WRITE_FREQUENCY_MINUTES = 20;

        protected const int TYPICAL_POINTS_PER_SCAN = 2000;
        protected const int TYPICAL_MAX_POINTS_PER_SCAN = 3000;

        // The Demultiplexing code expects arrival times 10x the arrival time values stored in the .UIMF file
        protected const int ARRIVAL_TIME_SCALAR = 10;

        // Belov has this hard-coded to 10; units are nanoseconds
        protected const int TIME_SCALE = 10;

        // Class-wide variables
        protected string mOutputFileNameOverride = String.Empty;                // If empty, then is auto-defined based on the input file
        protected int mDemuxMinNumberOfIons = DEFAULT_DEMUX_MIN_NUMBER_OF_IONS;
        protected int mDemuxThreadsToUse = DEFAULT_THREAD_COUNT;        // Number of simultaneous demultiplexing events to allow
        protected int mDemuxNoiseDataScanBoundary = DEFAULT_NOISE_DATA_SCAN_BOUNDARY;
        protected int mDemuxNoiseDataArrivalTimeBoundary = DEFAULT_NOISE_DATA_ARRIVAL_TIME_BOUNDARY;

        protected bool mPreviewMode = false;                            // When true, then demultiplexes the data, but does not create a new .UIMF file

        protected int mFrameFirst = 0;                                  // If non-zero, then will start demultiplexing at this frame
        protected int mFrameLast = 0;                                   // If non-zero, then will finish demultiplexing with this frame

        protected bool mResumeDemultiplexing = false;                   // If true, then try to resume demultiplexing by appending new data to an existing .uimf.tmp file

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

        private Thread thread_Demultiplex;
        private string Multiplexed_Filename;
        private int current_frame;

        public bool flag_Stopped = false;

        UIMF_File.Utilities.progress_Processing pb_DecodeProgress;

        public UIMF_Demultiplexer(UIMFLibrary.DataReader uimf_reader, string filename, UIMF_File.Utilities.progress_Processing ptr_frame_progress, bool flag_auto)
        {
            this.Multiplexed_Filename = filename;
            this.uimf_Reader = uimf_reader;

            this.flag_Stopped = false;

            this.pb_DecodeProgress = ptr_frame_progress;
            this.pb_DecodeProgress.Caption = "Decoding " + Path.GetFileName(this.Multiplexed_Filename) + "...";
           // this.pb_DecodeProgress.SetValue(0, 0);
            this.pb_DecodeProgress.Show();

            if (flag_auto)
            {
                this.thread_Demultiplex = new Thread(new ThreadStart(this.process_Demultiplex));
                this.thread_Demultiplex.Priority = System.Threading.ThreadPriority.Lowest;
                this.thread_Demultiplex.Start();
            }
        }

        public unsafe void process_Demultiplex()
        {
            int i;

            List<int> list_Bins = new List<int>();
            List<int> list_Intensity = new List<int>();

            Stopwatch stop_watch = new Stopwatch();
            string sSequenceName;
            double dIndexConverter = 10;
            string name_instrument;
            Belov_Transform.BelovTransform belovTransform;

            double[] tof_offset;
            int[] scan_length;
            int[][] arrival_time;
            int[][] intensities;
            int[][] decoded_arrival_time;
            int[][] decoded_intensities;
            double bin_resolution;

            GlobalParameters gp = this.uimf_Reader.GetGlobalParameters();
            FrameParameters fp;

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

            bin_resolution = Math.Log(gp.BinWidth * 10.0) / Math.Log(2.0);

            // make sure the instrument name is in the right format - either QTOF or TOF
            name_instrument = gp.InstrumentName;
            if ((name_instrument == null) || (name_instrument.Length == 0))
                name_instrument = "QTOF";
            else if (name_instrument != "QTOF" && name_instrument != "TOF")
            {
                // BelovTransform.cpp nly knows about instruments QTOF and TOF
                // Try to auto-update mCachedGlobalParams.InstrumentName
                if (name_instrument.ToUpper().StartsWith("IMS"))
                    name_instrument = "QTOF";
                else
                {
                    //  ShowMessage("Instrument name of " + name_instrument + " is not recognized by BelovTransform.cpp; results will likely be invalid");
                    name_instrument = "QTOF";
                }
            }

            /*
            byte[] bytes_instrumentname = Encoding.ASCII.GetBytes(name_instrument);
            sbyte* sbytes_instrumentname;
            fixed (byte* p = bytes_instrumentname)
            {
                sbytes_instrumentname = (sbyte*)p;
            }
            */

            bool flag_success = false;
            //byte[] bytes_SequenceName;
            //sbyte* sbytes_SequenceName;
            int max_time = 0;

            this.pb_DecodeProgress.Min = 0;
            this.pb_DecodeProgress.Max = gp.NumFrames;
            this.pb_DecodeProgress.Show();
            this.pb_DecodeProgress.Update();
            this.pb_DecodeProgress.Initialize();
            //for (this.current_frame = 0; ((this.current_frame < this.uimf_Reader.get_NumFramesCurrentFrameType()) && !this.flag_Stopped && !this.pb_DecodeProgress.flag_Stop); this.current_frame++)
            for (this.current_frame = 600; ((this.current_frame < this.uimf_Reader.get_NumFramesCurrentFrameType()) && (current_frame == 600) && !this.flag_Stopped && !this.pb_DecodeProgress.flag_Stop); this.current_frame++)
            {
                this.pb_DecodeProgress.SetValue(this.current_frame, (int)stop_watch.ElapsedMilliseconds);

                stop_watch.Reset();
                stop_watch.Start();

                belovTransform = new Belov_Transform.BelovTransform();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                try
                {
                    fp = this.uimf_Reader.GetFrameParameters(this.current_frame);
                    sSequenceName = "4Bit_24OS.txt"; // System.IO.Path.GetFileName(fp.IMFProfile);

                    tof_offset = new double[fp.Scans];
                    tof_offset[0] = 0;
                    for (i = 1; i < fp.Scans; i++)
                    {
                        tof_offset[i] = tof_offset[i - 1] + fp.AverageTOFLength;
                    }

                    scan_length = new int[fp.Scans];

                    arrival_time = new int[fp.Scans][];
                    intensities = new int[fp.Scans][];
                    for (i = 0; (i < fp.Scans); i++)
                    {
                        try
                        {
                            scan_length[i] = this.uimf_Reader.GetCountPerSpectrum(this.current_frame, i);
                            arrival_time[i] = new int[scan_length[i]];
                            intensities[i] = new int[scan_length[i]];
                            this.uimf_Reader.GetSpectrum(this.current_frame, i, intensities[i], arrival_time[i]);
                          
                            // convert the bin time to a usable format.
                            for (int j = 0; j < scan_length[i]; j++)
                            {
                                arrival_time[i][j] = (int)(((arrival_time[i][j] - gp.TimeOffset) * gp.BinWidth) * ARRIVAL_TIME_SCALAR);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("get spectrum: " + i.ToString() + "\n" + ex.ToString());
                        }
                    }

                    // get the arrival times in the right format for mikes code.
                    for (i = 0; i < fp.Scans; i++)
                    {
                        for (int j = 0; j < scan_length[i]; j++)
                            arrival_time[i][j] *= (int) gp.BinWidth;
                    }

                    flag_success = true;
                    try
                    {
                        // MessageBox.Show("OnInputRawData:");
                        if (!belovTransform.OnInputRawData(sSequenceName, name_instrument,
                            fp.Scans, gp.Bins,
                            dIndexConverter, TIME_SCALE,
                            bin_resolution, tof_offset, mDemuxMinNumberOfIons,
                            scan_length, arrival_time, intensities))
                        {
                            flag_success = false;
                            pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @OnInputRawData().", true);
                            continue;
                        }
                        else
                            flag_success = true;
                    }
                    catch (Exception ex)
                    {
                        pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @OnInputRawData:  " + ex.ToString(), true);
                        continue;
                    }

                    int num_elementsperframe = 0;
                    int num_spectra = 0;
                    int num_elementsperspectrum = 0;

                    if (flag_success)
                    {
                        try
                        {
                            if (!belovTransform.OnRetrieveTransformParameters(out num_elementsperframe, out num_spectra, out num_elementsperspectrum))
                            {
                                pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @OnRetrieveTransformParameters().", true);
                                continue;
                            }
                            else
                                flag_success = true;
                        }
                        catch (Exception ex)
                        {
                            pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @OnRetrieveTransformParameters:  " + ex.ToString(), true);
                            continue;
                        }
                    }

                    if (flag_success)
                    {
                        decoded_arrival_time = new int[num_spectra][];
                        decoded_intensities = new int[num_spectra][];
                        int iArrivalTime;
                        int iArrivalBin;

                        for (i = 0; i < num_spectra; i++)
                        {
                            decoded_arrival_time[i] = new int[num_elementsperspectrum];
                            decoded_intensities[i] = new int[num_elementsperspectrum];
                        }

                        try
                        {
                            if (belovTransform.OnRetrieveTransformedData(decoded_arrival_time, decoded_intensities))
                                flag_success = true;
                            else
                            {
                                pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @ OnRetrieveTransformedData().", true);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            pb_DecodeProgress.add_Status("Frame " + current_frame.ToString() + " failed @ OnRetrieveTransformedData():  " + ex.ToString(), true);
                            continue;
                        }

                        fp.Scans = num_spectra;
                        UIMF_Writer.InsertFrame(fp);
                        for (i = 0; i < num_spectra; i++)
                        {
                            list_Bins.Clear();
                            list_Intensity.Clear();
                            for (int j = 0; j < decoded_arrival_time[i].Length; j++)
                            {
                                if (decoded_intensities[i][j] > 0)
                                {
                                    iArrivalTime = ConvertArrivalTimeDemuxToInt(decoded_arrival_time[i][j], (fp.AverageTOFLength * i));
                                    iArrivalBin = ConvertArrivalTimeToBin(iArrivalTime, gp.BinWidth);

                                    list_Bins.Add(iArrivalBin);
                                    list_Intensity.Add(decoded_intensities[i][j]);
                                }
                            }
                            UIMF_Writer.InsertScan(fp, i, list_Bins, list_Intensity, gp.BinWidth, gp.TimeOffset);
                        }
                    }
                }
                catch (Exception ex)
                {
                    flag_success = false;
                    MessageBox.Show(ex.ToString());
                }

                stop_watch.Stop();
                if (stop_watch.ElapsedMilliseconds > max_time)
                {
                    max_time = (int)stop_watch.ElapsedMilliseconds;
                    this.pb_DecodeProgress.add_Status("Max Time: Frame " + this.current_frame.ToString() + " ..... " + max_time.ToString() + " msec", false);
                }
            }

            if (this.pb_DecodeProgress.Success())
            {
                pb_DecodeProgress.Hide();
            }

            UIMF_Writer.FlushUIMF();
            UIMF_Writer.CloseUIMF();
        }

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
    }
}
