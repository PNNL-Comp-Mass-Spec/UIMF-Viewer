#if THERMO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Drawing.Drawing2D;

namespace UIMF_DataViewer
{
    public struct FileData
    {
        public double mz;
        public float intensity;

        public FileData(double m, float i)
        {
            mz = m;
            intensity = i;
        }
    }

    public class BinViewer : UIMF_File.DataViewer
    {
        private bool flag_FileOpen = false;
        private string file_BIN;

        private double current_minMZ;
        private double current_maxMZ;
        private int current_minSpectrum;
        private int current_maxSpectrum;

        private double new_minMZ;
        private double new_maxMZ;
        private int new_minSpectrum;
        private int new_maxSpectrum;

        private int frame_minSpectrum;
        private int frame_maxSpectrum;

        private double raw_minMZ;
        private double raw_maxMZ;
        private int raw_minSpectrum;
        private int raw_maxSpectrum;

        int num_spectra;

        private double[] lookup_table;

        private double current_MZPerPixelY;
        private int current_MZPerPixelX;

        private const double MIN_GRAPHED_MZ = .5;
        private const int MIN_GRAPHED_SPECTRA = 10;
        private const int MAX_GRAPHED_SPECTRA = 1500;

        private double[][,] data_point;

        public BinViewer(string filename)
            : base()
        {
            this.file_BIN = filename;
            this.OpenBINFile(this.file_BIN);

            this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F0");

            Generate2DIntensityArray(0);
            this.GraphFrame(this.data_2D);

            // this.slider_ColorMap.set_Colors((float).2, (float).3, (float).4, (float).5, (float).6, (float).7);

            this.rb_CompleteChromatogram.Enabled = this.rb_PartialChromatogram.Enabled = false;
            this.pnl_Chromatogram.Visible = false;

            this.lbl_IonMobilityValue.Text = "Spectra: ";
            this.lbl_TimeOffset.Visible = false;
            this.lbl_CursorScanTime.Visible = false;
            this.lbl_CursorTOF.Visible = false;
            this.label3.Visible = this.label2.Visible = false;
            this.label5.Visible = false;
            this.lbl_TOForMZ.Visible = false;
            this.num_FrameIndex.Visible = false;
            this.cb_EnableMZRange.Visible = false;
            this.gb_MZRange.Visible = false;

            this.label4.Top = this.lbl_CursorMobility.Top + this.lbl_CursorMobility.Height + 6;
            this.lbl_CursorMZ.Top = this.label4.Top;
            this.tabpages_FrameInfo.TabPages.Remove(this.tabPage_Calibration);
            this.tabpages_FrameInfo.Height = 100;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            this.flag_Alive = false;
            this.flag_Closing = true;

            this.AllowDrop = false;
            this.flag_update2DGraph = false;

            while (this.flag_collecting_data)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (disposing)
            {
                try
                {
                    /*
                    bitmap = null;

                    data_driftTIC = null;
                    data_tofTIC = null;
                    */
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Disposing object IonMobilityDataViewShort:  " + ex.ToString());
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            base.Dispose(disposing);
        }

        // ///////////////////////////////////////////////////////////////////////////////////
        // BIN FILE
        //
        public void OpenBINFile(string bin_filename)
        {
            FileStream fs = new FileStream(bin_filename, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            int filelength = (int)fs.Length;
            int total_datapoints = ((int)fs.Length - 8) / 16;
            this.num_spectra = br.ReadInt32();
            int f_spectrum;
            double f_mz;
            float f_intensity;
            FileData fd;
            ArrayList[] file_data = new ArrayList[this.num_spectra];
 
            for (int i = 0; i < this.num_spectra; i++)
                file_data[i] = new ArrayList();

            this.data_point = new double[this.num_spectra][,];

            this.raw_minMZ = 10000000;
            this.raw_maxMZ = 0;
            FileStream fs1 = new FileStream(@"c:\ionmobilitydata\test.csv", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs1);
            for (int i = 0; i < total_datapoints-10; i++)
            {
                f_spectrum = br.ReadInt32();
                f_mz = br.ReadDouble();
                f_intensity = br.ReadSingle();

                fd = new FileData(f_mz, f_intensity);
                file_data[f_spectrum].Add(fd);

                sw.WriteLine(f_spectrum.ToString() + ", " + f_mz.ToString() + ", " + f_intensity.ToString());

                if (f_mz < this.raw_minMZ)
                    this.raw_minMZ = f_mz;
                if (f_mz > this.raw_maxMZ)
                    this.raw_maxMZ = f_mz;
            }

            // add zero's to the file.
            for (int j = 0; j < this.num_spectra; j++)
            {
                for (int i = 1; i < file_data[j].Count; i++)
                {
                    if (((FileData)file_data[j][i]).mz - ((FileData)file_data[j][i - 1]).mz > .05)
                    {
                        fd = new FileData(((FileData)file_data[j][i]).mz - .002, (float)0.0);
                        file_data[j].Insert(i, fd);

                        fd = new FileData(((FileData)file_data[j][i - 1]).mz + .002, (float)0.0);
                        file_data[j].Insert(i, fd);

                        i += 2;
                    }
                }
                if (file_data[j].Count > 0)
                {
                    fd = new FileData(((FileData)file_data[j][file_data[j].Count - 1]).mz + .001, (float)0.0);
                    file_data[j].Add(fd);
                }
            }
            sw.Flush();
            sw.Close();
            fs1.Close();

            // create a usable data array
            for (int j = 0; j < this.num_spectra; j++)
            {
                this.data_point[j] = new double[2, file_data[j].Count];
                for (int i = 0; i < file_data[j].Count; i++)
                {
                    this.data_point[j][0, i] = ((FileData)file_data[j][i]).mz;
                    this.data_point[j][1, i] = ((FileData)file_data[j][i]).intensity;
                }
            }
            br.Close();
            fs.Close();

            // destroy the file data
            for (int i = 0; i < this.num_spectra; i++)
                file_data[i].Clear();

            this.frame_minSpectrum = this.new_minSpectrum = this.current_minSpectrum = this.raw_minSpectrum = 0;
            this.frame_maxSpectrum = this.new_maxSpectrum = this.current_maxSpectrum = this.raw_maxSpectrum = this.num_spectra-1;

            //MessageBox.Show(this.frame_minSpectrum.ToString() + " " + this.frame_maxSpectrum.ToString());

            this.new_minMZ = this.current_minMZ = this.raw_minMZ;
            this.new_maxMZ = this.current_maxMZ = this.raw_maxMZ;

            //MessageBox.Show(this.new_minMZ.ToString() + " " + this.new_maxMZ.ToString());

            this.num_minBin.Value = Convert.ToDecimal(this.current_minMZ);
            this.num_maxBin.Value = Convert.ToDecimal(this.current_maxMZ);

            this.flag_FileOpen = true;
        }

        public bool isOpen()
        {
            return this.flag_FileOpen;
        }

        protected override void pb_2DMap_DblClick(object sender, System.EventArgs e)
        {
            // Reinitialize
            _zoomX.Clear();
            _zoomBin.Clear();

            this.current_MZPerPixelX = 1;

            this.new_minMZ = this.raw_minMZ;
            this.new_minSpectrum = this.raw_minSpectrum;
            this.new_maxMZ = this.raw_maxMZ;
            this.new_maxSpectrum = this.raw_maxSpectrum;

            this.flag_selection_drift = false;
            this.plot_Mobility.ClearRange();

            this.flag_update2DGraph = true;

            this.AutoScrollPosition = new Point(0, 0);
            // this.ResizeThis();
        }

        // Handler for the pb_2DMap's ContextMenu
        protected override void ZoomContextMenu(object sender, System.EventArgs e)
        {
            // Who sent you?
            if (sender == this.menuItemZoomFull)
            {
                this.pb_2DMap_DblClick((object)null, (System.EventArgs)null);
            }
            else if (sender == this.menuItemZoomPrevious)
            {
                if (_zoomX.Count < 2)
                {
                    this.pb_2DMap_DblClick((object)null, (System.EventArgs)null);
                    return;
                }
                this.new_minSpectrum = ((Point)_zoomX[_zoomX.Count - 2]).X;
                this.new_maxSpectrum = ((Point)_zoomX[_zoomX.Count - 2]).Y;

                this.new_minMZ = ((dPoint)_zoomBin[_zoomBin.Count - 2]).X;
                this.new_maxMZ = ((dPoint)_zoomBin[_zoomBin.Count - 2]).Y;

                _zoomX.RemoveAt(_zoomX.Count - 1);
                _zoomBin.RemoveAt(_zoomBin.Count - 1);

                this.flag_update2DGraph = true;
            }
            else if (sender == this.menuItemZoomOut) // double the view window
            {
                int temp = this.current_maxSpectrum - this.current_minSpectrum + 1;
                this.new_minSpectrum = this.current_minSpectrum - (temp / 3) - 1;
                if (this.new_minSpectrum < this.frame_minSpectrum)
                    this.new_minSpectrum = this.frame_minSpectrum;
                this.new_maxSpectrum = this.current_maxSpectrum + (temp / 3) + 1;
                if (this.new_maxSpectrum > this.frame_maxSpectrum)
                    this.new_maxSpectrum = this.frame_maxSpectrum;

                double temp_mz = this.current_maxMZ - this.current_minMZ + 1;
                this.new_minMZ = this.current_minMZ - temp_mz - 1;
                if (this.new_minMZ < this.raw_minMZ)
                    this.new_minMZ = this.raw_minMZ;
                this.new_maxMZ = this.current_maxMZ + temp_mz + 1;
                if (this.new_maxMZ > this.maximum_Bins)
                    this.new_maxMZ = this.maximum_Bins - 1;

                _zoomX.Add(new Point(this.new_minSpectrum, this.new_maxSpectrum));
                _zoomBin.Add(new dPoint(this.new_minMZ, this.new_maxMZ));

                this.flag_update2DGraph = true;

                //this.Zoom(new System.Drawing.Point(new_minMobility, new_maxBin), new System.Drawing.Point(new_maxMobility, new_minBin));
            }
        }

        protected override void Zoom(Point p1, Point p2)
        {
            lock (this.lock_graphing)
            {
                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                // Prep variables
                int min_Px = Math.Min(p1.X, p2.X);
                int max_Px = Math.Max(p1.X, p2.X);
                int min_Py = this.pb_2DMap.Height - Math.Max(p1.Y, p2.Y);
                if (min_Py < 0)
                    min_Py = 0;
                int max_Py = this.pb_2DMap.Height - Math.Min(p1.Y, p2.Y);
                if (max_Py >= this.lookup_table.Length)
                    max_Py = this.lookup_table.Length - 1;

                // MessageBox.Show("(" + min_Px.ToString() + ", " + min_Py.ToString() + ")(" + max_Px.ToString() + ", " + max_Py.ToString() + ")");

                this.new_minMZ = this.lookup_table[min_Py];
                this.new_maxMZ = this.lookup_table[max_Py];
                while ((this.new_maxMZ - this.new_minMZ < MIN_GRAPHED_MZ) && ((min_Py > 0) || (max_Py < this.lookup_table.Length - 1)))
                {
                    if (min_Py > 0)
                        min_Py--;
                    this.new_minMZ = this.lookup_table[min_Py];
                    if (max_Py < this.lookup_table.Length - 1)
                        max_Py++;
                    this.new_maxMZ = this.lookup_table[max_Py];
                }

                this.new_minSpectrum = this.current_minSpectrum + (min_Px / this.current_MZPerPixelX);
                this.new_maxSpectrum = this.current_minSpectrum + (max_Px / this.current_MZPerPixelX);
                if (this.new_maxSpectrum - this.new_minSpectrum < MIN_GRAPHED_SPECTRA)
                {
                    this.new_minSpectrum -= (MIN_GRAPHED_SPECTRA - (this.new_maxSpectrum - this.new_minSpectrum)) / 2;
                    this.new_maxSpectrum = this.new_minSpectrum + MIN_GRAPHED_SPECTRA;
                }

                // MessageBox.Show(this.current_minSpectrum.ToString()+"(" + this.new_minSpectrum.ToString() + ", " + this.new_minMZ.ToString() + ")(" + this.new_maxSpectrum.ToString() + ", " + this.new_maxMZ.ToString() + ")");

                // save new zoom...
                _zoomX.Add(new Point(new_minSpectrum, new_maxSpectrum));
                _zoomBin.Add(new dPoint(this.new_minMZ, this.new_maxMZ));

                // this.current_maxBin = this.new_maxBin;
                //  this.current_minBin = this.new_minBin;

                this.flag_update2DGraph = true;
            }
        }

        protected override void invoke_axisTOF()
        {
            try
            {
                this.flag_enterBinRange = true;

                double increment_MZ = (this.current_maxMZ - this.current_minMZ) / (double)(this.pb_2DMap.Height);

                this.num_maxBin.Value = Convert.ToDecimal(this.current_maxMZ);
                this.num_minBin.Value = Convert.ToDecimal(this.current_minMZ);
                this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((this.current_maxMZ - this.current_minMZ) / 3);

                // this.plot_TOF.Update();
                // this.plot_TOF.Enabled = false;
                this.plot_TOF.PlotX(tic_TOF, this.current_minMZ, increment_MZ);

                this.flag_enterBinRange = false;
            }
            catch (Exception ex)
            {
                // MessageBox.Show("Plot Axis Mobility: " + ex.StackTrace.ToString() + "\n\n" + ex.ToString());
                //this.plot_TOF.PlotAreaColor = Color.OrangeRed;
                Thread.Sleep(100);
                this.flag_update2DGraph = true;
            }
        }

        protected override void invoke_axisMobility()
        {
            double increment_MobilityValue;

            //this.plot_Mobility.ClearRange();
            //MessageBox.Show("invoke_axisMobility");
            try
            {
                plot_Mobility.HitSize = (current_MZPerPixelX >= 1) ? new SizeF(1.0f, 2 * plot_Mobility_HEIGHT) : new SizeF(-current_MZPerPixelX, 2 * plot_Mobility.Height);

                plot_Mobility.XMax = this.pb_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;

                // these values are used to prevent the values from changing during the plotting... yikes!
                increment_MobilityValue = 1.0;
                this.plot_Mobility.PlotY(tic_Mobility, 0, this.current_maxSpectrum - this.current_minSpectrum + 1, this.current_minSpectrum, increment_MobilityValue);

                // set min and max here, they will not adjust to zooming
                this.flag_enterMobilityRange = true; // prevent events form occurring.
                this.num_minMobility.Value = Convert.ToDecimal(this.current_minSpectrum);

#if false
                this.hsb_2DMap.Maximum = this.raw_maxSpectrum - (this.current_maxSpectrum - this.current_minSpectrum);
                this.vsb_2DMap.Maximum = (int) (this.raw_maxMZ - (this.current_maxMZ - this.current_minMZ));
                this.hsb_2DMap.Minimum = this.raw_minSpectrum;
                this.vsb_2DMap.Minimum = (int) this.raw_minMZ;

                this.hsb_2DMap.Value = this.current_minSpectrum;
                if (this.vsb_2DMap.Maximum > this.current_minMZ)
                    this.vsb_2DMap.Value = this.vsb_2DMap.Maximum - (int) this.current_minMZ;
                else
                    this.vsb_2DMap.Value = (int) this.raw_minMZ;

                this.hsb_2DMap.SmallChange = 30; // (this.current_maxMobility - this.current_minMobility) / 5;
                this.hsb_2DMap.LargeChange = 60; // (this.current_maxMobility - this.current_minMobility) * 4 / 5;
                this.vsb_2DMap.SmallChange = (int) ((this.current_maxMZ - this.current_minMZ) / 5);
                this.vsb_2DMap.LargeChange = (int) ((this.current_maxMZ - this.current_minMZ) * 4 / 5);
#endif

                this.num_minMobility.Maximum = this.num_maxMobility.Maximum = this.raw_maxSpectrum;
                if (this.current_maxSpectrum > this.frame_maxSpectrum)
                {
                    this.current_maxSpectrum = this.frame_maxSpectrum;
                    this.flag_update2DGraph = true;
                }
                this.num_maxMobility.Value = Convert.ToDecimal(this.current_maxSpectrum);
                this.num_minMobility.Increment = this.num_maxMobility.Increment = Convert.ToDecimal((this.current_maxSpectrum - this.current_minSpectrum) / 3);

                this.plot_Mobility.Update();
                this.flag_enterMobilityRange = false; // OK, clear this flag to make the controls usable
            }
            catch (Exception ex)
            {
                MessageBox.Show("Plot Axis Mobility: " + ex.StackTrace.ToString() + "\n\n" + ex.ToString());
                // this.plot_Mobility.PlotAreaColor = Color.Orange;
                Thread.Sleep(100);
                this.flag_update2DGraph = true;
            }
        }


        protected void GraphFrame(int[][] frame_data)
        {
            lock (this.lock_graphing)
            {
                this.flag_selection_drift = false;

                string exp_date = "";

                // Initialize boundaries
                new_minMobility = 0;
                new_maxMobility = this.current_maxSpectrum;
                new_minBin = 0;
                new_maxBin = (int)this.current_maxMZ;

                maximum_Mobility = new_maxMobility;
                maximum_Bins = new_maxBin;

                this.num_minMobility.Minimum = -100;
                this.num_maxMobility.Maximum = 100000;

                // set min and max here, they will not adjust to zooming
                this.flag_enterMobilityRange = true; // prevent events form occurring.
                this.num_minMobility.Value = Convert.ToDecimal(new_minMobility);
                this.num_maxMobility.Value = Convert.ToDecimal(new_maxMobility);
                this.flag_enterMobilityRange = false; // OK, clear this flag to make the controls usable

                this.current_minBin = this.new_minBin;
                this.current_maxBin = this.new_maxBin;
                this.current_minMobility = this.new_minMobility;
                this.current_maxMobility = this.new_maxMobility;

                // frame is created, allow frame cycling.
                this.flag_update2DGraph = true;

                this.vsb_2DMap.Minimum = 0;
                this.vsb_2DMap.Maximum = this.maximum_Bins;
                //this.vsb_2DMap.SmallChange = this.current_valuesPerPixelY * 1000;

                this.hsb_2DMap.Minimum = 0;
                this.hsb_2DMap.Maximum = 0;
                //this.hsb_2DMap.SmallChange = this.current_MZPerPixelX * 1000;

                this.num_maxMobility.Minimum = Convert.ToDecimal(0);
                this.num_maxMobility.Maximum = Convert.ToDecimal(this.raw_maxSpectrum);
                this.num_minMobility.Minimum = Convert.ToDecimal(0);
                this.num_minMobility.Maximum = Convert.ToDecimal(this.raw_maxSpectrum);

                this.Text = Path.GetFileNameWithoutExtension(this.file_BIN);

                //this.tabpages_FrameInfo.Top = this.num_minBin.Top + this.num_minBin.Height;
                // this.Height = 

                if (this.Height > Screen.PrimaryScreen.Bounds.Height - 40)
                    this.Height = Screen.PrimaryScreen.Bounds.Height - 40;
                else if (this.Top + this.Height > Screen.PrimaryScreen.Bounds.Height - 40)
                {
                    this.Top = (Screen.PrimaryScreen.Bounds.Height - this.Height - 40) / 2;
                }

                this.AutoScrollPosition = new Point(0, 0);
#if RESIZE
                try
                {
                    this.ResizeThis();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Resize Error" + ex.ToString());
                }
#endif
                this.Show();

                if (this.thread_GraphFrame == null)
                {
                    // thread GraphFrame
                    this.thread_GraphFrame = new Thread(new ThreadStart(this.tick_GraphRAW));
                    this.thread_GraphFrame.Priority = System.Threading.ThreadPriority.Normal;
                    this.thread_GraphFrame.Start();
                }
            }
        }


        protected override void Generate2DIntensityArray(int frame_index)
        {
            // Determine the frame size
            this.get_ViewableIntensities();
            //            this.XCaliburAccess();
            if (this.flag_Closing)
                return;

            this.xAxis_Mobility.Caption = "Spectra";

            this.yAxis_TOF.Caption = "m/z";

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void get_ViewableIntensities()
        {
            if (this.flag_collecting_data || this.flag_Closing)
                return;
            this.flag_collecting_data = true;

            int i;
            double x1 = 0, x2 = 0;
            double y1 = 0, y2 = 0;
            int pixel;
            int data_width = this.current_maxSpectrum - this.current_minSpectrum + 1;
            int data_height = this.tabpages_Main.Height - 300;
            int index_raw;
            double interpolated_intensity = 0;
            int start_pixel;

            double slope, intercept;

            bool flag_maxdatapoint = false;
            double max_datapoint = 0.0;

            this.current_MZPerPixelY = (this.current_maxMZ - this.current_minMZ) / (double)data_height;
            lookup_table = new double[data_height];
            for (i = 0; i < data_height; i++)
            {
                lookup_table[i] = this.current_minMZ + (((double)i) * this.current_MZPerPixelY);
            }

            if (data_width > num_spectra)
            {
                if (num_spectra > MAX_GRAPHED_SPECTRA)
                    data_width = MAX_GRAPHED_SPECTRA;
                else
                    data_width = num_spectra;
            }
            else
            {
                this.current_MZPerPixelX = (int)((this.slider_ColorMap.Left - this.pb_2DMap.Left - 30) / data_width);
                if (this.current_MZPerPixelX == 0)
                    this.current_MZPerPixelX = 1;
            }

            // since we don't have bins, we are going to plot based on pixel.
            this.data_2D = new int[(int)data_width][];
            for (i = 0; i < data_width; i++)
            {
                this.data_2D[i] = new int[(int)data_height];
            }

            int spectrum = this.current_minSpectrum;
            int array_size;

            //MessageBox.Show(data_width.ToString() + "  " + this.pb_2DMap.Width + " = " + this.current_minSpectrum.ToString() + " < " + this.current_maxSpectrum.ToString());
            for (i = this.current_minSpectrum; i <= this.current_maxSpectrum; i++)
            {
                array_size = this.data_point[i].GetLength(1);
                if (array_size <= 0)
                    continue;

                this.data_2D[i - this.current_minSpectrum][0] = 0;

                // find the min_mz in the spectra
                index_raw = 0;
                start_pixel = 1;
                while ((index_raw+1 < array_size) && (lookup_table[0] > data_point[i][0, index_raw+1]))
                    index_raw++;
                if (index_raw >= array_size)
                    continue;
                while ((start_pixel + 1 < lookup_table.Length) && (lookup_table[start_pixel + 1] < data_point[i][0, index_raw+1]))
                    start_pixel++;

                x1 = 0.0;
                y1 = 0.0;
                this.data_maxIntensity = 0;
                for (pixel = start_pixel; pixel < data_height-1; pixel++)
                {
                    flag_maxdatapoint = false;
                    max_datapoint = 0;
                    while ((index_raw + 2 < array_size) && (lookup_table[pixel] > this.data_point[i][0, index_raw + 1]))
                    {
                        flag_maxdatapoint = true;
                        max_datapoint += this.data_point[i][1, index_raw + 1];
                        index_raw++;
                        if (index_raw >= array_size)
                            break;
                    }

                    if (index_raw + 1 < array_size)
                    {
                        x1 = this.data_point[i][0, index_raw];
                        x2 = this.data_point[i][0, index_raw + 1];
                        y1 = this.data_point[i][1, index_raw];
                        y2 = this.data_point[i][1, index_raw + 1];
                    }

                    if (flag_maxdatapoint)
                    {
                        this.data_2D[i - this.current_minSpectrum][pixel] = (int)max_datapoint;
                    }
                    else
                    {
                        // y = mx - b
                        slope = (y2 - y1) / (x2 - x1);
                        intercept = (slope * x2) - y2;
                        interpolated_intensity = (int)((slope * lookup_table[pixel]) - intercept);

                        if (interpolated_intensity > 0)
                            this.data_2D[i - this.current_minSpectrum][pixel] = (int)interpolated_intensity;
                    }
                }
            }


            try
            {
                int sel_min = (this.selection_min_drift - this.current_minSpectrum);
                int sel_max = (this.selection_max_drift - this.current_minSpectrum);

                int current_scan;
                int bin_value;
                this.data_maxIntensity = 0;
                this.data_driftTIC = new double[data_width];
                this.data_tofTIC = new double[data_height];
                for (current_scan = 0; current_scan < data_width; current_scan++)
                    for (bin_value = 0; bin_value < data_height; bin_value++)
                    {
                        if (this.inside_Polygon(current_scan, bin_value))
                        {
                            this.data_driftTIC[current_scan] += this.data_2D[current_scan][bin_value];

                            if (!flag_selection_drift || ((current_scan >= sel_min) && (current_scan <= sel_max)))
                                this.data_tofTIC[bin_value] += data_2D[current_scan][bin_value];

                            if (this.data_2D[current_scan][bin_value] > this.data_maxIntensity)
                            {
                                this.data_maxIntensity = this.data_2D[current_scan][bin_value];
                                this.posX_MaxIntensity = current_scan;
                                this.posY_MaxIntensity = bin_value;
                            }
                        }
                    }

                this.plot_axisMobility(this.data_driftTIC);
                this.plot_axisTOF(this.data_tofTIC);
                //  this.plot_Mobility.PlotY(this.data_driftTIC, (double)0, 1.0);
                // this.plot_TOF.PlotX(this.data_tofTIC, current_minMZ, this.current_MZPerPixelY); //wfd
            }
            catch (Exception ex)
            {
                MessageBox.Show("4: " + ex.ToString());
            }
            // this.data_maxIntensity = 800;
#if false

            MessageBox.Show("test");

            FileStream fs = new FileStream(@"C:\IonMobilityData\test.csv", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            try
            {
                i = 0;
                rawData.GetMassListFromScanNum(
                  ref i,
                  noFilter,
                  effective_CutOff_Type,
                  effective_CutOff,
                  allPeaksReturned,
                  doNotCentroid,
                  ref outCentroidWidth,
                  ref outRawData,
                  ref outPeakFlags,
                  ref outArraySize);

                dp = (System.Double[,])outRawData;

                for (int j = 0; j < data_height; j++)
                {
                    sw.WriteLine(lookup_table[j] + ", " + this.data_2D[0][j]);
                }
                for (int j = 0; j < dp.GetLength(1); j++)
                {
                    sw.WriteLine(dp[0, j].ToString() + ", " + dp[1, j].ToString());
                }
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            MessageBox.Show("test done");
#endif
            try
            {
                this.pb_2DMap.Width = (this.data_2D.Length * this.current_MZPerPixelX) + 1;
                this.pb_2DMap.Height = this.data_2D[0].Length;
                this.pb_2DMap.Size = new Size(this.pb_2DMap.Width, this.pb_2DMap.Height);

                // Identify the picture frame with my new Bitmap.
                if (this.pb_2DMap.Image == null)
                {
                    this.pb_2DMap.Image = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                    this.bitmap = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                }

                // Spit out the data to screen
                this.DrawBitmap(this.data_2D, this.data_maxIntensity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            // align everything
            if (this.current_valuesPerPixelY > 0)
            {
                this.plot_TOF.Height = this.pb_2DMap.Height + this.plot_TOF.Height - this.plot_TOF.PlotAreaBounds.Height;
                this.plot_TOF.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4;
            }
            else
            {
                this.plot_TOF.Height = this.pb_2DMap.Height + this.plot_TOF.Height - this.plot_TOF.PlotAreaBounds.Height + this.current_valuesPerPixelY;
                this.plot_TOF.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 - this.current_valuesPerPixelY / 2;
            }

            this.num_minBin.Top = this.plot_TOF.Top + this.plot_TOF.Height + 4;
            this.vsb_2DMap.Height = this.pb_2DMap.Height;

            this.pb_2DMap.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 + this.plot_TOF.PlotAreaBounds.Top;
            this.hsb_2DMap.Top = this.pb_2DMap.Top - this.hsb_2DMap.Height;
            this.vsb_2DMap.Top = this.pb_2DMap.Top;

            if ((this.plot_TOF.Top + this.plot_TOF.Height) < (this.pb_2DMap.Top + this.pb_2DMap.Height + 16))
                this.plot_Mobility.Top = this.pb_2DMap.Top + this.pb_2DMap.Height + 16;
            else
                this.plot_Mobility.Top = this.plot_TOF.Top + this.plot_TOF.Height;
            this.num_minMobility.Top = this.num_maxMobility.Top = this.plot_Mobility.Top + this.plot_Mobility.Height + 4;
            this.cb_MaxScanValue.Top = this.plot_Mobility.Top + this.plot_Mobility.Height - this.cb_MaxScanValue.Height - 2;

            if (this.current_MZPerPixelX == 1)
            {
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width;
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width;
            }
            else
            {
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width - this.current_MZPerPixelX;
                this.plot_Mobility.Left = this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.current_MZPerPixelX / 2;
            }

            this.num_minMobility.Left = this.plot_Mobility.Left;
            this.num_maxMobility.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.num_maxMobility.Width; //- (this.plot_Mobility.PlotAreaBounds.Width - this.pb_2DMap.Width)
            this.cb_MaxScanValue.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.cb_MaxScanValue.Width - 3;

            this.pb_2DMap.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.plot_Mobility.PlotAreaBounds.Left;
            this.hsb_2DMap.Left = this.pb_2DMap.Left;

            this.hsb_2DMap.Width = this.pb_2DMap.Width;
            this.vsb_2DMap.Left = this.pb_2DMap.Left + this.pb_2DMap.Width;

            this.tabpages_FrameInfo.Top = this.tab_DataViewer.Height - this.tabpages_FrameInfo.Height - 6;

            this.flag_collecting_data = false;
        }

        protected override void pb_2DMap_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            int w;
            int xl;
            int xwidth;
            int min_mobility;

            if (pb_2DMap.Image == null)
                return;

            // DrawImage seems to make the selection box more responsive.
            if (!this.rb_CompleteChromatogram.Checked && !this.rb_PartialChromatogram.Checked)
                e.Graphics.DrawImage(pb_2DMap.Image, 0, 0);

            if (_mouseDragging) //&& !toolBar1.Buttons[0].Pushed)
                this.DrawRectangle(e.Graphics, _mouseDownPoint, _mouseMovePoint);

#if false
            //   this.plot_Height = this.data_2D[0].Length;
            //   this.plot_Width = this.data_2D.Length;

            this.num_FrameRange.Top = this.hsb_2DMap.Top - this.num_FrameRange.Height - 4;
            this.num_FrameRange.Left = this.vsb_2DMap.Left + this.vsb_2DMap.Width - this.num_FrameRange.Width;
            this.lbl_FrameRange.Top = this.num_FrameRange.Top + 2;
            this.lbl_FrameRange.Left = this.num_FrameRange.Left - this.lbl_FrameRange.Width - 2;
#endif

            // this section draws the highlight on the plot.
            if (this.flag_selection_drift)
            {
                min_mobility = this.new_minSpectrum;

                w = this.pb_2DMap.Width / (this.current_maxSpectrum - this.current_minSpectrum + 1);
                xl = ((this.selection_min_drift - this.current_minSpectrum) * w);
                xwidth = (this.selection_max_drift - this.selection_min_drift + 1) * w;

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(145, 111, 111, 126)), xl, 0, xwidth, this.pb_2DMap.Height);
            }

            this.draw_Corners(e.Graphics);
        }

        protected override void export_IntensityMatrix(string filename)
        {
            int i;

            double mob_width = this.data_2D.Length;
            double[] drift_axis = new double[(int)mob_width];

            double tof_height = this.data_2D[0].Length;
            double[] tof_axis = new double[(int)tof_height];

            double increment;

            increment = (this.current_maxMobility - this.current_minMobility) / mob_width;
            drift_axis[0] = this.current_minMobility;
            for (i = 1; i < mob_width; i++)
                drift_axis[i] = (drift_axis[i - 1] + increment);

            increment = (this.current_maxMZ - this.current_minMZ) / tof_height;

            tof_axis[0] = this.current_minMZ;
            for (i = 1; i < tof_height; i++)
            {
                tof_axis[i] = (tof_axis[i - 1] + (float)increment);
            }

            UIMF_File.Utilities.TextExport tex = new UIMF_File.Utilities.TextExport();
            tex.Export(filename, "m/z", this.data_2D, drift_axis, tof_axis);
        }

        // /////////////////////////////////////////////////////////////////////
        // UpdateCursorReading()
        //
        protected override void UpdateCursorReading(System.Windows.Forms.MouseEventArgs e)
        {
            double mobility = (current_MZPerPixelX == 1 ? e.X : this.current_minSpectrum + (e.X / this.current_MZPerPixelX));

            this.lbl_CursorMobility.Text = mobility.ToString();
            this.lbl_CursorScanTime.Text = mobility.ToString("0.0000");

            if (this.data_2D == null)
                return;
            // time_offset = this.imfReader.Experiment_Properties.TimeOffset;

            try
            {
                // TOF is quite easy.  Using the current_valuesPerPixelY which is TOF related.
                double mz_pixel = this.current_minMZ + ((this.pb_2DMap.Height - e.Y - 1) * this.current_MZPerPixelY);
                this.lbl_CursorMZ.Text = mz_pixel.ToString();

                if (current_valuesPerPixelY < 0)
                {
                    this.plot_TOF.Refresh();

                    Graphics g = this.plot_TOF.CreateGraphics();
                    int y_step = ((e.Y / current_valuesPerPixelY) * current_valuesPerPixelY) + this.plot_TOF.PlotAreaBounds.Top;
                    Pen dp = new Pen(new SolidBrush(Color.Red), 1);
                    dp.DashStyle = DashStyle.Dot;
                    g.DrawLine(dp, this.plot_TOF.PlotAreaBounds.Left, y_step, this.plot_TOF.PlotAreaBounds.Left + this.plot_TOF.PlotAreaBounds.Width, y_step);
                    int amp_index = (this.pb_2DMap.Height - e.Y - 1) / (-current_valuesPerPixelY);
                    string amplitude = this.data_tofTIC[amp_index].ToString();
                    Font amp_font = new Font("lucida", 8, FontStyle.Regular);
                    int left_str = this.plot_TOF.PlotAreaBounds.Left - (int)g.MeasureString(amplitude, amp_font).Width - 10;

                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), left_str, y_step - 7, this.plot_TOF.PlotAreaBounds.Left - 1, y_step - 7);
                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), left_str, y_step - 7, left_str, y_step + 6);
                    g.FillRectangle(new SolidBrush(Color.GhostWhite), left_str + 1, y_step - 6, this.plot_TOF.PlotAreaBounds.Left - left_str - 1, 13);
                    g.DrawLine(new Pen(new SolidBrush(Color.White), 1), left_str + 1, y_step + 7, this.plot_TOF.PlotAreaBounds.Left - 1, y_step + 7);

                    g.DrawString(amplitude, amp_font, new SolidBrush(Color.Red), left_str + 5, y_step - 6);
                }
            }
            catch (Exception ex)
            {
                // This occurs when you are zooming into the plot and go off the edge to the
                // top.  Try it...  perfect place to ignore an error

                // MessageBox.Show("UpdateCursorReading:  " + ex.ToString());
                // Console.WriteLine(ex.ToString());
            }
        }


        /**********************************************************************
        * This is where the work is done
        */
        [STAThread]
        protected void tick_GraphRAW()
        {
            while (this.flag_Alive)
            {
                try
                {
                    while (this.flag_update2DGraph && this.flag_Alive)
                    {
                        this.flag_update2DGraph = false;

                        if ((this.current_minSpectrum != this.new_minSpectrum) ||
                            (this.current_maxSpectrum != this.new_maxSpectrum) ||
                            (this.current_minMZ != this.new_minMZ) ||
                            (this.current_maxMZ != this.new_maxMZ))
                        {
                            if (this.new_minSpectrum < this.raw_minSpectrum)
                                this.current_minSpectrum = this.raw_minSpectrum;
                            else
                                this.current_minSpectrum = this.new_minSpectrum;

                            if (this.new_maxSpectrum > this.raw_maxSpectrum)
                                this.current_maxSpectrum = this.raw_maxSpectrum;
                            else
                                this.current_maxSpectrum = this.new_maxSpectrum;

                            if (this.new_maxMZ > this.raw_maxMZ)
                                this.current_maxMZ = this.raw_maxMZ;
                            else
                                this.current_maxMZ = this.new_maxMZ;
                            if (this.new_minMZ < this.raw_minMZ)
                                this.current_minMZ = this.raw_minMZ;
                            else
                                this.current_minMZ = this.new_minMZ;
                        }

                        try
                        {
                            this.Graph_2DPlot();
                        }
                        catch (System.NullReferenceException ex)
                        {
                            this.BackColor = Color.White;
                            Thread.Sleep(100);
                            this.flag_update2DGraph = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("tick_GraphFrame graph_2dplot: " + ex.ToString() + "\n\n" + ex.StackTrace.ToString());
                        }
                        // this.flag_update2DGraph = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "cycle_GraphRAW: " + ex.ToString() + "\n\n" + ex.StackTrace.ToString());
                }

                this.flag_GraphingFrame = false;
                Thread.Sleep(500);
            }
        }

        // /////////////////////////////////////////////////////
        // Map scrollbar
        //
        protected override void hsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int old_min = this.current_minMobility;
            int old_max = this.current_maxMobility;

            this.current_minSpectrum = this.new_minSpectrum = this.hsb_2DMap.Value;
            this.current_maxSpectrum = this.new_maxSpectrum = (old_max + (this.new_minSpectrum - old_min));

            this.flag_update2DGraph = true;
        }

        protected override void vsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int old_min = this.current_minBin;
            int old_max = this.current_maxBin;

            this.new_minMZ = this.vsb_2DMap.Maximum - this.vsb_2DMap.Value;
            this.new_maxMZ = (old_max + (this.new_minMZ - old_min));

            this.flag_update2DGraph = true;
        }

        // ///////////////////////////////////////////////////////////////
        // Graph_2DPlot()
        //
        private void Graph_2DPlot()
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            if (this.data_2D == (int[][])null)
            {
                MessageBox.Show("Graph_2DPlot(): data for frame is null");
                return;
            }

            this.flag_kill_mouse = true;

            lock (lock_graphing)
            {
                try
                {
                    try
                    {
                        Generate2DIntensityArray(0);
                    }
                    catch (Exception ex)
                    {
                        this.BackColor = Color.Black;
                        MessageBox.Show("Graph_2DPlot() generate2dintensityarray(): " + ex.ToString());
                    }
                    // MessageBox.Show("GraphFrame: " + this.data_2D.Length.ToString() + ", " + this.data_2D[0].Length.ToString());

                    if (this.flag_Closing)
                        return;

                    if (data_2D == null)
                        MessageBox.Show("no data");
                    // this.pb_2DMap.Width = this.data_2D.Length;
                    // this.pb_2DMap.Height = this.data_2D[0].Length;

                    this.pb_2DMap.Size = new Size(this.pb_2DMap.Width, this.pb_2DMap.Height);

                    // Identify the picture frame with my new Bitmap.
                    if (this.pb_2DMap.Image == null)
                    {
                        pb_2DMap.Image = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                        bitmap = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                    }

                    // Spit out the data to screen
                    this.DrawBitmap(this.data_2D, this.data_maxIntensity);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Graph_2DPlot:  " + ex.InnerException.ToString() + "\n" + ex.ToString());
                    Console.WriteLine(ex.ToString());
                    this.flag_update2DGraph = true;
                }
            }

            if (this.pb_2DMap.Left + this.pb_2DMap.Width + 170 > this.Width)
            {
                this.Width = this.pb_2DMap.Left + this.pb_2DMap.Width + 170;
                this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
            }

            this.slider_ColorMap.Invalidate();

            this.flag_kill_mouse = false;
        }

        // ////////////////////////////////////////////////////////////////////////////////
        // Control the mz axis
        //
        protected override void num_minBin_ValueChanged(object sender, System.EventArgs e)
        {
            double bin_diff;
            double min, max;

            if (this.flag_enterBinRange)
                return;
            this.flag_enterBinRange = true;

            if (this.num_minBin.Value >= this.num_maxBin.Value)
                this.num_maxBin.Value = Convert.ToDecimal(Convert.ToDouble(this.num_minBin.Value) + 1.0);

            try
            {
                min = Convert.ToDouble(this.num_minBin.Value);
                max = Convert.ToDouble(this.num_maxBin.Value);

                bin_diff = ((max - min) / (double)this.pb_2DMap.Height);
                new_minMZ = min;
                if (bin_diff > 0.0)
                    this.new_maxMZ = this.new_minMZ + (bin_diff * this.pb_2DMap.Height);
                else
                    this.new_maxMZ = max;

                _zoomX.Add(new Point(new_minSpectrum, new_maxSpectrum));
                _zoomBin.Add(new dPoint(new_minMZ, new_maxMZ));

                this.flag_update2DGraph = true;

                this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxBin.Value) - Convert.ToDouble(this.num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("num_minBin_ValueChanged:  " + ex.ToString());
            }

            this.flag_enterBinRange = false;
        }

        protected override void num_maxBin_ValueChanged(object sender, System.EventArgs e)
        {
            double min, max;
            double bin_diff;

            if (this.flag_enterBinRange)
                return;
            this.flag_enterBinRange = true;

            if (this.num_minBin.Value >= this.num_maxBin.Value)
                this.num_minBin.Value = Convert.ToDecimal(Convert.ToDouble(this.num_maxBin.Value) - 1.0);

            try
            {
                min = Convert.ToDouble(this.num_minBin.Value);
                max = Convert.ToDouble(this.num_maxBin.Value);

                bin_diff = ((max - min) / (double)this.pb_2DMap.Height);
                new_maxMZ = max;
                if (bin_diff > 0)
                    this.new_minMZ = new_maxMZ - (bin_diff * (double)this.pb_2DMap.Height);
                else
                    this.new_minMZ = min;

                _zoomX.Add(new Point(new_minSpectrum, new_maxSpectrum));
                _zoomBin.Add(new dPoint(new_minMZ, new_maxMZ));


                this.flag_update2DGraph = true;

                this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxBin.Value) - Convert.ToDouble(this.num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("num_maxBin_ValueChanged:  " + ex.ToString());
            }

            this.flag_enterBinRange = false;
        }

        protected override void num_Mobility_ValueChanged(object sender, System.EventArgs e)
        {
            int min, max;

            if (this.flag_enterMobilityRange)
                return;
            this.flag_enterMobilityRange = true;

            min = Convert.ToInt32(this.num_minMobility.Value);
            max = Convert.ToInt32(this.num_maxMobility.Value);

            this.num_minMobility.Increment = this.num_maxMobility.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxMobility.Value) - Convert.ToDouble(this.num_minMobility.Value)) / 4.0);

            new_maxSpectrum = max;
            new_minSpectrum = min;

            _zoomX.Add(new Point(min, max));
            _zoomBin.Add(new dPoint(new_minMZ, new_maxMZ));

            this.flag_update2DGraph = true;

            this.flag_enterMobilityRange = false;
        }

        // //////////////////////////////////////////////////////////////////
        //
        //
        protected override void show_MaxIntensity(object sender, System.EventArgs e)
        {
            int topX;
            int topY;
            int widthX;
            int widthY;

            topX = (this.posX_MaxIntensity * this.current_MZPerPixelX) - 15;
            widthX = this.current_MZPerPixelX + 30;

            if (this.current_MZPerPixelY < 0)
            {
                topY = (int)(this.pb_2DMap.Height - 15 - ((this.posY_MaxIntensity + 1) * (-this.current_MZPerPixelY)));
                widthY = (int)(-this.current_MZPerPixelY + 30);
            }
            else
            {
                topY = this.pb_2DMap.Height - 15 - this.posY_MaxIntensity;
                widthY = 30;
            }

            Graphics g = this.pb_2DMap.CreateGraphics();
            Pen p1 = new Pen(new SolidBrush(Color.Black), 3);
            g.DrawEllipse(p1, topX, topY, widthX, widthY);
            Pen p2 = new Pen(new SolidBrush(Color.White), 1);
            g.DrawEllipse(p2, topX, topY, widthX, widthY);
        }

#if false
        private void XCaliburAccess()
                {
            // SFinniganRawfile2Wrap.cpp is using the same,
            //   0x5FE970B2,0x29C3,0x11D3,{0x81,0x1D,0x00,0x10,0x4B,0x30,0x48,0x96

            //This error happend:
            //  COMException was unhandled.
            //  Retrieving the COM class factory for component with CLSID
            //  {5FE970B2-29C3-11D3-811D-00104B304896} failed due to the
            //  following error: 80040154
            //
            //  80040154 is ERROR_CLASS_NOTREGISTERED
            //
            //Reason:
            //  Project NewXCalibur/Properties/Build/General/Platform target
            //  was set to "Any". It should be set to "x86" as COM components
            //  are 32 bit only ("Any" will run as 64 bit on a 64 bit
            //  version of Windows.)
            //
#if false
            mRawfile = new XRawfile();

            string filename =
              //@"H:\toDelete\2009-09-28\BAB-BM-JVO-NE-1-5-20-T-A02.RAW";
              @"L:\tempCollection\temp19,LyrisYeast\2005-03-04,LGYeastSILACtest\Lyris-YeastSILAC_Lys8-MS3.RAW";

            mRawfile.Open(filename);

            mRawfile.SetCurrentController(0, 1); //Mass spec device, first MS device.

            int lastSpectrumNumber = -17;
            mRawfile.GetLastSpectrumNumber(ref lastSpectrumNumber);
#endif

            //Input
            //int spectrumNumber = 9213; //A "SIM" spectrum.
            int spectrumNumber = 9206; //A "Full MS" spectrum.

            //Common for the two calls
            string noFilter = null;

            int noCutOff_Type = 0;
            int noCutOff = 0;
            int allPeaksReturned = 0;
            int doNotCentroid = 0;

            //Cut-off configuration
            int effective_CutOff_Type = 0;
            int effective_CutOff = 0;

            effective_CutOff_Type = noCutOff_Type;
            effective_CutOff = noCutOff;

            //Classic call. GetMassListFromScanNum()
            for (int i=0; i<10; i++)
            {
                //int absoluteCutOff_Type = 1;
                //int minimumCutOff = 1;

                //Outputs
                int outArraySize = -1;
                Object outRawData = null; //Keep compiler happy.
                double outCentroidWidth = 0.0;
                object outPeakFlags = null; //Keep compiler happy.

                rawData.GetMassListFromScanNum(
                  ref i,
                  noFilter,
                  effective_CutOff_Type,
                  effective_CutOff,
                  allPeaksReturned,
                  doNotCentroid,
                  ref outCentroidWidth,
                  ref outRawData,
                  ref outPeakFlags,
                  ref outArraySize);

                MessageBox.Show(outArraySize.ToString());
            }

#if false
            //New call. GetMassListFromScanNum()
            {
                //mRawfile3 = new XRAWFILE2Lib.IXRawfile3();
                //mRawfile3 = new XRawfile();

                mRawfile3.Open(filename);
                mRawfile3.SetCurrentController(0, 1); //Mass spec device, first MS device.
                int lastSpectrumNumber2 = -17;
                mRawfile3.GetLastSpectrumNumber(ref lastSpectrumNumber2);

                //Output
                int outArraySize = -1;
                Object outRawData = null; //Keep compiler happy.
                double outCentroidWidth = 0.0;
                object outPeakFlags = null; //Keep compiler happy.

                //606.84 Th: highest peak in 9206, Full ms.
                //[606.00;608.50]

                string massRangeStr = "606.00-608.50";

                //Compiles, but an instance of mRawfile3 can not be created...
                mRawfile3.GetMassListRangeFromScanNum(
                  ref spectrumNumber,
                  noFilter,
                  effective_CutOff_Type,
                  effective_CutOff,
                  allPeaksReturned,
                  doNotCentroid,
                  ref outCentroidWidth,
                  ref outRawData,
                  ref outPeakFlags,
                  massRangeStr,
                  ref outArraySize);

                //Compile error:
                // Error	1	'XRAWFILE2Lib.XRawfile' does not contain a
                // definition for 'GetMassListRangeFromScanNum' and no
                // extension method 'GetMassListRangeFromScanNum' accepting a
                // first argument of type 'XRAWFILE2Lib.XRawfile' could be
                // found (are you missing a using directive or an assembly
                // reference?)
                // D:\dproj\tryouts\NewXCalibur\NewXCalibur\src\XCaliburAccess\
                // XCaliburAccess.cs	133	26	NewXCalibur
                mRawfile.GetMassListRangeFromScanNum(
                  ref spectrumNumber,
                  noFilter,
                  effective_CutOff_Type,
                  effective_CutOff,
                  allPeaksReturned,
                  doNotCentroid,
                  ref outCentroidWidth,
                  ref outRawData,
                  ref outPeakFlags,
                  massRangeStr,
                  ref outArraySize);

                int peter2 = 2;
            }
#endif
            int peter3 = 3;
        } //Constructor.
#endif
    }
}
#endif