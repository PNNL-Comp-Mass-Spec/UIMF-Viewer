////////////////////////////////////////////////////////////////////////////////////
// This is a library of functions to write and extract data from UIMF files
// Authors: Yan Shi, William Danielson III, and Anuj Shah
// Pacific Northwest National Laboratory
// December 2008
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SQLite;

namespace UIMFLibrary
{
    public class GlobalParameters
    {
		//public DateTime DateStarted;             // 1, Date Experiment was acquired 
        public string DateStarted;
        public int NumFrames;                  // 2, Number of frames in dataset
        public int TimeOffset;                 // 3, Offset from 0. All bin numbers must be offset by this amount 
        public double BinWidth;                // 4, Width of TOF bins (in ns) 
        public int Bins;                       // 5, Total number of TOF bins in frame
		public float TOFCorrectionTime;
        public float FrameDataBlobVersion;     // 6, Version of FrameDataBlob in T_Frame 
        public float ScanDataBlobVersion;      // 7, Version of ScanInfoBlob in T_Frame 
        public string TOFIntensityType;        // 8, Data type of intensity in each TOF record (ADC is int/TDC is short/FOLDED is float) 
        public string DatasetType;             // 9, Type of dataset (HMS/HMSMS/HMS-MSn) 
		public int Prescan_TOFPulses;		   // 10 - 14, Prescan parameters
		public int Prescan_Accumulations;
		public int Prescan_TICThreshold;
		public bool Prescan_Continuous;		   // True or False
		public string Prescan_Profile;	       // If continuous is true, set this to NULL;
	}
    public class FrameParameters
    {
        public int FrameNum;                   // 0, Primary Key, Contains the frame number
		public double StartTime;               // 1, Start time of frame, in minutes
        public double Duration;                // 2, Duration of frame, in seconds
        public int Accumulations;              // 3, Number of collected and summed acquisitions in a frame 
        public short FrameType;                // 4, Bitmap: 0=MS (Regular); 1=MS/MS (Frag); 2=Prescan; 4=Multiplex 
        public int Scans;                      // 5, Number of TOF scans
		public string IMFProfile;			   // new, IMFProfile Name
		public double TOFLosses;			   // new TOF Losses
        public double AverageTOFLength;        // 6, Average time between TOF trigger pulses 
        public double CalibrationSlope;        // 7, Value of k0
        public double CalibrationIntercept;    // 8, Value of t0
		public double a2;	//The six parameters below are coefficients for residual mass error correction
		public double b2;	//ResidualMassError=a2t+b2t^3+c2t^5+d2t^7+e2t^9+f2t^11
		public double c2;
		public double d2;
		public double e2;
		public double f2;
		public double Temperature;             // 9, Ambient temperature
        public double voltHVRack1;             // 10, Voltage setting in the IMS system
        public double voltHVRack2;             // 11, Voltage setting in the IMS system
        public double voltHVRack3;             // 12, Voltage setting in the IMS system
        public double voltHVRack4;             // 13, Voltage setting in the IMS system
        public double voltCapInlet;            // 14, Capilary Inlet Voltage 
        public double voltEntranceIFTIn;       // 15, IFT In Voltage 
        public double voltEntranceIFTOut;      // 16, IFT Out Voltage 
        public double voltEntranceCondLmt;     // 17, Cond Limit Voltage
        public double voltTrapOut;             // 18, Trap Out Voltage
        public double voltTrapIn ;             // 19, Trap In Voltage
        public double voltJetDist;             // 20, Jet Disruptor Voltage
        public double voltQuad1;               // 21, Fragmentation Quadrupole Voltage
        public double voltCond1;               // 22, Fragmentation Conductance Voltage
        public double voltQuad2;               // 23, Fragmentation Quadrupole Voltage
        public double voltCond2;               // 24, Fragmentation Conductance Voltage
        public double voltIMSOut;              // 25, IMS Out Voltage
        public double voltExitIFTIn;           // 26, IFT In Voltage
        public double voltExitIFTOut;          // 27, IFT Out Voltage
        public double voltExitCondLmt;         // 28, Cond Limit Voltage
        public double PressureFront;           // 29, Pressure at IMS entrance
        public double PressureBack;            // 30, Pressure at IMS exit
        public short MPBitOrder;               // 31, Determines original size of bit sequence
        public double[] FragmentationProfile;  // 36, Voltage profile used in fragmentation
    }
    public class IMSCOMP
    {
        public const string dll = "IMSCOMP.dll";

        [DllImport(dll, CharSet = CharSet.Ansi)]
        public static unsafe extern int compress_buffer(
            double* in_data,
            int blocksize,
            byte* out_data);
		[DllImport(dll, CharSet = CharSet.Ansi)]
		public static unsafe extern int lzf_compress(byte* in_data, int in_len, byte* out_data, int out_len);
		[DllImport(dll, CharSet = CharSet.Ansi)]
		public static unsafe extern int lzf_decompress(byte* in_data, int in_len, byte* out_data, int out_len);
	}
    public class IMSCOMP_wrapper
    {
        public static unsafe int compress_buffer(
            ref double[] in_data, int blocksize, ref byte[] out_data)
        {
            fixed (double* i = in_data)
            {
                fixed (byte* o = out_data)
                {
                    return IMSCOMP.compress_buffer(i, blocksize, o);
                }
            }
        }
		public static unsafe int compress_lzf(ref byte[] in_data, int in_len, ref byte[] out_data, int out_len)
		{
			fixed (byte* i = in_data)
			{
				fixed (byte* o = out_data)
				{
					return IMSCOMP.lzf_compress(i, in_len, o, out_len);
				}
			}
		}
		public static unsafe int decompress_lzf(ref byte[] in_data, int in_len, ref byte[] out_data, int out_len)
		{
			fixed (byte* i = in_data)
			{
				fixed (byte* o = out_data)
				{
					return IMSCOMP.lzf_decompress(i, in_len, o, out_len);
				}
			}
		}
	}
}
