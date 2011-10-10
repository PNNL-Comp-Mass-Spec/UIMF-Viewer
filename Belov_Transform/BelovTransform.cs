using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Belov_Transform
{
    public class BelovTransform
    {
        //==DECLARATIONS OF GLOBAL VARIABLES AND POINTERS TO GLOBAL BUFFERS===============/

        Matrix matrix;
        Matrix inv_mat;

        bool status = false;

        Calibration oCalibration;

        int[] PRS;
        int[] PRS_rotated;
        double[] PRS_weighed_folded;

        double dBinResolution;
        double dTimeOffset;
        double dTimeScale;
        double dIndexConverter;
        double dSlope;
        double dIntercept;

        int FOLDING_FACTOR, TOFperBin, BitNumber;
        int NumberOfElementsPerFrame;

        int[][] IntensityTransformed;
        int[][] IntensityRaw;
        int[][] ArrivalTime;

        float[][] intensity_inverted;
        float[] inverted_array_TOF_bins;

        bool PRS_ENABLED, NORMAL_MATRIX, INVERSE_TRANSFORM, INVERSE_MATRIX;
        int NumberOfElementsForTransform, NumberOfSpectraForTransform;
        int N_SCANS_PER_MODULATION_BIN, N_BINS_EXTENDED_PRS, N_BINS_FOLDED_PRS;

        // This is the constructor of a class that has been exported.
        // see BelovTransform.h for the class definition
        public BelovTransform()
        {
            PRS_ENABLED = false;
            NORMAL_MATRIX = false;
            INVERSE_TRANSFORM = false;
            INVERSE_MATRIX = false;

            return;
        }

        //===========================================API functions=================================================================================//
        public bool OnInputCalibrationParameters(string[] AsCalibrantName, int iNumberOfCalibrants,
                                                             double[] AdCalibrantMZ, int[] AiCalibrantCharge,
                                                             int iNumberOfSpectra, int iNumberOfBins, double dTOFBin_Width,
                                                             double[] AdTOFOffset, double dTime_Offset,
                                                             int iMinNumberOfIons, double dIndex_Converter, double dTime_Scale,
                                                             int[] AiScanLength, int[][] AiArrivalTime, int[][] AiIntensity,
                                                             double dSlopeExternal, double dInterceptExternal)
        {
            //define parameters 
            this.dTimeOffset = dTime_Offset;
            this.dTimeScale = dTime_Scale;
            this.dIndexConverter = dIndex_Converter;
            this.dBinResolution = dTOFBin_Width;
            this.dSlope = dSlopeExternal;
            this.dIntercept = dInterceptExternal;

            oCalibration.dBinResolution = dTOFBin_Width;
            oCalibration.dTimeOffset = dTime_Offset;
            oCalibration.dTimeScale = dTime_Scale;
            oCalibration.iNumberOfSpectraForCalibration = iNumberOfSpectra;
            oCalibration.iNumberOfElementsForCalibration = iNumberOfBins;

            if (!(status = this.OnGenerateArraysForInverseTransform(iNumberOfSpectra, iNumberOfBins, this.dBinResolution,
                                                            AdTOFOffset, iMinNumberOfIons,
                                                            AiScanLength, AiArrivalTime, AiIntensity)))
                return status;

            oCalibration.iNumberOfElementsForCalibration = this.NumberOfElementsForTransform;

            if (!(status = oCalibration.OnGenerateArraysForCalibration(this.ArrivalTime, this.IntensityRaw)))
                return status;

            oCalibration.AllocateCalibrationArrays(iNumberOfCalibrants);
            oCalibration.iNumberOfAllCalibrants = iNumberOfCalibrants;

            status = oCalibration.OnInternalCalibration(AsCalibrantName, iNumberOfCalibrants, AdCalibrantMZ,
                                            AiCalibrantCharge, iNumberOfBins,
                                            oCalibration.iCalibrantArrivalTime, oCalibration.iCalibrantSummedIntensity,
                                            this.dSlope, this.dIntercept);


            return status;
        }

        public bool OnRetrieveCalibrationCoefficients(int piNumberOfCalibrants, string[] AsCalibrantName,
                                                                  double[] AdCalibrantMZ, int[] AiCalibrantCharge, double[] AdError_PPM,
                                                                  double pdSlope, double pdIntercept)
        {
            piNumberOfCalibrants = oCalibration.iNumberOfNonZeroCalibrants;

            int i = 0;

            for (i = 0; i < oCalibration.iNumberOfNonZeroCalibrants; ++i)
            {
                oCalibration.AsCalibrantName[i] = String.Copy(AsCalibrantName[i]);
                AdCalibrantMZ[i] = oCalibration.AdCalibrantMZ[i];
                AiCalibrantCharge[i] = oCalibration.AiCalibrantCharge[i];
                AdError_PPM[i] = oCalibration.AdError_PPM[i];
            }

            pdSlope = oCalibration.dSlope_internal;
            pdIntercept = oCalibration.dIntercept_internal;

            if (oCalibration.dSlope_internal == 0 || oCalibration.dIntercept_internal == 0)
                status = false;
            else 
                status = true;

            return status;
        }

        public bool OnInputRawData(string sSequenceName, string sInstrumentName,
                                               int iNumberOfSpectra, int iNumberOfElements,
                                               double dIndex_Converter, double dTime_Scale,
                                               double dTOFBinWidth, double[] AdTOFOffset, int iMinNumberOfIons,
                                               int[] AiScanLength, int[][] AiArrivalTime, int[][] AiIntensity)
        {
            this.dTimeOffset = AdTOFOffset[1];
            this.dTimeScale = dTime_Scale;
            this.dIndexConverter = dIndex_Converter;
            this.dBinResolution = dTOFBinWidth;

            this.OnPRSGenerate(sSequenceName, sInstrumentName);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!(status = this.OnGenerateArraysForInverseTransform(iNumberOfSpectra, iNumberOfElements, this.dBinResolution,
                                                            AdTOFOffset, iMinNumberOfIons,
                                                            AiScanLength, AiArrivalTime, AiIntensity)))
                return status;

            this.NumberOfSpectraForTransform = iNumberOfSpectra;

            status = this.OnInverseTransform(this.BitNumber, this.NumberOfElementsForTransform,
                                                        this.NumberOfSpectraForTransform,
                                                        AdTOFOffset[1]);

            return status;
        }

        public bool OnRetrieveTransformParameters(out int piNumberOfElementsPerFrame, out int piNumberOfSpectra, out int piNumberOfElementsPerSpectrum)
        {
            piNumberOfElementsPerFrame = this.NumberOfElementsForTransform * this.NumberOfSpectraForTransform;
            piNumberOfSpectra = this.NumberOfSpectraForTransform;
            piNumberOfElementsPerSpectrum = this.NumberOfElementsForTransform;

            if (piNumberOfElementsPerFrame == 0)
                status = false;
            else
                status = true;

            return status;
        }


        public bool OnRetrieveTransformedData(int[][] AiArrivalTime, int[][] AiIntensityTransformed)
        {

            for (int i = 0; i < this.NumberOfSpectraForTransform; ++i)
                for (int j = 0; j < this.NumberOfElementsForTransform; j++)
                {
                    AiArrivalTime[i][j] = this.ArrivalTime[i][j];
                    AiIntensityTransformed[i][j] = this.IntensityTransformed[i][j];
                }

            if (AiArrivalTime != null) //**AiArrivalTime > 0 )
                status = true;
            else
                status = false;

            return status;
        }

        //====================================END OF API FUNCTIONS================================================================//

        //=============================local DLL functions========================================================================//

        //----------------PseudoRandomSequenceGenerator---------------------------------------------------------------------------//

        bool OnPRSGenerate(string sSequenceName, string sInstrumentName) 
    {
	    // TODO: Add your control notification handler code here
	    int i, length, wrap_around, pivot_point = 0;
	    double RescalingFactor;
        int[] buffer = new int[1022];
        int[] buffer1 = new int[1022];

	    PRS_ENABLED = false;
    	
	    if(String.Compare(sInstrumentName, "TOF") == 0)
	    {
		    if(String.Compare(sSequenceName, "4bit_40OS.txt") == 0 || String.Compare(sSequenceName, "4Bit_40OS.txt") == 0)
            {
                BitNumber = 4; 
                TOFperBin = 4;
            }
		    if(String.Compare(sSequenceName, "5bit_20OS.txt") == 0 || String.Compare(sSequenceName, "5Bit_20OS.txt") == 0)
            {
                BitNumber = 5;
                TOFperBin = 2; 
            }
		    if(String.Compare(sSequenceName, "6bit_10OS.txt") == 0 || String.Compare(sSequenceName, "6Bit_10OS.txt") == 0) 
            {
                BitNumber = 6; 
                TOFperBin = 1;
            }

		    FOLDING_FACTOR = 10;
	    }
	    else if(String.Compare(sInstrumentName, "QTOF") == 0)
	    {	
		    if(String.Compare(sSequenceName, "4bit_24OS.txt") == 0 || String.Compare(sSequenceName, "4Bit_24OS.txt") == 0)
            {
                BitNumber = 4;
                TOFperBin = 4;
            }		
		    if(String.Compare(sSequenceName, "5bit_12OS.txt") == 0 || String.Compare(sSequenceName, "5Bit_12OS.txt") == 0)
            {
                BitNumber = 5; 
                TOFperBin = 2;
            }
		    if(String.Compare(sSequenceName, "6bit_6OS.txt") == 0 || String.Compare(sSequenceName, "6Bit_6OS.txt") == 0)
            {
                BitNumber = 6; 
                TOFperBin = 1;
            }

            //MessageBox.Show("folding: " + sSequenceName + ", " + sSequenceName +" "+ BitNumber.ToString() + ", " + TOFperBin.ToString());

		    FOLDING_FACTOR = 6;
	    }
	    else 
        {
            status = false; 
            return status;
        }

	    length = BitNumber;
    		
	    PRS = new int[(int)(Math.Pow(2.0, length)-1)*TOFperBin]; //pseudo-random sequence array
	    PRS_rotated = new int[(int)(Math.Pow(2.0, length)-1)*TOFperBin]; //pseudo-random sequence array
	    PRS_weighed_folded = new double[(int)(Math.Pow(2.0, length)-1)*TOFperBin]; //folded weighed pseudo- random sequence
    		
	    if(length == 4)
	    {
		    //primitive polynomial : x^4 + x + 1
            for (i = 0; i < 256; i++)
                buffer[i] = 0;
		    buffer[0] = 1;

		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-2] == 0) || (buffer[length-1] == 1) && (buffer[length-2] == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;

			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];

			    buffer[length-4] = wrap_around;
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
    			
		    }
	    }

	    if(length == 5)
	    {
		    //primitive polynomial : x^5 + x^2 + 1
            for (i = 0; i < 256; i++)
                buffer[i] = 0;
            buffer[0] = 1;

		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-3] == 0) || (buffer[length-1] == 1) && (buffer[length-3] == 1))
			    wrap_around = 0;
			    else wrap_around = 1;

			    buffer1[length-4] = buffer[length-5];
			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];


			    buffer[length-5] = wrap_around;
			    buffer[length-4] = buffer1[length-4];
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
		    }
	    }

	    if(length == 6)
	    {
		    //primitive polynomial : x^6 + x + 1
            for (i = 0; i < 256; i++)
                buffer[i] = 0;
            buffer[0] = 1;

		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-2] == 0) || (buffer[length-1] == 1) && (buffer[length-2] == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;

			    buffer1[length-5] = buffer[length-6];
			    buffer1[length-4] = buffer[length-5];
			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];


			    buffer[length-6] = wrap_around;
			    buffer[length-5] = buffer1[length-5];
			    buffer[length-4] = buffer1[length-4];
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
		    }
	    }

	    if(length == 8)
	    {
		    // primitive polynomial : x^8 + x^6 + x^5 + x + 1
            for (i = 0; i < 256; i++)
                buffer[i] = 0;
            buffer[0] = 1;

		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-2] == 0) || (buffer[length-1] == 1) && (buffer[length-2] == 1))
			        wrap_around = 0; 
			    else 
                    wrap_around = 1;
			    if((buffer[length-6] == 0) && (wrap_around == 0) || (buffer[length-6] == 1) && (wrap_around == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;
			    if ((buffer[length-7] == 0) && (wrap_around == 0) || (buffer[length-7] == 1) && (wrap_around == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;

			    buffer1[length-8] = wrap_around; 
			    buffer1[length-7] = buffer[length-8];			
			    buffer1[length-6] = buffer[length-7];
			    buffer1[length-5] = buffer[length-6];
			    buffer1[length-4] = buffer[length-5];
			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];

			    buffer[length-8] = buffer1[length-8];
			    buffer[length-7] = buffer1[length-7];
			    buffer[length-6] = buffer1[length-6];
			    buffer[length-5] = buffer1[length-5];
			    buffer[length-4] = buffer1[length-4];
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
		    }
	    }

	    if(length == 9)
	    {
		    // primitive polynomial : x^9 + x^4 + 1
		    int[] initial_set = {1,1,0,1,0,0,1,0,0};
    		
		    for(i = 0; i <9; i++)
			    buffer[i] = initial_set[i];
    		
		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-5] == 0) || (buffer[length-1] == 1) && (buffer[length-5] == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;

			    buffer1[length-9] = wrap_around;
			    buffer1[length-8] = buffer[length-9];
			    buffer1[length-7] = buffer[length-8];			
			    buffer1[length-6] = buffer[length-7];
			    buffer1[length-5] = buffer[length-6];
			    buffer1[length-4] = buffer[length-5];
			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];

			    buffer[length-9] = buffer1[length-9];
			    buffer[length-8] = buffer1[length-8];
			    buffer[length-7] = buffer1[length-7];
			    buffer[length-6] = buffer1[length-6];
			    buffer[length-5] = buffer1[length-5];
			    buffer[length-4] = buffer1[length-4];
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
		    }
	    }

	    if(length == 10)
	    {
		    // primitive polynomial : x^10 + x^3 + 1
		    int[] initial_set = {0,1,0,0,0,0,1,0,0,0};
    		
		    for(i = 0; i <10; i++)
			    buffer[i] = initial_set[i];
    		
		    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    {
			    PRS[i] = buffer[length-1];

			    if ((buffer[length-1] == 0) && (buffer[length-4] == 0) || (buffer[length-1] == 1) && (buffer[length-4] == 1))
			        wrap_around = 0;
			    else 
                    wrap_around = 1;

			    buffer1[length-10] = wrap_around;
			    buffer1[length-9] = buffer[length-10];
			    buffer1[length-8] = buffer[length-9];
			    buffer1[length-7] = buffer[length-8];			
			    buffer1[length-6] = buffer[length-7];
			    buffer1[length-5] = buffer[length-6];
			    buffer1[length-4] = buffer[length-5];
			    buffer1[length-3] = buffer[length-4];
			    buffer1[length-2] = buffer[length-3];
			    buffer1[length-1] = buffer[length-2];

			    buffer[length-10] = buffer1[length-10];
			    buffer[length-9] = buffer1[length-9];
			    buffer[length-8] = buffer1[length-8];
			    buffer[length-7] = buffer1[length-7];
			    buffer[length-6] = buffer1[length-6];
			    buffer[length-5] = buffer1[length-5];
			    buffer[length-4] = buffer1[length-4];
			    buffer[length-3] = buffer1[length-3];
			    buffer[length-2] = buffer1[length-2];
			    buffer[length-1] = buffer1[length-1];
		    }
	    }

	    //rotate sequence
    	
	    if(BitNumber == 6) 
            pivot_point = 57;
	    //comparing 4Peptides_1uM_6_5Torr_1bit_4ms_500uS_0000.Accum_1.IMF
	    //& 4Peptides_1uM_6_5Torr_6bit_500uS_0000.Accum_1.IMF

	    if(BitNumber == 5)
            pivot_point = 4;
	    //comparing 4Peptides_1uM_6_5Torr_1bit_4ms_500uS_0000.Accum_1.IMF
	    //& 4Peptides_1uM_6_5Torr_5bit_500uS_0000.Accum_1.IMF

	    if(BitNumber == 4)
            pivot_point = 3; 
	    //matched single averaging; 
	    //comparing 4Peptides_1uM_6_5Torr_1bit_4ms_500uS_0000.Accum_1.IMF 
	    // & 4Peptides_1uM_6_5Torr_4bit_500uS_0000.Accum_1.IMF
    	
	    int i_max = (int)Math.Pow(2.0,length)-1;
	    for(i = 0; i < i_max; i++)
	    {
		    if(i < Math.Pow(2.0,length) - 1 - pivot_point) 
			    PRS_rotated[i] = PRS[pivot_point+i];
		    else
			    PRS_rotated[i] = PRS[i - i_max + pivot_point];
	    }

	    for(i = 0; i < i_max; i++)
		    PRS[i] = PRS_rotated[i];

	    RescalingFactor = 2/Math.Pow(2.0,BitNumber);

	    for(i = 0; i < Math.Pow(2.0,length)-1; i++)
		    if(PRS[i] != 0)
			    PRS_weighed_folded[i] = PRS[i] * RescalingFactor;

	    PRS_ENABLED = true;
	    status = true;

	    return status;
    }

#if BELOV
    // belov
    bool OnGenerateArraysForInverseTransform(int NumberOfSpectra, int NumberOfTOFBins, double TOFBinWidth, 
										     double* TOFOffset, int MinNumberOfIons, 
										     int* ScanLength, int** ArrivalTimeInput, int** IntensityInput)
    {
	    NumberOfElementsForTransform = 0;
	    int i,j,k;

	    int* non_zero_counter = null;
	    int** AccumArray = null;
    	
	    AccumArray = (int**)calloc(NumberOfSpectra, sizeof(int*));
	    for(i = 0; i < NumberOfSpectra; i++)
	    {
		    AccumArray[i] = (int*)calloc(NumberOfTOFBins, sizeof(int));
		    for(j = 0; j < NumberOfTOFBins; j++)
		    {
			    AccumArray[i][j] = 0;
		    }
	    }

	    k = 0;

	    for(i =0; i < NumberOfSpectra; i++)
	    {
		    for(j = 0; j < ScanLength[i]; j++)
		    {
		        k = (int) (ArrivalTimeInput[i][j]/dIndexConverter);
		        AccumArray[i][k] = IntensityInput[i][j];							
            }
	    }

	    //scan the IMS frame across TOF domain (same TOF bins) and find 
	    //different non-zero entries that showed up at least once in any spectrum
	    non_zero_counter = (int*)calloc(NumberOfTOFBins, sizeof(int));
	    for(j = 0; j < NumberOfTOFBins; ++j)
	    {
		    for(i= 0; i < NumberOfSpectra; ++i)
		    {
			    if(AccumArray[i][j] > MinNumberOfIons) //AccumArray[i][j] > 0 for very low intensity data
			    {	
				    non_zero_counter[j] = 1;
				    NumberOfElementsForTransform++;
				    break;
			    }
		    }
	    }
    	
	    //allocate memory for all spectra with non-zero entries
	    ArrivalTime = (int**)calloc(NumberOfSpectra, sizeof(int*));
	    for(i = 0; i < NumberOfSpectra; i++)
	    {
		    ArrivalTime[i] = (int*)calloc(NumberOfElementsForTransform, sizeof(int));
		    for(j = 0; j < NumberOfElementsForTransform; j++)
		    {
			    ArrivalTime[i][j] = 0.0;
		    }
	    }

	    IntensityRaw = (int**)calloc(NumberOfSpectra, sizeof(int*));
	    for(i = 0; i < NumberOfSpectra; i++)
	    {
		    IntensityRaw[i] = (int*)calloc(NumberOfElementsForTransform, sizeof(int));
		    for(j = 0; j < NumberOfElementsForTransform; j++)
		    {
			    IntensityRaw[i][j] = 0;
		    }
	    }
    	
	    //run analysis second time and populate ion_intensity and arrival_time buffers
	    //then deallocate memory taken by AccumArray[i][j] (j stands for all TOF bins) 
        k = 0;
	    for(j = 0; j < NumberOfTOFBins; ++j)
	    {
		    if(*(non_zero_counter+j) ==1 )
		    {
			    for(i= 0; i < NumberOfSpectra; ++i)
			    {
				    IntensityRaw[i][k] = AccumArray[i][j];
				    ArrivalTime[i][k] = (int)(j*Math.Pow(2.0,TOFBinWidth)/dTimeScale + TOFOffset[i]); // arrival time in ns
   			    }
    				
			    k++; // k increases up to NumberOfElementsForTransform
		    }
	    }

        // --------------------------------------------------------
        FILE *fp;
        char stuff[25];
        fp = fopen("c:\\Develop\\belov\\testbelov_accum.csv","w"); // open for writing 
        fprintf(fp, "Intensity, TOF, Spectrum, element, ,%lf, %lf, %lf, %lf, %d\n",TOFBinWidth ,dTimeScale , TOFOffset[0], TOFOffset[1], NumberOfElementsForTransform);
               
        k = 0;
	    for(j = 0; j < NumberOfTOFBins; ++j)
	    {
		    if(*(non_zero_counter+j) ==1 )
		    {
			    for(i= 0; i < NumberOfSpectra; ++i)
			    {
				    if(IntensityRaw[i][k] > MinNumberOfIons) 
                        fprintf(fp,"%d, %d, %d, %d\n", IntensityRaw[i][k], ArrivalTime[i][k], i, k);
                }
   			    k++; // k increases up to NumberOfElementsForTransform
            }
        }
        fflush(fp);
        fclose(fp);
        // --------------------------------------------------------

	    if(k != NumberOfElementsForTransform)
	    {
		    status = false;
	    }
	    else status = true;

	    if(non_zero_counter != null)
	    {
		    free(non_zero_counter);
		    non_zero_counter = null;
	    }
	    if(AccumArray != null)
	    {
		    for (i=0; i < NumberOfSpectra; i++)
            {
                free(AccumArray[i]);
                AccumArray[i] = null; 
            }
		    free(AccumArray);
		    AccumArray = null;
	    }
    		
	    return status;
    }
#else
        // bill
        bool OnGenerateArraysForInverseTransform(int NumberOfSpectra, int NumberOfTOFBins, double TOFBinWidth,
                                                 double[] TOFOffset, int MinNumberOfIons,
                                                 int[] ScanLength, int[][] ArrivalTimeInput, int[][] IntensityInput)
        {
            NumberOfElementsForTransform = 0;
            int i, j, k;
            int hold_value;

            int[] non_zero_counter = new int[NumberOfTOFBins];

            for (i = 0; i < NumberOfTOFBins; i++)
                non_zero_counter[i] = -1;

            try
            {
                //scan the IMS frame across TOF domain (same TOF bins) and find 
                //different non-zero entries that showed up at least once in any spectrum
                for (i = 0; i < NumberOfSpectra; i++)
                    for (j = 0; j < ScanLength[i]; j++)
                        if (IntensityInput[i][j] > MinNumberOfIons)
                        {
                            k = (int)(ArrivalTimeInput[i][j] / dIndexConverter);
                            if (k < NumberOfTOFBins)
                                non_zero_counter[k] = 1;
                        }
            }
            catch (Exception ex)
            {
                MessageBox.Show("1: " + ex.ToString());
            }
            NumberOfElementsForTransform = 0;
            for (j = 0; j < NumberOfTOFBins; j++)
                if (non_zero_counter[j] == 1)
                    non_zero_counter[j] = NumberOfElementsForTransform++;

            try
            {
                //allocate memory for all spectra with non-zero entries
                ArrivalTime = new int[NumberOfSpectra][];
                IntensityRaw = new int[NumberOfSpectra][];
                for (i = 0; i < NumberOfSpectra; i++)
                {
                    ArrivalTime[i] = new int[NumberOfElementsForTransform];
                    IntensityRaw[i] = new int[NumberOfElementsForTransform];

#if false
                for (j = 0; j < NumberOfElementsForTransform; j++)
                {
                    ArrivalTime[i][j] = 0;
                    IntensityRaw[i][j] = 0;
                }
#endif
                }

                //populate arrival_time buffers
                k = 0;
                for (j = 0; j < NumberOfTOFBins; ++j)
                    if (non_zero_counter[j] >= 0)
                    {
                        hold_value = (int)(j * Math.Pow(2.0, TOFBinWidth) / dTimeScale);
                        for (i = 0; i < NumberOfSpectra; ++i)
                            ArrivalTime[i][k] = (int)(hold_value + TOFOffset[i]); // arrival time in ns

                        k++; // k increases up to NumberOfElementsForTransform
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show("2: " + ex.ToString());
            }

            try
            {
                // run analysis second time and populate ion_intensity and arrival_time buffers
                // then deallocate memory taken by AccumArray[i][j] (j stands for all TOF bins)
                for (i = 0; (i < NumberOfSpectra); ++i)
                {
                    if (i > ScanLength.Length)
                        MessageBox.Show(NumberOfSpectra.ToString()+": "+i.ToString() + " < " + ScanLength.Length.ToString());
                    for (j = 0; j < ScanLength[i]; ++j)
                    {
                        if (IntensityRaw.Length == 0)
                            continue; 

                        k = (int)(ArrivalTimeInput[i][j] / dIndexConverter);
                        
                        if (k >= non_zero_counter.Length)
                            MessageBox.Show("k=" + k.ToString() + ", non_zero_counter.Length=" + non_zero_counter.Length.ToString());

                        if (IntensityRaw.Length < i)
                            MessageBox.Show("IntensityRaw.Length " + IntensityRaw.Length.ToString() +", "+ i.ToString());

                        if (non_zero_counter[k] >= IntensityRaw[i].Length)
                            MessageBox.Show("intensity raw");


                        if (non_zero_counter[k] >= 0)
                            IntensityRaw[i][non_zero_counter[k]] = IntensityInput[i][j];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("3: " + IntensityRaw[i].Length.ToString() + "[], " + IntensityInput[i].Length.ToString() + "\n" + ex.StackTrace.ToString());
            }

#if TESTLOG
            // --------------------------------------------------------
            FileStream fs = new FileStream("c:\\Develop\\belov\\testcsharp_accum.csv", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine("Intensity, TOF, Spectrum, element,, " + TOFBinWidth.ToString() + " ," + dTimeScale.ToString() + " ," + TOFOffset[0].ToString() + ", " + TOFOffset[1].ToString() + ", " + NumberOfElementsForTransform.ToString());

            for (i = 0; (i < NumberOfSpectra); ++i)
            {
                for (j = 0; j < ScanLength[i]; ++j)
                {
                    k = (int)(ArrivalTimeInput[i][j] / dIndexConverter);

                    if (non_zero_counter[k] >= 0)
                        if (IntensityRaw[i][non_zero_counter[k]] > MinNumberOfIons)
                            sw.WriteLine(IntensityRaw[i][non_zero_counter[k]].ToString() + ", " + ArrivalTime[i][non_zero_counter[k]].ToString() + ", " + i.ToString() + ", " + non_zero_counter[k].ToString());
                }
            }
            sw.Flush();
            sw.Close();
            fs.Close();
            // --------------------------------------------------------
#endif

            return true;
        }
#endif

    bool OnInverseTransform(int bit_number, int NumberOfElements, int NumberOfSpectra, double TOF_length) 
    {
	    double k, intensity_max, SpuriousNoiseWindow; 
	    int i_max, j_max;
	    int min_spectrum = 50;
	    int max_spectrum = 800;
	    int j_start,found_signal, spectrum_right, spectrum_left, spectrum_right_ref, spectrum_left_ref, counter = 0;
	    int spectrum_offset = 5, inverse_matrix_generator_counter = 0;
	    bool stop_counter = false;

	    float min_drift_time = 8000000; //minimum  detectable drift time: 8 ms in ns
	    float max_drift_time = 60000000.0F; //maximum detectable drift time: 60 ms in ns

	    if(!PRS_ENABLED ) 
        {
            status = false;
            return status;
        }

	    if(inverse_matrix_generator_counter == 0)
	    {
		    NORMAL_MATRIX = false;
		    INVERSE_TRANSFORM = false;

		    matrix = new Matrix();
		    inv_mat = new Matrix();

		    matrix.nRow_ = (int)(Math.Pow(2.0,bit_number)-1); // process weighed folded matrix
		    matrix.nCol_ = (int)(Math.Pow(2.0,bit_number)-1);
		    matrix.Reallocate();
    	
		    for(int i = 0; i < matrix.nRow_; i++) 
            {
                NORMAL_MATRIX = true; 
                matrix.data_[i] = PRS_weighed_folded[i];
            }

		    for(int j = 1; j < matrix.nCol_; ++j)
		    {
                matrix.data_[matrix.nCol_*j] = matrix.data_[matrix.nRow_-1 + matrix.nCol_*(j-1)]; 
			    for(int i = 1; i < matrix.nRow_; ++i)
                    matrix.data_[i+matrix.nCol_*j] = matrix.data_[i-1 + matrix.nCol_*(j-1)];
		    }

            if (matrix.inv(matrix, out inv_mat) != 0)
            {
                INVERSE_MATRIX = true;
                NORMAL_MATRIX = false;

                //  for(int i = 0; i < matrix.nRow_*matrix.nCol_; ++i) //writing inverse matrix values to data_ array
                //      inv_mat.data_[i];

#if TESTLOG // intensity_inverted is correct.
                FileStream fs = new FileStream("c:\\Develop\\belov\\testcsharp_invmatrix.csv", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);

                for (int i = 0; i < matrix.nRow_ * matrix.nCol_; ++i) //writing inverse matrix values to data_ array
                    sw.WriteLine(matrix.data_[i].ToString()+","+inv_mat.data_[i].ToString());
                sw.Flush();
                sw.Close();
                fs.Close();
#endif

                ++inverse_matrix_generator_counter; // need to generate inverse matrix only once

                status = false;
            }
            else
                MessageBox.Show("oh no");
	    }

	    if(INVERSE_MATRIX)
	    {
		    int i;
    	
		    INVERSE_TRANSFORM = true;

		    //performing inverse transform
		    int modulation_index = 0, j, row_index = 0, column_index = 0, TOF_index = 0;
		    double inverted_TOF_bins = 0;
    								
		    int row_index_new = 0, column_index_new = 0, result_index = 0;

		    N_SCANS_PER_MODULATION_BIN = FOLDING_FACTOR*TOFperBin;
		    N_BINS_EXTENDED_PRS = (int)(Math.Pow(2.0,BitNumber)-1)*FOLDING_FACTOR*TOFperBin;
            N_BINS_FOLDED_PRS = (int)(Math.Pow(2.0, BitNumber) - 1); 

		    //allocate memory
		    OnAllocateArraysForInverseTransform(N_BINS_EXTENDED_PRS, NumberOfElements);
    						
		    for(modulation_index = 0; modulation_index < N_SCANS_PER_MODULATION_BIN; ++modulation_index)
		    {
			    TOF_index = 0; 
			    result_index = modulation_index;				
			    do
			    {						
				    do
				    {
                        column_index = 0;				
					    do
					    {
						    //calculate one element in the original_data_array (i) which is the sum (j) of detected_data_array(i) * inverse_matrix[i][j]			
						    if(column_index == 0) 
                                inverted_TOF_bins = this.IntensityRaw[result_index][TOF_index]*inv_mat.data_[row_index + column_index];
						    else if(column_index > 0) 
                                inverted_TOF_bins = inverted_TOF_bins + this.IntensityRaw[result_index][TOF_index]*inv_mat.data_[row_index + column_index];
    					
						    result_index = result_index + N_SCANS_PER_MODULATION_BIN; 

						    if(column_index < N_BINS_FOLDED_PRS*(N_BINS_FOLDED_PRS-1))
							    column_index = column_index + N_BINS_FOLDED_PRS;
					    } while(result_index < N_BINS_EXTENDED_PRS);
    				
					    //save the element of the original data array(i) into a vector and increment i (N_PRS_BINS_EXPER times)							
					    inverted_array_TOF_bins[row_index] = (float)inverted_TOF_bins;

					    ++row_index;
					    result_index = modulation_index;
				    } while(row_index < N_BINS_FOLDED_PRS); 
    									
				    int u = 0; 					
				    //break  the original data vector apart and put its elements into the corresponding bins of the original data array
				    do
				    {
					    intensity_inverted[result_index][TOF_index] = inverted_array_TOF_bins[u];
					    ++u;
    						
					    result_index = result_index + N_SCANS_PER_MODULATION_BIN;
    						
				    } while (result_index < N_BINS_EXTENDED_PRS);

				    ++TOF_index; //indexation within TOF scan, which is a subscan of a modulation bin
				    row_index = 0;
				    result_index = 0;
			    } while (TOF_index <  NumberOfElements);  //ADC
		    }

#if TESTLOG // intensity_inverted is correct.
            FileStream fs = new FileStream("c:\\Develop\\belov\\testcsharp_inverted.csv", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            for(i = 0; i < NumberOfSpectra; ++i)
		    {
                sw.WriteLine();
			    for(j = 0; j < NumberOfElements; ++j)
			    {
				    if(intensity_inverted[i][j] > Math.Pow(10.0,-2)) 
                        sw.Write(intensity_inverted[i][j].ToString()+", ");		
                }
            }
            sw.Flush();
            sw.Close();
            fs.Close();
#endif

		    //clean up transfromed data array
		    //----------------------------------------------------------------------------------------------------------------------

		    min_spectrum = 49; //(int)(min_drift_time / TOF_length);
		    max_spectrum = 360; //(int)(max_drift_time / TOF_length);
     
		    OnBoxCarNoiseRemoval(N_BINS_EXTENDED_PRS, NumberOfElements, 4, intensity_inverted);
		    for(i = 0; i < NumberOfSpectra; i ++) 
			    for(j = 0; j < NumberOfElements; j++)
				    if(intensity_inverted[i][j] <= Math.Pow(10.0,-2)) 
                        intensity_inverted[i][j] = 0;		
    			     
#if TESTLOG
            FILE *fp2;
#if BELOV
            fp2 = fopen("c:\\Develop\\belov\\testbelov_boxcar.csv","w"); // open for writing 
#else
            fp2 = fopen("c:\\Develop\\belov\\testbill_boxcar.csv","w"); // open for writing 
#endif
            for(i = 0; i < NumberOfSpectra; ++i)
		    {
                fprintf(fp2, "\n");
			    for(j = 0; j < NumberOfElements; ++j)
			    {
				    if(intensity_inverted[i][j] > Math.Pow(10.0,-2)) 
                        fprintf(fp2, "%f, ", intensity_inverted[i][j]);		
                }
            }
            fflush(fp2);
            fclose(fp2);
#endif

		    //Remove spurious peaks; code below removes spurious peak after zero fill
		    for(j = 0; j < NumberOfElements; j++)
		    {
			    intensity_max = 0;
			    i_max = 0;
			    for(i = 0; i < NumberOfSpectra; i ++)
			    {
				    //find maximum intensity for a given TOF bin
				    if(intensity_inverted[i][j] > intensity_max)
				    {
					    j_max = j;
					    i_max = i;
					    intensity_max = intensity_inverted[i][j];    								
				    }

				    //if before min_spectrum or after max_spectrum, remove noise
				    if(i < min_spectrum || i > max_spectrum)
					    intensity_inverted[i][j] = 0.0F;		
			    }
    					
			    //find peak maximum and remove noise if observed spectrum_offset apart from contiguous peak
			    counter = 0;

                spectrum_right = i_max;
                spectrum_left = i_max;
                spectrum_right_ref = i_max + 1;
                spectrum_left_ref = i_max - 1;

                if (spectrum_right_ref >= NumberOfSpectra)
                    spectrum_right_ref = NumberOfSpectra - 1;

                if (spectrum_left_ref >= NumberOfSpectra)
                    spectrum_left_ref = NumberOfSpectra - 1;
                else if (spectrum_left_ref < 0)
                    spectrum_left_ref = 0;
                do
			    {
                    if((counter > 0) && (intensity_inverted[spectrum_right_ref][j] > 0) || (intensity_inverted[spectrum_left_ref][j]>0))
				    {
					    if((spectrum_right_ref - spectrum_right > spectrum_offset) && (spectrum_left - spectrum_left_ref > spectrum_offset))
					    {
						    stop_counter = true;
					    }
					    else
					    {
						    if(intensity_inverted[spectrum_right_ref][j] >0) 
                                spectrum_right = spectrum_right_ref;

						    if(intensity_inverted[spectrum_left_ref][j] >0) 
                                spectrum_left = spectrum_left_ref;
					    }
				    }
				    if(counter != 0)
				    {
					     spectrum_right_ref++;
					     spectrum_left_ref--;

					     if(spectrum_right_ref >= NumberOfSpectra)
                             spectrum_right_ref = NumberOfSpectra - 1; //prevent overrunning the buffer

					     if(spectrum_left_ref >= NumberOfSpectra)
                             spectrum_left_ref = NumberOfSpectra - 1; //prevent overrunning the buffer
					     else if(spectrum_left_ref < 0)
                             spectrum_left_ref = 0;
				    } 
				    counter++;

			    } while(!stop_counter && spectrum_right_ref < NumberOfSpectra && spectrum_left_ref > 0);

			    stop_counter = false;

			    if(spectrum_right >=  NumberOfSpectra)
                    spectrum_right = NumberOfSpectra -1; //prevent overrunning the buffer
			    if(spectrum_left <  1)
                    spectrum_left = 1; //prevent overrunning the buffer

			    for(i = spectrum_right+1; i < NumberOfSpectra; i++) 
                    intensity_inverted[i][j] = 0;
			    for(i = spectrum_left-1; i >= 0; i--) 
                    intensity_inverted[i][j] = 0;
		    }
    	
#if TESTLOG
            // ------------------------
            FILE *fp4;
#if BELOV
            fp4 = fopen("c:\\Develop\\belov\\testbelov_removespurious.csv","w"); // open for writing 
#else
            fp4 = fopen("c:\\Develop\\belov\\testbill_removespurious.csv","w"); // open for writing 
#endif
            for(i = 0; i < NumberOfSpectra; ++i)
		    {
                fprintf(fp4, "\n");
			    for(j = 0; j < NumberOfElements; ++j)
			    {
                    fprintf(fp4, "%d, ", this.ArrivalTime[i][j]);
//				    if(intensity_inverted[i][j] > Math.Pow(10.0,-2)) 
//                        fprintf(fp4, "%f, ", intensity_inverted[i][j]);		
                }
            }
            fflush(fp4);
            fclose(fp4);
            // ------------------------
#endif 

		    SpuriousNoiseWindow = 50.0;
    					
		    for(i =0; i < NumberOfSpectra; ++i)
		    {
			    j_start = 0;
			    do
			    {
				    j = j_start;
				    found_signal = 0;
				    do
				    { 
					    if(	intensity_inverted[i][j] > 0) 
						    found_signal ++;
    								
                        k = this.ArrivalTime[i][j] - this.ArrivalTime[i][j_start];
					    j++;
				    } while ((k < SpuriousNoiseWindow) && (j < NumberOfElements));

	                //-------------------------------------------------------------------------------------------
	                //RELEASE ISSUE: if j++ operation preceeds the inequality:  arrival_time_LIST[i][j] - arrival_time_LIST[i][j_start] < SpuriousNoiseWindown
	                //Release crashes. It looks like j goes out of array bounds before the j < SpectraWithNonZeroEntries
                    //                          condition is verified, resulting in memory corrution
	                //-------------------------------------------------------------------------------------------
    							
				    j_start = j;
				    if(found_signal >= 1 && found_signal <= 3)
				    {
					    if(j_start >= SpuriousNoiseWindow && j_start < NumberOfElements) //not using j_start < SpectraWithNonZeroEntries resulted in damaged memory block, spent several hours chasing down that bug
					    {
						    for(j = j_start; j > j_start - SpuriousNoiseWindow; --j) 
                                intensity_inverted[i][j] = 0.0F;
					    }
				    }
			    } while (j_start < NumberOfElements);
		    }
    	
#if TESTLOG
            // ------------------------
            FILE *fp3;
#if BELOV
            fp3 = fopen("c:\\Develop\\belov\\testbelov_spurious.csv","w"); // open for writing 
#else
            fp3 = fopen("c:\\Develop\\belov\\testbill_spurious.csv","w"); // open for writing 
#endif

            fprintf(fp3, "%lf, %d\n\n", SpuriousNoiseWindow, NumberOfElements);
            for(i = 0; i < NumberOfSpectra; ++i)
		    {
                fprintf(fp3, "\n");
			    for(j = 0; j < NumberOfElements; ++j)
			    {
				    if(intensity_inverted[i][j] > Math.Pow(10.0,-2)) 
                        fprintf(fp3, "%f, ", intensity_inverted[i][j]);		
                }
            }
            fflush(fp3);
            fclose(fp3);
            // ------------------------
#endif

		    //---------------------------------------------
		    // generate max and total intensity
		    for(i = 0; i < NumberOfSpectra; ++i)
		    {
			    for(j = 0; j < NumberOfElements; ++j)
			    {
				    if(intensity_inverted[i][j] <= Math.Pow(10.0,-2)) 
                        intensity_inverted[i][j] = 0;	
				    else if(this.ArrivalTime[i][j] > 40000000) 
                        intensity_inverted[i][j] = 0;
                    else //if(intensity_inverted[i][j] > 0.0)
				    {
					    this.IntensityTransformed[i][j] = (int)intensity_inverted[i][j];						
				    }
			    }
		    }

#if TESTLOG
            FILE *fp;
            char stuff[25];
#if BELOV
            fp = fopen("c:\\Develop\\belov\\testbelov_transform.csv","w"); // open for writing 
#else
            fp = fopen("c:\\Develop\\belov\\testbill_transform.csv","w"); // open for writing 
#endif
            for(i = 0; i < NumberOfSpectra; ++i)
                fprintf(fp, "%d, %d\n",iMax_Intensity[i], iTotal_Intensity[i]);
            fprintf(fp, "====================================");
            for(i = 0; i < NumberOfSpectra; ++i)
		    {
			    for(j = 0; j < NumberOfElements; ++j)
			    {
                    if (this.IntensityTransformed[i][j] > 0)
                        fprintf(fp, "%d, %d\n",this.ArrivalTime[i][j], this.IntensityTransformed[i][j]);
                }
            }
            fflush(fp);
            fclose(fp);
#endif
	    }
	    else
	    {			
		    status = false;
		    return status;
	    }

	    status = true;
	    return status;
    }

        void OnBoxCarNoiseRemoval(int iNumberOfSpectra, int iNumberOfElements, int iNumBoxCarBins, float[][] AfIntensity)
        {
            //allocate memory for box car array	
            float[] box_car_array = new float[iNumBoxCarBins];
            int SpectraWithNonZeroEntriesBoxCar;

            int i, j, k, j_max;
            double box_car_min;
            double box_car_max;
            float box_car_min_array;
            float box_car_max_array;

            box_car_min = -100;
            box_car_max = 1000;

            for (i = 0; i < iNumberOfSpectra; ++i)
            {
                SpectraWithNonZeroEntriesBoxCar = iNumberOfElements;

                j = 0;
                do
                {
                    k = 0;
                    //write intensities into box_car_array
                    do
                    {
                        box_car_array[k] = AfIntensity[i][j];
                        k++;
                        j++;

                    } while (k < iNumBoxCarBins && j < SpectraWithNonZeroEntriesBoxCar); //not including j < ... statement results in buffer overrun and damaged memory blocks
                    j_max = j;

                    //find min and max of the box_car_array
                    box_car_min_array = (float)Math.Pow(10.0, 5);
                    box_car_max_array = (float)-Math.Pow(10.0, 5);
                    for (k = 0; k < iNumBoxCarBins; ++k)
                    {
                        if (box_car_array[k] < box_car_min_array)
                            box_car_min_array = box_car_array[k];
                        if (box_car_array[k] > box_car_max_array)
                            box_car_max_array = box_car_array[k];
                    }

                    //if signal within box car window exhibits oscillations above min & max, reset intensities to zero
                    if ((box_car_min_array < box_car_min && box_car_max_array > box_car_max) || box_car_min_array < box_car_min)
                    {
                        for (j = j_max - iNumBoxCarBins; j < j_max; ++j)
                            AfIntensity[i][j] = 0.0F;
                    }
                } while (j < SpectraWithNonZeroEntriesBoxCar);
            }

            return;
        }

        void OnAllocateArraysForInverseTransform(int iN_BINS_EXTENDED_PRS, int iNumberOfElements)
        {
            int i, j;

            if (INVERSE_TRANSFORM)
            {
                //inverse transform memory allocations
                inverted_array_TOF_bins = new float[iN_BINS_EXTENDED_PRS];

                intensity_inverted = new float[iN_BINS_EXTENDED_PRS][];
                IntensityTransformed = new int[iN_BINS_EXTENDED_PRS][];

                for (i = 0; i < iN_BINS_EXTENDED_PRS; i++)
                {
                    intensity_inverted[i] = new float[iNumberOfElements];
                   //memset(intensity_inverted[i], 0, iNumberOfElements * sizeof(float));

                    IntensityTransformed[i] = new int[iNumberOfElements];
                   // memset(IntensityTransformed[i], 0, iNumberOfElements * sizeof(int));
                }
            }

            return;
        }
    }
}
