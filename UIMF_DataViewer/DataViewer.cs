#define COMPRESS_TO_100K

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using UIMFLibrary;
using System.Diagnostics;
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

        private int current_frame_type;
        private bool flag_isTIMS = false;

        private bool flag_isFullscreen = false;

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

            this.cb_FrameType.SelectedIndex = this.ptr_UIMFDatabase.get_FrameType();
            this.Filter_FrameType(this.ptr_UIMFDatabase.get_FrameType());
            this.ptr_UIMFDatabase.CurrentFrameIndex = 0;

            this.ptr_UIMFDatabase.set_FrameType(current_frame_type, true);
            this.cb_FrameType.SelectedIndexChanged += new System.EventHandler(this.cb_FrameType_SelectedIndexChanged);

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

#if COMPRESS_TO_100K
            // MessageBox.Show("initializeCalibrants: " + this.UIMF_DataReader.mz_Calibration.k.ToString());
            this.pnl_postProcessing.InitializeCalibrants(1, this.ptr_UIMFDatabase.UimfFrameParams.CalibrationSlope, this.ptr_UIMFDatabase.UimfFrameParams.CalibrationIntercept);
#else
            this.pnl_postProcessing.InitializeCalibrants(this.UIMF_GlobalParams.BinWidth, this.UIMF_DataReader.m_frameParameters.CalibrationSlope, this.UIMF_DataReader.m_frameParameters.CalibrationIntercept);
#endif

            this.pnl_postProcessing.tb_SaveDecodeFilename.Text = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);
            this.pnl_postProcessing.tb_SaveDecodeDirectory.Text = Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile);

            if (this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth != .25)
                this.pnl_postProcessing.gb_Compress4GHz.Hide();
            else
            {
                this.pnl_postProcessing.btn_Compress1GHz.Click += new System.EventHandler(this.btn_Compress1GHz_Click);
                this.pnl_postProcessing.tb_SaveCompressFilename.Text = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);
                this.pnl_postProcessing.tb_SaveCompressDirectory.Text = Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile);
            }
        }

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
            this.slider_PlotBackground.btn_GreyValue.Move += new EventHandler(this.slider_Background_Move);

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

                this.menuItem_SelectionCorners.Click += new System.EventHandler(this.menuItem_SelectionCorners_Click);
                this.menuItem_ScanTime.Click += new System.EventHandler(this.ScanTime_ContextMenu);
                this.menuItem_Mobility.Click += new System.EventHandler(this.Mobility_ContextMenu);
                this.menuItem_ExportCompressed.Click += new System.EventHandler(this.menuItem_ExportCompressed_Click);
                this.menuItem_ExportComplete.Click += new System.EventHandler(this.menuItem_ExportComplete_Click);
                this.menuItem_ExportAll.Click += new System.EventHandler(this.menuItem_ExportAll_Click);
                this.menuItem_SuperFrame.Click += new System.EventHandler(this.menuItem_SuperFrame_Click);
                this.menuItem_SuperExperiment.Click += new System.EventHandler(this.menuItem_SuperExperiment_Click);
                this.menuItem_CopyToClipboard.Click += new System.EventHandler(this.menuItem_CopyToClipboard_Click);
                this.menuItem_CaptureExperimentFrame.Click += new System.EventHandler(this.menuItem_CaptureExperimentFrame_Click);
                this.menuItem_SaveIMF.Click += new System.EventHandler(this.menuitem_SaveIMF_Click);
                this.menuItem_WriteUIMF.Click += new System.EventHandler(this.menuitem_WriteUIMF_Click);
                this.menuItem_UseScans.Click += new System.EventHandler(this.menuItem_UseScans_Click);
                this.menuItem_UseDriftTime.Click += new System.EventHandler(this.menuItem_UseDriftTime_Click);
                this.menuItem_Exportnew_driftTIC.Click += new System.EventHandler(this.menuItem_ExportDriftTIC_Click);
                this.menuItem_Frame_driftTIC.Click += new System.EventHandler(this.menuItem_Frame_driftTIC_Click);
                this.menuItem_Time_driftTIC.Click += new System.EventHandler(this.menuItem_Time_driftTIC_Click);
                this.menuItem_TOFExport.Click += new System.EventHandler(this.menuItem_TOFExport_Click);
                this.menuItem_TOFMaximum.Click += new System.EventHandler(this.menuItem_TOFMaximum_Click);
                this.menuItemZoomFull.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItemZoomPrevious.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItemZoomOut.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItem_MaxIntensities.Click += new System.EventHandler(this.menuItem_TOFMaximum_Click);
                this.menuItemConvertToMZ.Click += new System.EventHandler(this.ConvertContextMenu);
                this.menuItemConvertToTOF.Click += new System.EventHandler(this.ConvertContextMenu);

                this.pnl_2DMap.DoubleClick += new System.EventHandler(this.pnl_2DMap_DblClick);
                this.pnl_2DMap.MouseLeave += new System.EventHandler(this.pnl_2DMap_MouseLeave);
                this.pnl_2DMap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pnl_2DMap_MouseMove);
                this.pnl_2DMap.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnl_2DMap_MouseDown);
                this.pnl_2DMap.Paint += new System.Windows.Forms.PaintEventHandler(this.pnl_2DMap_Paint);
                this.pnl_2DMap.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pnl_2DMap_MouseUp);

                //this.plot_Mobility.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_Mobility_MouseDown);
                this.plot_Mobility.ContextMenu = contextMenu_driftTIC;
                this.plot_Mobility.RangeChanged += new UIMF_File.Utilities.RangeEventHandler(this.OnPlotTICRangeChanged);
                this.pb_PlayRightIn.Click += new System.EventHandler(this.pb_PlayRightIn_Click);
                this.pb_PlayLeftOut.Click += new System.EventHandler(this.pb_PlayLeftOut_Click);
                this.pb_PlayLeftIn.Click += new System.EventHandler(this.pb_PlayLeftIn_Click);
                this.pb_PlayRightOut.Click += new System.EventHandler(this.pb_PlayRightOut_Click);
                this.num_FrameIndex.ValueChanged += new System.EventHandler(this.num_FrameIndex_ValueChanged);
                this.cb_EnableMZRange.CheckedChanged += new System.EventHandler(this.cb_EnableMZRange_CheckedChanged);
                this.num_MZ.ValueChanged += new System.EventHandler(this.num_MZ_ValueChanged);
                this.num_PPM.ValueChanged += new System.EventHandler(this.num_PPM_ValueChanged);
                this.lbl_FramesShown.Click += new System.EventHandler(this.lbl_FramesShown_Click);
                this.btn_setCalDefaults.Click += new System.EventHandler(this.btn_setCalDefaults_Click);

                this.num_minMobility.ValueChanged += new System.EventHandler(this.num_Mobility_ValueChanged);
                this.num_maxMobility.ValueChanged += new System.EventHandler(this.num_Mobility_ValueChanged);
                this.num_maxBin.ValueChanged += new System.EventHandler(this.num_maxBin_ValueChanged);
                this.num_minBin.ValueChanged += new System.EventHandler(this.num_minBin_ValueChanged);
                //this.plot_TOF.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_TOF_MouseDown);
                this.plot_TOF.ContextMenu = contextMenu_TOF;

                this.rb_CompleteChromatogram.CheckedChanged += new System.EventHandler(this.rb_CompleteChromatogram_CheckedChanged);
                this.rb_PartialChromatogram.CheckedChanged += new System.EventHandler(this.rb_PartialChromatogram_CheckedChanged);
                this.num_FrameCompression.ValueChanged += new System.EventHandler(this.num_FrameCompression_ValueChanged);

                this.btn_TIC.Click += new System.EventHandler(this.btn_TIC_Click);

                this.num_FrameRange.ValueChanged += new System.EventHandler(this.num_FrameRange_ValueChanged);
                this.slide_FrameSelect.ValueChanged += this.slide_FrameSelect_ValueChanged;

                this.vsb_2DMap.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vsb_2DMap_Scroll);
                this.hsb_2DMap.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsb_2DMap_Scroll);

                this.tb_CalT0.Click += new System.EventHandler(this.CalibratorT0_Changed);
                this.tb_CalT0.Leave += new System.EventHandler(this.CalibratorT0_Changed);
                this.tb_CalA.Click += new System.EventHandler(this.CalibratorA_Changed);
                this.tb_CalA.Leave += new System.EventHandler(this.CalibratorA_Changed);

                this.btn_Reset.Click += new System.EventHandler(this.btn_Reset_Clicked);
                this.slide_Threshold.ValueChanged += this.slide_Threshold_ValueChanged;
                this.btn_revertCalDefaults.Click += new System.EventHandler(this.btn_revertCalDefaults_Click);

                this.pnl_postProcessing.btn_AttemptCalibration.Click += new System.EventHandler(this.btn_CalibrateFrames_Click);
                this.pnl_postProcessing.btn_ManualCalibration.Click += new System.EventHandler(this.btn_ApplyCalculatedCalibration_Click);
                this.pnl_postProcessing.btn_ExperimentCalibration.Click += new System.EventHandler(this.btn_ApplyCalibration_Experiment_Click);

                this.tabpages_Main.DrawItem += new DrawItemEventHandler(this.tabpages_Main_DrawItem);
                this.tabpages_Main.SelectedIndexChanged += new EventHandler(this.tabpages_Main_SelectedIndexChanged);

                for (int i = 0; i < this.slider_ColorMap.btn_Slider.Length; i++)
                    this.slider_ColorMap.btn_Slider[i].MouseUp += new System.Windows.Forms.MouseEventHandler(this.ColorSelector_Change);
                this.slider_ColorMap.lbl_MaxIntensity.MouseEnter += new System.EventHandler(this.show_MaxIntensity);

                this.Resize += new EventHandler(this.IonMobilityDataView_Resize);
              //  this.tabpages_Main.Resize += new EventHandler(this.tabpages_Main_Resize);

#if BELOV_TRANSFORM
                this.pnl_postProcessing.btn_DecodeMultiplexing.Click += new System.EventHandler(this.btn_DecodeMultiplexing_Click);
#endif
                this.AllowDrop = true;
                this.DragDrop += new System.Windows.Forms.DragEventHandler(DataViewer_DragDrop);
                this.DragEnter += new System.Windows.Forms.DragEventHandler(DataViewer_DragEnter);
                this.lb_DragDropFiles.SelectedIndexChanged +=new EventHandler(lb_DragDropFiles_SelectedIndexChanged);

                this.pb_PlayDownOut.MouseDown += new MouseEventHandler(this.pb_PlayDownOut_MOUSEDOWN);
                this.pb_PlayDownOut.MouseUp += new MouseEventHandler(this.pb_PlayDownOut_MOUSEUP);
                this.pb_PlayUpOut.MouseDown += new MouseEventHandler(this.pb_PlayUpOut_MOUSEDOWN);
                this.pb_PlayUpOut.MouseUp += new MouseEventHandler(this.pb_PlayUpOut_MOUSEUP);
            }

            this.tabpages_Main.Width = this.ClientSize.Width + ((this.tabpages_Main.Height - this.tab_DataViewer.ClientSize.Height) / 2);
            this.tabpages_Main.Height = this.ClientSize.Height + (this.tabpages_Main.Height - this.tab_DataViewer.ClientSize.Height);
            this.tabpages_Main.Left = 0;
            this.tabpages_Main.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            this.flag_Resizing = true;
            Invoke(new ThreadStart(this.ResizeThis));
        }

        private static Microsoft.Win32.RegistryKey MainKey
        {
            get { return Application.UserAppDataRegistry; }
        }

        private void tabpages_Main_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (this.tabpages_Main.SelectedTab == this.tab_PostProcessing)
            {
                if (this.Width < this.pnl_postProcessing.dg_Calibrants.Left + this.pnl_postProcessing.dg_Calibrants.Width + 70)
                {
                    this.Width = this.pnl_postProcessing.dg_Calibrants.Left + this.pnl_postProcessing.dg_Calibrants.Width + 70;
                    this.tabpages_Main.Width = this.Width;
                }
            }
            else if (this.tabpages_Main.SelectedTab == this.tab_InstrumentSettings)
            {
                this.pnl_InstrumentSettings.update_Frame(this.ptr_UIMFDatabase.CurrentFrameIndex, this.ptr_UIMFDatabase.GetFrameParams(this.ptr_UIMFDatabase.CurrentFrameIndex));
            }
        }

        private void tabpages_Main_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            Font fntTab;
            Brush bshFore;
            Brush bshBack;
            Font tab_font = new System.Drawing.Font("Comic Sans MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            if (e.Index != this.tabpages_Main.SelectedIndex)
            {
                fntTab = new Font(e.Font, FontStyle.Bold);

                bshFore = Brushes.Ivory;
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, Color.RoyalBlue, Color.DimGray, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                e.Graphics.FillRectangle(bshBack, e.Bounds);
            }
            else
            {
                fntTab = new Font(e.Font, FontStyle.Regular);

                bshFore = Brushes.Black;
                //bshBack = new SolidBrush(Color.WhiteSmoke);   // Color.GhostWhite);
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, Color.White, Color.WhiteSmoke, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                e.Graphics.FillRectangle(bshBack, e.Bounds);
            }

            string tabName = this.tabpages_Main.TabPages[e.Index].Text;
            System.Drawing.SizeF s = e.Graphics.MeasureString(tabName, tab_font);

            e.Graphics.RotateTransform(270.0f);
            e.Graphics.TranslateTransform(-s.Width, 0);
            // MessageBox.Show((e.Bounds.Left).ToString()+","+ (e.Bounds.Top).ToString());
            e.Graphics.DrawString(tabName, tab_font, bshFore, -e.Bounds.Top - 28, e.Bounds.Left + 4);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            // moving the application on and off the screen causes the
            // pb_2DGraph to rewrite - while the other paint event locks the bits.
            // ignore the system paint.
        }

        // /////////////////////////////////////////////////////////////////////////////////////////////
        // resize start at the left side, top to bottom
        //
        public void IonMobilityDataView_Resize(object obj, System.EventArgs e)
        {
            this.flag_ResizeThis = true;
        }

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

        protected virtual void Zoom(Point p1, Point p2)
        {
            lock (this.lock_graphing)
            {
                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                // Prep variables
                float min_Px = Math.Min(p1.X, p2.X);
                float max_Px = Math.Max(p1.X, p2.X);
                float min_Py = this.pnl_2DMap.Height - Math.Max(p1.Y, p2.Y);
                float max_Py = this.pnl_2DMap.Height - Math.Min(p1.Y, p2.Y);

                // don't zoom if the user mistakenly presses the mouse button
                if ((max_Px - min_Px < -this.current_valuesPerPixelX) && (max_Py - min_Py < -this.current_valuesPerPixelY))
                    return;

                // Calculate the data enclosing boundaries
                // Need to do new_maxMobility first since new_minMobilitychanges beforehand
                new_maxMobility = (current_valuesPerPixelX == 1) ? (int)max_Px : (int)(new_minMobility + (max_Px / -current_valuesPerPixelX));
                new_minMobility = (current_valuesPerPixelX == 1) ? (int)min_Px : (int)(new_minMobility + (min_Px / -current_valuesPerPixelX));

                if (new_maxMobility - new_minMobility < MIN_GRAPHED_MOBILITY)
                {
                    new_minMobility -= (MIN_GRAPHED_MOBILITY - (new_maxMobility - new_minMobility)) / 2;
                    new_maxMobility = new_minMobility + MIN_GRAPHED_MOBILITY;
                }

                // MessageBox.Show(new_maxMobility.ToString()+", "+new_minMobility.ToString());
                if ((min_Py != 0) || (max_Py != this.pnl_2DMap.Height))
                {
                    if (this.current_valuesPerPixelY < 0)
                    {
                        new_maxBin = (int)this.ptr_UIMFDatabase.GetBinForPixel((int)max_Py / -this.current_valuesPerPixelY);
                        new_minBin = (int)this.ptr_UIMFDatabase.GetBinForPixel((int)min_Py / -this.current_valuesPerPixelY);
                    }
                    else
                    {
                        new_maxBin = (int)this.ptr_UIMFDatabase.GetBinForPixel((int)max_Py);
                        new_minBin = (int)this.ptr_UIMFDatabase.GetBinForPixel((int)min_Py);
                    }
                }

                if (this.new_maxMobility - this.new_minMobility < MIN_GRAPHED_MOBILITY)
                {
                    this.new_maxMobility = ((this.new_maxMobility + this.new_minMobility) / 2) + (MIN_GRAPHED_MOBILITY / 2);
                    this.new_minMobility = ((this.new_maxMobility + this.new_minMobility) / 2) - (MIN_GRAPHED_MOBILITY / 2);
                }

                if (this.new_minMobility < 0)
                    this.new_minMobility = 0;
                if (this.new_maxMobility > this.maximum_Mobility)
                    this.new_maxMobility = this.maximum_Mobility;

                if (this.new_minBin < 0)
                    this.new_minBin = 0;
                if (this.new_maxBin > this.maximum_Bins)
                    this.new_maxBin = this.maximum_Bins;

                // save new zoom...
                _zoomX.Add(new Point(new_minMobility, new_maxMobility));
                _zoomBin.Add(new Point(new_minBin, new_maxBin));

                this.current_maxBin = this.new_maxBin;
                this.current_minBin = this.new_minBin;

                this.flag_update2DGraph = true;
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
                        this.current_minMobility, data_width, this.current_minBin, data_height, this.current_valuesPerPixelY, this.data_2D, min_MZRange_bin, max_MZRange_bin);
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
            int total_frames = this.ptr_UIMFDatabase.get_NumFrames(this.ptr_UIMFDatabase.get_FrameType());
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

        // ////////////////////////////////////////////////////////////////////////////
        // change the background color
        //
        // private void slider_Background_MouseUp(object obj, System.Windows.Forms.MouseEventArgs e)
        private void slider_Background_Move(object obj, System.EventArgs e)
        {
            if (this.pnl_2DMap != null)
            {
                this.slider_PlotBackground.Update();
                this.flag_update2DGraph = true;

                if (this.slider_PlotBackground.get_Value() >= 250)
                {
                    this.Opacity = .75;
                    this.TopMost = true;
                }
                else if (this.Opacity != 1.0)
                {
                    this.Opacity = 1.0;
                    this.TopMost = false;
                }
            }
        }

        //wfd
        private void pnl_2DMap_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if ((this.flag_kill_mouse) || // if plotting the plot, prevent zooming!
                (this.flag_CinemaPlot))
                return;

            // Graphics g = this.pnl_2DMap.CreateGraphics();
            // g.DrawString(e.X.ToString(), new Font(FontFamily.GenericSerif, 10, FontStyle.Regular), new SolidBrush(Color.Yellow), 10, 50);

            if (this.menuItem_SelectionCorners.Checked && (e.Button == MouseButtons.Middle))
                MessageBox.Show("Mouse at " + e.X.ToString() + ", " + e.Y.ToString() + (this.inside_Polygon_Pixel(e.X, this.pnl_2DMap.Height - e.Y) ? " is inside" : " is outside"));

            // Starting a zoom process
            if (e.Button == MouseButtons.Left)
            {
                if ((e.X > this.pnl_2DMap.Width - 17) && (e.Y < 17))
                {
                    this.flag_isFullscreen = !this.flag_isFullscreen;
                    if (this.flag_isFullscreen)
                    {
                        this.max_plot_height = this.tab_DataViewer.ClientSize.Height - 400;
                        this.max_plot_width = this.tab_DataViewer.ClientSize.Width - 100;
                    }
                    else
                    {
                        this.max_plot_width = this.tab_DataViewer.ClientSize.Width;
                        this.max_plot_height = this.tab_DataViewer.ClientSize.Height;
                    }

                    this.flag_ResizeThis = true;
                    this.flag_update2DGraph = true;
                }

                _mouseDragging = true;

                this.Cursor = Cursors.Cross;
                _mouseDownPoint = new Point(e.X, e.Y);
                _mouseMovePoint = new Point(e.X, e.Y);
            }

            // Pop-up Menu
            if (e.Button == MouseButtons.Right)
                contextMenu_pb_2DMap.Show(this, new Point(e.X + this.pnl_2DMap.Left, e.Y + this.pnl_2DMap.Top));

            for (int i = 0; i < 4; i++)
            {
                if ((Math.Abs(e.X - this.corner_2DMap[i].X) <= 6) && (Math.Abs(e.Y - this.corner_2DMap[i].Y) <= 6))
                {
                    this.flag_MovingCorners = i;
                    return;
                }
            }

            // this section draws the intensities on the different pixels if they are big
            // enough.  Lot of waste; but it only occurs when the mouse is pressed down.
            //
            // wfd:  this will do for now.  I am sure there is a much more efficient method of
            // handling this.  For now, the race is on.
            if ((current_valuesPerPixelY < -10) && (current_valuesPerPixelX < -20))
            {
                this.pnl_2DMap_Extensions = this.pnl_2DMap.CreateGraphics();
                for (int i = 0; i <= this.data_2D.Length - 1; i++)
                    for (int j = 0; j <= this.data_2D[0].Length - 1; j++)
                    {
                        if (this.data_2D[i][j] != 0)
                        {
                            this.pnl_2DMap_Extensions.DrawString(this.data_2D[i][j].ToString("#"), map_font, back_brush, (i * -this.current_valuesPerPixelX) + 1, this.pnl_2DMap.Height - ((j + 1) * -this.current_valuesPerPixelY) - 1);
                            this.pnl_2DMap_Extensions.DrawString(this.data_2D[i][j].ToString("#"), map_font, fore_brush, (i * -this.current_valuesPerPixelX), this.pnl_2DMap.Height - ((j + 1) * -this.current_valuesPerPixelY));
                        }
                    }
            }
        }

        protected int prev_cursorX = 0;
        protected int prev_cursorY = 0;
        protected virtual void pnl_2DMap_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.flag_kill_mouse) // if plotting the plot, prevent zooming!
                return;

            if ((Math.Abs(prev_cursorX - e.X) > 3) || (Math.Abs(prev_cursorY - e.Y) > 3))
            {
                prev_cursorX = e.X;
                prev_cursorY = e.Y;
                UpdateCursorReading(e);
            }
            else
                return;

            if (this.flag_MovingCorners >= 0)
            {
                if (e.X < 0)
                    this.corner_2DMap[this.flag_MovingCorners].X = 0;
                else if (e.X > this.pnl_2DMap.Width)
                    this.corner_2DMap[this.flag_MovingCorners].X = this.pnl_2DMap.Width;
                else
                    this.corner_2DMap[this.flag_MovingCorners].X = e.X;

                if (e.Y < 0)
                    this.corner_2DMap[this.flag_MovingCorners].Y = 0;
                else if (e.Y > this.pnl_2DMap.Height)
                    this.corner_2DMap[this.flag_MovingCorners].Y = this.pnl_2DMap.Height;
                else
                    this.corner_2DMap[this.flag_MovingCorners].Y = e.Y;

                this.pnl_2DMap.Invalidate();
                return;
            }

            // Draw a rectangle along with the dragging mouse.
            if (_mouseDragging) //&& !toolBar1.Buttons[0].Pushed)
            {
                int x, y;
                // Ensure that the mouse point does not overstep its bounds
                x = Math.Min(e.X, this.pnl_2DMap.Width - 1);
                x = Math.Max(x, 0);
                y = Math.Min(e.Y, this.pnl_2DMap.Height - 1);
                y = Math.Max(y, 0);

                _mouseMovePoint = new Point(x, y);

                this.pnl_2DMap.Invalidate();
            }
        }

        /* Taken from Robert Sedgewick, Algorithms in C++ */
        /*  returns whether, in traveling from the first to the second
    	to the third point, we turn counterclockwise (+1) or not (-1) */
        int ccw(Point p0, Point p1, Point p2)
        {
            int dx1, dx2, dy1, dy2;

            dx1 = p1.X - p0.X;
            dy1 = p1.Y - p0.Y;

            dx2 = p2.X - p0.X;
            dy2 = p2.Y - p0.Y;

            if (dx1 * dy2 > dy1 * dx2)
                return +1;
            if (dx1 * dy2 < dy1 * dx2)
                return -1;
            if ((dx1 * dx2 < 0) || (dy1 * dy2 < 0))
                return -1;
            if ((dx1 * dx1 + dy1 * dy1) < (dx2 * dx2 + dy2 * dy2))
                return +1;
            return 0;
        }

        public bool inside_Polygon(int scan, int bin)
        {
            int x_pixel;
            int y_pixel;

            if (this.current_valuesPerPixelX == 1)
                x_pixel = scan;
            else
                x_pixel = (scan - this.current_minMobility) * -this.current_valuesPerPixelX;

            if (current_valuesPerPixelY > 0)
                y_pixel = ((bin - this.current_minBin) / current_valuesPerPixelY);
            else
                y_pixel = ((bin - this.current_minBin) * -current_valuesPerPixelY);
            return this.inside_Polygon_Pixel(x_pixel, y_pixel);
        }

        public bool inside_Polygon(int scan, double mz)
        {
            int x_pixel;
            int y_pixel;

            if (this.current_valuesPerPixelX == 1)
                x_pixel = scan;
            else
                x_pixel = (scan - this.current_minMobility) * -this.current_valuesPerPixelX;

            // to find the y_pixel, the mz is linearized vertically.
            double height = this.pnl_2DMap.Height;
            double mzMax = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
            double mzMin = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
            y_pixel = (int)(height * (mz-mzMin) / (mzMax - mzMin));

            return this.inside_Polygon_Pixel(x_pixel, y_pixel);
        }

        public bool inside_Polygon_Pixel(int x_pixel, int y_pixel)
        {
            if (this.menuItem_SelectionCorners.Checked)
            {
                // in situations where you are zoomed in to where the points are larger than a pixel, you need to compensate.
                if (this.current_valuesPerPixelX < 0)
                    x_pixel *= -this.current_valuesPerPixelX;
                if (this.current_valuesPerPixelY < 0)
                    y_pixel *= -this.current_valuesPerPixelY;

                y_pixel = this.pnl_2DMap.Height - y_pixel;

                // due to strange shapes, I have to split this into triangles for results to be correct.
                Point pt = new Point(x_pixel, y_pixel);
                if ((ccw(this.corner_2DMap[0], this.corner_2DMap[1], pt) > 0) &&
                    (ccw(this.corner_2DMap[1], this.corner_2DMap[2], pt) > 0) &&
                    (ccw(this.corner_2DMap[2], this.corner_2DMap[0], pt) > 0))
                    return true;

                if ((ccw(this.corner_2DMap[2], this.corner_2DMap[3], pt) > 0) &&
                    (ccw(this.corner_2DMap[3], this.corner_2DMap[0], pt) > 0) &&
                    (ccw(this.corner_2DMap[0], this.corner_2DMap[2], pt) > 0)) // counter clockwise
                    return true;

                return false;
            }
            else
                return true;
        }

        void swap_corners(int index1, int index2)
        {
            //MessageBox.Show("swap corners:  "+index1.ToString()+"   "+index2.ToString());

            Point tmp_point = this.corner_2DMap[index1];
            this.corner_2DMap[index1] = this.corner_2DMap[index2];
            this.corner_2DMap[index2] = tmp_point;
        }

        // make the points for the most outter points starting in the upper left corner
        private void convex_Polygon()
        {
            //MessageBox.Show("convex Polygon:  ");
            if (ccw(this.corner_2DMap[0], this.corner_2DMap[1], this.corner_2DMap[2]) < 0) // counter clockwise
                this.swap_corners(1, 2);
            if (ccw(this.corner_2DMap[1], this.corner_2DMap[2], this.corner_2DMap[3]) < 0) // counter clockwise
                this.swap_corners(2, 3);
            if (ccw(this.corner_2DMap[2], this.corner_2DMap[3], this.corner_2DMap[0]) < 0) // counter clockwise
                this.swap_corners(3, 0);
            if (ccw(this.corner_2DMap[3], this.corner_2DMap[0], this.corner_2DMap[1]) < 0) // counter clockwise
                this.swap_corners(0, 1);

            int upper_left = 0;
            int smallest_dist = 1000000000;
            int dist;
            for (int i = 0; i < 4; i++)
            {
                dist = (this.corner_2DMap[i].Y ^ 2) + (this.corner_2DMap[i].X ^ 2);
                if (dist < smallest_dist)
                {
                    upper_left = i;
                    smallest_dist = dist;
                }
            }

            for (int i = 0; i < upper_left; i++) // if upper left is 0, then we are good to go
            {
                Point tmp_point = this.corner_2DMap[0];
                this.corner_2DMap[0] = this.corner_2DMap[1];
                this.corner_2DMap[1] = this.corner_2DMap[2];
                this.corner_2DMap[2] = this.corner_2DMap[3];
                this.corner_2DMap[3] = tmp_point;
            }
        }

        private void pnl_2DMap_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.flag_kill_mouse)  // if plotting the plot, prevent zooming!
                return;

            if (this.flag_MovingCorners >= 0)
            {
                convex_Polygon();

                // ensure there are no negative angles.
                this.flag_MovingCorners = -1; // no more moving corner

                this.flag_update2DGraph = true;
            }

            if (this.pnl_2DMap_Extensions != null)
            {
                this.pnl_2DMap.Refresh();
                this.pnl_2DMap_Extensions = null;
            }

            this.Cursor = Cursors.Default;

            int minframe_Data_number;
            int maxframe_Data_number;
            int min_select_mobility;
            int max_select_mobility;

            // Zoom the image in...
            if (e.Button == MouseButtons.Left)
            {
                // most likely a double click
                if ((Math.Abs(this._mouseDownPoint.X - this._mouseMovePoint.X) < 3) &&
                    (Math.Abs(this._mouseDownPoint.Y - this._mouseMovePoint.Y) < 3))
                {
                    _mouseMovePoint = _mouseDownPoint;
                    this._mouseDragging = false;
                    return;
                }

                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    this.minFrame_Chromatogram = 0;
                    this.maxFrame_Chromatogram = this.ptr_UIMFDatabase.set_FrameType(this.current_frame_type) - 1;

                    // select the range of frames
                    if (this.chromatogram_valuesPerPixelX < 0)
                    {
                        if (this._mouseDownPoint.X > this._mouseMovePoint.X)
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);

                           // minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                          //  maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                          //  MessageBox.Show("here");

                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            //minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                            //maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                        }
                    }
                    else // we have compressed the chromatogram
                    {
                        if (this._mouseDownPoint.X > this._mouseMovePoint.X)
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            //minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * this.chromatogram_valuesPerPixelX) + 1;
                            //maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * this.chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                            //minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * this.chromatogram_valuesPerPixelX) + 1;
                            //maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * this.chromatogram_valuesPerPixelX) + 1;
                        }
                    }

                  //  MessageBox.Show("wfd: " + maxframe_Data_number.ToString() + " - " + minframe_Data_number.ToString() + " + 1");
                    if (minframe_Data_number < 1)
                        minframe_Data_number = 1;
                    if (maxframe_Data_number > this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames)
                        maxframe_Data_number = this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames;
                    this.slide_FrameSelect.Value = maxframe_Data_number;

                  //  MessageBox.Show("wfd: "+maxframe_Data_number.ToString()+" - "+minframe_Data_number.ToString()+" + 1");
                    this.num_FrameRange.Value = maxframe_Data_number - minframe_Data_number + 1;

                    this.plot_Mobility.StopAnnotating(false);

                    // select the mobility highlight
                    // select the range of frames
                    //MessageBox.Show(this.chromatogram_valuesPerPixelY.ToString());
                    if (this.current_valuesPerPixelY < 0)
                    {
                        if (this._mouseDownPoint.Y > this._mouseMovePoint.Y)
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseMovePoint.Y) / -this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseDownPoint.Y) / -this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = min_select_mobility;
                            this.selection_max_drift = max_select_mobility;
                        }
                        else
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseDownPoint.Y) / -this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseMovePoint.Y) / -this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = max_select_mobility;
                            this.selection_max_drift = min_select_mobility;
                        }
                    }
                    else
                    {
                        if (this._mouseDownPoint.Y > this._mouseMovePoint.Y)
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseMovePoint.Y) * this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseDownPoint.Y) * this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = min_select_mobility;
                            this.selection_max_drift = max_select_mobility;
                        }
                        else
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseDownPoint.Y) * this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pnl_2DMap.Height - this._mouseMovePoint.Y) * this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = max_select_mobility;
                            this.selection_max_drift = min_select_mobility;
                        }
                    }

                    this.flag_selection_drift = true;
                    //this.plot_Mobility.SetRange(min_select_mobility, max_select_mobility);

                    this.rb_PartialChromatogram.Checked = false;
                    this.rb_CompleteChromatogram.Checked = false;

                    this.new_minBin = 0;
                    this.new_minMobility = 0;
                    this.new_maxBin = this.maximum_Bins;
                    this.new_maxMobility = this.maximum_Mobility;

                    this.AutoScrollPosition = new Point(0, 0);
                    this.ptr_UIMFDatabase.CurrentFrameIndex = (int)this.slide_FrameSelect.Value;

                    this.Chromatogram_CheckedChanged();
                }
                else if (this._mouseDragging)
                {
                    this.Zoom(_mouseMovePoint, _mouseDownPoint);
                }
            }

            // This will erase the rectangle
            _mouseMovePoint = _mouseDownPoint;
            _mouseDragging = false;

            //   this._selecting_oblong_region = false;

            this.flag_update2DGraph = true;
        }

        private bool flag_Painting = false;
        protected virtual void pnl_2DMap_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            int w;
            int xl;
            int xwidth;
            int min_mobility;

            if (this.pnl_2DMap.BackgroundImage == null)
                return;

            if (this.flag_Painting)
                return;
            this.flag_Painting = true;

            // DrawImage seems to make the selection box more responsive.
            if (!this.rb_CompleteChromatogram.Checked && !this.rb_PartialChromatogram.Checked)
                e.Graphics.DrawImage(this.pnl_2DMap.BackgroundImage, 0, 0);

            if (_mouseDragging) //&& !toolBar1.Buttons[0].Pushed)
                this.DrawRectangle(e.Graphics, _mouseDownPoint, _mouseMovePoint);

            //   this.plot_Height = this.data_2D[0].Length;
            //   this.plot_Width = this.data_2D.Length;

            // this section draws the highlight on the plot.
            if (this.flag_selection_drift)
            {
                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    w = this.pnl_2DMap.Width / this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames;
                    xl = (this.selection_min_drift * w);

                    // if (this.current_valuesPerPixelX < 0)
                    //     xl += this.current_valuesPerPixelX;
                    xwidth = (this.selection_max_drift - this.selection_min_drift + 1) * w;
                }
                else
                {
                    if (this.flag_viewMobility)
                        min_mobility = this.new_minMobility;
                    else
                        min_mobility = (int)(((double)this.new_minMobility) * (this.mean_TOFScanTime / 1000000));

                    w = this.pnl_2DMap.Width / (this.current_maxMobility - this.current_minMobility + 1);
                    xl = ((this.selection_min_drift - min_mobility) * w);

                    //MessageBox.Show("here");
                    // if (this.current_valuesPerPixelX < 0)
                    //     xl += this.current_valuesPerPixelX;
                    xwidth = (this.selection_max_drift - this.selection_min_drift + 1) * w;
                    // e.Graphics.DrawString("(" + this.selection_min_drift.ToString() + " - " + min_mobility.ToString() + ") * " + w.ToString() + " = " + xl.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 10);
                    //e.Graphics.DrawString((this.mean_TOFScanTime / 1000000).ToString() + " ... " + min_mobility.ToString() + "    (" + this.selection_max_drift.ToString() + " - " + this.selection_min_drift.ToString() + " + 1) * " + w.ToString() + " = " + xwidth.ToString() + " ... " + new_minMobility.ToString() + ", " + new_maxMobility.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 50);
                }
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(145, 111, 111, 126)), xl, 0, xwidth, this.pnl_2DMap.Height);
            }

            if (!this.flag_CinemaPlot)
            {
                if (this.flag_isFullscreen)
                    e.Graphics.DrawImage(this.pb_Shrink.BackgroundImage, this.pnl_2DMap.Width - 17, 2);
                else
                    e.Graphics.DrawImage(this.pb_Expand.BackgroundImage, this.pnl_2DMap.Width - 17, 2);
            }

            this.draw_Corners(e.Graphics);

            this.flag_Painting = false;
        }

        public void draw_Corners(Graphics g)
        {
            if (!this.menuItem_SelectionCorners.Checked)
                return;

            // shade the outside of the selected polygon
            Point[][] points = new Point[4][];
            for (int i = 0; i < 4; i++)
                points[i] = new Point[4];

            points[0][0] = points[3][0] = this.corner_2DMap[0]; // top left
            points[0][1] = points[1][0] = this.corner_2DMap[1]; // top right
            points[2][0] = points[1][1] = this.corner_2DMap[2]; // bot right
            points[2][1] = points[3][1] = this.corner_2DMap[3]; // bot left

            points[0][3] = points[3][3] = new Point(0, 0); // top left
            points[0][2] = points[1][3] = new Point(this.pnl_2DMap.Width, 0); // top right
            points[2][3] = points[1][2] = new Point(this.pnl_2DMap.Width, this.pnl_2DMap.Height); // bot right
            points[2][2] = points[3][2] = new Point(0, this.pnl_2DMap.Height); // bot left

            for (int i = 0; i < 4; i++)
                g.FillPolygon(new SolidBrush(Color.FromArgb(144, 111, 11, 111)), points[i]);

            g.DrawLine(thick_pen, this.corner_2DMap[0].X, this.corner_2DMap[0].Y, this.corner_2DMap[1].X, this.corner_2DMap[1].Y);
            g.DrawLine(thick_pen, this.corner_2DMap[1].X, this.corner_2DMap[1].Y, this.corner_2DMap[2].X, this.corner_2DMap[2].Y);
            g.DrawLine(thick_pen, this.corner_2DMap[2].X, this.corner_2DMap[2].Y, this.corner_2DMap[3].X, this.corner_2DMap[3].Y);
            g.DrawLine(thick_pen, this.corner_2DMap[3].X, this.corner_2DMap[3].Y, this.corner_2DMap[0].X, this.corner_2DMap[0].Y);

            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), this.corner_2DMap[0].X - 2, this.corner_2DMap[0].Y - 2, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), this.corner_2DMap[1].X - 5, this.corner_2DMap[1].Y - 2, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), this.corner_2DMap[2].X - 5, this.corner_2DMap[2].Y - 5, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), this.corner_2DMap[3].X - 2, this.corner_2DMap[3].Y - 5, 8, 8);
        }

        public void reset_Corners()
        {
            if (this.menuItem_SelectionCorners.Checked)
            {
                this.corner_2DMap[0] = new Point((int)(this.pnl_2DMap.Width * .15), (int)(this.pnl_2DMap.Height * .15));
                this.corner_2DMap[1] = new Point((int)(this.pnl_2DMap.Width * .85), (int)(this.pnl_2DMap.Height * .15));
                this.corner_2DMap[2] = new Point((int)(this.pnl_2DMap.Width * .85), (int)(this.pnl_2DMap.Height * .85));
                this.corner_2DMap[3] = new Point((int)(this.pnl_2DMap.Width * .15), (int)(this.pnl_2DMap.Height * .85));
            }
        }

        // Handler for the pb_2DMap's ContextMenu
        protected virtual void ZoomContextMenu(object sender, System.EventArgs e)
        {
            // Who sent you?
            if (sender == this.menuItemZoomFull)
            {
                // Reinitialize
                _zoomX.Clear();
                _zoomBin.Clear();

                this.new_minBin = 0;
                this.new_minMobility = 0;
                this.new_maxBin = this.maximum_Bins;
                this.new_maxMobility = this.maximum_Mobility;

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                this.flag_update2DGraph = true;
            }
            else if (sender == this.menuItemZoomPrevious)
            {
                if (_zoomX.Count < 2)
                {
                    this.pnl_2DMap_DblClick((object)null, (System.EventArgs)null);
                    return;
                }
                new_minMobility = ((Point)_zoomX[_zoomX.Count - 2]).X;
                new_maxMobility = ((Point)_zoomX[_zoomX.Count - 2]).Y;

                new_minBin = ((Point)_zoomBin[_zoomBin.Count - 2]).X;
                new_maxBin = ((Point)_zoomBin[_zoomBin.Count - 2]).Y;

                _zoomX.RemoveAt(_zoomX.Count - 1);
                _zoomBin.RemoveAt(_zoomBin.Count - 1);

                this.flag_update2DGraph = true;
            }
            else if (sender == this.menuItemZoomOut) // double the view window
            {
                int temp = this.current_maxMobility - this.current_minMobility + 1;
                new_minMobility = this.current_minMobility - (temp / 3) - 1;
                if (new_minMobility < 0)
                    this.new_minMobility = 0;
                new_maxMobility = this.current_maxMobility + (temp / 3) + 1;
                if (this.new_maxMobility > this.maximum_Mobility)
                    this.new_maxMobility = this.maximum_Mobility - 1;

                temp = this.current_maxBin - this.current_minBin + 1;
                new_minBin = this.current_minBin - temp - 1;
                if (new_minBin < 0)
                    new_minBin = 0;
                new_maxBin = this.current_maxBin + temp + 1;
                if (new_maxBin > this.maximum_Bins)
                    new_maxBin = this.maximum_Bins - 1;

                _zoomX.Add(new Point(new_minMobility, new_maxMobility));
                _zoomBin.Add(new Point(new_minBin, new_maxBin));

                this.flag_update2DGraph = true;

                //this.Zoom(new System.Drawing.Point(new_minMobility, new_maxBin), new System.Drawing.Point(new_maxMobility, new_minBin));
            }
        }

        private void Mobility_ContextMenu(object sender, System.EventArgs e)
        {
            this.flag_viewMobility = true;

            this.flag_update2DGraph = true;

            this.menuItem_Mobility.Checked = true;
            this.menuItem_ScanTime.Checked = false;
        }

        private void ScanTime_ContextMenu(object sender, System.EventArgs e)
        {
            if (this.mean_TOFScanTime == -1.0)
            {
                MessageBox.Show(this, "The mean scan time is not available for this frame.");
                return;
            }
            this.flag_viewMobility = false;

            this.flag_update2DGraph = true;

            this.menuItem_Mobility.Checked = false;
            this.menuItem_ScanTime.Checked = true;
        }

        private void ConvertContextMenu(object sender, System.EventArgs e)
        {
            if (sender == menuItemConvertToMZ)
            {
                menuItemConvertToMZ.Checked = true;
                menuItemConvertToTOF.Checked = false;

                if (!this.rb_PartialChromatogram.Checked && !this.rb_CompleteChromatogram.Checked)
                {
                    this.flag_display_as_TOF = false;
                    this.flag_update2DGraph = true;
                }
            }
            else if (sender == menuItemConvertToTOF)
            {
                flag_display_as_TOF = true;
                menuItemConvertToMZ.Checked = false;
                menuItemConvertToTOF.Checked = true;

                if (!this.rb_PartialChromatogram.Checked && !this.rb_CompleteChromatogram.Checked)
                    this.flag_update2DGraph = true;
            }
        }

        private void menuItem_UseScans_Click(object sender, System.EventArgs e)
        {
            _useDriftTime = false;
            menuItem_UseScans.Checked = true;
            menuItem_UseDriftTime.Checked = false;
        }

        private void menuItem_UseDriftTime_Click(object sender, System.EventArgs e)
        {
            _useDriftTime = true;
            menuItem_UseDriftTime.Checked = true;
            menuItem_UseScans.Checked = false;
        }

        private void menuItem_SetScanTime_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            SolidBrush b = new SolidBrush(e.ForeColor);
            e.DrawBackground(); //Draw the menu item background
            e.Graphics.DrawString(((MenuItem)sender).Text, SystemInformation.MenuFont, b, e.Bounds.Left + 16, e.Bounds.Top + 2, StringFormat.GenericTypographic);
            b.Dispose();
        }

        private void menuItem_SetScanTime_MeasureItem(object sender, System.Windows.Forms.MeasureItemEventArgs e)
        {
            System.Drawing.SizeF s = e.Graphics.MeasureString(((MenuItem)sender).Text, SystemInformation.MenuFont, 1024, StringFormat.GenericTypographic);
            s.Width += SystemInformation.MenuCheckSize.Width; // for the checkmark if any
            e.ItemHeight = (int)s.Height + 5;
            e.ItemWidth = (int)s.Width;
        }

        private const Int32 SRCCOPY = 0xCC0020;
        private void menuItem_CaptureExperimentFrame_Click(object sender, System.EventArgs e)
        {
            string folder = Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile);
            string exp_name = Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile);
            string filename = folder + "\\" + exp_name + ".Accum_" + this.ptr_UIMFDatabase.CurrentFrameIndex.ToString("0000") + ".BMP";
            this.SaveExperimentGUI(filename);

            MessageBox.Show(this, "Image capture for Frame saved to Desktop in file: \n" + filename);
        }

        public void SaveExperimentGUI(string thumbnail_path)
        {
            int save_width = this.tabpages_Main.Width;
            int save_height = this.tabpages_Main.Height;

            this.Update();
            using (Graphics g1 = CreateGraphics())
            {
                Image experiment_image = new Bitmap(save_width, save_height, g1);
                using (Graphics g2 = Graphics.FromImage(experiment_image))
                {
                    IntPtr dc1 = g1.GetHdc();
                    IntPtr dc2 = g2.GetHdc();
                    BitBlt(dc2, 0, 0, save_width, save_height, dc1, 0, 0, SRCCOPY);
                    g2.ReleaseHdc(dc2);
                    g1.ReleaseHdc(dc1);
                }

                experiment_image.Save(thumbnail_path, ImageFormat.Bmp);
            }
        }

        private void btn_Export_Click(object sender, System.EventArgs e)
        {
            MessageBox.Show("btn_Export_Click   IonMobilityDataView");
#if false
            int[] tof;
            int[] intensities;
            int tic;

            frame_Data.GetSpectra(export_Spectra, out tof, out intensities, out tic);
            System.IO.StreamWriter w = new System.IO.StreamWriter("c:\\spectra" + export_Spectra.ToString() + ".csv");

            for (int i = 0; i < tof.Length; i++)
            {
                if (i == 0)
                    w.WriteLine("{0},{1},{2}", tof[i], intensities[i], tic);
                else
                    w.WriteLine("{0},{1}", tof[i], intensities[i]);
            }
            w.Close();
#endif
        }

        private void menuItem_Frame_driftTIC_Click(object sender, System.EventArgs e)
        {
            this.menuItem_Time_driftTIC.Checked = false;
            this.menuItem_Frame_driftTIC.Checked = true;

            this.flag_Chromatogram_Frames = true;
        }

        private void menuItem_Time_driftTIC_Click(object sender, System.EventArgs e)
        {
            this.menuItem_Time_driftTIC.Checked = true;
            this.menuItem_Frame_driftTIC.Checked = false;

            this.flag_Chromatogram_Frames = false;
        }

        private void menuItem_ExportDriftTIC_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            save_dialog.Title = "Select a file to export data to...";
            save_dialog.Filter = "Comma-separated variables (*.csv)|*.csv";
            save_dialog.FilterIndex = 1;

            //this.plot_Mobility.PlotY(tic_Mobility, (double)this.minFrame_Chromatogram * increment_MobilityValue, increment_MobilityValue);

            if (save_dialog.ShowDialog(this) == DialogResult.OK)
            {
                System.IO.StreamWriter w = new System.IO.StreamWriter(save_dialog.FileName);
                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    double increment_MobilityValue = this.mean_TOFScanTime * (this.maximum_Mobility + 1) * this.ptr_UIMFDatabase.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations) / 1000000.0 / 1000.0;
                    for (int i = 0; i < tic_Mobility.Length; i++)
                    {
                        w.WriteLine("{0},{1}", (i*increment_MobilityValue) + this.minFrame_Chromatogram, tic_Mobility[i]);
                    }
                }
                else
                {
                    double increment_MobilityValue = mean_TOFScanTime / 1000000.0;
                    double min_MobilityValue = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
                    for (int i = 0; i < tic_Mobility.Length; i++)
                    {
                        w.WriteLine("{0},{1}", (i * increment_MobilityValue) + min_MobilityValue, tic_Mobility[i]);
                    }
                }
                w.Close();
            }
        }

        private void IonMobilityDataView_Closed(object sender, System.EventArgs e)
        {
            RegistrySave(Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
            ptr_UIMFDatabase.Dispose();
        }

        private void pnl_2DMap_MouseLeave(object sender, System.EventArgs e)
        {
            _interpolation_points.Clear();
        }

        protected virtual void pnl_2DMap_DblClick(object sender, System.EventArgs e)
        {
            int frame_number;

            if (this.flag_CinemaPlot)
            {
                this.StopCinema();
                return;
            }

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Width = this.pnl_2DMap.Left + this.ptr_UIMFDatabase.UimfFrameParams.Scans + 170;

                this.rb_PartialChromatogram.Checked = false;
                this.rb_CompleteChromatogram.Checked = false;

                this.plot_Mobility.StopAnnotating(false);

                this.Chromatogram_CheckedChanged();

                // MessageBox.Show(this.chromatogram_valuesPerPixelX.ToString());
                //if (this.chromatogram_valuesPerPixelX < 0)
                frame_number = this.minFrame_Chromatogram + (this.prev_cursorX * (this.maxFrame_Chromatogram - this.minFrame_Chromatogram) / this.pnl_2DMap.Width);
                //MessageBox.Show(frame_number.ToString());

#if false
                    frame_number = this.minFrame_Chromatogram + (this.prev_cursorX * Convert.ToInt32(this.num_FrameCompression.Value) / (-this.chromatogram_valuesPerPixelX)) + 1;
                else
                    frame_number = this.minFrame_Chromatogram + (this.prev_cursorX * Convert.ToInt32(this.num_FrameCompression.Value)) + 1;
#endif
                // MessageBox.Show(frame_number.ToString()+"="+this.minFrame_Chromatogram.ToString() + "  " + this.prev_cursorX.ToString() + "  " + this.current_valuesPerPixelX.ToString());

                if (frame_number < 1)
                    frame_number = 1;
                if (frame_number > this.ptr_UIMFDatabase.get_NumFrames(this.current_frame_type))
                    frame_number = this.ptr_UIMFDatabase.get_NumFrames(this.current_frame_type) - 1;

                this.slide_FrameSelect.Value = frame_number;

                this.ptr_UIMFDatabase.CurrentFrameIndex = (int)this.slide_FrameSelect.Value;
                this.plot_Mobility.ClearRange();
                this.num_FrameRange.Value = 1;

                this.vsb_2DMap.Show();  // gets hidden with Chromatogram
                this.hsb_2DMap.Show();

                // this.imf_ReadFrame(this.new_frame_index, out frame_Data);
                this.max_plot_width = this.ptr_UIMFDatabase.UimfFrameParams.Scans;
                this.flag_update2DGraph = true;
            }
            else
            {
                // Reinitialize
                _zoomX.Clear();
                _zoomBin.Clear();

                this.new_minBin = 0;
                this.new_minMobility = 0;
                this.new_maxBin = this.maximum_Bins;
                this.new_maxMobility = this.maximum_Mobility;

                this.num_minMobility.Value = 0;
                this.num_maxMobility.Value = this.maximum_Mobility;

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();
                this.flag_update2DGraph = true;

                this.AutoScrollPosition = new Point(0, 0);
                // this.ResizeThis();
            }
        }

        private void OnPlotTICRangeChanged(object sender, UIMF_File.Utilities.RangeEventArgs e)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                return;

            Graphics g = this.pnl_2DMap.CreateGraphics();

            this.selection_min_drift = e.Min;
            this.selection_max_drift = e.Max;

            this.flag_selection_drift = e.Selecting;

            this.flag_update2DGraph = true;
        }

        private void menuItem_ExportCompressed_Click(object sender, System.EventArgs e)
        {
            try
            {
                SaveFileDialog save_dialog = new SaveFileDialog();
                save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                save_dialog.Title = "Select a file to export data to...";
                save_dialog.Filter = "Comma-separated values (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                save_dialog.FilterIndex = 1;

                if (save_dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                        this.export_ChromatogramIntensityMatrix(save_dialog.FileName);
                    else
                        this.export_IntensityMatrix(save_dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void menuItem_ExportComplete_Click(object sender, System.EventArgs e)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                MessageBox.Show(this, "This viewer is not prepared to export the chromatogram.  Please request it.");
                return;
            }

            try
            {
                SaveFileDialog save_dialog = new SaveFileDialog();
                save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                save_dialog.Title = "Select a file to export data to...";
                save_dialog.Filter = "Comma-separated values (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                save_dialog.FilterIndex = 1;

                if (save_dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.export_CompleteIntensityMatrix(save_dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // mike wants complete dump.
        private void export_ChromatogramIntensityMatrix(string filename)
        {
            int frames_width = this.ptr_UIMFDatabase.get_NumFrames(this.ptr_UIMFDatabase.get_FrameType());
            double[] frames_axis = new double[frames_width];
            int mob_height = this.ptr_UIMFDatabase.UimfFrameParams.Scans;
            double[] drift_axis = new double[mob_height];

            int [][]dump_chromatogram = new int[frames_width][];
            for (int i=0; i<frames_width; i++)
            {
                dump_chromatogram[i] = this.ptr_UIMFDatabase.GetDriftChromatogram(i);
            }

            for (int i = 1; i < frames_width; i++)
                frames_axis[i] = i;
            for (int i=1; i<mob_height; i++)
                drift_axis[i] = i;

            Utilities.TextExport tex = new Utilities.TextExport();
            tex.Export(filename, "scans\frame", dump_chromatogram, frames_axis, drift_axis);
        }

        protected virtual void export_IntensityMatrix(string filename)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                MessageBox.Show("export_IntensityMatrix needs work chromatogram");
                return;
            }

            int i;
            Generate2DIntensityArray();

            double mob_width = this.data_2D.Length;
            double[] drift_axis = new double[(int)mob_width];

            double tof_height = this.data_2D[0].Length;
            double[] tof_axis = new double[(int)tof_height];

            double increment = mean_TOFScanTime / 1000000.0;
            int bin_value;

            //increment = (((double)(this.current_maxMobility - this.current_minMobility)) * this.mean_TOFScanTime) / mob_width / 1000000.0;
            //drift_axis[0] = this.current_minMobility * this.mean_TOFScanTime / mob_width / 1000000.0;
            drift_axis[0] = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
            for (i = 1; i < mob_width; i++)
                drift_axis[i] = (drift_axis[i - 1] + (double)increment);

            if (flag_display_as_TOF)
            {
                double min_TOF = (this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                double max_TOF = (this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                double increment_TOF = (max_TOF - min_TOF) / ((double)this.pnl_2DMap.Height);
                for (i = 0; i < tof_height; i++)
                {
                    tof_axis[i] = ((double)i * increment_TOF) + min_TOF;
                }
            }
            else
            {
                // linearize the mz and find the bin.
                // calculate the mz, then convert to TOF for all the values.
                double mzMax = Convert.ToDouble(this.num_maxBin.Value);
                double mzMin = Convert.ToDouble(this.num_minBin.Value);

                increment = (mzMax - mzMin) / (tof_height - 1.0);

                tof_axis[0] = mzMin;
                for (i = 1; i < tof_height; i++)
                {
                    tof_axis[i] = (tof_axis[i - 1] + increment);
                }
            }
            Utilities.TextExport tex = new Utilities.TextExport();
            if (flag_display_as_TOF)
                tex.Export(filename, "bin", this.data_2D, drift_axis, tof_axis);
            else
                tex.Export(filename, "m/z", this.data_2D, drift_axis, tof_axis);
        }

        protected virtual void export_CompleteIntensityMatrix(string filename)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                MessageBox.Show("export_IntensityMatrix needs work chromatogram");
                return;
            }

            //string points = "";
            int i,j;
            int minbin = this.current_minBin;
            int maxbin = this.current_maxBin;
            int minmobility = this.current_minMobility;
            int maxmobility = this.current_maxMobility;
            int xpos, ypos;

            Generate2DIntensityArray();
            if (this.menuItem_SelectionCorners.Checked)
            {
                int largest_x = this.corner_2DMap[0].X;
                int largest_y = this.pnl_2DMap.Height - this.corner_2DMap[0].Y;
                int smallest_x = this.corner_2DMap[0].X;
                int smallest_y = this.pnl_2DMap.Height - this.corner_2DMap[0].Y;
                for (i = 1; i < 4; i++)
                {
                    if (largest_x < this.corner_2DMap[i].X)
                        largest_x = this.corner_2DMap[i].X;
                    if (largest_y < this.pnl_2DMap.Height - this.corner_2DMap[i].Y)
                        largest_y = this.pnl_2DMap.Height - this.corner_2DMap[i].Y;

                    if (smallest_x > this.corner_2DMap[i].X)
                        smallest_x = this.corner_2DMap[i].X;
                    if (smallest_y > this.pnl_2DMap.Height - this.corner_2DMap[i].Y)
                        smallest_y = this.pnl_2DMap.Height - this.corner_2DMap[i].Y;
                }

                xpos = this.current_minMobility + (largest_x * (this.current_maxMobility - this.current_minMobility) / this.pnl_2DMap.Width) + 1;
                ypos = this.current_minBin + (largest_y * (this.current_maxBin - this.current_minBin) / this.pnl_2DMap.Height);
                if (xpos < maxmobility)
                    maxmobility = xpos;
                if (ypos < maxbin)
                    maxbin = ypos;

              //  points += xpos.ToString() + ", " + ypos.ToString() + "\n";

                xpos = this.current_minMobility + (smallest_x * (this.current_maxMobility - this.current_minMobility) / this.pnl_2DMap.Width) + 1;
                ypos = this.current_minBin + (smallest_y * (this.current_maxBin - this.current_minBin) / this.pnl_2DMap.Height);
                if (xpos > minmobility)
                    minmobility = xpos;
                if (ypos > minbin)
                    minbin = ypos;

              //  points += xpos.ToString() + ", " + ypos.ToString() + "\n\n";
            }

            int total_scans = maxmobility - minmobility + 1;
            int total_bins = maxbin - minbin + 1;

            //points += maxmobility.ToString() + " - "+minmobility.ToString() + " = "+total_scans.ToString() + "\n ";
            //points += maxbin.ToString()+ " - " + minbin.ToString()+ " = " + total_bins.ToString();
           // MessageBox.Show(points);

            //double mob_width = this.ptr_UIMFDatabase.UIMF_FrameParams.Scans;
            double[] drift_axis = new double[total_scans];

            //double tof_height = this.ptr_UIMFDatabase.UIMF_GlobalParams.Bins;
            double[] tof_axis = new double[total_bins];

            double increment;
            //int bin_value;

            increment = (((double)(this.ptr_UIMFDatabase.UimfFrameParams.Scans)) * this.mean_TOFScanTime) / this.ptr_UIMFDatabase.UimfFrameParams.Scans / 1000000.0;

            drift_axis[0] = ((double)minmobility) * increment;

            for (i = 1; i < total_scans; i++)
                drift_axis[i] = (drift_axis[i - 1] + (double)increment);

            if (flag_display_as_TOF)
            {
                for (i = minbin; i <= maxbin; i++)
                {
                    tof_axis[i - minbin] = ((double)i) * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1.0e-4;
                }
            }
            else
            {
                // linearize the mz and find the bin.
                // calculate the mz, then convert to TOF for all the values.
                for (i = minbin; i <= maxbin; i++)
                {
                    tof_axis[i - minbin] = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(((double)i) * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                }
            }

            // MessageBox.Show(minbin.ToString() + "  mz " + this.ptr_UIMFDatabase.mzCalibration.TOFtoMZ(((double)i) * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin).ToString());
            var export_data = this.ptr_UIMFDatabase.AccumulateFrameData(this.ptr_UIMFDatabase.CurrentFrameNum, this.ptr_UIMFDatabase.CurrentFrameNum,
                this.flag_display_as_TOF, minmobility, maxmobility, minbin, maxbin);
#if false // TODO: OLD
            int[][] export_data = new int[total_scans][];
            for (i = 0; i < total_scans; i++)
            {
                export_data[i] = new int[total_bins];
            }
            export_data = this.ptr_UIMFDatabase.AccumulateFrameDataUncompressed(this.ptr_UIMFDatabase.CurrentFrameIndex, this.flag_display_as_TOF, minmobility, minbin, export_data);
#endif

            // if masking, clear everything outside of mask to zero.
            if (this.menuItem_SelectionCorners.Checked)
            {
                int tics = 0;
                for (i = 0; i < total_scans; i++)
                for (j = 0; j < total_bins; j++)
                {
                    tics += export_data[i][j];
                    //if (!this.inside_Polygon((minmobility + i) * this.pnl_2DMap.Width / (this.current_maxMobility - this.current_minMobility), (minbin + j) * this.pnl_2DMap.Height / (this.current_maxBin - this.current_minBin)))
                    //    export_data[i][j] = 0;
                    // MessageBox.Show(tics.ToString());
                }
            }

            Utilities.TextExport tex = new Utilities.TextExport();
            if (flag_display_as_TOF)
                tex.Export(filename, "bin", export_data, drift_axis, tof_axis);
            else
                tex.Export(filename, "m/z", export_data, drift_axis, tof_axis);
        }

        private void menuItem_ExportAll_Click(object sender, System.EventArgs e)
        {
            MessageBox.Show(this, "menuItem_ExportAll_Click ionmobilitydataview does nothing");
#if false
            if (this.flag_display_as_TOF)
            {
                MessageBox.Show("Exporting requires data to be shown in MZ mode.");
                return;
            }

            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            save_dialog.Title = "Select a file to export data to...";
            save_dialog.Filter = "Comma-separated values (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            save_dialog.FilterIndex = 1;

            if (save_dialog.ShowDialog(this) == DialogResult.OK)
            {
                for (int i = 1; i <= this.slide_FrameSelect.Range.Maximum; i++)
                {
                    this.slide_FrameSelect.Value = i;
                    this.flag_update2DGraph = false;

                    this.imf_ReadFrame(i, out frame_Data);
                   // this.Graph_2DPlot();
                    this.pnl_2DMap.Update();
                    this.slide_FrameSelect.Update();

                    export_IntensityMatrix(save_dialog.FileName.Split('.')[0] + i.ToString("0000") + ".csv");
                }
            }
#endif
        }

        private void menuItem_CopyToClipboard_Click(object sender, System.EventArgs e)
        {
            Clipboard.SetDataObject(this.pnl_2DMap.BackgroundImage);
        }

        private void menuItem_TOFExport_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            save_dialog.Title = "Select a file to export data to...";
            save_dialog.Filter = "Comma-separated variables (*.csv)|*.csv";
            save_dialog.FilterIndex = 1;

            if (save_dialog.ShowDialog(this) == DialogResult.OK)
            {
                System.IO.StreamWriter sw_TOF = new System.IO.StreamWriter(save_dialog.FileName);

                if (this.rb_PartialChromatogram.Checked || this.rb_CompleteChromatogram.Checked)
                {
                    double increment_TOFValue = 1.0;
                    for (int i = 0; i < this.tic_TOF.Length; i++)
                    {
                        sw_TOF.WriteLine("{0},{1}", (i*increment_TOFValue)+this.minMobility_Chromatogram, tic_TOF[i]);
                    }
                }
                else
                {
                    if (flag_display_as_TOF)
                    {
                        double min_TOF = (this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                        double max_TOF = (this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4);
                        double increment_TOF = (max_TOF - min_TOF) / (double)(this.pnl_2DMap.Height);
                        for (int i = 0; i < this.tic_TOF.Length; i++)
                        {
                            sw_TOF.WriteLine("{0},{1}", ((double)i * increment_TOF) + min_TOF, tic_TOF[i]);
                        }
                    }
                    else
                    {
                        int[] saved_intensities = new int[this.ptr_UIMFDatabase.UimfGlobalParams.Bins];
                        int[] frame_intensities;
                        double mz = 0.0;

                        for (int i = this.ptr_UIMFDatabase.CurrentFrameIndex - this.ptr_UIMFDatabase.FrameWidth + 1; i <= this.ptr_UIMFDatabase.CurrentFrameIndex; i++)
                        {
                            frame_intensities = this.ptr_UIMFDatabase.GetSumScans(i, this.current_minMobility, this.current_maxMobility);

                            for (int j = 0; j < this.ptr_UIMFDatabase.UimfGlobalParams.Bins; j++)
                                saved_intensities[j] += frame_intensities[j];
                        }

                        double mzMax = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                        double mzMin = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                        for (int i = 0; i < saved_intensities.Length; i++)
                        {
                            mz = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ((double)i * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                            if ((mz >= mzMin) && (mz <= mzMax))
                                sw_TOF.WriteLine("{0},{1}", mz, saved_intensities[i]);
                        }
                    }
                }
                sw_TOF.Close();
            }
        }

        protected virtual void num_Mobility_ValueChanged(object sender, System.EventArgs e)
        {
            int min, max;

            if (this.flag_enterMobilityRange)
                return;
            this.flag_enterMobilityRange = true;

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.minFrame_Chromatogram = Convert.ToInt32(this.num_minMobility.Value);
                this.maxFrame_Chromatogram = Convert.ToInt32(this.num_maxMobility.Value);

                this.flag_chromatograph_collected_COMPLETE = false;
                this.flag_chromatograph_collected_PARTIAL = false;

                this.flag_update2DGraph = true;
                this.flag_enterMobilityRange = false;
                return;
            }

            min = Convert.ToInt32(this.num_minMobility.Value);
            max = Convert.ToInt32(this.num_maxMobility.Value);

            this.num_minMobility.Increment = this.num_maxMobility.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxMobility.Value) - Convert.ToDouble(this.num_minMobility.Value)) / 4.0);

            new_maxMobility = max;
            new_minMobility = min;

            _zoomX.Add(new Point(min, max));
            _zoomBin.Add(new Point(new_minBin, new_maxBin));

            this.flag_update2DGraph = true;

            this.flag_enterMobilityRange = false;
        }

        // ////////////////////////////////////////////////////////////////////////////////
        // This needs some more work.
        //
        protected virtual void num_minBin_ValueChanged(object sender, System.EventArgs e)
        {
            double bin_diff;
            double min, max;

            if (this.flag_enterBinRange)
                return;
            this.flag_enterBinRange = true;

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.minMobility_Chromatogram = Convert.ToInt32(this.num_minBin.Value);

                if (this.maxMobility_Chromatogram - this.minMobility_Chromatogram < 10)
                {
                    this.maxMobility_Chromatogram = this.minMobility_Chromatogram + 10;
                    this.num_maxBin.Value = this.maxMobility_Chromatogram;
                }
                if (this.maxMobility_Chromatogram > this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1)
                {
                    this.maxMobility_Chromatogram = this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1;
                    this.minMobility_Chromatogram = this.maxMobility_Chromatogram - 10;

                    this.num_minBin.Value = this.minMobility_Chromatogram;
                    this.num_maxBin.Value = this.maxMobility_Chromatogram;
                }

                this.flag_update2DGraph = true;
                this.flag_enterBinRange = false;
                return;
            }

            if (this.num_minBin.Value >= this.num_maxBin.Value)
                this.num_maxBin.Value = Convert.ToDecimal(Convert.ToDouble(this.num_minBin.Value) + 1.0);

            try
            {
                if (this.flag_display_as_TOF)
                {
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin;
                    max = this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = ((max - min + 1.0) / this.pnl_2DMap.Height);
                new_minBin = (int)min + 1;
                if (bin_diff > 0.0)
                    this.new_maxBin = this.new_minBin + (int)(bin_diff * this.pnl_2DMap.Height);
                else
                    this.new_maxBin = (int)max;

                _zoomX.Add(new Point(new_minMobility, new_maxMobility));
                _zoomBin.Add(new Point(new_minBin, new_maxBin));

                // this.lbl_ExperimentDate.Text = (new_maxBin * (TenthsOfNanoSecondsPerBin * 1e-4)).ToString() + " < " + new_maxBin.ToString();
                this.flag_update2DGraph = true;

                this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxBin.Value) - Convert.ToDouble(this.num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TRAPPED:  " + ex.ToString());
            }

            this.flag_enterBinRange = false;
        }

        protected virtual void num_maxBin_ValueChanged(object sender, System.EventArgs e)
        {
            double min, max;
            int bin_diff;

            if (this.flag_enterBinRange)
                return;
            this.flag_enterBinRange = true;

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.maxMobility_Chromatogram = Convert.ToInt32(this.num_maxBin.Value);
                if (this.maxMobility_Chromatogram > this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1)
                    this.maxMobility_Chromatogram = this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1;

                if (this.maxMobility_Chromatogram - this.minMobility_Chromatogram < 10)
                {
                    this.minMobility_Chromatogram = this.maxMobility_Chromatogram - 10;
                    this.num_minBin.Value = this.minMobility_Chromatogram;
                }
                if (this.minMobility_Chromatogram < 0)
                {
                    this.minMobility_Chromatogram = 0;
                    this.maxMobility_Chromatogram = 10;

                    this.num_minBin.Value = this.minMobility_Chromatogram;
                    this.num_maxBin.Value = this.maxMobility_Chromatogram;
                }

                this.flag_update2DGraph = true;
                this.flag_enterBinRange = false;
                return;
            }

            if (this.num_minBin.Value >= this.num_maxBin.Value)
                this.num_minBin.Value = Convert.ToDecimal(Convert.ToDouble(this.num_maxBin.Value) - 1.0);

            try
            {
                if (this.flag_display_as_TOF)
                {
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin;
                    max = this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = (int)((max - min + 1) / this.pnl_2DMap.Height);
                new_maxBin = (int)max + 1;
                if (bin_diff > 0)
                    this.new_minBin = new_maxBin - (bin_diff * this.pnl_2DMap.Height);
                else
                    this.new_minBin = (int)min;

                _zoomX.Add(new Point(new_minMobility, new_maxMobility));
                _zoomBin.Add(new Point(new_minBin, new_maxBin));

                // this.lbl_ExperimentDate.Text = (new_maxBin * (TenthsOfNanoSecondsPerBin * 1e-4)).ToString() + " < " + new_maxBin.ToString();
                this.flag_update2DGraph = true;

                this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(this.num_maxBin.Value) - Convert.ToDouble(this.num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TRAPPED:  " + ex.ToString());
            }

            this.flag_enterBinRange = false;
        }

        // //////////////////////////////////////////////////////////////////////////////
        // Frame Selection
        //
        private void slide_FrameSelect_MouseDown(object obj, MouseEventArgs e)
        {
            this.StopCinema();
        }

        // ////////////////////////////////////////////////////////////////////
        // Select Frame Range
        //
        private void num_FrameRange_ValueChanged(object sender, EventArgs e)
        {
            if ((double)this.num_FrameRange.Value > this.slide_FrameSelect.Maximum+1)
            {
                this.num_FrameRange.Value = Convert.ToDecimal(this.slide_FrameSelect.Maximum+1);
                return;
            }
            this.ptr_UIMFDatabase.FrameWidth = Convert.ToInt32(this.num_FrameRange.Value);

            if (this.slide_FrameSelect.Value < Convert.ToDouble(this.num_FrameRange.Value))
            {
                this.slide_FrameSelect.Value = (int)(Convert.ToDouble(this.num_FrameRange.Value) - 1);
            }

            if (this.num_FrameRange.Value > 1)
            {
                this.lbl_FramesShown.Show();

                if (this.Cinemaframe_DataChange > 0)
                    this.Cinemaframe_DataChange = Convert.ToInt32(this.num_FrameRange.Value / 3) + 1;
                else
                    this.Cinemaframe_DataChange = -(Convert.ToInt32(this.num_FrameRange.Value / 3) + 1);
            }
            else
                this.lbl_FramesShown.Hide();

            this.flag_update2DGraph = true;
        }

        private void num_FrameIndex_ValueChanged(object sender, EventArgs e)
        {
            this.slide_FrameSelect.Value = Convert.ToDouble(this.num_FrameIndex.Value);
        }

        private void slide_FrameSelect_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.slide_FrameSelect.Value - Convert.ToDouble(this.num_FrameRange.Value) < 0)
                this.slide_FrameSelect.Value = Convert.ToDouble(this.num_FrameRange.Value) - 1.0;

            if ((double) this.slide_FrameSelect.Value != (double) ((int) this.slide_FrameSelect.Value))
            {
                this.slide_FrameSelect.Value = (int) ((double) this.slide_FrameSelect.Value+.5);
            }

            this.flag_update2DGraph = true;
        }

        // //////////////////////////////////////////////////////////////////////////
        // Display Settings
        //
        private void slide_Threshold_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            this.flag_update2DGraph = true;
        }

        private void ColorSelector_Change(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.flag_update2DGraph = true;
        }

        protected virtual void show_MaxIntensity(object sender, System.EventArgs e)
        {
            int topX;
            int topY;
            int widthX;
            int widthY;

            if (this.current_valuesPerPixelX < 0)
            {
                topX = (this.posX_MaxIntensity * (-this.current_valuesPerPixelX)) - 15;
                widthX = (-this.current_valuesPerPixelX) + 30;
            }
            else
            {
                topX = this.posX_MaxIntensity - 15;
                widthX = 30;
            }

            if (this.current_valuesPerPixelY < 0)
            {
                topY = this.pnl_2DMap.Height - 15 - ((this.posY_MaxIntensity + 1) * (-this.current_valuesPerPixelY));
                widthY = (-this.current_valuesPerPixelY) + 30;
            }
            else
            {
                topY = this.pnl_2DMap.Height - 15 - this.posY_MaxIntensity;
                widthY = 30;
            }

            Graphics g = this.pnl_2DMap.CreateGraphics();
            Pen p1 = new Pen(new SolidBrush(Color.Black), 3);
            g.DrawEllipse(p1, topX, topY, widthX, widthY);
            Pen p2 = new Pen(new SolidBrush(Color.White), 1);
            g.DrawEllipse(p2, topX, topY, widthX, widthY);
        }

        private void btn_Reset_Clicked(object sender, System.EventArgs e)
        {
            this.slide_Threshold.Value = 1;
            this.slider_PlotBackground.set_Value(30);
            this.slider_ColorMap.reset_Settings();

            // redraw everything.
            this.flag_update2DGraph = true;
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
                            this.Filter_FrameType(this.ptr_UIMFDatabase.get_FrameType());
                            this.ptr_UIMFDatabase.CurrentFrameIndex = 0;
                        }

                        if (this.ptr_UIMFDatabase.get_NumFrames(this.ptr_UIMFDatabase.get_FrameType()) <= 0)
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

                        if (this.ptr_UIMFDatabase.CurrentFrameIndex < this.ptr_UIMFDatabase.get_NumFrames(this.ptr_UIMFDatabase.get_FrameType()))
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
            if (frame_index >= this.ptr_UIMFDatabase.get_NumFrames(this.ptr_UIMFDatabase.get_FrameType()))
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

        // /////////////////////////////////////////////////////////////////////
        // UpdateCursorReading()
        //
        protected virtual void UpdateCursorReading(System.Windows.Forms.MouseEventArgs e)
        {
            if ((this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked) ||
                (this.tabpages_FrameInfo.SelectedTab != this.tabPage_Cursor))
                return;

            double mobility = (current_valuesPerPixelX == 1 ? e.X : this.current_minMobility + (e.X / -this.current_valuesPerPixelX));

            this.lbl_CursorMobility.Text = mobility.ToString();
            if (this.mean_TOFScanTime != -1.0)
                this.lbl_CursorScanTime.Text = (mobility * this.mean_TOFScanTime).ToString("0.0000");
            else
                this.lbl_CursorScanTime.Text = "Not Available";

            if (this.data_2D == null)
                return;
            // time_offset = this.imfReader.Experiment_Properties.TimeOffset;

            try
            {
                if (this.flag_display_as_TOF)
                {
                    // TOF is quite easy.  Using the current_valuesPerPixelY which is TOF related.
                    int tof_bin = ((current_valuesPerPixelY > 0) ? this.current_minBin + ((this.pnl_2DMap.Height - e.Y - 1) * current_valuesPerPixelY) : this.current_minBin + ((this.pnl_2DMap.Height - e.Y - 1) / -current_valuesPerPixelY));

                    // this is required to match with the MZ values
                    tof_bin--;   // wfd:  This is a Cheat!!! not sure what side of this belongs MZ or TOF

                    this.lbl_CursorTOF.Text = (tof_bin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin * 1e-4).ToString();
                    this.lbl_CursorMZ.Text = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ((float)(tof_bin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin)).ToString();
                }
                else
                {
                    // Much more difficult to find where the mz <-> TOF index correlation
                    //
                    // linearize the mz and find the cursor.
                    // calculate the mz, then convert to TOF for all the values.
                    double mzMax = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_maxBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                    double mzMin = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ(this.current_minBin * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);

                    double diffMZ = mzMax - mzMin;
                    double rangeTOF = this.current_maxBin - this.current_minBin;
                    double indexY = (current_valuesPerPixelY > 0) ? (this.pnl_2DMap.Height - e.Y - 1) * current_valuesPerPixelY : (this.pnl_2DMap.Height - e.Y - 1) / (-current_valuesPerPixelY);
                    double mz = (indexY / rangeTOF) * diffMZ + mzMin;
                    double tof_value = this.ptr_UIMFDatabase.MzCalibration.MZtoTOF(mz);

                    this.lbl_CursorMZ.Text = mz.ToString();
                    this.lbl_CursorTOF.Text = (tof_value * 1e-4).ToString(); // convert to usec
                }

                this.lbl_TimeOffset.Text = "Time Offset = " + this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0).ToString() + " nsec";

                if (current_valuesPerPixelY < 0)
                {
                    this.plot_TOF.Refresh();

                    Graphics g = this.plot_TOF.CreateGraphics();
                    int y_step = ((e.Y / current_valuesPerPixelY) * current_valuesPerPixelY) + (int)this.plot_TOF.GraphPane.Chart.Rect.Top;
                    Pen dp = new Pen(new SolidBrush(Color.Red), 1);
                    dp.DashStyle = DashStyle.Dot;
                    g.DrawLine(dp, this.plot_TOF.GraphPane.Chart.Rect.Left, y_step, this.plot_TOF.GraphPane.Chart.Rect.Left + this.plot_TOF.GraphPane.Chart.Rect.Width, y_step);
                    int amp_index = (this.pnl_2DMap.Height - e.Y - 1) / (-current_valuesPerPixelY);
                    string amplitude = this.data_tofTIC[amp_index].ToString();
                    Font amp_font = new Font("lucida", 8, FontStyle.Regular);
                    int left_str = (int)this.plot_TOF.GraphPane.Chart.Rect.Left - (int)g.MeasureString(amplitude, amp_font).Width - 10;

                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), left_str, y_step - 7, this.plot_TOF.GraphPane.Chart.Rect.Left - 1, y_step - 7);
                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), left_str, y_step - 7, left_str, y_step + 6);
                    g.FillRectangle(new SolidBrush(Color.GhostWhite), left_str + 1, y_step - 6, this.plot_TOF.GraphPane.Chart.Rect.Left - left_str - 1, 13);
                    g.DrawLine(new Pen(new SolidBrush(Color.White), 1), left_str + 1, y_step + 7, this.plot_TOF.GraphPane.Chart.Rect.Left - 1, y_step + 7);

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

        // //////////////////////////////////////////////////////////////////
        // play
        //
        private bool flag_CinemaPlot = false;
        private int Cinemaframe_DataChange = 0;
        private void pb_PlayLeftIn_Click(object sender, EventArgs e)
        {
            this.StopCinema();
        }

        private void pb_PlayRightIn_Click(object sender, EventArgs e)
        {
            this.StopCinema();
        }

        private void pb_PlayLeftOut_Click(object sender, EventArgs e)
        {
            if (this.slide_FrameSelect.Value <= this.slide_FrameSelect.Minimum) // frame index starts at 0
                return;

            this.pb_PlayLeftOut.Hide();
            this.pb_PlayRightOut.Show();

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = -(Convert.ToInt32(this.num_FrameRange.Value) / 3) - 1;
            this.slide_FrameSelect.Value += this.Cinemaframe_DataChange;
        }

        private void pb_PlayRightOut_Click(object sender, EventArgs e)
        {
            if (this.slide_FrameSelect.Value >= this.slide_FrameSelect.Maximum)
                return;

            this.pb_PlayRightOut.Hide();
            this.pb_PlayLeftOut.Show();

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = (Convert.ToInt32(this.num_FrameRange.Value) / 3) + 1;
            if (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange > Convert.ToInt32(this.slide_FrameSelect.Maximum))
                this.slide_FrameSelect.Value = this.slide_FrameSelect.Maximum - Convert.ToInt32(this.num_FrameRange.Value);
            else
            {
                if (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange > this.slide_FrameSelect.Maximum)
                    this.slide_FrameSelect.Value = this.slide_FrameSelect.Maximum - this.Cinemaframe_DataChange;
                else
                    this.slide_FrameSelect.Value += this.Cinemaframe_DataChange;

            }
        }

        private void StopCinema()
        {
            this.pb_PlayRightOut.Show();
            this.pb_PlayLeftOut.Show();

            this.flag_CinemaPlot = false;
            this.Cinemaframe_DataChange = 0;

            this.flag_update2DGraph = true;
        }

        // /////////////////////////////////////////////////////
        // Map scrollbar
        //
        protected virtual void hsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int old_min = this.current_minMobility;
            int old_max = this.current_maxMobility;

            this.current_minMobility = this.new_minMobility = this.hsb_2DMap.Value;
            this.current_maxMobility = this.new_maxMobility = (old_max + (this.new_minMobility - old_min));

            this.flag_update2DGraph = true;
        }

        protected virtual void vsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int old_min = this.current_minBin;
            int old_max = this.current_maxBin;

            this.new_minBin = this.vsb_2DMap.Maximum - this.vsb_2DMap.Value;
            this.new_maxBin = (old_max + (this.new_minBin - old_min));

            this.flag_update2DGraph = true;
        }

        // ////////////////////////////////////////////////////////////////
        //
        //
        private void menuItem_SelectionCorners_Click(object sender, EventArgs e)
        {
            this.menuItem_SelectionCorners.Checked = !this.menuItem_SelectionCorners.Checked;

            if (this.menuItem_SelectionCorners.Checked)
                this.reset_Corners();

            this.flag_update2DGraph = true;
        }

        // ////////////////////////////////////////////////////////////////
        // Show only the maximum values - do not sum bins.
        //
        private void menuItem_TOFMaximum_Click(object sender, EventArgs e)
        {
            this.menuItem_TOFMaximum.Checked = !this.menuItem_TOFMaximum.Checked;
            this.menuItem_MaxIntensities.Checked = this.menuItem_TOFMaximum.Checked;

            if (this.menuItem_TOFMaximum.Checked)
                this.plot_TOF.BackColor = Color.AntiqueWhite;
            else
                this.plot_TOF.BackColor = Color.White;

            this.flag_update2DGraph = true;
        }

        // ---------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------
        // ---------------                                            ----------------------
        // ---------------               DRAG DROP FILES              ----------------------
        // ---------------                                            ----------------------
        // ---------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------

        // ////////////////////////////////////////////////
        //
        private void pb_PlayDownOut_MOUSEDOWN(object obj, MouseEventArgs e)
        {
            int selected_row = (int)this.lb_DragDropFiles.SelectedIndices[0];
            this.pb_PlayDownOut.Hide();
        }

        // ////////////////////////////////////////////////
        //
        private void pb_PlayDownOut_MOUSEUP(object obj, MouseEventArgs e)
        {
            this.pb_PlayDownOut.Show();
        }

        // ////////////////////////////////////////////////
        //
        private void pb_PlayUpOut_MOUSEDOWN(object obj, MouseEventArgs e)
        {
            int selected_row = (int)this.lb_DragDropFiles.SelectedIndices[0];
            this.pb_PlayUpOut.Hide();

            //MessageBox.Show("Selected Row: "+this.dg_ExperimentList.SelectedRows[0].ToString());
            if (selected_row - 1 < 0)
                return;
        }

        // ////////////////////////////////////////////////
        //
        private void pb_PlayUpOut_MOUSEUP(object obj, MouseEventArgs e)
        {
            this.pb_PlayUpOut.Show();
        }

        // /////////////////////////////////////////////////////////////
        // Drag-Drop IMF file onto the graph
        //
        private void cb_ExperimentControlled_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.flag_CinemaPlot)
            {
                this.flag_Closing = true; // halt cinema frame processing asap.
                this.StopCinema();
                Thread.Sleep(100);
                this.flag_Closing = false; // we are not closing
            }

            this.index_CurrentExperiment = this.cb_ExperimentControlled.SelectedIndex;
            this.lb_DragDropFiles.ClearSelected();
            this.lb_DragDropFiles.SetSelected(this.index_CurrentExperiment, true);

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Width = this.pnl_2DMap.Left + this.ptr_UIMFDatabase.UimfFrameParams.Scans + 170;

                this.rb_PartialChromatogram.Checked = false;
                this.rb_CompleteChromatogram.Checked = false;

                this.plot_Mobility.StopAnnotating(false);

                this.Chromatogram_CheckedChanged();

                this.ptr_UIMFDatabase.CurrentFrameIndex = (int)this.slide_FrameSelect.Value;
                this.plot_Mobility.ClearRange();
                this.num_FrameRange.Value = 1;

                this.vsb_2DMap.Show();  // gets hidden with Chromatogram
                this.hsb_2DMap.Show();
            }

            this.ptr_UIMFDatabase = (UIMFDataWrapper)this.array_Experiments[this.index_CurrentExperiment];

            this.vsb_2DMap.Value = 0;

            if (this.ptr_UIMFDatabase.CurrentFrameIndex < this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames - 1)
                this.num_FrameIndex.Value = 0;
            this.num_FrameIndex.Maximum = this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames - 1;
            this.num_FrameIndex.Value = this.ptr_UIMFDatabase.CurrentFrameIndex;

            if (this.num_FrameIndex.Maximum > 0)
            {
                this.slide_FrameSelect.Minimum = 0;
                this.slide_FrameSelect.Maximum = this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames - 1;
            }
            else
                this.elementHost_FrameSelect.Hide();  // hidden elsewhere; but if there is only one frame this needs to disappear.

            this.cb_FrameType.SelectedIndex = (int) this.ptr_UIMFDatabase.get_FrameType();

            this.flag_update2DGraph = true;
        }

        private void lb_DragDropFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.StopCinema();

            this.flag_update2DGraph = true;
        }

        private void DataViewer_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int i;
            int j;
            string temp;

            try
            {
                for (i = (files.Length - 1); i >= 0; i--)
                {
                    for (j = 1; j <= i; j++)
                    {
                        if (string.Compare(files[j - 1], files[j], true) > 0)
                        {
                            temp = files[j - 1];
                            files[j - 1] = files[j];
                            files[j] = temp;
                        }
                    }
                }

                for (i = 0; i < files.Length; i++)
                {
                    if (File.Exists(files[i]) && (Path.GetExtension(files[i]).ToLower() == ".uimf"))
                    {
                        this.ptr_UIMFDatabase = new UIMFDataWrapper(files[i]);
                        this.array_Experiments.Add(this.ptr_UIMFDatabase);

                        this.lb_DragDropFiles.Items.Add(files[i]);
                        this.lb_DragDropFiles.ClearSelected();

                        this.cb_ExperimentControlled.Items.Add(Path.GetFileNameWithoutExtension(files[i]));
                        this.cb_ExperimentControlled.SelectedIndex = this.cb_ExperimentControlled.Items.Count - 1;

                        this.Filter_FrameType(this.ptr_UIMFDatabase.get_FrameType());
                        this.ptr_UIMFDatabase.CurrentFrameIndex = 0;
                        this.ptr_UIMFDatabase.set_FrameType(current_frame_type, true);

                        Generate2DIntensityArray();
                        this.GraphFrame(this.data_2D, true);

                        this.cb_FrameType.SelectedIndex = (int) this.ptr_UIMFDatabase.get_FrameType();
                    }
                    else
                        MessageBox.Show(this, "'" + Path.GetFileName(files[i]) + "' is not in correct format.\n\nOnly UIMF files can be added.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            this.lb_DragDropFiles.Show();
            this.pb_PlayDownOut.Show();
            this.pb_PlayDownIn.Show();
            this.pb_PlayUpOut.Show();
            this.pb_PlayUpIn.Show();
            this.cb_Exclusive.Show();
        }

        private void DataViewer_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
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

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //
        private void btn_Refresh_Click(object sender, EventArgs e)
        {
            this.tab_DataViewer.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));

            this.plot_Mobility.Dispose();
            this.plot_TOF.Dispose();

            SetupPlots();

            this.plot_axisMobility(this.data_driftTIC);
            this.plot_axisTOF(this.data_tofTIC);

            // MessageBox.Show("refresh");
            //this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
            this.flag_ResizeThis = true;
            this.btn_Refresh.Enabled = true;

        }

        private void menuitem_WriteUIMF_Click(object sender, EventArgs e)
        {
            UIMFLibrary.GlobalParams Global_Params;
            UIMFLibrary.DataWriter UIMF_Writer;
            DateTime dt_StartExperiment;
            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            save_dialog.CheckFileExists = false;
            save_dialog.Title = "Save merged frame to UIMF file...";
            save_dialog.Filter = "Comma-separated variables (*.uimf)|*.uimf";
            save_dialog.FilterIndex = 1;

            if (save_dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (File.Exists(save_dialog.FileName))
                {
                    UIMF_Writer = new UIMFLibrary.DataWriter(save_dialog.FileName);
                    Global_Params = UIMF_Writer.GetGlobalParams().Clone();

                    Global_Params.AddUpdateValue(GlobalParamKeyType.NumFrames, Global_Params.NumFrames + 1);

                    UIMF_Writer.InsertGlobal(Global_Params);
                }
                else
                {
                    UIMF_Writer = new UIMFLibrary.DataWriter(save_dialog.FileName);
                    UIMF_Writer.CreateTables(null);

                    Global_Params = this.ptr_UIMFDatabase.GetGlobalParams().Clone();

                    dt_StartExperiment = new DateTime(1970, 1, 1);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.DateStarted, dt_StartExperiment.ToLocalTime().ToShortDateString() + " " + dt_StartExperiment.ToLocalTime().ToLongTimeString());
                    Global_Params.AddUpdateValue(GlobalParamKeyType.NumFrames, 1);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.TimeOffset, 0);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.InstrumentName, "MergeFrames");

                    UIMF_Writer.InsertGlobal(Global_Params);
                }

                AppendUIMFFrame(UIMF_Writer, Global_Params.NumFrames-1);

                UIMF_Writer.Dispose();
            }
        }

        public void AppendUIMFFrame(UIMFLibrary.DataWriter UIMF_Writer, int frame_number)
        {
            int nonzero_bins;
            int i;
            int time_offset = 0;
            int b;
            int[] mapped_bins;
            int total_bins;
            int scan;
            FrameParams fp;
            int[] scan_data;
            int exp_index;
            int start_index;
            int end_index;
            int frames;
            double mapped_intercept;
            double mapped_slope;
            double mz;
            int new_bin;
            double new_mz;

            fp = this.ptr_UIMFDatabase.UimfFrameParams.Clone();
            total_bins = this.ptr_UIMFDatabase.UimfGlobalParams.Bins;

            fp.Values.Remove(FrameParamKeyType.Accumulations);
            fp.Values.Remove(FrameParamKeyType.DurationSeconds);
            fp.Values.Remove(FrameParamKeyType.FragmentationProfile);

            // entrance voltages
            fp.Values.Remove(FrameParamKeyType.VoltEntranceHPFIn);
            fp.Values.Remove(FrameParamKeyType.VoltTrapIn);
            fp.Values.Remove(FrameParamKeyType.VoltTrapOut);
            fp.Values.Remove(FrameParamKeyType.VoltEntranceHPFOut);
            fp.Values.Remove(FrameParamKeyType.VoltEntranceCondLmt);
            fp.Values.Remove(FrameParamKeyType.VoltJetDist);
            fp.Values.Remove(FrameParamKeyType.VoltCapInlet);

            // exit voltages
            fp.Values.Remove(FrameParamKeyType.VoltIMSOut);
            fp.Values.Remove(FrameParamKeyType.VoltExitHPFIn);
            fp.Values.Remove(FrameParamKeyType.VoltExitHPFOut);
            fp.Values.Remove(FrameParamKeyType.VoltExitCondLmt);

            fp.Values.Remove(FrameParamKeyType.VoltQuad1);
            fp.Values.Remove(FrameParamKeyType.VoltCond1);
            fp.Values.Remove(FrameParamKeyType.VoltQuad2);
            fp.Values.Remove(FrameParamKeyType.VoltCond2);

            // pressure monitors
            fp.Values.Remove(FrameParamKeyType.HighPressureFunnelPressure);
            fp.Values.Remove(FrameParamKeyType.IonFunnelTrapPressure);
            fp.Values.Remove(FrameParamKeyType.RearIonFunnelPressure);
            fp.Values.Remove(FrameParamKeyType.QuadrupolePressure);

            UIMF_Writer.InsertFrame(frame_number, fp);

            //MessageBox.Show(this.current_valuesPerPixelX.ToString() + ", " + this.current_valuesPerPixelY.ToString());

            mapped_bins = new int[total_bins];
            mapped_intercept = this.ptr_UIMFDatabase.UimfFrameParams.CalibrationIntercept;
            mapped_slope = this.ptr_UIMFDatabase.UimfFrameParams.CalibrationSlope;
            for (scan = 0; scan < fp.Scans; scan++)
            {
                // zero out the mapped bins
                for (i = 0; i < total_bins; i++)
                    mapped_bins[i] = 0;

                // we need to do a scan at a time, map and sum bins.
                for (exp_index = 0; exp_index < this.lb_DragDropFiles.Items.Count; exp_index++)
                {
                    if (this.lb_DragDropFiles.GetSelected(exp_index))
                    {
                        this.ptr_UIMFDatabase = (UIMFDataWrapper)this.array_Experiments[exp_index];

                        start_index = this.ptr_UIMFDatabase.CurrentFrameIndex - (this.ptr_UIMFDatabase.FrameWidth - 1);
                        end_index = this.ptr_UIMFDatabase.CurrentFrameIndex;

                        // collect the data
                        for (frames = start_index; (frames <= end_index) && !this.flag_Closing; frames++)
                        {
                            // this is in bin resolution.
                            scan_data = this.ptr_UIMFDatabase.GetSumScans(frames, scan, scan);

                            // convert to mz resolution then map into bin resolution - sum into mapped_bins[]
                            for (i = 0; i < scan_data.Length; i++)
                            {
                                new_bin = this.ptr_UIMFDatabase.MapBinCalibration(i, mapped_slope, mapped_intercept);

                                if (new_bin < mapped_bins.Length)
                                {
                                    if (flag_display_as_TOF)
                                    {
                                        if (this.inside_Polygon(scan, new_bin))
                                            mapped_bins[new_bin] += scan_data[i];
                                    }
                                    else
                                    {
                                        new_mz = this.ptr_UIMFDatabase.MzCalibration.TOFtoMZ((double)i * this.ptr_UIMFDatabase.TenthsOfNanoSecondsPerBin);
                                        if (this.inside_Polygon(scan, new_mz))
                                            mapped_bins[new_bin] += scan_data[i];
                                    }
                                }
                            }
                        }
                    }
                }

                nonzero_bins = 0;
                for (i = 0; i < mapped_bins.Length; i++)
                {
                    if (mapped_bins[i] != 0)
                        nonzero_bins++;
                }

                var nzVals = new Tuple<int, int>[nonzero_bins];

                // collect the data
                b = 0;
                for (i = time_offset; (i < total_bins) && (b < nonzero_bins); i++)
                    if (mapped_bins[i] != 0)
                    {
                        nzVals[b] = new Tuple<int, int>(i - time_offset, mapped_bins[i]);

                        b++;
                    }

                UIMF_Writer.InsertScan(frame_number, fp, scan, nzVals, this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth, 0);
            }
        }

        // ///////////////////////////////////////////////////////////////////////////////////////
        // Super Frame - Merge files
        //
        private void menuItem_SuperExperiment_Click(object sender, EventArgs e)
        {
            this.form_ExportExperiment = new UIMF_File.Utilities.ExportExperiment();
            this.form_ExportExperiment.btn_ExportExperimentOK.Click += new EventHandler(this.form_ExportExperiment_OK_Click);
            this.form_ExportExperiment.Show();
        }

        private void form_ExportExperiment_OK_Click(object sender, EventArgs e)
        {
            int step = Convert.ToInt32(this.form_ExportExperiment.num_Step.Value);
            int merge = Convert.ToInt32(this.form_ExportExperiment.num_Merge.Value);
            string name = this.form_ExportExperiment.tb_Name.Text;
            string directory = Path.Combine(this.form_ExportExperiment.tb_Directory.Text, name);
            string file_merge_IMF;

            this.form_ExportExperiment.Hide();

            if ((step <= 0) || (merge <= 0))
            {
                MessageBox.Show(this, "Either the step or merge value is not correct, please correct the problem.");
                this.form_ExportExperiment.Show();
                return;
            }

            if (Directory.Exists(directory))
            {
                MessageBox.Show(this, "The experiment already exists in the location you have specified, please change the name");
                this.form_ExportExperiment.Show();
                return;
            }

            Directory.CreateDirectory(directory);
            this.slide_FrameSelect.Value = merge;
            this.num_FrameRange.Value = merge;

            this.Enabled = false;
            //this.flag_Halt = true;
            try
            {
                for (int i = 1; i <= ((this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames - merge) / step) + 1; i++)
                {
                    //  MessageBox.Show((i * step).ToString());
                    //  continue;
                    this.slide_FrameSelect.Value = ((i - 1) * step) + merge;

                    this.Graph_2DPlot();
                    this.Update();

                    file_merge_IMF = Path.Combine(directory, name + ".Accum_" + i.ToString() + ".IMF");
                    this.save_MergedFrame(file_merge_IMF, true, i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //this.ptr_UIMFDatabase.UIMF_GlobalParams.NumFrames = ((this.ptr_UIMFDatabase.UIMF_GlobalParams.NumFrames - merge) / step) + 1;
            this.ptr_UIMFDatabase.UimfGlobalParams.AddUpdateValue(GlobalParamKeyType.NumFrames, ((this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames - merge) / step) + 1);
            this.Enabled = true;

            this.form_ExportExperiment.Dispose();
        }

        private void menuItem_SuperFrame_Click(object obj, System.EventArgs e)
        {
            MessageBox.Show("menuItem_SuperFrame_Click needs work");
        }

        private bool save_MergedFrame(string file_merge_IMF, bool flag_calc_accumulations, int frame_number)
        {
            MessageBox.Show("save_MergedFrame needs work");
            return false;
        }

        private void lbl_FramesShown_Click(object sender, EventArgs e)
        {
            if (this.num_FrameRange.Value > 1)
                return;

            this.lbl_FramesShown.Text = "Enter TIC Threshold: ";

            this.num_TICThreshold.Visible = true;
            this.num_TICThreshold.Left = this.lbl_FramesShown.Left + this.lbl_FramesShown.Width;
            this.num_TICThreshold.Top = this.lbl_FramesShown.Top - 4;

            this.btn_TIC.Visible = true;
        }

        private void btn_TIC_Click(object sender, EventArgs e)
        {
            this.btn_TIC.Hide();
            this.num_TICThreshold.Hide();

            this.calc_TIC();
        }

        private void btn_ShowChromatogram_Click(object sender, EventArgs e)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Chromatogram_GUI_Settings();
            }

            this.btn_Reset.PerformClick();

            GC.Collect();
            if (this.flag_chromatograph_collected_COMPLETE)
            {
                this.rb_CompleteChromatogram.BackColor = Color.LawnGreen;
                this.rb_PartialChromatogram.BackColor = Color.Transparent;
            }
            else
            {
                this.rb_CompleteChromatogram.BackColor = Color.Transparent;
                this.rb_PartialChromatogram.BackColor = Color.LawnGreen;
            }
            //  this.flag_update2DGraph = true;
        }

        public void Chromatogram_GUI_Settings()
        {
            if (this.rb_CompleteChromatogram.Checked)
                this.flag_chromatograph_collected_PARTIAL = false;
            else
                this.flag_chromatograph_collected_COMPLETE = false;

           // this.flag_update2DGraph = true;

            this.ptr_UIMFDatabase.CurrentFrameIndex = (int)this.slide_FrameSelect.Value;
            this.plot_Mobility.StopAnnotating(true);

            this.flag_selection_drift = false;
            this.plot_Mobility.ClearRange();

            this.pb_PlayLeftIn.Hide();
            this.pb_PlayLeftOut.Hide();
            this.pb_PlayRightIn.Hide();
            this.pb_PlayRightOut.Hide();

            this.vsb_2DMap.Hide();
            this.hsb_2DMap.Hide();

            //this.cb_Chromatogram.Enabled = false;
            this.flag_display_as_TOF = false;

            this.num_minBin.DecimalPlaces = 0;
            this.num_minBin.Increment = 1;
            this.num_maxBin.DecimalPlaces = 0;
            this.num_maxBin.Increment = 1;

            this.num_minMobility.DecimalPlaces = 0;
            this.num_minMobility.Increment = 1;
            this.num_maxMobility.DecimalPlaces = 0;
            this.num_maxMobility.Increment = 1;
        }

        private void rb_PartialChromatogram_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rb_PartialChromatogram.Checked)
            {
                if (this.num_FrameCompression.Value == 1)
                {
                    this.rb_CompleteChromatogram.Checked = true;
                    return;
                }

                this.rb_PartialChromatogram.ForeColor = Color.LawnGreen;
                this.rb_CompleteChromatogram.Enabled = false;
                this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
                this.Chromatogram_CheckedChanged();

                this.pnl_2DMap.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                this.rb_CompleteChromatogram.Enabled = true;

                this.pnl_2DMap.BackgroundImageLayout = ImageLayout.None;
            }
        }

        private void rb_CompleteChromatogram_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rb_CompleteChromatogram.Checked)
            {
                this.rb_PartialChromatogram.Enabled = false;
                this.rb_CompleteChromatogram.ForeColor = Color.LawnGreen;
                this.rb_PartialChromatogram.ForeColor = Color.Yellow;
                this.Chromatogram_CheckedChanged();

                this.pnl_2DMap.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                this.rb_PartialChromatogram.Enabled = true;

                this.pnl_2DMap.BackgroundImageLayout = ImageLayout.None;
            }
        }

        private void num_FrameCompression_ValueChanged(object sender, EventArgs e)
        {
            if (this.num_FrameCompression.Value != this.current_frame_compression)
            {
                this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
                this.rb_PartialChromatogram.ForeColor = Color.Yellow;

                this.flag_chromatograph_collected_COMPLETE = false;
                this.flag_chromatograph_collected_PARTIAL = false;
            }
            else
            {
                if (this.flag_chromatograph_collected_COMPLETE)
                {
                    this.rb_CompleteChromatogram.ForeColor = Color.LawnGreen;
                    this.rb_PartialChromatogram.ForeColor = Color.Yellow;

                    this.flag_chromatograph_collected_COMPLETE = true;
                    this.flag_chromatograph_collected_PARTIAL = false;
                }
                else if (this.flag_chromatograph_collected_PARTIAL)
                {
                    this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
                    this.rb_PartialChromatogram.ForeColor = Color.LawnGreen;

                    this.flag_chromatograph_collected_PARTIAL = true;
                    this.flag_chromatograph_collected_COMPLETE = false;
                }
                else
                {
                    this.flag_chromatograph_collected_COMPLETE = false;
                    this.flag_chromatograph_collected_PARTIAL = false;
                }
            }
        }

        public void Chromatogram_CheckedChanged()
        {
            if (this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames < 2)
            {
                if (!this.rb_CompleteChromatogram.Checked && !this.rb_PartialChromatogram.Checked)
                    return;

                MessageBox.Show("Chromatogram's are not available with less than 2 frames");

                this.rb_CompleteChromatogram.Checked = false;
                this.rb_PartialChromatogram.Checked = false;

                this.rb_CompleteChromatogram.Enabled = false;
                this.rb_PartialChromatogram.Enabled = false;

                return;
            }

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                if (this.rb_CompleteChromatogram.Checked)
                    this.flag_chromatograph_collected_PARTIAL = false;
                else
                    this.flag_chromatograph_collected_COMPLETE = false;

                this.hsb_2DMap.Value = 0;

                this.ptr_UIMFDatabase.CurrentFrameIndex = (int)this.slide_FrameSelect.Value;
                this.plot_Mobility.StopAnnotating(true);

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                this.pb_PlayLeftIn.Hide();
                this.pb_PlayLeftOut.Hide();
                this.pb_PlayRightIn.Hide();
                this.pb_PlayRightOut.Hide();

                this.elementHost_FrameSelect.Hide();
                this.lbl_FrameRange.Hide();
                this.num_FrameRange.Hide();
                this.lbl_Chromatogram.Text = "Peak Chromatogram";
                this.num_FrameIndex.Hide();

                this.lbl_FramesShown.Hide();

                this.cb_FrameType.Hide();
                this.lbl_Chromatogram.Show();

                this.vsb_2DMap.Hide();
                // this.hsb_2DMap.Hide();

                this.flag_display_as_TOF = false;

                this.num_minBin.DecimalPlaces = 0;
                this.num_maxBin.DecimalPlaces = 0;
                this.num_minBin.Increment = 1;
                this.num_maxBin.Increment = 1;

                this.num_minMobility.DecimalPlaces = 0;
                this.num_minMobility.Increment = 1;
                this.num_maxMobility.DecimalPlaces = 0;
                this.num_maxMobility.Increment = 1;
            }
            else
            {
                if (this.menuItemConvertToTOF.Checked)
                    this.flag_display_as_TOF = true;
                else
                    this.flag_display_as_TOF = false;

                this.lbl_Chromatogram.Text = "Frame:  ";
                this.lbl_Chromatogram.ForeColor = Color.Black;
                this.num_FrameIndex.Show();
                if (this.ptr_UIMFDatabase.UimfGlobalParams.NumFrames > 1)
                {
                    this.elementHost_FrameSelect.Show();

                    this.lbl_FrameRange.Show();
                    this.num_FrameRange.Show();

                    if (this.num_FrameRange.Value > 1)
                        this.lbl_FramesShown.Show();

                    this.pb_PlayLeftIn.Show();
                    this.pb_PlayLeftOut.Show();
                    this.pb_PlayRightIn.Show();
                    this.pb_PlayRightOut.Show();
                }

                this.vsb_2DMap.Show();
                this.hsb_2DMap.Show();

                this.cb_FrameType.Show();
                this.lbl_Chromatogram.Hide();

                this.Update();

                this.num_minBin.DecimalPlaces = 4;
                this.num_maxBin.DecimalPlaces = 4;
                this.num_minBin.Increment = 20;
                this.num_maxBin.Increment = 20;

                this.num_minMobility.DecimalPlaces = 2;
                this.num_minMobility.Increment = 20;
                this.num_maxMobility.DecimalPlaces = 2;
                this.num_maxMobility.Increment = 20;
            }

            this.btn_Reset.PerformClick();
            GC.Collect();

            this.flag_update2DGraph = true;
        }

        // //////////////////////////////////////////////////////////////////////////////////
        // create IMF file
        //
        private void menuitem_SaveIMF_Click(object sender, EventArgs e)
        {
            string file_accum_IMF;
            int i, j = 0, k;
            long escape_position;
            int counter_TIC = 0;
            int counter_bin = 0;
            int[] num_BinTICs;
            int[] bytes_Bin;

            double bin_width;

            file_accum_IMF = Path.Combine(Path.GetDirectoryName(this.ptr_UIMFDatabase.UimfDataFile), Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile) + ".Accum_" + this.ptr_UIMFDatabase.CurrentFrameNum.ToString() + ".IMF");

            num_BinTICs = new int[this.ptr_UIMFDatabase.UimfFrameParams.Scans];
            bytes_Bin = new int[this.ptr_UIMFDatabase.UimfFrameParams.Scans];
            bin_width = this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth;
            /////////////////////////////////////////////////////////
            //////                                             //////
            //////                                             //////
            //////              WRITE IMF FILE                 //////
            //////                                             //////
            //////                                             //////
            /////////////////////////////////////////////////////////
            StreamWriter sw_IMF = new StreamWriter(file_accum_IMF, false);
            sw_IMF.WriteLine("DataType: 11");
            sw_IMF.WriteLine("DataSubType: int");
            sw_IMF.WriteLine("TOFSpectra: " + this.ptr_UIMFDatabase.UimfFrameParams.Scans.ToString());
            sw_IMF.WriteLine("NumBins: " + this.ptr_UIMFDatabase.UimfGlobalParams.Bins.ToString());
            sw_IMF.WriteLine("BinWidth: " + bin_width.ToString("0.00") + " ns");
            sw_IMF.WriteLine("Accumulations: " + this.ptr_UIMFDatabase.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations).ToString());
            sw_IMF.WriteLine("TimeOffset: " + this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0).ToString());

            sw_IMF.WriteLine("CalibrationSlope: " + this.ptr_UIMFDatabase.UimfFrameParams.CalibrationSlope);
            sw_IMF.WriteLine("CalibrationIntercept: " + this.ptr_UIMFDatabase.UimfFrameParams.CalibrationIntercept);

            sw_IMF.WriteLine("FrameNumber: " + this.ptr_UIMFDatabase.CurrentFrameNum.ToString());
            sw_IMF.WriteLine("AverageTOFLength: " + this.ptr_UIMFDatabase.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength).ToString("0.00") + " ns");

            if (string.IsNullOrWhiteSpace(this.ptr_UIMFDatabase.UimfFrameParams.GetValue(FrameParamKeyType.MultiplexingEncodingSequence, "")))
            {
                MessageBox.Show("menuitem_SaveIMF_Click - putting in IMFProfile...");
                sw_IMF.WriteLine("MultiplexingProfile: 4Bit_24OS.txt"); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");
            }
            else
                sw_IMF.WriteLine("MultiplexingProfile: " + this.ptr_UIMFDatabase.UimfFrameParams.GetValue(FrameParamKeyType.MultiplexingEncodingSequence, "")); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");

            sw_IMF.WriteLine("End");
            sw_IMF.Flush();
            sw_IMF.Close();

            FileStream fs_IMF = new FileStream(file_accum_IMF, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter bw_IMF = new BinaryWriter(fs_IMF);
            bw_IMF.Seek(0, SeekOrigin.End);

            // First write the escape character, which divides the ICR-2LS header from the binary data
            bw_IMF.Write((byte)27); // 27 is the ESC char
            escape_position = fs_IMF.Position;

            // Write number of accumulated TOFSpectra within the Accum Frame
            // Accumulation file will therefore be self-contained
            bw_IMF.Write((int)this.ptr_UIMFDatabase.UimfFrameParams.Scans);

            // Write counter_TIC values and the channel data size (Nodes * sizeof(Node values)]for each channel
            // Each record is made up of [Int32 TOFValue, Int16 Count]
            for (i = 0; i < this.ptr_UIMFDatabase.UimfFrameParams.Scans * 2; i++)
                bw_IMF.Write(Convert.ToInt32(0));

            double[] spectrum_array = new double[0];
            int[] bins_array = new int[0];

            num_BinTICs = new int[this.ptr_UIMFDatabase.UimfFrameParams.Scans];
            bytes_Bin = new int[this.ptr_UIMFDatabase.UimfFrameParams.Scans];

            //MessageBox.Show(this.uimf_FrameParameters.FrameNum.ToString());
            for (k = 0; k < this.ptr_UIMFDatabase.UimfFrameParams.Scans; k++)
            {
                counter_TIC = 0;
                counter_bin = 0;

                try
                {
                    this.ptr_UIMFDatabase.GetSpectrum(this.ptr_UIMFDatabase.CurrentFrameNum, (DataReader.FrameType) this.ptr_UIMFDatabase.get_FrameType(), k, out spectrum_array, out bins_array);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("menuitem_SaveIMF_Click UIMF_DataReader: " + ex.ToString());
                }

                for (j = 0; j < spectrum_array.Length; j++)
                {
                    counter_bin++;
                    counter_TIC += bins_array[j];
                    bw_IMF.Write((spectrum_array[j] - this.ptr_UIMFDatabase.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0)) * 10); // * binWidth);
                    bw_IMF.Write(bins_array[j]);
                }

                num_BinTICs[k] = counter_TIC;
                bytes_Bin[k] = counter_bin;
            }

            // Go back to the Escape Position, then pass the number of TOFSpectraPerFrame
            bw_IMF.Seek((int)escape_position + 4, SeekOrigin.Begin);

            for (k = 0; k < this.ptr_UIMFDatabase.UimfFrameParams.Scans; k++)
            {
                bw_IMF.Write(num_BinTICs[k]);
                bw_IMF.Write(bytes_Bin[k] * 8);
            }

            bw_IMF.Flush();

            bw_IMF.Close();
            fs_IMF.Close();
        }

        // /////////////////////////////////////////////////////////////
        // Set Calibration.
        //
        // the trick here is to mess with the settings without messing with the file until
        // it is requested.
        //
        public void set_Calibration(float K, float T0)
        {
            MessageBox.Show("set_Calibration needs work, IonMobilityDataView");

#if false
            if (this.frame_Data == null)
                return;

            this.mz_Calibration = cal;

            this.imfReader.Experiment_Properties.cal_a = this.mz_Calibration.A;
            this.imfReader.Experiment_Properties.cal_t0 = this.mz_Calibration.B;
            this.imfReader.Experiment_Properties.cal_Type = this.mz_Calibration.Type;

            this.tb_CalA.Text = this.imfReader.Experiment_Properties.text_CalA();
            this.tb_CalT0.Text = this.imfReader.Experiment_Properties.text_CalT0();

            this.lbl_CalibratorType.Text = this.mz_Calibration.Description;
#endif
        }

        private void CalibratorA_Changed(object obj, System.EventArgs e)
        {
            // modify the view; but not the file.
            try
            {
                this.ptr_UIMFDatabase.MzCalibration.K = (float)Convert.ToDouble(this.tb_CalA.Text);
                Calibrator_Changed();
            }
            catch (Exception ex)
            {
                this.tb_CalA.BackColor = Color.Red;
                this.btn_revertCalDefaults.Show();
            }
        }

        private void CalibratorT0_Changed(object obj, System.EventArgs e)
        {
            try
            {
                this.ptr_UIMFDatabase.MzCalibration.T0 = (float)Convert.ToDouble(this.tb_CalT0.Text);
                Calibrator_Changed();
            }
            catch (Exception ex)
            {
                this.tb_CalT0.BackColor = Color.Red;
                this.btn_revertCalDefaults.Show();
            }
        }

        public void Calibrator_Changed()
        {
            if ((Convert.ToDouble(this.tb_CalA.Text) != this.ptr_UIMFDatabase.MzCalibration.K) ||
                (Convert.ToDouble(this.tb_CalT0) != this.ptr_UIMFDatabase.MzCalibration.T0))
            {
               // this.m_frameParameters.CalibrationSlope = Convert.ToDouble(this.tb_CalA.Text); //this.UIMF_DataReader.mz_Calibration.k * 10000.0;
              //  this.m_frameParameters.CalibrationIntercept = Convert.ToDouble(this.tb_CalT0.Text); // this.UIMF_DataReader.mz_Calibration.t0 / 10000.0;
               // this.update_CalibrationCoefficients();

                this.date_Calibration.Value = DateTime.Now;

                this.tabpages_FrameInfo.SelectedTab = this.tabPage_Calibration;

                this.btn_revertCalDefaults.Show();
                this.btn_setCalDefaults.Show();
            }

            // Redraw
            // Save old scroll value to move there after conversion
            this.flag_update2DGraph = true;
        }

        private void update_CalibrationCoefficients()
        {
            this.tb_CalA.Text = this.ptr_UIMFDatabase.MzCalibration.K.ToString("E");
            this.tb_CalT0.Text = this.ptr_UIMFDatabase.MzCalibration.T0.ToString("E");
            this.lbl_CalibratorType.Text = this.ptr_UIMFDatabase.MzCalibration.Description;

            this.pnl_postProcessing.set_ExperimentalCoefficients(this.ptr_UIMFDatabase.MzCalibration.K * 10000.0, this.ptr_UIMFDatabase.MzCalibration.T0 / 10000.0);
        }

        private void btn_setCalDefaults_Click(object sender, System.EventArgs e)
        {

            this.Enabled = false;

            this.ptr_UIMFDatabase.UpdateAllCalibrationCoefficients((float)(Convert.ToSingle(this.tb_CalA.Text) * 10000.0), (float)(Convert.ToSingle(this.tb_CalT0.Text) / 10000.0));

            this.update_CalibrationCoefficients();

            this.Enabled = true;
            this.flag_update2DGraph = true;

            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();
        }

        private void btn_revertCalDefaults_Click(object sender, System.EventArgs e)
        {
            this.ptr_UIMFDatabase.ReloadFrameParameters();

            this.update_CalibrationCoefficients();

            this.flag_update2DGraph = true;

            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Internal Calibration
        //
        private void btn_ApplyCalculatedCalibration_Click(object sender, EventArgs e)
        {
            this.ptr_UIMFDatabase.UpdateCalibrationCoefficients(this.ptr_UIMFDatabase.CurrentFrameIndex, (float)this.pnl_postProcessing.Calculated_Slope,
                (float)this.pnl_postProcessing.Calculated_Intercept);

            this.update_CalibrationCoefficients();

            this.pnl_postProcessing.InitializeCalibrants(this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth, this.pnl_postProcessing.Calculated_Slope, this.pnl_postProcessing.Calculated_Intercept);

            this.flag_update2DGraph = true;
        }

        private void btn_ApplyCalibration_Experiment_Click(object sender, EventArgs e)
        {
            //MessageBox.Show((Convert.ToDouble(this.tb_CalA.Text) * 10000.0).ToString() + "  " + this.pnl_postProcessing.Experimental_Slope.ToString());
            this.ptr_UIMFDatabase.UpdateAllCalibrationCoefficients((float)this.pnl_postProcessing.get_Experimental_Slope(),
                (float)this.pnl_postProcessing.get_Experimental_Intercept());

            this.update_CalibrationCoefficients();

            this.pnl_postProcessing.InitializeCalibrants(this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth, this.pnl_postProcessing.get_Experimental_Slope(), this.pnl_postProcessing.get_Experimental_Intercept());

            this.flag_update2DGraph = true;
        }

        private bool flag_AutoCalibrate = false;
        private void btn_CalibrateFrames_Click(object sender, EventArgs e)
        {
            this.AutoCalibrateExperiment(false);
        }

        public void AutoCalibrateExperiment(bool flag_auto)
        {
            this.flag_AutoCalibrate = flag_auto;

            if (this.thread_Calibrate != null)
            {
                this.thread_Calibrate.Abort();
                this.thread_Calibrate = null;
            }

            if (this.thread_Calibrate == null)
            {
                // thread GraphFrame
                this.thread_Calibrate = new Thread(new ThreadStart(this.tick_Calibrate));
                this.thread_Calibrate.Priority = System.Threading.ThreadPriority.Normal;
            }

            this.pnl_postProcessing.update_Calibrants();

            this.thread_Calibrate.Start();
        }

        private void tick_Calibrate()
        {
            double slope;
            double intercept;
            int total_calibrants_matched;

            bool flag_CalibrateExperiment = false;

            this.Update();

            this.slide_FrameSelect.Value = this.ptr_UIMFDatabase.CurrentFrameIndex;
            this.Update();

            this.Calibrate_Frame(this.ptr_UIMFDatabase.CurrentFrameIndex, out slope, out intercept, out total_calibrants_matched);

            if (double.IsNaN(slope) || double.IsNaN(intercept))
            {
                DialogResult dr = MessageBox.Show(this, "Calibration failed.\n\nShould I continue?", "Calibration failed", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                    return;
            }
            else if (flag_CalibrateExperiment)
            {
                this.ptr_UIMFDatabase.UpdateCalibrationCoefficients(this.ptr_UIMFDatabase.CurrentFrameIndex, (float)slope, (float)intercept);
            }
            else if (slope <= 0)
            {
                //MessageBox.Show(this, "Calibration Failed");
                return;
            }
            else
            {
                this.ptr_UIMFDatabase.MzCalibration.K = slope / 10000.0;
                this.ptr_UIMFDatabase.MzCalibration.T0 = intercept * 10000.0;
                this.update_CalibrationCoefficients();
            }

            this.Update();

            if (this.flag_AutoCalibrate)
                this.ptr_UIMFDatabase.UpdateAllCalibrationCoefficients((float)(Convert.ToSingle(this.tb_CalA.Text) * 10000.0), (float)(Convert.ToSingle(this.tb_CalT0.Text) / 10000.0), this.flag_AutoCalibrate);

            this.flag_update2DGraph = true;
            this.Enabled = true;
        }

        public void Calibrate_Frame(int frame_index, out double calibration_slope, out double calibration_intercept, out int total_calibrants_matched)
        {
            int i, j, k;
            int scans;

            int uimf_bins;
            int maximum_spectrum = 0;

            double[] nonzero_bins;
            double[] nonzero_intensities;
            int above_noise_bins = 0;
#if NOISE_LEVEL
            int noise_level; // = Convert.ToInt32(this.pnl_postProcessing.num_NoiseLevel.Value);
#endif
            int compressed_bins = 0;
            int added_zeros = 0;

            int NOISE_REGION = 50;
            int noise_peaks = 0;
            int noise_intensity = 0;
            int compression;
            double[] summed_spectrum;
            bool[] flag_above_noise;
            double[] spectrum = new double[this.ptr_UIMFDatabase.UimfGlobalParams.Bins];
            int[] max_spectrum = new int[this.ptr_UIMFDatabase.UimfGlobalParams.Bins];
            int[] bins = new int[this.ptr_UIMFDatabase.UimfGlobalParams.Bins];

            double slope = this.ptr_UIMFDatabase.UimfFrameParams.CalibrationSlope;
            double intercept = this.ptr_UIMFDatabase.UimfFrameParams.CalibrationIntercept;

            int CalibrantCountMatched = 100;
            int CalibrantCountValid = 0;
            double AverageAbsoluteValueMassError = 0.0;
            double AverageMassError = 0.0;

#if COMPRESS_TO_100K
            if (this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth == .25)
                compression = 4;
            else
#endif
                compression = 1;

            calibration_slope = -1.0;
            calibration_intercept = -1.0;
            total_calibrants_matched = 0;

            summed_spectrum = new double[this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression];
            flag_above_noise = new bool[this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression];

            if (CalibrantCountMatched > 4)
            {
                // clear arrays
                for (i = 0; i < this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression; i++)
                {
                    flag_above_noise[i] = false;
                    max_spectrum[i] = 0;
                    summed_spectrum[i] = 0;
                    max_spectrum[i] = 0;
                }

                bins = this.ptr_UIMFDatabase.GetSumScans(this.ptr_UIMFDatabase.ArrayFrameNum[frame_index], 0, this.ptr_UIMFDatabase.UimfFrameParams.Scans);

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
                for (j = NOISE_REGION / 2; (j < (this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression) - NOISE_REGION); j++)
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
                for (i = 1; i < this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression; i++)
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
                for (i = 0; (i < (this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression) - 1) && (compressed_bins < above_noise_bins + added_zeros); i++)
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
                this.pnl_postProcessing.CalibrateFrame(summed_spectrum, nonzero_intensities, nonzero_bins,
                    this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth * (double)compression, this.ptr_UIMFDatabase.UimfGlobalParams.Bins / compression,
                    this.ptr_UIMFDatabase.UimfFrameParams.Scans, slope, intercept);

                CalibrantCountMatched = this.pnl_postProcessing.get_CalibrantCountMatched();
                CalibrantCountValid = this.pnl_postProcessing.get_CalibrantCountValid();
                AverageAbsoluteValueMassError = this.pnl_postProcessing.get_AverageAbsoluteValueMassError();
                AverageMassError = this.pnl_postProcessing.get_AverageMassError();

                if (CalibrantCountMatched == CalibrantCountValid)
                {
                    // done, slope and intercept acceptable
                    calibration_slope = this.pnl_postProcessing.get_Experimental_Slope();
                    calibration_intercept = this.pnl_postProcessing.get_Experimental_Intercept();
                    total_calibrants_matched = CalibrantCountMatched;
                    //break;
                }
                else if (CalibrantCountMatched > 4)
                    this.pnl_postProcessing.disable_CalibrantMaxPPMError();
            }

            this.ptr_UIMFDatabase.ClearFrameParametersCache();
        }

        private void btn_Clean_Click(object sender, EventArgs e)
        {
            MessageBox.Show("not sure what this does.  Needs work.  wfd 02/22/11");

            string filename = "c:\\IonMobilityData\\Gordon\\Calibration\\QC\\8pep_10fr_600scans_01_0000\\" + Path.GetFileNameWithoutExtension(this.ptr_UIMFDatabase.UimfDataFile) + "_clean.UIMF";

            if (File.Exists(filename))
                File.Delete(filename);

            DataWriter uimf_writer = new DataWriter(filename);
            FrameParams fp = new FrameParams();
            GlobalParams gp = new GlobalParams();
            int uimf_bins;

            uimf_writer.CreateTables("int");

            gp = this.ptr_UIMFDatabase.GetGlobalParams();
            MessageBox.Show("gp: " + gp.NumFrames.ToString());

            for (int i = 1; i <= gp.NumFrames; i++)
            {
                fp = this.ptr_UIMFDatabase.GetFrameParams(i);

                uimf_writer.InsertFrame(i, fp);

                for (int j = 0; j < this.ptr_UIMFDatabase.UimfFrameParams.Scans; j++)
                {
                    double[] binList = new double[410000];
                    int[] intensityList = new int[410000];

                    uimf_bins = this.ptr_UIMFDatabase.GetSpectrum(this.ptr_UIMFDatabase.ArrayFrameNum[i], (DataReader.FrameType) this.ptr_UIMFDatabase.get_FrameType(), j, out binList, out intensityList);
                    var nzVals = new Tuple<int, int>[uimf_bins];

                    for (int k = 0; k < uimf_bins; k++)
                    {
                        nzVals[k] = new Tuple<int, int>((int)binList[k] - 10000, intensityList[k]);
                    }

                    uimf_writer.InsertScan(i, fp, j, nzVals, this.ptr_UIMFDatabase.UimfGlobalParams.BinWidth, 0);
                }
            }

            uimf_writer.Dispose();
            MessageBox.Show("created " + filename);
        }

        // ///////////////////////////////////////////////////////////////
        // Select FrameType
        //
        private void cb_FrameType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.flag_CinemaPlot = false;

            this.Filter_FrameType(this.cb_FrameType.SelectedIndex);

            this.flag_FrameTypeChanged = true;
            this.flag_update2DGraph = true;

        }

        private void Filter_FrameType(int frame_type)
        {
            if (this.current_frame_type == frame_type)
                return;

            int frame_count = 0;
            object[] read_values = new object[0];

            frame_count = this.ptr_UIMFDatabase.set_FrameType(frame_type);
            this.current_frame_type = frame_type;
            this.ptr_UIMFDatabase.CurrentFrameIndex = -1;

            Invoke(new ThreadStart(format_Screen));

            // Reinitialize
            _zoomX.Clear();
            _zoomBin.Clear();

            this.new_minBin = 0;
            this.new_minMobility = 0;

            this.new_maxBin = this.maximum_Bins = this.ptr_UIMFDatabase.UimfGlobalParams.Bins - 1;
            this.new_maxMobility = this.maximum_Mobility = this.ptr_UIMFDatabase.UimfFrameParams.Scans - 1;

            if (frame_count == 0)
                return;

            if (this.ptr_UIMFDatabase.get_NumFrames(frame_type) > DESIRED_WIDTH_CHROMATOGRAM)
                this.num_FrameCompression.Value = this.ptr_UIMFDatabase.get_NumFrames(frame_type) / DESIRED_WIDTH_CHROMATOGRAM;
            else
            {
                this.rb_PartialChromatogram.Enabled = false;
                this.num_FrameCompression.Value = 1;
            }
            this.current_frame_compression = Convert.ToInt32(this.num_FrameCompression.Value);

            this.flag_selection_drift = false;
            this.plot_Mobility.ClearRange();

            this.num_FrameRange.Value = 1;
            this.num_FrameIndex.Maximum = frame_count - 1;
            this.num_FrameIndex.Value = 0;
            this.slide_FrameSelect.Value = 0;

            // MessageBox.Show(this.array_FrameNum.Length.ToString());

            if (frame_count < 2)
            {
                this.rb_CompleteChromatogram.Enabled = false;
                this.rb_PartialChromatogram.Enabled = false;

                this.pnl_Chromatogram.Enabled = false;
            }
            else
            {
                this.rb_CompleteChromatogram.Enabled = true;
                this.rb_PartialChromatogram.Enabled = true;

                this.pnl_Chromatogram.Enabled = true;
            }

            this.flag_update2DGraph = true;
        }

        private void format_Screen()
        {
            int frame_count = this.ptr_UIMFDatabase.get_NumFrames(this.current_frame_type);

            if (frame_count == 0)
            {
                this.pnl_2DMap.Visible = false;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = false;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = false;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = false;
                this.elementHost_FrameSelect.Visible = false;

                this.plot_TOF.GraphPane.CurveList[0].Points = new BasicArrayPointList(new double[0], new double[0]);
                this.plot_Mobility.GraphPane.CurveList[0].Points = new BasicArrayPointList(new double[0], new double[0]);

                this.lbl_FrameRange.Visible = false;
                this.num_FrameRange.Visible = false;

                return;
            }
            else
            {
                this.pnl_2DMap.Visible = true;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = true;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = true;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = true;

                this.pnl_2DMap.Visible = true;

                if (frame_count == 1)
                {
                    this.elementHost_FrameSelect.Hide();
                    this.pb_PlayLeftIn.Hide();
                    this.pb_PlayLeftOut.Hide();
                    this.pb_PlayRightIn.Hide();
                    this.pb_PlayRightOut.Hide();
                    this.num_FrameRange.Hide();
                    this.lbl_FrameRange.Hide();
                }
                else
                {
                    this.slide_FrameSelect.Value = 0;
                    if (!this.elementHost_FrameSelect.Visible)
                        this.elementHost_FrameSelect.Visible = true;
                    this.slide_FrameSelect.Minimum = 0;
                    this.slide_FrameSelect.Maximum = frame_count - 1;

                    this.lbl_FrameRange.Visible = false;
                    this.num_FrameRange.Visible = false;

                    this.pb_PlayLeftIn.Show();
                    this.pb_PlayLeftOut.Show();
                    this.pb_PlayRightIn.Show();
                    this.pb_PlayRightOut.Show();
                    this.num_FrameRange.Show();
                    this.lbl_FrameRange.Show();

                    this.elementHost_FrameSelect.Refresh();
                }
            }
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

        private void cb_EnableMZRange_CheckedChanged(object sender, EventArgs e)
        {
            this.flag_chromatograph_collected_COMPLETE = false;
            this.flag_chromatograph_collected_PARTIAL = false;

            this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
            this.rb_PartialChromatogram.ForeColor = Color.Yellow;

            this.flag_update2DGraph = true;
        }

        private void num_MZ_ValueChanged(object sender, EventArgs e)
        {
            this.flag_chromatograph_collected_COMPLETE = false;
            this.flag_chromatograph_collected_PARTIAL = false;

            this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
            this.rb_PartialChromatogram.ForeColor = Color.Yellow;

            this.flag_update2DGraph = true;
        }

        private void num_PPM_ValueChanged(object sender, EventArgs e)
        {
            this.flag_chromatograph_collected_COMPLETE = false;
            this.flag_chromatograph_collected_PARTIAL = false;

            this.rb_CompleteChromatogram.ForeColor = Color.Yellow;
            this.rb_PartialChromatogram.ForeColor = Color.Yellow;

            this.flag_update2DGraph = true;
        }

#if BELOV_TRANSFORM
        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Decode Multiplexed UIMF File
        //
        UIMF_BelovTransform.UIMF_BelovTransform uimf_BelovTransform;
        private void btn_DecodeMultiplexing_Click(object sender, EventArgs e)
        {
            this.DecodeMultiplexing();
        }

        public void DecodeMultiplexing()
        {
            this.Hide();
            if ((this.frame_progress == null) || this.frame_progress.IsDisposed)
            {
                this.frame_progress = new UIMF_File.Utilities.progress_Processing();
                this.frame_progress.btn_Cancel.Click += new EventHandler(btn_ProgressDecodeCancel_Click);
                this.frame_progress.Show();
            }

            this.uimf_BelovTransform = new UIMF_BelovTransform.UIMF_BelovTransform(this.ptr_UIMFDatabase,
                Path.Combine(this.pnl_postProcessing.tb_SaveDecodeDirectory.Text, this.pnl_postProcessing.tb_SaveDecodeFilename.Text),
                this.frame_progress, true);
        }

        private void btn_ProgressDecodeCancel_Click(object obj, System.EventArgs e)
        {
            if (!this.Visible)
            {
                this.Show();
                if (this.uimf_BelovTransform != null)
                    this.uimf_BelovTransform.flag_Stopped = true;
            }
        }
#endif

        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Compress 4GHz Data to 1GHz
        //
        Thread thread_Compress;
        private void btn_Compress1GHz_Click(object sender, EventArgs e)
        {
            this.Hide();
            if ((this.frame_progress == null) || this.frame_progress.IsDisposed)
            {
                this.frame_progress = new UIMF_File.Utilities.progress_Processing();
                this.frame_progress.btn_Cancel.Click += new EventHandler(btn_ProgressCompressCancel_Click);
                this.frame_progress.Show();
            }

            this.thread_Compress = new Thread(new ThreadStart(this.Compress4GHzUIMF));
            this.thread_Compress.Priority = System.Threading.ThreadPriority.Lowest;
            this.thread_Compress.Start();

            //Invoke(new ThreadStart(this.Compress4GHzUIMF));
        }

        private void btn_ProgressCompressCancel_Click(object obj, System.EventArgs e)
        {
            this.Show();
#if BELOV_TRANSFORM
            if (this.uimf_BelovTransform != null)
                this.uimf_BelovTransform.flag_Stopped = true;
#endif
        }

        private void Compress4GHzUIMF()
        {
            UIMFLibrary.GlobalParams gp = this.ptr_UIMFDatabase.GetGlobalParams();
            UIMFLibrary.FrameParams fp;
            string name_instrument;
            int i;
            int j;
            int k;
            int current_frame;
            int[] current_intensities = new int[gp.Bins/4];

            double[] array_Bins = new double[0];
            int[] array_Intensity = new int[0];
            var list_nzVals = new List<Tuple<int, int>>();
            List<int> list_Scans = new List<int>();
            List<int> list_Count = new List<int>();

            Stopwatch stop_watch = new Stopwatch();

            // create new UIMF File
            string UIMF_filename = Path.Combine(this.pnl_postProcessing.tb_SaveCompressDirectory.Text, this.pnl_postProcessing.tb_SaveCompressFilename.Text + "_1GHz.UIMF");
            if (File.Exists(UIMF_filename))
            {
                if (MessageBox.Show("File Exists", "File Exists, Replace?", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    File.Delete(UIMF_filename);
                else
                    return;
            }

            UIMFLibrary.DataWriter UIMF_Writer = new UIMFLibrary.DataWriter(UIMF_filename);
            UIMF_Writer.CreateTables(null);

            gp.AddUpdateValue(GlobalParamKeyType.BinWidth, 1);
            gp.AddUpdateValue(GlobalParamKeyType.Bins, gp.Bins / 4);
            UIMF_Writer.InsertGlobal(gp);

            // make sure the instrument name is in the right format - either QTOF or TOF
            name_instrument = gp.GetValueString(GlobalParamKeyType.InstrumentName);
            if ((name_instrument == null) || (name_instrument.Length == 0))
                name_instrument = "QTOF";
            else if (name_instrument != "QTOF" && name_instrument != "TOF")
            {
                // BelovTransform.cpp nly knows about instruments QTOF and TOF
                // Try to auto-update mCachedGlobalParams.InstrumentName
                if (name_instrument.ToUpper().StartsWith("IMS"))
                    name_instrument = "QTOF";
                else
                {
                    //  ShowMessage("Instrument name of " + name_instrument + " is not recognized by BelovTransform.cpp; results will likely be invalid");
                    name_instrument = "QTOF";
                }
            }

            int max_time = 0;

            this.frame_progress.Min = 0;
            this.frame_progress.Max = gp.NumFrames;
            this.frame_progress.Show();
            this.frame_progress.Update();
            this.frame_progress.Initialize();

            for (current_frame = 0; ((current_frame < (int) this.ptr_UIMFDatabase.get_FrameType()) && !this.frame_progress.flag_Stop); current_frame++)
            {
                this.frame_progress.SetValue(current_frame, (int)stop_watch.ElapsedMilliseconds);

                stop_watch.Reset();
                stop_watch.Start();

                fp = this.ptr_UIMFDatabase.GetFrameParams(current_frame);
                UIMF_Writer.InsertFrame(current_frame, fp);

                for (i = 0; i < fp.Scans; i++)
                {
                    for (j = 0; j < gp.Bins; j++)
                    {
                        current_intensities[j] = 0;
                    }

                    this.ptr_UIMFDatabase.GetSpectrum(this.ptr_UIMFDatabase.ArrayFrameNum[current_frame], (DataReader.FrameType) this.ptr_UIMFDatabase.get_FrameType(), i, out array_Bins, out array_Intensity);

                    for (j = 0; j < array_Bins.Length; j++)
                        current_intensities[(int) array_Bins[j] / 4] += array_Intensity[j];

                    list_nzVals.Clear();
                    for (j=0; j<gp.Bins; j++)
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
                    this.frame_progress.add_Status("Max Time: Frame " + current_frame.ToString() + " ..... " + max_time.ToString() + " msec", false);
                }
            }

            this.Show();
            if (this.frame_progress.Success())
            {
                this.frame_progress.Close();
            }

            UIMF_Writer.Dispose();
        }
    }
}
