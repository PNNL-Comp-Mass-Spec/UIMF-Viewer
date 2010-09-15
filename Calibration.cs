#if false
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace IonMobility
{
    public enum Species
    {
        PEPTIDE,
        CALIBRANT
    }
    enum PeakPicking
    {
        APEX,
        THREE_POINT_QUADRATIC
    }
    enum Instrument
    {
        AGILENT_TDC = 0,
        SCIEX = 1
    }
    enum CalibrationType
    {
        STANDARD,
        AGILENT,
        EXTERNAL
    }
    struct Peptide
    {
        public string name;
        public double mz;
        public int charge;

        public bool enabled;
    }
    struct Coefficients
    {
        public double intercept;
        public double slope;
    }
    class Calibration
    {
        /*******************************************************************************************
         * 
         * CODE BY MIKE BELOV TO CALIBRATE DATA
         * 
         */
        private Peptide[] Calibrants;
        private Peptide current_Peptide;
        private Peptide current_Calibrant;

        private const int NUM_CALIBRANTS = 19;
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
        private const double TOF_delay = 276000;
        private const double N_TIME_STEPS = 1022;
        private const double NOISE_INTERVAL = 30;
        private const double PeptideInterval = 20;

        private const double Sciex_slope = (double)0.35660427;
	    private const double Sciex_intercept = (double)-0.07565783;
        private const double Agilent_slope = (double) 0.57417985;
        private const double Agilent_intercept = (double)0.03456597;

        double Slope_internal, Intercept_internal;

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

        private double BinResolution;

        public Calibration(double bin_resolution)
        {
            BinResolution = bin_resolution;

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
        }

        // //////////////////////////////////////////////////////////////////////////////////////
        // routine for finding monoisotopic peak of a peptide
        //
        double OnFindingMonoisotopicPeak(double TOF_peptide, Species species_ID, int PeptideNumber, PeakPicking peak_picking, int tofs_per_frame)
        {
            int i;
            int j;

            double SNR_noise_level;

            int peak_number;
            double charge = 1;
            int peak_number_mono = 11;
            int NumNoiseBins;
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
            bool[,] TOF_MONOISOTOPE = new bool[10,10];
            long SIGNAL_THRESHOLD = 10;
            bool found_peptide_pos = false;

            int Pos_noise;
            int Pos_peptide;
            int SNR = 3;
            int NumNonZeroNoiseBins = 0;
            double noise_intensity_average = 0;
            double noise_intensity0 = 0;

            int bins_per_tof = 0;
            if (BinResolution <= 3)
                bins_per_tof = 80;
            else if (BinResolution <= 3.5)
                bins_per_tof = 60;  //3.3219 gives 1.0 ns
            else if (BinResolution >= 3.5)
                bins_per_tof = 40;

            double[] peptide_array_local = new double[bins_per_tof];

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

            //allocate memory for arrays with added zeros
            double[] arrival_time2 = new double[tofs_per_frame]; //drift time
            double[] arrival_time_TOF2 = new double[tofs_per_frame]; //arrival time in tof
            double[] mz2 = new double[tofs_per_frame];
            double[] sum_intensity2 = new double[tofs_per_frame];

            double[,] pVariable;
            double peptide_monoisotope_max = -1; // initial condition, no peaks found yet

            if (species_ID == Species.PEPTIDE)
            {
                charge = current_Peptide.charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                TOF_monoisotope_shift = (double)(1000.0 / (Slope * charge * 2.0 * Math.Sqrt(current_Peptide.mz))); //spacing between isotopes
            }
            else if (species_ID == Species.CALIBRANT)
            {
                charge = current_Peptide.charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                TOF_monoisotope_shift = (double)(1000.0 / (Slope * charge * 2.0 * Math.Sqrt(current_Calibrant.mz))); //spacing between isotopes
            }

            TOF_checkup = 2 * Math.Pow(2, BinResolution) / 10;

            isotope_ratio = 2.5;
            if (charge == 1)
            {
                isotope_ratio = 10.0;
                SNR = 2;
            }

            Pos_peptide = 0;
            for (i = 0; i < tofs_per_frame; i++)
            {
                if ((TOF_peptide - arrival_time_TOF2[i]) < PeptideInterval)
                {
                    Pos_peptide = i;
                    found_peptide_pos = true;
                    break;
                }
            }
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
                    TOF_MONOISOTOPE[i,j] = false;

            // estimate average noise intensity at the left wing of isotopic distribution
            i = 0;
            do
            {
                Pos_noise = Pos_peptide - i;
                ++i;
            } while (arrival_time_TOF2[Pos_peptide] - arrival_time_TOF2[Pos_noise] < NOISE_INTERVAL);

            NumNoiseBins = i - 1;
            NumNonZeroNoiseBins = 0;
            noise_intensity_average = 0;

            if (arrival_time_TOF2[Pos_peptide] - arrival_time_TOF2[Pos_noise] > NOISE_INTERVAL)
            {
                Pos_noise = Pos_noise + 1;
                NumNoiseBins = i - 2;
            }

            for (i = Pos_noise; i < Pos_noise + NumNoiseBins; ++i)
            {
                if (sum_intensity2[i] > 0 && NumNonZeroNoiseBins == 0)
                {
                    NumNonZeroNoiseBins++;
                    noise_intensity_average = noise_intensity_average + sum_intensity2[i];
                    noise_intensity0 = noise_intensity_average;
                }

                if (sum_intensity2[i] > 0 && NumNonZeroNoiseBins > 0) /*&& fabs(sum_intensity2[i]/noise_intensity0) < 10*/
                {
                    NumNonZeroNoiseBins++;
                    noise_intensity_average = noise_intensity_average + sum_intensity2[i];
                }
            }

            // finding average noise for all NumNoiseBins without taking into account the outlyars
            if (NumNonZeroNoiseBins > 0) 
                noise_intensity_average = noise_intensity_average / NumNonZeroNoiseBins;

            for (i = Pos_peptide; i < Pos_peptide + bins_per_tof; ++i)
            {
                // now identify isotopic peaks
                if ((i > 4) && (peak_number < 10))
                {
                    SNR_noise_level = noise_intensity_average * SNR;

                    if (((sum_intensity2[i] > SIGNAL_THRESHOLD) && (sum_intensity2[i] > SNR_noise_level)) ||
                        ((sum_intensity2[i - 4] > SIGNAL_THRESHOLD) && (sum_intensity2[i - 4] > SNR_noise_level)) ||
                        ((sum_intensity2[i - 3] > SIGNAL_THRESHOLD) && (sum_intensity2[i - 3] > SNR_noise_level)) ||
                        ((sum_intensity2[i - 2] > SIGNAL_THRESHOLD) && (sum_intensity2[i - 2] > SNR_noise_level)) ||
                        ((sum_intensity2[i - 1] > SIGNAL_THRESHOLD) && (sum_intensity2[i - 1] > SNR_noise_level)))
                    {
                        if ((sum_intensity2[i - 2] > sum_intensity2[i - 1]) &&
                            (sum_intensity2[i - 2] > sum_intensity2[i]) &&
                            (sum_intensity2[i - 3] < sum_intensity2[i - 2]) &&
                            (sum_intensity2[i - 4] < sum_intensity2[i - 2]))
                        {
                            peptide_local_max[peak_number] = sum_intensity2[i - 2];
                            TOF_offset_local[peak_number] = arrival_time_TOF2[i - 2]; // define file pointer position corresponding to the local max signal
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
                    for (j = 0; j < peak_number; ++j)
                    {
                        if (i != j)
                        {
                            if (Math.Abs(Math.Abs(TOF_offset_local[i] - TOF_offset_local[j]) - TOF_monoisotope_shift) < TOF_checkup)
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
                                                        TOF_MONOISOTOPE[i,j] = false;
                                                        PEAK_COUNTER = true; //accounts for the case of several peaks between putative isotopic peaks
                                                    }
                                                    else
                                                    {
                                                        peak_number_mono = i;
                                                        TOF_MONOISOTOPE[i,j] = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TOF_MONOISOTOPE[i,j] = true;
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
                                                        TOF_MONOISOTOPE[i,j] = false;
                                                        PEAK_COUNTER = true;
                                                    }
                                                    else
                                                    {
                                                        peak_number_mono = j;
                                                        TOF_MONOISOTOPE[i,j] = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TOF_MONOISOTOPE[i,j] = true;
                                                peak_number_mono = j;
                                            }
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
                    if (TOF_MONOISOTOPE[i,j])
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

                        sum_yx2 = sum_yx2 + sum_intensity2[pos_peptide_max] * i * i;
                        sum_yx = sum_yx + sum_intensity2[pos_peptide_max] * i;
                        sum_y = sum_y + sum_intensity2[pos_peptide_max];
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

                    TOF_monoisotope = TOF_monoisotope + arrival_time_TOF2[pos_peptide_max - 2];

                    if (TOF_monoisotope > 110000)
                        TOF_monoisotope = 0; //set TOF to zero if outside of m/z range

                    peptide_monoisotope_max = peptide_local_max[peak_number_mono];
                }
            }
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

        double Slope;
        double Intercept;

        void OnInternalCalibration(CalibrationType cal_type, Instrument inst_type, int PeptideNumber, int tofs_per_frame)
        {
            int NumNonZeroCalibrants = 0;
            int NumNonZeroCalibrantsCorrected = 0;
            bool USE_EXTERNAL_CALIBRATION = false;
            int i;
            int j;

            Calibrants = new Peptide[NUM_CALIBRANTS];
            // double[] Peptides.mz = new double[NUM_CALIBRANTS];

            double[] pTOF_calibrant = new double[NUM_CALIBRANTS];
            double[] pmz_calibrantmz_new = new double[NUM_CALIBRANTS];
            double[] pmz_calibrantmz_exp = new double[NUM_CALIBRANTS];
            double[] pTOF_calibrant_external = new double[NUM_CALIBRANTS];
            double[] pTOF_calibrant_external_non_zero = new double[NUM_CALIBRANTS];
            double[] pmz_calibrantmz_non_zero = new double[NUM_CALIBRANTS];

            if (inst_type == Instrument.AGILENT_TDC)
            {
                Slope = Agilent_slope;
                Intercept = Agilent_intercept;
            }
            if (inst_type == Instrument.SCIEX)
            {
                Slope = Sciex_slope;
                Intercept = Sciex_intercept;
            }

            double[] error_ppm_calibrant = new double[NUM_CALIBRANTS];

            double sum_mz_term = 0;
            double sum_TOF_term = 0;
            double sum_TOF_term_squared = 0;
            double sum_mz_TOF_term = 0;

            for (i = 0; i < NUM_CALIBRANTS; ++i)
            {
                pTOF_calibrant[i] = 0;
                pTOF_calibrant_external[i] = 0;
                pTOF_calibrant_external_non_zero[i] = 0;
                pmz_calibrantmz_non_zero[i] = 0;
                pmz_calibrantmz_new[i] = 0;
                Calibrants[i].mz = 0;
            }

            //Initialize calibrants using names

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

            j = 0;
            for (i = 0; i < NUM_CALIBRANTS; ++i)
            {
                if (Calibrants[i].mz > 0)
                {
                    pTOF_calibrant_external[i] = (double)((Math.Sqrt(Calibrants[i].mz) / Slope) + Intercept);
                    pTOF_calibrant_external_non_zero[j] = pTOF_calibrant_external[i];
                    pmz_calibrantmz_non_zero[j] = Calibrants[i].mz;
                    Calibrants[j].name = Calibrants[i].name;
                    Calibrants[j].charge = Calibrants[i].charge;

                    NumNonZeroCalibrants++;
                    j++;
                }
                else
                {
                    pTOF_calibrant_external[i] = 0;
                }
            }

            // now go to TOF spectra and find peak maxima
            for (j = 0; j < NumNonZeroCalibrants; ++j)
            {
                current_Peptide.name = Calibrants[j].name;
                current_Peptide.charge = Calibrants[j].charge;
                current_Calibrant.mz = pmz_calibrantmz_non_zero[j];

                pTOF_calibrant[j] = OnFindingMonoisotopicPeak(pTOF_calibrant_external_non_zero[j] * 1000, Species.PEPTIDE, PeptideNumber, PeakPicking.THREE_POINT_QUADRATIC, tofs_per_frame);
                if (pTOF_calibrant[j] - pTOF_calibrant_external_non_zero[j] > 0.05)
                    pTOF_calibrant[j] = 0; //false identification is offset is more than 0.05 us
                Calibrants[j].mz = pmz_calibrantmz_non_zero[j];
                if (j == 0)
                    NumNonZeroCalibrantsCorrected = NumNonZeroCalibrants;
                if (pTOF_calibrant[j] == 0)
                {
                    NumNonZeroCalibrantsCorrected--;
                    Calibrants[j].mz = 0; //if calibrant wasn't found, set its m/z to zero

                    if ((NumNonZeroCalibrantsCorrected < 2) && ((cal_type == CalibrationType.STANDARD) || (cal_type == CalibrationType.AGILENT)))
                        USE_EXTERNAL_CALIBRATION = true;
                }
            }

            //least-square fit
            if (!USE_EXTERNAL_CALIBRATION)
            {
                if (NumNonZeroCalibrants > 1 && (cal_type == CalibrationType.STANDARD))
                {
                    for (i = 0; i < NumNonZeroCalibrants; i++) //take all the requested calibrants but use only non-zero entries
                    {
                       // internal_calibrants_found = true;
                        if (Calibrants[i].mz > 0)
                        {
                            pmz_calibrantmz_new[i] = Math.Sqrt(Calibrants[i].mz);

                            sum_mz_term = sum_mz_term + pmz_calibrantmz_new[i];
                            sum_TOF_term = sum_TOF_term + pTOF_calibrant[i];
                            sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(pTOF_calibrant[i], 2);
                            sum_mz_TOF_term = sum_mz_TOF_term + pmz_calibrantmz_new[i] * pTOF_calibrant[i];
                        }
                    }

                    Slope_internal = ((NumNonZeroCalibrantsCorrected * sum_mz_TOF_term) - (sum_mz_term * sum_TOF_term)) / (NumNonZeroCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                    Intercept_internal = (sum_mz_term / NumNonZeroCalibrantsCorrected) - ((Slope_internal * sum_TOF_term) / NumNonZeroCalibrantsCorrected);
                }

                if (NumNonZeroCalibrants > 1 && (cal_type == CalibrationType.AGILENT))
                {
                    for (i = 0; i < NumNonZeroCalibrants; i++) //take all the requested calibrants but use only non-zero entries
                    {
                       // internal_calibrants_found = true;
                        if (Calibrants[i].mz > 0)
                        {
                            pmz_calibrantmz_new[i] = Math.Sqrt(Calibrants[i].mz);

                            sum_mz_term = sum_mz_term + pmz_calibrantmz_new[i];
                            sum_TOF_term = sum_TOF_term + pTOF_calibrant[i];
                            sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(pTOF_calibrant[i], 2);
                            sum_mz_TOF_term = sum_mz_TOF_term + pmz_calibrantmz_new[i] * (pTOF_calibrant[i]);
                        }
                    }

                    Slope_internal = (NumNonZeroCalibrantsCorrected * sum_mz_TOF_term - sum_mz_term * sum_TOF_term) / (NumNonZeroCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                    Intercept_internal = (sum_mz_term / NumNonZeroCalibrantsCorrected) - ((Slope_internal * sum_TOF_term) / NumNonZeroCalibrantsCorrected);
                    Intercept_internal = -Intercept_internal / Slope_internal;
                }
            }
            else
            {
                if ((cal_type == CalibrationType.STANDARD) || (cal_type == CalibrationType.AGILENT))
                {
                    if (inst_type == Instrument.AGILENT_TDC)
                    {
                        Slope_internal = (double)Agilent_slope;
                        Intercept_internal = (double)Agilent_intercept;
                    }
                    if (inst_type == Instrument.SCIEX)
                    {
                        Slope_internal = (double)Sciex_slope;
                        Intercept_internal = (double)Sciex_intercept;
                    }
                }

                USE_EXTERNAL_CALIBRATION = false;
            }

            if (Slope_internal == 0 || Intercept_internal == 0)
            {
                if (inst_type == Instrument.AGILENT_TDC)
                {
                    Slope_internal = (double)Agilent_slope;
                    Intercept_internal = (double)Agilent_intercept;
                }
                if (inst_type == Instrument.SCIEX)
                {
                    Slope_internal = (double)Sciex_slope;
                    Intercept_internal = (double)Sciex_intercept;
                }
            }

            for (j = 0; j < NumNonZeroCalibrants; ++j)
            {
                if (Calibrants[j].enabled)
                {
                    if (cal_type == CalibrationType.AGILENT)
                    {
                        pmz_calibrantmz_exp[j] = Math.Pow((pTOF_calibrant[j] - Intercept_internal) * Slope_internal, 2);
                        error_ppm_calibrant[j] = ((pmz_calibrantmz_exp[j] - Calibrants[j].mz) / Calibrants[j].mz) * Math.Pow(10, 6);
                    }
                    if (cal_type == CalibrationType.STANDARD)
                    {
                        pmz_calibrantmz_exp[j] = Math.Pow((pTOF_calibrant[j] * Slope_internal) + Intercept_internal, 2);
                        error_ppm_calibrant[j] = ((pmz_calibrantmz_exp[j] - Calibrants[j].mz) / Calibrants[j].mz) * Math.Pow(10, 6);
                    }
                }
            }

            return;
        }
    }
}
#endif