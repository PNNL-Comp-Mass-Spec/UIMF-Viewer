using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
        public UIMFLibrary.GlobalParameters UIMF_GlobalParameters;
        public UIMFLibrary.FrameParameters UIMF_FrameParameters;

        public int current_frame_index;
        public int frame_width;

        private int current_frame_type;
        public int[] array_FrameNum = new int[0];

        public UIMFDataWrapper(string uimf_file)
            : base(uimf_file)
        {
            this.UIMF_DataFile = uimf_file;
            this.UIMF_GlobalParameters = this.GetGlobalParameters();

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

        public double get_AveFrameDuration_Seconds()
        {
            double ave_duration;

            m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
            m_preparedStatement.CommandText = "SELECT sum(duration) FROM Frame_parameters WHERE FrameType=" + this.current_frame_type.ToString();
            this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();

            double total_duration = Convert.ToInt32(this.m_sqliteDataReader[0]);
            m_preparedStatement.Dispose();

            double total_frames = (double)set_FrameType(this.current_frame_type, false);

            if (total_frames > 0)
                ave_duration = total_duration / total_frames;
            else
                ave_duration = total_duration;

            return ave_duration;
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

            m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
            m_preparedStatement.CommandText = "UPDATE Frame_Parameters " +
                                             "SET CalibrationSlope = " + slope.ToString() + ", " +
                                                 "CalibrationIntercept = " + intercept.ToString();
            if (bAutoCalibrating)
                m_preparedStatement.CommandText += ", CalibrationDone = 1";

            m_preparedStatement.CommandText += " WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString();

            m_preparedStatement.ExecuteNonQuery();
            m_preparedStatement.Dispose();

            // Make sure the mz_Calibration object is up-to-date
            // These values will likely also get updated via the call to reset_FrameParameters (which then calls GetFrameParameters)
            this.m_mzCalibration.k = slope / 10000.0;
            this.m_mzCalibration.t0 = intercept * 10000.0;

            this.reset_FrameParameters();
        }

        public void updateAll_CalibrationCoefficients(float slope, float intercept)
        {
            bool bAutoCalibrating = false;
            updateAll_CalibrationCoefficients(slope, intercept, bAutoCalibrating);
        }

        /// <summary>
        /// /// Update the calibration coefficients for all frames
        /// </summary>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        public void updateAll_CalibrationCoefficients(float slope, float intercept, bool bAutoCalibrating)
        {
            m_preparedStatement = m_uimfDatabaseConnection.CreateCommand();
            m_preparedStatement.CommandText = "UPDATE Frame_Parameters " +
                                             "SET CalibrationSlope = " + slope.ToString() + ", " +
                                                 "CalibrationIntercept = " + intercept.ToString();
            if (bAutoCalibrating)
                m_preparedStatement.CommandText += ", CalibrationDone = 1";

            m_preparedStatement.ExecuteNonQuery();
            m_preparedStatement.Dispose();

            this.reset_FrameParameters();
        }

        public void reset_FrameParameters()
        {
            this.m_frameParametersCache.Clear();
            this.GetFrameParameters(this.array_FrameNum[this.current_frame_index]);
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
        /// <param name="ForceReload">True to force a re-load of the data from the Frame_Parameters table</param>
        /// <returns>The number of frames in the file that have the given frame type</returns>
        public int set_FrameType(int frame_type, bool force_reload)
        {
            int frame_count;
            int i;

            // If the frame type is already correct, then we don't need to re-query the database
            if ((this.current_frame_type == frame_type) && !force_reload)
                return this.array_FrameNum.Length;

            current_frame_type = frame_type;

            this.m_preparedStatement = this.m_uimfDatabaseConnection.CreateCommand();
            if (frame_type == 0)
                this.m_preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters";
            else
                this.m_preparedStatement.CommandText = "SELECT COUNT(FrameNum) FROM Frame_Parameters WHERE FrameType = " + this.current_frame_type.ToString();
            this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();

            frame_count = Convert.ToInt32(this.m_sqliteDataReader[0]);
            this.m_sqliteDataReader.Dispose();

            if (frame_count == 0)
            {
                this.array_FrameNum = new int[0];
                return 0;
            }

            // build an array of frame numbers for instant referencing.
            this.array_FrameNum = new int[frame_count];
            this.m_preparedStatement.Dispose();

            this.m_preparedStatement = this.m_uimfDatabaseConnection.CreateCommand();
            if (frame_type == 0)
                this.m_preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters ORDER BY FrameNum ASC";
            else
                this.m_preparedStatement.CommandText = "SELECT FrameNum FROM Frame_Parameters WHERE FrameType = " + this.current_frame_type.ToString() + " ORDER BY FrameNum ASC";

            this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();

            i = 0;
            while (this.m_sqliteDataReader.Read())
            {
                this.array_FrameNum[i] = Convert.ToInt32(this.m_sqliteDataReader[0]);
                i++;
            }

            this.UIMF_FrameParameters = this.GetFrameParameters(this.array_FrameNum[0]);
            this.m_preparedStatement.Dispose();

            //MessageBox.Show(frame_count.ToString());
            return frame_count;
        }

        public int load_Frame(int frame_index)
        {
            if ((frame_index < this.array_FrameNum.Length) && (frame_index > 0))
            {
                this.UIMF_FrameParameters = this.GetFrameParameters(this.array_FrameNum[frame_index]);
                return this.array_FrameNum[frame_index];
            }
            else
                return -1;
        }

        /// <summary>
        /// Returns the number of frames for the current frame type
        /// </summary>
        /// <returns></returns>
        public int get_NumFramescurrent_frame_type()
        {
            return this.array_FrameNum.Length;
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

        public double get_pixelMZ(int bin)
        {
            if ((this.calibration_table != null) && (bin < this.calibration_table.Length))
                return this.calibration_table[bin];
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

            // Create a calibration lookup table -- for speed
            this.calibration_table = new double[data_height];
            if (flag_TOF)
            {
                for (i = 0; i < data_height; i++)
                    this.calibration_table[i] = start_bin + ((double)i * (double)(end_bin - start_bin) / (double)data_height);
            }
            else
            {
                double mz_min = (double)this.m_mzCalibration.TOFtoMZ((float)((start_bin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));
                double mz_max = (double)this.m_mzCalibration.TOFtoMZ((float)((end_bin / this.m_globalParameters.BinWidth) * TenthsOfNanoSecondsPerBin));

                for (i = 0; i < data_height; i++)
                    this.calibration_table[i] = (double)this.m_mzCalibration.MZtoTOF(mz_min + ((double)i * (mz_max - mz_min) / (double)data_height)) * this.m_globalParameters.BinWidth / (double)TenthsOfNanoSecondsPerBin;
            }

            // ensure the correct Frame parameters are set
            if (this.array_FrameNum[frame_index] != this.UIMF_FrameParameters.FrameNum)
            {
                this.UIMF_FrameParameters = (UIMFLibrary.FrameParameters)this.GetFrameParameters(this.array_FrameNum[frame_index]);
            }

            // This function extracts intensities from selected scans and bins in a single frame 
            // and returns a two-dimetional array intensities[scan][bin]
            // frameNum is mandatory and all other arguments are optional
            this.m_preparedStatement = this.m_uimfDatabaseConnection.CreateCommand();
            this.m_preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString() + " AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + (start_scan + data_width - 1).ToString();

            this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();
            this.m_preparedStatement.Dispose();

            // accumulate the data into the plot_data
            if (y_compression < 0)
            {
                pixel_y = 1;

                //MessageBox.Show(start_bin.ToString() + " " + end_bin.ToString());

                for (scans_data = 0; ((scans_data < data_width) && this.m_sqliteDataReader.Read()); scans_data++)
                {
                    current_scan = Convert.ToInt32(this.m_sqliteDataReader["ScanNum"]) - start_scan;
                    compressed_BinIntensity = (byte[])(this.m_sqliteDataReader["Intensities"]);

                    if (compressed_BinIntensity.Length == 0)
                        continue;

                    index_current_bin = 0;
                    decompress_length = UIMFLibrary.IMSCOMP_wrapper.decompress_lzf(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);

                    for (bin_data = 0; (bin_data < decompress_length) && (index_current_bin <= end_bin); bin_data += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_data);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity;   // concurrent zeros
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
                    }
                }
            }
            else    // each pixel accumulates more than 1 bin of data
            {
                for (scans_data = 0; ((scans_data < data_width) && this.m_sqliteDataReader.Read()); scans_data++)
                {
                    current_scan = Convert.ToInt32(this.m_sqliteDataReader["ScanNum"]) - start_scan;
                    // if (current_scan >= data_width)
                    //     break;

                    compressed_BinIntensity = (byte[])(this.m_sqliteDataReader["Intensities"]);

                    if (compressed_BinIntensity.Length == 0)
                        continue;

                    index_current_bin = 0;
                    decompress_length = UIMFLibrary.IMSCOMP_wrapper.decompress_lzf(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);

                    pixel_y = 1;

                    double calibrated_bin = 0;
                    for (bin_value = 0; (bin_value < decompress_length) && (index_current_bin < end_bin); bin_value += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_value);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity; // concurrent zeros
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
                    }
                }
            }

            this.m_sqliteDataReader.Close();
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
            if (this.array_FrameNum[frame_index] != this.UIMF_FrameParameters.FrameNum)
            {
                this.UIMF_FrameParameters = (UIMFLibrary.FrameParameters)this.GetFrameParameters(this.array_FrameNum[frame_index]);
            }

            // This function extracts intensities from selected scans and bins in a single frame 
            // and returns a two-dimetional array intensities[scan][bin]
            // frameNum is mandatory and all other arguments are optional
            this.m_preparedStatement = this.m_uimfDatabaseConnection.CreateCommand();
            this.m_preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString() + " AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + end_scan.ToString();

            // MessageBox.Show("ss: "+this.array_FrameNum[frame_index].ToString());

            this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();
            this.m_preparedStatement.Dispose();

            for (scans_data = start_scan; ((scans_data <= end_scan) && this.m_sqliteDataReader.Read()); scans_data++)
            {
                compressed_BinIntensity = (byte[])(this.m_sqliteDataReader["Intensities"]);

                if (compressed_BinIntensity.Length == 0)
                    continue;

                index_current_bin = 0;
                decompress_length = UIMFLibrary.IMSCOMP_wrapper.decompress_lzf(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);

                for (bin_data = 0; (bin_data < decompress_length); bin_data += 4)
                {
                    int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_data);

                    if (int_BinIntensity < 0)
                    {
                        index_current_bin += -int_BinIntensity;   // concurrent zeros
                    }
                    else
                    {
                        TOF_Array[index_current_bin] += int_BinIntensity;
                        index_current_bin++;
                    }
                }
            }

            this.m_sqliteDataReader.Close();
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
                mobility_data = new int[this.UIMF_FrameParameters.Scans];

                // This function extracts intensities from selected scans and bins in a single frame 
                // and returns a two-dimetional array intensities[scan][bin]
                // frameNum is mandatory and all other arguments are optional
                this.m_preparedStatement = this.m_uimfDatabaseConnection.CreateCommand();
                this.m_preparedStatement.CommandText = "SELECT ScanNum, Intensities FROM Frame_Scans WHERE FrameNum = " + this.array_FrameNum[frame_index].ToString();// +" AND ScanNum >= " + start_scan.ToString() + " AND ScanNum <= " + (start_scan + data_width).ToString();

                this.m_sqliteDataReader = this.m_preparedStatement.ExecuteReader();
                this.m_preparedStatement.Dispose();

                for (mobility_index = 0; ((mobility_index < this.UIMF_FrameParameters.Scans) && this.m_sqliteDataReader.Read()); mobility_index++)
                {
                    current_scan = Convert.ToInt32(this.m_sqliteDataReader["ScanNum"]);
                    compressed_BinIntensity = (byte[])(this.m_sqliteDataReader["Intensities"]);

                    if ((compressed_BinIntensity.Length == 0) || (current_scan >= this.UIMF_FrameParameters.Scans))
                        continue;

                    index_current_bin = 0;
                    decompress_length = UIMFLibrary.IMSCOMP_wrapper.decompress_lzf(ref compressed_BinIntensity, compressed_BinIntensity.Length, ref stream_BinIntensity, this.m_globalParameters.Bins * 4);

                    for (bin_index = 0; (bin_index < decompress_length); bin_index += 4)
                    {
                        int_BinIntensity = BitConverter.ToInt32(stream_BinIntensity, bin_index);

                        if (int_BinIntensity < 0)
                        {
                            index_current_bin += -int_BinIntensity;   // concurrent zeros
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
