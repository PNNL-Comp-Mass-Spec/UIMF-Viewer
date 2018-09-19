using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using UIMFLibrary;
using System.Linq;
using ZedGraph;

// ******************************************************************************************************
// * Programmer:  William Danielson
// *
// * Description:  Base object for the Int and Short Viewer.  The changes were to drastic to also
// *               include the float viewer.
// *
// * Revisions:
// *    090130 - Added the ability to do TIC Threshold Counting.  I expect to remove it or somehow prevent
// *             the code from defaulting to calculate it everytime.  Need for speed!
// *
// *
namespace UIMF_File
{
    public struct PixelData
    {
        public byte blue;
        public byte green;
        public byte red;
    }

    public unsafe partial class DataViewer : System.Windows.Forms.Form
    {
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, Int32 dwRop);

        #region Fields

        // mz==something, TOF==null
        private bool flag_display_as_TOF;

        // For drawing on the pb_2DMap
        protected bool _mouseDragging;
        protected Point _mouseDownPoint;
        protected Point _mouseMovePoint;

        protected bool flag_collecting_data = false;

        // Four elements used for Fast Pixellation
        private int pixel_width;
        private Byte* pBase = null;
        protected Bitmap bitmap;
        private Bitmap tmp_Bitmap;
        private BitmapData bitmapData = null;
        Point[] corner_2DMap = new Point[4];

        // Variables for mapping
        protected int current_valuesPerPixelX, current_valuesPerPixelY;
        protected int new_minMobility, new_maxMobility;
        protected int current_minMobility, current_maxMobility;
        protected int new_minBin, new_maxBin;
        protected int current_minBin, current_maxBin;

        private int chromatogram_valuesPerPixelX, chromatogram_valuesPerPixelY;
        private double[] chromatogram_driftTIC;
        private double[] chromatogram_tofTIC;

        // Save previous zoom points
        protected ArrayList _zoomX = new ArrayList();
        protected ArrayList _zoomBin = new ArrayList();

        //private System.Windows.Forms.Timer timer_GraphFrame;
        protected System.Threading.Thread thread_GraphFrame;
        private System.Threading.Thread thread_Calibrate;

        // Smoothing and slicing
        private bool _useDriftTime = true;
        private Point _contextMenuLocation;

        // Non-square zoom
        private ArrayList _interpolation_points = new ArrayList();
        protected bool flag_selection_drift = false;
        protected int selection_min_drift, selection_max_drift;

        private System.Drawing.Font map_font = new System.Drawing.Font("Verdana", 7);
        private System.Drawing.Brush fore_brush = new SolidBrush(Color.White);
        private System.Drawing.Brush back_brush = new SolidBrush(Color.DimGray);

        private double mean_TOFScanTime = 0.0;
        protected bool flag_enterMobilityRange = true;
        protected bool flag_enterBinRange = true;
        protected bool flag_viewMobility = true;
        protected bool flag_update2DGraph = false;
        protected bool flag_Chromatogram_Frames = false;

        private const int MIN_GRAPHED_BINS = 20;
        private const int MIN_GRAPHED_MOBILITY = 10;
        protected int maximum_Mobility = 0;
        protected int maximum_Bins = 0;

        private int minMobility_Chromatogram = 0;
        private int maxMobility_Chromatogram = 599;
        private int minFrame_Chromatogram = 0;
        private int maxFrame_Chromatogram = 499;

        protected UIMF_File.Utilities.Intensity_ColorMap slider_ColorMap;

        protected int posX_MaxIntensity = 0;
        protected int posY_MaxIntensity = 0;

        protected int[][] data_2D;
        private double[][] text_data_2D;

        // private int[] new_data_driftTIC;
        protected double[] data_driftTIC;
        // private int[] new_data_tofTIC;
        protected double[] data_tofTIC;
        protected int data_maxIntensity;

        private int[][] chromat_data;
        private int chromat_max;

        private int export_Spectra = 0;

        private const int ANCHOR_POINT_TOP = 500;
        private const int ANCHOR_POINT_LEFT = 0;

        private const int PANEL_ANCHOR_POINT_LEFT = 20;

        private const int axis_TOF_DESIRED_HEIGHT = 514;
        private const int axis_TOF_SIZE_DIFF = 26;

        private const int LEGEND_BUFFER_WIDTH = 20;

        private const int DESIRED_WIDTH_CHROMATOGRAM = 1500;

        private const int DRIFT_PLOT_LOCATION_X = -6;
        private const int DRIFT_PLOT_LOCATION_Y = -6;
        protected const int DRIFT_PLOT_WIDTH_DIFF = 12;
        private const int DRIFT_PLOT_HEIGHT_DIFF = 9;

        private const int TOF_PLOT_LOCATION_X = -6;
        private const int TOF_PLOT_LOCATION_Y = -6;
        private const int TOF_PLOT_WIDTH_DIFF = 10;
        private const int TOF_PLOT_HEIGHT_DIFF = 11;

        private const int TIC_INTENSITY_AXIS_DIST = 3;
        const int PLOT_TOF_WIDTH = 200;
        protected const int plot_Mobility_HEIGHT = 150;

        public bool flag_chromatograph_collected_PARTIAL = false;
        public bool flag_chromatograph_collected_COMPLETE = false;

        protected bool flag_GraphingFrame = false;

        protected bool flag_Alive = true;

        public bool flag_kill_mouse = false;
        protected object lock_graphing = new object();

        private UIMF_File.Utilities.ExportExperiment form_ExportExperiment;

        private int flag_MovingCorners = -1;

        protected int max_plot_width = 200;
        protected int max_plot_height = 200;

        private int current_frame_compression;

        public UIMF_File.PostProcessing pnl_postProcessing = null;

        protected bool flag_Closing = false;
        protected bool flag_FrameTypeChanged = false;

        protected bool flag_ResizeThis = false;
        protected bool flag_Resizing = false;

        private ArrayList array_Experiments;
        public UIMF_File.UIMFDataWrapper ptr_UIMFDatabase;
        private int index_CurrentExperiment = 0;

        private UIMFDataWrapper.ReadFrameType current_frame_type;
        private bool flag_isTIMS = false;

        private bool flag_isFullscreen = false;

        #endregion

        #region Construct and Dispose

        public DataViewer()
        {
            try
            {
                this.array_Experiments = new ArrayList();

                this.build_Interface(true);

                this.cb_FrameType.Items.Add("Thermo File");
                this.cb_FrameType.SelectedIndex = 0;

                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = false;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = false;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = false;
                this.elementHost_FrameSelect.Visible = false;
                this.num_TICThreshold.Visible = false;
                this.btn_TIC.Visible = false;

                // TODO: //this.plot_TOF.ClearData();
                // TODO: //this.plot_Mobility.ClearData();

                this.lbl_FrameRange.Visible = false;
                this.num_FrameRange.Visible = false;

                this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("DataViewer(): " + ex.ToString());
            }
        }

        public DataViewer(string uimf_file, bool flag_enablecontrols)
        {
            this.array_Experiments = new ArrayList();

            try
            {
                this.ptr_UIMFDatabase = new UIMFDataWrapper(uimf_file);
                this.array_Experiments.Add(this.ptr_UIMFDatabase);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            this.current_minBin = 0;
            this.current_maxBin = this.maximum_Bins = this.ptr_UIMFDatabase.UimfGlobalParams.Bins;

            try
            {
                this.build_Interface(flag_enablecontrols);
            }
            catch (Exception ex)
            {
                MessageBox.Show("failed to build interface()\n\n" + ex.ToString());
            }

            this.pnl_InstrumentSettings.set_defaultFragmentationVoltages(this.ptr_UIMFDatabase.GetDefaultFragVoltages());

            for (int i = 0; i < 5; i++)
                this.cb_FrameType.Items.Add(this.ptr_UIMFDatabase.FrameTypeDescription(i));

            //this.slide_FrameSelect.Range = new NationalInstruments.UI.Range(0, this.ptr_UIMFDatabase.UIMF_GlobalParams.NumFrames);
            this.slide_FrameSelect.Minimum = 0;
            this.slide_FrameSelect.Maximum = this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames;

            this.current_minBin = 0;
            this.current_maxBin = 10;

            this.lb_DragDropFiles.Items.Add(this.ptr_UIMFDatabase.UimfDataFile);
            this.cb_ExperimentControlled.Items.Add(Path.GetFileName(this.ptr_UIMFDatabase.UimfDataFile));
            this.cb_ExperimentControlled.SelectedIndex = 0;

            this.cb_FrameType.SelectedIndex = (int)this.ptr_UIMFDatabase.CurrentFrameType;
            this.Filter_FrameType(this.ptr_UIMFDatabase.CurrentFrameType);
            this.ptr_UIMFDatabase.CurrentFrameIndex = 0;

            this.ptr_UIMFDatabase.SetCurrentFrameType(current_frame_type, true);
            this.cb_FrameType.SelectedIndexChanged += this.cb_FrameType_SelectedIndexChanged;

            Generate2DIntensityArray();
            this.GraphFrame(this.data_2D, flag_enablecontrols);

            if (!string.IsNullOrWhiteSpace(this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.InstrumentName, "")))
            {
                this.flag_isTIMS = (this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.InstrumentName, "").StartsWith("TIMS") ? true : false);
                if (this.flag_isTIMS)
                    this.plot_Mobility.set_TIMSRamp(this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.a2, this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.b2,
                        this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.c2, this.ptr_UIMFDatabase.UimfFrameParams.Scans,
                        (int)(7500000.0 / this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength))); // msec gap
            }
            else
                this.flag_isTIMS = false;

            this.num_TICThreshold.Visible = false;
            this.btn_TIC.Visible = false;

            if (this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames > DESIRED_WIDTH_CHROMATOGRAM)
                this.num_FrameCompression.Value = this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames / DESIRED_WIDTH_CHROMATOGRAM;
            else
                this.num_FrameCompression.Value = 1;
            this.current_frame_compression = Convert.ToInt32(this.num_FrameCompression.Value);

            this.Width = this.pnl_2DMap.Left + this.ptr_UIMFDatabase.UimfFrameParams.Scans + 170;

            this.pnl_postProcessing.InitializeCalibrants(1, this.ptr_UIMFDatabase.UimfFrameParams.CalibrationSlope, this.ptr_UIMFDatabase.UimfFrameParams.CalibrationIntercept);

            this.pnl_postProcessing.tb_SaveDecodeFilename.Text = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);
            this.pnl_postProcessing.tb_SaveDecodeDirectory.Text = Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile);

            if (this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth != .25)
                this.pnl_postProcessing.gb_Compress4GHz.Hide();
            else
            {
                this.pnl_postProcessing.btn_Compress1GHz.Click += this.btn_Compress1GHz_Click;
                this.pnl_postProcessing.tb_SaveCompressFilename.Text = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);
                this.pnl_postProcessing.tb_SaveCompressDirectory.Text = Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            this.flag_Alive = false;
            this.flag_Closing = true;

            RegistrySave(Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
            this.pnl_postProcessing.Save_Registry();

            if (this.flag_CinemaPlot)
            {
                this.StopCinema();
                Thread.Sleep(300);
            }

            this.AllowDrop = false;
            this.flag_update2DGraph = false;

            while (this.flag_collecting_data)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (disposing)
            {
                ptr_UIMFDatabase.Dispose();

                if (components != null)
                {
                    components.Dispose();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            base.Dispose(disposing);
        }

        private void IonMobilityDataView_Closed(object sender, System.EventArgs e)
        {
            RegistrySave(Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
            ptr_UIMFDatabase.Dispose();
        }

        #endregion

        #region UI Setup

        private void build_Interface(bool flag_enablecontrols)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            this.pb_Shrink.Hide();
            this.pb_Expand.Hide();

            this.tabpages_Main.Top = (this.tab_DataViewer.ClientSize.Height - this.tabpages_Main.Height)/2;

            this.lb_DragDropFiles.Visible = false;
            this.cb_Exclusive.Visible = false;

            this.pb_PlayDownOut.Visible = false;
            this.pb_PlayDownIn.Visible = false;
            this.pb_PlayUpOut.Visible = false;
            this.pb_PlayUpIn.Visible = false;

            this.pnl_InstrumentSettings = new UIMF_DataViewer.InstrumentSettings();
            this.tab_InstrumentSettings.Controls.Add(this.pnl_InstrumentSettings);
            this.pnl_InstrumentSettings.Top = 0;
            this.pnl_InstrumentSettings.Left = 0;
            this.pnl_InstrumentSettings.Width = Screen.PrimaryScreen.Bounds.Width;
            this.pnl_InstrumentSettings.Height = Screen.PrimaryScreen.Bounds.Height;

            this.pnl_postProcessing = new PostProcessing(MainKey);
            this.pnl_postProcessing.Left = 0;
            this.pnl_postProcessing.Top = 0;

            this.tab_PostProcessing.Controls.Add(this.pnl_postProcessing);

            this.slider_ColorMap = new UIMF_File.Utilities.Intensity_ColorMap();
            this.tab_DataViewer.Controls.Add(this.slider_ColorMap);
            this.slider_PlotBackground = new UIMF_File.Utilities.GrayScaleSlider(this.pb_SliderBackground);
            this.tab_DataViewer.Controls.Add(this.slider_PlotBackground);

            menuItem_UseDriftTime.Checked = !_useDriftTime;
            menuItem_UseScans.Checked = _useDriftTime;

            this.AutoScroll = false;

            SetupPlots();

            this.plot_TOF.Left = 0;
            this.plot_TOF.Top = 0;

            //this.slider_PlotBackground.btn_GreyValue.MouseUp += new MouseEventHandler( this.slider_Background_MouseUp );
            this.slider_PlotBackground.btn_GreyValue.Move += this.slider_Background_Move;

            // starts with the mobility view
            this.flag_viewMobility = true;
            this.menuItem_Mobility.Checked = true;
            this.menuItem_ScanTime.Checked = false;

            // start the heartbeat
            this.slide_FrameSelect.Value = 0;

            // default values in the calibration require no interface
            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();

            this.pb_PlayLeftIn.SendToBack();
            this.pb_PlayLeftOut.BringToFront();
            this.pb_PlayRightIn.SendToBack();
            this.pb_PlayRightOut.BringToFront();
            this.elementHost_FrameSelect.SendToBack();

            this.lbl_FramesShown.Hide();

            //this.AllowDrop = true;

            Thread.Sleep(200);
            this.Show();
            this.menuItem_ScanTime.PerformClick();

            if (flag_enablecontrols)
            {
                this.menuItem_Time_driftTIC.Checked = true;
                this.menuItem_Frame_driftTIC.Checked = false;

                this.menuItem_SelectionCorners.Click += this.menuItem_SelectionCorners_Click;
                this.menuItem_ScanTime.Click += this.ScanTime_ContextMenu;
                this.menuItem_Mobility.Click += this.Mobility_ContextMenu;
                this.menuItem_ExportCompressed.Click += this.menuItem_ExportCompressed_Click;
                this.menuItem_ExportComplete.Click += this.menuItem_ExportComplete_Click;
                this.menuItem_ExportAll.Click += this.menuItem_ExportAll_Click;
                this.menuItem_SuperFrame.Click += this.menuItem_SuperFrame_Click;
                this.menuItem_SuperExperiment.Click += this.menuItem_SuperExperiment_Click;
                this.menuItem_CopyToClipboard.Click += this.menuItem_CopyToClipboard_Click;
                this.menuItem_CaptureExperimentFrame.Click += this.menuItem_CaptureExperimentFrame_Click;
                this.menuItem_SaveIMF.Click += this.menuitem_SaveIMF_Click;
                this.menuItem_WriteUIMF.Click += this.menuitem_WriteUIMF_Click;
                this.menuItem_UseScans.Click += this.menuItem_UseScans_Click;
                this.menuItem_UseDriftTime.Click += this.menuItem_UseDriftTime_Click;
                this.menuItem_Exportnew_driftTIC.Click += this.menuItem_ExportDriftTIC_Click;
                this.menuItem_Frame_driftTIC.Click += this.menuItem_Frame_driftTIC_Click;
                this.menuItem_Time_driftTIC.Click += this.menuItem_Time_driftTIC_Click;
                this.menuItem_TOFExport.Click += this.menuItem_TOFExport_Click;
                this.menuItem_TOFMaximum.Click += this.menuItem_TOFMaximum_Click;
                this.menuItemZoomFull.Click += this.ZoomContextMenu;
                this.menuItemZoomPrevious.Click += this.ZoomContextMenu;
                this.menuItemZoomOut.Click += this.ZoomContextMenu;
                this.menuItem_MaxIntensities.Click += this.menuItem_TOFMaximum_Click;
                this.menuItemConvertToMZ.Click += this.ConvertContextMenu;
                this.menuItemConvertToTOF.Click += this.ConvertContextMenu;

                this.pnl_2DMap.DoubleClick += this.pnl_2DMap_DblClick;
                this.pnl_2DMap.MouseLeave += this.pnl_2DMap_MouseLeave;
                this.pnl_2DMap.MouseMove += this.pnl_2DMap_MouseMove;
                this.pnl_2DMap.MouseDown += this.pnl_2DMap_MouseDown;
                this.pnl_2DMap.Paint += this.pnl_2DMap_Paint;
                this.pnl_2DMap.MouseUp += this.pnl_2DMap_MouseUp;

                this.plot_Mobility.ContextMenu = contextMenu_driftTIC;
                this.plot_Mobility.RangeChanged += this.OnPlotTICRangeChanged;
                this.pb_PlayRightIn.Click += this.pb_PlayRightIn_Click;
                this.pb_PlayLeftOut.Click += this.pb_PlayLeftOut_Click;
                this.pb_PlayLeftIn.Click += this.pb_PlayLeftIn_Click;
                this.pb_PlayRightOut.Click += this.pb_PlayRightOut_Click;
                this.num_FrameIndex.ValueChanged += this.num_FrameIndex_ValueChanged;
                this.cb_EnableMZRange.CheckedChanged += this.cb_EnableMZRange_CheckedChanged;
                this.num_MZ.ValueChanged += this.num_MZ_ValueChanged;
                this.num_PPM.ValueChanged += this.num_PPM_ValueChanged;
                this.lbl_FramesShown.Click += this.lbl_FramesShown_Click;
                this.btn_setCalDefaults.Click += this.btn_setCalDefaults_Click;

                this.num_minMobility.ValueChanged += this.num_Mobility_ValueChanged;
                this.num_maxMobility.ValueChanged += this.num_Mobility_ValueChanged;
                this.num_maxBin.ValueChanged += this.num_maxBin_ValueChanged;
                this.num_minBin.ValueChanged += this.num_minBin_ValueChanged;
                this.plot_TOF.ContextMenu = contextMenu_TOF;

                this.rb_CompleteChromatogram.CheckedChanged += this.rb_CompleteChromatogram_CheckedChanged;
                this.rb_PartialChromatogram.CheckedChanged += this.rb_PartialChromatogram_CheckedChanged;
                this.num_FrameCompression.ValueChanged += this.num_FrameCompression_ValueChanged;

                this.btn_TIC.Click += this.btn_TIC_Click;

                this.num_FrameRange.ValueChanged += this.num_FrameRange_ValueChanged;
                this.slide_FrameSelect.ValueChanged += this.slide_FrameSelect_ValueChanged;

                this.vsb_2DMap.Scroll += this.vsb_2DMap_Scroll;
                this.hsb_2DMap.Scroll += this.hsb_2DMap_Scroll;

                this.tb_CalT0.Click += this.CalibratorT0_Changed;
                this.tb_CalT0.Leave += this.CalibratorT0_Changed;
                this.tb_CalA.Click += this.CalibratorA_Changed;
                this.tb_CalA.Leave += this.CalibratorA_Changed;

                this.btn_Reset.Click += this.btn_Reset_Clicked;
                this.slide_Threshold.ValueChanged += this.slide_Threshold_ValueChanged;
                this.btn_revertCalDefaults.Click += this.btn_revertCalDefaults_Click;

                this.pnl_postProcessing.btn_AttemptCalibration.Click += this.btn_CalibrateFrames_Click;
                this.pnl_postProcessing.btn_ManualCalibration.Click += this.btn_ApplyCalculatedCalibration_Click;
                this.pnl_postProcessing.btn_ExperimentCalibration.Click += this.btn_ApplyCalibration_Experiment_Click;

                this.tabpages_Main.DrawItem += this.tabpages_Main_DrawItem;
                this.tabpages_Main.SelectedIndexChanged += this.tabpages_Main_SelectedIndexChanged;

                for (int i = 0; i < this.slider_ColorMap.btn_Slider.Length; i++)
                    this.slider_ColorMap.btn_Slider[i].MouseUp += this.ColorSelector_Change;
                this.slider_ColorMap.lbl_MaxIntensity.MouseEnter += this.show_MaxIntensity;

                this.Resize += this.IonMobilityDataView_Resize;
              //  this.tabpages_Main.Resize += new EventHandler(this.tabpages_Main_Resize);

                this.AllowDrop = true;
                this.DragDrop += DataViewer_DragDrop;
                this.DragEnter += DataViewer_DragEnter;
                this.lb_DragDropFiles.SelectedIndexChanged += lb_DragDropFiles_SelectedIndexChanged;

                this.pb_PlayDownOut.MouseDown += this.pb_PlayDownOut_MOUSEDOWN;
                this.pb_PlayDownOut.MouseUp += this.pb_PlayDownOut_MOUSEUP;
                this.pb_PlayUpOut.MouseDown += this.pb_PlayUpOut_MOUSEDOWN;
                this.pb_PlayUpOut.MouseUp += this.pb_PlayUpOut_MOUSEUP;
            }

            this.tabpages_Main.Width = this.ClientSize.Width + ((this.tabpages_Main.Height - this.tab_DataViewer.ClientSize.Height) / 2);
            this.tabpages_Main.Height = this.ClientSize.Height + (this.tabpages_Main.Height - this.tab_DataViewer.ClientSize.Height);
            this.tabpages_Main.Left = 0;
            this.tabpages_Main.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            this.flag_Resizing = true;
            Invoke(new ThreadStart(this.ResizeThis));
        }

        private void SetupPlots()
        {
            this.plot_TOF = new ZedGraph.ZedGraphControl();
            this.waveform_TOFPlot = new ZedGraph.LineItem("TOF");

            this.plot_Mobility = new Utilities.PointAnnotationGraph();
            this.waveform_MobilityPlot = new ZedGraph.LineItem("Mobility");

            // https://sourceforge.net/p/zedgraph/bugs/81/
            // ZedGraph does not handle the font size quite properly; scale the numbers to get what we want
            var zedGraphFontScaleFactor = 96F / 72F;

            //
            // plot_TOF
            //
            this.plot_TOF.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.plot_TOF.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_TOF.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.plot_TOF.BorderStyle = BorderStyle.Fixed3D;
            this.plot_TOF.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.plot_TOF.IsEnableHEdit = false;
            this.plot_TOF.IsEnableHPan = false;
            this.plot_TOF.IsEnableHZoom = false;
            this.plot_TOF.IsEnableSelection = false;
            this.plot_TOF.IsEnableVEdit = false;
            this.plot_TOF.IsEnableVPan = false;
            this.plot_TOF.IsEnableVZoom = false;
            this.plot_TOF.IsEnableZoom = false;
            this.plot_TOF.IsEnableWheelZoom = false;
            this.plot_TOF.Location = new System.Drawing.Point(18, 102);
            this.plot_TOF.Name = "plot_TOF";
            this.plot_TOF.GraphPane.Chart.Fill.Color = System.Drawing.Color.White;
            this.plot_TOF.GraphPane.CurveList.Add(this.waveform_TOFPlot);
            this.plot_TOF.Size = new System.Drawing.Size(204, 440);
            this.plot_TOF.TabIndex = 20;
            this.plot_TOF.TabStop = false;
            this.plot_TOF.GraphPane.Title.IsVisible = false;
            this.plot_TOF.GraphPane.Legend.IsVisible = false;
            this.plot_TOF.GraphPane.XAxis.Scale.IsReverse = true;
            this.plot_TOF.GraphPane.XAxis.Scale.IsLabelsInside = true;
            this.plot_TOF.GraphPane.XAxis.MajorGrid.Color = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.plot_TOF.GraphPane.XAxis.MajorGrid.IsVisible = true;
            this.plot_TOF.GraphPane.XAxis.CrossAuto = false;
            this.plot_TOF.GraphPane.XAxis.Cross = 1000000; // TODO: Set automatically
            this.plot_TOF.GraphPane.IsFontsScaled = false; // TODO:
            this.plot_TOF.GraphPane.XAxis.Scale.MaxAuto = true;
            this.plot_TOF.GraphPane.XAxis.Scale.Mag = 0;
            this.plot_TOF.GraphPane.XAxis.Scale.Format = "0.0E00";
            this.plot_TOF.GraphPane.XAxis.Scale.LabelGap = 0;
            this.plot_TOF.GraphPane.YAxis.Scale.Mag = 0;
            this.plot_TOF.GraphPane.YAxis.MinorTic.IsInside = false;
            this.plot_TOF.GraphPane.YAxis.MinorTic.IsCrossInside = false;
            this.plot_TOF.GraphPane.YAxis.MinorTic.IsOpposite = false;
            this.plot_TOF.GraphPane.YAxis.MajorTic.IsInside = false;
            this.plot_TOF.GraphPane.YAxis.MajorTic.IsCrossInside = false;
            this.plot_TOF.GraphPane.YAxis.MajorTic.IsOpposite = false;
            this.plot_TOF.GraphPane.YAxis.Scale.MaxAuto = true; // TODO:
            this.plot_TOF.GraphPane.XAxis.Scale.FontSpec.Family = "Verdana";
            this.plot_TOF.GraphPane.XAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_TOF.GraphPane.YAxis.Scale.FontSpec.Family = "Verdana";
            this.plot_TOF.GraphPane.YAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_TOF.GraphPane.Margin.Left -= 5;
            this.plot_TOF.GraphPane.Margin.Top = 25;
            this.plot_TOF.GraphPane.Margin.Right = 5;
            this.plot_TOF.GraphPane.Margin.Bottom = 5;
            this.plot_TOF.ContextMenu = contextMenu_TOF;
            //
            // waveform_TOFPlot
            //
            this.waveform_TOFPlot.Color = System.Drawing.Color.DarkBlue;
            this.waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.Transparent);

            // Label the axis
            this.plot_TOF.GraphPane.XAxis.Title.Text = "Time of Flight";
            this.plot_TOF.GraphPane.XAxis.Title.FontSpec.Family = "Verdana";
            this.plot_TOF.GraphPane.XAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_TOF.GraphPane.XAxis.Title.IsVisible = false;

            //
            // plot_Mobility
            //
            this.plot_Mobility.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_Mobility.BorderStyle = BorderStyle.Fixed3D;
            this.plot_Mobility.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.plot_Mobility.Location = new System.Drawing.Point(242, 572);
            this.plot_Mobility.Name = "plot_DriftPlot";
            this.plot_Mobility.GraphPane.Chart.Fill.Color = System.Drawing.Color.White;
            this.plot_Mobility.GraphPane.CurveList.Add(this.waveform_MobilityPlot);
            this.plot_Mobility.Size = new System.Drawing.Size(510, 111);
            this.plot_Mobility.TabIndex = 24;
            this.plot_Mobility.ContextMenu = contextMenu_driftTIC;
            this.plot_Mobility.RangeChanged += new Utilities.RangeEventHandler(this.OnPlotTICRangeChanged);
            this.plot_Mobility.GraphPane.Title.IsVisible = false;
            this.plot_Mobility.GraphPane.Legend.IsVisible = false;
            this.plot_Mobility.GraphPane.XAxis.Scale.Mag = 0;
            this.plot_Mobility.GraphPane.XAxis.MinorTic.IsInside = false;
            this.plot_Mobility.GraphPane.XAxis.MinorTic.IsCrossInside = false;
            this.plot_Mobility.GraphPane.XAxis.MinorTic.IsOpposite = false;
            this.plot_Mobility.GraphPane.XAxis.MajorTic.IsInside = false;
            this.plot_Mobility.GraphPane.XAxis.MajorTic.IsCrossInside = false;
            this.plot_Mobility.GraphPane.XAxis.MajorTic.IsOpposite = false;
            this.plot_Mobility.GraphPane.XAxis.Scale.MaxAuto = true; // TODO:
            this.plot_Mobility.GraphPane.XAxis.Scale.FontSpec.Family = "Verdana";
            this.plot_Mobility.GraphPane.XAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_Mobility.GraphPane.YAxis.Scale.FontSpec.Family = "Verdana";
            this.plot_Mobility.GraphPane.YAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_Mobility.GraphPane.YAxis.Scale.MaxAuto = true;
            this.plot_Mobility.GraphPane.YAxis.Scale.LabelGap = 0;
            this.plot_Mobility.IsEnableHEdit = false;
            this.plot_Mobility.IsEnableHPan = false;
            this.plot_Mobility.IsEnableHZoom = false;
            this.plot_Mobility.IsEnableSelection = false;
            this.plot_Mobility.IsEnableVEdit = false;
            this.plot_Mobility.IsEnableVPan = false;
            this.plot_Mobility.IsEnableVZoom = false;
            this.plot_Mobility.IsEnableZoom = false;
            this.plot_Mobility.IsEnableWheelZoom = false;
            this.plot_Mobility.GraphPane.XAxis.Scale.Format = "F2";
            this.plot_Mobility.GraphPane.XAxis.Scale.MaxAuto = true;
            this.plot_Mobility.GraphPane.YAxis.Scale.IsLabelsInside = true;
            this.plot_Mobility.GraphPane.IsFontsScaled = false; // TODO:
            this.plot_Mobility.GraphPane.YAxis.Scale.Mag = 0;
            this.plot_Mobility.GraphPane.YAxis.Scale.Format = "0.0E00";
            this.plot_Mobility.GraphPane.Margin.Left = -5;
            this.plot_Mobility.GraphPane.Margin.Top = 5;
            this.plot_Mobility.GraphPane.Margin.Right = 40;
            this.plot_Mobility.GraphPane.Margin.Bottom -= 5;
            //
            // waveform_MobilityPlot
            //
            this.waveform_MobilityPlot.Color = System.Drawing.Color.Crimson;
            this.waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, System.Drawing.Color.Salmon);

            // Label the axes
            this.plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Scans";
            this.plot_Mobility.GraphPane.XAxis.Title.FontSpec.Family = "Verdana";
            this.plot_Mobility.GraphPane.XAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_Mobility.GraphPane.YAxis.Title.Text = "Drift Intensity";
            this.plot_Mobility.GraphPane.YAxis.Title.FontSpec.Family = "Verdana";
            this.plot_Mobility.GraphPane.YAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            this.plot_Mobility.GraphPane.YAxis.Title.IsVisible = false;
            this.plot_Mobility.GraphPane.YAxis.Cross = 1000000;

            // Add the controls
            this.tab_DataViewer.Controls.Add(this.plot_TOF);
            this.tab_DataViewer.Controls.Add(this.plot_Mobility);
            this.plot_TOF.Show();

            this.plot_TOF.Width = 200;
            this.plot_Mobility.Height = 150;
        }

        #endregion

        #region Registry

        private static Microsoft.Win32.RegistryKey MainKey
        {
            get { return Application.UserAppDataRegistry; }
        }

        private void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using (Microsoft.Win32.RegistryKey sk = key.CreateSubKey(this.Name))
            {
            }
        }

        private void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey sk = key.OpenSubKey(this.Name))
                {
                }
            }
            catch { }
        }

        #endregion

        protected virtual void ResizeThis()
        {
            if (this.flag_isFullscreen)
            {
                this.pnl_2DMap.Left = 0;
                this.pnl_2DMap.Top = 0;

                this.max_plot_height = this.tab_DataViewer.ClientSize.Height;
                this.max_plot_width = this.tab_DataViewer.ClientSize.Width;

                this.pnl_2DMap.BringToFront();

                // --------------------------------------------------------------------------------------------------
                // middle top
                this.pnl_FrameControl.Left = this.pnl_2DMap.Left + 20;
                this.pnl_FrameControl.Width = this.pnl_2DMap.Width - 40;

                // pnl_FrameControl
                this.cb_ExperimentControlled.Top = 4;
                this.cb_ExperimentControlled.Left = 4;
                this.cb_ExperimentControlled.Width = this.pnl_FrameControl.Width - 10;

                this.elementHost_FrameSelect.Top = this.cb_ExperimentControlled.Top + this.cb_ExperimentControlled.Height + 4;

                this.cb_FrameType.Top = this.lbl_Chromatogram.Top = this.elementHost_FrameSelect.Top + 16;

                this.num_FrameIndex.Top = this.lbl_Chromatogram.Top;
                this.lbl_Chromatogram.Left = 30;
                this.cb_FrameType.Left = 4;
                this.num_FrameIndex.Left = this.cb_FrameType.Left + this.cb_FrameType.Width + 4;

                this.pb_PlayLeftIn.Top = this.pb_PlayLeftOut.Top = this.pb_PlayRightIn.Top = this.pb_PlayRightOut.Top = this.elementHost_FrameSelect.Top + 21;
                this.pb_PlayLeftIn.Left = this.pb_PlayLeftOut.Left = this.num_FrameIndex.Left + this.num_FrameIndex.Width + 6;
                this.pb_PlayRightIn.Left = this.pb_PlayRightOut.Left = this.pnl_FrameControl.Width - 32;

                this.elementHost_FrameSelect.Left = this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width;
                this.elementHost_FrameSelect.Width = this.pb_PlayRightIn.Left - (this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width);

                this.num_FrameRange.Top = this.elementHost_FrameSelect.Top + this.elementHost_FrameSelect.Height - 4;
                this.num_FrameRange.Left = this.elementHost_FrameSelect.Left + this.elementHost_FrameSelect.Width - this.num_FrameRange.Width;
                this.lbl_FrameRange.Top = this.num_FrameRange.Top + 2;
                this.lbl_FrameRange.Left = this.num_FrameRange.Left - this.lbl_FrameRange.Width - 2;

                this.lbl_FramesShown.Left = this.num_FrameIndex.Left - 30;
                this.lbl_FramesShown.Top = this.num_FrameRange.Top + 4;

                this.pnl_FrameControl.Height = this.num_FrameRange.Top + this.num_FrameRange.Height + 6;
                this.pnl_FrameControl.BringToFront();

                this.flag_Resizing = false;

                return;
            }

            this.pnl_postProcessing.Width = this.tab_PostProcessing.Width + 50;
            this.pnl_postProcessing.Height = this.tab_PostProcessing.Height + 50;

            // Start at the top!
            //
            // --------------------------------------------------------------------------------------------------
            // Far left column
            this.btn_Refresh.Top = 4;
            this.btn_Refresh.Left = 4;

            this.lbl_ExperimentDate.Top = 4;
            this.lbl_ExperimentDate.Left = this.btn_Refresh.Left + this.btn_Refresh.Width + 10; // this.pnl_2DMap.Left + this.pnl_2DMap.Width - this.lbl_ExperimentDate.Width;

            this.num_maxBin.Top = this.pnl_FrameControl.Top + this.pnl_FrameControl.Height - this.num_maxBin.Height - 6;

            this.tabpages_FrameInfo.Top = this.tab_DataViewer.Height - this.tabpages_FrameInfo.Height - 6;
            this.pnl_Chromatogram.Top = this.tabpages_FrameInfo.Top - this.pnl_Chromatogram.Height - 6;

            this.num_minBin.Left = this.num_maxBin.Left = 20;
            this.plot_TOF.Left = 20;

            this.tabpages_FrameInfo.Left = 5;
            this.pnl_Chromatogram.Left = 5;

            // max_plot_height ************************************************
            this.max_plot_height = this.tab_DataViewer.Height - 420;

            // --------------------------------------------------------------------------------------------------
            // middle top
            this.pnl_FrameControl.Left = this.pnl_2DMap.Left;
            this.pnl_FrameControl.Width = this.tab_DataViewer.ClientSize.Width - this.pnl_FrameControl.Left - 10;

            // pnl_FrameControl
            this.cb_ExperimentControlled.Top = 4;
            this.cb_ExperimentControlled.Left = 4;
            this.cb_ExperimentControlled.Width = this.pnl_FrameControl.Width - 10;

            this.elementHost_FrameSelect.Top = this.cb_ExperimentControlled.Top + this.cb_ExperimentControlled.Height + 4;

            this.cb_FrameType.Top = this.lbl_Chromatogram.Top = this.elementHost_FrameSelect.Top + 16;

            this.num_FrameIndex.Top = this.lbl_Chromatogram.Top;
            this.lbl_Chromatogram.Left = 30;
            this.cb_FrameType.Left = 4;
            this.num_FrameIndex.Left = this.cb_FrameType.Left + this.cb_FrameType.Width + 4;

            this.pb_PlayLeftIn.Top = this.pb_PlayLeftOut.Top = this.pb_PlayRightIn.Top = this.pb_PlayRightOut.Top = this.elementHost_FrameSelect.Top + 21;
            this.pb_PlayLeftIn.Left = this.pb_PlayLeftOut.Left = this.num_FrameIndex.Left + this.num_FrameIndex.Width + 6;
            this.pb_PlayRightIn.Left = this.pb_PlayRightOut.Left = this.pnl_FrameControl.Width - 32;

            this.elementHost_FrameSelect.Left = this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width;
            this.elementHost_FrameSelect.Width = this.pb_PlayRightIn.Left - (this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width);

            this.num_FrameRange.Top = this.elementHost_FrameSelect.Top + this.elementHost_FrameSelect.Height - 4;
            this.num_FrameRange.Left = this.elementHost_FrameSelect.Left + this.elementHost_FrameSelect.Width - this.num_FrameRange.Width;
            this.lbl_FrameRange.Top = this.num_FrameRange.Top + 2;
            this.lbl_FrameRange.Left = this.num_FrameRange.Left - this.lbl_FrameRange.Width - 2;

            this.lbl_FramesShown.Left = this.num_FrameIndex.Left - 30;
            this.lbl_FramesShown.Top = this.num_FrameRange.Top + 4;

            this.pnl_FrameControl.Height = this.num_FrameRange.Top + this.num_FrameRange.Height + 6;

            // --------------------------------------------------------------------------------------------------
            // Right
            this.slider_PlotBackground.Height = (this.max_plot_height / 3) + 5;
            this.slider_PlotBackground.Top = this.pnl_FrameControl.Top + this.pnl_FrameControl.Height + 10;

            this.elementHost_Threshold.Height = this.max_plot_height - this.btn_Reset.Height - this.slider_PlotBackground.Height;
            this.elementHost_Threshold.Top = this.slider_PlotBackground.Top + this.slider_PlotBackground.Height;

            this.btn_Reset.Top = this.elementHost_Threshold.Top + this.elementHost_Threshold.Height;

            this.slider_ColorMap.Height = (this.elementHost_Threshold.Top + this.elementHost_Threshold.Height) - this.slider_PlotBackground.Top;
            this.slider_ColorMap.Top = this.slider_PlotBackground.Top;

            this.elementHost_Threshold.Left = this.tab_DataViewer.Width - (this.elementHost_Threshold.Width + 25) - 10;
            this.slider_PlotBackground.Left = this.elementHost_Threshold.Left;
            this.slider_ColorMap.Left = this.elementHost_Threshold.Left - this.slider_ColorMap.Width - 10;
            this.btn_Reset.Left = this.slider_ColorMap.Left + 12;

            // Middle Bottom
            this.num_minMobility.Top = this.plot_Mobility.Top + plot_Mobility_HEIGHT + 5;
            this.num_maxMobility.Top = this.num_minMobility.Top;

            // pb_2DMap Size
            // max_plot_width *********************************************
            this.max_plot_width = this.slider_ColorMap.Left - this.pnl_2DMap.Left - 20;

            // --------------------------------------------------------------------------------------------------
            // selection corners
            if (this.menuItem_SelectionCorners.Checked)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this.corner_2DMap[i].X < 0)
                        this.corner_2DMap[i].X = (int)((double)this.pnl_2DMap.Width * .05);
                    else if (this.corner_2DMap[i].X > this.pnl_2DMap.Width)
                        this.corner_2DMap[i].X = (int)((double)this.pnl_2DMap.Width * .95);

                    if (this.corner_2DMap[i].Y < 0)
                        this.corner_2DMap[i].Y = (int)((double)this.pnl_2DMap.Height * .05);
                    else if (this.corner_2DMap[i].Y > this.pnl_2DMap.Height)
                        this.corner_2DMap[i].Y = (int)((double)this.pnl_2DMap.Height * .95);
                }
                this.pnl_2DMap.Invalidate();
                return;
            }

            // --------------------------------------------------------------------------------------------------
            // make sure the frame is on the screen.
            if (this.Left + this.Width > Screen.PrimaryScreen.Bounds.Width)
            {
                if (this.Width > Screen.PrimaryScreen.Bounds.Width)
                {
                    //MessageBox.Show("moving to left = 0, width = " + this.Width.ToString());
                    this.Left = 0;
                }
                else
                    this.Left = (Screen.PrimaryScreen.Bounds.Width - this.Width) / 2;
            }

            if (this.Top + this.Height > Screen.PrimaryScreen.Bounds.Height)
            {
                if (this.Height > Screen.PrimaryScreen.Bounds.Height)
                    this.Top = 0;
                else
                    this.Top = (Screen.PrimaryScreen.Bounds.Height - this.Height) / 2;
            }

#if TRACK_RESIZE_EVENTS
            this.lbl_ExperimentDate.Text = (count_resizes++).ToString();
#endif

            this.gb_MZRange.Left = this.tabpages_Main.Left + this.tabpages_Main.Width - this.gb_MZRange.Width - 45;
            this.gb_MZRange.Top = this.tabpages_Main.Top + this.tabpages_Main.Height - this.gb_MZRange.Height - 15;

            this.cb_EnableMZRange.Left = this.gb_MZRange.Left + 6;
            this.cb_EnableMZRange.Top = this.gb_MZRange.Top;
            this.cb_EnableMZRange.BringToFront();

            // bottom drag drop items
            this.cb_Exclusive.Top = this.num_maxMobility.Top + 8;
            this.cb_Exclusive.Left = this.pnl_2DMap.Left + ((this.pnl_2DMap.Width - this.cb_Exclusive.Width) / 2);
            this.cb_Exclusive.Width = this.pnl_2DMap.Width - 50;

            this.lb_DragDropFiles.Top = this.cb_Exclusive.Top + this.cb_Exclusive.Height - 2;
            this.lb_DragDropFiles.Height = this.ClientSize.Height - this.lb_DragDropFiles.Top - 6;
            this.lb_DragDropFiles.Left = this.pnl_2DMap.Left + 30;
            this.lb_DragDropFiles.Width = this.gb_MZRange.Left - this.lb_DragDropFiles.Left - 20;

            this.pb_PlayUpIn.Top = this.pb_PlayUpOut.Top = this.lb_DragDropFiles.Top + ((this.lb_DragDropFiles.Height / 2) - this.pb_PlayUpOut.Height - 2);
            this.pb_PlayDownIn.Top = this.pb_PlayDownOut.Top = this.lb_DragDropFiles.Top + (this.lb_DragDropFiles.Height / 2) + 2;
            this.pb_PlayDownIn.Left = this.pb_PlayDownOut.Left = this.pb_PlayUpIn.Left = this.pb_PlayUpOut.Left = this.lb_DragDropFiles.Left - this.pb_PlayUpIn.Width - 4;

            if (this.tabpages_Main.SelectedTab == this.tab_InstrumentSettings)
                this.pnl_InstrumentSettings.Resize_This();

            // redraw
            this.flag_Resizing = false;
            this.flag_update2DGraph = true;
        }
#if TRACK_RESIZE_EVENTS
      public int count_resizes = 0;
#endif

        protected virtual void GraphFrame(int[][] frame_data, bool flag_enablecontrols)
        {
            lock (this.lock_graphing)
            {
                this.flag_selection_drift = false;

                this.lbl_ExperimentDate.Text = this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.DateStarted, "");
                this.update_CalibrationCoefficients();

                // Initialize boundaries
                new_minMobility = 0;
                new_maxMobility = this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1; //  this.imfReader.Experiment_Properties.TOFSpectraPerFrame-1;
                new_minBin = 0;
                new_maxBin = this.ptr_UIMFDatabase.UimfGlobalParams.Bins - 1;

                this.maximum_Mobility = new_maxMobility;
                this.maximum_Bins = new_maxBin;

                this.num_minMobility.Minimum = -100;
                this.num_maxMobility.Maximum = 10000000;

                // set min and max here, they will not adjust to zooming
                this.flag_enterMobilityRange = true; // prevent events form occurring.
                this.num_minMobility.Value = Convert.ToDecimal(new_minMobility);
                this.num_maxMobility.Value = Convert.ToDecimal(new_maxMobility);
                this.flag_enterMobilityRange = false; // OK, clear this flag to make the controls usable

                // this.flag_enterBinRange = true;
                // this.num_minBin.Minimum = -100; //Convert.ToDecimal(new_minBin);
                // this.num_maxBin.Maximum = Convert.ToDecimal(new_maxBin);
                // this.flag_enterBinRange = false; // OK, clear this flag to make the controls usable

                try
                {
                    this.mean_TOFScanTime = this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                    // MessageBox.Show("mean_tof = " + this.mean_TOFScanTime.ToString());
                    decimal val = Convert.ToDecimal(this.mean_TOFScanTime);
                }
                catch (Exception ex)
                {
                    // ignore the error, can't find the file with the meanTOFscan.  This
                    // can occur (does occur) with drag-n-drop functionality of IMF files.
                    //MessageBox.Show(ex.ToString());
                }

                this.current_minBin = this.new_minBin;
                this.current_maxBin = this.new_maxBin;
                this.current_minMobility = this.new_minMobility;
                this.current_maxMobility = this.new_maxMobility;

                if (this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames < 2)
                {
                    this.elementHost_FrameSelect.Hide();
                    this.num_FrameRange.Hide();
                    this.lbl_FrameRange.Hide();

                    this.pb_PlayLeftIn.Hide();
                    this.pb_PlayLeftOut.Hide();
                    this.pb_PlayRightIn.Hide();
                    this.pb_PlayRightOut.Hide();
                }
                else
                {
                    this.elementHost_FrameSelect.Show();
                    this.num_FrameRange.Show();
                    this.lbl_FrameRange.Show();
                }

                // frame is created, allow frame cycling.
                this.flag_update2DGraph = true;

                this.vsb_2DMap.Minimum = 0;
                this.vsb_2DMap.Maximum = this.maximum_Bins;
                //this.vsb_2DMap.SmallChange = this.current_valuesPerPixelY * 1000;

                this.hsb_2DMap.Minimum = 0;
                this.hsb_2DMap.Maximum = 0;
                //this.hsb_2DMap.SmallChange = this.current_valuesPerPixelX * 1000;

                this.num_maxMobility.Minimum = Convert.ToDecimal(0);
                this.num_maxMobility.Maximum = Convert.ToDecimal(this.maximum_Mobility);
                this.num_minMobility.Minimum = Convert.ToDecimal(0);
                this.num_minMobility.Maximum = Convert.ToDecimal(this.maximum_Mobility);

                this.Text = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);

                this.AutoScrollPosition = new Point(0, 0);

                this.Show();

                if (flag_enablecontrols && (this.thread_GraphFrame == null))
                {
                    // thread GraphFrame
                    this.thread_GraphFrame = new Thread(new ThreadStart(this.tick_GraphFrame));
                    this.thread_GraphFrame.Priority = System.Threading.ThreadPriority.Normal;
                    this.thread_GraphFrame.Start();
                }
            }
        }

        // Generate a map out of the data, whether TOF or m/z
        //
        // wfd:  there may be a problem in here dealing with the differences between the
        //       mz plot and the TOF plot.  in the loop, you will see that the y's are going
        //       to different limits.  While it appears to work, it can not be trusted.
        protected virtual void Generate2DIntensityArray()
        {
            bool flag_newframe = false;
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                MessageBox.Show("ERROR:  should not be here");
                return;
            }

            double frameSelectValue = 0;
            this.slide_FrameSelect.Dispatcher.Invoke(() => frameSelectValue = this.slide_FrameSelect.Value);

            // Determine the frame size
            if (this.ptr_UIMFDatabase.CurrentFrameIndex != Convert.ToInt32(frameSelectValue))
            {
                flag_newframe = true;
                this.ptr_UIMFDatabase.CurrentFrameIndex = Convert.ToInt32(frameSelectValue);
            }

            this.get_ViewableIntensities();

            if (flag_newframe && this.flag_isTIMS)
                this.plot_Mobility.set_TIMSRamp(this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.a2, this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.b2,
                    this.ptr_UIMFDatabase.UimfFrameParams.MassCalibrationCoefficients.c2, this.ptr_UIMFDatabase.UimfFrameParams.Scans,
                    (int) (7500000.0/this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength))); // msec gap

            if (this.flag_Closing)
            {
                return;
            }

            if (this.flag_viewMobility)
                this.plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Scans";
            else
                this.plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Time (msec)";

            if (this.flag_display_as_TOF)
                this.plot_TOF.GraphPane.YAxis.Title.Text = "Time of Flight (usec)";
            else
                this.plot_TOF.GraphPane.YAxis.Title.Text = "m/z";

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        protected virtual void get_ViewableIntensities()
        {
            if (this.flag_collecting_data)
                return;
            this.flag_collecting_data = true;

            int exp_index;
            int start_index;
            int end_index;

            int frames;
            int temp;
            int data_height;
            int data_width;
            int total_mobility;
            int total_bins;

            int new_2dmap_width = 0;
            int new_2dmap_height = 0;

            int max_MZRange_bin;
            int min_MZRange_bin;
            float select_MZ = (float)Convert.ToDouble(this.num_MZ.Value);
            float select_PPM = (float)(select_MZ * Convert.ToDouble(this.num_PPM.Value) / 1000000.0);
            if (this.cb_EnableMZRange.Checked)
            {
                // min_TOF = (this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);

                min_MZRange_bin = (int)(((double)this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(select_MZ - select_PPM)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                max_MZRange_bin = (int)(((double)this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(select_MZ + select_PPM)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);

                this.current_minBin = (int)(((double)this.ptr_UIMFDatabase.MzCalibration.MZtoTOF((float)(select_MZ - (select_PPM * 1.5)))) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                this.current_maxBin = (int)(((double)this.ptr_UIMFDatabase.MzCalibration.MZtoTOF((float)(select_MZ + (select_PPM * 1.5)))) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
            }
            else
            {
                min_MZRange_bin = 0;
                max_MZRange_bin = this.ptr_UIMFDatabase.UimfGlobalParams.Bins;
            }

            if (this.current_maxBin < this.current_minBin)
            {
                MessageBox.Show("(this.current_maxBin < this.current_minBin): (" + this.current_maxBin.ToString() + " < " + this.current_minBin.ToString() + ")" + maximum_Bins.ToString());

                temp = this.current_minBin;
                this.current_minBin = this.current_maxBin;
                this.current_maxBin = temp;
            }
            total_bins = (this.current_maxBin - this.current_minBin) + 1;

            if (this.current_maxMobility < this.current_minMobility)
            {
                temp = this.current_minMobility;
                this.current_minMobility = this.current_maxMobility;
                this.current_maxMobility = temp;
            }
            total_mobility = (this.current_maxMobility - this.current_minMobility) + 1;

            // resize data to fit screen
            if (this.max_plot_height < total_bins)
            {
                this.current_valuesPerPixelY = (total_bins / this.max_plot_height);

                this.current_maxBin = this.current_minBin + (this.current_valuesPerPixelY * this.max_plot_height);

                if (this.current_maxBin > this.maximum_Bins)
                {
                    this.current_minBin -= (this.current_maxBin - this.maximum_Bins);
                    this.current_maxBin = this.maximum_Bins - 1;
                }
                if (this.current_minBin < 0)
                {
                    MessageBox.Show("Bill " + "(" + this.current_maxBin.ToString() + " < " + this.current_minBin.ToString() + ")\n\n" + this.max_plot_height.ToString() + " < " + total_bins.ToString() + "\n\nget_ViewableIntensities: this.current_maxBin is already this.maximum_Bins  -- should never happen");
                    this.current_minBin = 0;
                }

                total_bins = (this.current_maxBin - this.current_minBin) + 1;
                this.current_valuesPerPixelY = (total_bins / this.max_plot_height);
            }
            else // the pixels get taller...
            {
                this.current_valuesPerPixelY = -(max_plot_height / total_bins);
                if (this.current_valuesPerPixelY >= 0)
                    this.current_valuesPerPixelY = -1;

                // create calibration table
                this.current_maxBin = this.current_minBin + (this.max_plot_height / -this.current_valuesPerPixelY);

                if (this.current_maxBin > this.maximum_Bins)
                {
                    this.current_maxBin = this.maximum_Bins;
                    this.current_minBin = this.maximum_Bins - (this.max_plot_height / -this.current_valuesPerPixelY);
                }
                if (this.current_minBin < 0)
                {
                    this.current_minBin = 0;
                    this.current_maxBin = (this.max_plot_height / -this.current_valuesPerPixelY);
                }

                if ((this.current_maxBin - this.current_minBin) < MIN_GRAPHED_BINS)
                {
                    this.current_minBin = ((this.current_maxBin + this.current_minBin) - MIN_GRAPHED_BINS) / 2;
                    this.current_maxBin = this.current_minBin + MIN_GRAPHED_BINS;
                }

                total_bins = (this.current_maxBin - this.current_minBin) + 1;
                this.current_valuesPerPixelY = -(max_plot_height / total_bins);

                // OK, make sure we have a good fit on the screen.
                if (this.current_valuesPerPixelY >= 0)
                {
                    this.current_valuesPerPixelY = -1;
                    if ((total_bins * -this.current_valuesPerPixelY) + 1 > this.max_plot_height)
                    {
                        this.current_maxBin = this.current_minBin + this.max_plot_height;
                        total_bins = (this.current_maxBin - this.current_minBin) + 1;
                    }
                }
                else
                {
                    // good enough- just awful.
                    while (((total_bins + 1) * -this.current_valuesPerPixelY) + 1 < this.max_plot_height)
                    {
                        //int offset_fit = (this.max_plot_height - ((total_bins+1) * -this.current_valuesPerPixelY))/2;
                        this.current_minBin--;
                        this.current_maxBin++;
                        total_bins = (this.current_maxBin - this.current_minBin) + 1;
                    }
                }
            }

            if (this.current_valuesPerPixelY > 0)
                new_2dmap_height = (total_bins / this.current_valuesPerPixelY) + 1;
            else
                new_2dmap_height = (total_bins * -this.current_valuesPerPixelY) + 1;
            if (this.pnl_2DMap.Height != new_2dmap_height)
            {
                if (this.pnl_2DMap.InvokeRequired)
                {
                    this.pnl_2DMap.Invoke(new MethodInvoker(delegate { this.pnl_2DMap.Height = new_2dmap_height; }));
                }
                else
                {
                    this.pnl_2DMap.Height = new_2dmap_height;
                }
                this.flag_ResizeThis = true;
            }

            if (max_plot_width < total_mobility)
            {
                // in this case we will not overlap pixels.  We can create another scrollbar to handle too wide plots
                this.current_valuesPerPixelX = -1;

                this.current_minMobility = this.hsb_2DMap.Value;
                this.current_maxMobility = this.current_minMobility + this.max_plot_width;

                if (this.current_maxMobility > this.maximum_Mobility)
                {
                    this.current_maxMobility = this.maximum_Mobility;
                    this.current_minMobility = Convert.ToInt32(this.num_minMobility.Value); // 0; // this.maximum_Mobility - this.max_plot_width;
                }

#if false
                this.current_valuesPerPixelX = (total_mobility / this.max_plot_width) + 1;

                this.current_maxMobility = this.current_minMobility + (this.max_plot_width * this.current_valuesPerPixelX);
                if (this.current_minMobility < 0)
                {
                    this.current_minMobility = 0;
                    this.current_maxMobility = (this.max_plot_width * this.current_valuesPerPixelY);
                }
                if (this.current_maxMobility > this.maximum_Mobility)
                    this.current_maxMobility = this.maximum_Mobility;
#endif
            }
            else
            {
                this.current_valuesPerPixelX = -(this.max_plot_width / total_mobility);
                // MessageBox.Show("max_plot_width=" + max_plot_width + ", this.current_valuesPerPixelX=" + this.current_valuesPerPixelX.ToString());

#if false // erin did not like my attempt at extending out the plot.  Aug 2, 2010
                    this.current_maxMobility = this.current_minMobility + (this.max_plot_width / -this.current_valuesPerPixelX) - 1;

                    if (this.current_maxMobility > this.maximum_Mobility)
                    {
                        this.current_maxMobility = this.maximum_Mobility;
                        this.current_minMobility = this.maximum_Mobility - (this.max_plot_width / -this.current_valuesPerPixelX);
                    }
                    if (this.current_minMobility < 0)
                    {
                        this.current_minMobility = 0;
                        this.current_maxMobility = (this.max_plot_width / -this.current_valuesPerPixelX);
                    }
                    if (this.current_maxMobility > this.maximum_Mobility)
                        this.current_maxMobility = this.maximum_Mobility;
#endif
            }

            total_mobility = (this.current_maxMobility - this.current_minMobility) + 1;

            // calculate width of data
            if (this.current_valuesPerPixelX > 0)
                new_2dmap_width = (total_mobility / this.current_valuesPerPixelX) + 1;
            else
                new_2dmap_width = (total_mobility * -this.current_valuesPerPixelX) + 1;
            if (this.pnl_2DMap.Width != new_2dmap_width)
            {
                this.flag_ResizeThis = true;
                if (this.pnl_2DMap.InvokeRequired)
                {
                    this.pnl_2DMap.Invoke(new MethodInvoker(delegate { this.pnl_2DMap.Width = new_2dmap_width; }));
                }
                else
                {
                    this.pnl_2DMap.Width = new_2dmap_width;
                }
            }

            // create array to store visual data
            if (this.current_valuesPerPixelX < 0)
                data_width = total_mobility;
            else
                data_width = this.pnl_2DMap.Width;
            if (this.current_valuesPerPixelY < 0)
                data_height = total_bins;
            else
                data_height = this.pnl_2DMap.Height;

#if OLD // TODO:
            this.data_2D = new int[data_width][];
            for (int n = 0; n < data_width; n++)
                this.data_2D[n] = new int[data_height];
#endif

            // show frame range
            var frameSelectValue = 0.0;
            this.elementHost_FrameSelect.Invoke(new MethodInvoker(delegate
            {
                frameSelectValue = this.slide_FrameSelect.Value;
                if ((this.slide_FrameSelect.Value - Convert.ToInt32(this.num_FrameRange.Value) + 1) < 0)
                    this.slide_FrameSelect.SelectionStart = 0;
                else
                    this.slide_FrameSelect.SelectionStart =
                        ((double) (this.slide_FrameSelect.Value - Convert.ToInt32(this.num_FrameRange.Value) + 1)) - .1;
                this.slide_FrameSelect.SelectionEnd = this.slide_FrameSelect.Value;
            }));

            if (this.num_FrameIndex.Maximum >= (int)frameSelectValue)
                this.num_FrameIndex.Invoke(new MethodInvoker(delegate { this.num_FrameIndex.Value = (int)frameSelectValue; }));

            for (exp_index = 0; exp_index < this.lb_DragDropFiles.Items.Count; exp_index++)
            {
                if (this.lb_DragDropFiles.GetSelected(exp_index))
                {
                    this.ptr_UIMFDatabase = (UIMFDataWrapper)this.array_Experiments[exp_index];

                    start_index = this.ptr_UIMFDatabase.CurrentFrameIndex - (this.ptr_UIMFDatabase.FrameWidth - 1);
                    end_index = this.ptr_UIMFDatabase.CurrentFrameIndex;

                    if (Convert.ToInt32(this.num_FrameRange.Value) > 1)
                    {
                        this.lbl_FramesShown.Invoke(new MethodInvoker(delegate
                        {
                            this.lbl_FramesShown.Show();
                            this.lbl_FramesShown.Text = "Showing Frames: " + start_index.ToString() + " to " + end_index.ToString();
                        }));
                    }

                    // collect the data
#if OLD // TODO:
                    for (frames = start_index; (frames <= end_index) && !this.flag_Closing; frames++)
                    {
                        // this.lbl_ExperimentDate.Text = "accumulate_FrameData: " + (++count_times).ToString() + "  "+start_index.ToString()+"<"+end_index.ToString();

                        try
                        {
                            if (this.data_2D == null)
                                MessageBox.Show("null");
                            this.data_2D = this.ptr_UIMFDatabase.AccumulateFrameData(frames, this.flag_display_as_TOF, this.current_minMobility, this.current_minBin, min_MZRange_bin, max_MZRange_bin, this.data_2D, this.current_valuesPerPixelY);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("accumulate_FrameData:  " + ex.ToString());
                        }
                    }
#endif
                    /*/
                    this.data_2D = this.ptr_UIMFDatabase.AccumulateFrameData(this.ptr_UIMFDatabase.ArrayFrameNum[start_index], this.ptr_UIMFDatabase.ArrayFrameNum[end_index], this.flag_display_as_TOF,
                        this.current_minMobility, this.current_minMobility + data_width, this.current_minBin, this.current_minBin + (data_height * this.current_valuesPerPixelY),
                        this.current_valuesPerPixelY, this.data_2D, min_MZRange_bin, max_MZRange_bin);
                    /*/
                    this.data_2D = this.ptr_UIMFDatabase.AccumulateFrameDataByCount(this.ptr_UIMFDatabase.ArrayFrameNum[start_index], this.ptr_UIMFDatabase.ArrayFrameNum[end_index], this.flag_display_as_TOF,
                        this.current_minMobility, data_width, this.current_minBin, data_height, this.current_valuesPerPixelY, /*this.data_2D*/ null, min_MZRange_bin, max_MZRange_bin);
                    /**/

                    try
                    {
                        int sel_min;
                        int sel_max;
                        if (this.flag_viewMobility)
                        {
                            sel_min = (this.selection_min_drift - this.current_minMobility);
                            sel_max = (this.selection_max_drift - this.current_minMobility);
                        }
                        else
                        {
                            sel_min = (int)((this.selection_min_drift - (int)(this.current_minMobility * (this.mean_TOFScanTime / 1000000))));
                            sel_max = (int)((this.selection_max_drift - (int)(this.current_minMobility * (this.mean_TOFScanTime / 1000000)))); //  * (this.mean_TOFScanTime / 100000));
                        }

                        int current_scan;
                        int bin_value;
                        this.data_maxIntensity = 0;
                        this.data_driftTIC = new double[data_width];
                        this.data_tofTIC = new double[data_height];

                        for (current_scan = 0; current_scan < data_width; current_scan++)
                        {
                            for (bin_value = 0; bin_value < data_height; bin_value++)
                            {
                                if (this.inside_Polygon_Pixel(current_scan, bin_value))
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
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                    this.update_CalibrationCoefficients();
                }
            }

            // point to the selected experiment whether it is enabled or not
            this.ptr_UIMFDatabase = (UIMFDataWrapper)this.array_Experiments[this.index_CurrentExperiment];

            if (!this.flag_isFullscreen)
            {
                this.plot_axisMobility(this.data_driftTIC);
                this.plot_axisTOF(this.data_tofTIC);

                plot_TOF.Invoke(new MethodInvoker(delegate {
                // align everything
                if (this.current_valuesPerPixelY > 0)
                {
                    this.plot_TOF.Height = this.pnl_2DMap.Height + this.plot_TOF.Height - (int)this.plot_TOF.GraphPane.Chart.Rect.Height;
                    this.plot_TOF.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4;
                }
                else
                {
                    this.plot_TOF.Height = this.pnl_2DMap.Height + this.plot_TOF.Height - (int)this.plot_TOF.GraphPane.Chart.Rect.Height + this.current_valuesPerPixelY;
                    this.plot_TOF.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 - this.current_valuesPerPixelY / 2;
                }

                this.num_minBin.Top = this.plot_TOF.Top + this.plot_TOF.Height + 4;
                this.vsb_2DMap.Height = this.pnl_2DMap.Height;

                this.pnl_2DMap.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 + (int)this.plot_TOF.GraphPane.Chart.Rect.Top;
                this.hsb_2DMap.Top = this.pnl_2DMap.Top - this.hsb_2DMap.Height;
                this.vsb_2DMap.Top = this.pnl_2DMap.Top;

                if ((this.plot_TOF.Top + this.plot_TOF.Height) < (this.pnl_2DMap.Top + this.pnl_2DMap.Height + 16))
                    this.plot_Mobility.Top = this.pnl_2DMap.Top + this.pnl_2DMap.Height + 16;
                else
                    this.plot_Mobility.Top = this.plot_TOF.Top + this.plot_TOF.Height;
                this.num_minMobility.Top = this.num_maxMobility.Top = this.plot_Mobility.Top + this.plot_Mobility.Height + 4;

                if (this.current_valuesPerPixelX > 0)
                {
                    this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width;
                    this.plot_Mobility.Width = this.pnl_2DMap.Width + this.plot_Mobility.Width - (int)this.plot_Mobility.GraphPane.Chart.Rect.Width;
                }
                else
                {
                    this.plot_Mobility.Width = this.pnl_2DMap.Width + this.plot_Mobility.Width - (int)this.plot_Mobility.GraphPane.Chart.Rect.Width + this.current_valuesPerPixelX;
                    this.plot_Mobility.Left = this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width - this.current_valuesPerPixelX / 2;
                }

                this.num_minMobility.Left = this.plot_Mobility.Left;
                this.num_maxMobility.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.num_maxMobility.Width; //- (this.plot_Mobility.PlotAreaBounds.Width - this.pnl_2DMap.Width)

                this.pnl_2DMap.Left = this.plot_TOF.Left + this.plot_TOF.Width + (int)this.plot_Mobility.GraphPane.Chart.Rect.Left;
                this.hsb_2DMap.Left = this.pnl_2DMap.Left;

                this.hsb_2DMap.Width = this.pnl_2DMap.Width;
                this.vsb_2DMap.Left = this.pnl_2DMap.Left + this.pnl_2DMap.Width;
                }));
            }

            this.flag_collecting_data = false;
        }

        private void calc_TIC()
        {
#if false
            if (this.num_FrameRange.Value > 1)
                return;

            long[] bins = new long[current_maxBin - current_minBin];
            long TIC_Count = 0;
            int y_pos = 0;
            int i;
            int threshold = Convert.ToInt32(this.num_TICThreshold.Value);

            for (i = 0; i < bins.Length; i++)
                bins[i] = 0;

            for (int x_pos = current_minMobility; x_pos < current_maxMobility; x_pos++)
            {
                sumDrift = 0;

                // Bin the spectra.
                int minIndex = 0;
                int maxIndex = 0;
                int indexX = x_pos - current_minMobility;

                this.frame_Data.SpectraBounds(x_pos, current_minBin, current_maxBin, out minIndex, out maxIndex);

                // If minIndex < 0 there is no data in this spectrum
                if (minIndex < 0)
                    continue;

                try
                {
                    for (y_pos = minIndex; y_pos < maxIndex; y_pos++)
                    {
                        if ((current_minBin > this.frame_Data.TOFValues[y_pos]) || (current_maxBin < this.frame_Data.TOFValues[y_pos]))
                            continue;

                        bins[this.frame_Data.TOFValues[y_pos] - current_minBin] += this.frame_Data.Intensities[y_pos];
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString() + "\n\n" + current_minBin.ToString() + " < " + (this.frame_Data.TOFValues[y_pos] - current_minBin).ToString() + " < " + current_maxBin.ToString());
                }
            }

            int bins_count = 0;
            string test = "";
            for (y_pos = 0; y_pos < current_maxBin - current_minBin; y_pos++)
                if (bins[y_pos] > threshold)
                {
                    bins_count++;
                    TIC_Count += bins[y_pos];
                    if (bins_count < 100)
                        test += y_pos.ToString() + " " + bins[y_pos].ToString() + ", ";
                }

            //
            this.lbl_FramesShown.Show();
            this.lbl_FramesShown.Text = "(TIC Count > " + threshold.ToString() + ") = " + TIC_Count.ToString();
#endif
        }

        private void Generate2DIntensityArray_Chromatogram()
        {
            int i;
            int mobility_index;
            int frame_index;
            int[] mobility_data = new int[0];

            int compression;
            int compression_collection;
            int total_frames = this.ptr_UIMFDatabase.GetNumberOfFrames(this.ptr_UIMFDatabase.CurrentFrameType);
            int total_scans = this.ptr_UIMFDatabase.UimfFrameParams.Scans;

            int data_height;
            int data_width = total_frames / Convert.ToInt32(this.num_FrameCompression.Value);

            int new_2dmap_height;
            int new_2dmap_width;
            int max_MZRange_bin;
            int min_MZRange_bin;
            float select_MZ = (float)Convert.ToDouble(this.num_MZ.Value);
            float select_PPM = (float)(select_MZ * Convert.ToDouble(this.num_PPM.Value) / 1000000.0);

            if (this.cb_EnableMZRange.Checked)
            {
                min_MZRange_bin = (int) (((double) this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(select_MZ - select_PPM)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                max_MZRange_bin = (int) (((double) this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(select_MZ + select_PPM)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);

                // MessageBox.Show(min_MZRange_bin.ToString() + "<" + max_MZRange_bin.ToString());
            }
            else
            {
                min_MZRange_bin = 0;
                max_MZRange_bin = this.ptr_UIMFDatabase.UimfGlobalParams.Bins;
            }

            if (!this.flag_chromatograph_collected_COMPLETE && !this.flag_chromatograph_collected_PARTIAL)
            {
                this.CreateProgressBar();

                // only collect this one time.
                this.chromat_data = new int[total_frames / Convert.ToInt32(this.num_FrameCompression.Value)][];
                for (mobility_index = 0; mobility_index < total_frames / Convert.ToInt32(this.num_FrameCompression.Value); mobility_index++)
                    this.chromat_data[mobility_index] = new int[total_scans + 1];

                this.flag_collecting_data = true;

                if (this.rb_PartialChromatogram.Checked)
                    compression_collection = 1;
                else
                    compression_collection = Convert.ToInt32(this.num_FrameCompression.Value);

                for (mobility_index = 0; (mobility_index < data_width) && this.flag_Alive; mobility_index++) // wfd
                {
                    for (compression = 0; compression < compression_collection; compression++)
                    {
                        this.progress_ReadingFile.Value = mobility_index;
                        this.progress_ReadingFile.Update();

                        frame_index = (mobility_index * Convert.ToInt32(this.num_FrameCompression.Value)) + compression;
                        //MessageBox.Show(frame_index.ToString());

                        mobility_data = this.ptr_UIMFDatabase.GetDriftChromatogram(frame_index, min_MZRange_bin, max_MZRange_bin);
                        for (i = 0; i < mobility_data.Length; i++)
                            this.chromat_data[mobility_index][i] += mobility_data[i];
                    }
                }

                this.progress_ReadingFile.Dispose();

                this.flag_collecting_data = false;

                if (this.rb_CompleteChromatogram.Checked)
                    this.flag_chromatograph_collected_COMPLETE = true;
                else
                    this.flag_chromatograph_collected_PARTIAL = true;

                if (!this.flag_Alive)
                    return;
            }

            // -------------------------------------------------------------------------
            // data collected put it into the data_2d array, compress for viewing.
            //
            // allow the chromatogram to compress vertically; but not horizontally.
            //
            this.current_minMobility = this.hsb_2DMap.Value;
            this.chromatogram_valuesPerPixelX = -1;

            //  MessageBox.Show("("+max_plot_width.ToString()+" < "+total_frames.ToString()+")"+data_width.ToString());

            if (max_plot_width < total_frames)
            {
                this.current_maxMobility = total_frames;

                // in this case we will not overlap pixels.  We can create another scrollbar to handle too wide plots
                this.chromatogram_valuesPerPixelX = -1;

                this.current_minMobility = this.hsb_2DMap.Value;
                this.current_maxMobility = this.current_minMobility + this.max_plot_width;
            }
            else
            {
                this.current_maxMobility = max_plot_width;

                this.chromatogram_valuesPerPixelX = -(this.max_plot_width / total_frames);

                this.current_maxMobility = this.current_minMobility + (this.max_plot_width / -this.chromatogram_valuesPerPixelX) - 1;
                if (this.current_maxMobility > this.maximum_Mobility)
                {
                    this.current_maxMobility = this.maximum_Mobility;
                    this.current_minMobility = this.maximum_Mobility - (this.max_plot_width / -this.chromatogram_valuesPerPixelX);
                }
                if (this.current_minMobility < 0)
                {
                    this.current_minMobility = 0;
                    this.current_maxMobility = (this.max_plot_width / -this.chromatogram_valuesPerPixelX);
                }
                if (this.current_maxMobility > this.maximum_Mobility)
                    this.current_maxMobility = this.maximum_Mobility;
            }

            // total_frames = (this.current_maxMobility - this.current_minMobility) + 1;
            if (this.chromatogram_valuesPerPixelX > 0)
                new_2dmap_width = (data_width / this.chromatogram_valuesPerPixelX) + 1;
            else
                new_2dmap_width = (data_width * -this.chromatogram_valuesPerPixelX) + 1;

            if (new_2dmap_width > this.slider_ColorMap.Left - this.pnl_2DMap.Left)
                this.tab_DataViewer.Width = this.pnl_2DMap.Left + new_2dmap_width + 175;
            else
            {
                this.chromatogram_valuesPerPixelX = -((((this.slider_ColorMap.Left - this.pnl_2DMap.Left) / new_2dmap_width) * new_2dmap_width) / data_width);
                new_2dmap_width = (data_width * -this.chromatogram_valuesPerPixelX) + 1;
            }

            if (this.pnl_2DMap.Width != new_2dmap_width)
            {
                this.pnl_2DMap.Width = new_2dmap_width;
                this.flag_ResizeThis = true;
            }

            if (this.current_maxMobility > total_frames)
            {
                this.current_maxMobility = total_frames - 1;// -this.pnl_2DMap.Width - 1;
                this.current_minMobility = this.current_maxMobility - this.pnl_2DMap.Width;
            }

            this.chromatogram_valuesPerPixelY = 1; //(total_scans / this.max_plot_height);
            this.current_minBin = 0;
            if (this.max_plot_height > total_scans - 1)
                this.current_maxBin = this.current_minBin + total_scans - 1;
            else
                this.current_maxBin = this.current_minBin + this.max_plot_height;

            total_scans = (this.current_maxBin - this.current_minBin);
            this.chromatogram_valuesPerPixelY = 1; //(total_scans / this.max_plot_height);

            new_2dmap_height = (total_scans / this.chromatogram_valuesPerPixelY) + 1;
            if (this.pnl_2DMap.Height != new_2dmap_height)
            {
                this.pnl_2DMap.Height = new_2dmap_height;
            }

            //-----------------------------------------------------------------------------------------
            // create array to store visual data
            if (this.chromatogram_valuesPerPixelY < 0)
                data_height = total_scans;
            else
                data_height = this.pnl_2DMap.Height;

            this.data_2D = new int[data_width][];
            for (int n = 0; n < data_width; n++)
                this.data_2D[n] = new int[data_height];

            //-----------------------------------------------------------------------------------------
            // collect the data for viewing.
            this.chromat_max = 0;

            if (data_width > this.pnl_2DMap.Width)
            {
                this.hsb_2DMap.SmallChange = this.pnl_2DMap.Width / 5;
                this.hsb_2DMap.LargeChange = this.pnl_2DMap.Width * 4 / 5;

                this.hsb_2DMap.Maximum = data_width; // -this.hsb_2DMap.LargeChange - 1;
                // MessageBox.Show(total_frames.ToString());
                this.num_maxMobility.Maximum = total_frames;
                this.minFrame_Chromatogram = this.current_minMobility; //  this.hsb_2DMap.Value;
                //  this.lbl_ExperimentDate.Text = this.hsb_2DMap.Maximum.ToString() + ", " + this.minFrame_Chromatogram.ToString();
            }
            else
            {
                this.hsb_2DMap.Maximum = total_frames - data_width;
                this.minFrame_Chromatogram = 0;
            }

            this.maxFrame_Chromatogram = this.minFrame_Chromatogram + this.pnl_2DMap.Width;
            // MessageBox.Show("0 "+this.chromatogram_valuesPerPixelY.ToString());

            // ok, making chromatogram_valuesPerPixelX always negative.
            if (this.chromatogram_valuesPerPixelY < 0)
            {
                //MessageBox.Show("here");
                // pixel_y = 1;

                for (frame_index = 0; frame_index < data_width; frame_index++)
                {
                    for (mobility_index = 0; mobility_index < data_height; mobility_index++)
                    {
                        this.data_2D[frame_index][mobility_index] += this.chromat_data[frame_index + this.minFrame_Chromatogram][mobility_index];

                        if (this.data_2D[frame_index][mobility_index] > this.data_maxIntensity)
                        {
                            this.chromat_max = data_2D[frame_index][mobility_index];

                            this.posX_MaxIntensity = frame_index;
                            this.posY_MaxIntensity = mobility_index;
                        }
                    }
                }
                MessageBox.Show("max: " + this.data_maxIntensity.ToString());
            }
            else
            {
                // MessageBox.Show("height: " + data_height.ToString() + ", " + this.chromat_data[0].Length.ToString());
                // MessageBox.Show("width: " + data_width.ToString() + ", " + this.chromat_data.Length.ToString());
                for (frame_index = 0; (frame_index < data_width); frame_index++)
                    for (mobility_index = 0; mobility_index < data_height; mobility_index++)
                    {
                        this.data_2D[frame_index][mobility_index] = this.chromat_data[frame_index + this.minFrame_Chromatogram][mobility_index];

                        if (this.data_2D[frame_index][mobility_index] > this.chromat_max)
                        {
                            this.chromat_max = this.data_2D[frame_index][mobility_index];
                            this.posX_MaxIntensity = frame_index;
                            this.posY_MaxIntensity = mobility_index;
                        }
                    }

                this.data_maxIntensity = this.chromat_max;
                //  MessageBox.Show("done: "+this.pnl_2DMap.Width.ToString());
            }

            //   MessageBox.Show("1");

            // ------------------------------------------------------------------------------
            // create the side plots
            this.chromatogram_driftTIC = new double[data_width];
            this.chromatogram_tofTIC = new double[data_height];
            for (frame_index = 0; frame_index < data_width; frame_index++)
                for (mobility_index = 0; mobility_index < data_height; mobility_index++)
                {
                    // peak chromatogram
                    if (this.data_2D[frame_index][mobility_index] > this.chromatogram_driftTIC[frame_index])
                        this.chromatogram_driftTIC[frame_index] = this.data_2D[frame_index][mobility_index];

                    this.chromatogram_tofTIC[mobility_index] += this.data_2D[frame_index][mobility_index];
                    if (this.data_2D[frame_index][mobility_index] > this.data_maxIntensity)
                    {
                        this.data_maxIntensity = this.data_2D[frame_index][mobility_index];
                        this.posX_MaxIntensity = frame_index;
                        this.posY_MaxIntensity = mobility_index;
                    }
                }

            this.plot_axisTOF(this.chromatogram_tofTIC);
            this.plot_axisMobility(this.chromatogram_driftTIC);

            // align everything
            this.plot_TOF.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4;
            this.plot_TOF.Height = this.pnl_Chromatogram.Top - this.plot_TOF.Top - 30;

            this.num_minBin.Top = this.plot_TOF.Top + this.plot_TOF.Height + 4;

            this.plot_Mobility.Top = this.plot_TOF.Top + this.plot_TOF.Height;
            this.num_minMobility.Top = this.num_maxMobility.Top = this.plot_Mobility.Top + this.plot_Mobility.Height + 4;
            this.vsb_2DMap.Height = this.pnl_2DMap.Height;

            this.pnl_2DMap.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 + (int)this.plot_TOF.GraphPane.Chart.Rect.Top;
            this.hsb_2DMap.Top = this.pnl_2DMap.Top - this.hsb_2DMap.Height;
            this.vsb_2DMap.Top = this.pnl_2DMap.Top;
            // MessageBox.Show("3");

            if (this.chromatogram_valuesPerPixelX > 0)
            {
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.chromatogram_valuesPerPixelX/2;
                this.plot_Mobility.Width = this.pnl_2DMap.Width + this.plot_Mobility.Width - (int)this.plot_Mobility.GraphPane.Chart.Rect.Width - this.chromatogram_valuesPerPixelX;
            }
            else
            {
                this.plot_Mobility.Width = this.pnl_2DMap.Width + this.plot_Mobility.Width - (int)this.plot_Mobility.GraphPane.Chart.Rect.Width + this.chromatogram_valuesPerPixelX;
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width + (-this.chromatogram_valuesPerPixelX / 2);
            }

            this.num_minMobility.Left = this.plot_Mobility.Left;
            this.num_maxMobility.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.num_maxMobility.Width; //- ((int)this.plot_Mobility.GraphPane.Chart.Rect.Width - this.pnl_2DMap.Width)

            this.cb_FrameType.Top = this.num_minMobility.Top + 40;
            this.cb_FrameType.Left = this.num_minMobility.Left + 5;

            this.pnl_2DMap.Left = this.plot_TOF.Left + this.plot_TOF.Width + (int)this.plot_Mobility.GraphPane.Chart.Rect.Left;
            this.hsb_2DMap.Left = this.pnl_2DMap.Left;

            this.hsb_2DMap.Width = this.pnl_2DMap.Width;
            this.vsb_2DMap.Left = this.pnl_2DMap.Left + this.pnl_2DMap.Width;

            if (this.flag_viewMobility)
                this.plot_TOF.GraphPane.YAxis.Title.Text = "Mobility - Scans";
            else
                this.plot_TOF.GraphPane.YAxis.Title.Text = "Mobility - Time (msec)";
            this.ResizeThis();

            this.flag_collecting_data = false;
        }

        public void KillUpdates()
        {
            this.flag_Alive = false;
        }

        /**********************************************************************
        * This is where the work is done
        */
        [STAThread]
        protected virtual void tick_GraphFrame()
        {
            int new_frame_number = 0;
            int current_frame_number = 0;

            this.slide_FrameSelect.Dispatcher.Invoke(() => this.slide_FrameSelect.Value = 0);

            if (this.flag_GraphingFrame)
                return;
            this.flag_GraphingFrame = true;

            while (this.flag_Alive)
            {
                if (!this.pnl_2DMap.Visible && !this.flag_FrameTypeChanged)
                {
                    Thread.Sleep(200);
                    continue;
                }

                if (this.flag_ResizeThis && !this.flag_Resizing)
                {
                    this.flag_Resizing = true;
                    this.flag_ResizeThis = false;
                    Invoke(new ThreadStart(ResizeThis));
                }

                try
                {
                    while (this.flag_update2DGraph && this.flag_Alive)
                    {
                        this.flag_update2DGraph = false;

                        if (this.flag_FrameTypeChanged)
                        {
                            this.flag_FrameTypeChanged = false;
                            this.Filter_FrameType(this.ptr_UIMFDatabase.CurrentFrameType);
                            this.ptr_UIMFDatabase.CurrentFrameIndex = 0;
                        }

                        if (this.ptr_UIMFDatabase.GetNumberOfFrames(this.ptr_UIMFDatabase.CurrentFrameType) <= 0)
                        {
                            this.flag_update2DGraph = false;
                            break;
                        }

                        if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                        {
                            this.Graph_2DPlot();
                            this.flag_update2DGraph = false;
                            break;
                        }

                        current_frame_number = this.ptr_UIMFDatabase.LoadFrame(this.ptr_UIMFDatabase.CurrentFrameIndex);
                        if (new_frame_number != current_frame_number)
                        {
                            new_frame_number = current_frame_number;

                            this.update_CalibrationCoefficients();
                        }

                        if (this.ptr_UIMFDatabase.CurrentFrameIndex < this.ptr_UIMFDatabase.GetNumberOfFrames(this.ptr_UIMFDatabase.CurrentFrameType))
                        {
                            //#if false
                            if (this.menuItem_ScanTime.Checked)
                            {
                                // MessageBox.Show("tof scan time: " + this.mean_TOFScanTime.ToString());
                                // Get the mean TOF scan time
                                this.mean_TOFScanTime = this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                                if (this.mean_TOFScanTime <= 0)
                                {
                                    this.menuItem_Mobility.PerformClick();
                                }
                            }

                            if ((this.current_minMobility != this.new_minMobility) ||
                                (this.current_maxMobility != this.new_maxMobility) ||
                                (this.current_maxBin != this.new_maxBin) ||
                                (this.current_minBin != this.new_minBin))
                            {
                                if (this.new_minMobility < 0)
                                    this.current_minMobility = 0;
                                else
                                    this.current_minMobility = this.new_minMobility;

                                if (this.new_maxMobility > this.maximum_Mobility)
                                    this.current_maxMobility = this.maximum_Mobility;
                                else
                                    this.current_maxMobility = this.new_maxMobility;

                                if (this.new_maxBin > this.maximum_Bins)
                                    this.current_maxBin = this.maximum_Bins;
                                else
                                    this.current_maxBin = this.new_maxBin;
                                if (this.new_minBin < 0)
                                    this.current_minBin = 0;
                                else
                                    this.current_minBin = this.new_minBin;
                            }

                            try
                            {
                               //  MessageBox.Show(this, "slide_FrameSelect.Value: " + slide_FrameSelect.Value.ToString()+"("+this.current_frame_index.ToString()+")");
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

                            if (this.flag_CinemaPlot)
                            {
                                this.elementHost_FrameSelect.Invoke(new MethodInvoker(delegate
                                {
                                if ((this.slide_FrameSelect.Value + this.Cinemaframe_DataChange >= 0) &&
                                    (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange <= this.slide_FrameSelect.Maximum))
                                {
                                    this.slide_FrameSelect.Value += this.Cinemaframe_DataChange;
                                }
                                else
                                {
                                    if (this.Cinemaframe_DataChange > 0)
                                    {
                                        this.pb_PlayRightIn_Click((object) null, (EventArgs) null);
                                        this.slide_FrameSelect.Value = this.slide_FrameSelect.Maximum;
                                    }
                                    else
                                    {
                                        this.pb_PlayLeftIn_Click((object) null, (EventArgs) null);
                                        this.slide_FrameSelect.Value = Convert.ToDouble(this.num_FrameRange.Value) - 1;
                                    }
                                }
                                }));

                                this.flag_update2DGraph = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                        MessageBox.Show(this, "cycle_GraphFrame: " + ex.ToString() + "\n\n" + ex.StackTrace.ToString());
                    }));
                }

                this.flag_GraphingFrame = false;
                Thread.Sleep(500);
            }
        }

        /***************************************************************
         * The sections below only display and do not set the following values
         *
         *      this.current_minBin, this.current_maxBin
         *      this.current_minMobility, this.current_maxMobility
         */

        // ///////////////////////////////////////////////////////////////
        // Graph_2DPlot()
        //
        public void Graph_2DPlot()
        {
            int frame_index = this.ptr_UIMFDatabase.CurrentFrameIndex;
            if (frame_index >= this.ptr_UIMFDatabase.GetNumberOfFrames(this.ptr_UIMFDatabase.CurrentFrameType))
            {
                MessageBox.Show("Graph_2DPlot: "+frame_index+"\n\nAttempting to graph frame beyond list");
                return;
            }

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
                    current_maxMobility = new_maxMobility;
                    current_minMobility = new_minMobility;
                    current_maxBin = new_maxBin;
                    current_minBin = new_minBin;

                    current_valuesPerPixelX = (current_maxMobility - current_minMobility + 1 < this.pnl_2DMap.Width) ?
                        -(this.pnl_2DMap.Width / (current_maxMobility - current_minMobility + 1)) : 1;

                    // For initial viz., don't want to expand widths of datasets with few TOFs
                    // if(current_maxMobility == this.imfReader.Experiment_Properties.TOFSpectraPerFrame-1 && current_minMobility== 0)
                    if (current_maxMobility == this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1 && current_minMobility == 0)
                        current_valuesPerPixelX = 1;

                    current_valuesPerPixelY = ((current_maxBin - current_minBin + 1 < this.pnl_2DMap.Height) ?
                        -(this.pnl_2DMap.Height / (current_maxBin - current_minBin + 1)) : ((current_maxBin - current_minBin + 1) / this.pnl_2DMap.Height));

                    // In case current_maxBin - current_minBin + 1 is not evenly divisible by current_valuesPerPixelY, we need to adjust one of
                    // these quantities to make it so.
                    if (current_valuesPerPixelY > 0)
                    {
                        current_maxBin = current_minBin + (this.pnl_2DMap.Height * current_valuesPerPixelY) - 1;
                        // TODO: //this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                        this.waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.DarkBlue);
                    }
                    else
                    {
                        if (current_valuesPerPixelY < -5)
                        {
                            // TODO: //this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.EmptyCircle;
                            this.waveform_TOFPlot.Symbol = new Symbol(SymbolType.Circle, Color.DarkBlue);
                            this.waveform_TOFPlot.Symbol.Fill.Color = Color.Transparent;
                        }
                        else
                        {
                            // TODO: //this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                            this.waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.DarkBlue);
                        }
                    }

                    if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                    {
                        try
                        {
                            this.Generate2DIntensityArray_Chromatogram();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("graph_2dplot chromatogram:  " + ex.ToString());
                        }

                        if (this.flag_Closing)
                            return;

                        this.pnl_2DMap.Size = new Size(this.pnl_2DMap.Width, this.pnl_2DMap.Height);

                        // Identify the picture frame with my new Bitmap.
                        if (this.pnl_2DMap.BackgroundImage == null)
                        {
                            this.pnl_2DMap.BackgroundImage = new Bitmap(this.pnl_2DMap.Width, this.pnl_2DMap.Height);
                            bitmap = new Bitmap(this.pnl_2DMap.Width, this.pnl_2DMap.Height);
                        }

                        // Spit out the data to screen
                        this.DrawBitmap(this.data_2D, this.data_maxIntensity);

                        this.pnl_2DMap.Size = new Size(this.pnl_2DMap.Width, (int)this.plot_TOF.GraphPane.Chart.Rect.Height);
                    }
                    else
                    {
                        try
                        {
                            Generate2DIntensityArray();
                        }
                        catch (Exception ex)
                        {
                            this.BackColor = Color.Black;
                            MessageBox.Show("Graph_2DPlot() generate2dintensityarray(): " + ex.ToString()+"\n\n"+ex.StackTrace.ToString());
                        }
                        // MessageBox.Show("GraphFrame: " + this.data_2D.Length.ToString() + ", " + this.data_2D[0].Length.ToString());

                        if (this.flag_Closing)
                            return;

                        if (data_2D == null)
                            MessageBox.Show("no data");
                        // this.pnl_2DMap.Width = this.data_2D.Length;
                        // this.pnl_2DMap.Height = this.data_2D[0].Length;

                        this.pnl_2DMap.Size = new Size(this.pnl_2DMap.Width, this.pnl_2DMap.Height);

                        // Identify the picture frame with my new Bitmap.
                        if (this.pnl_2DMap.BackgroundImage == null)
                        {
                            this.pnl_2DMap.BackgroundImage = new Bitmap(this.pnl_2DMap.Width, this.pnl_2DMap.Height);
                            bitmap = new Bitmap(this.pnl_2DMap.Width, this.pnl_2DMap.Height);
                        }

                        // Spit out the data to screen
                        this.DrawBitmap(this.data_2D, this.data_maxIntensity);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        MessageBox.Show("Graph_2DPlot:  " + ex.InnerException.ToString() + "\n" + ex.ToString());
                    }
                    else
                    {
                        MessageBox.Show("Graph_2DPlot:  " + ex.ToString());
                    }
                    Console.WriteLine(ex.ToString());
                    this.flag_update2DGraph = true;
                }
            }

            if (!this.flag_isFullscreen)
            {
                if (this.pnl_2DMap.Left + this.pnl_2DMap.Width + 170 > this.Width)
                {
                    //MessageBox.Show(this.Width.ToString() + " < " + (this.pnl_2DMap.Left + this.pnl_2DMap.Width + 170).ToString());
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate { this.Width = this.pnl_2DMap.Left + this.pnl_2DMap.Width + 170; }));
                    }
                    else
                    {
                        this.Width = this.pnl_2DMap.Left + this.pnl_2DMap.Width + 170;
                    }
                    this.flag_ResizeThis = true;
                    //this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
                }

                this.slider_ColorMap.Invalidate();
            }

            this.calc_TIC();

            this.flag_kill_mouse = false;
        }

        #region Drawing

        // Create an image out of the data array
        protected unsafe virtual void DrawBitmap(int[][] new_data2D, int new_maxIntensity)
        {
            if (this.flag_collecting_data)
            {
                return;
            }

            int perPixelY = 1; // this.current_valuesPerPixelY;
            int perPixelX = 1; // this.current_valuesPerPixelX;
            int pos_X = 0;
            if (new_data2D.Length > this.pnl_2DMap.Width)
                perPixelX = 1;
            else
                perPixelX = -(this.pnl_2DMap.Width / new_data2D.Length);

            if (this.current_valuesPerPixelY >= 0)
                perPixelY = 1;
            else
                perPixelY = this.current_valuesPerPixelY;

            LockBitmap();

            double thresholdValue = 0;
            if (this.elementHost_Threshold.InvokeRequired)
            {
                this.elementHost_Threshold.Invoke(new MethodInvoker(delegate { thresholdValue = this.slide_Threshold.Value; }));
            }
            else
            {
                thresholdValue = this.slide_Threshold.Value;
            }

            int threshold = Convert.ToInt32(thresholdValue) - 1;
            float divisor_range = (float)(new_maxIntensity - threshold);
            if (divisor_range <= 0)
                divisor_range = new_maxIntensity; // clears out everything anyway...
            //wfd
            //perPixelY = 1;
            // Start drawing
            try
            {
                // MessageBox.Show("data2d: " + new_data2D[0].Length.ToString());
                int yMax = new_data2D[0].Length;

                for (int y = 0; (y < yMax); y++)
                {
                    // problem with flashing colors.  This fixes it.  Got to figure out how it happened
                    //if ((((yMax - y) * -perPixelY) - 1) > this.pnl_2DMap.Height)
                    //    continue;

                    // Important to ensure each scan line begins at a pixel, not halfway into a pixel, e.g.
                    PixelData* pPixel = (perPixelY > 0) ? PixelAt(0, yMax - y - 1) :
                            PixelAt(0, ((yMax - y) * -perPixelY) - 1);
                    pos_X = 0;
                    for (int x = 0; (x < new_data2D.Length) && (pos_X - perPixelX < this.pnl_2DMap.Width); x++)
                    {
                        PixelData* copyPixel;

                        try
                        {
                            if (new_data2D[x][y] > threshold)
                            {
                                try
                                {
                                    slider_ColorMap.getRGB(((float)(new_data2D[x][y] - threshold)) / divisor_range, pPixel);
                                }
                                catch (Exception ex)
                                {
                                    //MessageBox.Show(ex.ToString());
                                    this.BackColor = Color.Red;
                                    this.Update();
                                    // MessageBox.Show(pos_X.ToString()+", "+y.ToString()+"  "+ex.ToString());
                                }
                            }
                            else
                            {
                                try
                                {
                                    // this will make the background white - doesn't work if the continue; statement is below
                                    pPixel->red = pPixel->green = pPixel->blue = (byte)this.slider_PlotBackground.get_Value();
                                }
                                catch (Exception ex)
                                {
                                    this.BackColor = Color.Blue;
                                    this.Update();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("ERROR: " + (pPixel == null ? "null" : "not null") + "\nX=" + x.ToString() + ", y=" + y.ToString() + "\n" + ex.StackTrace.ToString() + "\n\n" + ex.ToString());
                        }

                        copyPixel = pPixel;
                        pPixel++;
                        pos_X += -perPixelX;
                        //#if false
                        for (int i = 1; (i < -perPixelX) && (pos_X < this.pnl_2DMap.Width); i++)
                        {
                            try
                            {
                                pPixel->blue = copyPixel->blue;
                                pPixel->green = copyPixel->green;
                                pPixel->red = copyPixel->red;
                                pPixel++;
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        //#endif
                    }
                    //#if false
                    try
                    {
                        // this section thickens the squares vertically
                        // Copy the scan line if we have to do many pixels per value
                        for (int i = 1; i < -perPixelY; i++)
                        {
                            if ((yMax - y) * -perPixelY - i < this.pnl_2DMap.Height)
                            {
                                PixelData* copyPixel;
                                try
                                {
                                    copyPixel = PixelAt(0, (yMax - y) * -perPixelY - 1);
                                    pPixel = PixelAt(0, (yMax - y) * -perPixelY - 1 - i);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("arg!:  pixelat problem");
                                    return;
                                }

                                int vert_thickness = new_data2D.Length * Math.Abs(perPixelX);
                                for (int x = 0; x < vert_thickness; x++)
                                {
                                    pPixel->blue = copyPixel->blue;
                                    pPixel->green = copyPixel->green;
                                    pPixel->red = copyPixel->red;
                                    pPixel++;
                                    copyPixel++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("ERROR 2: " + ex.ToString());
                    }
                    //#endif
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DrawBitmap: " + ex.ToString());
                // wfd this is a cheat!!!!  Must fix.  Problem with zooming!
                UnlockBitmap();

                // this.imf_ReadFrame(this.new_frame_index, out frame_Data);
                this.flag_update2DGraph = true;

                this.BackColor = Color.Yellow;

                this.flag_update2DGraph = true;
                return;
            }
            this.BackColor = Color.Silver;
            this.slider_ColorMap.set_MaxIntensity(new_maxIntensity);

            //this.Width = this.pnl_2DMap.Left + this.pnl_2DMap.Width + 170;

            UnlockBitmap();
        }

        private unsafe PixelData* PixelAt(int x, int y)
        {
            return (PixelData*)(pBase + (y * pixel_width) + (x * sizeof(PixelData)));
        }

        private unsafe void LockBitmap()
        {

            Rectangle bounds = new Rectangle(0, 0, this.pnl_2DMap.Width, this.pnl_2DMap.Height);
            // MessageBox.Show("this.plot_Width: " + this.plot_Width.ToString());

            // Figure out the number of bytes in a row
            // This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length.
            pixel_width = this.pnl_2DMap.Width * sizeof(PixelData);
            if (pixel_width % 4 != 0)
            {
                pixel_width = 4 * (pixel_width / 4 + 1);
            }
            //MessageBox.Show("pixel_width: " + pixel_width.ToString());

            tmp_Bitmap = new Bitmap(this.pnl_2DMap.Width, this.pnl_2DMap.Height);
            bitmapData = tmp_Bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        private unsafe void UnlockBitmap()
        {
            try
            {
                tmp_Bitmap.UnlockBits(bitmapData);

                this.bitmap = tmp_Bitmap;
            }
            catch (Exception ex)
            {
                this.BackColor = Color.AliceBlue;
                //  MessageBox.Show("TRAPPED:  unlocking bitmap, destroying and retrying!");
                // this is caused from zooming, changing the max and min values of the axis, etc
                // multiple areas attempting to access the plot.
                this.flag_update2DGraph = true;
            }
            bitmapData = null;
            pBase = null;

            this.pnl_2DMap.BackgroundImage = this.bitmap;
            // this.pnl_2DMap.Refresh();
        }

        private Point PixelSize
        {
            get
            {
                GraphicsUnit unit = GraphicsUnit.Pixel;
                RectangleF bounds = bitmap.GetBounds(ref unit);

                return new Point((int)bounds.Width, (int)bounds.Height);
            }
        }

        protected void DrawRectangle(Graphics g, Point p1, Point p2)
        {
            if (p1 == p2)
                return;
            Pen p = new Pen(Color.LemonChiffon, 1.0f);
            Point[] pts = new Point[5];
            pts[0] = p1;
            pts[1] = new Point(p2.X, p1.Y);
            pts[2] = p2;
            pts[3] = new Point(p1.X, p2.Y);
            pts[4] = p1;

            g.DrawLines(p, pts);
        }

        private void CreateProgressBar()
        {
            try
            {
                Invoke(new ThreadStart(invoke_CreateProgressBar));
            }
            catch (Exception ex)
            {
            }
        }

        private void invoke_CreateProgressBar()
        {
            this.progress_ReadingFile = new System.Windows.Forms.ProgressBar();
            //
            // progress_ReadingFile
            //
            this.progress_ReadingFile.BackColor = System.Drawing.Color.SlateGray;
            this.progress_ReadingFile.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.progress_ReadingFile.Location = new System.Drawing.Point(244, 728);
            this.progress_ReadingFile.Name = "progress_ReadingFile";
            this.progress_ReadingFile.Size = new System.Drawing.Size(512, 12);
            this.progress_ReadingFile.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progress_ReadingFile.TabIndex = 55;
            this.progress_ReadingFile.Value = 11;
            this.progress_ReadingFile.Visible = false;
            this.tab_DataViewer.Controls.Add(this.progress_ReadingFile);

            this.progress_ReadingFile.Top = this.pnl_2DMap.Top + this.pnl_2DMap.Height / 2;
            this.progress_ReadingFile.Left = this.pnl_2DMap.Left;
            this.progress_ReadingFile.Width = this.pnl_2DMap.Width;
            this.progress_ReadingFile.Maximum = (this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames / Convert.ToInt32(this.num_FrameCompression.Value)) + 1;
            this.progress_ReadingFile.Show();

            this.progress_ReadingFile.BringToFront();
        }

        /* ***************************************************************
         * The Axis plots
         */
#if DELEGATE
        delegate void update_axisMobility();
#endif
        protected double[] tic_Mobility;
        protected void plot_axisMobility(double[] tic_mobility)
        {
            if (this.flag_Closing || (tic_mobility == null) || (tic_mobility.Length < 5))
            {
                //MessageBox.Show(this, "tic_mobity is Null");
                return;
            }
            // in a desparate attempt to create safe threads!!!  don't get the UI thread stuff.
#if DELEGATE
            update_axisMobility dlg = delegate()
            {
#endif
            try
            {
                this.tic_Mobility = new double[tic_mobility.Length];
                tic_mobility.CopyTo(tic_Mobility, 0);
                Invoke(new ThreadStart(invoke_axisMobility));
            }
            catch (Exception ex)
            {
                this.flag_update2DGraph = true;
                MessageBox.Show("catch mobility" + ex.ToString());
            }
#if DELEGATE
            };
            dlg.Invoke();
#endif
        }

        protected virtual void invoke_axisMobility()
        {
            double min_MobilityValue;
            double increment_MobilityValue;

            //this.plot_Mobility.ClearRange();

            try
            {
                plot_Mobility.HitSize = (current_valuesPerPixelX >= 1) ? new SizeF(1.0f, 2 * plot_Mobility_HEIGHT) : new SizeF(-current_valuesPerPixelX, 2 * plot_Mobility.Height);

                //	plot_Mobility.Left = DRIFT_PLOT_LOCATION_X;
                //	plot_Mobility.Width = this.pnl_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;

                if (current_valuesPerPixelX < -5)
                {
                    if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                    {
                        //this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                        this.waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, Color.Salmon);
                    }
                    else
                    {
                        //this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.EmptyCircle;
                        this.waveform_MobilityPlot.Symbol = new Symbol(SymbolType.Circle, Color.Salmon);
                        this.waveform_MobilityPlot.Symbol.Fill.Color = Color.Transparent;
                    }
                }
                else
                {
                    //this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                    this.waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, Color.Salmon);
                }

                plot_Mobility.XMax = this.pnl_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;
                double minX = 0;
                double maxX = 0;

                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    if (this.minFrame_Chromatogram < 1)
                    {
                        this.maxFrame_Chromatogram -= this.minFrame_Chromatogram;
                        this.minFrame_Chromatogram = 1;
                    }

                    this.flag_enterMobilityRange = true;
#if !NEEDS_WORK
                    this.maxFrame_Chromatogram = this.ptr_UIMFDatabase.LoadFrame((int)this.slide_FrameSelect.Maximum);
                    this.num_maxMobility.Value = this.num_maxMobility.Maximum = this.maxFrame_Chromatogram;
#else // needs work
                    if (this.minFrame_Chromatogram < 0)
                        this.minFrame_Chromatogram = 0;

                    this.num_minMobility.Value = Convert.ToDecimal(this.minFrame_Chromatogram);
                    if (this.num_minMobility.Value + this.pnl_2DMap.Width > this.num_FrameSelect.Maximum)
                    {
                        this.minFrame_Chromatogram = Convert.ToInt32(this.num_FrameSelect.Maximum) - this.pnl_2DMap.Width;
                        if (this.minFrame_Chromatogram < 0)
                            this.minFrame_Chromatogram = 0;
                        this.num_minMobility.Value = Convert.ToDecimal(this.minFrame_Chromatogram);
                    }

                    //MessageBox.Show("hsb: " + this.hsb_2DMap.Value.ToString() + " " + this.minFrame_Chromatogram.ToString());

                    this.maxFrame_Chromatogram = Convert.ToInt32(this.num_FrameSelect.Maximum);

                    this.num_minMobility.Maximum = this.num_maxMobility.Maximum = this.num_FrameSelect.Maximum;

                    //MessageBox.Show(this.num_FrameSelect.Maximum.ToString() + "  " + this.minFrame_Chromatogram.ToString()+"  "+(this.minFrame_Chromatogram + tic_Mobility.Length).ToString());
                    //       this.num_maxMobility.Value = Convert.ToDecimal(this.minFrame_Chromatogram + tic_Mobility.Length);
                    //    else

                    if (this.chromatogram_valuesPerPixelX < 0)
                        this.num_maxMobility.Value = this.num_minMobility.Value + (this.pnl_2DMap.Width / -this.chromatogram_valuesPerPixelX); // Convert.ToDecimal(tic_Mobility.Length);
                    else
                        this.num_maxMobility.Value = this.num_minMobility.Value + this.pnl_2DMap.Width; // Convert.ToDecimal(tic_Mobility.Length);
#endif
                    this.flag_enterMobilityRange = false;

                    //MessageBox.Show("OK");

                    // MessageBox.Show(this.uimf_FrameParameters.Accumulations.ToString());
                    if ((this.mean_TOFScanTime == 0) || this.flag_Chromatogram_Frames)
                    {
                        //this.plot_Mobility.PlotY(tic_Mobility, (double)0, 1.0 * Convert.ToDouble(this.num_FrameCompression.Value));
                        this.plot_Mobility.GraphPane.CurveList[0].Points = new BasicArrayPointList(Enumerable.Range(0, tic_Mobility.Length).Select(x => x * Convert.ToDouble(this.num_FrameCompression.Value)).ToArray(), tic_Mobility);

                        //this.xAxis_Mobility.Caption = "Frame Number";
                        this.plot_Mobility.GraphPane.XAxis.Title.Text = "Frame Number";

                        minX = 0;
                        maxX = (tic_Mobility.Length - 1) * Convert.ToDouble(this.num_FrameCompression.Value) - 1; // TODO: Revisit this last "- 1" - why is it needed?
                    }
                    else
                    {
                        increment_MobilityValue = this.mean_TOFScanTime * (this.maximum_Mobility + 1) * this.ptr_UIMFDatabase.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations) / 1000000.0 / 1000.0;
                        //this.plot_Mobility.PlotY(tic_Mobility, (double)this.minFrame_Chromatogram * increment_MobilityValue, increment_MobilityValue);
                        this.plot_Mobility.GraphPane.CurveList[0].Points = new BasicArrayPointList(Enumerable.Range(0, tic_Mobility.Length).Select(x => x * increment_MobilityValue + this.minFrame_Chromatogram * increment_MobilityValue).ToArray(), tic_Mobility);

                        //this.xAxis_Mobility.Caption = "Frames - Time (sec)";
                        this.plot_Mobility.GraphPane.XAxis.Title.Text = "Frames - Time (sec)";

                        minX = (double)this.minFrame_Chromatogram * increment_MobilityValue;
                        maxX = (tic_Mobility.Length - 1) * increment_MobilityValue + this.minFrame_Chromatogram * increment_MobilityValue - 1; // TODO: Revisit this last "- 1" - why is it needed?
                    }
                }
                else
                {
                    if (this.current_minMobility < 0)
                    {
                        this.current_maxMobility -= this.current_minMobility;
                        this.current_minMobility = 0;
                    }

                    if (this.flag_viewMobility)
                    {
                        // these values are used to prevent the values from changing during the plotting... yikes!
                        min_MobilityValue = this.current_minMobility;
                        increment_MobilityValue = 1.0;
                        // TODO: //this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F0");
                        this.plot_Mobility.GraphPane.XAxis.Scale.Format = "F0";
                        //this.plot_Mobility.PlotY(tic_Mobility, 0, this.current_maxMobility - this.current_minMobility + 1, min_MobilityValue, increment_MobilityValue);
                        this.plot_Mobility.GraphPane.CurveList[0].Points = new BasicArrayPointList(Enumerable.Range(0, this.current_maxMobility - this.current_minMobility + 1).Select(x => x * increment_MobilityValue + min_MobilityValue).ToArray(),
                            tic_Mobility.Take(this.current_maxMobility - this.current_minMobility + 1).ToArray());

                        minX = min_MobilityValue;
                        maxX = (this.current_maxMobility - this.current_minMobility + 1) * increment_MobilityValue + min_MobilityValue - 1; // TODO: Revisit this last "- 1" - why is it needed?
                    }
                    else
                    {
                        // these values are used to prevent the values from changing during the plotting... yikes!
                        min_MobilityValue = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
                        increment_MobilityValue = mean_TOFScanTime / 1000000.0;
                        // TODO: //this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F2");
                        this.plot_Mobility.GraphPane.XAxis.Scale.Format = "F2";
                        //this.plot_Mobility.PlotY(tic_Mobility, min_MobilityValue, increment_MobilityValue);
                        this.plot_Mobility.GraphPane.CurveList[0].Points = new BasicArrayPointList(Enumerable.Range(0, tic_Mobility.Length).Select(x => x * increment_MobilityValue + min_MobilityValue).ToArray(), tic_Mobility);

                        minX = min_MobilityValue;
                        maxX = (tic_Mobility.Length - 1) * increment_MobilityValue + min_MobilityValue - 1; // TODO: Revisit this last "- 1" - why is it needed?
                    }

                    // set min and max here, they will not adjust to zooming
                    this.flag_enterMobilityRange = true; // prevent events form occurring.
                    this.num_minMobility.Value = Convert.ToDecimal(this.current_minMobility);

                    this.hsb_2DMap.Maximum = this.maximum_Mobility - (this.current_maxMobility - this.current_minMobility);
                    this.vsb_2DMap.Maximum = this.maximum_Bins - (this.current_maxBin - this.current_minBin);
                    this.hsb_2DMap.Minimum = 0;
                    this.vsb_2DMap.Minimum = 0;

                    this.hsb_2DMap.Value = this.current_minMobility;
                    if (this.vsb_2DMap.Maximum > this.current_minBin)
                        this.vsb_2DMap.Value = this.vsb_2DMap.Maximum - this.current_minBin;
                    else
                        this.vsb_2DMap.Value = 0;

                    this.hsb_2DMap.SmallChange = 30; // (this.current_maxMobility - this.current_minMobility) / 5;
                    this.hsb_2DMap.LargeChange = 60; // (this.current_maxMobility - this.current_minMobility) * 4 / 5;
                    this.vsb_2DMap.SmallChange = (this.current_maxBin - this.current_minBin) / 5;
                    this.vsb_2DMap.LargeChange = (this.current_maxBin - this.current_minBin) * 4 / 5;

                    this.num_minMobility.Maximum = this.num_maxMobility.Maximum = this.maximum_Mobility;
                    if (this.current_maxMobility > this.maximum_Mobility)
                        this.current_maxMobility = this.maximum_Mobility;
                    this.num_maxMobility.Value = Convert.ToDecimal(this.current_maxMobility);
                    this.num_minMobility.Increment = this.num_maxMobility.Increment = Convert.ToDecimal((this.current_maxMobility - this.current_minMobility) / 3);
                }

                this.plot_Mobility.GraphPane.XAxis.Scale.Min = minX;// - 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                this.plot_Mobility.GraphPane.XAxis.Scale.Max = maxX;// + 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                this.plot_Mobility.GraphPane.AxisChange();
                this.plot_Mobility.Refresh();
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

#if DELEGATE
        delegate void update_axisTOF();
#endif
        protected double[] tic_TOF;
        protected void plot_axisTOF(double[] tof)
        {
            if (this.flag_Closing || (tof == null) || (tof.Length < 5))
                return;

            // in a desparate attempt to create safe threads!!!  don't get the UI thread stuff.
#if DELEGATE
            update_axisTOF dlg = delegate()
            {
#endif
            try
            {
                this.tic_TOF = new double[tof.Length];
                tof.CopyTo(tic_TOF, 0);
                Invoke(new ThreadStart(invoke_axisTOF));
            }
            catch (Exception ex)
            {
                this.BackColor = Color.Pink;
                this.flag_update2DGraph = true;
                //MessageBox.Show("catch tof");
            }
#if DELEGATE
            };
            dlg.Invoke();
#endif
        }

        protected virtual void invoke_axisTOF()
        {
            double min_BinValue;
            double increment_BinValue;

            try
            {
                // s_data = new double[tof.Length];
                // Array.Copy(tof, s_data, tof.Length);
                this.flag_enterBinRange = true;
                double minY = 0;
                double maxY = 0;

                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    if (this.minMobility_Chromatogram < 0)
                        this.minMobility_Chromatogram = 0;
                    this.num_minBin.Value = Convert.ToDecimal(this.minMobility_Chromatogram);

                    if (this.maxMobility_Chromatogram > this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1)
                        this.maxMobility_Chromatogram = this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1;
                    this.num_maxBin.Value = Convert.ToDecimal(this.maxMobility_Chromatogram);

                    if (this.flag_viewMobility)
                    {
                        //this.plot_TOF.PlotX(tic_TOF, this.minMobility_Chromatogram, 1.0);
                        this.waveform_TOFPlot.Points = new BasicArrayPointList(tic_TOF, Enumerable.Range(this.minMobility_Chromatogram, tic_TOF.Length).Select(x => (double) x).ToArray());

                        minY = this.minMobility_Chromatogram;
                        maxY = (tic_TOF.Length - 1) + this.minMobility_Chromatogram;
                    }
                    else
                    {
                        //this.plot_TOF.PlotX(tic_TOF, this.minMobility_Chromatogram, this.ptr_UIMFDatabase.UIMF_FrameParameters.AverageTOFLength / 1000000.0);
                        this.waveform_TOFPlot.Points = new BasicArrayPointList(tic_TOF, Enumerable.Range(0, tic_TOF.Length).Select(x => this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000.0 * x + this.minMobility_Chromatogram).ToArray());

                        minY = this.minMobility_Chromatogram;
                        maxY = this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000.0 * (tic_TOF.Length - 1) + this.minMobility_Chromatogram;
                    }
                }
                else
                {
                    if (flag_display_as_TOF)
                    {
                        double min_TOF = (this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                        double max_TOF = (this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                        double increment_TOF = (max_TOF - min_TOF) / (double)(this.pnl_2DMap.Height);
                        if (current_valuesPerPixelY < 0)
                            increment_TOF *= (double)-current_valuesPerPixelY;

                        this.num_maxBin.Value = Convert.ToDecimal(max_TOF);
                        this.num_minBin.Value = Convert.ToDecimal(min_TOF);
                        this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((max_TOF - min_TOF) / 3);

                        min_BinValue = min_TOF;
                        increment_BinValue = increment_TOF;

                        //this.plot_TOF.Update();
                        //this.plot_TOF.Enabled = false;
                        //this.plot_TOF.PlotX(tic_TOF, min_BinValue, increment_BinValue); //wfd
                        this.waveform_TOFPlot.Points = new BasicArrayPointList(tic_TOF, Enumerable.Range(0, tic_TOF.Length).Select(x => increment_BinValue * x + min_BinValue).ToArray());

                        minY = min_BinValue;
                        maxY = increment_BinValue * (tic_TOF.Length - 1) + min_BinValue;
                    }
                    else
                    {
                        // Confirmed working... 061213
                        // Much more difficult to find where the mz <-> TOF index correlation
                        double mzMin = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                        double mzMax = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);

                        double increment_TOF = (mzMax - mzMin) / (double)this.pnl_2DMap.Height;
                        if (current_valuesPerPixelY < 0)
                            increment_TOF *= (double)-current_valuesPerPixelY;

                        this.num_maxBin.Value = Convert.ToDecimal(mzMax);
                        this.num_minBin.Value = Convert.ToDecimal(mzMin);

                        min_BinValue = mzMin;
                        increment_BinValue = increment_TOF;

                        //this.plot_TOF.Update();
                        //this.plot_TOF.Enabled = false;
                        //this.plot_TOF.PlotX(tic_TOF, min_BinValue, increment_BinValue); //wfd
                        this.waveform_TOFPlot.Points = new BasicArrayPointList(tic_TOF, Enumerable.Range(0, tic_TOF.Length).Select(x => increment_BinValue * x + min_BinValue).ToArray());

                        minY = min_BinValue;
                        maxY = increment_BinValue * (tic_TOF.Length - 1) + min_BinValue;
                    }
                }

                this.plot_TOF.GraphPane.YAxis.Scale.Min = minY;// - 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                this.plot_TOF.GraphPane.YAxis.Scale.Max = maxY;// + 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                this.plot_TOF.GraphPane.AxisChange();
                this.plot_TOF.Refresh();
                //this.plot_TOF.Enabled = true;
                this.flag_enterBinRange = false;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Plot Axis Mobility: " + ex.StackTrace.ToString() + "\n\n" + ex.ToString());
                this.plot_TOF.BackColor = Color.OrangeRed;
                Thread.Sleep(100);
                this.flag_update2DGraph = true;
            }
        }

        #endregion
    }
}
