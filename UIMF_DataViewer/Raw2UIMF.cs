using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UIMF_DataViewer
{
    public class Raw2UIMF
    {
        private ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO rawData;

        private bool flag_FileOpen = false;

        public Raw2UIMF(string filename)
        {
            if ((this.rawData == null) && !this.flag_FileOpen)
            {
                this.rawData = new ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO();
                this.rawData.LoadMSMethodInfo = true;

                this.flag_FileOpen = this.rawData.OpenRawFile(filename);
            }
        }

        private string getTime()
        {
            string TimeInString = "";
            int hour = DateTime.Now.Hour;
            int min = DateTime.Now.Minute;
            int sec = DateTime.Now.Second;

            TimeInString = (hour < 10) ? "0" + hour.ToString() : hour.ToString();
            TimeInString += ":" + ((min < 10) ? "0" + min.ToString() : min.ToString());
            TimeInString += ":" + ((sec < 10) ? "0" + sec.ToString() : sec.ToString());
            return TimeInString;
        }

        public bool isOpen()
        {
            return this.flag_FileOpen;
        }

        public void ConvertBINtoUIMF(string bin_filename, string uimf_filename)
        {
            FileStream fs = new FileStream(bin_filename, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            int filelength = (int)fs.Length;
            int total_datapoints = ((int) filelength - 4) / (4+8+4);
            int total_scans = br.ReadInt32();
            //int total_scans = br.ReadInt32();
            //int total_bins = br.ReadInt32();

            long[] scans = new long[total_datapoints];
            double[] mzs = new double[total_datapoints];
            float[] intensities = new float[total_datapoints];

            for (int i = 0; i < total_datapoints; i++)
            {
                scans[i] = br.ReadInt32();
                mzs[i] = br.ReadDouble();
                intensities[i] = br.ReadSingle();

                //MessageBox.Show(scans[i].ToString() + ", " + bins[i].ToString() + ", " + mzs[i].ToString() + ", " + intensities[i].ToString());
            }

            //MessageBox.Show(total_datapoints.ToString() + ", " + total_scans.ToString());

            br.Close();
            fs.Close();

             // UIMF Filename
            UIMFLibrary.MZ_Calibrator mz_Calibration;

            UIMFLibrary.DataWriter uimf_writer = new UIMFLibrary.DataWriter();
            UIMFLibrary.GlobalParameters uimf_global = new UIMFLibrary.GlobalParameters();
            UIMFLibrary.FrameParameters uimf_frame = new UIMFLibrary.FrameParameters();

            uimf_writer.OpenUIMF(uimf_filename);
            uimf_writer.CreateTables(null);

            uimf_global.DateStarted = DateTime.Now.ToLongDateString();
            uimf_global.NumFrames = 1;
            uimf_global.TimeOffset = 0;
            uimf_global.BinWidth = .1;
            uimf_global.Bins = 1300000; // total_bins
            uimf_global.TOFCorrectionTime = 0;
            uimf_global.FrameDataBlobVersion = 0.1F;
            uimf_global.ScanDataBlobVersion = 0.1F;
            uimf_global.TOFIntensityType = "";
            uimf_global.DatasetType = "";
            uimf_global.Prescan_TOFPulses = 0;
            uimf_global.Prescan_Accumulations = 0;
            uimf_global.Prescan_TICThreshold = 0;
            uimf_global.Prescan_Continuous = false;
            uimf_global.Prescan_Profile = "";
            uimf_writer.InsertGlobal(uimf_global);

            uimf_frame.FrameNum = 1;

            double start_time = 10;
           // MessageBox.Show(start_time.ToString() + ", " + end_time.ToString());

            uimf_frame.StartTime = start_time;               // 1, Start time of frame, in minutes
            uimf_frame.Duration = 500; // 2, Duration of frame, in seconds
            uimf_frame.Accumulations = 1;                     // 3, Number of collected and summed acquisitions in a frame
            uimf_frame.FrameType = 0;                         // 4, Bitmap: 0=MS (Regular); 1=MS/MS (Frag); 2=Prescan; 4=Multiplex

            uimf_frame.Scans = total_scans;         // 5, Number of TOF scans
            uimf_frame.IMFProfile = "";			              // new, IMFProfile Name
            uimf_frame.TOFLosses = 0;			              // new TOF Losses
            uimf_frame.AverageTOFLength = 200000;                 // 6, Average time between TOF trigger pulses
            uimf_frame.CalibrationSlope = .35;               // 7, Value of k0
            uimf_frame.CalibrationIntercept = 0;              // 8, Value of t0
            uimf_frame.a2 = 0.0;	                          // The six parameters below are coefficients for residual mass error correction
            uimf_frame.b2 = 0.0;	                          // ResidualMassError=a2t+b2t^3+c2t^5+d2t^7+e2t^9+f2t^11
            uimf_frame.c2 = 0.0;
            uimf_frame.d2 = 0.0;
            uimf_frame.e2 = 0.0;
            uimf_frame.f2 = 0.0;
            uimf_frame.Temperature = 0.0;                     // 9, Ambient temperature
            uimf_frame.voltHVRack1 = 0.0;                     // 10, Voltage setting in the IMS system
            uimf_frame.voltHVRack2 = 0.0;                     // 11, Voltage setting in the IMS system
            uimf_frame.voltHVRack3 = 0.0;                     // 12, Voltage setting in the IMS system
            uimf_frame.voltHVRack4 = 0.0;                     // 13, Voltage setting in the IMS system
            uimf_frame.voltCapInlet = 0.0;                    // 14, Capilary Inlet Voltage
            uimf_frame.voltEntranceIFTIn = 0.0;               // 15, IFT In Voltage
            uimf_frame.voltEntranceIFTOut = 0.0;              // 16, IFT Out Voltage
            uimf_frame.voltEntranceCondLmt = 0.0;             // 17, Cond Limit Voltage
            uimf_frame.voltTrapOut = 0.0;                     // 18, Trap Out Voltage
            uimf_frame.voltTrapIn = 0.0;                      // 19, Trap In Voltage
            uimf_frame.voltJetDist = 0.0;                     // 20, Jet Disruptor Voltage
            uimf_frame.voltQuad1 = 0.0;                       // 21, Fragmentation Quadrupole Voltage
            uimf_frame.voltCond1 = 0.0;                       // 22, Fragmentation Conductance Voltage
            uimf_frame.voltQuad2 = 0.0;                       // 23, Fragmentation Quadrupole Voltage
            uimf_frame.voltCond2 = 0.0;                       // 24, Fragmentation Conductance Voltage
            uimf_frame.voltIMSOut = 0.0;                      // 25, IMS Out Voltage
            uimf_frame.voltExitIFTIn = 0.0;                   // 26, IFT In Voltage
            uimf_frame.voltExitIFTOut = 0.0;                  // 27, IFT Out Voltage
            uimf_frame.voltExitCondLmt = 0.0;                 // 28, Cond Limit Voltage
            uimf_frame.PressureFront = 0.0;                   // 29, Pressure at IMS entrance
            uimf_frame.PressureBack = 0.0;                    // 30, Pressure at IMS exit
            uimf_frame.MPBitOrder = (short)0;                // 31, Determines original size of bit sequence
            uimf_frame.FragmentationProfile = new double[0];  // 36, Voltage profile used in fragmentation
            uimf_writer.InsertFrame(uimf_frame);

            mz_Calibration = new UIMFLibrary.MZ_Calibrator((float)(uimf_frame.CalibrationSlope / 10000.0), (float)(uimf_frame.CalibrationIntercept * 10000.0));

            double[] lookup_table = new double[uimf_global.Bins];
            for (int i = 0; i < uimf_global.Bins; i++)
                lookup_table[i] = mz_Calibration.TOFtoMZ((double)i * (uimf_global.BinWidth * 10.0));
//            MessageBox.Show(lookup_table[uimf_global.Bins - 1].ToString());

            List<int> new_bins;
            List<int> new_intensities;
            long current_scan = 0;
            int count_per_scan;
            int current_lookup_index = 0;
            int prev_lookup_index = 0;
            int intensity_step;
            try
            {
                for (int i = 0; i < total_datapoints - 1; i++)
                {
                    // count elements in current scan
                    current_scan = scans[i];
                    count_per_scan = 0;
                    while (current_scan == scans[i + count_per_scan])
                        count_per_scan++;

                    //MessageBox.Show("count_per_scan " + count_per_scan.ToString());

                    new_bins = new List<int>();
                    new_intensities = new List<int>();
                    current_lookup_index = 0;

                    if (count_per_scan > 0)
                    {
                        prev_lookup_index = 0;
                        while (mzs[i] > lookup_table[prev_lookup_index])
                            prev_lookup_index++;
                        new_bins.Add(prev_lookup_index); // (int)(mz_Calibration.MZtoTOF((float)(mzs[i + j])) / ((double)(uimf_global.BinWidth * 10.0)));
                        new_intensities.Add((int)intensities[i]);

                        for (int j = 1; j < count_per_scan; j++)
                        {
                            while (mzs[i + j] > lookup_table[current_lookup_index])
                                current_lookup_index++;

                            if (current_lookup_index - prev_lookup_index < 20)
                            {
                                intensity_step = (int) ((intensities[i + j] - intensities[i + j - 1]) / (current_lookup_index - prev_lookup_index));
                                for (int k = 0; k < current_lookup_index-prev_lookup_index - 1; k++)
                                {
                                    new_bins.Add(prev_lookup_index + k + 1);
                                    new_intensities.Add((int)intensities[i + j - 1] + (intensity_step * (k+1)));
                                }
                            }
                            prev_lookup_index = current_lookup_index;

                            new_bins.Add(current_lookup_index); // (int)(mz_Calibration.MZtoTOF((float)(mzs[i + j])) / ((double)(uimf_global.BinWidth * 10.0)));
                            new_intensities.Add((int)intensities[i + j]);
                        }

                        uimf_writer.InsertScan(uimf_frame, (int) current_scan, new_bins, new_intensities, 1, 0);

                        new_bins.Clear();
                        new_intensities.Clear();
                        i += count_per_scan;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            //sw_test.Flush();
            //sw_test.Close();
            //fs_test.Close();

            uimf_writer.CloseUIMF(uimf_filename);
        }

        public void ConvertRAWtoUIMF(string uimf_filename)
        {
            double[] mzs = new double[1];
            double[] intensities = new double[1];
            DateTime acquisition_date = DateTime.Now;
            int last_scan = 0;
            ThermoRawFileReaderDLL.FinniganFileIO.FinniganFileReaderBaseClass.udtScanHeaderInfoType udtScanHeader;

            if (File.Exists(uimf_filename))
                File.Delete(uimf_filename);

            UIMFLibrary.MZ_Calibrator mz_Calibration;

            UIMFLibrary.DataWriter uimf_writer = new UIMFLibrary.DataWriter();
            UIMFLibrary.GlobalParameters uimf_global = new UIMFLibrary.GlobalParameters();
            UIMFLibrary.FrameParameters uimf_frame = new UIMFLibrary.FrameParameters();

            uimf_writer.OpenUIMF(uimf_filename, true);
            uimf_writer.CreateTables(null);

            acquisition_date = rawData.FileInfo.CreationDate;

            uimf_global.DateStarted = acquisition_date.ToLongDateString();
            uimf_global.NumFrames = 1;
            uimf_global.TimeOffset = 0;
            uimf_global.BinWidth = .1;
            uimf_global.Bins = 1300000; // total_bins
            uimf_global.TOFCorrectionTime = 0;
            uimf_global.FrameDataBlobVersion = 0.1F;
            uimf_global.ScanDataBlobVersion = 0.1F;
            uimf_global.TOFIntensityType = "";
            uimf_global.DatasetType = "";
            uimf_global.Prescan_TOFPulses = 0;
            uimf_global.Prescan_Accumulations = 0;
            uimf_global.Prescan_TICThreshold = 0;
            uimf_global.Prescan_Continuous = false;
            uimf_global.Prescan_Profile = "";
            uimf_writer.InsertGlobal(uimf_global);

            uimf_frame.FrameNum = 1;

            last_scan = this.rawData.GetNumScans();

            this.rawData.GetScanInfo(0, out udtScanHeader);

            uimf_frame.StartTime = DateTime.Now.Minute;               // 1, Start time of frame, in minutes
            uimf_frame.Duration = 500; // 2, Duration of frame, in seconds
            uimf_frame.Accumulations = 1;                     // 3, Number of collected and summed acquisitions in a frame
            uimf_frame.FrameType = 0;                         // 4, Bitmap: 0=MS (Regular); 1=MS/MS (Frag); 2=Prescan; 4=Multiplex

            uimf_frame.Scans = last_scan;         // 5, Number of TOF scans
            uimf_frame.IMFProfile = "";			              // new, IMFProfile Name
            uimf_frame.TOFLosses = 0;			              // new TOF Losses
            uimf_frame.AverageTOFLength = 200000;                 // 6, Average time between TOF trigger pulses
            uimf_frame.CalibrationSlope = .35;               // 7, Value of k0
            uimf_frame.CalibrationIntercept = 0;              // 8, Value of t0
            uimf_frame.a2 = 0.0;	                          // The six parameters below are coefficients for residual mass error correction
            uimf_frame.b2 = 0.0;	                          // ResidualMassError=a2t+b2t^3+c2t^5+d2t^7+e2t^9+f2t^11
            uimf_frame.c2 = 0.0;
            uimf_frame.d2 = 0.0;
            uimf_frame.e2 = 0.0;
            uimf_frame.f2 = 0.0;
            uimf_frame.Temperature = 0.0;                     // 9, Ambient temperature
            uimf_frame.voltHVRack1 = 0.0;                     // 10, Voltage setting in the IMS system
            uimf_frame.voltHVRack2 = 0.0;                     // 11, Voltage setting in the IMS system
            uimf_frame.voltHVRack3 = 0.0;                     // 12, Voltage setting in the IMS system
            uimf_frame.voltHVRack4 = 0.0;                     // 13, Voltage setting in the IMS system
            uimf_frame.voltCapInlet = 0.0;                    // 14, Capilary Inlet Voltage
            uimf_frame.voltEntranceIFTIn = 0.0;               // 15, IFT In Voltage
            uimf_frame.voltEntranceIFTOut = 0.0;              // 16, IFT Out Voltage
            uimf_frame.voltEntranceCondLmt = 0.0;             // 17, Cond Limit Voltage
            uimf_frame.voltTrapOut = 0.0;                     // 18, Trap Out Voltage
            uimf_frame.voltTrapIn = 0.0;                      // 19, Trap In Voltage
            uimf_frame.voltJetDist = 0.0;                     // 20, Jet Disruptor Voltage
            uimf_frame.voltQuad1 = 0.0;                       // 21, Fragmentation Quadrupole Voltage
            uimf_frame.voltCond1 = 0.0;                       // 22, Fragmentation Conductance Voltage
            uimf_frame.voltQuad2 = 0.0;                       // 23, Fragmentation Quadrupole Voltage
            uimf_frame.voltCond2 = 0.0;                       // 24, Fragmentation Conductance Voltage
            uimf_frame.voltIMSOut = 0.0;                      // 25, IMS Out Voltage
            uimf_frame.voltExitIFTIn = 0.0;                   // 26, IFT In Voltage
            uimf_frame.voltExitIFTOut = 0.0;                  // 27, IFT Out Voltage
            uimf_frame.voltExitCondLmt = 0.0;                 // 28, Cond Limit Voltage
            uimf_frame.PressureFront = 0.0;                   // 29, Pressure at IMS entrance
            uimf_frame.PressureBack = 0.0;                    // 30, Pressure at IMS exit
            uimf_frame.MPBitOrder = (short)0;                // 31, Determines original size of bit sequence
            uimf_frame.FragmentationProfile = new double[0];  // 36, Voltage profile used in fragmentation
            uimf_writer.InsertFrame(uimf_frame);

            mz_Calibration = new UIMFLibrary.MZ_Calibrator((uimf_frame.CalibrationSlope / 10000.0), (uimf_frame.CalibrationIntercept * 10000.0));

            double[] lookup_table = new double[uimf_global.Bins];
            for (int i = 0; i < uimf_global.Bins; i++)
                lookup_table[i] = mz_Calibration.TOFtoMZ((float)i * (uimf_global.BinWidth * 10.0));
            //MessageBox.Show(lookup_table[uimf_global.Bins - 1].ToString());

            List<int> new_bins;
            List<int> new_intensities;
            int current_lookup_index = 0;
            int prev_lookup_index = 0;
            int intensity_step;
            new_bins = new List<int>();
            new_intensities = new List<int>();

            for (int i = 0; i < last_scan; i++)
            {
                this.rawData.GetScanData(i, ref mzs, ref intensities, ref udtScanHeader);
                current_lookup_index = 0;

                if (mzs.Length > 0)
                {
                    prev_lookup_index = 0;
                    while (mzs[0] > lookup_table[prev_lookup_index])
                        prev_lookup_index++;

                    new_bins.Add(prev_lookup_index); // (int)(mz_Calibration.MZtoTOF((float)(mzs[i + j])) / ((double)(uimf_global.BinWidth * 10.0)));
                    new_intensities.Add((int)intensities[0]);

                    for (int j = 1; j < mzs.Length; j++)
                    {
                        while (mzs[j] > lookup_table[current_lookup_index])
                            if (++current_lookup_index > uimf_global.Bins)
                                break;

                        if (current_lookup_index - prev_lookup_index < 20)
                        {
                            intensity_step = (int)((intensities[j] - intensities[j - 1]) / (current_lookup_index - prev_lookup_index));
                            for (int k = 0; k < current_lookup_index - prev_lookup_index - 1; k++)
                            {
                                new_bins.Add(prev_lookup_index + k + 1);
                                new_intensities.Add((int)intensities[j - 1] + (intensity_step * (k + 1)));
                            }
                        }
                        prev_lookup_index = current_lookup_index;

                        new_bins.Add(current_lookup_index);
                        new_intensities.Add((int)intensities[j]);
                    }

                    uimf_writer.InsertScan(uimf_frame, i, new_bins, new_intensities, 1, 0);

                    this.rawData.GetScanInfo(i, out udtScanHeader);
                    uimf_writer.InsertScanParameters(i, udtScanHeader.MSLevel);
                }

                new_bins.Clear();
                new_intensities.Clear();
            }

            uimf_writer.CloseUIMF(uimf_filename);
        }

#if false
        private void btn_SelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Xcalibur files (*.RAW)|*.RAW|Agilent files (*.wiff)|*.wiff|Micromass files (_FUNC*.DAT)|_FUNC*.DAT|Bruker files(acqu)|acqu|S files ICR2LS Format(*.*)|*.*|S files SUN Extrel Format(*.*)|*.*|MZ Xml File(*.mzXML)|*.mzXML|PNNL IMF File(*.IMF)|*.IMF|Bruker Ascii peak File(*.ascii)|*.ascii|Raw Ascii File(*.txt)|*.txt|All files(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.rawData = new DeconToolsV2.Readers.clsRawData();

                this.rawData.LoadFile(openFileDialog1.FileName, DeconToolsV2.Readers.FileType.FINNIGAN);
                tb_SpectraFile.Text = openFileDialog1.FileName;
                btn_SendData.Enabled = true;
            }
        }

        private bool flag_doubleclick = false;
        public void mctl_spectra_DoubleClick(object obj, System.EventArgs e)
        {
            this.flag_doubleclick = true;
            this.ShowSpectrum(this.mintScanNum);
            this.flag_doubleclick = false;
        }

        public void ShowSpectrum(int new_scan_num)
        {
            float[] mzs = new float[1];
            float[] intensities = new float[1];

            if (new_scan_num > this.mintScanNum)
            {
                this.mintScanNum = new_scan_num;
                while ((mintScanNum < this.rawData.GetNumScans()) && (this.rawData.GetMSLevel(mintScanNum) != 1))
                    mintScanNum++;

                if (mintScanNum == this.rawData.GetNumScans())
                {
                    return;
                }
            }
            else
            {
                this.mintScanNum = new_scan_num;
                while ((mintScanNum > 0) && (this.rawData.GetMSLevel(mintScanNum) != 1))
                    mintScanNum--;
                if (mintScanNum == this.rawData.GetNumScans())
                {
                    return;
                }
            }

            this.rawData.GetSpectrum(mintScanNum, ref mzs, ref intensities);

            SmartMSComm.clsSpectrum spectrum = new SmartMSComm.clsSpectrum(mintScanNum, ref mzs, ref intensities, mintScanNum / 8527);
        }
#endif
    }
}
