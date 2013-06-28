using System;
using System.Collections.Generic;
using System.Text;

namespace Belov_Transform
{
    class Calibration
    {

        public int[] iCalibrantArrivalTime;
        public int[] iCalibrantSummedIntensity;
        public double[] AdError_PPM;
        public string[] AsCalibrantName;
        public int[] AiCalibrantCharge;
        public double[] AdCalibrantMZ;

        public double dBinResolution, dTimeScale, dTimeOffset;
        public double dSlope, dIntercept;
        public double dSlope_internal, dIntercept_internal;
        public int iNumberOfSpectraForCalibration, iNumberOfElementsForCalibration;
        public int iNumberOfNonZeroCalibrants, iNumberOfAllCalibrants;

        bool status;
        double dCoefficientA, dCoefficientB, dCoefficientC;
        int[] CalibrantSummedIntensity;

        //////////////////////////////////////////////////////////////////////
        // Construction/Destruction
        //////////////////////////////////////////////////////////////////////

        public Calibration()
        {
            AdError_PPM = null;
            AsCalibrantName = null;
            AiCalibrantCharge = null;
            AdCalibrantMZ = null;
            iCalibrantArrivalTime = null;
            iCalibrantSummedIntensity = null;
            CalibrantSummedIntensity = null;
        }

        //----------------------------------CALIBRATION ROUTINES----------------------------------------------------------------------------------------------//

        public bool OnGenerateArraysForCalibration(int[][] iArrivalTime, int[][] iIntensity)
        {

            //sum spectra intensity
            int i, j;
            status = false;

            CalibrantSummedIntensity = new int[iNumberOfElementsForCalibration];

            for (j = 0; j < iNumberOfElementsForCalibration; ++j)
            {
                CalibrantSummedIntensity[j] = 0;
                for (i = 0; i < iNumberOfSpectraForCalibration; ++i)
                {
                    if (iIntensity[i][j] > 0)
                    {
                        CalibrantSummedIntensity[j] = CalibrantSummedIntensity[j] + iIntensity[i][j];
                        status = true;
                    }

                }
            }

            OnAddZerosToSummedArray(iArrivalTime[0], CalibrantSummedIntensity);

            return status;
        }

        void OnAddZerosToSummedArray(int[] iSummedArrivalTime, int[] iSummedIntensity)
        {

            int j, k, added_zeros;

            int[] zero_add_point_left = new int[iNumberOfElementsForCalibration];
            int[] zero_add_point_right = new int[iNumberOfElementsForCalibration];
            int[] arrival_time = null;
            double[] mz = null;

            //procedure to enclose single ion events between zeros in time domain

            added_zeros = 0;

            int check = (int)(Math.Pow(2.0, dBinResolution) / dTimeScale * 2);

            for (j = 0; j < iNumberOfElementsForCalibration; j++)
            {
                if (j > 1 && iSummedIntensity[j] != 0)
                {
                    if (iSummedArrivalTime[j] - iSummedArrivalTime[j - 1] > (int)(Math.Pow(2.0, dBinResolution) / dTimeScale * 2))
                    {
                        zero_add_point_left[j] = j;
                        added_zeros = added_zeros + 1;
                    }
                    if (iSummedArrivalTime[j + 1] - iSummedArrivalTime[j] > (int)(Math.Pow(2.0, dBinResolution) / dTimeScale * 2))
                    {
                        zero_add_point_right[j] = j;
                        added_zeros = added_zeros + 1;
                    }
                }
            }

            //allocate memory for arrays with added zeros
            arrival_time = new int[iNumberOfElementsForCalibration + added_zeros]; //drift time
            iCalibrantArrivalTime = new int[iNumberOfElementsForCalibration + added_zeros]; //arrival time in tof
            mz = new double[iNumberOfElementsForCalibration + added_zeros];
            iCalibrantSummedIntensity = new int[iNumberOfElementsForCalibration + added_zeros];

            k = 0;
            for (j = 0; j < iNumberOfElementsForCalibration; ++j)
            {
                if ((j != zero_add_point_left[j] && j != zero_add_point_right[j]) || j == 0)
                {
                    iCalibrantSummedIntensity[k] = iSummedIntensity[j];
                    arrival_time[k] = iSummedArrivalTime[j];

                    mz[k] = Math.Pow((double)((arrival_time[k] + dTimeOffset) / 1000 - dIntercept), 2) * Math.Pow(dSlope, 2);
                    iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                    if (j == 0 || j == iNumberOfElementsForCalibration - 1)
                        iCalibrantSummedIntensity[k] = 0; //set first and last non-zero TOF bin to 0 as this is guaranteed noise
                    k++;
                }
                else
                {
                    if (j == zero_add_point_left[j] && zero_add_point_left[j] != 0)
                    {
                        iCalibrantSummedIntensity[k] = 0;
                        arrival_time[k] = (int)(iSummedArrivalTime[j] - Math.Pow(2.0, dBinResolution) / dTimeScale);

                        mz[k] = Math.Pow((arrival_time[k] + dTimeOffset) / 1000 - dIntercept, 2) * Math.Pow(dSlope, 2);
                        iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                        k++;
                        iCalibrantSummedIntensity[k] = iSummedIntensity[j];
                        arrival_time[k] = iSummedArrivalTime[j];
                        mz[k] = Math.Pow((arrival_time[k] + dTimeOffset) / 1000 - dIntercept, 2) * Math.Pow(dSlope, 2);
                        iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                        k++;
                    }
                    if (j == zero_add_point_right[j] && zero_add_point_right[j] != 0)
                    {
                        if (zero_add_point_right[j] != zero_add_point_left[j])
                        {
                            iCalibrantSummedIntensity[k] = iSummedIntensity[j];
                            arrival_time[k] = iSummedArrivalTime[j];

                            mz[k] = Math.Pow((arrival_time[k] + dTimeOffset) / 1000 - dIntercept, 2) * Math.Pow(dSlope, 2);
                            iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                            k++;

                            iCalibrantSummedIntensity[k] = 0;
                            arrival_time[k] = (int)(iSummedArrivalTime[j] + Math.Pow(2.0, dBinResolution) / dTimeScale);

                            mz[k] = Math.Pow((arrival_time[k] + dTimeOffset) / 1000 - dIntercept, 2) * Math.Pow(dSlope, 2);
                            iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                            k++;
                        }
                        else
                        {
                            iCalibrantSummedIntensity[k] = 0;
                            arrival_time[k] = (int)(iSummedArrivalTime[j] + Math.Pow(2.0, dBinResolution) / dTimeScale);

                            mz[k] = Math.Pow((arrival_time[k] + dTimeOffset) / 1000 - dIntercept, 2) * Math.Pow(dSlope, 2);
                            iCalibrantArrivalTime[k] = (int)(arrival_time[k] + dTimeOffset);

                            k++;
                        }
                    }
                }
            }

            iNumberOfElementsForCalibration = iNumberOfElementsForCalibration + added_zeros;

            return;
        }


        public bool OnInternalCalibration(string[] Calibrant_Name, int NumberOfCalibrants, double[] Calibrant_MZ, int[] Calibrant_Charge, int NumberOfBins, int[] Calibrant_Frame_TOF, int[] Calibrant_Frame_Intensity, double Slope, double Intercept)
        {
            int NumNonZeroCalibrants = 0, NumNonZeroCalibrantsCorrected = 0;
            bool use_external_calibration = false, internal_calibrants_found = false;
            int i, j;
            double sum_mz_term = 0, sum_TOF_term = 0, sum_TOF_term_squared = 0, sum_mz_TOF_term = 0;
            double sum_x4 = 0, sum_x3 = 0, sum_x2 = 0, sum_x = 0, sum_yx2 = 0, sum_yx = 0, sum_y = 0;

            double[] pmz_calibrant = new double[NumberOfCalibrants];
            double[] pmz_calibrant_new = new double[NumberOfCalibrants];
            int[] pcharge_calibrant = new int[NumberOfCalibrants];
            double[] pTOF_calibrant = new double[NumberOfCalibrants];
            double[] pmz_calibrant_exp = new double[NumberOfCalibrants];
            double[] pTOF_calibrant_external = new double[NumberOfCalibrants];
            double[] pTOF_calibrant_external_non_zero = new double[NumberOfCalibrants];
            double[] pmz_calibrant_non_zero = new double[NumberOfCalibrants];
            int[] pcharge_calibrant_non_zero = new int[NumberOfCalibrants];
            double[] error_ppm_calibrant = new double[NumberOfCalibrants];

            string[] calibrant_name = new string[NumberOfCalibrants];

            for (i = 0; i < NumberOfCalibrants; ++i)
            {
                pTOF_calibrant[i] = 0;
                pTOF_calibrant_external[i] = 0;
                pTOF_calibrant_external_non_zero[i] = 0;
                pmz_calibrant_non_zero[i] = 0;
                pmz_calibrant_new[i] = 0;
                pmz_calibrant[i] = Calibrant_MZ[i];
                pcharge_calibrant[i] = Calibrant_Charge[i];
            }

            j = 0;
            for (i = 0; i < NumberOfCalibrants; ++i)
            {
                if (pmz_calibrant[i] > 0)
                {
                    pTOF_calibrant_external[i] = (double)(Math.Sqrt(Calibrant_MZ[i]) / Slope + Intercept);
                    pTOF_calibrant_external_non_zero[j] = pTOF_calibrant_external[i];
                    pmz_calibrant_non_zero[j] = Calibrant_MZ[i];
                    pcharge_calibrant_non_zero[j] = Calibrant_Charge[i];
                    Calibrant_Name[i] = String.Copy(calibrant_name[j]);
                    NumNonZeroCalibrants++;
                    j++;
                }
                else
                {
                    pTOF_calibrant_external[i] = 0;
                }
            }

            //now go to TOF spectra and find peak maxima
            for (j = 0; j < NumNonZeroCalibrants; ++j)
            {
                pTOF_calibrant[j] = OnFindingMonoisotopicPeak(NumberOfBins, pTOF_calibrant_external_non_zero[j] * 1000, calibrant_name[j], pmz_calibrant_non_zero[j], pcharge_calibrant_non_zero[j], Calibrant_Frame_TOF, Calibrant_Frame_Intensity, Slope, Intercept);
                if (pTOF_calibrant[j] - pTOF_calibrant_external_non_zero[j] > 0.05)
                    pTOF_calibrant[j] = 0; //false identification is offset is more than 0.05 us
                pmz_calibrant[j] = pmz_calibrant_non_zero[j];
                if (j == 0)
                    NumNonZeroCalibrantsCorrected = NumNonZeroCalibrants;
                if (pTOF_calibrant[j] == 0)
                {
                    NumNonZeroCalibrantsCorrected--;
                    pmz_calibrant[j] = 0; //if calibrant wasn't found, set its m/z to zero
                    if (NumNonZeroCalibrantsCorrected < 2)
                        use_external_calibration = true;
                }
            }

            //least-square fit
            if (!use_external_calibration)
            {
                if (NumNonZeroCalibrants > 1)
                {
                    for (i = 0; i < NumNonZeroCalibrants; i++) //take all the requested calibrants but use only non-zero entries
                    {
                        internal_calibrants_found = true;
                        status = true;
                        if (pmz_calibrant[i] > 0)
                        {
                            pmz_calibrant_new[i] = Math.Sqrt(pmz_calibrant[i]);

                            sum_mz_term = sum_mz_term + pmz_calibrant_new[i];
                            sum_TOF_term = sum_TOF_term + pTOF_calibrant[i];
                            sum_TOF_term_squared = sum_TOF_term_squared + Math.Pow(pTOF_calibrant[i], 2);
                            sum_mz_TOF_term = sum_mz_TOF_term + pmz_calibrant_new[i] * (pTOF_calibrant[i]);
                        }
                    }

                    dSlope_internal = (NumNonZeroCalibrantsCorrected * sum_mz_TOF_term - sum_mz_term * sum_TOF_term) / (NumNonZeroCalibrantsCorrected * sum_TOF_term_squared - Math.Pow(sum_TOF_term, 2));
                    dIntercept_internal = sum_mz_term / NumNonZeroCalibrantsCorrected - dSlope_internal / NumNonZeroCalibrantsCorrected * sum_TOF_term;
                    dIntercept_internal = -dIntercept_internal / dSlope_internal;
                }
            }
            else
            {
                dSlope_internal = Slope;
                dIntercept_internal = Intercept;

                internal_calibrants_found = false;
                use_external_calibration = true;
                status = false;
            }

            if (dSlope_internal == 0 || dIntercept_internal == 0)
            {
                dSlope_internal = Slope;
                dIntercept_internal = Intercept;

                internal_calibrants_found = false;
                use_external_calibration = true;
                status = false;

            }

            for (j = 0; j < NumNonZeroCalibrants; ++j)
            {
                if (pmz_calibrant[j] > 0)
                {
                    pmz_calibrant_exp[j] = Math.Pow((pTOF_calibrant[j] - dIntercept_internal) * dSlope_internal, 2);
                    error_ppm_calibrant[j] = (pmz_calibrant_exp[j] - pmz_calibrant[j]) / pmz_calibrant[j] * Math.Pow(10.0, 6);
                }
            }

            for (j = 0; j < NumNonZeroCalibrants; ++j)
            {
                AdCalibrantMZ[j] = pmz_calibrant_exp[j];
                AiCalibrantCharge[j] = pcharge_calibrant_non_zero[j];
                AdError_PPM[j] = error_ppm_calibrant[j];
                calibrant_name[j] = String.Copy(AsCalibrantName[j]);
            }

            iNumberOfNonZeroCalibrants = NumNonZeroCalibrants;

            return status;
        }


        ///routine for finding monoisotopic peak of a peptide
        double OnFindingMonoisotopicPeak(int NumberOfBins, double calibrant_TOF_ns, string calibrant_name, double calibrant_mz, int calibrant_charge, int[] calibrant_frame_TOF, int[] calibrant_frame_intensities, double Slope, double Intercept)
        {
            int peak_number, charge = 1, peak_number_mono = 11, peak_misfit = 10, NumNoiseBins, i, j, pos_peptide_max = 0;
            int[] Pos_peptide_max = new int[10];
            double TOF_monoisotope = 0, isotope_ratio = 2.5;
            double[] peptide_local_max = new double[10];
            double[] TOF_offset_local = new double[10];
            double peptide_monoisotope_max;
            double TOF_monoisotope_shift, TOF_checkup = 2;
            bool TOF_MONOISOTOPE_FOUND = false, PEAK_COUNTER;
            bool[,] TOF_MONOISOTOPE = new bool[10, 10];
            long SIGNAL_THRESHOLD;
            bool found_peptide_pos = false;

            double PeptideSearchInterval = 20, NoiseSearchInterval = 30;

            int Pos_noise, Pos_peptide = 0, SNR = 3, NumNonZeroNoiseBins = 0, NumBinsTOF = 80;
            double noise_intensity_average = 0;
            double noise_intensity0 = 0;
            double[] peptide_array_local;
            double[,] pVariable;

            peptide_array_local = new double[NumBinsTOF];

            pVariable = new double[3, 4];

            peptide_monoisotope_max = -1; // initial condition, no peaks found yet

            //three point quadratic declarations
            double sum_x4 = 0, sum_x3 = 0, sum_x2 = 0, sum_x = 0, sum_yx2 = 0, sum_yx = 0, sum_y = 0;

            //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
            TOF_monoisotope_shift = (double)(1 / Slope / calibrant_charge / 2 / Math.Sqrt(calibrant_mz)) * 1000; //spacing between isotopes
            SIGNAL_THRESHOLD = 10;

            if (calibrant_charge == 1)
            {
                isotope_ratio = 10.0;
                SNR = 50;
            }

            for (i = 0; i < NumberOfBins; i++)
            {
                if ((calibrant_TOF_ns - calibrant_frame_TOF[i]) < PeptideSearchInterval)
                {
                    Pos_peptide = i;
                    found_peptide_pos = true;
                    break;
                }
            }

            if (found_peptide_pos == false)
            {
                return TOF_monoisotope = 0;
            }

            peak_number = 0;

            for (i = 0; i < 10; i++)
            {
                peptide_local_max[i] = 0;
                TOF_offset_local[i] = 0;
                Pos_peptide_max[i] = 0;
            }

            for (i = 0; i < 10; i++)
            {
                for (j = 0; j < 10; j++)
                    TOF_MONOISOTOPE[i, j] = false;
            }

            for (i = 0; i < NumBinsTOF; i++)
                peptide_array_local[i] = 0.0;

            //estimate average noise intensity at the left wing of isotopic distribution
            i = 0;
            do
            {
                Pos_noise = Pos_peptide - i;
                ++i;

            } while (calibrant_frame_TOF[Pos_peptide] - calibrant_frame_TOF[Pos_noise] < NoiseSearchInterval);

            NumNoiseBins = i - 1;
            NumNonZeroNoiseBins = 0;
            noise_intensity_average = 0;

            if (calibrant_frame_TOF[Pos_peptide] - calibrant_frame_TOF[Pos_noise] > NoiseSearchInterval)
            {
                Pos_noise = Pos_noise + 1;
                NumNoiseBins = i - 2;
            }

            for (i = Pos_noise; i < Pos_noise + NumNoiseBins; ++i)
            {
                if (calibrant_frame_intensities[i] > 0 && NumNonZeroNoiseBins == 0)
                {
                    NumNonZeroNoiseBins++;
                    noise_intensity_average = noise_intensity_average + calibrant_frame_intensities[i];
                    noise_intensity0 = noise_intensity_average;
                }

                if (calibrant_frame_intensities[i] > 0 && NumNonZeroNoiseBins > 0)
                {
                    NumNonZeroNoiseBins++;
                    noise_intensity_average = noise_intensity_average + calibrant_frame_intensities[i];
                }
            }

            //finding average noise for all NumNoiseBins without taking into account the outlyars
            if (NumNonZeroNoiseBins > 0)
                noise_intensity_average = noise_intensity_average / NumNonZeroNoiseBins;

            if (noise_intensity_average == 0)
                SIGNAL_THRESHOLD = 500;

            for (i = Pos_peptide; i < Pos_peptide + NumBinsTOF; ++i)
            {

                //now identify isotopic peaks
                if ((calibrant_frame_intensities[i] > SIGNAL_THRESHOLD && calibrant_frame_intensities[i] > noise_intensity_average * SNR && i > 4 && peak_number < 10)
                    || (calibrant_frame_intensities[i - 4] > SIGNAL_THRESHOLD && calibrant_frame_intensities[i - 4] > noise_intensity_average * SNR && i > 4 && peak_number < 10)
                    || (calibrant_frame_intensities[i - 3] > SIGNAL_THRESHOLD && calibrant_frame_intensities[i - 3] > noise_intensity_average * SNR && i > 4 && peak_number < 10)
                    || (calibrant_frame_intensities[i - 2] > SIGNAL_THRESHOLD && calibrant_frame_intensities[i - 2] > noise_intensity_average * SNR && i > 4 && peak_number < 10)
                    || (calibrant_frame_intensities[i - 1] > SIGNAL_THRESHOLD && calibrant_frame_intensities[i - 1] > noise_intensity_average * SNR && i > 4 && peak_number < 10))
                {
                    if ((calibrant_frame_intensities[i - 2] > calibrant_frame_intensities[i - 1])
                        && (calibrant_frame_intensities[i - 2] > calibrant_frame_intensities[i])
                        && (calibrant_frame_intensities[i - 3] < calibrant_frame_intensities[i - 2])
                        && (calibrant_frame_intensities[i - 4] < calibrant_frame_intensities[i - 2]))
                    {
                        peptide_local_max[peak_number] = calibrant_frame_intensities[i - 2];
                        TOF_offset_local[peak_number] = calibrant_frame_TOF[i - 2]; // define file pointer position corresponding to the local max signal
                        Pos_peptide_max[peak_number] = i - 2;
                        peak_number++;
                    }
                }
            }

            if (peak_number < 3 && calibrant_charge != 1)
            {
                //didn't find isotopic cluster for multiply charged states
                return TOF_monoisotope;
            }

            //peak_number =1;
            double peptide_local_max1 = peptide_local_max[0];

            //Analyze all combinations of found peptide peaks and find the two matching the criterium of TOF_checkup. 
            //Then select the smallest index as peak_number_mono
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
                                        if (j - i > 1 && calibrant_charge != 1) // verifying that peaks in between the putative isotopic peaks are less than both isotopes
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
                                        if (i - j > 1 && calibrant_charge != 1) // verifying that intensities of peaks in between the putative isotopic peaks are less than that of both isotopes
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

            //check if monoisotopic peak was ever found
            TOF_MONOISOTOPE_FOUND = false;

            for (i = 0; i < 10; i++)
            {
                for (j = 0; j < 10; j++)
                {
                    if (TOF_MONOISOTOPE[i, j] == true)
                        TOF_MONOISOTOPE_FOUND = true;
                }
            }

            if (peak_number_mono > 10 || !TOF_MONOISOTOPE_FOUND)
                TOF_monoisotope = 0;
            else
            {
                //THREE POINT QUADRATIC peak picking - return TOF monoisotope after routine below
                if (TOF_MONOISOTOPE_FOUND)
                {
                    for (i = 0; i < 3; ++i)
                    {
                        //sum_x4 = sum_x4 + Math.Pow((double)((Pos_peptide_max[peak_number_mono]+sizeof(double)*(i-1))/sizeof(double) - fSpectrumNumber * NumElementsIn)*AcquisitionBin + offset_mariner,4);
                        sum_x4 = sum_x4 + Math.Pow((double)i, 4);
                        sum_x3 = sum_x3 + Math.Pow((double)i, 3);
                        sum_x2 = sum_x2 + Math.Pow((double)i, 2);
                        sum_x = sum_x + i;

                        pos_peptide_max = Pos_peptide_max[peak_number_mono] + (i - 1);

                        sum_yx2 = sum_yx2 + calibrant_frame_intensities[pos_peptide_max] * Math.Pow((double)i, 2);
                        sum_yx = sum_yx + calibrant_frame_intensities[pos_peptide_max] * i;
                        sum_y = sum_y + calibrant_frame_intensities[pos_peptide_max];
                    }

                    double[,] local_Variable = { { sum_x4, sum_x3, sum_x2, sum_yx2 }, { sum_x3, sum_x2, sum_x, sum_yx }, { sum_x2, sum_x, 3, sum_y } };

                    for (int k = 0; k < 3; ++k)
                    {
                        for (j = 0; j < 4; ++j)
                        {
                            pVariable[k, j] = local_Variable[k, j];
                        }
                    }

                    OnQuadraticLeastSquareFit(pVariable);

                    if (dCoefficientA != 0) TOF_monoisotope = -dCoefficientB / 2 / dCoefficientA;
                    else TOF_monoisotope = 0;

                    TOF_monoisotope = TOF_monoisotope + calibrant_frame_TOF[pos_peptide_max - 2];

                    if (TOF_monoisotope > 110000) TOF_monoisotope = 0; //set TOF to zero if outside of m/z range

                    peptide_monoisotope_max = peptide_local_max[peak_number_mono];
                }
            }

            TOF_monoisotope = TOF_monoisotope / 1000;

            return TOF_monoisotope;

        }

        void OnQuadraticLeastSquareFit(double[,] Variable)
        {
            double Determinant0, DeterminantA, DeterminantB, DeterminantC;
            int i = 0, j = 0;

            dCoefficientA = 0;
            dCoefficientB = 0;
            dCoefficientC = 0;

            Determinant0 = Math.Pow(-1.0, i + j) * Variable[i, j] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 2] - Variable[i + 2, j + 1] * Variable[i + 1, j + 2]);
            Determinant0 = Determinant0 + Math.Pow(-1.0, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j] * Variable[i + 2, j + 2] - Variable[i + 2, j] * Variable[i + 1, j + 2]);
            Determinant0 = Determinant0 + Math.Pow(-1.0, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j] * Variable[i + 2, j + 1] - Variable[i + 2, j] * Variable[i + 1, j + 1]);

            DeterminantA = Math.Pow(-1.0, i + j) * Variable[i, j + 3] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 2] - Variable[i + 2, j + 1] * Variable[i + 1, j + 2]);
            DeterminantA = DeterminantA + Math.Pow(-1.0, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 2] - Variable[i + 2, j + 3] * Variable[i + 1, j + 2]);
            DeterminantA = DeterminantA + Math.Pow(-1.0, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 1] - Variable[i + 2, j + 3] * Variable[i + 1, j + 1]);

            DeterminantB = Math.Pow(-1.0, i + j) * Variable[i, j] * (Variable[i + 1, j + 3] * Variable[i + 2, j + 2] - Variable[i + 2, j + 3] * Variable[i + 1, j + 2]);
            DeterminantB = DeterminantB + Math.Pow(-1.0, i + j + 1) * Variable[i, j + 3] * (Variable[i + 1, j] * Variable[i + 2, j + 2] - Variable[i + 2, j] * Variable[i + 1, j + 2]);
            DeterminantB = DeterminantB + Math.Pow(-1.0, i + j + 2) * Variable[i, j + 2] * (Variable[i + 1, j] * Variable[i + 2, j + 3] - Variable[i + 2, j] * Variable[i + 1, j + 3]);

            DeterminantC = Math.Pow(-1.0, i + j) * Variable[i, j] * (Variable[i + 1, j + 1] * Variable[i + 2, j + 3] - Variable[i + 2, j + 1] * Variable[i + 1, j + 3]);
            DeterminantC = DeterminantC + Math.Pow(-1.0, i + j + 1) * Variable[i, j + 1] * (Variable[i + 1, j] * Variable[i + 2, j + 3] - Variable[i + 2, j] * Variable[i + 1, j + 3]);
            DeterminantC = DeterminantC + Math.Pow(-1.0, i + j + 2) * Variable[i, j + 3] * (Variable[i + 1, j] * Variable[i + 2, j + 1] - Variable[i + 2, j] * Variable[i + 1, j + 1]);

            if (Determinant0 != 0)
            {
                dCoefficientA = DeterminantA / Determinant0;
                dCoefficientB = DeterminantB / Determinant0;
                dCoefficientC = DeterminantC / Determinant0;
            }
            else
            {
                dCoefficientA = 0;
                dCoefficientB = 0;
                dCoefficientC = 0;
            }

            return;
        }

        public void AllocateCalibrationArrays(int NumberOfElements)
        {
            int i = 0;

            //allocate global arrays
            AdError_PPM = new double[NumberOfElements];
            AiCalibrantCharge = new int[NumberOfElements];
            AdCalibrantMZ = new double[NumberOfElements];

            AsCalibrantName = new string[NumberOfElements];

            return;
        }
    }
}
