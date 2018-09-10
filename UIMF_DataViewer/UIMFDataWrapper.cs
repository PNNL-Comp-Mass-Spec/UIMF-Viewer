using System;
using System.Data.SQLite;
using UIMFLibrary;

namespace UIMF_File
{
    public class UIMFDataWrapper : UIMFLibrary.DataReader
    {
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // Viewer functionality
        //
        // William Danielson
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////
        //
        private double[] calibration_table;

        public string UIMF_DataFile;
        public UIMFLibrary.GlobalParams UIMF_GlobalParams;
        public UIMFLibrary.FrameParams UIMF_FrameParams;
        public int CurrentFrameNum;

        public int current_frame_index;
        public int frame_width;

        private int current_frame_type;
        public int[] array_FrameNum = new int[0];

        private double[] default_FragVoltages = (double[]) null;

        public UIMFLibrary.MzCalibrator mzCalibration;

        public UIMFDataWrapper(string uimf_file)
            : base(uimf_file)
        {
            this.UIMF_DataFile = uimf_file;
            this.UIMF_GlobalParams = this.GetGlobalParams();

            this.default_FragVoltages = this.get_DefaultFragVoltages();

            this.current_frame_type = this.get_FrameType();
            this.current_frame_index = 0;

            this.frame_width = 1;
        }

        public string FrameTypeDescription(int frameType)
        {
            if (frameType == 0)
                return "All Frames";
            else
                return FrameTypeDescription((FrameType)frameType);
        }

        public double[] get_DefaultFragVoltages()
        {
            if (default_FragVoltages == null)
            {
                // get the MS1 frames and use the fragmentation voltages on that.
                int current_frame_type = get_FrameType();
                int num_frames = this.set_FrameType(1);

                // if there are MS1 frames, set the default voltages.
                if (num_frames > 0)
                {
                    this.load_Frame(0); // will be a MS frame

                    if (this.UIMF_FrameParams.HasParameter(FrameParamKeyType.FragmentationProfile))
                    {
                        var fragSequence = FrameParamUtilities.ConvertByteArrayToFragmentationSequence(Convert.FromBase64String(this.UIMF_FrameParams.GetValueString(FrameParamKeyType.FragmentationProfile)));
                        if (fragSequence.Length == 4)
                            default_FragVoltages = fragSequence;
                        else
                            default_FragVoltages = (double[]) null;
                    }
                }

                // reset the frame type.
                this.set_FrameType(current_frame_type);
            }

            return default_FragVoltages;
        }

        bool flag_test = true;
        public int map_BinCalibration(int current_bin, double new_slope, double new_intercept)
        {
            double mz = this.mzCalibration.TOFtoMZ(((double) current_bin) * this.TenthsOfNanoSecondsPerBin);
            double r = (Math.Sqrt(mz));

#if false
            if (flag_test)
            {
                flag_test = false;
                double mz_test = this.mzCalibration.TOFtoMZ(((double) 80000) * this.TenthsOfNanoSecondsPerBin);
                int new_bin = (int) (((Math.Sqrt(mz_test) / new_slope) + new_intercept) * this.m_globalParameters.BinWidth * 10000.0 / this.TenthsOfNanoSecondsPerBin);
                MessageBox.Show("map bin 80000 bin to mz=" + mz_test.ToString() + " then back to bin "+ new_bin.ToString());
            }
#endif

            return (int)((((r / new_slope) + new_intercept) * this.m_globalParameters.BinWidth * 10000.0 / this.TenthsOfNanoSecondsPerBin) + .5); // .5 for rounding
        }

        public void update_CalibrationCoefficients(int frame_index, float slope, float intercept)
        {
            bool bAutoCalibrating = false;
            update_CalibrationCoefficients(frame_index, slope, intercept, bAutoCalibrating);
        }

        /// <summary>
        /// Update the calibration coefficients for a single frame
        /// </summary>
        /// <param name="frame_index"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        public void update_CalibrationCoefficients(int frame_index, float slope, float intercept, bool bAutoCalibrating)
        {
            if (frame_index > this.array_FrameNum.Length)
                return;

            using (var writer = new DataWriter(this.UIMF_DataFile))
            {
                writer.UpdateCalibrationCoefficients(this.array_FrameNum[frame_index], slope, intercept, bAutoCalibrating);
            }

            // Make sure the mz_Calibration object is up-to-date
            // These values will likely also get updated via the call to reset_FrameParameters (which then calls GetFrameParameters)
            this.mzCalibration.K = slope / 10000.0;
            this.mzCalibration.T0 = intercept * 10000.0;

            this.reset_FrameParameters();
        }

        public void updateAll_CalibrationCoefficients(float slope, float intercept)
        {
            updateAll_CalibrationCoefficients(slope, intercept, false);
        }

        /// <summary>
        /// /// Update the calibration coefficients for all frames
        /// </summary>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        public void updateAll_CalibrationCoefficients(float slope, float intercept, bool bAutoCalibrating)
        {
            using (var writer = new DataWriter(this.UIMF_DataFile))
            {
                writer.UpdateAllCalibrationCoefficients(slope, intercept, bAutoCalibrating);
            }

            this.reset_FrameParameters();
        }

        public void reset_FrameParameters()
        {
            this.UIMF_FrameParams = this.GetFrameParams(this.array_FrameNum[this.current_frame_index]);
            this.mzCalibration = GetMzCalibrator(UIMF_FrameParams);
        }

        public void clear_FrameParametersCache()
        {
            this.m_CachedFrameParameters.Clear();
        }


        /// <summary>
        /// Returns the frame index for the given frame number
        /// Only searches frame numbers of the current frame type (get_FrameType)
        /// </summary>
        /// <param name="frame_number"></param>
        /// <returns>Frame Index if found; otherwise; a negative number if not a valid frame number</returns>
        public int get_FrameIndex(int frame_number)
        {
            return Array.BinarySearch(this.array_FrameNum, frame_number);
        }

        /// <summary>
        /// Returns the current frame type
        /// </summary>
        /// <returns></returns>
        public int get_FrameType()
        {
            return this.current_frame_type;
        }

        /// <summary>
        /// Set the frame type (using enum FrameType)
        /// </summary>
        /// <param name="eFrameType">Frame type to set; see FrameType for types</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int set_FrameType(int eFrameType)
        {
            bool force_reload = false;
            return set_FrameType(eFrameType, force_reload);
        }

        /// <summary>
        /// Set the frame type
        /// </summary>
        /// <param name="frame_type">Frame type to set; see FrameType for types</param>
        /// <param name="force_reload">True to force a re-load of the data from the Frame_Parameters table</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int set_FrameType(int frame_type, bool force_reload)
        {
            int frame_count;
            int i;

            // If the frame type is already correct, then we don't need to re-query the database
            if ((this.current_frame_type == frame_type) && !force_reload)
                return this.array_FrameNum.Length;

            current_frame_type = frame_type;

            var preparedStatement = this.m_dbConnection.CreateCommand();
            if (frame_type == 0)
                preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters";
            else
                preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters WHERE FrameType = " + this.current_frame_type.ToString();
            var sqliteDataReader = preparedStatement.ExecuteReader();

            sqliteDataReader.Read();
            frame_count = Convert.ToInt32(sqliteDataReader[0]);
            sqliteDataReader.Dispose();

            if (frame_count == 0)
            {
                this.array_FrameNum = new int[0];
                return 0;
            }

            // build an array of frame numbers for instant referencing.
            this.array_FrameNum = new int[frame_count];
            preparedStatement.Dispose();

            preparedStatement = this.m_dbConnection.CreateCommand();
            if (frame_type == 0)
                preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters ORDER BY FrameNum ASC";
            else
                preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters WHERE FrameType = " + this.current_frame_type.ToString() + " ORDER BY FrameNum ASC";

            sqliteDataReader = preparedStatement.ExecuteReader();

            i = 0;
            while (sqliteDataReader.Read())
            {
                this.array_FrameNum[i] = Convert.ToInt32(sqliteDataReader[0]);
                i++;
            }

            this.CurrentFrameNum = this.array_FrameNum[0];
            this.UIMF_FrameParams = this.GetFrameParams(this.array_FrameNum[0]);
            this.mzCalibration = this.GetMzCalibrator(this.UIMF_FrameParams);

            preparedStatement.Dispose();

            //MessageBox.Show(frame_count.ToString());
            return frame_count;
        }

        public int load_Frame(int frame_index)
        {
            if ((frame_index < this.array_FrameNum.Length) && (frame_index > 0))
            {
                this.CurrentFrameNum = this.array_FrameNum[frame_index];
                this.UIMF_FrameParams = this.GetFrameParams(this.array_FrameNum[frame_index]);
                this.mzCalibration = this.GetMzCalibrator(this.UIMF_FrameParams);

                return this.array_FrameNum[frame_index];
            }
            else
                return -1;
        }

        /// <summary>
        /// Note that calling this function will auto-update the current frame type to frametype
        /// </summary>
        /// <param name="frametype"></param>
        /// <returns></returns>
        public int get_NumFrames(int frame_type)
        {
            return this.set_FrameType(frame_type);
        }

        public new double GetBinForPixel(int pixel)
        {
            // TODO: Renamed to "GetBinForPixel", and the UIMFLibrary version returns -1 on error...
            if (this.calibration_table == null)
            {
                // Random, likely safe return value...
                return 100000;
            }
            if (pixel < this.calibration_table.Length)
                return this.calibration_table[pixel];
            else
                return this.calibration_table[this.calibration_table.Length - 1]; // return maximum bin
        }

        public int[][] accumulate_FrameData(int frame_index, bool flag_TOF, int start_scan, int start_bin, int[][] frame_data, int y_compression)
        {
            return this.accumulate_FrameData(frame_index, flag_TOF, start_scan, start_bin, 0, this.m_globalParameters.Bins, frame_data, y_compression);
        }

        public int[][] accumulate_FrameData(int frame_index, bool flag_TOF, int start_scan, int start_bin, int min_mzbin, int max_mzbin, int[][] frame_data, int y_compression)
        {
            if ((frame_index < 0) || (frame_index >= this.array_FrameNum.Length))
                return frame_data;

            int i;

            int data_width = frame_data.Length;
            int data_height = frame_data[0].Length;

            byte[] compressed_BinIntensity;
            byte[] stream_BinIntensity = new byte[this.m_globalParameters.Bins * 4];
            int scans_data;
            int index_current_bin;
            int bin_data;
            int int_BinIntensity;
            int decompress_length;
            int pixel_y = 0;
            int current_scan;
            int bin_value;
            int end_bin;

            // this.UIMF_FrameParameters = this.GetFrameParameters(this.array_FrameNum[frame_index]);

            if (y_compression > 0)
                end_bin = start_bin + (data_height * y_compression);
            else if (y_compression < 0)
                end_bin = start_bin + data_height - 1;
            else
            {
                throw new Exception("UIMFLibrary accumulate_PlotData: Compression == 0");
            }

            // ensure the correct Frame parameters are set
            if (this.array_FrameNum[frame_index] != this.CurrentFrameNum)
            {
                this.CurrentFrameNum = this.array_FrameNum[frame_index];
                this.UIMF_FrameParams = this.GetFrameParams(this.array_FrameNum[frame_index]);
                this.mzCalibration = this.GetMzCalibrator(this.UIMF_FrameParams);
            }

            // Create a calibration lookup table -- for speed
            this.calibration_table = new double[data_height];
            if (flag_TOF)
            {
                for (i = 0; i < data_height; i++)
                    this.calibration_table[i] = start_bin + ((double)i * (double)(end_bin - start_bin) / (double)data_height);
            }
            else
            {
                double mz_min = (double)this.mzCalibration.TOFtoMZ((float)((start_bin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));
                double mz_max = (double)this.mzCalibration.TOFtoMZ((float)((end_bin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));

                for (i = 0; i < data_height; i++)
                    this.calibration_table[i] = (double)this.mzCalibration.MZtoTOF(mz_min + ((double)i * (mz_max - mz_min) / (double)data_height)) * this.m_globalParameters.BinWidth / (double)TenthsOfNanoSecondsPerBin;
            }

            // This function extracts intensities from selected scans and bins in a single frame
            // and returns a two-dimetional array intensities[scan][bin]
            // frameNum is mandatory and all other arguments are optional
            var preparedStatement = this.m_dbConnection.CreateCommand();
            preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString() + " AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + (start_scan + data_width - 1).ToString();
            var sqliteDataReader = preparedStatement.ExecuteReader();
            preparedStatement.Dispose();

            // accumulate the data into the plot_data
            if (y_compression < 0)
            {
                pixel_y = 1;

                //MessageBox.Show(start_bin.ToString() + " " + end_bin.ToString());

                for (scans_data = 0; ((scans_data < data_width) && sqliteDataReader.Read()); scans_data++)
                {
                    current_scan = Convert.ToInt32(sqliteDataReader["ScanNum"]) - start_scan;
                    compressed_BinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                    if (compressed_BinIntensity.Length == 0)
                        continue;

                    index_current_bin = 0;
                    //decompress_length = CLZF2.Decompress(compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                    decompress_length = CLZF2.Decompress(compressed_BinIntensity, ref stream_BinIntensity);

                    int previousValue = 0;
                    for (bin_data = 0; (bin_data < decompress_length) && (index_current_bin <= end_bin); bin_data += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_data);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity;   // concurrent zeros
                        }
                        else if (int_BinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                        {
                            // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                            // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                            // consecutive zeroes to hit the underflow limit
                            // Really, the encoding we are using should never output a zero.
                        }
                        else if ((index_current_bin < min_mzbin) || (index_current_bin < start_bin))
                            index_current_bin++;
                        else if (index_current_bin > max_mzbin)
                            break;
                        else
                        {
                            frame_data[current_scan][index_current_bin - start_bin] += int_BinIntensity;
                            index_current_bin++;
                        }
                        previousValue = int_BinIntensity;
                    }
                }
            }
            else    // each pixel accumulates more than 1 bin of data
            {
                for (scans_data = 0; ((scans_data < data_width) && sqliteDataReader.Read()); scans_data++)
                {
                    current_scan = Convert.ToInt32(sqliteDataReader["ScanNum"]) - start_scan;

                    compressed_BinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                    if (compressed_BinIntensity.Length == 0)
                        continue;

                    index_current_bin = 0;
                    //decompress_length = LZFCompressionUtil.Decompress(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                    decompress_length = CLZF2.Decompress(compressed_BinIntensity, ref stream_BinIntensity);

                    pixel_y = 1;

                    double calibrated_bin = 0;
                    int previousValue = 0;
                    for (bin_value = 0; (bin_value < decompress_length) && (index_current_bin < end_bin); bin_value += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_value);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity; // concurrent zeros
                        }
                        else if (int_BinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                        {
                            // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                            // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                            // consecutive zeroes to hit the underflow limit
                            // Really, the encoding we are using should never output a zero.
                        }
                        else if ((index_current_bin < min_mzbin) || (index_current_bin < start_bin))
                            index_current_bin++;
                        else if (index_current_bin > max_mzbin)
                            break;
                        else
                        {
                            calibrated_bin = (double)index_current_bin;

                            for (i = pixel_y; i < data_height; i++)
                            {
                                if (this.calibration_table[i] > calibrated_bin)
                                {
                                    pixel_y = i;
                                    frame_data[current_scan][pixel_y] += int_BinIntensity;
                                    break;
                                }
                            }
                            index_current_bin++;
                        }
                        previousValue = int_BinIntensity;
                    }
                }
            }

            sqliteDataReader.Close();
            return frame_data;
        }

        public int[] Get_SumScans(int frame_index, int start_scan, int end_scan)
        {
            if ((frame_index < 0) || (frame_index >= this.array_FrameNum.Length))
                return (int[])null;

            int i;

            byte[] compressed_BinIntensity;
            byte[] stream_BinIntensity = new byte[this.m_globalParameters.Bins * 4];
            int[] TOF_Array = new int[this.m_globalParameters.Bins];
            int scans_data;
            int index_current_bin;
            int bin_data;
            int int_BinIntensity;
            int decompress_length;

            // ensure the correct Frame parameters are set
            if (this.array_FrameNum[frame_index] != this.CurrentFrameNum)
            {
                this.CurrentFrameNum = this.array_FrameNum[frame_index];
                this.UIMF_FrameParams = this.GetFrameParams(this.array_FrameNum[frame_index]);
                this.mzCalibration = this.GetMzCalibrator(this.UIMF_FrameParams);
            }

            // This function extracts intensities from selected scans and bins in a single frame
            // and returns a two-dimetional array intensities[scan][bin]
            // frameNum is mandatory and all other arguments are optional
            var preparedStatement = this.m_dbConnection.CreateCommand();
            preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString() + " AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + end_scan.ToString();

            // MessageBox.Show("ss: "+this.array_FrameNum[frame_index].ToString());

            var sqliteDataReader = preparedStatement.ExecuteReader();
            preparedStatement.Dispose();

            for (scans_data = start_scan; ((scans_data <= end_scan) && sqliteDataReader.Read()); scans_data++)
            {
                compressed_BinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                if (compressed_BinIntensity.Length == 0)
                    continue;

                index_current_bin = 0;
                //decompress_length = LZFCompressionUtil.Decompress(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                decompress_length = CLZF2.Decompress(compressed_BinIntensity, ref stream_BinIntensity);

                int previousValue = 0;
                for (bin_data = 0; (bin_data < decompress_length); bin_data += 4)
                {
                    int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_data);

                    if (int_BinIntensity < 0)
                    {
                        index_current_bin += -int_BinIntensity;   // concurrent zeros
                    }
                    else if (int_BinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                    {
                        // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                        // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                        // consecutive zeroes to hit the underflow limit
                        // Really, the encoding we are using should never output a zero.
                    }
                    else
                    {
                        TOF_Array[index_current_bin] += int_BinIntensity;
                        index_current_bin++;
                    }
                    previousValue = int_BinIntensity;
                }
            }

            sqliteDataReader.Close();
            return TOF_Array;
        }

        public int[] get_MobilityData(int frame_index)
        {
            return get_MobilityData(frame_index, 0, this.m_globalParameters.Bins);
        }

        public int[] get_MobilityData(int frame_index, int min_mzbin, int max_mzbin)
        {
            int[] mobility_data = new int[0];
            int mobility_index;
            byte[] compressed_BinIntensity;
            byte[] stream_BinIntensity = new byte[this.m_globalParameters.Bins * 4];
            int current_scan;
            int int_BinIntensity;
            int decompress_length;
            int bin_index;
            int index_current_bin;

            try
            {
                this.load_Frame(frame_index);
                mobility_data = new int[this.UIMF_FrameParams.Scans];

                // This function extracts intensities from selected scans and bins in a single frame
                // and returns a two-dimensional array intensities[scan][bin]
                // frameNum is mandatory and all other arguments are optional
                var preparedStatement = this.m_dbConnection.CreateCommand();
                preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString();// +" AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + (start_scan + data_width).ToString();

                var sqliteDataReader = preparedStatement.ExecuteReader();
                preparedStatement.Dispose();

                for (mobility_index = 0; ((mobility_index < this.UIMF_FrameParams.Scans) && sqliteDataReader.Read()); mobility_index++)
                {
                    current_scan = Convert.ToInt32(sqliteDataReader["ScanNum"]);
                    compressed_BinIntensity = (byte[])(sqliteDataReader["Intensities"]);

                    if ((compressed_BinIntensity.Length == 0) || (current_scan >= this.UIMF_FrameParams.Scans))
                        continue;

                    index_current_bin = 0;
                    //decompress_length = CLZF2.lzf_decompress(compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);
                    decompress_length = CLZF2.Decompress(compressed_BinIntensity, ref stream_BinIntensity);

                    int previousValue = 0;
                    for (bin_index = 0; (bin_index < decompress_length); bin_index += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_index);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity;   // concurrent zeros
                        }
                        else if (int_BinIntensity == 0 && (previousValue.Equals(short.MinValue) || previousValue.Equals(int.MinValue)))
                        {
                            // Do nothing: this is to handle an old bug in the run-length zero encoding, that would do a
                            // double-output of a zero (output a zero, and add it to the zero count) if there were enough
                            // consecutive zeroes to hit the underflow limit
                            // Really, the encoding we are using should never output a zero.
                        }
                        else if (index_current_bin < min_mzbin)
                            index_current_bin++;
                        else if (index_current_bin > max_mzbin)
                            break;
                        else
                        {
                            try
                            {
                                mobility_data[current_scan] += int_BinIntensity;
                            }
                            catch (Exception)
                            {
                                throw new Exception(mobility_index.ToString() + "  " + current_scan.ToString());
                            }
                        }
                        previousValue = int_BinIntensity;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("get_MobilityData: \n\n" + ex.ToString());
            }

            return mobility_data;
        }
    }
}
