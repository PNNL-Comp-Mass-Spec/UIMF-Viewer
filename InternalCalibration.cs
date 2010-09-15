using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace IonMobility
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
    public struct Peptide
    {
        public bool enabled;
     
        public string name;
        public double mz;
        public int charge;
        public double tof;
        public double bins;

        public double new_mz;
        public double new_tof;
        public double error_ppm;
    }
    public struct Coefficients
    {
        public double intercept;
        public double slope;
    }

    /*******************************************************************************************
    * 
    * CODE BY MIKE BELOV TO CALIBRATE DATA
    * 
    */
    public partial class InternalCalibration : System.Windows.Forms.UserControl
    {
        private Peptide[] Calibrants;
    //    private Peptide current_Peptide;
        private Peptide current_Calibrant;

        private const int NUM_CALIBRANTS = 20;
        private const int N_PRS_BINS = 511;

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
        private double Slope_theor;         // TOF calibration constant (slope): sqrt(mass/charge) = slope*t, where t is in s
        private double n_density;           // ions number density in the IMS drift tube, m^-3

        private long N_TOF_BINS_PER_MODULATION_BIN;
        private long N_TOF_BINS_PER_TOF_SCAN;
        private long N_TOF_BINS_PER_IMS_BIN;
        private long N_SCANS_PER_MODULATION_BIN;

     //   private double BinResolution;

        public double Slope;
        public double Intercept;

        private int spectra_with_nonzeroentries = 0;

        public InternalCalibration() //int bin_resolution)
        {
          //  BinResolution = bin_resolution;
            
#if COEFFICIENTS
            coef_Sciex.slope = (double)0.35660427;
            coef_Sciex.intercept = (double)-0.07565783;
            coef_Agilent.slope = (double)0.57417985;
            coef_Agilent.intercept = (double)0.03456597;
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
            Slope_theor = (double)0.42 * (double)Math.Pow(10, 6);   // TOF calibration constant (slope): sqrt(mass/charge) = slope*t, where t is in s
            n_density = (double)(n_Lodschmidt * pressure_IMS / pressure_atm); // ions number density in the IMS drift tube, m^-3

            N_TOF_BINS_PER_MODULATION_BIN = (long)(t_modulation_IMS / TOF_step);
            N_TOF_BINS_PER_TOF_SCAN = (long)(t_TOF / TOF_step);
            N_TOF_BINS_PER_IMS_BIN = (long)(N_TIME_STEPS * N_TOF_BINS_PER_TOF_SCAN / N_PRS_BINS);
            N_SCANS_PER_MODULATION_BIN = (long)(t_modulation_IMS / t_TOF);
             
            InitializeComponent();
        }

        public double get_intercept()
        {
            return this.Intercept; //  this.coef_Internal.intercept;
        }
        public double get_slope()
        {
            return this.Slope; // coef_Internal.slope;
        }

        public void CreateArrays(double[] sum_intensities, double[] bin_arrival_time, double bin_width, int total_bins, double mz_slope, double mz_intercept)
        {
            int bins_per_frame = total_bins;
            this.spectra_with_nonzeroentries = sum_intensities.Length;

            this.Slope = mz_slope;
            this.Intercept = mz_intercept;

            this.sum_intensity2 = new double[spectra_with_nonzeroentries];
            this.arrival_time_TOF2 = new double[spectra_with_nonzeroentries]; //arrival time in bins
            
            Array.Copy(sum_intensities, sum_intensity2, sum_intensities.Length);
            Array.Copy(bin_arrival_time, arrival_time_TOF2, bin_arrival_time.Length);

            this.mz2 = new double[spectra_with_nonzeroentries];
            for (int i = 0; i < spectra_with_nonzeroentries; i++)
            {
               // this.arrival_time_TOF2[i] *= bin_width;
                mz2[i] = Math.Pow((this.arrival_time_TOF2[i] / 1000.0) - this.Intercept, 2) * this.Slope * this.Slope;
            }
            // mz_LIST2[i][k] = (float)pow((double)(arrival_time_LIST2[i][k] - *(TOF_offset_buffer + i) + TimeOffset) / 1000 - Intercept, 2) * (float)pow((double)Slope, 2);

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

        // //////////////////////////////////////////////////////////////////////////////////////
        // routine for finding monoisotopic peak of a peptide
        //
        private double OnFindingMonoisotopicPeak(double TOF_peptide, Species species_ID, PeakPicking peak_picking, int spectra_with_nonzeroentries)
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
                TOF_monoisotope_shift = (double)(1000.0 / (this.Slope * charge * 2.0 * Math.Sqrt(current_Peptide.mz))); //spacing between isotopes
            }
            else if (species_ID == Species.CALIBRANT)
#endif
            {
                charge = current_Calibrant.charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                TOF_monoisotope_shift = (double)(1000.0 / (this.Slope * charge * 2.0 * Math.Sqrt(current_Calibrant.mz))); //spacing between isotopes
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

            //MessageBox.Show(this, "NumNonZeroNoiseBins "+NumNonZeroNoiseBins.ToString()+", noise_intensity_average: "+noise_intensity_average.ToString());

            //MessageBox.Show(this, Pos_peptide.ToString() + "  " + bins_per_tof.ToString());
            for (i = Pos_peptide; (i < Pos_peptide + 80) && (i<sum_intensity2.Length); ++i)
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
           
            // MessageBox.Show(this, "peak_number<3: " + peak_number.ToString()+"  charge!=1: "+charge.ToString());

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
                //MessageBox.Show(this, "Peak Number: "+peak_number.ToString());
                for (i = 0; i < peak_number; ++i)
                {
                    for (j = i + 1; j < peak_number; ++j)
                    {
                        // MessageBox.Show(this, "here: (TOF[i] " + TOF_offset_local[i].ToString("0.0") + "- TOF[j] " + TOF_offset_local[j].ToString("0.0") + ") - shift " + TOF_monoisotope_shift.ToString("0.0") + "(" + Math.Abs(Math.Abs(TOF_offset_local[i] - TOF_offset_local[j]) - TOF_monoisotope_shift).ToString() + " < " + TOF_checkup.ToString("0.0"));
                        if (Math.Abs(Math.Abs(TOF_offset_local[i] - TOF_offset_local[j]) - TOF_monoisotope_shift) < TOF_checkup * 10.0)
                        {
                            //MessageBox.Show(this, "inside: (TOF[i] " + TOF_offset_local[i].ToString("0.0") + "- TOF[j] " + TOF_offset_local[j].ToString("0.0") + ") - shif" + TOF_monoisotope_shift.ToString("0.0")+ " < "+TOF_checkup.ToString("0.0"));
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

           // MessageBox.Show(this, "TOF_MONOISOTOPE_FOUND: " + TOF_MONOISOTOPE_FOUND.ToString());

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

                   // MessageBox.Show(TOF_monoisotope.ToString());

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
        public void InitializeCalibrants(double bin_width, double mz_slope, double mz_intercept)
        {
            Calibrants = new Peptide[NUM_CALIBRANTS];

            this.bin_Width = bin_width;

            this.Slope = mz_slope;
            this.Intercept = mz_intercept;

            //Angiotensin_I 3+ 
            Calibrants[0].mz = 432.89975;
            Calibrants[0].name = "Angiotensin_I";
            Calibrants[0].charge = 3;

            //Angiotensin 2+
            Calibrants[1].mz = 648.845996;
            Calibrants[1].name = "Angiotensin_I";
            Calibrants[1].charge = 2;

            //Bradykinin +3 
            Calibrants[2].mz = 354.1943928;
            Calibrants[2].name = "Bradykinin";
            Calibrants[2].charge = 3;

            // Bradykinin +2
            Calibrants[3].mz = 530.78795;
            Calibrants[3].name = "Bradykinin";
            Calibrants[3].charge = 2;

            // Neurotensin +2
            Calibrants[4].mz = 836.962074;
            Calibrants[4].name = "Neurotensin";
            Calibrants[4].charge = 2;

            //Neurotensin +3
            Calibrants[5].mz = 558.310475;
            Calibrants[5].name = "Neurotensin";
            Calibrants[5].charge = 3;

            //Fibrinopeptide +2
            Calibrants[6].mz = 768.8498483;
            Calibrants[6].name = "Fibrinopeptide_A";
            Calibrants[6].charge = 2;

            //Renin +2
            Calibrants[7].mz = 513.281968;
            Calibrants[7].name = "Renin";
            Calibrants[7].charge = 2;

            // "Renin +1 
            Calibrants[8].mz = 1025.556667;
            Calibrants[8].name = "Renin";
            Calibrants[8].charge = 1;

            Calibrants[9].mz = 674.37132;
            Calibrants[9].name = "Substance_P";
            Calibrants[9].charge = 2;

            Calibrants[10].mz = 820.472489;
            Calibrants[10].name = "KVPQVSTPTLVEVSR";
            Calibrants[10].charge = 2;

            //bsa
            Calibrants[11].mz = 547.317418;
            Calibrants[11].name = "KVPQVSTPTLVEVSR";
            Calibrants[11].charge = 3;

            //bsa
            Calibrants[12].mz = 571.860788;
            Calibrants[12].name = "KQTALVELLK";
            Calibrants[12].charge = 2;

            //bsa
            Calibrants[13].mz = 653.361684;
            Calibrants[13].name = "HLVDEPQNLIK";
            Calibrants[13].charge = 2;

            //Fibrinopeptide +3
            Calibrants[14].mz = 512.90229;
            Calibrants[14].name = "Fibrinopeptide_A";
            Calibrants[14].charge = 3;

            // bsa
            Calibrants[15].mz = 480.6087469;
            Calibrants[15].name = "RHPEYAVSVLLR";
            Calibrants[15].charge = 3;

            // bsa
            Calibrants[16].mz = 417.211886;
            Calibrants[16].name = "FKDLGEEHFK";
            Calibrants[16].charge = 3;

            //bsa
            Calibrants[17].mz = 363.007718;
            Calibrants[17].name = "LCVLHEKTPVSEKVTK";
            Calibrants[17].charge = 5;

            //bsa  
            Calibrants[18].mz = 454.895578;
            Calibrants[18].name = "SLHTLFGDELCK";
            Calibrants[18].charge = 3;

            //bsa  
            Calibrants[19].mz = 693.813909;
            Calibrants[19].name = "YICDNQDTISSK";
            Calibrants[19].charge = 2;

            while (this.dg_Calibrants.Rows.Count > 1)
                this.dg_Calibrants.Rows.RemoveAt(0);
            for (int row = 0; row < Calibrants.Length; row++)
            {
                try
                {
                    this.dg_Calibrants.Rows.Add();

                    this.dg_Calibrants.Rows[row].Cells[1].Value = Calibrants[row].name;
                    this.dg_Calibrants.Rows[row].Cells[2].Value = Calibrants[row].mz; // num frames
                    this.dg_Calibrants.Rows[row].Cells[3].Value = Calibrants[row].charge;

                    this.dg_Calibrants.Rows[row].Cells[4].Value = (double)((Math.Sqrt(Calibrants[row].mz) / this.Slope) + this.Intercept);
                    this.dg_Calibrants.Rows[row].Cells[5].Value = ((double) this.dg_Calibrants.Rows[row].Cells[4].Value) * 1000.0 / bin_width;

                   // MessageBox.Show(this.Slope.ToString() + " " + this.Intercept.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("set_ExperimentList:  " + ex.ToString());
                }
            }
        }

        /*
        public void set_ExperimentList(Experiment_Settings[] list_experiments)
        {
            int row;
            int i;

            if (list_experiments == null)
                return;

            this.clear_Experiments();

        }

        public Experiment_Settings[] get_ExperimentList()
        {
            int bad_row_count = 0;
            bool[] flag_bad_row = new bool[this.dg_ExperimentList.RowCount - 1];
            int bad_rows_skipped = 0;

            // check for incomplete rows - remove them!
            for (int i = 0; i < this.dg_ExperimentList.RowCount - 1; i++)
            {
                try
                {
                    if ((((string)this.dg_ExperimentList.Rows[i].Cells[0].Value).Trim().Length < 1) ||
                        (Convert.ToInt32(this.dg_ExperimentList.Rows[i].Cells[1].Value) <= 0) ||
                        (((string)this.dg_ExperimentList.Rows[i].Cells[2].Value).Trim().Length < 1))
                    {
                        MessageBox.Show("Experiment AutoList Row " + (i + 1).ToString() + " incomplete!  Skipping it.");
                        flag_bad_row[i] = true;
                        bad_row_count++;


                        this.dg_ExperimentList.Rows[i].Cells[0].Style.BackColor = Color.Yellow;
                        this.dg_ExperimentList.Rows[i].Cells[1].Style.BackColor = Color.Yellow;
                        this.dg_ExperimentList.Rows[i].Cells[2].Style.BackColor = Color.Yellow;
                    }
                    else
                    {
                        flag_bad_row[i] = false;

                        this.dg_ExperimentList.Rows[i].Cells[0].Style.BackColor = Color.White;
                        this.dg_ExperimentList.Rows[i].Cells[1].Style.BackColor = Color.White;
                        this.dg_ExperimentList.Rows[i].Cells[2].Style.BackColor = Color.White;
                    }
                }
                catch (Exception ex)
                {
                    if (((string)this.dg_ExperimentList.Rows[i].Cells[0].Value) != null)
                        MessageBox.Show("Experiment AutoList Row " + (i + 1).ToString() + " error!  Skipping it.\n" + ((string)this.dg_ExperimentList.Rows[i].Cells[0].Value).Trim());
                    flag_bad_row[i] = true;
                    bad_row_count++;
                }
            }

            //MessageBox.Show(bad_row_count.ToString() + "  " + this.dg_ExperimentList.RowCount.ToString());

            // ok create a savable, usable list and send it off
            Experiment_Settings[] list_experiments = new Experiment_Settings[this.dg_ExperimentList.RowCount - bad_row_count - 1];
            for (int i = 0; i < list_experiments.Length + bad_row_count; i++)
            {
                if (!flag_bad_row[i])
                {
                    try
                    {
                        list_experiments[i - bad_rows_skipped].dir_Experiment = (string)this.dg_ExperimentList.Rows[i].Cells[0].Value;
                        list_experiments[i - bad_rows_skipped].Frames = Convert.ToInt32(this.dg_ExperimentList.Rows[i].Cells[1].Value);
                        list_experiments[i - bad_rows_skipped].Method = (string)this.dg_ExperimentList.Rows[i].Cells[2].Value;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("failed " + i.ToString() + ": " + ex.ToString());
                    }
                }
                else
                {
                    bad_rows_skipped++;
                }
            }
            return list_experiments;
        }
        */

        // this is all dealing with calibrants
        public void OnInternalCalibration(CalibrationType cal_type, Instrument inst_type, int tofs_per_frame)
        {
            int NumEnabledCalibrants = 0;
            int NumEnabledCalibrantsCorrected = 0;
            int i;
            int j;

            double sum_mz_term = 0;
            double sum_TOF_term = 0;
            double sum_TOF_term_squared = 0;
            double sum_mz_TOF_term = 0;

            j = 0;
            for (i = 0; i < this.dg_Calibrants.Rows.Count - 1; ++i)
            {
                Calibrants[i].bins = 0;
                Calibrants[i].new_tof = 0;
                Calibrants[i].new_mz = 0;
                Calibrants[i].error_ppm = 0;
                
                if ((this.dg_Calibrants.Rows[i].Cells[0].Value != null) &&
                    (Convert.ToBoolean(this.dg_Calibrants.Rows[i].Cells[0].Value)))
                {
                    Calibrants[i].enabled = true;

                    Calibrants[i].tof = (double)((Math.Sqrt(Calibrants[i].mz) / this.Slope) + this.Intercept);
                    Calibrants[i].bins = Calibrants[i].tof / this.bin_Width;

                    //MessageBox.Show(this, "Slope: "+this.Slope.ToString()+", Intercept: "+this.Intercept.ToString()+", pTOF_calibrant_external_non_zero: " + pTOF_calibrant_external_non_zero[i].ToString());

                    NumEnabledCalibrants++;
                    j++;
                }
                else
                {
                    Calibrants[i].enabled = false;
                }
            }

            // now go to TOF spectra and find peak maxima
            NumEnabledCalibrantsCorrected = NumEnabledCalibrants;
            for (i = 0; i < this.dg_Calibrants.Rows.Count - 1; i++)
            {
                if (Calibrants[i].enabled) //Calibrants[i].mz > 0)
                {
                    current_Calibrant.name = Calibrants[i].name;
                    current_Calibrant.charge = Calibrants[i].charge;
                    current_Calibrant.mz = Calibrants[i].mz;

                    //MessageBox.Show(this, "PCENZ: " + (pTOF_calibrant_external_non_zero[j] * 1000).ToString() + ", tofs_per_frame: " + tofs_per_frame.ToString());

                   // MessageBox.Show(this, this.dg_Calibrants.Rows[i].Cells[1].Value + "   " + ((double)this.dg_Calibrants.Rows[i].Cells[2].Value).ToString());

                    // pTOF_calibrant[j] = OnFindingMonoisotopicPeak(pTOF_calibrant_external_non_zero[j] * 1000,  // wfd
                    Calibrants[i].new_tof = OnFindingMonoisotopicPeak((double)this.dg_Calibrants.Rows[i].Cells[5].Value,  // wfd
                        Species.CALIBRANT, PeakPicking.THREE_POINT_QUADRATIC, tofs_per_frame);

                  //  if (pTOF_calibrant[i] - pTOF_calibrant_external_non_zero[i] > 0.05)
                  //      pTOF_calibrant[i] = 0; //false identification is offset is more than 0.05 us

                    if (Calibrants[i].new_tof == 0)
                    {
                        NumEnabledCalibrantsCorrected--;
                        Calibrants[i].enabled = false;
                        // Calibrants[i].mz = 0; //if calibrant wasn't found, set its m/z to zero
                    }
                }
            }

            //MessageBox.Show(this, "NumEnabledCalibrants: "+NumEnabledCalibrants.ToString());
            cal_type = CalibrationType.AGILENT;

#if false
            //least-square fit
            if (NumEnabledCalibrants > 1 && (cal_type == CalibrationType.STANDARD))
            {
                for (i = 0; i < NumEnabledCalibrants; i++) //take all the requested calibrants but use only non-zero entries
                {
                    // internal_calibrants_found = true;
                    if (Calibrants[i].mz > 0)
                    {
                        pmz_calibrant_new[i] = Math.Sqrt(Calibrants[i].mz);

                        sum_mz_term = sum_mz_term + pmz_calibrant_new[i];
                        sum_TOF_term = sum_TOF_term + pTOF_calibrant[i];
                        sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(pTOF_calibrant[i], 2);
                        sum_mz_TOF_term = sum_mz_TOF_term + pmz_calibrant_new[i] * pTOF_calibrant[i];
                    }
                }

                coef_Internal.slope = ((NumEnabledCalibrantsCorrected * sum_mz_TOF_term) - (sum_mz_term * sum_TOF_term)) / (NumEnabledCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                coef_Internal.intercept = (sum_mz_term / NumEnabledCalibrantsCorrected) - ((coef_Internal.slope * sum_TOF_term) / NumEnabledCalibrantsCorrected);
            }
#endif

            if (NumEnabledCalibrants > 1 && (cal_type == CalibrationType.AGILENT))
            {
                for (i = 0; i < this.dg_Calibrants.Rows.Count - 1; i++)
                {
                    if (Calibrants[i].enabled) //Calibrants[i].mz > 0)
                    {
                        // internal_calibrants_found = true;
                        if (Calibrants[i].mz > 0)
                        {
                            //MessageBox.Show("huh");
                            Calibrants[i].new_mz = Math.Sqrt(Calibrants[i].mz);

                            sum_mz_term = sum_mz_term + Calibrants[i].new_mz;
                            sum_TOF_term = sum_TOF_term + Calibrants[i].new_tof;
                            sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(Calibrants[i].new_tof, 2);
                            sum_mz_TOF_term = sum_mz_TOF_term + Calibrants[i].new_mz * (Calibrants[i].new_tof);
                        }

                        coef_Internal.slope = (NumEnabledCalibrantsCorrected * sum_mz_TOF_term - sum_mz_term * sum_TOF_term) / (NumEnabledCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                        coef_Internal.intercept = (sum_mz_term / NumEnabledCalibrantsCorrected) - ((coef_Internal.slope * sum_TOF_term) / NumEnabledCalibrantsCorrected);
                        coef_Internal.intercept = -coef_Internal.intercept / coef_Internal.slope;
                    }
                }
            }

            this.Slope = coef_Internal.slope;
            this.Intercept = coef_Internal.intercept;

            this.tb_Slope.Text = this.Slope.ToString();
            this.tb_Intercept.Text = this.Intercept.ToString();


            for (j = 0; j < this.dg_Calibrants.Rows.Count - 1; ++j)
            {
                if (this.Calibrants[j].enabled)
                {
                    Calibrants[j].new_mz = Math.Pow((Calibrants[j].new_tof - coef_Internal.intercept) * coef_Internal.slope, 2);
                    Calibrants[j].error_ppm = ((Calibrants[j].new_mz - Calibrants[j].mz) / Calibrants[j].mz) * Math.Pow(10, 6);

                    if ((Calibrants[j].new_tof > 0) && (Math.Abs(Calibrants[j].error_ppm) < 50000))
                    {
                        this.dg_Calibrants.Rows[j].Cells[6].Value = Calibrants[j].error_ppm;
                        this.dg_Calibrants.Rows[j].Cells[7].Value = Calibrants[j].new_mz;
                        this.dg_Calibrants.Rows[j].Cells[8].Value = Calibrants[j].new_tof;
                    }
                    else
                    {
                        this.dg_Calibrants.Rows[j].Cells[6].Value = "failed";
                        this.dg_Calibrants.Rows[j].Cells[7].Value = "";
                        this.dg_Calibrants.Rows[j].Cells[8].Value = "";
                    }
                }
                else
                {
                    this.dg_Calibrants.Rows[j].Cells[6].Value = "";
                    this.dg_Calibrants.Rows[j].Cells[7].Value = "";
                    this.dg_Calibrants.Rows[j].Cells[8].Value = "";
                }
            }

            return;
        }
    }
}
