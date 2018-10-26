using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
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
    ///
    /// </summary>
    /// <remarks>CODE BY MIKE BELOV TO CALIBRATE DATA</remarks>
    public class PostProcessingViewModel : ReactiveObject
    {
        private CalibrantSet currentCalibrantSet = CalibrantSet.All;
        private string customCalibrantsFilePath;
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
        private readonly UIMFDataWrapper uimfReader = null;
        private double tofBinWidth = 0;

        // TODO: Have this information hard-coded, but also set up and use an external file (or files) specified by the user for customizing the calibrants; also support loading different sets of calibrants, rather than showing all of them in a single list
        public IReadOnlyReactiveList<CalibrantSet> CalibrantSets { get; }

        public CalibrantSet CurrentCalibrantSet
        {
            get => currentCalibrantSet;
            set => this.RaiseAndSetIfChanged(ref currentCalibrantSet, value);
        }

        public string CustomCalibrantsFilePath
        {
            get => customCalibrantsFilePath;
            set => this.RaiseAndSetIfChanged(ref customCalibrantsFilePath, value);
        }

        public string CustomCalibrantsFileDescription => CalibrantInfo.FileFormatDescription;

        public ReactiveList<CalibrantInfo> Calibrants { get; } = new ReactiveList<CalibrantInfo>(CalibrantInfo.GetCalibrantSet(CalibrantSet.All, null));

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

        public ReactiveCommand<Unit, Unit> BrowseForCalibrantsFileCommand { get; }
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

            CalibrantSets = new ReactiveList<CalibrantSet>(Enum.GetValues(typeof(CalibrantSet)).Cast<CalibrantSet>());

            BrowseForCalibrantsFileCommand = ReactiveCommand.Create(BrowseForCalibrantsFile);
            AttemptToCalibrateCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CalibrateFrames()));
            ApplyCalculatedToFrameCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => ApplyCalculatedCalibration()));
            ApplyExperimentalToAllFramesCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => ApplyCalibrationAllFrames()));
            BrowseDecodeDirectoryCommand = ReactiveCommand.Create(DecodeDirectoryBrowse);
            //DecodeExperimentCommand;
            BrowseCompressDirectoryCommand = ReactiveCommand.Create(CompressDirectoryBrowse);
            CompressExperimentCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => Compress4GHzTo1GHzUIMF()));

            this.WhenAnyValue(x => x.Ion1Mz, x => x.Ion1TOFBin, x => x.Ion2Mz, x => x.Ion2TOFBin).Subscribe(x => Recalculate2PointCalibration());
            this.WhenAnyValue(x => x.CurrentCalibrantSet).Subscribe(x => LoadCalibrantSet(x));
        }

        public PostProcessingViewModel(UIMFDataWrapper uimf) : this()
        {
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

            CalibrationSuccessful = false;
        }

        private CalibrantInfo currentCalibrant = new CalibrantInfo();

        private const int MinNumCalibrants = 4;
        private const double MaxErrorAcceptable = 5.0;

        private const double NoiseInterval = 30;
        private const double PeptideInterval = 20;

        private Coefficients internalCoefficients;

        //allocate memory for arrays with added zeros
        //  double[] arrival_time2; //drift time
        private double[] arrivalTimeTof2; //arrival time in tof
        private double[] mz2;
        private double[] sumIntensity2;

        public void CalibrateFrame(double[] summedSpectrum, double[] sumIntensities, double[] binArrivalTime, double binWidth, int totalBins, int totalScans, double mzExperimentalSlope, double mzExperimentalIntercept)
        {
            var numEnabledCalibrants = 0;
            //Calibrants.ClearSelection();

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                for (var i = 0; i < Calibrants.Count - 1; ++i)
                {
                    var calibrant = Calibrants[i];
                    calibrant.Bins = 0;
                    calibrant.TOFExperimental = 0;
                    calibrant.MzExperimental = 0;
                    calibrant.ErrorPPM = 0;
                    calibrant.NotFound = false;

                    if (calibrant.Enabled)
                    {
                        Calibrants[i].TOF = (double) ((Math.Sqrt(Calibrants[i].Mz) / ExperimentalSlope) + ExperimentalIntercept);
                        Calibrants[i].Bins = Calibrants[i].TOF / tofBinWidth;

                        numEnabledCalibrants++;
                    }
                }
            });

            while (numEnabledCalibrants >= MinNumCalibrants)
            {
                var spectraWithNonZeroEntries = sumIntensities.Length;

                SetExperimentalCoefficients(mzExperimentalSlope, mzExperimentalIntercept);

                sumIntensity2 = new double[spectraWithNonZeroEntries];
                arrivalTimeTof2 = new double[spectraWithNonZeroEntries]; //arrival time in bins

                Array.Copy(sumIntensities, sumIntensity2, sumIntensities.Length);
                Array.Copy(binArrivalTime, arrivalTimeTof2, binArrivalTime.Length);

                mz2 = new double[spectraWithNonZeroEntries];
                for (var i = 0; i < spectraWithNonZeroEntries; i++)
                {
                    // arrival_time_TOF2[i] *= bin_width;
                    mz2[i] = Math.Pow((arrivalTimeTof2[i] / 1000.0) - ExperimentalIntercept, 2) * ExperimentalSlope * ExperimentalSlope;
                }
                // mz_LIST2[i][k] = (float)pow((double)(arrival_time_LIST2[i][k] - *(TOF_offset_buffer + i) + TimeOffset) / 1000 - ExperimentalIntercept, 2) * (float)pow((double)ExperimentalSlope, 2);

                var maxErrorIndex = InternalCalibration(CalibrationType.STANDARD, Instrument.AGILENT_TDC, totalScans, numEnabledCalibrants);

                if (Math.Abs(Calibrants[maxErrorIndex].ErrorPPM) < MaxErrorAcceptable)
                {
                    SetExperimentalCoefficients(internalCoefficients.ExperimentalSlope, internalCoefficients.ExperimentalIntercept);
                    break;
                }
                else
                {
                    var index = maxErrorIndex;
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        CalculatedIntercept = 0;
                        CalculatedSlope = 0;

                        SetExperimentalCoefficients(ExperimentalSlope, ExperimentalIntercept);
                        internalCoefficients.ExperimentalSlope = ExperimentalSlope;
                        internalCoefficients.ExperimentalIntercept = ExperimentalIntercept;

                        Calibrants[index].Enabled = false;

                        // TODO: (mz_experimental column): Calibrants[index].Cells[7].Value = "FAILED";

                        numEnabledCalibrants = 0;
                        for (var i = 0; i < Calibrants.Count - 1; i++)
                        {
                            if (Calibrants[i].Enabled)
                                numEnabledCalibrants++;
                        }
                    });
                }
            }

#if false
            FileStream fs = new FileStream(@"C:\IonMobilityData\Calibration\NewCalib.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < spectra_with_nonzeroentries; i++)
            {
                sw.WriteLine(mz2[i].ToString("0.0000") + ", " + sumIntensity2[i].ToString() + ", " + arrivalTimeTof2[i].ToString());
            }
            sw.Flush();
            sw.Close();
            fs.Close();
#endif
        }

        public int InternalCalibration(CalibrationType calibrationType, Instrument instrumentType, int tofsPerFrame, int numEnabledCalibrants)
        {
            // now go to TOF spectra and find peak maxima
            var numEnabledCalibrantsCorrected = numEnabledCalibrants;

            for (var i = 0; i < Calibrants.Count - 1; i++)
            {
                var calibrant = Calibrants[i];

                if (calibrant.Enabled) //calibrant.Mz > 0)
                {
                    currentCalibrant.Name = calibrant.Name;
                    currentCalibrant.Charge = calibrant.Charge;
                    currentCalibrant.Mz = calibrant.Mz;

                    var expTof = FindMonoisotopicPeak(calibrant.Bins, Species.CALIBRANT, PeakPicking.THREE_POINT_QUADRATIC, tofsPerFrame);
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

            var maxErrorIndex = 0;
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                //MessageBox.Show(this, "numEnabledCalibrants: "+numEnabledCalibrants.ToString());
                calibrationType = CalibrationType.AGILENT;
                if (calibrationType == CalibrationType.AGILENT)
                {
                    var sumMzTerm = 0.0;
                    var sumTofTerm = 0.0;
                    var sumTofTermSquared = 0.0;
                    var sumMzTofTerm = 0.0;
                    for (var i = 0; i < Calibrants.Count - 1; i++)
                    {
                        var calibrant = Calibrants[i];
                        if (calibrant.Enabled) //calibrant.Mz > 0)
                        {
                            // internal_calibrants_found = true;
                            if (calibrant.Mz > 0)
                            {
                                calibrant.MzExperimental = Math.Sqrt(calibrant.Mz);

                                sumMzTerm = sumMzTerm + calibrant.MzExperimental;
                                sumTofTerm = sumTofTerm + calibrant.TOFExperimental;
                                sumTofTermSquared = sumTofTermSquared + Math.Pow(calibrant.TOFExperimental, 2);
                                sumMzTofTerm = sumMzTofTerm + calibrant.MzExperimental * (calibrant.TOFExperimental);
                            }

                            internalCoefficients.ExperimentalSlope = (numEnabledCalibrantsCorrected * sumMzTofTerm - sumMzTerm * sumTofTerm) /
                                                              (numEnabledCalibrantsCorrected * sumTofTermSquared - Math.Pow(sumTofTerm, 2));
                            internalCoefficients.ExperimentalIntercept = (sumMzTerm / numEnabledCalibrantsCorrected) -
                                                                  ((internalCoefficients.ExperimentalSlope * sumTofTerm) / numEnabledCalibrantsCorrected);
                            internalCoefficients.ExperimentalIntercept = -internalCoefficients.ExperimentalIntercept / internalCoefficients.ExperimentalSlope;
                        }
                    }
                }

                // check the results
                for (var i = 0; i < Calibrants.Count - 1; ++i)
                    if (Calibrants[i].Enabled)
                    {
                        maxErrorIndex = i;
                        break;
                    }

                var success = true;
                for (var i = maxErrorIndex; i < Calibrants.Count - 1; ++i)
                {
                    var calibrant = Calibrants[i];
                    if (calibrant.Enabled)
                    {
                        calibrant.MzExperimental = Math.Pow((calibrant.TOFExperimental - internalCoefficients.ExperimentalIntercept) * internalCoefficients.ExperimentalSlope, 2);
                        calibrant.ErrorPPM = ((calibrant.MzExperimental - calibrant.Mz) / calibrant.Mz) * 1e6;

                        if (Math.Abs(calibrant.ErrorPPM) > Math.Abs(Calibrants[maxErrorIndex].ErrorPPM))
                            maxErrorIndex = i;

                        if (calibrant.TOFExperimental <= 0)
                        {
                            success = false;
                        }
                    }
                }

                CalibrationSuccessful = success;
            });

            return maxErrorIndex;
        }

        /// <summary>
        /// routine for finding monoisotopic peak of a peptide
        /// </summary>
        /// <param name="tofPeptide"></param>
        /// <param name="speciesId"></param>
        /// <param name="peakPicking"></param>
        /// <param name="spectraWithNonzeroEntries"></param>
        /// <returns></returns>
        private double FindMonoisotopicPeak(double tofPeptide, Species speciesId, PeakPicking peakPicking, int spectraWithNonzeroEntries)
        {
            /*
            int bins_per_tof = 0;
            if (BinResolution <= 3)
                bins_per_tof = 80;
            else if (BinResolution <= 3.5)
                bins_per_tof = 60;  //3.3219 gives 1.0 ns
            else if (BinResolution >= 3.5)
                bins_per_tof = 40;
            */

            int charge;
            double tofMonoisotopeShift;
#if false
            if (speciesId == Species.PEPTIDE)
            {
                charge = current_Peptide.charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                tofMonoisotopeShift = (double)(1000.0 / (ExperimentalSlope * charge * 2.0 * Math.Sqrt(current_Peptide.mz))); //spacing between isotopes
            }
            else if (speciesId == Species.CALIBRANT)
#endif
            {
                charge = currentCalibrant.Charge;

                //setting up TOF shifts for C12 and C13 peaks as well as signal threshold
                tofMonoisotopeShift = (double)(1000.0 / (ExperimentalSlope * charge * 2.0 * Math.Sqrt(currentCalibrant.Mz))); //spacing between isotopes
            }

            // tofCheckup = 2 * Math.Pow(2, BinResolution) / 10;
            var tofCheckup = 2.0 / tofBinWidth;

            //tofMonoisotopeShift = 1000;
            // tofCheckup = 200;
            //MessageBox.Show(this, "tofCheckup=" + tofCheckup.ToString() + ", tofMonoisotopeShift=" + tofMonoisotopeShift.ToString());

            var isotopeRatio = 2.5;
            var snr = 3;
            if (charge == 1)
            {
                isotopeRatio = 10.0;
                snr = 2;
            }

            var posPeptide = 0;
            var foundPeptidePos = false;
            for (var i = 0; i < spectraWithNonzeroEntries; i++)
            {
                if ((tofPeptide - arrivalTimeTof2[i]) < PeptideInterval / tofBinWidth)
                {
                    posPeptide = i;
                    foundPeptidePos = true;
                    break;
                }
            }
            // MessageBox.Show(this, "foundPeptidePos: " + foundPeptidePos.ToString()+ "  "+(arrivalTimeTof2[i]* bin_Width).ToString() );

            if (!foundPeptidePos)
                return 0;


            var peptideLocalMax = new double[10];
            var tofOffsetLocal = new double[10];
            var posPeptideMaxMatches = new int[10];
            var peakNumber = 0;
            for (var i = 0; i < 10; i++)
            {
                peptideLocalMax[i] = 0;
                tofOffsetLocal[i] = 0;
                posPeptideMaxMatches[i] = 0;
            }

            var tofMonoisotopeCheckArray = new bool[10, 10];
            for (var i = 0; i < 10; i++)
                for (var j = 0; j < 10; j++)
                    tofMonoisotopeCheckArray[i, j] = false;

            // estimate average noise intensity at the left wing of isotopic distribution
            int posNoise;
            var i2 = 0;
            do
            {
                posNoise = posPeptide - i2;
                ++i2;
            } while ((posNoise > 0) && (arrivalTimeTof2[posPeptide] - arrivalTimeTof2[posNoise] < NoiseInterval / tofBinWidth));

            var numNoiseBins = i2 - 1;
            var numNonZeroNoiseBins = 0;
            double noiseIntensityAverage = 0;

            if (arrivalTimeTof2[posPeptide] - arrivalTimeTof2[posNoise] > NoiseInterval / tofBinWidth)
            {
                posNoise = posNoise + 1;
                numNoiseBins = i2 - 2;
            }

            try
            {
                for (var i = posNoise; i < posNoise + numNoiseBins; ++i)
                {
                    if (sumIntensity2[i] > 0 && numNonZeroNoiseBins == 0)
                    {
                        numNonZeroNoiseBins++;
                        noiseIntensityAverage = noiseIntensityAverage + sumIntensity2[i];
                    }

                    if (sumIntensity2[i] > 0 && numNonZeroNoiseBins > 0) /*&& fabs(sum_intensity2[i]/noise_intensity0) < 10*/
                    {
                        numNonZeroNoiseBins++;
                        noiseIntensityAverage = noiseIntensityAverage + sumIntensity2[i];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("arg" + ex.ToString());
            }

            // finding average noise for all NumNoiseBins without taking into account the outliers
            if (numNonZeroNoiseBins > 0)
                noiseIntensityAverage = noiseIntensityAverage / numNonZeroNoiseBins / 4.0;

            long signalThreshold = 5;
            for (var i = posPeptide; (i < posPeptide + 80) && (i < sumIntensity2.Length); ++i)
            {
                // now identify isotopic peaks
                if ((i > 4) && (peakNumber < 10))
                {
                    var snrNoiseLevel = noiseIntensityAverage * snr;

                    if (((sumIntensity2[i] > signalThreshold) && (sumIntensity2[i] > snrNoiseLevel)) ||
                        ((sumIntensity2[i - 4] > signalThreshold) && (sumIntensity2[i - 4] > snrNoiseLevel)) ||
                        ((sumIntensity2[i - 3] > signalThreshold) && (sumIntensity2[i - 3] > snrNoiseLevel)) ||
                        ((sumIntensity2[i - 2] > signalThreshold) && (sumIntensity2[i - 2] > snrNoiseLevel)) ||
                        ((sumIntensity2[i - 1] > signalThreshold) && (sumIntensity2[i - 1] > snrNoiseLevel)))
                    {
                        if ((sumIntensity2[i - 2] > sumIntensity2[i - 1]) && //
                            (sumIntensity2[i - 1] > sumIntensity2[i]) &&     // fixed this line
                            (sumIntensity2[i - 3] < sumIntensity2[i - 2]) && //
                            (sumIntensity2[i - 4] < sumIntensity2[i - 2]))   //
                        {
                            peptideLocalMax[peakNumber] = sumIntensity2[i - 2];
                            tofOffsetLocal[peakNumber] = arrivalTimeTof2[i - 2]; // define file pointer position corresponding to the local max signal
                            posPeptideMaxMatches[peakNumber] = i - 2;
                            peakNumber++;
                        }
                    }
                }
            }

            if ((peakNumber < 3) && (charge != 1))
            {
                // didn't find isotopic cluster for multiply charged states
                return 0;
            }

            var peakNumberMono = 11;

            // Analyze all combinations of found peptide peaks and find the two matching the criterium of TOF_checkup.
            // Then select the smallest index as peak_number_mono
            if ((speciesId == Species.PEPTIDE) || (speciesId == Species.CALIBRANT))
            {
                for (var i = 0; i < peakNumber; ++i)
                {
                    for (var j = i + 1; j < peakNumber; ++j)
                    {
                        if (Math.Abs(Math.Abs(tofOffsetLocal[i] - tofOffsetLocal[j]) - tofMonoisotopeShift) < tofCheckup * 10.0)
                        {
                            if (peptideLocalMax[i] != 0)
                            {
                                if ((peptideLocalMax[i] / peptideLocalMax[j] < isotopeRatio && peptideLocalMax[i] / peptideLocalMax[j] > 1 / isotopeRatio) || (peptideLocalMax[j] / peptideLocalMax[i] < isotopeRatio && peptideLocalMax[j] / peptideLocalMax[i] > 1 / isotopeRatio))
                                {
                                    bool peakCounter;
                                    if (i < j && peakNumberMono > i)
                                    {
                                        peakCounter = false;
                                        if (j - i > 1 && (charge != 1)) // verifying that peaks in between the putative isotopic peaks are less than both isotopes
                                        {
                                            for (var k = i + 1; k <= j - 1; ++k)
                                            {
                                                if (peptideLocalMax[k] > peptideLocalMax[i] || peptideLocalMax[k] > peptideLocalMax[j] || peakCounter)
                                                {
                                                    tofMonoisotopeCheckArray[i, j] = false;
                                                    peakCounter = true; //accounts for the case of several peaks between putative isotopic peaks
                                                }
                                                else
                                                {
                                                    peakNumberMono = i;
                                                    tofMonoisotopeCheckArray[i, j] = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            tofMonoisotopeCheckArray[i, j] = true;
                                            peakNumberMono = i;
                                        }
                                    }
                                    if (i > j && peakNumberMono > j)
                                    {
                                        peakCounter = false;
                                        if (i - j > 1 && (charge != 1)) // verifying that intensities of peaks in between the putative isotopic peaks are less than that of both isotopes
                                        {
                                            for (var k = j + 1; k <= i - 1; ++k)
                                            {
                                                if (peptideLocalMax[k] > peptideLocalMax[i] || peptideLocalMax[k] > peptideLocalMax[j] || peakCounter)
                                                {
                                                    tofMonoisotopeCheckArray[i, j] = false;
                                                    peakCounter = true;
                                                }
                                                else
                                                {
                                                    peakNumberMono = j;
                                                    tofMonoisotopeCheckArray[i, j] = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            tofMonoisotopeCheckArray[i, j] = true;
                                            peakNumberMono = j;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // check if monoisotopic peak was ever found
            var tofMonoisotopeFound = false;
            for (var i = 0; i < 10; i++)
                for (var j = 0; j < 10; j++)
                {
                    if (tofMonoisotopeCheckArray[i, j])
                        tofMonoisotopeFound = true;
                }

            var tofMonoisotope = 0.0;
            if (peakNumberMono > 10 || !tofMonoisotopeFound)
                tofMonoisotope = 0;
            else if (tofMonoisotopeFound)
            {
                // if PeakPicking.APEX peak picking - return TOF monoisotope from the next line
                if (peakPicking == PeakPicking.APEX)
                {
                    tofMonoisotope = (double)posPeptideMaxMatches[peakNumberMono];
                }

                // if THREE POINT QUADRATIC peak picking - return TOF monoisotope after routine below
                if (peakPicking == PeakPicking.THREE_POINT_QUADRATIC)
                {
                    // three point quadratic declarations
                    var sumX4 = 0.0;
                    var sumX3 = 0.0;
                    var sumX2 = 0.0;
                    var sumX = 0.0;
                    var sumYx2 = 0.0;
                    var sumYx = 0.0;
                    var sumY = 0.0;
                    var posPeptideMax = 0;

                    for (var i = 0; i < 3; ++i)
                    {
                        //sum_x4 = sum_x4 + Math.Pow((double)((posPeptideMaxMatches[peak_number_mono]+sizeof(double)*(i-1))/sizeof(double) - fSpectrumNumber * NumElementsIn)*AcquisitionBin + offset_mariner,4);
                        sumX4 = sumX4 + Math.Pow(i, 4);
                        sumX3 = sumX3 + Math.Pow(i, 3);
                        sumX2 = sumX2 + Math.Pow(i, 2);
                        sumX = sumX + i;

                        posPeptideMax = posPeptideMaxMatches[peakNumberMono] + (i - 1);

                        sumYx2 = sumYx2 + sumIntensity2[posPeptideMax] * i * i;
                        sumYx = sumYx + sumIntensity2[posPeptideMax] * i;
                        sumY = sumY + sumIntensity2[posPeptideMax];
                    }

                    double[,] variable =
                    {
                        {sumX4, sumX3, sumX2, sumYx2},
                        {sumX3, sumX2, sumX, sumYx},
                        {sumX2, sumX, 3, sumY}
                    };

                    OnQuadraticLeastSquareFit(variable, out var coefficientA, out var coefficientB, out var coefficientC);

                    if (coefficientA != 0)
                        tofMonoisotope = -coefficientB / (2 * coefficientA);
                    else
                        tofMonoisotope = 0;

                    tofMonoisotope = tofMonoisotope + (arrivalTimeTof2[posPeptideMax - 2] * tofBinWidth);

                    //   if (tofMonoisotope > 110000)
                    //      tofMonoisotope = 0; //set TOF to zero if outside of m/z range
                }
            }
            //MessageBox.Show(this, "tofMonoisotope: "+tofMonoisotope.ToString());
            tofMonoisotope = tofMonoisotope / 1000;

            return tofMonoisotope;
        }

        void OnQuadraticLeastSquareFit(double[,] variable, out double coefficientA, out double coefficientB, out double coefficientC)
        {
            int i = 0, j = 0;

            coefficientA = 0;
            coefficientB = 0;
            coefficientC = 0;

            var determinant0 = Math.Pow(-1, i + j) * variable[i, j] * (variable[i + 1, j + 1] * variable[i + 2, j + 2] - variable[i + 2, j + 1] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 1) * variable[i, j + 1] * (variable[i + 1, j] * variable[i + 2, j + 2] - variable[i + 2, j] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 2) * variable[i, j + 2] * (variable[i + 1, j] * variable[i + 2, j + 1] - variable[i + 2, j] * variable[i + 1, j + 1]);

            var determinantA = Math.Pow(-1, i + j) * variable[i, j + 3] * (variable[i + 1, j + 1] * variable[i + 2, j + 2] - variable[i + 2, j + 1] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 1) * variable[i, j + 1] * (variable[i + 1, j + 3] * variable[i + 2, j + 2] - variable[i + 2, j + 3] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 2) * variable[i, j + 2] * (variable[i + 1, j + 3] * variable[i + 2, j + 1] - variable[i + 2, j + 3] * variable[i + 1, j + 1]);

            var determinantB = Math.Pow(-1, i + j) * variable[i, j] * (variable[i + 1, j + 3] * variable[i + 2, j + 2] - variable[i + 2, j + 3] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 1) * variable[i, j + 3] * (variable[i + 1, j] * variable[i + 2, j + 2] - variable[i + 2, j] * variable[i + 1, j + 2]) +
                               Math.Pow(-1, i + j + 2) * variable[i, j + 2] * (variable[i + 1, j] * variable[i + 2, j + 3] - variable[i + 2, j] * variable[i + 1, j + 3]);

            var determinantC = Math.Pow(-1, i + j) * variable[i, j] * (variable[i + 1, j + 1] * variable[i + 2, j + 3] - variable[i + 2, j + 1] * variable[i + 1, j + 3]) +
                               Math.Pow(-1, i + j + 1) * variable[i, j + 1] * (variable[i + 1, j] * variable[i + 2, j + 3] - variable[i + 2, j] * variable[i + 1, j + 3]) +
                               Math.Pow(-1, i + j + 2) * variable[i, j + 3] * (variable[i + 1, j] * variable[i + 2, j + 1] - variable[i + 2, j] * variable[i + 1, j + 1]);

            if (determinant0 != 0)
            {
                coefficientA = determinantA / determinant0;
                coefficientB = determinantB / determinant0;
                coefficientC = determinantC / determinant0;
            }
            else
            {
                coefficientA = 0;
                coefficientB = 0;
                coefficientC = 0;
            }
        }

        public void InitializeCalibrants(double binWidth, double calibrationSlope, double calibrationIntercept)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                using (Calibrants.SuppressChangeNotifications())
                {
                    Calibrants.Clear();
                    Calibrants.AddRange(CalibrantInfo.GetCalibrantSet(CurrentCalibrantSet, CustomCalibrantsFilePath));
                }
            });

            CalculateCalibrantExperimentalValues(binWidth, calibrationSlope, calibrationIntercept);
        }

        public void CalculateCalibrantExperimentalValues(double binWidth, double calibrationSlope, double calibrationIntercept)
        {
            tofBinWidth = binWidth;

            SetExperimentalCoefficients(calibrationSlope, calibrationIntercept);
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                foreach (var calibrant in Calibrants)
                {
                    try
                    {
                        calibrant.TOF = Math.Sqrt(calibrant.Mz) / ExperimentalSlope + ExperimentalIntercept;
                        calibrant.Bins = (calibrant.TOF) * 1000.0 / binWidth;

                        // MessageBox.Show(ExperimentalSlope.ToString() + " " + ExperimentalIntercept.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("CalculateCalibrantExperimentalValues:  " + calibrant.ToString() + "\n\n" + ex.ToString());
                    }
                }
            });
        }

        // this is all dealing with calibrants

        private void Recalculate2PointCalibration()
        {
            if (Ion1TOFBin.Equals(0) || Ion2TOFBin.Equals(0) || Ion1Mz.Equals(0) || Ion2Mz.Equals(0))
            {
                return;
            }

            // This function is only triggered by UI-bound property-changed events, so we don't need to wrap with with RxApp.MainThreadScheduler.Schedule().
            CalculatedIntercept = (Math.Sqrt(Ion2Mz) * Ion1TOFBin - Math.Sqrt(Ion1Mz) * Ion2TOFBin) / (Math.Sqrt(Ion2Mz) - Math.Sqrt(Ion1Mz));
            CalculatedSlope = Math.Sqrt(Ion1Mz) / (Ion1TOFBin - CalculatedIntercept);
        }

        public void SetExperimentalCoefficients(double slope, double intercept)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                ExperimentalSlope = slope;
                ExperimentalIntercept = intercept;
            });
        }

        // /////////////////////////////////////////////////////////////////////////////
        // logic developed by Matt Monroe 06/23/2011
        //
        public int GetCalibrantCountMatched()
        {
            var count = 0;

            for (var j = 0; j < Calibrants.Count - 1; ++j)
            {
                if ((Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                }
            }
            return count;
        }

        public int GetCalibrantCountValid()
        {
            var count = 0;

            for (var j = 0; j < Calibrants.Count - 1; ++j)
            {
                if ((Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 10.0))
                {
                    count++;
                }
            }
            return count;
        }

        public double GetAverageMassError()
        {
            var count = 0;
            var error = 0.0;

            for (var j = 0; j < Calibrants.Count - 1; ++j)
            {
                if ((Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                    error += Calibrants[j].ErrorPPM;
                }
            }

            return error / (double)count;
        }

        public double GetAverageAbsoluteValueMassError()
        {
            var count = 0;
            var error = 0.0;

            for (var j = 0; j < Calibrants.Count - 1; ++j)
            {
                if ((Calibrants[j].Enabled) && (Calibrants[j].TOFExperimental > 0) && (Math.Abs(Calibrants[j].ErrorPPM) < 50000.0))
                {
                    count++;
                    error += Math.Abs(Calibrants[j].ErrorPPM);
                }
            }

            return error / (double)count;
        }

        public int DisableCalibrantMaxPPMError()
        {
            var countCalibrants = 0;
            var errorMax = 0.0;
            var indexMax = 0;

            for (var j = 0; j < Calibrants.Count - 1; ++j)
            {
                if (Calibrants[j].Enabled)
                {
                    countCalibrants++;
                    if (errorMax < Math.Abs(Calibrants[j].ErrorPPM))
                    {
                        errorMax = Math.Abs(Calibrants[j].ErrorPPM);
                        indexMax = j;
                    }
                }
            }

            countCalibrants--;

            RxApp.MainThreadScheduler.Schedule(() => Calibrants[indexMax].Enabled = false);

            return countCalibrants;
        }

        private void BrowseForCalibrantsFile()
        {
            var fileBrowser = new CommonOpenFileDialog
            {
                Title = "Select Custom Calibrants File",
                Filters = { new CommonFileDialogFilter("Tab-separated", "*.tsv;*.txt"), new CommonFileDialogFilter("Comma-separated", "*.csv") }
            };

            if (!string.IsNullOrWhiteSpace(CustomCalibrantsFilePath))
            {
                var dir = Path.GetDirectoryName(CustomCalibrantsFilePath);
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = ".";
                }

                if (Directory.Exists(dir))
                {
                    fileBrowser.InitialDirectory = Path.GetDirectoryName(CustomCalibrantsFilePath);
                }
            }

            var result = fileBrowser.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                // Just set the backing variable to something that is not 'custom', to allow the normal handling to handle everything else.
                currentCalibrantSet = CalibrantSet.PolyalaninesNegative;
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    CustomCalibrantsFilePath = fileBrowser.FileName;
                    CurrentCalibrantSet = CalibrantSet.Custom;
                });
            }
        }

        private void LoadCalibrantSet(CalibrantSet calibrantSet)
        {
            var calibrants = CalibrantInfo.GetCalibrantSet(calibrantSet, CustomCalibrantsFilePath);

            using (Calibrants.SuppressChangeNotifications())
            {
                Calibrants.Clear();
                Calibrants.AddRange(calibrants);
            }

            // Set the theoretical experimental values
            CalculateCalibrantExperimentalValues(tofBinWidth, ExperimentalSlope, ExperimentalIntercept);
        }

        private void DecodeDirectoryBrowse()
        {
            var folderBrowser = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Select Decoded UIMF Experiment Folder" };
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
            var folderBrowser = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Select Compressed 1GHz UIMF Experiment Folder" };
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
            uimfReader.UpdateCalibrationCoefficients(uimfReader.CurrentFrameIndex, CalculatedSlope,
                CalculatedIntercept);

            OnCalibrationChanged();

            InitializeCalibrants(uimfReader.UimfGlobalParams.BinWidth, CalculatedSlope, CalculatedIntercept);
        }

        private void ApplyCalibrationAllFrames()
        {
            //MessageBox.Show((Convert.ToDouble(tb_CalA.Text) * 10000.0).ToString() + "  " + pnl_postProcessing.ExperimentalSlope.ToString());
            uimfReader.UpdateAllCalibrationCoefficients(ExperimentalSlope, ExperimentalIntercept);

            OnCalibrationChanged();

            InitializeCalibrants(uimfReader.UimfGlobalParams.BinWidth, ExperimentalSlope, ExperimentalIntercept);
        }

        private void CalibrateFrames()
        {
            var flagAutoCalibrate = false;

            CalibrateFrame(uimfReader.CurrentFrameIndex, out var slope, out var intercept, out _);

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
                uimfReader.MzCalibration.K = slope / 10000.0;
                uimfReader.MzCalibration.T0 = intercept * 10000.0;
            }

            if (flagAutoCalibrate)
                uimfReader.UpdateAllCalibrationCoefficients(slope, intercept, flagAutoCalibrate);

            OnCalibrationChanged();
        }

        private void CalibrateFrame(int frameIndex, out double calibrationSlope, out double calibrationIntercept, out int totalCalibrantsMatched)
        {
            var maximumSpectrum = 0;

            var noiseRegion = 50;
            int compression;
            var maxSpectrum = new int[uimfReader.UimfGlobalParams.Bins];

            var slope = uimfReader.UimfFrameParams.CalibrationSlope;
            var intercept = uimfReader.UimfFrameParams.CalibrationIntercept;

            var calibrantCountMatched = 100;

            if (uimfReader.UimfGlobalParams.BinWidth.Equals(0.25))
                compression = 4;
            else if (uimfReader.UimfGlobalParams.BinWidth.Equals(0.50))
                compression = 4;
            else
                compression = 1;

            calibrationSlope = -1.0;
            calibrationIntercept = -1.0;
            totalCalibrantsMatched = 0;

            var summedSpectrum = new double[uimfReader.UimfGlobalParams.Bins / compression];
            var flagAboveNoise = new bool[uimfReader.UimfGlobalParams.Bins / compression];

            if (calibrantCountMatched > 4)
            {
                // clear arrays
                int i;
                for (i = 0; i < uimfReader.UimfGlobalParams.Bins / compression; i++)
                {
                    flagAboveNoise[i] = false;
                    maxSpectrum[i] = 0;
                    summedSpectrum[i] = 0;
                    maxSpectrum[i] = 0;
                }

                var bins = uimfReader.GetSumScans(uimfReader.ArrayFrameNum[frameIndex], 0, uimfReader.UimfFrameParams.Scans);

                int j;
                for (j = 0; j < bins.Length; j++)
                {
                    summedSpectrum[j / compression] += bins[j];

                    if (maxSpectrum[j / compression] < summedSpectrum[j / compression])
                    {
                        maxSpectrum[j / compression] = (int)summedSpectrum[j / compression];

                        if (maximumSpectrum < summedSpectrum[j / compression])
                            maximumSpectrum = (int)summedSpectrum[j / compression];
                    }
                }

                // determine noise level and filter summed spectrum
                for (j = noiseRegion / 2; (j < (uimfReader.UimfGlobalParams.Bins / compression) - noiseRegion); j++)
                {
                    // get the total intensity and divide by the number of peaks
                    var noisePeaks = 0;
                    var noiseIntensity = 0;
                    int k;
                    for (k = j - (noiseRegion / 2); k < j + (noiseRegion / 2); k++)
                    {
                        if (maxSpectrum[k] > 0)
                        {
                            noiseIntensity += maxSpectrum[k];
                            noisePeaks++;
                        }
                    }

                    if (noisePeaks > 0)
                    {
                        if (maxSpectrum[j] > noiseIntensity / noisePeaks) // the average level...
                            flagAboveNoise[j] = true;
                    }
                    else
                        flagAboveNoise[j] = false;
                }

                // calculate size of the array of filtered sum spectrum for calibration routine
                var aboveNoiseBins = 0;
                var addedZeros = 0;
                for (i = 1; i < uimfReader.UimfGlobalParams.Bins / compression; i++)
                {
                    if (flagAboveNoise[i])
                    {
                        aboveNoiseBins++;
                    }
                    else if (flagAboveNoise[i - 1])
                    {
                        addedZeros += 2;
                    }
                }

                // compress the arrays to nonzero with greater than noiselevel;
                var compressedBins = 0;
                var nonzeroBins = new double[aboveNoiseBins + addedZeros];
                var nonzeroIntensities = new double[aboveNoiseBins + addedZeros];
                for (i = 0; (i < (uimfReader.UimfGlobalParams.Bins / compression) - 1) && (compressedBins < aboveNoiseBins + addedZeros); i++)
                {
                    if (flagAboveNoise[i])
                    {
                        nonzeroBins[compressedBins] = i;
                        nonzeroIntensities[compressedBins] = summedSpectrum[i];
                        compressedBins++;
                    }
                    else if ((i > 0) && ((flagAboveNoise[i - 1] || flagAboveNoise[i + 1])))
                    {
                        nonzeroBins[compressedBins] = i;
                        nonzeroIntensities[compressedBins] = 0;
                        compressedBins++;
                    }
                }

                // pass arrays into calibration routine
                CalibrateFrame(summedSpectrum, nonzeroIntensities, nonzeroBins,
                    uimfReader.UimfGlobalParams.BinWidth * (double)compression, uimfReader.UimfGlobalParams.Bins / compression,
                    uimfReader.UimfFrameParams.Scans, slope, intercept);

                calibrantCountMatched = GetCalibrantCountMatched();
                var calibrantCountValid = GetCalibrantCountValid();
                GetAverageAbsoluteValueMassError();
                GetAverageMassError();

                if (calibrantCountMatched == calibrantCountValid)
                {
                    // done, slope and intercept acceptable
                    calibrationSlope = ExperimentalSlope;
                    calibrationIntercept = ExperimentalIntercept;
                    totalCalibrantsMatched = calibrantCountMatched;
                    //break;
                }
                else if (calibrantCountMatched > 4)
                    DisableCalibrantMaxPPMError();
            }

            uimfReader.ClearFrameParametersCache();
        }

        #endregion

        #region 4GHz to 1GHz compression

        /// <summary>
        /// Compress 4GHz Data to 1GHz
        /// </summary>
        private void Compress4GHzTo1GHzUIMF()
        {
            var gp = uimfReader.GetGlobalParams();
            int currentFrame;
            var currentIntensities = new int[gp.Bins / 4];

            var listNzValues = new List<Tuple<int, int>>();

            var stopWatch = new Stopwatch();

            // create new UIMF File
            var uimfFilename = Path.Combine(CompressSaveDirectory, CompressSaveFilename + "_1GHz.UIMF");
            if (File.Exists(uimfFilename))
            {
                if (MessageBox.Show("File Exists", "File Exists, Replace?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    File.Delete(uimfFilename);
                else
                    return;
            }

            var uimfWriter = new DataWriter(uimfFilename);
            uimfWriter.CreateTables(null);

            gp.AddUpdateValue(GlobalParamKeyType.BinWidth, 1);
            gp.AddUpdateValue(GlobalParamKeyType.Bins, gp.Bins / 4);
            uimfWriter.InsertGlobal(gp);

            var maxTime = 0;

            var cancelToken = new CancellationTokenSource();
            var progress = new ProgressViewModel(gp.NumFrames, cancelToken);
            var progressWindow = new ProgressWindow { DataContext = progress, ShowActivated = true };
            progressWindow.Show();

            for (currentFrame = 0; ((currentFrame < (int)uimfReader.CurrentFrameType) && !cancelToken.IsCancellationRequested); currentFrame++)
            {
                progress.SetProgress(currentFrame, stopWatch.ElapsedMilliseconds);

                stopWatch.Reset();
                stopWatch.Start();

                var fp = uimfReader.GetFrameParams(currentFrame);
                uimfWriter.InsertFrame(currentFrame, fp);

                int i;
                for (i = 0; i < fp.Scans; i++)
                {
                    int j;
                    for (j = 0; j < gp.Bins; j++)
                    {
                        currentIntensities[j] = 0;
                    }

                    uimfReader.GetSpectrum(uimfReader.ArrayFrameNum[currentFrame], uimfReader.FrameTypeDict[uimfReader.ArrayFrameNum[currentFrame]], i, out var arrayBins, out var arrayIntensity);

                    for (j = 0; j < arrayBins.Length; j++)
                        currentIntensities[(int)arrayBins[j] / 4] += arrayIntensity[j];

                    listNzValues.Clear();
                    for (j = 0; j < gp.Bins; j++)
                    {
                        if (currentIntensities[j] > 0)
                        {
                            listNzValues.Add(new Tuple<int, int>(j, currentIntensities[j]));
                        }
                    }

                    uimfWriter.InsertScan(currentFrame, fp, i, listNzValues, 1, gp.GetValueInt32(GlobalParamKeyType.TimeOffset) / 4);
                }

                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > maxTime)
                {
                    maxTime = (int)stopWatch.ElapsedMilliseconds;
                    progress.AddStatus("Max Time: Frame " + currentFrame.ToString() + " ..... " + maxTime.ToString() + " msec", false);
                }
            }

            if (progress.Success)
            {
                progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
            }

            uimfWriter.Dispose();
        }

        #endregion

    }
}
