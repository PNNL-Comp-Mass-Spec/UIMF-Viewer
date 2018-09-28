using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using UIMFLibrary;
using UIMF_File;

namespace UIMF_DataViewer.PostProcessing
{
    public enum Species
    {
        PEPTIDE,
        CALIBRANT
    }
    public enum PeakPicking
    {
        APEX,
        THREE_POINT_QUADRATIC
    }
    public enum Instrument
    {
        AGILENT_TDC = 0,
        SCIEX = 1
    }
    public enum CalibrationType
    {
        STANDARD,
        AGILENT,
        EXTERNAL
    }

    public struct Coefficients
    {
        public double ExperimentalIntercept;
        public double ExperimentalSlope;
    }

    /// <summary>
    /// Calibration Settings - to save to registry!
    /// </summary>
    public class CalibrationSettings
    {
        public bool[] IonSelection;
        public int NumCalibrants = 0;

        public CalibrationSettings()
        {
            IonSelection = new bool[NumCalibrants];
            for (int i = 0; i < NumCalibrants; i++)
                IonSelection[i] = false;
        }

        public static CalibrationSettings Load(RegistryKey parentKey)
        {
            CalibrationSettings p = new CalibrationSettings();

            try
            {
                SaviorClass.Savior.Read(p, parentKey.CreateSubKey("Calibration_Settings"));
            }
            catch (Exception ex)
            {
                p.IonSelection = new bool[p.NumCalibrants];
                for (int i = 0; i < p.NumCalibrants; i++)
                    p.IonSelection[i] = false;
            }
            return p;
        }

        public void Save(RegistryKey parentKey)
        {
            SaviorClass.Savior.Save(this, parentKey.CreateSubKey("Calibration_Settings"));
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>CODE BY MIKE BELOV TO CALIBRATE DATA</remarks>
    public class PostProcessingViewModel : ReactiveObject
    {
        private double ion1TOFBin;
        private double ion1Mz;
        private double ion2TOFBin;
        private double ion2Mz;
        private double calculatedSlope = 0.0;
        private double calculatedIntercept = 0.0;
        private double experimentalSlope = 0.0;
        private double experimentalIntercept = 0.0;
        private bool calibrationSuccessful;
        private bool showDecode;
        private string decodeSaveFilename;
        private string decodeSaveDirectory;
        private bool showCompress;
        private string compressSaveFilename;
        private string compressSaveDirectory;
        private UIMFDataWrapper uimfReader = null;

        public ReactiveList<CalibrantInfo> Calibrants { get; } = new ReactiveList<CalibrantInfo>(CalibrantInfo.GetDefaultCalibrants());

        public double Ion1TOFBin
        {
            get => ion1TOFBin;
            set => this.RaiseAndSetIfChanged(ref ion1TOFBin, value);
        }

        public double Ion1Mz
        {
            get => ion1Mz;
            set => this.RaiseAndSetIfChanged(ref ion1Mz, value);
        }

        public double Ion2TOFBin
        {
            get => ion2TOFBin;
            set => this.RaiseAndSetIfChanged(ref ion2TOFBin, value);
        }

        public double Ion2Mz
        {
            get => ion2Mz;
            set => this.RaiseAndSetIfChanged(ref ion2Mz, value);
        }

        public double CalculatedSlope
        {
            get => calculatedSlope;
            set => this.RaiseAndSetIfChanged(ref calculatedSlope, value);
        }

        public double CalculatedIntercept
        {
            get => calculatedIntercept;
            set => this.RaiseAndSetIfChanged(ref calculatedIntercept, value);
        }

        public double ExperimentalSlope
        {
            get => experimentalSlope;
            set => this.RaiseAndSetIfChanged(ref experimentalSlope, value);
        }

        public double ExperimentalIntercept
        {
            get => experimentalIntercept;
            set => this.RaiseAndSetIfChanged(ref experimentalIntercept, value);
        }

        public bool CalibrationSuccessful
        {
            get => calibrationSuccessful;
            set => this.RaiseAndSetIfChanged(ref calibrationSuccessful, value);
        }

        public bool ShowDecode
        {
            get => showDecode;
            set => this.RaiseAndSetIfChanged(ref showDecode, value);
        }

        public string DecodeSaveFilename
        {
            get => decodeSaveFilename;
            set => this.RaiseAndSetIfChanged(ref decodeSaveFilename, value);
        }

        public string DecodeSaveDirectory
        {
            get => decodeSaveDirectory;
            set => this.RaiseAndSetIfChanged(ref decodeSaveDirectory, value);
        }

        public bool ShowCompress
        {
            get => showCompress;
            set => this.RaiseAndSetIfChanged(ref showCompress, value);
        }

        public string CompressSaveFilename
        {
            get => compressSaveFilename;
            set => this.RaiseAndSetIfChanged(ref compressSaveFilename, value);
        }

        public string CompressSaveDirectory
        {
            get => compressSaveDirectory;
            set => this.RaiseAndSetIfChanged(ref compressSaveDirectory, value);
        }

        public ReactiveCommand<Unit, Unit> AttemptToCalibrateCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyCalculatedToFrameCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyExperimentalToAllFramesCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDecodeDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> DecodeExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseCompressDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> CompressExperimentCommand { get; }

        public PostProcessingViewModel()
        {
            ShowDecode = true;
            ShowCompress = true;

            AttemptToCalibrateCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CalibrateFrames()));
            ApplyCalculatedToFrameCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => ApplyCalculatedCalibration()));
            ApplyExperimentalToAllFramesCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => ApplyCalibrationAllFrames()));
            BrowseDecodeDirectoryCommand = ReactiveCommand.Create(DecodeDirectoryBrowse);
            //DecodeExperimentCommand;
            BrowseCompressDirectoryCommand = ReactiveCommand.Create(CompressDirectoryBrowse);
             CompressExperimentCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => Compress4GHzTo1GHzUIMF())); ;
        }

        public PostProcessingViewModel(RegistryKey mainKey, UIMFDataWrapper uimf) : this()
        {
            parent_key = mainKey;
            uimfReader = uimf;

            DecodeSaveFilename = Path.GetFileNameWithoutExtension(uimf.UimfDataFile);
            DecodeSaveDirectory = Path.GetDirectoryName(uimf.UimfDataFile);
            // TODO: get the first frame, and detect if there is a "Decoded" frame parameter with the value of "1"?
            ShowDecode = false; // TODO: False, because the demultiplexing code is not available.

            if (!uimf.UimfGlobalParams.BinWidth.Equals(0.25))
            {
                ShowCompress = false;
            }
            else
            {
                CompressSaveFilename = Path.GetFileNameWithoutExtension(uimf.UimfDataFile);
                CompressSaveDirectory = Path.GetDirectoryName(uimf.UimfDataFile);
            }

            this.settings_Calibration = new CalibrationSettings();
            //  BinResolution = bin_resolution;

#if COEFFICIENTS
            coef_Sciex.Slope = (double)0.35660427;
            coef_Sciex.Intercept = (double)-0.07565783;
            coef_Agilent.Slope = (double)0.57417985;
            coef_Agilent.Intercept = (double)0.03456597;
#endif

            elem_charge = 1.60217733 * Math.Pow(10, -19); // elementary charge, Coulomb
            k_b = 1.380658 * Math.Pow(10, -23);           // Boltzmann's constant, Joule/K
            AMU = 1.66055402 * Math.Pow(10, -27);         // atomic mass unit, kg
            n_Lodschmidt = 2.69 * Math.Pow(10, 25);       // Lodschmidt number density, m^-3

            //INITIAL PARAMETERS
            Sigma_av_reserpine = 300.0 * (double)Math.Pow(10, -20); // analyte collisional cross section, m^2
            t_modulation_IMS = 200 * (double)Math.Pow(10, -6);      // modulation bin width for IMS drift tube, s
            t_TOF = 200 * (double)Math.Pow(10, -6);                 // TOF spectrum acquisition rate, s
            spatial_step_0 = 50 * (double)Math.Pow(10, -6);         // initial granulation for IMS spatial distribution analysis, m
            TOF_step = (double)Math.Pow(10, -7);                    // granulation for TOF spectrum analysis, s
            slope_theor = (double)0.42 * (double)Math.Pow(10, 6);   // TOF calibration constant (ExperimentalSlope): sqrt(mass/charge) = ExperimentalSlope*t, where t is in s
            n_density = (double)(n_Lodschmidt * pressure_IMS / pressure_atm); // ions number density in the IMS drift tube, m^-3

            N_TOF_BINS_PER_MODULATION_BIN = (long)(t_modulation_IMS / TOF_step);
            N_TOF_BINS_PER_TOF_SCAN = (long)(t_TOF / TOF_step);
            N_TOF_BINS_PER_IMS_BIN = (long)(N_TIME_STEPS * N_TOF_BINS_PER_TOF_SCAN / N_PRS_BINS);
            N_SCANS_PER_MODULATION_BIN = (long)(t_modulation_IMS / t_TOF);

            this.CalibrationSuccessful = false;

            this.Load_Registry();
        }

        private CalibrantInfo currentCalibrant = new CalibrantInfo();

        private const int MIN_NUM_CALIBRANTS = 4;
        private const int NUM_CALIBRANTS = 25;
        private const int N_PRS_BINS = 511;
        private const double MAX_ERROR_ACCEPTABLE = 5.0;

        private const double T = 298;            // ambient temperature, K
        private const double pressure_IMS = 4;   // pressure in the IMS drift tube, torr
        private const double pressure_atm = 760; // atmospheric pressure, torr

        private const double mass_N2 = 14;         // molecular mass of nitrogen gas, Da
        private const double mass_reserpine = 609; // molecular mass of analyte (reserpine), Da
        private const double L_drift = 2;          // length of an IMS drift tube, m
        private const double U_drift = 4000;       //potential across IMS drift tube, V
        private const double N_0 = 100;            //initial number of ions per modulation bin
        private const double d_ext = (double)0.03; //width of TOF extraction region, m
        private const double E_field = U_drift / L_drift;  //electric field strength inside the IMS drift tube, V/m
        //private const double TOF_DELAY = 276000;
        private const double N_TIME_STEPS = 1022;
        private const double NOISE_INTERVAL = 30;
        private const double PEPTIDE_INTERVAL = 20;

#if COEFFICIENTS
        public Coefficients coef_Sciex;
        public Coefficients coef_Agilent;
#endif
        public Coefficients coef_Internal;

        //allocate memory for arrays with added zeros
        //  double[] arrival_time2; //drift time
        double[] arrival_time_TOF2; //arrival time in tof
        double[] mz2;
        double[] sum_intensity2;


        //INITIAL PARAMETERS
        private double elem_charge;         // elementary charge, Coulomb
        private double k_b;                 // Boltzmann's constant, Joule/K
        private double AMU;                 // atomic mass unit, kg
        private double n_Lodschmidt;        // Lodschmidt number density, m^-3
        private double Sigma_av_reserpine;  // analyte collisional cross section, m^2
        private double t_modulation_IMS;    // modulation bin width for IMS drift tube, s
        private double t_TOF;               // TOF spectrum acquisition rate, s
        private double spatial_step_0 = 50; // initial granulation for IMS spatial distribution analysis, m
        private double TOF_step;            // granulation for TOF spectrum analysis, s
        private double slope_theor;         // TOF calibration constant (ExperimentalSlope): sqrt(mass/charge) = ExperimentalSlope*t, where t is in s
        private double n_density;           // ions number density in the IMS drift tube, m^-3

        private long N_TOF_BINS_PER_MODULATION_BIN;
        private long N_TOF_BINS_PER_TOF_SCAN;
        private long N_TOF_BINS_PER_IMS_BIN;
        private long N_SCANS_PER_MODULATION_BIN;

        //   private double BinResolution;

        private int spectra_with_nonzeroentries = 0;

        RegistryKey parent_key;
        CalibrationSettings settings_Calibration;

        public double get_Experimental_Intercept()
        {
            return this.ExperimentalIntercept; //  this.coef_Internal.ExperimentalIntercept;
        }
        public double get_Experimental_Slope()
        {
            return this.ExperimentalSlope; // coef_Internal.ExperimentalSlope;
        }

        public void CalibrateFrame(double[] summed_spectrum, double[] sum_intensities, double[] bin_arrival_time, double bin_width, int total_bins, int total_scans, double mz_Experimental_Slope, double mz_Experimental_Intercept)
        {
            int i;
            int bins_per_frame = total_bins;
            int numEnabledCalibrants = 0;

            int max_error_index = 0;

            //this.Calibrants.ClearSelection();

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                for (i = 0; i < this.Calibrants.Count - 1; ++i)
                {
                    var calibrant = Calibrants[i];
                    calibrant.Bins = 0;
                    calibrant.TOFExperimental = 0;
                    calibrant.MzExperimental = 0;
                    calibrant.ErrorPPM = 0;
                    calibrant.NotFound = false;

                    if (calibrant.Enabled)
                    {
                        this.Calibrants[i].TOF = (double) ((Math.Sqrt(this.Calibrants[i].Mz) / this.ExperimentalSlope) + this.ExperimentalIntercept);
                        this.Calibrants[i].Bins = this.Calibrants[i].TOF / this.bin_Width;

                        numEnabledCalibrants++;
                    }
                }
            });

            while (numEnabledCalibrants >= MIN_NUM_CALIBRANTS)
            {
                this.spectra_with_nonzeroentries = sum_intensities.Length;

                this.set_ExperimentalCoefficients(mz_Experimental_Slope, mz_Experimental_Intercept);

                this.sum_intensity2 = new double[spectra_with_nonzeroentries];
                this.arrival_time_TOF2 = new double[spectra_with_nonzeroentries]; //arrival time in bins

                Array.Copy(sum_intensities, sum_intensity2, sum_intensities.Length);
                Array.Copy(bin_arrival_time, arrival_time_TOF2, bin_arrival_time.Length);

                this.mz2 = new double[spectra_with_nonzeroentries];
                for (i = 0; i < spectra_with_nonzeroentries; i++)
                {
                    // this.arrival_time_TOF2[i] *= bin_width;
                    mz2[i] = Math.Pow((this.arrival_time_TOF2[i] / 1000.0) - this.ExperimentalIntercept, 2) * this.ExperimentalSlope * this.ExperimentalSlope;
                }
                // mz_LIST2[i][k] = (float)pow((double)(arrival_time_LIST2[i][k] - *(TOF_offset_buffer + i) + TimeOffset) / 1000 - ExperimentalIntercept, 2) * (float)pow((double)ExperimentalSlope, 2);

                max_error_index = this.InternalCalibration(CalibrationType.STANDARD, Instrument.AGILENT_TDC, total_scans, numEnabledCalibrants);

                if (Math.Abs(this.Calibrants[max_error_index].ErrorPPM) < MAX_ERROR_ACCEPTABLE)
                {
                    this.set_ExperimentalCoefficients(coef_Internal.ExperimentalSlope, coef_Internal.ExperimentalIntercept);
                    break;
                }
                else
                {
                    var index = max_error_index;
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        this.CalculatedIntercept = 0;
                        this.CalculatedSlope = 0;

                        this.set_ExperimentalCoefficients(this.ExperimentalSlope, this.ExperimentalIntercept);
                        coef_Internal.ExperimentalSlope = this.ExperimentalSlope;
                        coef_Internal.ExperimentalIntercept = this.ExperimentalIntercept;

                        this.Calibrants[index].Enabled = false;

                        // TODO: (mz_experimental column): this.Calibrants[index].Cells[7].Value = "FAILED";

                        numEnabledCalibrants = 0;
                        for (i = 0; i < this.Calibrants.Count - 1; i++)
                        {
                            if (this.Calibrants[i].Enabled)
                                numEnabledCalibrants++;
                        }
                    });
                }
            }

            //MessageBox.Show("'" + this.Calibrants.Rows[7].Cells[6].Value.ToString() + "'  " + this.Calibrants.Rows[7].Cells[6].Value.ToString().Length.ToString() + "\n" + this.Calibrants[7].error_ppm.ToString());
#if false
            FileStream fs = new FileStream(@"C:\IonMobilityData\Calibration\NewCalib.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < spectra_with_nonzeroentries; i++)
            {
                sw.WriteLine(mz2[i].ToString("0.0000") + ", " + sum_intensity2[i].ToString() + ", " + arrival_time_TOF2[i].ToString());
            }
            sw.Flush();
            sw.Close();
            fs.Close();
#endif
        }

        public int InternalCalibration(CalibrationType cal_type, Instrument inst_type, int tofs_per_frame, int num_enabled_calibrants)
        {
            int numEnabledCalibrantsCorrected = 0;

            double sum_mz_term = 0;
            double sum_TOF_term = 0;
            double sum_TOF_term_squared = 0;
            double sum_mz_TOF_term = 0;

            bool flag_success = false;

            int max_error_index = 0;

            // now go to TOF spectra and find peak maxima
            numEnabledCalibrantsCorrected = num_enabled_calibrants;

            for (var i = 0; i < this.Calibrants.Count - 1; i++)
            {
                var calibrant = Calibrants[i];

                if (calibrant.Enabled) //calibrant.Mz > 0)
                {
                    currentCalibrant.Name = calibrant.Name;
                    currentCalibrant.Charge = calibrant.Charge;
                    currentCalibrant.Mz = calibrant.Mz;

                    var expTof = FindMonoisotopicPeak(calibrant.Bins, Species.CALIBRANT, PeakPicking.THREE_POINT_QUADRATIC, tofs_per_frame);
                    var index = i;

                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        calibrant.TOFExperimental = expTof;
                        if (calibrant.TOFExperimental.Equals(0))
                        {
                            numEnabledCalibrantsCorrected--;
                            calibrant.Enabled = false;
                            calibrant.NotFound = true;
                        }
                    });
                }
            }

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                //MessageBox.Show(this, "numEnabledCalibrants: "+numEnabledCalibrants.ToString());
                cal_type = CalibrationType.AGILENT;
                if (cal_type == CalibrationType.AGILENT)
                {
                    for (var i = 0; i < this.Calibrants.Count - 1; i++)
                    {
                        var calibrant = Calibrants[i];
                        if (calibrant.Enabled) //calibrant.Mz > 0)
                        {
                            // internal_calibrants_found = true;
                            if (calibrant.Mz > 0)
                            {
                                calibrant.MzExperimental = Math.Sqrt(calibrant.Mz);

                                sum_mz_term = sum_mz_term + calibrant.MzExperimental;
                                sum_TOF_term = sum_TOF_term + calibrant.TOFExperimental;
                                sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(calibrant.TOFExperimental, 2);
                                sum_mz_TOF_term = sum_mz_TOF_term + calibrant.MzExperimental * (calibrant.TOFExperimental);
                            }

                            coef_Internal.ExperimentalSlope = (numEnabledCalibrantsCorrected * sum_mz_TOF_term - sum_mz_term * sum_TOF_term) /
                                                              (numEnabledCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                            coef_Internal.ExperimentalIntercept = (sum_mz_term / numEnabledCalibrantsCorrected) -
                                                                  ((coef_Internal.ExperimentalSlope * sum_TOF_term) / numEnabledCalibrantsCorrected);
                            coef_Internal.ExperimentalIntercept = -coef_Internal.ExperimentalIntercept / coef_Internal.ExperimentalSlope;
                        }
                    }
                }

                // check the results
                for (var i = 0; i < this.Calibrants.Count - 1; ++i)
                    if (this.Calibrants[i].Enabled)
                    {
                        max_error_index = i;
                        break;
                    }

                flag_success = true;
                for (var i = max_error_index; i < this.Calibrants.Count - 1; ++i)
                {
                    var calibrant = Calibrants[i];
                    if (calibrant.Enabled)
                    {
                        calibrant.MzExperimental = Math.Pow((calibrant.TOFExperimental - coef_Internal.ExperimentalIntercept) * coef_Internal.ExperimentalSlope, 2);
                        calibrant.ErrorPPM = ((calibrant.MzExperimental - calibrant.Mz) / calibrant.Mz) * 1e6;

                        if (Math.Abs(calibrant.ErrorPPM) > Math.Abs(this.Calibrants[max_error_index].ErrorPPM))
                            max_error_index = i;

                        if (calibrant.TOFExperimental <= 0)
                        {
                            flag_success = false;
                        }
                    }
                }

                this.CalibrationSuccessful = flag_success;
            });

            return max_error_index;
        }

        /// <summary>
        /// routine for finding monoisotopic peak of a peptide
        /// </summary>
        /// <param name="TOF_peptide"></param>
        /// <param name="species_ID"></param>
        /// <param name="peak_picking"></param>
        /// <param name="spectra_with_nonzeroentries"></param>
        /// <returns></returns>
        private double FindMonoisotopicPeak(double TOF_peptide, Species species_ID, PeakPicking peak_picking, int spectra_with_nonzeroentries)
        {
            int i;
            int j;

            double SNR_noise_level;

            int peak_number = 0;
            double charge = 1;
            int peak_number_mono = 11;
            int NumNoiseBins = 0;
            int pos_peptide_max = 0;
            int[] Pos_peptide_max = new int[10];

            double TOF_monoisotope = 0.0;
            double isotope_ratio;
            double[] peptide_local_max = new double[10];
            double[] TOF_offset_local = new double[10];
            double TOF_monoisotope_shift = 0;
            double TOF_checkup = 0;
            bool TOF_MONOISOTOPE_FOUND = false;
            bool PEAK_COUNTER;
            bool[,] TOF_MONOISOTOPE = new bool[10, 10];
            long SIGNAL_THRESHOLD = 5;
            bool found_peptide_pos = false;

            int Pos_noise = 0;
            int Pos_peptide = 0;
            int SNR = 3;
            int NumNonZeroNoiseBins = 0;
            double noise_intensity_average = 0;
            double noise_intensity0 = 0;

            /*
            int bins_per_tof = 0;
            if (BinResolution <= 3)
                bins_per_tof = 80;
            else if (BinResolution <= 3.5)
                bins_per_tof = 60;  //3.3219 gives 1.0 ns
            else if (BinResolution >= 3.5)
                bins_per_tof = 40;
            */

            double[] peptide_array_local = new double[80];

            // three point quadratic declarations
            double sum_x4 = 0;
            double sum_x3 = 0;
            double sum_x2 = 0;
            double sum_x = 0;
            double sum_yx2 = 0;
            double sum_yx = 0;
            double sum_y = 0;

            double pCoefficientA = 0.0;
            double pCoefficientB = 0.0;
            double pCoefficientC = 0.0;

            double[,] pVariable;
            double peptide_monoisotope_max = -1; // initial condition, no peaks found yet

#if false
            if (species_ID == Species.PEPTIDE)
            {
                charge = current_Peptide.charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                TOF_monoisotope_shift = (double)(1000.0 / (this.ExperimentalSlope * charge * 2.0 * Math.Sqrt(current_Peptide.mz))); //spacing between isotopes
            }
            else if (species_ID == Species.CALIBRANT)
#endif
            {
                charge = currentCalibrant.Charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                TOF_monoisotope_shift = (double)(1000.0 / (this.ExperimentalSlope * charge * 2.0 * Math.Sqrt(currentCalibrant.Mz))); //spacing between isotopes
            }

            // TOF_checkup = 2 * Math.Pow(2, BinResolution) / 10;
            TOF_checkup = 2.0 / this.bin_Width; // *Math.Pow(2, 2) / 10;

            //TOF_monoisotope_shift = 1000;
            // TOF_checkup = 200;
            //MessageBox.Show(this, "TOF_checkup=" + TOF_checkup.ToString() + ", TOF_monoisotope_shift=" + TOF_monoisotope_shift.ToString());

            isotope_ratio = 2.5;
            if (charge == 1)
            {
                isotope_ratio = 10.0;
                SNR = 2;
            }

            Pos_peptide = 0;
            for (i = 0; i < this.spectra_with_nonzeroentries; i++)
            {
                if ((TOF_peptide - this.arrival_time_TOF2[i]) < PEPTIDE_INTERVAL / this.bin_Width)
                {
                    Pos_peptide = i;
                    found_peptide_pos = true;
                    break;
                }
            }
            // MessageBox.Show(this, "found_peptide_pos: " + found_peptide_pos.ToString()+ "  "+(this.arrival_time_TOF2[i]* this.bin_Width).ToString() );

            if (!found_peptide_pos)
                return TOF_monoisotope = 0;

            peak_number = 0;
            for (i = 0; i < 10; i++)
            {
                peptide_local_max[i] = 0;
                TOF_offset_local[i] = 0;
                Pos_peptide_max[i] = 0;
            }

            for (i = 0; i < 10; i++)
                for (j = 0; j < 10; j++)
                    TOF_MONOISOTOPE[i, j] = false;

            // estimate average noise intensity at the left wing of isotopic distribution
            i = 0;
            do
            {
                Pos_noise = Pos_peptide - i;
                ++i;
            } while ((Pos_noise > 0) && (this.arrival_time_TOF2[Pos_peptide] - this.arrival_time_TOF2[Pos_noise] < NOISE_INTERVAL / this.bin_Width));

            NumNoiseBins = i - 1;
            NumNonZeroNoiseBins = 0;
            noise_intensity_average = 0;

            if (this.arrival_time_TOF2[Pos_peptide] - this.arrival_time_TOF2[Pos_noise] > NOISE_INTERVAL / this.bin_Width)
            {
                Pos_noise = Pos_noise + 1;
                NumNoiseBins = i - 2;
            }

            try
            {
                for (i = Pos_noise; i < Pos_noise + NumNoiseBins; ++i)
                {
                    if (this.sum_intensity2[i] > 0 && NumNonZeroNoiseBins == 0)
                    {
                        NumNonZeroNoiseBins++;
                        noise_intensity_average = noise_intensity_average + this.sum_intensity2[i];
                        noise_intensity0 = noise_intensity_average;
                    }

                    if (this.sum_intensity2[i] > 0 && NumNonZeroNoiseBins > 0) /*&& fabs(this.sum_intensity2[i]/noise_intensity0) < 10*/
                    {
                        NumNonZeroNoiseBins++;
                        noise_intensity_average = noise_intensity_average + this.sum_intensity2[i];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("arg" + ex.ToString());
            }

            // finding average noise for all NumNoiseBins without taking into account the outlyars
            if (NumNonZeroNoiseBins > 0)
                noise_intensity_average = noise_intensity_average / NumNonZeroNoiseBins / 4.0;

            for (i = Pos_peptide; (i < Pos_peptide + 80) && (i < sum_intensity2.Length); ++i)
            {
                // now identify isotopic peaks
                if ((i > 4) && (peak_number < 10))
                {
                    SNR_noise_level = noise_intensity_average * SNR;

                    if (((this.sum_intensity2[i] > SIGNAL_THRESHOLD) && (this.sum_intensity2[i] > SNR_noise_level)) ||
                        ((this.sum_intensity2[i - 4] > SIGNAL_THRESHOLD) && (this.sum_intensity2[i - 4] > SNR_noise_level)) ||
                        ((this.sum_intensity2[i - 3] > SIGNAL_THRESHOLD) && (this.sum_intensity2[i - 3] > SNR_noise_level)) ||
                        ((this.sum_intensity2[i - 2] > SIGNAL_THRESHOLD) && (this.sum_intensity2[i - 2] > SNR_noise_level)) ||
                        ((this.sum_intensity2[i - 1] > SIGNAL_THRESHOLD) && (this.sum_intensity2[i - 1] > SNR_noise_level)))
                    {
                        if ((this.sum_intensity2[i - 2] > this.sum_intensity2[i - 1]) && //
                            (this.sum_intensity2[i - 1] > this.sum_intensity2[i]) &&     // fixed this line
                            (this.sum_intensity2[i - 3] < this.sum_intensity2[i - 2]) && //
                            (this.sum_intensity2[i - 4] < this.sum_intensity2[i - 2]))   //
                        {
                            peptide_local_max[peak_number] = this.sum_intensity2[i - 2];
                            TOF_offset_local[peak_number] = this.arrival_time_TOF2[i - 2]; // define file pointer position corresponding to the local max signal
                            Pos_peptide_max[peak_number] = i - 2;
                            peak_number++;
                        }
                    }
                }
            }

            if ((peak_number < 3) && (charge != 1))
            {
                // didn't find isotopic cluster for multiply charged states
                TOF_monoisotope = 0;
                return TOF_monoisotope;
            }

            double peptide_local_max1 = peptide_local_max[0];

            // Analyze all combinations of found peptide peaks and find the two matching the criterium of TOF_checkup.
            // Then select the smallest index as peak_number_mono
            if ((species_ID == Species.PEPTIDE) || (species_ID == Species.CALIBRANT))
            {
                for (i = 0; i < peak_number; ++i)
                {
                    for (j = i + 1; j < peak_number; ++j)
                    {
                        if (Math.Abs(Math.Abs(TOF_offset_local[i] - TOF_offset_local[j]) - TOF_monoisotope_shift) < TOF_checkup * 10.0)
                        {
                            if (peptide_local_max[i] != 0)
                            {
                                if ((peptide_local_max[i] / peptide_local_max[j] < isotope_ratio && peptide_local_max[i] / peptide_local_max[j] > 1 / isotope_ratio) || (peptide_local_max[j] / peptide_local_max[i] < isotope_ratio && peptide_local_max[j] / peptide_local_max[i] > 1 / isotope_ratio))
                                {
                                    if (i < j && peak_number_mono > i)
                                    {
                                        PEAK_COUNTER = false;
                                        if (j - i > 1 && (charge != 1)) // verifying that peaks in between the putative isotopic peaks are less than both isotopes
                                        {
                                            for (int k = i + 1; k <= j - 1; ++k)
                                            {
                                                if (peptide_local_max[k] > peptide_local_max[i] || peptide_local_max[k] > peptide_local_max[j] || PEAK_COUNTER)
                                                {
                                                    TOF_MONOISOTOPE[i, j] = false;
                                                    PEAK_COUNTER = true; //accounts for the case of several peaks between putative isotopic peaks
                                                }
                                                else
                                                {
                                                    peak_number_mono = i;
                                                    TOF_MONOISOTOPE[i, j] = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            TOF_MONOISOTOPE[i, j] = true;
                                            peak_number_mono = i;
                                        }
                                    }
                                    if (i > j && peak_number_mono > j)
                                    {
                                        PEAK_COUNTER = false;
                                        if (i - j > 1 && (charge != 1)) // verifying that intensities of peaks in between the putative isotopic peaks are less than that of both isotopes
                                        {
                                            for (int k = j + 1; k <= i - 1; ++k)
                                            {
                                                if (peptide_local_max[k] > peptide_local_max[i] || peptide_local_max[k] > peptide_local_max[j] || PEAK_COUNTER)
                                                {
                                                    TOF_MONOISOTOPE[i, j] = false;
                                                    PEAK_COUNTER = true;
                                                }
                                                else
                                                {
                                                    peak_number_mono = j;
                                                    TOF_MONOISOTOPE[i, j] = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            TOF_MONOISOTOPE[i, j] = true;
                                            peak_number_mono = j;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // check if monoisotopic peak was ever found
            TOF_MONOISOTOPE_FOUND = false;
            for (i = 0; i < 10; i++)
                for (j = 0; j < 10; j++)
                {
                    if (TOF_MONOISOTOPE[i, j])
                        TOF_MONOISOTOPE_FOUND = true;
                }

            if (peak_number_mono > 10 || !TOF_MONOISOTOPE_FOUND)
                TOF_monoisotope = 0;
            else if (TOF_MONOISOTOPE_FOUND)
            {
                // if PeakPicking.APEX peak picking - return TOF monoisotope from the next line
                if (peak_picking == PeakPicking.APEX)
                {
                    TOF_monoisotope = (double)Pos_peptide_max[peak_number_mono];
                    peptide_monoisotope_max = peptide_local_max[peak_number_mono];
                }

                // if THREE POINT QUADRATIC peak picking - return TOF monoisotope after routine below
                if (peak_picking == PeakPicking.THREE_POINT_QUADRATIC)
                {
                    for (i = 0; i < 3; ++i)
                    {
                        //sum_x4 = sum_x4 + Math.Pow((double)((Pos_peptide_max[peak_number_mono]+sizeof(double)*(i-1))/sizeof(double) - fSpectrumNumber * NumElementsIn)*AcquisitionBin + offset_mariner,4);
                        sum_x4 = sum_x4 + Math.Pow(i, 4);
                        sum_x3 = sum_x3 + Math.Pow(i, 3);
                        sum_x2 = sum_x2 + Math.Pow(i, 2);
                        sum_x = sum_x + i;

                        pos_peptide_max = Pos_peptide_max[peak_number_mono] + (i - 1);

                        sum_yx2 = sum_yx2 + this.sum_intensity2[pos_peptide_max] * i * i;
                        sum_yx = sum_yx + this.sum_intensity2[pos_peptide_max] * i;
                        sum_y = sum_y + this.sum_intensity2[pos_peptide_max];
                    }

                    double[,] local_Variable = {{sum_x4, sum_x3, sum_x2, sum_yx2},
                                           {sum_x3, sum_x2, sum_x, sum_yx},
                                           {sum_x2, sum_x,  3, sum_y}};

                    pVariable = (double[,])local_Variable.Clone();

                    OnQuadraticLeastSquareFit(pVariable, out pCoefficientA, out pCoefficientB, out pCoefficientC);

                    if (pCoefficientA != 0)
                        TOF_monoisotope = -pCoefficientB / (2 * pCoefficientA);
                    else
                        TOF_monoisotope = 0;

                    TOF_monoisotope = TOF_monoisotope + (this.arrival_time_TOF2[pos_peptide_max - 2] * this.bin_Width);

                    //   if (TOF_monoisotope > 110000)
                    //      TOF_monoisotope = 0; //set TOF to zero if outside of m/z range

                    peptide_monoisotope_max = peptide_local_max[peak_number_mono];
                }
            }
            //MessageBox.Show(this, "TOF_monoisotope: "+TOF_monoisotope.ToString());
            TOF_monoisotope = TOF_monoisotope / 1000;

            return TOF_monoisotope;
        }

        void OnQuadraticLeastSquareFit(double[,] Variable, out double pCoefficientA, out double pCoefficientB, out double pCoefficientC)
        {
            double Determinant0, DeterminantA, DeterminantB, DeterminantC;
            int i = 0, j = 0;

            pCoefficientA = 0;
            pCoefficientB = 0;
            pCoefficientC = 0;

            Determinant0 = Math.Pow(-1, i + j) * Variable[i, j] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 2] - Variable[i + 2, j + 1] * Variable[i + 1, j + 2]);
            Determinant0 = Determinant0 + Math.Pow(-1, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j] * Variable[i + 2, j + 2] - Variable[i + 2, j] * Variable[i + 1, j + 2]);
            Determinant0 = Determinant0 + Math.Pow(-1, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j] * Variable[i + 2, j + 1] - Variable[i + 2, j] * Variable[i + 1, j + 1]);

            DeterminantA = Math.Pow(-1, i + j) * Variable[i, j + 3] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 2] - Variable[i + 2, j + 1] * Variable[i + 1, j + 2]);
            DeterminantA = DeterminantA + Math.Pow(-1, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 2] - Variable[i + 2, j + 3] * Variable[i + 1, j + 2]);
            DeterminantA = DeterminantA + Math.Pow(-1, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 1] - Variable[i + 2, j + 3] * Variable[i + 1, j + 1]);

            DeterminantB = Math.Pow(-1, i + j) * Variable[i, j] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 2] - Variable[i + 2, j + 3] * Variable[i + 1, j + 2]);
            DeterminantB = DeterminantB + Math.Pow(-1, i + j + 1) * Variable[i, j + 3] * (Variable[i + 1, j] * Variable[i + 2, j + 2] - Variable[i + 2, j] * Variable[i + 1, j + 2]);
            DeterminantB = DeterminantB + Math.Pow(-1, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j] * Variable[i + 2, j + 3] - Variable[i + 2, j] * Variable[i + 1, j + 3]);

            DeterminantC = Math.Pow(-1, i + j) * Variable[i, j] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 3] - Variable[i + 2, j + 1] * Variable[i + 1, j + 3]);
            DeterminantC = DeterminantC + Math.Pow(-1, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j] * Variable[i + 2, j + 3] - Variable[i + 2, j] * Variable[i + 1, j + 3]);
            DeterminantC = DeterminantC + Math.Pow(-1, i + j + 2) * Variable[i, j + 3] * (Variable[i + 1, j] * Variable[i + 2, j + 1] - Variable[i + 2, j] * Variable[i + 1, j + 1]);

            if (Determinant0 != 0)
            {
                pCoefficientA = DeterminantA / Determinant0;
                pCoefficientB = DeterminantB / Determinant0;
                pCoefficientC = DeterminantC / Determinant0;
            }
            else
            {
                pCoefficientA = 0;
                pCoefficientB = 0;
                pCoefficientC = 0;
            }
        }

        double bin_Width = 0;
        public void InitializeCalibrants(double binWidth, double calibrationSlope, double calibrationIntercept)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                using (Calibrants.SuppressChangeNotifications())
                {
                    Calibrants.Clear();
                    Calibrants.AddRange(CalibrantInfo.GetDefaultCalibrants());
                }
            });

            CalculateCalibrantExperimentalValues(binWidth, calibrationSlope, calibrationIntercept);
            for (int i = 0; (i < this.settings_Calibration.NumCalibrants) && (i < this.Calibrants.Count); i++)
                this.Calibrants[i].Enabled = this.settings_Calibration.IonSelection[i];
        }

        public void CalculateCalibrantExperimentalValues(double binWidth, double calibrationSlope, double calibrationIntercept)
        {
            this.bin_Width = binWidth;

            this.set_ExperimentalCoefficients(calibrationSlope, calibrationIntercept);
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                foreach (var calibrant in Calibrants)
                {
                    try
                    {
                        calibrant.TOF = Math.Sqrt(calibrant.Mz) / this.ExperimentalSlope + this.ExperimentalIntercept;
                        calibrant.Bins = (calibrant.TOF) * 1000.0 / binWidth;

                        // MessageBox.Show(this.ExperimentalSlope.ToString() + " " + this.ExperimentalIntercept.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("CalculateCalibrantExperimentalValues:  " + calibrant.ToString() + "\n\n" + ex.ToString());
                    }
                }
            });
        }

        // this is all dealing with calibrants

        private void num_CalculateCalibration_ValueChanged(object sender, EventArgs e)
        {
            double bin1 = Convert.ToDouble(this.Ion1TOFBin);
            double bin2 = Convert.ToDouble(this.Ion2TOFBin);
            double mz1 = Convert.ToDouble(this.Ion1Mz);
            double mz2 = Convert.ToDouble(this.Ion2Mz);

            if ((bin1 == 0.0) || (bin2 == 0.0) || (mz1 == 0.0) || (mz2 == 0.0))
            {
                return;
            }

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                this.CalculatedIntercept = (Math.Sqrt(mz2) * bin1 - Math.Sqrt(mz1) * bin2) / (Math.Sqrt(mz2) - Math.Sqrt(mz1));
                this.CalculatedSlope = Math.Sqrt(mz1) / (bin1 - this.CalculatedIntercept);
            });
        }

        public void set_ExperimentalCoefficients(double slope, double intercept)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                this.ExperimentalSlope = slope;
                this.ExperimentalIntercept = intercept;
            });
        }

        public void update_Calibrants()
        {
            for (int i = 0; i < this.settings_Calibration.NumCalibrants && i < Calibrants.Count; i++)
            {
                this.settings_Calibration.IonSelection[i] = this.Calibrants[i].Enabled;
            }
        }

        public void Load_Registry()
        {
            this.settings_Calibration = CalibrationSettings.Load(this.parent_key);
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                for (int i = 0; (i < this.settings_Calibration.NumCalibrants) && (i < this.Calibrants.Count); i++)
                    this.Calibrants[i].Enabled = this.settings_Calibration.IonSelection[i];
            });
        }

        public void Save_Registry()
        {
            this.settings_Calibration.NumCalibrants = this.Calibrants.Count;

            this.settings_Calibration.IonSelection = new bool[this.settings_Calibration.NumCalibrants];
            update_Calibrants();

            this.settings_Calibration.Save(this.parent_key);
        }

        // /////////////////////////////////////////////////////////////////////////////
        // logic developed by Matt Monroe 06/23/2011
        //
        public int get_CalibrantCountMatched()
        {
            int count = 0;

            for (int j = 0; j < this.Calibrants.Count - 1; ++j)
            {
                if ((this.Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                }
            }
            return count;
        }

        public int get_CalibrantCountValid()
        {
            int count = 0;

            for (int j = 0; j < this.Calibrants.Count - 1; ++j)
            {
                if ((this.Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 10.0))
                {
                    count++;
                }
            }
            return count;
        }

        public double get_AverageMassError()
        {
            int count = 0;
            double error = 0.0;

            for (int j = 0; j < this.Calibrants.Count - 1; ++j)
            {
                if ((this.Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                    error += Calibrants[j].ErrorPPM;
                }
            }

            return error / (double)count;
        }

        public double get_AverageAbsoluteValueMassError()
        {
            int count = 0;
            double error = 0.0;

            for (int j = 0; j < this.Calibrants.Count - 1; ++j)
            {
                if ((this.Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                    error += Math.Abs(Calibrants[j].ErrorPPM);
                }
            }

            return error / (double)count;
        }

        public int disable_CalibrantMaxPPMError()
        {
            int count_calibrants = 0;
            double error_max = 0.0;
            int index_max = 0;

            for (int j = 0; j < this.Calibrants.Count - 1; ++j)
            {
                if (this.Calibrants[j].Enabled)
                {
                    count_calibrants++;
                    if (error_max < Math.Abs(Calibrants[j].ErrorPPM))
                    {
                        error_max = Math.Abs(Calibrants[j].ErrorPPM);
                        index_max = j;
                    }
                }
            }

            count_calibrants--;

            RxApp.MainThreadScheduler.Schedule(() => this.Calibrants[index_max].Enabled = false);

            return count_calibrants;
        }

        private void DecodeDirectoryBrowse()
        {
            var folderBrowser = new CommonOpenFileDialog();
            folderBrowser.IsFolderPicker = true;
            folderBrowser.Title = "Select Decoded UIMF Experiment Folder";
            if (Directory.Exists(DecodeSaveDirectory))
            {
                folderBrowser.InitialDirectory = DecodeSaveDirectory;
            }

            var result = folderBrowser.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                RxApp.MainThreadScheduler.Schedule(() => DecodeSaveDirectory = folderBrowser.FileName);
            }
        }

        private void CompressDirectoryBrowse()
        {
            var folderBrowser = new CommonOpenFileDialog();
            folderBrowser.IsFolderPicker = true;
            folderBrowser.Title = "Select Compressed 1GHz UIMF Experiment Folder";
            if (Directory.Exists(CompressSaveDirectory))
            {
                folderBrowser.InitialDirectory = CompressSaveDirectory;
            }

            var result = folderBrowser.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                RxApp.MainThreadScheduler.Schedule(() => CompressSaveDirectory = folderBrowser.FileName);
            }
        }

        #region Calibration

        public event EventHandler CalibrationChanged;

        private void OnCalibrationChanged()
        {
            CalibrationChanged?.Invoke(this, EventArgs.Empty);
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Internal Calibration
        //
        private void ApplyCalculatedCalibration()
        {
            this.uimfReader.UpdateCalibrationCoefficients(this.uimfReader.CurrentFrameIndex, (float)this.CalculatedSlope,
                (float)this.CalculatedIntercept);

            OnCalibrationChanged();

            this.InitializeCalibrants(this.uimfReader.UimfGlobalParams.BinWidth, this.CalculatedSlope, this.CalculatedIntercept);
        }

        private void ApplyCalibrationAllFrames()
        {
            //MessageBox.Show((Convert.ToDouble(this.tb_CalA.Text) * 10000.0).ToString() + "  " + this.pnl_postProcessing.Experimental_Slope.ToString());
            this.uimfReader.UpdateAllCalibrationCoefficients((float)this.get_Experimental_Slope(),
                (float)this.get_Experimental_Intercept());

            OnCalibrationChanged();

            this.InitializeCalibrants(this.uimfReader.UimfGlobalParams.BinWidth, this.get_Experimental_Slope(), this.get_Experimental_Intercept());
        }

        private void CalibrateFrames()
        {
            var flagAutoCalibrate = false;
            this.update_Calibrants();

            this.CalibrateFrame(this.uimfReader.CurrentFrameIndex, out var slope, out var intercept, out _);

            if (double.IsNaN(slope) || double.IsNaN(intercept))
            {
                var dr = MessageBox.Show("Calibration failed.\n\nShould I continue?", "Calibration failed", MessageBoxButton.OKCancel);
                if (dr == MessageBoxResult.Cancel)
                    return;
            }
            else if (slope <= 0)
            {
                //MessageBox.Show(this, "Calibration Failed");
                return;
            }
            else
            {
                this.uimfReader.MzCalibration.K = slope / 10000.0;
                this.uimfReader.MzCalibration.T0 = intercept * 10000.0;
            }

            if (flagAutoCalibrate)
                this.uimfReader.UpdateAllCalibrationCoefficients(slope, intercept, flagAutoCalibrate);

            OnCalibrationChanged();
        }

        private void CalibrateFrame(int frame_index, out double calibration_slope, out double calibration_intercept, out int total_calibrants_matched)
        {
            int i, j, k;
            int scans;

            int uimf_bins;
            int maximum_spectrum = 0;

            double[] nonzero_bins;
            double[] nonzero_intensities;
            int above_noise_bins = 0;
            int compressed_bins = 0;
            int added_zeros = 0;

            int NOISE_REGION = 50;
            int noise_peaks = 0;
            int noise_intensity = 0;
            int compression;
            double[] summed_spectrum;
            bool[] flag_above_noise;
            double[] spectrum = new double[this.uimfReader.UimfGlobalParams.Bins];
            int[] max_spectrum = new int[this.uimfReader.UimfGlobalParams.Bins];
            int[] bins = new int[this.uimfReader.UimfGlobalParams.Bins];

            double slope = this.uimfReader.UimfFrameParams.CalibrationSlope;
            double intercept = this.uimfReader.UimfFrameParams.CalibrationIntercept;

            int CalibrantCountMatched = 100;
            int CalibrantCountValid = 0;
            double AverageAbsoluteValueMassError = 0.0;
            double AverageMassError = 0.0;

            if (this.uimfReader.UimfGlobalParams.BinWidth == .25)
                compression = 4;
            else
                compression = 1;

            calibration_slope = -1.0;
            calibration_intercept = -1.0;
            total_calibrants_matched = 0;

            summed_spectrum = new double[this.uimfReader.UimfGlobalParams.Bins / compression];
            flag_above_noise = new bool[this.uimfReader.UimfGlobalParams.Bins / compression];

            if (CalibrantCountMatched > 4)
            {
                // clear arrays
                for (i = 0; i < this.uimfReader.UimfGlobalParams.Bins / compression; i++)
                {
                    flag_above_noise[i] = false;
                    max_spectrum[i] = 0;
                    summed_spectrum[i] = 0;
                    max_spectrum[i] = 0;
                }

                bins = this.uimfReader.GetSumScans(this.uimfReader.ArrayFrameNum[frame_index], 0, this.uimfReader.UimfFrameParams.Scans);

                for (j = 0; j < bins.Length; j++)
                {
                    summed_spectrum[j / compression] += bins[j];

                    if (max_spectrum[j / compression] < summed_spectrum[j / compression])
                    {
                        max_spectrum[j / compression] = (int)summed_spectrum[j / compression];

                        if (maximum_spectrum < summed_spectrum[j / compression])
                            maximum_spectrum = (int)summed_spectrum[j / compression];
                    }
                }

                // determine noise level and filter summed spectrum
                for (j = NOISE_REGION / 2; (j < (this.uimfReader.UimfGlobalParams.Bins / compression) - NOISE_REGION); j++)
                {
                    // get the total intensity and divide by the number of peaks
                    noise_peaks = 0;
                    noise_intensity = 0;
                    for (k = j - (NOISE_REGION / 2); k < j + (NOISE_REGION / 2); k++)
                    {
                        if (max_spectrum[k] > 0)
                        {
                            noise_intensity += max_spectrum[k];
                            noise_peaks++;
                        }
                    }

                    if (noise_peaks > 0)
                    {
                        if (max_spectrum[j] > noise_intensity / noise_peaks) // the average level...
                            flag_above_noise[j] = true;
                    }
                    else
                        flag_above_noise[j] = false;
                }

                // calculate size of the array of filtered sum spectrum for calibration routine
                above_noise_bins = 0;
                added_zeros = 0;
                for (i = 1; i < this.uimfReader.UimfGlobalParams.Bins / compression; i++)
                {
                    if (flag_above_noise[i])
                    {
                        above_noise_bins++;
                    }
                    else if (flag_above_noise[i - 1])
                    {
                        added_zeros += 2;
                    }
                }

                // compress the arrays to nonzero with greater than noiselevel;
                compressed_bins = 0;
                nonzero_bins = new double[above_noise_bins + added_zeros];
                nonzero_intensities = new double[above_noise_bins + added_zeros];
                for (i = 0; (i < (this.uimfReader.UimfGlobalParams.Bins / compression) - 1) && (compressed_bins < above_noise_bins + added_zeros); i++)
                {
                    if (flag_above_noise[i])
                    {
                        nonzero_bins[compressed_bins] = i;
                        nonzero_intensities[compressed_bins] = summed_spectrum[i];
                        compressed_bins++;
                    }
                    else if ((i > 0) && ((flag_above_noise[i - 1] || flag_above_noise[i + 1])))
                    {
                        nonzero_bins[compressed_bins] = i;
                        nonzero_intensities[compressed_bins] = 0;
                        compressed_bins++;
                    }
                }

                // pass arrays into calibration routine
                this.CalibrateFrame(summed_spectrum, nonzero_intensities, nonzero_bins,
                    this.uimfReader.UimfGlobalParams.BinWidth * (double)compression, this.uimfReader.UimfGlobalParams.Bins / compression,
                    this.uimfReader.UimfFrameParams.Scans, slope, intercept);

                CalibrantCountMatched = this.get_CalibrantCountMatched();
                CalibrantCountValid = this.get_CalibrantCountValid();
                AverageAbsoluteValueMassError = this.get_AverageAbsoluteValueMassError();
                AverageMassError = this.get_AverageMassError();

                if (CalibrantCountMatched == CalibrantCountValid)
                {
                    // done, slope and intercept acceptable
                    calibration_slope = this.get_Experimental_Slope();
                    calibration_intercept = this.get_Experimental_Intercept();
                    total_calibrants_matched = CalibrantCountMatched;
                    //break;
                }
                else if (CalibrantCountMatched > 4)
                    this.disable_CalibrantMaxPPMError();
            }

            this.uimfReader.ClearFrameParametersCache();
        }

        #endregion

        #region 4GHz to 1GHz compression

        /// <summary>
        /// Compress 4GHz Data to 1GHz
        /// </summary>
        private void Compress4GHzTo1GHzUIMF()
        {
            UIMFLibrary.GlobalParams gp = this.uimfReader.GetGlobalParams();
            UIMFLibrary.FrameParams fp;
            int i;
            int j;
            int k;
            int current_frame;
            int[] current_intensities = new int[gp.Bins / 4];

            double[] array_Bins = new double[0];
            int[] array_Intensity = new int[0];
            var list_nzVals = new List<Tuple<int, int>>();
            List<int> list_Scans = new List<int>();
            List<int> list_Count = new List<int>();

            Stopwatch stop_watch = new Stopwatch();

            // create new UIMF File
            string UIMF_filename = Path.Combine(this.CompressSaveDirectory, this.CompressSaveFilename + "_1GHz.UIMF");
            if (File.Exists(UIMF_filename))
            {
                if (MessageBox.Show("File Exists", "File Exists, Replace?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    File.Delete(UIMF_filename);
                else
                    return;
            }

            UIMFLibrary.DataWriter UIMF_Writer = new UIMFLibrary.DataWriter(UIMF_filename);
            UIMF_Writer.CreateTables(null);

            gp.AddUpdateValue(GlobalParamKeyType.BinWidth, 1);
            gp.AddUpdateValue(GlobalParamKeyType.Bins, gp.Bins / 4);
            UIMF_Writer.InsertGlobal(gp);

            int max_time = 0;

            var cancelToken = new CancellationTokenSource();
            var prog = new ProgressViewModel(gp.NumFrames, cancelToken);
            var progWindow = new ProgressWindow() { DataContext = prog };
            progWindow.ShowActivated = true;
            progWindow.Show();

            for (current_frame = 0; ((current_frame < (int)this.uimfReader.CurrentFrameType) && !cancelToken.IsCancellationRequested); current_frame++)
            {
                prog.SetProgress(current_frame, stop_watch.ElapsedMilliseconds);

                stop_watch.Reset();
                stop_watch.Start();

                fp = this.uimfReader.GetFrameParams(current_frame);
                UIMF_Writer.InsertFrame(current_frame, fp);

                for (i = 0; i < fp.Scans; i++)
                {
                    for (j = 0; j < gp.Bins; j++)
                    {
                        current_intensities[j] = 0;
                    }

                    this.uimfReader.GetSpectrum(this.uimfReader.ArrayFrameNum[current_frame], this.uimfReader.FrameTypeDict[this.uimfReader.ArrayFrameNum[current_frame]], i, out array_Bins, out array_Intensity);

                    for (j = 0; j < array_Bins.Length; j++)
                        current_intensities[(int)array_Bins[j] / 4] += array_Intensity[j];

                    list_nzVals.Clear();
                    for (j = 0; j < gp.Bins; j++)
                    {
                        if (current_intensities[j] > 0)
                        {
                            list_nzVals.Add(new Tuple<int, int>(j, current_intensities[j]));
                        }
                    }

                    UIMF_Writer.InsertScan(current_frame, fp, i, list_nzVals, 1, gp.GetValueInt32(GlobalParamKeyType.TimeOffset) / 4);
                }

                stop_watch.Stop();
                if (stop_watch.ElapsedMilliseconds > max_time)
                {
                    max_time = (int)stop_watch.ElapsedMilliseconds;
                    prog.AddStatus("Max Time: Frame " + current_frame.ToString() + " ..... " + max_time.ToString() + " msec", false);
                }
            }

            if (prog.Success)
            {
                progWindow.Dispatcher.Invoke(() => progWindow.Close());
            }

            UIMF_Writer.Dispose();
        }

        #endregion

    }
}
