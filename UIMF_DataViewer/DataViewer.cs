//#define DEBUGGING
//#define HIDE_CALIBRATION
#define COLOR_MAP
#define SHOW
//#define STOP_WATCH
#define COMPRESS_TO_100K

using System;
using System.Drawing;
using System.Drawing.Drawing2D; 
using System.Drawing.Imaging; 
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel; 
using System.Windows.Forms;
using System.Threading;
using System.IO; 
using Microsoft.Win32; 
using System.Runtime.InteropServices;
using UIMFLibrary;
using System.Diagnostics;

#if STOP_WATCH
using System.Diagnostics;
#endif

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

    public unsafe class DataViewer : System.Windows.Forms.Form
    {
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, Int32 dwRop);

        #region HIDE
        private System.Windows.Forms.ContextMenu contextMenu_pb_2DMap;
        private System.Windows.Forms.MenuItem menuItem_SelectionCorners;
        protected System.Windows.Forms.MenuItem menuItemZoomFull;
        protected System.Windows.Forms.MenuItem menuItemZoomPrevious;
        protected System.Windows.Forms.MenuItem menuItemZoomOut;
        private System.Windows.Forms.MenuItem menuItem_MaxIntensities;
        private System.Windows.Forms.MenuItem menuItem5;
        private System.Windows.Forms.MenuItem menuItemConvertToMZ;
        private System.Windows.Forms.MenuItem menuItemConvertToTOF;
        protected System.Windows.Forms.Label lbl_IonMobilityValue;
        protected System.Windows.Forms.Label lbl_TOForMZ;
        protected System.Windows.Forms.PictureBox pb_2DMap;
        private System.Windows.Forms.ContextMenu contextMenu_HorizontalAxis;
        private System.Windows.Forms.MenuItem menuItem_UseScans;
        private System.Windows.Forms.MenuItem menuItem_UseDriftTime;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.ContextMenu contextMenu_driftTIC;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.MenuItem menuItem8;
        private System.Windows.Forms.MenuItem menuItem_3pointSmoothing;
        private System.Windows.Forms.MenuItem menuItem_5pointSmoothing;
        private System.Windows.Forms.MenuItem menuItem_ResetSmoothing;
        private System.Windows.Forms.MenuItem menuItem9;
        private System.Windows.Forms.MenuItem menuItem_Exportnew_driftTIC;
        protected NationalInstruments.UI.WindowsForms.WaveformGraph plot_TOF;
      //  private NationalInstruments.UI.XAxis axis_xTOF;
     //   private NationalInstruments.UI.YAxis axis_yTOF;
        protected System.Windows.Forms.Label label2;
        private NationalInstruments.UI.WaveformPlot waveformPlot3;
        private NationalInstruments.UI.XAxis xAxis2;
        private NationalInstruments.UI.YAxis yAxis2;
        private NationalInstruments.UI.XYCursor xyCursor1;
       // private UIMF_File.Utilities.VerticalLabel verticalLabel_Threshold;
        private System.Windows.Forms.MenuItem menuItem_Export;
        private System.Windows.Forms.MenuItem menuItem_ExportAll;
        private System.Windows.Forms.MenuItem menuItem_CopyToClipboard;
        private System.Windows.Forms.MenuItem menuItem_SuperFrame;
        private System.Windows.Forms.MenuItem menuItem_SuperExperiment;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem3a;
        private System.Windows.Forms.MenuItem menuItem_Mobility;
        private System.Windows.Forms.MenuItem menuItem_ScanTime;
        protected UIMF_File.Utilities.PointAnnotationGraph plot_Mobility;
        private NationalInstruments.UI.XYCursor xyCursor2;
        private System.Windows.Forms.ContextMenu contextMenu_TOF;
        private System.Windows.Forms.MenuItem menuItem_TOFExport;
        private System.Windows.Forms.MenuItem menuItem_TOFMaximum;
        private System.Windows.Forms.PictureBox pb_SliderBackground;
       // private IContainer components;
        protected NationalInstruments.UI.XAxis xAxis_Mobility;
        private NationalInstruments.UI.YAxis yAxis_Mobility;
        protected NationalInstruments.UI.YAxis yAxis_TOF;
        protected System.Windows.Forms.Label lbl_TimeOffset;
        protected System.Windows.Forms.NumericUpDown num_minMobility;
        protected System.Windows.Forms.NumericUpDown num_maxMobility;
        protected System.Windows.Forms.NumericUpDown num_minBin;
        protected System.Windows.Forms.NumericUpDown num_maxBin;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label lbl_CursorMobility;
        protected System.Windows.Forms.Label lbl_CursorTOF;
        protected System.Windows.Forms.Label lbl_CursorMZ;
        protected NationalInstruments.UI.WaveformPlot waveform_TOFPlot;
        protected NationalInstruments.UI.WaveformPlot waveform_MobilityPlot;

        private UIMF_File.Utilities.GrayScaleSlider slider_PlotBackground;
        #endregion

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
        private NationalInstruments.UI.XAxis xAxis_TOF;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label lbl_CursorScanTime;
        public NationalInstruments.UI.WindowsForms.Slide slide_Threshold;
        private System.Windows.Forms.Button btn_Reset;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem_CaptureExperimentFrame;

        private const int TIC_INTENSITY_AXIS_DIST = 3;
        const int PLOT_TOF_WIDTH = 200;
        protected System.Windows.Forms.Label lbl_ExperimentDate;
        public System.Windows.Forms.TabControl tabpages_FrameInfo;
        private System.Windows.Forms.TabPage tabPage_Cursor;
        protected System.Windows.Forms.TabPage tabPage_Calibration;
        private System.Windows.Forms.Button btn_setCalDefaults;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DateTimePicker date_Calibration;
        private System.Windows.Forms.Label lbl_CalibratorType;
        private System.Windows.Forms.Button btn_revertCalDefaults;
        protected const int plot_Mobility_HEIGHT = 150;

        public bool flag_chromatograph_collected_PARTIAL = false;
        public bool flag_chromatograph_collected_COMPLETE = false;
        private PictureBox pb_PlayRightOut;
        private PictureBox pb_PlayRightIn;
        private PictureBox pb_PlayLeftIn;
        private PictureBox pb_PlayLeftOut;
        protected HScrollBar hsb_2DMap;
        protected VScrollBar vsb_2DMap;
        private TextBox tb_CalT0;
        private TextBox tb_CalA;
        private NationalInstruments.UI.WindowsForms.Thermometer slide_FrameSelect;
        private Label lbl_FrameRange;
        private NumericUpDown num_FrameRange;
        private Label lbl_FramesShown;
        public UIMF_File.Utilities.progress_Processing frame_progress;

        private int current_frame_index = 0; // -1 is dragdrop??????  0 is ignored...
        private ProgressBar progress_ReadingFile;
        protected bool flag_GraphingFrame = false;

        protected bool flag_Alive = true;

        public bool flag_kill_mouse = false;
        private Button btn_Refresh;  // while plotting, prevent zooming!
        protected object lock_graphing = new object();

        private UIMF_File.Utilities.ExportExperiment form_ExportExperiment;
        public NumericUpDown num_TICThreshold;
        private Button btn_TIC;
        protected CheckBox cb_MaxScanValue;

        public string UIMF_DataFile;
        public UIMFLibrary.DataReader UIMF_DataReader;

        private System.Drawing.Graphics pb_2DMap_Extensions;
        private Pen thick_pen = new Pen(new SolidBrush(Color.Silver), 1);
        private int flag_MovingCorners = -1;

        private int max_plot_width = 200;
        private int max_plot_height = 200;
        public NumericUpDown num_FrameCompression;

        private Label lbl_FrameCompression;
        public RadioButton rb_CompleteChromatogram;
        public RadioButton rb_PartialChromatogram;
        protected Panel pnl_Chromatogram;

        private int current_frame_compression;

        UIMF_File.PostProcessing pnl_postProcessing = null;

        private UIMFLibrary.GlobalParameters uimf_GlobalParameters;

        protected bool flag_Closing = false;
        private MenuItem menuItem_SaveIMF;
        protected TabControl tabpages_Main;
        protected TabPage tab_DataViewer;
        private TabPage tab_PostProcessing;
    //    private bool flag_TextFile = false;
        protected NumericUpDown num_FrameIndex;
        protected GroupBox gb_MZRange;
        private Label lbl_PPM;
        private Label lbl_MZ;
        private NumericUpDown num_PPM;
        private Label label1;
        private NumericUpDown num_MZ;
        protected CheckBox cb_EnableMZRange;
        private Label lbl_Chromatogram;
        private ComboBox cb_FrameType;
        protected bool flag_FrameTypeChanged = false;
        private TabPage tab_InstrumentSettings;

        private UIMF_DataViewer.InstrumentSettings pnl_InstrumentSettings;

        public DataViewer()
        {
            try
            {
                this.build_Interface(true);

                this.cb_FrameType.Items.Add("Thermo File");
                this.cb_FrameType.SelectedIndex = 0;

                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = false;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = false;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = false;
                this.slide_FrameSelect.Visible = false;
                this.num_TICThreshold.Visible = false;
                this.btn_TIC.Visible = false;

                this.plot_TOF.ClearData();
                this.plot_Mobility.ClearData();

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
#if DEBUGGING
            MessageBox.Show("1");
#endif
            this.UIMF_DataFile = uimf_file;

            this.UIMF_DataReader = new UIMFLibrary.DataReader();
            this.UIMF_DataReader.OpenUIMF(this.UIMF_DataFile);
            this.uimf_GlobalParameters = this.UIMF_DataReader.GetGlobalParameters();
            this.current_minBin = 0;
            this.current_maxBin = this.maximum_Bins = this.uimf_GlobalParameters.Bins;

            this.uimf_GlobalParameters = (UIMFLibrary.GlobalParameters)this.UIMF_DataReader.GetGlobalParameters();
#if DEBUGGING
            MessageBox.Show("1a");
#endif

            try
            {
                this.build_Interface(flag_enablecontrols);
            }
            catch (Exception ex)
            {
                MessageBox.Show("failed to build interface()\n\n" + ex.ToString());
            }

            for (int i = 0; i < DataReader.FRAME_TYPE_COUNT; i++)
                this.cb_FrameType.Items.Add(this.UIMF_DataReader.FrameTypeDescription(i));

            this.cb_FrameType.SelectedIndex = this.UIMF_DataReader.get_FrameType();

            this.slide_FrameSelect.Range = new NationalInstruments.UI.Range(0, this.uimf_GlobalParameters.NumFrames);

#if SCROLLBAR_BUSY
            this.hsb_2DMap.Leave += new System.EventHandler(this.leave_Scrollbar);
            this.hsb_2DMap.Enter += new System.EventHandler(this.enter_Scrollbar);
            this.vsb_2DMap.Leave += new System.EventHandler(this.leave_Scrollbar);
            this.vsb_2DMap.Enter += new System.EventHandler(this.enter_Scrollbar);
#endif

#if DEBUGGING
            MessageBox.Show("2");
#endif

            this.IonMobilityDataView_Resize((object)null, (EventArgs)null);

            this.Filter_FrameType(this.UIMF_DataReader.get_FrameType());
            Generate2DIntensityArray(0);
            this.GraphFrame(this.data_2D, flag_enablecontrols);

#if DEBUGGING
            MessageBox.Show("3");
#endif

            this.num_TICThreshold.Visible = false;
            this.btn_TIC.Visible = false;

            if (this.uimf_GlobalParameters.NumFrames > DESIRED_WIDTH_CHROMATOGRAM)
                this.num_FrameCompression.Value = this.uimf_GlobalParameters.NumFrames / DESIRED_WIDTH_CHROMATOGRAM;
            else
                this.num_FrameCompression.Value = 1;
            this.current_frame_compression = Convert.ToInt32(this.num_FrameCompression.Value);

            this.Width = this.pb_2DMap.Left + this.UIMF_DataReader.m_frameParameters.Scans + 170;

#if DEBUGGING
            MessageBox.Show("4");
#endif

#if COMPRESS_TO_100K
            // MessageBox.Show("initializeCalibrants: " + this.UIMF_DataReader.mz_Calibration.k.ToString());
            this.pnl_postProcessing.InitializeCalibrants(1, this.UIMF_DataReader.m_frameParameters.CalibrationSlope, this.UIMF_DataReader.m_frameParameters.CalibrationIntercept);
#else
            this.pnl_postProcessing.InitializeCalibrants(this.uimf_GlobalParameters.BinWidth, this.UIMF_DataReader.m_frameParameters.CalibrationSlope, this.UIMF_DataReader.m_frameParameters.CalibrationIntercept);
#endif

#if DEBUGGING
            MessageBox.Show("5");
#endif
            this.pnl_postProcessing.tb_SaveDecodeFilename.Text = Path.GetFileNameWithoutExtension(this.UIMF_DataFile);
            this.pnl_postProcessing.tb_SaveDecodeDirectory.Text = Path.GetDirectoryName(this.UIMF_DataFile);

            if (this.uimf_GlobalParameters.BinWidth != .25)
                this.pnl_postProcessing.gb_Compress4GHz.Hide();
            else
            {
                this.pnl_postProcessing.btn_Compress1GHz.Click += new System.EventHandler(this.btn_Compress1GHz_Click);
                this.pnl_postProcessing.tb_SaveCompressFilename.Text = Path.GetFileNameWithoutExtension(this.UIMF_DataFile);
                this.pnl_postProcessing.tb_SaveCompressDirectory.Text = Path.GetDirectoryName(this.UIMF_DataFile);
            }
        }

        private void build_Interface(bool flag_enablecontrols)
        {
#if DEBUGGING
            MessageBox.Show("A");
#endif

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.pnl_InstrumentSettings = new UIMF_DataViewer.InstrumentSettings();
            this.tab_InstrumentSettings.Controls.Add(this.pnl_InstrumentSettings);
            this.pnl_InstrumentSettings.Top = 0;
            this.pnl_InstrumentSettings.Left = 0;
            this.pnl_InstrumentSettings.Width = Screen.PrimaryScreen.Bounds.Width;
            this.pnl_InstrumentSettings.Height = Screen.PrimaryScreen.Bounds.Height;

            this.current_frame_index = 0;

            this.pnl_postProcessing = new PostProcessing(MainKey);
            this.pnl_postProcessing.Left = 0; 
            this.pnl_postProcessing.Top = 0;
#if DEBUGGING
            MessageBox.Show("B");
#endif

            this.tab_PostProcessing.Controls.Add(this.pnl_postProcessing);

#if CONTROL_BOX
            this.ControlBox = false;
#endif

            this.slider_ColorMap = new UIMF_File.Utilities.Intensity_ColorMap();
#if COLOR_MAP
            this.tab_DataViewer.Controls.Add(this.slider_ColorMap);
#endif
            this.slider_PlotBackground = new UIMF_File.Utilities.GrayScaleSlider(this.pb_SliderBackground);
            this.tab_DataViewer.Controls.Add(this.slider_PlotBackground);
#if DEBUGGING
            MessageBox.Show("C");
#endif

            this.plot_TOF.Left = 0;
            this.plot_TOF.Top = 0;

            menuItem_UseDriftTime.Checked = !_useDriftTime;
            menuItem_UseScans.Checked = _useDriftTime;

            this.AutoScroll = false;

            // label the axis'
            // left plot
            this.yAxis_TOF.Caption = "Time of Flight";
            this.yAxis_TOF.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));

            // bottom plot
            this.xAxis_Mobility.Caption = "Mobility - Scans";
            this.xAxis_Mobility.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.yAxis_Mobility.Caption = "Drift Intensity";
            this.yAxis_Mobility.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.yAxis_Mobility.Position = NationalInstruments.UI.YAxisPosition.Right;

            //this.slider_PlotBackground.btn_GreyValue.MouseUp += new MouseEventHandler( this.slider_Background_MouseUp );
            this.slider_PlotBackground.btn_GreyValue.Move += new EventHandler(this.slider_Background_Move);
#if DEBUGGING
            MessageBox.Show("D");
#endif

            // starts with the mobility view
            this.flag_viewMobility = true;
            this.menuItem_Mobility.Checked = true;
            this.menuItem_ScanTime.Checked = false;

            // start the heartbeat
            this.slide_FrameSelect.Value = 0;

            this.plot_TOF.Width = 200;
            this.plot_Mobility.Height = 150;

            // default values in the calibration require no interface
            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();

            this.pb_PlayLeftIn.SendToBack();
            this.pb_PlayLeftOut.BringToFront();
            this.pb_PlayRightIn.SendToBack();
            this.pb_PlayRightOut.BringToFront();
            this.slide_FrameSelect.SendToBack();
#if DEBUGGING
            MessageBox.Show("E");
#endif

            this.lbl_FramesShown.Hide();

            this.cb_MaxScanValue.BringToFront();
            this.cb_MaxScanValue.ForeColor = Color.DarkBlue;

            //this.AllowDrop = true;

#if DEBUGGING
            MessageBox.Show("F");
#endif

#if HIDE_CALIBRATION
            this.btn_Calibration.Visible = false;
#endif

            Thread.Sleep(200);
            this.Show();
            this.menuItem_ScanTime.PerformClick();

            if (flag_enablecontrols)
            {
                this.menuItem_SelectionCorners.Click += new System.EventHandler(this.menuItem_SelectionCorners_Click);
                this.menuItem_ScanTime.Click += new System.EventHandler(this.ScanTime_ContextMenu);
                this.menuItem_Mobility.Click += new System.EventHandler(this.Mobility_ContextMenu);
                this.menuItem_Export.Click += new System.EventHandler(this.menuItem_Export_Click);
                this.menuItem_ExportAll.Click += new System.EventHandler(this.menuItem_ExportAll_Click);
                this.menuItem_SuperFrame.Click += new System.EventHandler(this.menuItem_SuperFrame_Click);
                this.menuItem_SuperExperiment.Click += new System.EventHandler(this.menuItem_SuperExperiment_Click);
                this.menuItem_CopyToClipboard.Click += new System.EventHandler(this.menuItem_CopyToClipboard_Click);
                this.menuItem_CaptureExperimentFrame.Click += new System.EventHandler(this.menuItem_CaptureExperimentFrame_Click);
                this.menuItem_SaveIMF.Click += new System.EventHandler(this.menuitem_SaveIMF_Click);
                this.menuItem_UseScans.Click += new System.EventHandler(this.menuItem_UseScans_Click);
                this.menuItem_UseDriftTime.Click += new System.EventHandler(this.menuItem_UseDriftTime_Click);
                this.menuItem_Exportnew_driftTIC.Click += new System.EventHandler(this.menuItem_ExportDriftTIC_Click);
                this.menuItem_TOFExport.Click += new System.EventHandler(this.menuItem_TOFExport_Click);
                this.menuItem_TOFMaximum.Click += new System.EventHandler(this.menuItem_TOFMaximum_Click);
                this.menuItemZoomFull.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItemZoomPrevious.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItemZoomOut.Click += new System.EventHandler(this.ZoomContextMenu);
                this.menuItem_MaxIntensities.Click += new System.EventHandler(this.menuItem_TOFMaximum_Click);
                this.menuItemConvertToMZ.Click += new System.EventHandler(this.ConvertContextMenu);
                this.menuItemConvertToTOF.Click += new System.EventHandler(this.ConvertContextMenu);

                this.pb_2DMap.DoubleClick += new System.EventHandler(this.pb_2DMap_DblClick);
                this.pb_2DMap.MouseLeave += new System.EventHandler(this.pb_2DMap_MouseLeave);
                this.pb_2DMap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pb_2DMap_MouseMove);
                this.pb_2DMap.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pb_2DMap_MouseDown);
                this.pb_2DMap.Paint += new System.Windows.Forms.PaintEventHandler(this.pb_2DMap_Paint);
                this.pb_2DMap.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pb_2DMap_MouseUp);

                this.plot_Mobility.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_Mobility_MouseDown);
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
                this.plot_TOF.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_TOF_MouseDown);

                this.rb_CompleteChromatogram.CheckedChanged += new System.EventHandler(this.rb_CompleteChromatogram_CheckedChanged);
                this.rb_PartialChromatogram.CheckedChanged += new System.EventHandler(this.rb_PartialChromatogram_CheckedChanged);
                this.num_FrameCompression.ValueChanged += new System.EventHandler(this.num_FrameCompression_ValueChanged);
                
                this.cb_MaxScanValue.CheckedChanged += new System.EventHandler(this.cb_MaxScanValue_CheckedChanged);
                this.btn_TIC.Click += new System.EventHandler(this.btn_TIC_Click);
               
                this.num_FrameRange.ValueChanged += new System.EventHandler(this.num_FrameRange_ValueChanged);
                this.slide_FrameSelect.ValueChanged += new System.EventHandler(this.slide_FrameSelect_ValueChanged);
               
                this.vsb_2DMap.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vsb_2DMap_Scroll);
                this.hsb_2DMap.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsb_2DMap_Scroll);

                this.tb_CalT0.Click += new System.EventHandler(this.CalibratorT0_Changed);
                this.tb_CalT0.Leave += new System.EventHandler(this.CalibratorT0_Changed);
                this.tb_CalA.Click += new System.EventHandler(this.CalibratorA_Changed);
                this.tb_CalA.Leave += new System.EventHandler(this.CalibratorA_Changed);
              
                this.btn_Reset.Click += new System.EventHandler(this.btn_Reset_Clicked);
                this.slide_Threshold.ValueChanged += new System.EventHandler(this.slide_Threshold_ValueChanged);
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
                this.tabpages_Main.Resize += new EventHandler(this.tabpages_Main_Resize);
                
                this.cb_FrameType.SelectedIndexChanged += new System.EventHandler(this.cb_FrameType_SelectedIndexChanged);

                this.pnl_postProcessing.btn_DecodeMultiplexing.Click += new System.EventHandler(this.btn_DecodeMultiplexing_Click);
            }
        }

        private static RegistryKey MainKey
        {
            get { return Application.UserAppDataRegistry; }
        }

        private void tabpages_Main_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            /*
#if COMPRESS_TO_100K
            this.pnl_postProcessing.InitializeCalibrants(1, this.UIMF_DataReader.m_frameParameters.CalibrationSlope, this.UIMF_DataReader.m_frameParameters.CalibrationIntercept);
#else
            this.pnl_postProcessing.InitializeCalibrants(this.uimf_GlobalParameters.BinWidth, this.uimf_FrameParameters.CalibrationSlope, this.uimf_FrameParameters.CalibrationIntercept);
#endif
             */
            if (this.tabpages_Main.SelectedTab == this.tab_PostProcessing)
            {
                if (this.Width < this.pnl_postProcessing.dg_Calibrants.Left + this.pnl_postProcessing.dg_Calibrants.Width + 70)
                    this.Width = this.pnl_postProcessing.dg_Calibrants.Left + this.pnl_postProcessing.dg_Calibrants.Width + 70;
            }
            else if (this.tabpages_Main.SelectedTab == this.tab_InstrumentSettings)
            {
                this.pnl_InstrumentSettings.update_Frame(this.UIMF_DataReader.GetFrameParameters(this.current_frame_index));
            }
            else
                this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
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
        protected override void Dispose(bool disposing)
        {
            this.flag_Alive = false;
            this.flag_Closing = true;

            RegistrySave(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
            this.pnl_postProcessing.Save_Registry();

            if (this.flag_CinemaPlot)
            {
                this.StopCinema();
                Thread.Sleep(300);
            }

            if (this.UIMF_DataReader != null)
                this.UIMF_DataReader.CloseUIMF(this.UIMF_DataFile);

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

        protected override void OnPaint(PaintEventArgs e)
        {
            // moving the application on and off the screen causes the 
            // pb_2DGraph to rewrite - while the other paint event locks the bits.
            // ignore the system paint. 
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataViewer));
            this.contextMenu_pb_2DMap = new System.Windows.Forms.ContextMenu();
            this.menuItemZoomFull = new System.Windows.Forms.MenuItem();
            this.menuItemZoomPrevious = new System.Windows.Forms.MenuItem();
            this.menuItemZoomOut = new System.Windows.Forms.MenuItem();
            this.menuItem_MaxIntensities = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItemConvertToMZ = new System.Windows.Forms.MenuItem();
            this.menuItemConvertToTOF = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem_Mobility = new System.Windows.Forms.MenuItem();
            this.menuItem_ScanTime = new System.Windows.Forms.MenuItem();
            this.menuItem3a = new System.Windows.Forms.MenuItem();
            this.menuItem_SelectionCorners = new System.Windows.Forms.MenuItem();
            this.menuItem_Export = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportAll = new System.Windows.Forms.MenuItem();
            this.menuItem_SuperFrame = new System.Windows.Forms.MenuItem();
            this.menuItem_SuperExperiment = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem_CopyToClipboard = new System.Windows.Forms.MenuItem();
            this.menuItem_CaptureExperimentFrame = new System.Windows.Forms.MenuItem();
            this.menuItem_SaveIMF = new System.Windows.Forms.MenuItem();
            this.label3 = new System.Windows.Forms.Label();
            this.lbl_CursorScanTime = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbl_CursorMZ = new System.Windows.Forms.Label();
            this.lbl_CursorTOF = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbl_TOForMZ = new System.Windows.Forms.Label();
            this.lbl_IonMobilityValue = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbl_TimeOffset = new System.Windows.Forms.Label();
            this.lbl_CursorMobility = new System.Windows.Forms.Label();
            this.contextMenu_HorizontalAxis = new System.Windows.Forms.ContextMenu();
            this.menuItem_UseScans = new System.Windows.Forms.MenuItem();
            this.menuItem_UseDriftTime = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.xyCursor1 = new NationalInstruments.UI.XYCursor();
            this.waveformPlot3 = new NationalInstruments.UI.WaveformPlot();
            this.xAxis2 = new NationalInstruments.UI.XAxis();
            this.yAxis2 = new NationalInstruments.UI.YAxis();
            this.contextMenu_driftTIC = new System.Windows.Forms.ContextMenu();
            this.menuItem_3pointSmoothing = new System.Windows.Forms.MenuItem();
            this.menuItem_5pointSmoothing = new System.Windows.Forms.MenuItem();
            this.menuItem_ResetSmoothing = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuItem_Exportnew_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem9 = new System.Windows.Forms.MenuItem();
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this.plot_TOF = new NationalInstruments.UI.WindowsForms.WaveformGraph();
            this.waveform_TOFPlot = new NationalInstruments.UI.WaveformPlot();
            this.xAxis_TOF = new NationalInstruments.UI.XAxis();
            this.yAxis_TOF = new NationalInstruments.UI.YAxis();
            this.contextMenu_TOF = new System.Windows.Forms.ContextMenu();
            this.menuItem_TOFExport = new System.Windows.Forms.MenuItem();
            this.menuItem_TOFMaximum = new System.Windows.Forms.MenuItem();
            this.num_minMobility = new System.Windows.Forms.NumericUpDown();
            this.num_maxMobility = new System.Windows.Forms.NumericUpDown();
            this.num_maxBin = new System.Windows.Forms.NumericUpDown();
            this.num_minBin = new System.Windows.Forms.NumericUpDown();
            this.slide_Threshold = new NationalInstruments.UI.WindowsForms.Slide();
            this.btn_Reset = new System.Windows.Forms.Button();
            this.lbl_ExperimentDate = new System.Windows.Forms.Label();
            this.tabpages_FrameInfo = new System.Windows.Forms.TabControl();
            this.tabPage_Cursor = new System.Windows.Forms.TabPage();
            this.tabPage_Calibration = new System.Windows.Forms.TabPage();
            this.tb_CalT0 = new System.Windows.Forms.TextBox();
            this.tb_CalA = new System.Windows.Forms.TextBox();
            this.btn_setCalDefaults = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.date_Calibration = new System.Windows.Forms.DateTimePicker();
            this.lbl_CalibratorType = new System.Windows.Forms.Label();
            this.btn_revertCalDefaults = new System.Windows.Forms.Button();
            this.hsb_2DMap = new System.Windows.Forms.HScrollBar();
            this.vsb_2DMap = new System.Windows.Forms.VScrollBar();
            this.slide_FrameSelect = new NationalInstruments.UI.WindowsForms.Thermometer();
            this.lbl_FrameRange = new System.Windows.Forms.Label();
            this.num_FrameRange = new System.Windows.Forms.NumericUpDown();
            this.lbl_FramesShown = new System.Windows.Forms.Label();
            this.btn_Refresh = new System.Windows.Forms.Button();
            this.num_TICThreshold = new System.Windows.Forms.NumericUpDown();
            this.btn_TIC = new System.Windows.Forms.Button();
            this.cb_MaxScanValue = new System.Windows.Forms.CheckBox();
            this.num_FrameCompression = new System.Windows.Forms.NumericUpDown();
            this.lbl_FrameCompression = new System.Windows.Forms.Label();
            this.rb_CompleteChromatogram = new System.Windows.Forms.RadioButton();
            this.rb_PartialChromatogram = new System.Windows.Forms.RadioButton();
            this.pnl_Chromatogram = new System.Windows.Forms.Panel();
            this.tabpages_Main = new System.Windows.Forms.TabControl();
            this.tab_DataViewer = new System.Windows.Forms.TabPage();
            this.cb_FrameType = new System.Windows.Forms.ComboBox();
            this.gb_MZRange = new System.Windows.Forms.GroupBox();
            this.lbl_PPM = new System.Windows.Forms.Label();
            this.lbl_MZ = new System.Windows.Forms.Label();
            this.num_PPM = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.num_MZ = new System.Windows.Forms.NumericUpDown();
            this.cb_EnableMZRange = new System.Windows.Forms.CheckBox();
            this.num_FrameIndex = new System.Windows.Forms.NumericUpDown();
            this.pb_PlayRightOut = new System.Windows.Forms.PictureBox();
            this.pb_PlayLeftIn = new System.Windows.Forms.PictureBox();
            this.pb_2DMap = new System.Windows.Forms.PictureBox();
            this.pb_SliderBackground = new System.Windows.Forms.PictureBox();
            this.lbl_Chromatogram = new System.Windows.Forms.Label();
            this.plot_Mobility = new UIMF_File.Utilities.PointAnnotationGraph();
            this.xyCursor2 = new NationalInstruments.UI.XYCursor();
            this.waveform_MobilityPlot = new NationalInstruments.UI.WaveformPlot();
            this.xAxis_Mobility = new NationalInstruments.UI.XAxis();
            this.yAxis_Mobility = new NationalInstruments.UI.YAxis();
            this.pb_PlayLeftOut = new System.Windows.Forms.PictureBox();
            this.pb_PlayRightIn = new System.Windows.Forms.PictureBox();
            this.tab_PostProcessing = new System.Windows.Forms.TabPage();
            this.tab_InstrumentSettings = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.xyCursor1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.plot_TOF)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxBin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minBin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.slide_Threshold)).BeginInit();
            this.tabpages_FrameInfo.SuspendLayout();
            this.tabPage_Cursor.SuspendLayout();
            this.tabPage_Calibration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.slide_FrameSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameRange)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_TICThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameCompression)).BeginInit();
            this.pnl_Chromatogram.SuspendLayout();
            this.tabpages_Main.SuspendLayout();
            this.tab_DataViewer.SuspendLayout();
            this.gb_MZRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_PPM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayRightOut)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayLeftIn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_2DMap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_SliderBackground)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.plot_Mobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xyCursor2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayLeftOut)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayRightIn)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenu_pb_2DMap
            // 
            this.contextMenu_pb_2DMap.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemZoomFull,
            this.menuItemZoomPrevious,
            this.menuItemZoomOut,
            this.menuItem_MaxIntensities,
            this.menuItem5,
            this.menuItemConvertToMZ,
            this.menuItemConvertToTOF,
            this.menuItem3,
            this.menuItem_Mobility,
            this.menuItem_ScanTime,
            this.menuItem3a,
            this.menuItem_SelectionCorners,
            this.menuItem_Export,
            this.menuItem_ExportAll,
            this.menuItem_SuperFrame,
            this.menuItem_SuperExperiment,
            this.menuItem1,
            this.menuItem_CopyToClipboard,
            this.menuItem_CaptureExperimentFrame,
            this.menuItem_SaveIMF});
            // 
            // menuItemZoomFull
            // 
            this.menuItemZoomFull.Index = 0;
            this.menuItemZoomFull.Text = "Zoom Full";
            // 
            // menuItemZoomPrevious
            // 
            this.menuItemZoomPrevious.Index = 1;
            this.menuItemZoomPrevious.Text = "Zoom Previous";
            // 
            // menuItemZoomOut
            // 
            this.menuItemZoomOut.Index = 2;
            this.menuItemZoomOut.Text = "Zoom Out";
            // 
            // menuItem_MaxIntensities
            // 
            this.menuItem_MaxIntensities.Index = 3;
            this.menuItem_MaxIntensities.Text = "Show Max Intensities ONLY";
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 4;
            this.menuItem5.Text = "-";
            // 
            // menuItemConvertToMZ
            // 
            this.menuItemConvertToMZ.Index = 5;
            this.menuItemConvertToMZ.Text = "m/z";
            // 
            // menuItemConvertToTOF
            // 
            this.menuItemConvertToTOF.Index = 6;
            this.menuItemConvertToTOF.Text = "TOF";
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 7;
            this.menuItem3.Text = "-";
            // 
            // menuItem_Mobility
            // 
            this.menuItem_Mobility.Index = 8;
            this.menuItem_Mobility.Text = "Mobility";
            // 
            // menuItem_ScanTime
            // 
            this.menuItem_ScanTime.Index = 9;
            this.menuItem_ScanTime.Text = "Scan Time";
            // 
            // menuItem3a
            // 
            this.menuItem3a.Index = 10;
            this.menuItem3a.Text = "-";
            // 
            // menuItem_SelectionCorners
            // 
            this.menuItem_SelectionCorners.Index = 11;
            this.menuItem_SelectionCorners.Text = "Mask Plot Selection";
            // 
            // menuItem_Export
            // 
            this.menuItem_Export.Index = 12;
            this.menuItem_Export.Text = "Export Intensity Values";
            // 
            // menuItem_ExportAll
            // 
            this.menuItem_ExportAll.Index = 13;
            this.menuItem_ExportAll.Text = "Export All Frames Intensity Values";
            // 
            // menuItem_SuperFrame
            // 
            this.menuItem_SuperFrame.Index = 14;
            this.menuItem_SuperFrame.Text = "Export Superframe IMF file";
            // 
            // menuItem_SuperExperiment
            // 
            this.menuItem_SuperExperiment.Index = 15;
            this.menuItem_SuperExperiment.Text = "Create Merged IMF Frame Experiment...";
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 16;
            this.menuItem1.Text = "-";
            // 
            // menuItem_CopyToClipboard
            // 
            this.menuItem_CopyToClipboard.Index = 17;
            this.menuItem_CopyToClipboard.Text = "Copy Image to Clipboard";
            // 
            // menuItem_CaptureExperimentFrame
            // 
            this.menuItem_CaptureExperimentFrame.Index = 18;
            this.menuItem_CaptureExperimentFrame.Text = "Save Experiment GUI";
            // 
            // menuItem_SaveIMF
            // 
            this.menuItem_SaveIMF.Index = 19;
            this.menuItem_SaveIMF.Text = "Save Frame IMF";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(176, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(24, 20);
            this.label3.TabIndex = 35;
            this.label3.Text = "us";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_CursorScanTime
            // 
            this.lbl_CursorScanTime.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lbl_CursorScanTime.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CursorScanTime.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CursorScanTime.Location = new System.Drawing.Point(88, 32);
            this.lbl_CursorScanTime.Name = "lbl_CursorScanTime";
            this.lbl_CursorScanTime.Size = new System.Drawing.Size(88, 20);
            this.lbl_CursorScanTime.TabIndex = 34;
            this.lbl_CursorScanTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(8, 32);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 18);
            this.label5.TabIndex = 33;
            this.label5.Text = "Scan Time:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_CursorMZ
            // 
            this.lbl_CursorMZ.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lbl_CursorMZ.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CursorMZ.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CursorMZ.Location = new System.Drawing.Point(88, 88);
            this.lbl_CursorMZ.Name = "lbl_CursorMZ";
            this.lbl_CursorMZ.Size = new System.Drawing.Size(88, 20);
            this.lbl_CursorMZ.TabIndex = 32;
            this.lbl_CursorMZ.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_CursorTOF
            // 
            this.lbl_CursorTOF.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lbl_CursorTOF.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CursorTOF.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CursorTOF.Location = new System.Drawing.Point(88, 64);
            this.lbl_CursorTOF.Name = "lbl_CursorTOF";
            this.lbl_CursorTOF.Size = new System.Drawing.Size(88, 20);
            this.lbl_CursorTOF.TabIndex = 31;
            this.lbl_CursorTOF.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(8, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 18);
            this.label4.TabIndex = 27;
            this.label4.Text = "M/Z:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_TOForMZ
            // 
            this.lbl_TOForMZ.Location = new System.Drawing.Point(8, 64);
            this.lbl_TOForMZ.Name = "lbl_TOForMZ";
            this.lbl_TOForMZ.Size = new System.Drawing.Size(80, 18);
            this.lbl_TOForMZ.TabIndex = 3;
            this.lbl_TOForMZ.Text = "TOF:";
            this.lbl_TOForMZ.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_IonMobilityValue
            // 
            this.lbl_IonMobilityValue.Location = new System.Drawing.Point(8, 8);
            this.lbl_IonMobilityValue.Name = "lbl_IonMobilityValue";
            this.lbl_IonMobilityValue.Size = new System.Drawing.Size(80, 18);
            this.lbl_IonMobilityValue.TabIndex = 2;
            this.lbl_IonMobilityValue.Text = "Mobility:";
            this.lbl_IonMobilityValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(176, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(24, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "us";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_TimeOffset
            // 
            this.lbl_TimeOffset.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_TimeOffset.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_TimeOffset.Location = new System.Drawing.Point(16, 112);
            this.lbl_TimeOffset.Name = "lbl_TimeOffset";
            this.lbl_TimeOffset.Size = new System.Drawing.Size(168, 24);
            this.lbl_TimeOffset.TabIndex = 25;
            this.lbl_TimeOffset.Text = "Time Offset";
            this.lbl_TimeOffset.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_CursorMobility
            // 
            this.lbl_CursorMobility.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lbl_CursorMobility.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CursorMobility.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CursorMobility.Location = new System.Drawing.Point(88, 8);
            this.lbl_CursorMobility.Name = "lbl_CursorMobility";
            this.lbl_CursorMobility.Size = new System.Drawing.Size(88, 20);
            this.lbl_CursorMobility.TabIndex = 30;
            this.lbl_CursorMobility.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // contextMenu_HorizontalAxis
            // 
            this.contextMenu_HorizontalAxis.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_UseScans,
            this.menuItem_UseDriftTime,
            this.menuItem2});
            // 
            // menuItem_UseScans
            // 
            this.menuItem_UseScans.Index = 0;
            this.menuItem_UseScans.Text = "Scans";
            // 
            // menuItem_UseDriftTime
            // 
            this.menuItem_UseDriftTime.Index = 1;
            this.menuItem_UseDriftTime.Text = "Drift Time";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 2;
            this.menuItem2.Text = "-";
            // 
            // xyCursor1
            // 
            this.xyCursor1.HorizontalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            this.xyCursor1.Plot = this.waveformPlot3;
            this.xyCursor1.PointStyle = NationalInstruments.UI.PointStyle.Cross;
            this.xyCursor1.SnapMode = NationalInstruments.UI.CursorSnapMode.Fixed;
            this.xyCursor1.VerticalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            // 
            // waveformPlot3
            // 
            this.waveformPlot3.LineColor = System.Drawing.Color.Red;
            this.waveformPlot3.LineColorPrecedence = NationalInstruments.UI.ColorPrecedence.UserDefinedColor;
            this.waveformPlot3.XAxis = this.xAxis2;
            this.waveformPlot3.YAxis = this.yAxis2;
            // 
            // xAxis2
            // 
            this.xAxis2.Visible = false;
            // 
            // yAxis2
            // 
            this.yAxis2.Visible = false;
            // 
            // contextMenu_driftTIC
            // 
            this.contextMenu_driftTIC.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_3pointSmoothing,
            this.menuItem_5pointSmoothing,
            this.menuItem_ResetSmoothing,
            this.menuItem6,
            this.menuItem_Exportnew_driftTIC,
            this.menuItem9,
            this.menuItem8});
            // 
            // menuItem_3pointSmoothing
            // 
            this.menuItem_3pointSmoothing.Index = 0;
            this.menuItem_3pointSmoothing.Text = "";
            // 
            // menuItem_5pointSmoothing
            // 
            this.menuItem_5pointSmoothing.Index = 1;
            this.menuItem_5pointSmoothing.Text = "";
            // 
            // menuItem_ResetSmoothing
            // 
            this.menuItem_ResetSmoothing.Index = 2;
            this.menuItem_ResetSmoothing.Text = "";
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 3;
            this.menuItem6.Text = "-";
            // 
            // menuItem_Exportnew_driftTIC
            // 
            this.menuItem_Exportnew_driftTIC.Index = 4;
            this.menuItem_Exportnew_driftTIC.Text = "Export Data to File...";
            // 
            // menuItem9
            // 
            this.menuItem9.Index = 5;
            this.menuItem9.Text = "Copy Current TIC Image to Clipboard";
            // 
            // menuItem8
            // 
            this.menuItem8.Index = 6;
            this.menuItem8.Text = "Copy Full TIC Image to Clipboard";
            // 
            // plot_TOF
            // 
            this.plot_TOF.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_TOF.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.plot_TOF.Border = NationalInstruments.UI.Border.RaisedLite;
            this.plot_TOF.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.plot_TOF.InteractionMode = NationalInstruments.UI.GraphInteractionModes.None;
            this.plot_TOF.Location = new System.Drawing.Point(18, 52);
            this.plot_TOF.Name = "plot_TOF";
            this.plot_TOF.PlotAreaColor = System.Drawing.Color.White;
            this.plot_TOF.Plots.AddRange(new NationalInstruments.UI.WaveformPlot[] {
            this.waveform_TOFPlot});
            this.plot_TOF.SelectionColor = System.Drawing.Color.Lavender;
            this.plot_TOF.Size = new System.Drawing.Size(204, 440);
            this.plot_TOF.TabIndex = 20;
            this.plot_TOF.TabStop = false;
            this.plot_TOF.XAxes.AddRange(new NationalInstruments.UI.XAxis[] {
            this.xAxis_TOF});
            this.plot_TOF.YAxes.AddRange(new NationalInstruments.UI.YAxis[] {
            this.yAxis_TOF});
            // 
            // waveform_TOFPlot
            // 
            this.waveform_TOFPlot.LineColor = System.Drawing.Color.DarkBlue;
            this.waveform_TOFPlot.LineColorPrecedence = NationalInstruments.UI.ColorPrecedence.UserDefinedColor;
            this.waveform_TOFPlot.XAxis = this.xAxis_TOF;
            this.waveform_TOFPlot.YAxis = this.yAxis_TOF;
            // 
            // xAxis_TOF
            // 
            this.xAxis_TOF.Inverted = true;
            this.xAxis_TOF.MinorDivisions.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.xAxis_TOF.MinorDivisions.GridLineStyle = NationalInstruments.UI.LineStyle.Dot;
            this.xAxis_TOF.MinorDivisions.GridVisible = true;
            this.xAxis_TOF.MinorDivisions.TickVisible = true;
            this.xAxis_TOF.Position = NationalInstruments.UI.XAxisPosition.Top;
            // 
            // yAxis_TOF
            // 
            this.yAxis_TOF.Mode = NationalInstruments.UI.AxisMode.AutoScaleExact;
            // 
            // contextMenu_TOF
            // 
            this.contextMenu_TOF.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_TOFExport,
            this.menuItem_TOFMaximum});
            // 
            // menuItem_TOFExport
            // 
            this.menuItem_TOFExport.Index = 0;
            this.menuItem_TOFExport.Text = "Export Data to File...";
            // 
            // menuItem_TOFMaximum
            // 
            this.menuItem_TOFMaximum.Index = 1;
            this.menuItem_TOFMaximum.Text = "Show Maximum Intensities ONLY";
            // 
            // num_minMobility
            // 
            this.num_minMobility.DecimalPlaces = 2;
            this.num_minMobility.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_minMobility.Location = new System.Drawing.Point(246, 684);
            this.num_minMobility.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.num_minMobility.Name = "num_minMobility";
            this.num_minMobility.Size = new System.Drawing.Size(90, 21);
            this.num_minMobility.TabIndex = 25;
            this.num_minMobility.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // num_maxMobility
            // 
            this.num_maxMobility.DecimalPlaces = 2;
            this.num_maxMobility.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_maxMobility.Location = new System.Drawing.Point(666, 684);
            this.num_maxMobility.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.num_maxMobility.Name = "num_maxMobility";
            this.num_maxMobility.Size = new System.Drawing.Size(91, 21);
            this.num_maxMobility.TabIndex = 26;
            this.num_maxMobility.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // num_maxBin
            // 
            this.num_maxBin.DecimalPlaces = 4;
            this.num_maxBin.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_maxBin.Location = new System.Drawing.Point(17, 27);
            this.num_maxBin.Maximum = new decimal(new int[] {
            -1530494976,
            232830,
            0,
            0});
            this.num_maxBin.Name = "num_maxBin";
            this.num_maxBin.Size = new System.Drawing.Size(163, 21);
            this.num_maxBin.TabIndex = 28;
            this.num_maxBin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // num_minBin
            // 
            this.num_minBin.DecimalPlaces = 4;
            this.num_minBin.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_minBin.Location = new System.Drawing.Point(20, 504);
            this.num_minBin.Maximum = new decimal(new int[] {
            -1530494976,
            232830,
            0,
            0});
            this.num_minBin.Name = "num_minBin";
            this.num_minBin.Size = new System.Drawing.Size(158, 21);
            this.num_minBin.TabIndex = 29;
            this.num_minBin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // slide_Threshold
            // 
            this.slide_Threshold.Caption = "Threshold";
            this.slide_Threshold.CaptionBackColor = System.Drawing.Color.Transparent;
            this.slide_Threshold.CaptionFont = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slide_Threshold.CaptionVisible = false;
            this.slide_Threshold.FillBackColor = System.Drawing.Color.DimGray;
            this.slide_Threshold.FillColor = System.Drawing.Color.RoyalBlue;
            this.slide_Threshold.FillStyle = NationalInstruments.UI.FillStyle.VerticalGradient;
            this.slide_Threshold.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slide_Threshold.Location = new System.Drawing.Point(836, 80);
            this.slide_Threshold.Name = "slide_Threshold";
            this.slide_Threshold.Range = new NationalInstruments.UI.Range(1, 100000);
            this.slide_Threshold.ScalePosition = NationalInstruments.UI.NumericScalePosition.Right;
            this.slide_Threshold.ScaleType = NationalInstruments.UI.ScaleType.Logarithmic;
            this.slide_Threshold.Size = new System.Drawing.Size(64, 212);
            this.slide_Threshold.TabIndex = 36;
            this.slide_Threshold.Value = 1;
            // 
            // btn_Reset
            // 
            this.btn_Reset.BackColor = System.Drawing.Color.Gainsboro;
            this.btn_Reset.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Reset.Location = new System.Drawing.Point(840, 296);
            this.btn_Reset.Name = "btn_Reset";
            this.btn_Reset.Size = new System.Drawing.Size(44, 20);
            this.btn_Reset.TabIndex = 37;
            this.btn_Reset.Text = "Reset";
            this.btn_Reset.UseVisualStyleBackColor = false;
            // 
            // lbl_ExperimentDate
            // 
            this.lbl_ExperimentDate.BackColor = System.Drawing.Color.Transparent;
            this.lbl_ExperimentDate.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_ExperimentDate.ForeColor = System.Drawing.Color.Blue;
            this.lbl_ExperimentDate.Location = new System.Drawing.Point(80, 4);
            this.lbl_ExperimentDate.Name = "lbl_ExperimentDate";
            this.lbl_ExperimentDate.Size = new System.Drawing.Size(240, 16);
            this.lbl_ExperimentDate.TabIndex = 38;
            this.lbl_ExperimentDate.Text = "The date";
            // 
            // tabpages_FrameInfo
            // 
            this.tabpages_FrameInfo.Controls.Add(this.tabPage_Cursor);
            this.tabpages_FrameInfo.Controls.Add(this.tabPage_Calibration);
            this.tabpages_FrameInfo.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabpages_FrameInfo.Location = new System.Drawing.Point(12, 624);
            this.tabpages_FrameInfo.Name = "tabpages_FrameInfo";
            this.tabpages_FrameInfo.SelectedIndex = 0;
            this.tabpages_FrameInfo.Size = new System.Drawing.Size(208, 164);
            this.tabpages_FrameInfo.TabIndex = 42;
            // 
            // tabPage_Cursor
            // 
            this.tabPage_Cursor.BackColor = System.Drawing.Color.Gainsboro;
            this.tabPage_Cursor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tabPage_Cursor.Controls.Add(this.label3);
            this.tabPage_Cursor.Controls.Add(this.lbl_CursorScanTime);
            this.tabPage_Cursor.Controls.Add(this.label5);
            this.tabPage_Cursor.Controls.Add(this.lbl_CursorMZ);
            this.tabPage_Cursor.Controls.Add(this.lbl_CursorTOF);
            this.tabPage_Cursor.Controls.Add(this.label4);
            this.tabPage_Cursor.Controls.Add(this.lbl_TOForMZ);
            this.tabPage_Cursor.Controls.Add(this.lbl_IonMobilityValue);
            this.tabPage_Cursor.Controls.Add(this.label2);
            this.tabPage_Cursor.Controls.Add(this.lbl_TimeOffset);
            this.tabPage_Cursor.Controls.Add(this.lbl_CursorMobility);
            this.tabPage_Cursor.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Cursor.Name = "tabPage_Cursor";
            this.tabPage_Cursor.Size = new System.Drawing.Size(200, 138);
            this.tabPage_Cursor.TabIndex = 0;
            this.tabPage_Cursor.Text = " Cursor  ";
            // 
            // tabPage_Calibration
            // 
            this.tabPage_Calibration.BackColor = System.Drawing.Color.Gainsboro;
            this.tabPage_Calibration.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tabPage_Calibration.Controls.Add(this.tb_CalT0);
            this.tabPage_Calibration.Controls.Add(this.tb_CalA);
            this.tabPage_Calibration.Controls.Add(this.btn_setCalDefaults);
            this.tabPage_Calibration.Controls.Add(this.label9);
            this.tabPage_Calibration.Controls.Add(this.label8);
            this.tabPage_Calibration.Controls.Add(this.label7);
            this.tabPage_Calibration.Controls.Add(this.date_Calibration);
            this.tabPage_Calibration.Controls.Add(this.lbl_CalibratorType);
            this.tabPage_Calibration.Controls.Add(this.btn_revertCalDefaults);
            this.tabPage_Calibration.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Calibration.Name = "tabPage_Calibration";
            this.tabPage_Calibration.Size = new System.Drawing.Size(200, 138);
            this.tabPage_Calibration.TabIndex = 1;
            this.tabPage_Calibration.Text = " Calibration  ";
            this.tabPage_Calibration.Visible = false;
            // 
            // tb_CalT0
            // 
            this.tb_CalT0.BackColor = System.Drawing.Color.Black;
            this.tb_CalT0.ForeColor = System.Drawing.Color.White;
            this.tb_CalT0.Location = new System.Drawing.Point(42, 72);
            this.tb_CalT0.Name = "tb_CalT0";
            this.tb_CalT0.Size = new System.Drawing.Size(142, 21);
            this.tb_CalT0.TabIndex = 54;
            // 
            // tb_CalA
            // 
            this.tb_CalA.BackColor = System.Drawing.Color.Black;
            this.tb_CalA.ForeColor = System.Drawing.Color.White;
            this.tb_CalA.Location = new System.Drawing.Point(42, 52);
            this.tb_CalA.Name = "tb_CalA";
            this.tb_CalA.Size = new System.Drawing.Size(142, 21);
            this.tb_CalA.TabIndex = 51;
            // 
            // btn_setCalDefaults
            // 
            this.btn_setCalDefaults.BackColor = System.Drawing.Color.Gold;
            this.btn_setCalDefaults.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_setCalDefaults.ForeColor = System.Drawing.Color.Black;
            this.btn_setCalDefaults.Location = new System.Drawing.Point(108, 100);
            this.btn_setCalDefaults.Name = "btn_setCalDefaults";
            this.btn_setCalDefaults.Size = new System.Drawing.Size(80, 32);
            this.btn_setCalDefaults.TabIndex = 54;
            this.btn_setCalDefaults.Text = "Set as Default";
            this.btn_setCalDefaults.UseVisualStyleBackColor = false;
            // 
            // label9
            // 
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(20, 72);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(20, 20);
            this.label9.TabIndex = 51;
            this.label9.Text = "t0";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(20, 52);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(16, 20);
            this.label8.TabIndex = 50;
            this.label8.Text = "a";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(16, 4);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 24);
            this.label7.TabIndex = 49;
            this.label7.Text = "Date";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // date_Calibration
            // 
            this.date_Calibration.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.date_Calibration.Location = new System.Drawing.Point(68, 4);
            this.date_Calibration.Name = "date_Calibration";
            this.date_Calibration.Size = new System.Drawing.Size(112, 21);
            this.date_Calibration.TabIndex = 47;
            // 
            // lbl_CalibratorType
            // 
            this.lbl_CalibratorType.Location = new System.Drawing.Point(8, 32);
            this.lbl_CalibratorType.Name = "lbl_CalibratorType";
            this.lbl_CalibratorType.Size = new System.Drawing.Size(180, 16);
            this.lbl_CalibratorType.TabIndex = 43;
            this.lbl_CalibratorType.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_revertCalDefaults
            // 
            this.btn_revertCalDefaults.BackColor = System.Drawing.Color.DodgerBlue;
            this.btn_revertCalDefaults.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_revertCalDefaults.ForeColor = System.Drawing.Color.White;
            this.btn_revertCalDefaults.Location = new System.Drawing.Point(8, 100);
            this.btn_revertCalDefaults.Name = "btn_revertCalDefaults";
            this.btn_revertCalDefaults.Size = new System.Drawing.Size(80, 32);
            this.btn_revertCalDefaults.TabIndex = 55;
            this.btn_revertCalDefaults.Text = "Revert to Defaults";
            this.btn_revertCalDefaults.UseVisualStyleBackColor = false;
            // 
            // hsb_2DMap
            // 
            this.hsb_2DMap.Location = new System.Drawing.Point(244, 64);
            this.hsb_2DMap.Name = "hsb_2DMap";
            this.hsb_2DMap.Size = new System.Drawing.Size(500, 12);
            this.hsb_2DMap.TabIndex = 48;
            // 
            // vsb_2DMap
            // 
            this.vsb_2DMap.Location = new System.Drawing.Point(744, 76);
            this.vsb_2DMap.Name = "vsb_2DMap";
            this.vsb_2DMap.Size = new System.Drawing.Size(12, 492);
            this.vsb_2DMap.TabIndex = 49;
            // 
            // slide_FrameSelect
            // 
            this.slide_FrameSelect.AutoDivisionSpacing = false;
            this.slide_FrameSelect.EditRangeNumericFormatMode = NationalInstruments.UI.NumericFormatMode.CreateGenericMode("F0");
            this.slide_FrameSelect.FillBackColor = System.Drawing.Color.DarkGray;
            this.slide_FrameSelect.FillBaseValue = 3;
            this.slide_FrameSelect.FillColor = System.Drawing.Color.GhostWhite;
            this.slide_FrameSelect.FillMode = NationalInstruments.UI.NumericFillMode.ToBaseValue;
            this.slide_FrameSelect.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slide_FrameSelect.InteractionMode = ((NationalInstruments.UI.LinearNumericPointerInteractionModes)(((NationalInstruments.UI.LinearNumericPointerInteractionModes.DragPointer | NationalInstruments.UI.LinearNumericPointerInteractionModes.SnapPointer)
                        | NationalInstruments.UI.LinearNumericPointerInteractionModes.EditRange)));
            this.slide_FrameSelect.Location = new System.Drawing.Point(484, 0);
            this.slide_FrameSelect.MajorDivisions.Interval = 1;
            this.slide_FrameSelect.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F0");
            this.slide_FrameSelect.MinorDivisions.Interval = 5;
            this.slide_FrameSelect.MinorDivisions.TickVisible = false;
            this.slide_FrameSelect.Name = "slide_FrameSelect";
            this.slide_FrameSelect.Range = new NationalInstruments.UI.Range(0, 5);
            this.slide_FrameSelect.ScalePosition = NationalInstruments.UI.NumericScalePosition.Top;
            this.slide_FrameSelect.Size = new System.Drawing.Size(276, 40);
            this.slide_FrameSelect.TabIndex = 50;
            this.slide_FrameSelect.ThermometerStyle = NationalInstruments.UI.ThermometerStyle.Raised3D;
            this.slide_FrameSelect.ToolTipFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F0");
            this.slide_FrameSelect.Value = 4;
            // 
            // lbl_FrameRange
            // 
            this.lbl_FrameRange.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_FrameRange.Location = new System.Drawing.Point(592, 40);
            this.lbl_FrameRange.Name = "lbl_FrameRange";
            this.lbl_FrameRange.Size = new System.Drawing.Size(97, 20);
            this.lbl_FrameRange.TabIndex = 52;
            this.lbl_FrameRange.Text = "Frame Range:";
            this.lbl_FrameRange.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // num_FrameRange
            // 
            this.num_FrameRange.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_FrameRange.Location = new System.Drawing.Point(692, 36);
            this.num_FrameRange.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.num_FrameRange.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.num_FrameRange.Name = "num_FrameRange";
            this.num_FrameRange.Size = new System.Drawing.Size(56, 21);
            this.num_FrameRange.TabIndex = 51;
            this.num_FrameRange.TabStop = false;
            this.num_FrameRange.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.num_FrameRange.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbl_FramesShown
            // 
            this.lbl_FramesShown.AutoSize = true;
            this.lbl_FramesShown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.lbl_FramesShown.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_FramesShown.Location = new System.Drawing.Point(236, 44);
            this.lbl_FramesShown.Name = "lbl_FramesShown";
            this.lbl_FramesShown.Size = new System.Drawing.Size(86, 14);
            this.lbl_FramesShown.TabIndex = 53;
            this.lbl_FramesShown.Text = "showing frames";
            // 
            // btn_Refresh
            // 
            this.btn_Refresh.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btn_Refresh.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Refresh.Location = new System.Drawing.Point(8, 4);
            this.btn_Refresh.Name = "btn_Refresh";
            this.btn_Refresh.Size = new System.Drawing.Size(56, 23);
            this.btn_Refresh.TabIndex = 64;
            this.btn_Refresh.Text = "Refresh";
            this.btn_Refresh.UseVisualStyleBackColor = false;
            this.btn_Refresh.Click += new System.EventHandler(this.btn_Refresh_Click);
            // 
            // num_TICThreshold
            // 
            this.num_TICThreshold.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_TICThreshold.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.num_TICThreshold.Location = new System.Drawing.Point(364, 44);
            this.num_TICThreshold.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.num_TICThreshold.Name = "num_TICThreshold";
            this.num_TICThreshold.Size = new System.Drawing.Size(68, 20);
            this.num_TICThreshold.TabIndex = 65;
            this.num_TICThreshold.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // btn_TIC
            // 
            this.btn_TIC.BackColor = System.Drawing.Color.Salmon;
            this.btn_TIC.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_TIC.Location = new System.Drawing.Point(436, 44);
            this.btn_TIC.Name = "btn_TIC";
            this.btn_TIC.Size = new System.Drawing.Size(32, 20);
            this.btn_TIC.TabIndex = 66;
            this.btn_TIC.Text = "OK";
            this.btn_TIC.UseVisualStyleBackColor = false;
            // 
            // cb_MaxScanValue
            // 
            this.cb_MaxScanValue.AutoSize = true;
            this.cb_MaxScanValue.BackColor = System.Drawing.Color.Gainsboro;
            this.cb_MaxScanValue.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cb_MaxScanValue.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_MaxScanValue.Location = new System.Drawing.Point(648, 664);
            this.cb_MaxScanValue.Name = "cb_MaxScanValue";
            this.cb_MaxScanValue.Size = new System.Drawing.Size(141, 18);
            this.cb_MaxScanValue.TabIndex = 67;
            this.cb_MaxScanValue.TabStop = false;
            this.cb_MaxScanValue.Text = "Max Scan Value ONLY";
            this.cb_MaxScanValue.UseVisualStyleBackColor = false;
            // 
            // num_FrameCompression
            // 
            this.num_FrameCompression.Location = new System.Drawing.Point(148, 40);
            this.num_FrameCompression.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.num_FrameCompression.Name = "num_FrameCompression";
            this.num_FrameCompression.Size = new System.Drawing.Size(52, 20);
            this.num_FrameCompression.TabIndex = 73;
            this.num_FrameCompression.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_FrameCompression.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lbl_FrameCompression
            // 
            this.lbl_FrameCompression.ForeColor = System.Drawing.Color.Black;
            this.lbl_FrameCompression.Location = new System.Drawing.Point(12, 40);
            this.lbl_FrameCompression.Name = "lbl_FrameCompression";
            this.lbl_FrameCompression.Size = new System.Drawing.Size(132, 20);
            this.lbl_FrameCompression.TabIndex = 74;
            this.lbl_FrameCompression.Text = "Frame Compression:";
            this.lbl_FrameCompression.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rb_CompleteChromatogram
            // 
            this.rb_CompleteChromatogram.AutoSize = true;
            this.rb_CompleteChromatogram.ForeColor = System.Drawing.Color.Yellow;
            this.rb_CompleteChromatogram.Location = new System.Drawing.Point(8, 16);
            this.rb_CompleteChromatogram.Name = "rb_CompleteChromatogram";
            this.rb_CompleteChromatogram.Size = new System.Drawing.Size(196, 18);
            this.rb_CompleteChromatogram.TabIndex = 78;
            this.rb_CompleteChromatogram.Text = "Complete Peak Chromatogram";
            this.rb_CompleteChromatogram.UseVisualStyleBackColor = true;
            // 
            // rb_PartialChromatogram
            // 
            this.rb_PartialChromatogram.AutoSize = true;
            this.rb_PartialChromatogram.ForeColor = System.Drawing.Color.Yellow;
            this.rb_PartialChromatogram.Location = new System.Drawing.Point(8, 0);
            this.rb_PartialChromatogram.Name = "rb_PartialChromatogram";
            this.rb_PartialChromatogram.Size = new System.Drawing.Size(176, 18);
            this.rb_PartialChromatogram.TabIndex = 77;
            this.rb_PartialChromatogram.Text = "Partial Peak Chromatogram";
            this.rb_PartialChromatogram.UseVisualStyleBackColor = true;
            // 
            // pnl_Chromatogram
            // 
            this.pnl_Chromatogram.BackColor = System.Drawing.Color.DarkGray;
            this.pnl_Chromatogram.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_Chromatogram.Controls.Add(this.rb_CompleteChromatogram);
            this.pnl_Chromatogram.Controls.Add(this.rb_PartialChromatogram);
            this.pnl_Chromatogram.Controls.Add(this.num_FrameCompression);
            this.pnl_Chromatogram.Controls.Add(this.lbl_FrameCompression);
            this.pnl_Chromatogram.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pnl_Chromatogram.Location = new System.Drawing.Point(16, 532);
            this.pnl_Chromatogram.Name = "pnl_Chromatogram";
            this.pnl_Chromatogram.Size = new System.Drawing.Size(208, 68);
            this.pnl_Chromatogram.TabIndex = 79;
            // 
            // tabpages_Main
            // 
            this.tabpages_Main.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.tabpages_Main.Controls.Add(this.tab_DataViewer);
            this.tabpages_Main.Controls.Add(this.tab_InstrumentSettings);
            this.tabpages_Main.Controls.Add(this.tab_PostProcessing);
            this.tabpages_Main.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabpages_Main.Font = new System.Drawing.Font("Comic Sans MS", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabpages_Main.Location = new System.Drawing.Point(0, 0);
            this.tabpages_Main.Multiline = true;
            this.tabpages_Main.Name = "tabpages_Main";
            this.tabpages_Main.SelectedIndex = 0;
            this.tabpages_Main.Size = new System.Drawing.Size(1016, 832);
            this.tabpages_Main.TabIndex = 81;
            // 
            // tab_DataViewer
            // 
            this.tab_DataViewer.BackColor = System.Drawing.Color.Silver;
            this.tab_DataViewer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tab_DataViewer.Controls.Add(this.cb_FrameType);
            this.tab_DataViewer.Controls.Add(this.gb_MZRange);
            this.tab_DataViewer.Controls.Add(this.cb_EnableMZRange);
            this.tab_DataViewer.Controls.Add(this.num_FrameIndex);
            this.tab_DataViewer.Controls.Add(this.btn_Refresh);
            this.tab_DataViewer.Controls.Add(this.pb_PlayRightOut);
            this.tab_DataViewer.Controls.Add(this.pnl_Chromatogram);
            this.tab_DataViewer.Controls.Add(this.pb_PlayLeftIn);
            this.tab_DataViewer.Controls.Add(this.cb_MaxScanValue);
            this.tab_DataViewer.Controls.Add(this.pb_2DMap);
            this.tab_DataViewer.Controls.Add(this.btn_TIC);
            this.tab_DataViewer.Controls.Add(this.pb_SliderBackground);
            this.tab_DataViewer.Controls.Add(this.num_TICThreshold);
            this.tab_DataViewer.Controls.Add(this.num_minMobility);
            this.tab_DataViewer.Controls.Add(this.num_maxMobility);
            this.tab_DataViewer.Controls.Add(this.num_maxBin);
            this.tab_DataViewer.Controls.Add(this.num_minBin);
            this.tab_DataViewer.Controls.Add(this.lbl_Chromatogram);
            this.tab_DataViewer.Controls.Add(this.plot_Mobility);
            this.tab_DataViewer.Controls.Add(this.plot_TOF);
            this.tab_DataViewer.Controls.Add(this.lbl_FramesShown);
            this.tab_DataViewer.Controls.Add(this.slide_Threshold);
            this.tab_DataViewer.Controls.Add(this.lbl_FrameRange);
            this.tab_DataViewer.Controls.Add(this.btn_Reset);
            this.tab_DataViewer.Controls.Add(this.num_FrameRange);
            this.tab_DataViewer.Controls.Add(this.lbl_ExperimentDate);
            this.tab_DataViewer.Controls.Add(this.slide_FrameSelect);
            this.tab_DataViewer.Controls.Add(this.tabpages_FrameInfo);
            this.tab_DataViewer.Controls.Add(this.vsb_2DMap);
            this.tab_DataViewer.Controls.Add(this.pb_PlayLeftOut);
            this.tab_DataViewer.Controls.Add(this.hsb_2DMap);
            this.tab_DataViewer.Controls.Add(this.pb_PlayRightIn);
            this.tab_DataViewer.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_DataViewer.Location = new System.Drawing.Point(31, 4);
            this.tab_DataViewer.Name = "tab_DataViewer";
            this.tab_DataViewer.Padding = new System.Windows.Forms.Padding(3);
            this.tab_DataViewer.Size = new System.Drawing.Size(981, 824);
            this.tab_DataViewer.TabIndex = 0;
            this.tab_DataViewer.Text = "   Data Viewer    ";
            this.tab_DataViewer.UseVisualStyleBackColor = true;
            // 
            // cb_FrameType
            // 
            this.cb_FrameType.BackColor = System.Drawing.Color.Gainsboro;
            this.cb_FrameType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_FrameType.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_FrameType.FormattingEnabled = true;
            this.cb_FrameType.Location = new System.Drawing.Point(276, 16);
            this.cb_FrameType.Name = "cb_FrameType";
            this.cb_FrameType.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cb_FrameType.Size = new System.Drawing.Size(128, 27);
            this.cb_FrameType.TabIndex = 89;
            this.cb_FrameType.TabStop = false;
            // 
            // gb_MZRange
            // 
            this.gb_MZRange.BackColor = System.Drawing.Color.Transparent;
            this.gb_MZRange.Controls.Add(this.lbl_PPM);
            this.gb_MZRange.Controls.Add(this.lbl_MZ);
            this.gb_MZRange.Controls.Add(this.num_PPM);
            this.gb_MZRange.Controls.Add(this.label1);
            this.gb_MZRange.Controls.Add(this.num_MZ);
            this.gb_MZRange.Location = new System.Drawing.Point(748, 736);
            this.gb_MZRange.Name = "gb_MZRange";
            this.gb_MZRange.Size = new System.Drawing.Size(220, 76);
            this.gb_MZRange.TabIndex = 88;
            this.gb_MZRange.TabStop = false;
            // 
            // lbl_PPM
            // 
            this.lbl_PPM.BackColor = System.Drawing.Color.Transparent;
            this.lbl_PPM.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_PPM.Location = new System.Drawing.Point(8, 48);
            this.lbl_PPM.Name = "lbl_PPM";
            this.lbl_PPM.Size = new System.Drawing.Size(56, 20);
            this.lbl_PPM.TabIndex = 81;
            this.lbl_PPM.Text = "Range:";
            this.lbl_PPM.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_MZ
            // 
            this.lbl_MZ.BackColor = System.Drawing.Color.Transparent;
            this.lbl_MZ.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_MZ.Location = new System.Drawing.Point(8, 24);
            this.lbl_MZ.Name = "lbl_MZ";
            this.lbl_MZ.Size = new System.Drawing.Size(56, 20);
            this.lbl_MZ.TabIndex = 80;
            this.lbl_MZ.Text = "M/Z:";
            this.lbl_MZ.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // num_PPM
            // 
            this.num_PPM.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_PPM.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.num_PPM.Location = new System.Drawing.Point(64, 48);
            this.num_PPM.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.num_PPM.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.num_PPM.Name = "num_PPM";
            this.num_PPM.Size = new System.Drawing.Size(112, 22);
            this.num_PPM.TabIndex = 84;
            this.num_PPM.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_PPM.Value = new decimal(new int[] {
            150,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(176, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 20);
            this.label1.TabIndex = 82;
            this.label1.Text = "PPM";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // num_MZ
            // 
            this.num_MZ.DecimalPlaces = 4;
            this.num_MZ.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_MZ.Increment = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            this.num_MZ.Location = new System.Drawing.Point(64, 24);
            this.num_MZ.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.num_MZ.Name = "num_MZ";
            this.num_MZ.Size = new System.Drawing.Size(112, 22);
            this.num_MZ.TabIndex = 83;
            this.num_MZ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_MZ.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // cb_EnableMZRange
            // 
            this.cb_EnableMZRange.AutoSize = true;
            this.cb_EnableMZRange.BackColor = System.Drawing.Color.Silver;
            this.cb_EnableMZRange.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_EnableMZRange.Location = new System.Drawing.Point(820, 716);
            this.cb_EnableMZRange.Name = "cb_EnableMZRange";
            this.cb_EnableMZRange.Size = new System.Drawing.Size(138, 18);
            this.cb_EnableMZRange.TabIndex = 87;
            this.cb_EnableMZRange.Text = "Enable MZ Range";
            this.cb_EnableMZRange.UseVisualStyleBackColor = false;
            // 
            // num_FrameIndex
            // 
            this.num_FrameIndex.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_FrameIndex.Location = new System.Drawing.Point(408, 16);
            this.num_FrameIndex.Name = "num_FrameIndex";
            this.num_FrameIndex.Size = new System.Drawing.Size(68, 26);
            this.num_FrameIndex.TabIndex = 82;
            this.num_FrameIndex.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // pb_PlayRightOut
            // 
            this.pb_PlayRightOut.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.triangle_right_out;
            this.pb_PlayRightOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_PlayRightOut.Location = new System.Drawing.Point(900, 32);
            this.pb_PlayRightOut.Name = "pb_PlayRightOut";
            this.pb_PlayRightOut.Size = new System.Drawing.Size(24, 16);
            this.pb_PlayRightOut.TabIndex = 44;
            this.pb_PlayRightOut.TabStop = false;
            // 
            // pb_PlayLeftIn
            // 
            this.pb_PlayLeftIn.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.triangle_left_in;
            this.pb_PlayLeftIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_PlayLeftIn.Location = new System.Drawing.Point(848, 32);
            this.pb_PlayLeftIn.Name = "pb_PlayLeftIn";
            this.pb_PlayLeftIn.Size = new System.Drawing.Size(24, 16);
            this.pb_PlayLeftIn.TabIndex = 46;
            this.pb_PlayLeftIn.TabStop = false;
            // 
            // pb_2DMap
            // 
            this.pb_2DMap.BackColor = System.Drawing.Color.Black;
            this.pb_2DMap.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pb_2DMap.Location = new System.Drawing.Point(244, 76);
            this.pb_2DMap.Name = "pb_2DMap";
            this.pb_2DMap.Size = new System.Drawing.Size(500, 500);
            this.pb_2DMap.TabIndex = 2;
            this.pb_2DMap.TabStop = false;
            // 
            // pb_SliderBackground
            // 
            this.pb_SliderBackground.Image = ((System.Drawing.Image)(resources.GetObject("pb_SliderBackground.Image")));
            this.pb_SliderBackground.Location = new System.Drawing.Point(788, 20);
            this.pb_SliderBackground.Name = "pb_SliderBackground";
            this.pb_SliderBackground.Size = new System.Drawing.Size(11, 694);
            this.pb_SliderBackground.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pb_SliderBackground.TabIndex = 22;
            this.pb_SliderBackground.TabStop = false;
            // 
            // lbl_Chromatogram
            // 
            this.lbl_Chromatogram.AutoSize = true;
            this.lbl_Chromatogram.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Chromatogram.Location = new System.Drawing.Point(320, 16);
            this.lbl_Chromatogram.Name = "lbl_Chromatogram";
            this.lbl_Chromatogram.Size = new System.Drawing.Size(85, 23);
            this.lbl_Chromatogram.TabIndex = 33;
            this.lbl_Chromatogram.Text = "Frame:";
            this.lbl_Chromatogram.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // plot_Mobility
            // 
            this.plot_Mobility.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_Mobility.BackgroundImageAlignment = NationalInstruments.UI.ImageAlignment.Center;
            this.plot_Mobility.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.plot_Mobility.Border = NationalInstruments.UI.Border.RaisedLite;
            this.plot_Mobility.Cursors.AddRange(new NationalInstruments.UI.XYCursor[] {
            this.xyCursor2});
            this.plot_Mobility.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.plot_Mobility.InteractionMode = NationalInstruments.UI.GraphInteractionModes.None;
            this.plot_Mobility.Location = new System.Drawing.Point(246, 572);
            this.plot_Mobility.Name = "plot_Mobility";
            this.plot_Mobility.PlotAreaColor = System.Drawing.Color.White;
            this.plot_Mobility.Plots.AddRange(new NationalInstruments.UI.WaveformPlot[] {
            this.waveform_MobilityPlot});
            this.plot_Mobility.Size = new System.Drawing.Size(510, 111);
            this.plot_Mobility.TabIndex = 24;
            this.plot_Mobility.XAxes.AddRange(new NationalInstruments.UI.XAxis[] {
            this.xAxis_Mobility});
            this.plot_Mobility.YAxes.AddRange(new NationalInstruments.UI.YAxis[] {
            this.yAxis_Mobility});
            // 
            // xyCursor2
            // 
            this.xyCursor2.HorizontalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            this.xyCursor2.Plot = this.waveform_MobilityPlot;
            this.xyCursor2.PointStyle = NationalInstruments.UI.PointStyle.Plus;
            this.xyCursor2.VerticalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            // 
            // waveform_MobilityPlot
            // 
            this.waveform_MobilityPlot.LineColor = System.Drawing.Color.Crimson;
            this.waveform_MobilityPlot.LineColorPrecedence = NationalInstruments.UI.ColorPrecedence.UserDefinedColor;
            this.waveform_MobilityPlot.PointColor = System.Drawing.Color.Salmon;
            this.waveform_MobilityPlot.XAxis = this.xAxis_Mobility;
            this.waveform_MobilityPlot.YAxis = this.yAxis_Mobility;
            // 
            // xAxis_Mobility
            // 
            this.xAxis_Mobility.InteractionMode = NationalInstruments.UI.ScaleInteractionMode.None;
            this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F2");
            this.xAxis_Mobility.Mode = NationalInstruments.UI.AxisMode.AutoScaleExact;
            // 
            // pb_PlayLeftOut
            // 
            this.pb_PlayLeftOut.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.triangle_left_out;
            this.pb_PlayLeftOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_PlayLeftOut.Location = new System.Drawing.Point(876, 32);
            this.pb_PlayLeftOut.Name = "pb_PlayLeftOut";
            this.pb_PlayLeftOut.Size = new System.Drawing.Size(24, 16);
            this.pb_PlayLeftOut.TabIndex = 47;
            this.pb_PlayLeftOut.TabStop = false;
            // 
            // pb_PlayRightIn
            // 
            this.pb_PlayRightIn.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.triangle_right_in;
            this.pb_PlayRightIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_PlayRightIn.Location = new System.Drawing.Point(928, 32);
            this.pb_PlayRightIn.Name = "pb_PlayRightIn";
            this.pb_PlayRightIn.Size = new System.Drawing.Size(24, 16);
            this.pb_PlayRightIn.TabIndex = 45;
            this.pb_PlayRightIn.TabStop = false;
            // 
            // tab_PostProcessing
            // 
            this.tab_PostProcessing.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_PostProcessing.Location = new System.Drawing.Point(31, 4);
            this.tab_PostProcessing.Name = "tab_PostProcessing";
            this.tab_PostProcessing.Padding = new System.Windows.Forms.Padding(3);
            this.tab_PostProcessing.Size = new System.Drawing.Size(981, 824);
            this.tab_PostProcessing.TabIndex = 2;
            this.tab_PostProcessing.Text = "   Post Processing    ";
            this.tab_PostProcessing.UseVisualStyleBackColor = true;
            // 
            // tab_InstrumentSettings
            // 
            this.tab_InstrumentSettings.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_InstrumentSettings.Location = new System.Drawing.Point(31, 4);
            this.tab_InstrumentSettings.Name = "tab_InstrumentSettings";
            this.tab_InstrumentSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab_InstrumentSettings.Size = new System.Drawing.Size(981, 824);
            this.tab_InstrumentSettings.TabIndex = 1;
            this.tab_InstrumentSettings.Text = "   Instrument Settings    ";
            this.tab_InstrumentSettings.UseVisualStyleBackColor = true;
            // 
            // DataViewer
            // 
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(1040, 834);
            this.Controls.Add(this.tabpages_Main);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.Location = new System.Drawing.Point(30, 30);
            this.MinimumSize = new System.Drawing.Size(700, 600);
            this.Name = "DataViewer";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Experiment name";
            ((System.ComponentModel.ISupportInitialize)(this.xyCursor1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.plot_TOF)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minMobility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxMobility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxBin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minBin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.slide_Threshold)).EndInit();
            this.tabpages_FrameInfo.ResumeLayout(false);
            this.tabPage_Cursor.ResumeLayout(false);
            this.tabPage_Calibration.ResumeLayout(false);
            this.tabPage_Calibration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.slide_FrameSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameRange)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_TICThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameCompression)).EndInit();
            this.pnl_Chromatogram.ResumeLayout(false);
            this.pnl_Chromatogram.PerformLayout();
            this.tabpages_Main.ResumeLayout(false);
            this.tab_DataViewer.ResumeLayout(false);
            this.tab_DataViewer.PerformLayout();
            this.gb_MZRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.num_PPM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayRightOut)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayLeftIn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_2DMap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_SliderBackground)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.plot_Mobility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xyCursor2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayLeftOut)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayRightIn)).EndInit();
            this.ResumeLayout(false);

        }
#endregion

        // /////////////////////////////////////////////////////////////////////////////////////////////
        // resize start at the left side, top to bottom
        //
        bool flag_Resizing = false;
        public void IonMobilityDataView_Resize(object obj, System.EventArgs e)
        {
            try
            {
                this.tabpages_Main.Top = 0;

                //this.flag_Resizing = true;
                this.tabpages_Main.Width = this.Width - 6;
                this.tabpages_Main.Height = this.Height - 32;
                this.flag_Resizing = false;

                this.tabpages_Main_Resize((object)null, (System.EventArgs)null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("resize error: " + ex.ToString());
            }
        }

        protected virtual void tabpages_Main_Resize(object obj, System.EventArgs e)
        {
            if (this.flag_Resizing)
                return;
            this.flag_Resizing = true;

            this.pnl_postProcessing.Width = this.tab_PostProcessing.Width + 50;
            this.pnl_postProcessing.Height = this.tab_PostProcessing.Height + 50;

            // Start at the top!
            //
            // Far left column
            this.btn_Refresh.Top = 4;
            this.btn_Refresh.Left = 4;

            this.lbl_ExperimentDate.Top = 4;
            this.lbl_ExperimentDate.Left = this.btn_Refresh.Left + this.btn_Refresh.Width + 10; // this.pb_2DMap.Left + this.pb_2DMap.Width - this.lbl_ExperimentDate.Width;

            this.num_maxBin.Top = this.btn_Refresh.Top + this.btn_Refresh.Height + 6;

            this.tabpages_FrameInfo.Top = this.tab_DataViewer.Height - this.tabpages_FrameInfo.Height - 6;
            this.pnl_Chromatogram.Top = this.tabpages_FrameInfo.Top - this.pnl_Chromatogram.Height - 6;
            //   this.btn_ShowChromatogram.Top = this.tabpages_FrameInfo.Top - this.cb_Chromatogram.Height - 48;
            //   this.cb_Chromatogram.Top = this.btn_ShowChromatogram.Top + 4;

            // this.num_minBin.Top = this.cb_Chromatogram.Top - this.num_minBin.Height - 10;

            this.num_minBin.Left = this.num_maxBin.Left = 20;
            this.plot_TOF.Left = 20;

            this.tabpages_FrameInfo.Left = 5;
            this.pnl_Chromatogram.Left = 5;

            // max_plot_height ************************************************
            this.max_plot_height = this.tab_DataViewer.Height - 370;

            // Right 
            this.slider_PlotBackground.Height = (this.max_plot_height / 3) + 5;
            this.slider_PlotBackground.Top = this.num_FrameRange.Top + this.num_FrameRange.Height + 10;

            this.slide_Threshold.Height = this.max_plot_height - this.btn_Reset.Height - this.slider_PlotBackground.Height;
            this.slide_Threshold.Top = this.slider_PlotBackground.Top + this.slider_PlotBackground.Height;

            this.btn_Reset.Top = this.slide_Threshold.Top + this.slide_Threshold.Height;

            this.slider_ColorMap.Height = (this.slide_Threshold.Top + this.slide_Threshold.Height) - this.slider_PlotBackground.Top;
            this.slider_ColorMap.Top = this.slider_PlotBackground.Top;

            this.slide_Threshold.Left = this.tab_DataViewer.Width - this.slide_Threshold.Width - 10;
            this.slider_PlotBackground.Left = this.slide_Threshold.Left;
            this.slider_ColorMap.Left = this.slide_Threshold.Left - this.slider_ColorMap.Width - 10;
            this.btn_Reset.Left = this.slider_ColorMap.Left + 12;

            // Middle Top
            this.slide_FrameSelect.Top = 2;

            this.cb_FrameType.Top = this.lbl_Chromatogram.Top = this.pb_PlayLeftIn.Top - 4;
            this.num_FrameIndex.Top = this.lbl_Chromatogram.Top;
            this.lbl_Chromatogram.Left = this.plot_TOF.Left + this.plot_TOF.Width + 30;
            this.cb_FrameType.Left = this.lbl_Chromatogram.Left - 30;
            this.num_FrameIndex.Left = this.cb_FrameType.Left + this.cb_FrameType.Width + 4; // this.lbl_Frame.Width + 10;

            this.pb_PlayLeftIn.Top = this.pb_PlayLeftOut.Top = this.pb_PlayRightIn.Top = this.pb_PlayRightOut.Top = this.slide_FrameSelect.Top + 23;
            this.pb_PlayLeftIn.Left = this.pb_PlayLeftOut.Left = this.num_FrameIndex.Left + this.num_FrameIndex.Width + 6;
            this.pb_PlayRightIn.Left = this.pb_PlayRightOut.Left = this.Width - 80;// this.slide_FrameSelect.Left + this.slide_FrameSelect.Width - 4;
            this.slide_FrameSelect.Left = this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width - 10;
            this.slide_FrameSelect.Width = this.pb_PlayRightIn.Left - (this.pb_PlayLeftIn.Left + this.pb_PlayLeftIn.Width) + 20;

            this.lbl_FramesShown.Left = this.pb_2DMap.Left;
            this.lbl_FramesShown.Top = this.hsb_2DMap.Top - this.lbl_FramesShown.Height - 4;

            // Middle Bottom 
            this.num_minMobility.Top = this.plot_Mobility.Top + plot_Mobility_HEIGHT + 5;
            this.num_maxMobility.Top = this.num_minMobility.Top;

            // pb_2DMap Size
            // max_plot_width *********************************************
            this.max_plot_width = this.slider_ColorMap.Left - this.pb_2DMap.Left - 20;

            // selection corners
            if (this.menuItem_SelectionCorners.Checked)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this.corner_2DMap[i].X < 0)
                        this.corner_2DMap[i].X = (int)((double)this.pb_2DMap.Width * .05);
                    else if (this.corner_2DMap[i].X > this.pb_2DMap.Width)
                        this.corner_2DMap[i].X = (int)((double)this.pb_2DMap.Width * .95);

                    if (this.corner_2DMap[i].Y < 0)
                        this.corner_2DMap[i].Y = (int)((double)this.pb_2DMap.Height * .05);
                    else if (this.corner_2DMap[i].Y > this.pb_2DMap.Height)
                        this.corner_2DMap[i].Y = (int)((double)this.pb_2DMap.Height * .95);
                }
                this.pb_2DMap.Invalidate();
                return;
            }

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

            this.flag_update2DGraph = true;
            this.flag_Resizing = false;

            if (this.tabpages_Main.SelectedTab == this.tab_InstrumentSettings)
                this.pnl_InstrumentSettings.Resize_This();
        }
#if TRACK_RESIZE_EVENTS
      public int count_resizes = 0;
#endif

        protected virtual void GraphFrame(int[][] frame_data, bool flag_enablecontrols)
        {
            lock (this.lock_graphing)
            {
                int interval = 1;

                this.flag_selection_drift = false;

                this.lbl_ExperimentDate.Text = this.uimf_GlobalParameters.DateStarted;

                this.update_CalibrationCoefficients();

                // Initialize boundaries
                new_minMobility = 0;
                new_maxMobility = this.UIMF_DataReader.m_frameParameters.Scans - 1; //  this.imfReader.Experiment_Properties.TOFSpectraPerFrame-1;
                new_minBin = 0;
                new_maxBin = this.uimf_GlobalParameters.Bins - 1;

#if false
                // Belov 12/12/2006 - Max out the Graph to 100, 100 is the most our machine can handle
                // MessageBox.Show("bin width:  "+this.UIMF_DataReader.Global_Parameters.BinWidth.ToString());
                int hundred = (int)(100 * 10000 / (this.uimf_GlobalParameters.BinWidth * 10)); //TenthsOfNanoSecondsPerBin);
                if (new_maxBin > hundred)
                    new_maxBin = hundred;
#endif

                maximum_Mobility = new_maxMobility;
                maximum_Bins = new_maxBin;

                this.num_minMobility.Minimum = -100;
                this.num_maxMobility.Maximum = 100000;

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
                    this.mean_TOFScanTime = this.UIMF_DataReader.m_frameParameters.AverageTOFLength;
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

                if (this.uimf_GlobalParameters.NumFrames < 2)
                {
                    this.slide_FrameSelect.Hide();
                    this.num_FrameRange.Hide();
                    this.lbl_FrameRange.Hide();

                    this.pb_PlayLeftIn.Hide();
                    this.pb_PlayLeftOut.Hide();
                    this.pb_PlayRightIn.Hide();
                    this.pb_PlayRightOut.Hide();
                }
                else
                {
                    this.slide_FrameSelect.Show();
                    this.num_FrameRange.Show();
                    this.lbl_FrameRange.Show();

                    try
                    {
                        this.slide_FrameSelect.Value = this.current_frame_index;

                        if (this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex) < 10)
                        {
                            this.slide_FrameSelect.MajorDivisions.Interval = 1;
                            this.slide_FrameSelect.MinorDivisions.TickVisible = false;
                        }
                        else
                        {
                            interval = (this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex) / 5) - ((this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex) / 5) % 10);
                            if (interval > 0)
                                this.slide_FrameSelect.MajorDivisions.Interval = interval;
                            else
                                this.slide_FrameSelect.MajorDivisions.Interval = 1;
                            if (this.uimf_GlobalParameters.NumFrames < 50)
                                this.slide_FrameSelect.MinorDivisions.TickVisible = false;
                            else
                                this.slide_FrameSelect.MinorDivisions.Interval = this.slide_FrameSelect.MajorDivisions.Interval / 2;
                        }
                    }
                    catch (Exception ex)
                    {
                        // possibly a drag and drop frame.  If so, we don't want to be in this section anyhow.
                        this.slide_FrameSelect.Hide();
                        this.pb_PlayLeftIn.Hide();
                        this.pb_PlayLeftOut.Hide();
                        this.pb_PlayRightIn.Hide();
                        this.pb_PlayRightOut.Hide();
                    }
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

                this.Text = Path.GetFileNameWithoutExtension(this.UIMF_DataFile);

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
                float min_Py = this.pb_2DMap.Height - Math.Max(p1.Y, p2.Y);
                float max_Py = this.pb_2DMap.Height - Math.Min(p1.Y, p2.Y);

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
                if ((min_Py != 0) || (max_Py != this.pb_2DMap.Height))
                {
                    if (this.current_valuesPerPixelY < 0)
                    {
                        new_maxBin = (int)this.UIMF_DataReader.get_pixelMZ((int)max_Py / -this.current_valuesPerPixelY);
                        new_minBin = (int)this.UIMF_DataReader.get_pixelMZ((int)min_Py / -this.current_valuesPerPixelY);
                    }
                    else
                    {
                        new_maxBin = (int)this.UIMF_DataReader.get_pixelMZ((int)max_Py);
                        new_minBin = (int)this.UIMF_DataReader.get_pixelMZ((int)min_Py);
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
        protected virtual void Generate2DIntensityArray(int frame_index)
        {
            int num_frame_range = Convert.ToInt32(this.num_FrameRange.Value);

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                MessageBox.Show("ERROR:  should not be here");
                return;
            }

            // Width will either be the number of spectra, or it will be the number to display
            // multiplied by the number of X pixels per spectra
            /*
            #if !TESTING
                        this.current_minMobility = 250;
                        this.current_maxMobility = 300;
            #else
                        this.current_minMobility = 0;
                        this.current_maxMobility = this.uimf_FrameParameters.Scans-1;
            #endif
                        this.current_valuesPerPixelX = 1;
             */
#if false
            this.width_PlotMobility = (int) (this.current_valuesPerPixelX > 0 ? ((current_maxMobility - current_minMobility) / current_valuesPerPixelX) : (current_maxMobility - current_minMobility));

            // Height determined differently
            if (this.flag_display_as_TOF)
            {
               // current_minBin = 34300;
              //  current_maxBin = 34500;
                total_bins = (current_maxBin - current_minBin);
               
                if (total_bins > MAP_DESIRED_HEIGHT)
                {
                    this.current_valuesPerPixelY = (total_bins / MAP_DESIRED_HEIGHT) + 1;
                    this.height_PlotBin = total_bins / this.current_valuesPerPixelY;
                    this.current_valuesPerPixelY = total_bins / this.height_PlotBin;
                }
                else
                {
                    this.current_valuesPerPixelY = -((MAP_DESIRED_HEIGHT / total_bins) + 1);
                    this.height_PlotBin = total_bins * -this.current_valuesPerPixelY;
                    this.current_valuesPerPixelY = -this.height_PlotBin / total_bins;
                }
            }
            else
            {
                // Much more difficult to find where the mz <-> TOF index correlation
                //
                // linearize the mz and find the cursor.
                // calculate the mz, then convert to TOF for all the values.
                float mzMax = this.mz_Calibration.TOFtoMZ(current_maxBin * TenthsOfNanoSecondsPerBin);
                float mzMin = this.mz_Calibration.TOFtoMZ(current_minBin * TenthsOfNanoSecondsPerBin);

                float diffMZ = mzMax - mzMin;
                float rangeTOF = current_maxBin - current_minBin;

                total_bins = (int)(this.mz_Calibration.TOFtoMZ((float)((current_maxBin * TenthsOfNanoSecondsPerBin))) -
                    this.mz_Calibration.TOFtoMZ((float)((current_minBin * TenthsOfNanoSecondsPerBin))));
                //MessageBox.Show("total_bins: "+total_bins.ToString());

                if (total_bins > MAP_DESIRED_HEIGHT)
                {
                    this.current_valuesPerPixelY = (total_bins / MAP_DESIRED_HEIGHT) + 1;
                    this.height_PlotBin = total_bins / this.current_valuesPerPixelY;
                    this.current_valuesPerPixelY = total_bins / this.height_PlotBin;
                }
                else
                {
                    MessageBox.Show("total_bins < DESIRED_HEIGHT");
                    this.current_valuesPerPixelY = -(MAP_DESIRED_HEIGHT / total_bins) + 1;
                    this.height_PlotBin = total_bins * -this.current_valuesPerPixelY;
                    this.current_valuesPerPixelY = -this.height_PlotBin * total_bins;
                }
            }

            // plot dimensions
            this.data_driftTIC = new int[this.width_PlotMobility];
            this.data_tofTIC = new int[this.height_PlotBin];
#endif

          //  MessageBox.Show("arg: " + (frame_index - (num_frame_range - 1)).ToString()+" < "+frame_index.ToString());

            // Determine the frame size
            this.get_ViewableIntensities(frame_index-(num_frame_range - 1), frame_index);

            if (this.flag_Closing)
            {
                return;
            }

            this.current_frame_index = frame_index;

            if (this.flag_viewMobility)
                this.xAxis_Mobility.Caption = "Mobility - Scans";
            else
                this.xAxis_Mobility.Caption = "Mobility - Time (msec)";

            if (this.flag_display_as_TOF)
                this.yAxis_TOF.Caption = "Time of Flight (usec)";
            else
                this.yAxis_TOF.Caption = "m/z";

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void get_ViewableIntensities(int start_index, int end_index)
        {
            //MessageBox.Show("get_viewableintensities(" + start_index.ToString() + ", " + end_index.ToString()+")");
            if (this.flag_collecting_data)
                return;
            this.flag_collecting_data = true;

            int frames;
            int temp;
            int data_height;
            int data_width;
            int total_mobility;
            int total_bins;

            int max_MZRange_bin;
            int min_MZRange_bin;
            float select_MZ = (float)Convert.ToDouble(this.num_MZ.Value);
            float select_PPM = (float)(select_MZ * Convert.ToDouble(this.num_PPM.Value) / 1000000.0);
            if (this.cb_EnableMZRange.Checked)
            {
                min_MZRange_bin = this.UIMF_DataReader.mz_Calibration.MZtoTOF(select_MZ - select_PPM) / 10;
                max_MZRange_bin = this.UIMF_DataReader.mz_Calibration.MZtoTOF(select_MZ + select_PPM) / 10;

                this.current_minBin = this.UIMF_DataReader.mz_Calibration.MZtoTOF((float)(select_MZ - (select_PPM * 1.5))) / 10;
                this.current_maxBin = this.UIMF_DataReader.mz_Calibration.MZtoTOF((float)(select_MZ + (select_PPM * 1.5))) / 10;

                // MessageBox.Show(min_MZRange_bin.ToString() + "<" + max_MZRange_bin.ToString());
            }
            else
            {
                min_MZRange_bin = 0;
                max_MZRange_bin = this.uimf_GlobalParameters.Bins;
            }

            if (this.current_maxBin < this.current_minBin)
            {
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
                    this.current_maxBin = this.maximum_Bins;
                    this.current_minBin = this.maximum_Bins - (this.current_valuesPerPixelY * this.max_plot_height);
                }
                if (this.current_minBin < 0)
                {
                    MessageBox.Show("Bill "+"("+this.current_maxBin.ToString()+" < "+this.current_minBin.ToString()+")\n\n"+this.max_plot_height.ToString()+" < "+total_bins.ToString()+"\n\nget_ViewableIntensities: this.current_maxBin is already this.maximum_Bins  -- should never happen");
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
                this.pb_2DMap.Height = (total_bins / this.current_valuesPerPixelY) + 1;
            else
                this.pb_2DMap.Height = (total_bins * -this.current_valuesPerPixelY) + 1;

            if (max_plot_width < total_mobility)
            {
                // in this case we will not overlap pixels.  We can create another scrollbar to handle too wide plots
                this.current_valuesPerPixelX = -1;

                this.current_minMobility = this.hsb_2DMap.Value;
                this.current_maxMobility = this.current_minMobility + this.max_plot_width;

                if (this.current_maxMobility > this.maximum_Mobility)
                {
                    this.current_maxMobility = this.maximum_Mobility;
                    this.current_minMobility = this.maximum_Mobility - this.max_plot_width;
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
            if (this.current_valuesPerPixelX > 0)
                this.pb_2DMap.Width = (total_mobility / this.current_valuesPerPixelX) + 1;
            else
                this.pb_2DMap.Width = (total_mobility * -this.current_valuesPerPixelX) + 1;

            // create array to store visual data
            if (this.current_valuesPerPixelX < 0)
                data_width = total_mobility;
            else
                data_width = this.pb_2DMap.Width;
            if (this.current_valuesPerPixelY < 0)
                data_height = total_bins;
            else
                data_height = this.pb_2DMap.Height;

            this.data_2D = new int[data_width][];
            for (int n = 0; n < data_width; n++)
                this.data_2D[n] = new int[data_height];

            // show frame range
           // MessageBox.Show("show frame range: " + this.slide_FrameSelect.Value.ToString());
            if ((this.slide_FrameSelect.Value - Convert.ToInt32(this.num_FrameRange.Value) + 1) < 0)
                this.slide_FrameSelect.FillBaseValue = 0;
            else
                this.slide_FrameSelect.FillBaseValue = ((double)(this.slide_FrameSelect.Value - Convert.ToInt32(this.num_FrameRange.Value) + 1)) - .1;

            if (Convert.ToInt32(this.num_FrameRange.Value) > 1)
            {
                this.lbl_FramesShown.Show();
                this.lbl_FramesShown.Text = "Showing Frames: " + start_index.ToString() + " to " + end_index.ToString();
            }

            if (this.num_FrameIndex.Maximum >= (int)this.slide_FrameSelect.Value)
                this.num_FrameIndex.Value = (int)this.slide_FrameSelect.Value;

            // collect the data
            for (frames = start_index; (frames <= end_index) && !this.flag_Closing; frames++)
            {
               // this.lbl_ExperimentDate.Text = "accumulate_FrameData: " + (++count_times).ToString() + "  "+start_index.ToString()+"<"+end_index.ToString();

                try
                {
                    if (this.data_2D == null)
                        MessageBox.Show("null");
                    this.data_2D = this.UIMF_DataReader.accumulate_FrameData(frames, this.flag_display_as_TOF, this.current_minMobility, this.current_minBin, min_MZRange_bin, max_MZRange_bin, this.data_2D, this.current_valuesPerPixelY);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("accumulate_FrameData:  "+ex.ToString());
                }

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
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            this.update_CalibrationCoefficients();

#if false
            if (pixel_y < this.pb_2DMap.Height)
            {
                this.data_2D[scans][pixel_y] += int_BinIntensity;
                if (this.data_2D[scans][pixel_y] > this.data_maxIntensity)
                {
                    this.data_maxIntensity = this.data_2D[scans][pixel_y];
                    this.posX_MaxIntensity = scans;
                    this.posY_MaxIntensity = pixel_y;
                }
            }
#endif
            this.plot_axisMobility(this.data_driftTIC);
            this.plot_axisTOF(this.data_tofTIC);

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

            if (this.current_valuesPerPixelX > 0)
            {
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width;
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width;
            }
            else
            {
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width + this.current_valuesPerPixelX;
                this.plot_Mobility.Left = this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width - this.current_valuesPerPixelX / 2;
            }

            this.num_minMobility.Left = this.plot_Mobility.Left;
            this.num_maxMobility.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.num_maxMobility.Width; //- (this.plot_Mobility.PlotAreaBounds.Width - this.pb_2DMap.Width)
            this.cb_MaxScanValue.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.cb_MaxScanValue.Width - 3;

            this.pb_2DMap.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.plot_Mobility.PlotAreaBounds.Left;
            this.hsb_2DMap.Left = this.pb_2DMap.Left;

            this.hsb_2DMap.Width = this.pb_2DMap.Width;
            this.vsb_2DMap.Left = this.pb_2DMap.Left + this.pb_2DMap.Width;

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

            int best_width;
            int compression;
            int compression_collection;
            int total_frames = this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex);
            int total_scans = this.UIMF_DataReader.m_frameParameters.Scans;

            int data_height;
            int data_width = total_frames / Convert.ToInt32(this.num_FrameCompression.Value);

            int max_MZRange_bin;
            int min_MZRange_bin;
            float select_MZ = (float)Convert.ToDouble(this.num_MZ.Value);
            float select_PPM = (float)(select_MZ * Convert.ToDouble(this.num_PPM.Value) / 1000000.0);

            if (this.cb_EnableMZRange.Checked)
            {
                min_MZRange_bin = this.UIMF_DataReader.mz_Calibration.MZtoTOF(select_MZ - select_PPM) / 10;
                max_MZRange_bin = this.UIMF_DataReader.mz_Calibration.MZtoTOF(select_MZ + select_PPM) / 10;

                // MessageBox.Show(min_MZRange_bin.ToString() + "<" + max_MZRange_bin.ToString());
            }
            else
            {
                min_MZRange_bin = 0;
                max_MZRange_bin = this.uimf_GlobalParameters.Bins;
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

                        mobility_data = this.UIMF_DataReader.get_MobilityData(frame_index, min_MZRange_bin, max_MZRange_bin);
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
                best_width = (data_width / this.chromatogram_valuesPerPixelX) + 1;
            else
                best_width = (data_width * -this.chromatogram_valuesPerPixelX) + 1;

            if (best_width > this.slider_ColorMap.Left - this.pb_2DMap.Left)
                this.tab_DataViewer.Width = this.pb_2DMap.Left + best_width + 175;
            else
            {
                this.chromatogram_valuesPerPixelX = -((((this.slider_ColorMap.Left - this.pb_2DMap.Left) / best_width) * best_width) / data_width);
                best_width = (data_width * -this.chromatogram_valuesPerPixelX) + 1;
            }

            this.pb_2DMap.Width = best_width;

            if (this.current_maxMobility > total_frames)
            {
                this.current_maxMobility = total_frames - 1;// -this.pb_2DMap.Width - 1;
                this.current_minMobility = this.current_maxMobility - this.pb_2DMap.Width;
            }

            this.chromatogram_valuesPerPixelY = 1; //(total_scans / this.max_plot_height);
            this.current_minBin = 0;
            if (this.max_plot_height > total_scans - 1)
                this.current_maxBin = this.current_minBin + total_scans - 1;
            else
                this.current_maxBin = this.current_minBin + this.max_plot_height;

            total_scans = (this.current_maxBin - this.current_minBin);
            this.chromatogram_valuesPerPixelY = 1; //(total_scans / this.max_plot_height);
            this.pb_2DMap.Height = (total_scans / this.chromatogram_valuesPerPixelY) + 1;

            //-----------------------------------------------------------------------------------------
            // create array to store visual data
            if (this.chromatogram_valuesPerPixelY < 0)
                data_height = total_scans;
            else
                data_height = this.pb_2DMap.Height;

            this.data_2D = new int[data_width][];
            for (int n = 0; n < data_width; n++)
                this.data_2D[n] = new int[data_height];

            //-----------------------------------------------------------------------------------------
            // collect the data for viewing.
            this.chromat_max = 0;

            if (data_width > this.pb_2DMap.Width)
            {
                this.hsb_2DMap.SmallChange = this.pb_2DMap.Width / 5;
                this.hsb_2DMap.LargeChange = this.pb_2DMap.Width * 4 / 5;

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

            this.maxFrame_Chromatogram = this.minFrame_Chromatogram + this.pb_2DMap.Width;
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
                //  MessageBox.Show("done: "+this.pb_2DMap.Width.ToString());
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
            this.plot_TOF.Height = this.pnl_Chromatogram.Top - this.plot_TOF.Top - 30;

            this.num_minBin.Top = this.plot_TOF.Top + this.plot_TOF.Height + 4;
            this.plot_Mobility.Top = this.plot_TOF.Top + this.plot_TOF.Height;
            this.num_minMobility.Top = this.num_maxMobility.Top = this.plot_Mobility.Top + this.plot_Mobility.Height + 4;
            this.cb_MaxScanValue.Top = this.plot_Mobility.Top + this.plot_Mobility.Height - this.cb_MaxScanValue.Height - 2;
            this.vsb_2DMap.Height = this.pb_2DMap.Height;

            this.pb_2DMap.Top = this.num_maxBin.Top + this.num_maxBin.Height + 4 + this.plot_TOF.PlotAreaBounds.Top;
            this.hsb_2DMap.Top = this.pb_2DMap.Top - this.hsb_2DMap.Height;
            this.vsb_2DMap.Top = this.pb_2DMap.Top;
            // MessageBox.Show("3");

            if (this.chromatogram_valuesPerPixelX > 0)
            {
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.chromatogram_valuesPerPixelX/2;
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width - this.chromatogram_valuesPerPixelX;
            }
            else
            {
                this.plot_Mobility.Width = this.pb_2DMap.Width + this.plot_Mobility.Width - this.plot_Mobility.PlotAreaBounds.Width + this.chromatogram_valuesPerPixelX;
                this.plot_Mobility.Left = this.plot_TOF.Left + this.plot_TOF.Width + (-this.chromatogram_valuesPerPixelX / 2);
            }

            this.num_minMobility.Left = this.plot_Mobility.Left;
            this.num_maxMobility.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.num_maxMobility.Width; //- (this.plot_Mobility.PlotAreaBounds.Width - this.pb_2DMap.Width)
            this.cb_MaxScanValue.Left = this.plot_Mobility.Left + this.plot_Mobility.Width - this.cb_MaxScanValue.Width - 3;

            this.cb_FrameType.Top = this.num_minMobility.Top + 40;
            this.cb_FrameType.Left = this.num_minMobility.Left + 5;

            this.pb_2DMap.Left = this.plot_TOF.Left + this.plot_TOF.Width + this.plot_Mobility.PlotAreaBounds.Left;
            this.hsb_2DMap.Left = this.pb_2DMap.Left;

            this.hsb_2DMap.Width = this.pb_2DMap.Width;
            this.vsb_2DMap.Left = this.pb_2DMap.Left + this.pb_2DMap.Width;

            if (this.mean_TOFScanTime == 0)
                this.xAxis_Mobility.Caption = "Frames";
            else
                this.xAxis_Mobility.Caption = "Frames - Time (sec)";

            if (this.flag_viewMobility)
                this.yAxis_TOF.Caption = "Mobility - Scans";
            else
                this.yAxis_TOF.Caption = "Mobility - Time (msec)";

            this.flag_collecting_data = false;
        }

        // ////////////////////////////////////////////////////////////////////////////
        // change the background color
        //
        // private void slider_Background_MouseUp(object obj, System.Windows.Forms.MouseEventArgs e)
        private void slider_Background_Move(object obj, System.EventArgs e)
        {
            if (this.pb_2DMap != null)
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
        private void pb_2DMap_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if ((this.flag_kill_mouse) || // if plotting the plot, prevent zooming!
                (this.flag_CinemaPlot))
                return;

            // Graphics g = this.pb_2DMap.CreateGraphics();
            // g.DrawString(e.X.ToString(), new Font(FontFamily.GenericSerif, 10, FontStyle.Regular), new SolidBrush(Color.Yellow), 10, 50);

            // Starting a zoom process			
            if (e.Button == MouseButtons.Left)
            {
                _mouseDragging = true;

                this.Cursor = Cursors.Cross;
                _mouseDownPoint = new Point(e.X, e.Y);
                _mouseMovePoint = new Point(e.X, e.Y);
            }

            // Pop-up Menu
            if (e.Button == MouseButtons.Right)
                contextMenu_pb_2DMap.Show(this, new Point(e.X + pb_2DMap.Left, e.Y + pb_2DMap.Top));

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
                pb_2DMap_Extensions = this.pb_2DMap.CreateGraphics();
                for (int i = 0; i <= this.data_2D.Length - 1; i++)
                    for (int j = 0; j <= this.data_2D[0].Length - 1; j++)
                    {
                        if (this.data_2D[i][j] != 0)
                        {
                            pb_2DMap_Extensions.DrawString(this.data_2D[i][j].ToString("#"), map_font, back_brush, (i * -this.current_valuesPerPixelX) + 1, this.pb_2DMap.Height - ((j + 1) * -this.current_valuesPerPixelY) - 1);
                            pb_2DMap_Extensions.DrawString(this.data_2D[i][j].ToString("#"), map_font, fore_brush, (i * -this.current_valuesPerPixelX), this.pb_2DMap.Height - ((j + 1) * -this.current_valuesPerPixelY));
                        }
                    }
            }
        }

        protected int prev_cursorX = 0;
        protected int prev_cursorY = 0;
        protected virtual void pb_2DMap_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
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
                else if (e.X > this.pb_2DMap.Width)
                    this.corner_2DMap[this.flag_MovingCorners].X = this.pb_2DMap.Width;
                else
                    this.corner_2DMap[this.flag_MovingCorners].X = e.X;

                if (e.Y < 0)
                    this.corner_2DMap[this.flag_MovingCorners].Y = 0;
                else if (e.Y > this.pb_2DMap.Height)
                    this.corner_2DMap[this.flag_MovingCorners].Y = this.pb_2DMap.Height;
                else
                    this.corner_2DMap[this.flag_MovingCorners].Y = e.Y;

                this.pb_2DMap.Invalidate();
                return;
            }

            // Draw a rectangle along with the dragging mouse.
            if (_mouseDragging) //&& !toolBar1.Buttons[0].Pushed)
            {
                int x, y;
                // Ensure that the mouse point does not overstep its bounds
                x = Math.Min(e.X, pb_2DMap.Width - 1);
                x = Math.Max(x, 0);
                y = Math.Min(e.Y, pb_2DMap.Height - 1);
                y = Math.Max(y, 0);

                _mouseMovePoint = new Point(x, y);

                this.pb_2DMap.Invalidate();
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

        public bool inside_Polygon(int xPos, int yPos)
        {
            if (this.menuItem_SelectionCorners.Checked)
            {
                // this.lbl_ExperimentDate.Text = xPos.ToString() + ", " + yPos.ToString();
                yPos = this.pb_2DMap.Height - yPos;

                // due to strange shapes, I have to split this into triangles for results to be correct.
                Point pt = new Point(xPos, yPos);
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

        private void pb_2DMap_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
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

            if (this.pb_2DMap_Extensions != null)
            {
                this.pb_2DMap.Refresh();
                this.pb_2DMap_Extensions = null;
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
                    // select the range of frames
                    if (this.chromatogram_valuesPerPixelX < 0)
                    {
                        if (this._mouseDownPoint.X > this._mouseMovePoint.X)
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X / -this.chromatogram_valuesPerPixelX) + 1;
                        }
                    }
                    else // we have compressed the chromatogram
                    {
                        if (this._mouseDownPoint.X > this._mouseMovePoint.X)
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * this.chromatogram_valuesPerPixelX) + 1;
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * this.chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                            minframe_Data_number = this.minFrame_Chromatogram + (this._mouseDownPoint.X * this.chromatogram_valuesPerPixelX) + 1;
                            maxframe_Data_number = this.minFrame_Chromatogram + (this._mouseMovePoint.X * this.chromatogram_valuesPerPixelX) + 1;
                        }
                    }

                    if (minframe_Data_number < 1)
                        minframe_Data_number = 1;
                    if (maxframe_Data_number > this.uimf_GlobalParameters.NumFrames)
                        maxframe_Data_number = this.uimf_GlobalParameters.NumFrames;
                    this.slide_FrameSelect.Value = maxframe_Data_number;
                    this.num_FrameRange.Value = maxframe_Data_number - minframe_Data_number + 1;

                    // select the mobility highlight
                    // select the range of frames
                    //MessageBox.Show(this.chromatogram_valuesPerPixelY.ToString());
                    if (this.current_valuesPerPixelY < 0)
                    {
                        if (this._mouseDownPoint.Y > this._mouseMovePoint.Y)
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseMovePoint.Y) / -this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseDownPoint.Y) / -this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = min_select_mobility;
                            this.selection_max_drift = max_select_mobility;
                        }
                        else
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseDownPoint.Y) / -this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseMovePoint.Y) / -this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = max_select_mobility;
                            this.selection_max_drift = min_select_mobility;
                        }
                    }
                    else
                    {
                        if (this._mouseDownPoint.Y > this._mouseMovePoint.Y)
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseMovePoint.Y) * this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseDownPoint.Y) * this.chromatogram_valuesPerPixelY);

                            this.selection_min_drift = min_select_mobility;
                            this.selection_max_drift = max_select_mobility;
                        }
                        else
                        {
                            min_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseDownPoint.Y) * this.chromatogram_valuesPerPixelY);
                            max_select_mobility = this.minMobility_Chromatogram + ((this.pb_2DMap.Height - this._mouseMovePoint.Y) * this.chromatogram_valuesPerPixelY);

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
#if RESIZE
                    this.ResizeThis();
#endif
                    this.current_frame_index = (int)this.slide_FrameSelect.Value;

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

        protected virtual void pb_2DMap_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
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

            //   this.plot_Height = this.data_2D[0].Length;
            //   this.plot_Width = this.data_2D.Length;

            this.num_FrameRange.Top = this.hsb_2DMap.Top - this.num_FrameRange.Height - 4;
            this.num_FrameRange.Left = this.vsb_2DMap.Left + this.vsb_2DMap.Width - this.num_FrameRange.Width;
            this.lbl_FrameRange.Top = this.num_FrameRange.Top + 2;
            this.lbl_FrameRange.Left = this.num_FrameRange.Left - this.lbl_FrameRange.Width - 2;

            // this section draws the highlight on the plot.
            if (this.flag_selection_drift)
            {
                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    w = this.pb_2DMap.Width / this.uimf_GlobalParameters.NumFrames;
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

                    w = this.pb_2DMap.Width / (this.current_maxMobility - this.current_minMobility + 1);
                    xl = ((this.selection_min_drift - min_mobility) * w);

                    //MessageBox.Show("here");
                    // if (this.current_valuesPerPixelX < 0)
                    //     xl += this.current_valuesPerPixelX;
                    xwidth = (this.selection_max_drift - this.selection_min_drift + 1) * w;
                   // e.Graphics.DrawString("(" + this.selection_min_drift.ToString() + " - " + min_mobility.ToString() + ") * " + w.ToString() + " = " + xl.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 10);
                    //e.Graphics.DrawString((this.mean_TOFScanTime / 1000000).ToString() + " ... " + min_mobility.ToString() + "    (" + this.selection_max_drift.ToString() + " - " + this.selection_min_drift.ToString() + " + 1) * " + w.ToString() + " = " + xwidth.ToString() + " ... " + new_minMobility.ToString() + ", " + new_maxMobility.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 50);
                }
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(145, 111, 111, 126)), xl, 0, xwidth, this.pb_2DMap.Height);
            }

            this.draw_Corners(e.Graphics);
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
            points[0][2] = points[1][3] = new Point(this.pb_2DMap.Width, 0); // top right
            points[2][3] = points[1][2] = new Point(this.pb_2DMap.Width, this.pb_2DMap.Height); // bot right
            points[2][2] = points[3][2] = new Point(0, this.pb_2DMap.Height); // bot left

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
                this.corner_2DMap[0] = new Point((int)(this.pb_2DMap.Width * .15), (int)(this.pb_2DMap.Height * .15));
                this.corner_2DMap[1] = new Point((int)(this.pb_2DMap.Width * .85), (int)(this.pb_2DMap.Height * .15));
                this.corner_2DMap[2] = new Point((int)(this.pb_2DMap.Width * .85), (int)(this.pb_2DMap.Height * .85));
                this.corner_2DMap[3] = new Point((int)(this.pb_2DMap.Width * .15), (int)(this.pb_2DMap.Height * .85));
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
                    this.pb_2DMap_DblClick((object)null, (System.EventArgs)null);
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
            string folder = Path.GetDirectoryName(this.UIMF_DataFile);
            string exp_name = Path.GetFileNameWithoutExtension(this.UIMF_DataFile);
            string filename = folder + "\\" + exp_name + ".Accum_" + this.current_frame_index.ToString("0000") + ".BMP";
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
            //			double[] data = new double[100000];
            //			for(int i=0; i<data.Length; i++)
            //				data[i] = 1;
            //
            //			CDataSmoother s = new CDataSmoother();
            //			s.Add5Point();
            //			double[] smoothed_data;
            //			s.Smooth(data, out smoothed_data);
            //			int j;
            //			j=4;
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
                    double increment_MobilityValue = this.mean_TOFScanTime * (this.maximum_Mobility + 1) * this.UIMF_DataReader.m_frameParameters.Accumulations / 1000000.0 / 1000.0;
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
            this.UIMF_DataReader.CloseUIMF(this.UIMF_DataFile);
            RegistrySave(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        private void pb_2DMap_MouseLeave(object sender, System.EventArgs e)
        {
            _interpolation_points.Clear();
        }

        protected virtual void pb_2DMap_DblClick(object sender, System.EventArgs e)
        {
            int frame_number;

            if (this.flag_CinemaPlot)
            {
                this.StopCinema();
                return;
            }

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Width = this.pb_2DMap.Left + this.UIMF_DataReader.m_frameParameters.Scans + 170;

                this.rb_PartialChromatogram.Checked = false;
                this.rb_CompleteChromatogram.Checked = false;

                this.plot_Mobility.StopAnnotating(false);

                this.Chromatogram_CheckedChanged();

                // MessageBox.Show(this.chromatogram_valuesPerPixelX.ToString());
                if (this.chromatogram_valuesPerPixelX < 0)
                    frame_number = this.minFrame_Chromatogram + (this.prev_cursorX * Convert.ToInt32(this.num_FrameCompression.Value) / (-this.chromatogram_valuesPerPixelX)) + 1;
                else
                    frame_number = this.minFrame_Chromatogram + (this.prev_cursorX * Convert.ToInt32(this.num_FrameCompression.Value)) + 1;

                // MessageBox.Show(frame_number.ToString()+"="+this.minFrame_Chromatogram.ToString() + "  " + this.prev_cursorX.ToString() + "  " + this.current_valuesPerPixelX.ToString());

                if (frame_number < 1)
                    frame_number = 1;
                if (frame_number > this.uimf_GlobalParameters.NumFrames)
                    frame_number = this.uimf_GlobalParameters.NumFrames;

                this.slide_FrameSelect.Value = frame_number;

                this.current_frame_index = (int)this.slide_FrameSelect.Value;
                this.plot_Mobility.ClearRange();
                this.num_FrameRange.Value = 1;

                this.vsb_2DMap.Show();  // gets hidden with Chromatogram
                this.hsb_2DMap.Show();

                // this.imf_ReadFrame(this.new_frame_index, out frame_Data);

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

            Graphics g = this.pb_2DMap.CreateGraphics();

            this.selection_min_drift = e.Min;
            this.selection_max_drift = e.Max;

            this.flag_selection_drift = e.Selecting;

            this.flag_update2DGraph = true;
        }

        private void menuItem_Export_Click(object sender, System.EventArgs e)
        {
            if (this.flag_display_as_TOF)
            {
                MessageBox.Show("Export requires data to be shown in MZ mode.");
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

        // mike wants complete dump.
        private void export_ChromatogramIntensityMatrix(string filename)
        {
            int frames_width = this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex);
            double[] frames_axis = new double[frames_width];
            int mob_height = this.UIMF_DataReader.m_frameParameters.Scans;
            double[] drift_axis = new double[mob_height];

            int [][]dump_chromatogram = new int[frames_width][];
            for (int i=0; i<frames_width; i++)
            {
                dump_chromatogram[i] = this.UIMF_DataReader.get_MobilityData(i);
            }


#if false
            int frames_width = this.chromat_data.Length;
            double[] frames_axis = new double[frames_width];
            double frames_increment = Convert.ToInt32(this.num_FrameCompression.Value);

            int mob_height = this.chromat_data[0].Length;
            double[] drift_axis = new double[mob_height];
            double drift_increment = this.chromatogram_valuesPerPixelY;
#endif

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
            Generate2DIntensityArray(this.current_frame_index);
            double min, max;

            double mob_width = this.data_2D.Length;
            double[] drift_axis = new double[(int)mob_width];

            double tof_height = this.data_2D[0].Length;
            double[] tof_axis = new double[(int)tof_height];

            double increment;

            increment = (this.current_maxMobility - this.current_minMobility) / mob_width;
            drift_axis[0] = this.current_minMobility;
            for (i = 1; i < mob_width; i++)
                drift_axis[i] = (drift_axis[i - 1] + increment);
            for (i = 0; i < mob_width; i++)
                drift_axis[i] *= (float)this.mean_TOFScanTime;

            max = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);
            min = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);
            increment = (max - min) / tof_height;

            tof_axis[0] = min;
            for (i = 1; i < tof_height; i++)
            {
                tof_axis[i] = (tof_axis[i - 1] + (float)increment);
            }

            Utilities.TextExport tex = new Utilities.TextExport();
            tex.Export(filename, "m/z", this.data_2D, drift_axis, tof_axis);
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
                    this.pb_2DMap.Update();
                    this.slide_FrameSelect.Update();

                    export_IntensityMatrix(save_dialog.FileName.Split('.')[0] + i.ToString("0000") + ".csv");
                }
            }
#endif
        }

        private void menuItem_CopyToClipboard_Click(object sender, System.EventArgs e)
        {
            Clipboard.SetDataObject(pb_2DMap.Image);
        }

        private void plot_Mobility_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenu_driftTIC.Show(this, new Point(e.X + this.plot_Mobility.Left, e.Y + this.plot_Mobility.Top));
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
                        double min_TOF = (this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double max_TOF = (this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double increment_TOF = (max_TOF - min_TOF) / (double)(this.pb_2DMap.Height);
                        for (int i = 0; i < this.tic_TOF.Length; i++)
                        {
                            sw_TOF.WriteLine("{0},{1}", ((double) i * increment_TOF) + min_TOF, tic_TOF[i]);
                        }
                    }
                    else
                    {
                        double mzMin = this.UIMF_DataReader.mz_Calibration.TOFtoMZ((float)(this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin));
                        double mzMax = this.UIMF_DataReader.mz_Calibration.TOFtoMZ((float)(this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin));
                        double increment_MZ = (mzMax - mzMin) / (double)this.pb_2DMap.Height;
                        for (int i = 0; i < this.tic_TOF.Length; i++)
                        {
                            sw_TOF.WriteLine("{0},{1}", (i * increment_MZ) + mzMin, tic_TOF[i]);
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
                if (this.maxMobility_Chromatogram > this.UIMF_DataReader.m_frameParameters.Scans - 1)
                {
                    this.maxMobility_Chromatogram = this.UIMF_DataReader.m_frameParameters.Scans - 1;
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
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.UIMF_DataReader.mz_Calibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.UIMF_DataReader.TenthsOfNanoSecondsPerBin;
                    max = this.UIMF_DataReader.mz_Calibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.UIMF_DataReader.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = ((max - min + 1.0) / this.pb_2DMap.Height);
                new_minBin = (int)min + 1;
                if (bin_diff > 0.0)
                    this.new_maxBin = this.new_minBin + (int)(bin_diff * this.pb_2DMap.Height);
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
                if (this.maxMobility_Chromatogram > this.UIMF_DataReader.m_frameParameters.Scans - 1)
                    this.maxMobility_Chromatogram = this.UIMF_DataReader.m_frameParameters.Scans - 1;

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
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.UIMF_DataReader.mz_Calibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.UIMF_DataReader.TenthsOfNanoSecondsPerBin;
                    max = this.UIMF_DataReader.mz_Calibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.UIMF_DataReader.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = (int)((max - min + 1) / this.pb_2DMap.Height);
                new_maxBin = (int)max + 1;
                if (bin_diff > 0)
                    this.new_minBin = new_maxBin - (bin_diff * this.pb_2DMap.Height);
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
            if ((double)this.num_FrameRange.Value > this.slide_FrameSelect.Range.Maximum+1)
            {
                this.num_FrameRange.Value = Convert.ToDecimal(this.slide_FrameSelect.Range.Maximum+1);
                return;
            }

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

        private void slide_FrameSelect_ValueChanged(object sender, System.EventArgs e)
        {
            if (this.slide_FrameSelect.Value - Convert.ToDouble(this.num_FrameRange.Value) < 0)
                this.slide_FrameSelect.Value = Convert.ToDouble(this.num_FrameRange.Value) - 1.0;

            if ((double) this.slide_FrameSelect.Value != (double) ((int) this.slide_FrameSelect.Value))
            {
                this.slide_FrameSelect.Value = (int) ((double) this.slide_FrameSelect.Value+.5);
                return;
            }

            this.flag_update2DGraph = true;
        }

        // //////////////////////////////////////////////////////////////////////////
        // Display Settings
        //
        private void slide_Threshold_ValueChanged(object sender, System.EventArgs e)
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
                topY = this.pb_2DMap.Height - 15 - ((this.posY_MaxIntensity + 1) * (-this.current_valuesPerPixelY));
                widthY = (-this.current_valuesPerPixelY) + 30;
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

            this.slide_FrameSelect.Value = 0;

            if (this.flag_GraphingFrame)
                return;
            this.flag_GraphingFrame = true;

            while (this.flag_Alive)
            {
                if (!this.pb_2DMap.Visible && !this.flag_FrameTypeChanged)
                {
                    Thread.Sleep(200);
                    continue;
                }

                try
                {
                    while (this.flag_update2DGraph && this.flag_Alive)
                    {
                        this.flag_update2DGraph = false;

                        if (this.flag_FrameTypeChanged)
                        {
                            this.flag_FrameTypeChanged = false;
                            this.Filter_FrameType(this.cb_FrameType.SelectedIndex);
                            this.current_frame_index = 0;
                        }

                        if (this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex) <= 0)
                        {
                            this.flag_update2DGraph = false;
                            break;
                        }

                        if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                        {
                            this.Graph_2DPlot((int)this.slide_FrameSelect.Value);
                            this.flag_update2DGraph = false;
                            break;
                        }

                        current_frame_number = this.UIMF_DataReader.load_Frame(this.current_frame_index);
                        if (new_frame_number != current_frame_number)
                        {
                            new_frame_number = current_frame_number;

                            this.update_CalibrationCoefficients();
                        }

                        if (current_frame_index < this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex))
                        {
                            //#if false
                            if (this.menuItem_ScanTime.Checked)
                            {
                                // MessageBox.Show("tof scan time: " + this.mean_TOFScanTime.ToString());
                                // Get the mean TOF scan time
                                this.mean_TOFScanTime = this.UIMF_DataReader.m_frameParameters.AverageTOFLength;
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
                              // MessageBox.Show("slide_FrameSelect.Value: " + slide_FrameSelect.Value.ToString()+"("+this.current_frame_index.ToString()+")");
                                this.Graph_2DPlot((int)this.slide_FrameSelect.Value);
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
                                if ((this.slide_FrameSelect.Value + this.Cinemaframe_DataChange >= 0) &&
                                    (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange <= this.slide_FrameSelect.Range.Maximum))
                                {
                                    this.slide_FrameSelect.Value += this.Cinemaframe_DataChange;
                                }
                                else
                                {
                                    if (this.Cinemaframe_DataChange > 0)
                                    {
                                        this.pb_PlayRightIn_Click((object)null, (EventArgs)null);
                                        this.slide_FrameSelect.Value = this.slide_FrameSelect.Range.Maximum;
                                    }
                                    else
                                    {
                                        this.pb_PlayLeftIn_Click((object)null, (EventArgs)null);
                                        this.slide_FrameSelect.Value = Convert.ToDouble(this.num_FrameRange.Value) - 1;
                                    }
                                }
                                this.flag_update2DGraph = true;
                            }
#if CONTROL_BOX
                                this.ControlBox = true;
#endif
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "cycle_GraphFrame: " + ex.ToString() + "\n\n" + ex.StackTrace.ToString());
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
        public void Graph_2DPlot(int frame_index)
        {
            if (frame_index >= this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex))
            {
                MessageBox.Show("Graph_2DPlot: "+frame_index+"\n\nAttempting to graph frame beyond list");
                return;
            }

            if (this.WindowState == FormWindowState.Minimized)
                return;
#if false
            // Nothing to graph?
            if (this.current_frame_index != frame_number)
            {
                this.current_frame_index = 0;
                this.Generate2DIntensityArray(frame_number);
            }
#endif
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

                    current_valuesPerPixelX = (current_maxMobility - current_minMobility + 1 < this.pb_2DMap.Width) ?
                        -(this.pb_2DMap.Width / (current_maxMobility - current_minMobility + 1)) : 1;
					
                    // For initial viz., don't want to expand widths of datasets with few TOFs
                    // if(current_maxMobility == this.imfReader.Experiment_Properties.TOFSpectraPerFrame-1 && current_minMobility== 0)
                    if (current_maxMobility == this.UIMF_DataReader.m_frameParameters.Scans - 1 && current_minMobility == 0)
                        current_valuesPerPixelX = 1;

                    current_valuesPerPixelY = ((current_maxBin - current_minBin + 1 < this.pb_2DMap.Height) ?
                        -(this.pb_2DMap.Height / (current_maxBin - current_minBin + 1)) : ((current_maxBin - current_minBin + 1) / this.pb_2DMap.Height));

                    // In case current_maxBin - current_minBin + 1 is not evenly divisible by current_valuesPerPixelY, we need to adjust one of
                    // these quantities to make it so.
                    if (current_valuesPerPixelY > 0)
                    {
                        current_maxBin = current_minBin + (this.pb_2DMap.Height * current_valuesPerPixelY) - 1;
                        this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                    }
                    else
                    {
                        if (current_valuesPerPixelY < -5)
                        {
                                this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.EmptyCircle;
                        }
                        else
                            this.waveform_TOFPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
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

                        this.pb_2DMap.Size = new Size(this.pb_2DMap.Width, this.pb_2DMap.Height);

                        // Identify the picture frame with my new Bitmap.
                        if (this.pb_2DMap.Image == null)
                        {
                            pb_2DMap.Image = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                            bitmap = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
                        }

                        // Spit out the data to screen
                        this.DrawBitmap(this.data_2D, this.data_maxIntensity);

                        this.pb_2DMap.Size = new Size(this.pb_2DMap.Width, this.plot_TOF.PlotAreaBounds.Height);
                    }
                    else
                    {
                        try
                        {
                            // MessageBox.Show("help me: "+ frame_index.ToString());
                            Generate2DIntensityArray(frame_index);
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
                    int tof_bin = ((current_valuesPerPixelY > 0) ? this.current_minBin + ((this.pb_2DMap.Height - e.Y - 1) * current_valuesPerPixelY) : this.current_minBin + ((this.pb_2DMap.Height - e.Y - 1) / -current_valuesPerPixelY));

                    // this is required to match with the MZ values
                    tof_bin--;   // wfd:  This is a Cheat!!! not sure what side of this belongs MZ or TOF

                    this.lbl_CursorTOF.Text = (tof_bin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4).ToString();
                    this.lbl_CursorMZ.Text = this.UIMF_DataReader.mz_Calibration.TOFtoMZ((float)(tof_bin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin)).ToString();
                }
                else
                {
                    // Much more difficult to find where the mz <-> TOF index correlation
                    //
                    // linearize the mz and find the cursor.
                    // calculate the mz, then convert to TOF for all the values.
                    double mzMax = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);
                    double mzMin = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);

                    double diffMZ = mzMax - mzMin;
                    double rangeTOF = this.current_maxBin - this.current_minBin;
                    double indexY = (current_valuesPerPixelY > 0) ? (this.pb_2DMap.Height - e.Y - 1) * current_valuesPerPixelY : (this.pb_2DMap.Height - e.Y - 1) / (-current_valuesPerPixelY);
                    double mz = (indexY / rangeTOF) * diffMZ + mzMin;
                    double tof_value = this.UIMF_DataReader.mz_Calibration.MZtoTOF(mz);

                    this.lbl_CursorMZ.Text = mz.ToString();
                    this.lbl_CursorTOF.Text = (tof_value * 1e-4).ToString(); // convert to usec
                }

                this.lbl_TimeOffset.Text = "Time Offset = " + this.uimf_GlobalParameters.TimeOffset.ToString() + " nsec";

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

        #region Drawing

        // Create an image out of the data array
        protected virtual void DrawBitmap(int[][] new_data2D, int new_maxIntensity)
        {
            if (this.flag_collecting_data)
            {
                return;
            }

            int perPixelY = 1; // this.current_valuesPerPixelY;
            int perPixelX = 1; // this.current_valuesPerPixelX;
            int pos_X = 0;
            if (new_data2D.Length > this.pb_2DMap.Width)
                perPixelX = 1;
            else
                perPixelX = -(this.pb_2DMap.Width / new_data2D.Length);

            if (this.current_valuesPerPixelY >= 0)
                perPixelY = 1;
            else
                perPixelY = this.current_valuesPerPixelY;

            LockBitmap();

            int threshold = Convert.ToInt32(this.slide_Threshold.Value) - 1;
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
                    //if ((((yMax - y) * -perPixelY) - 1) > this.pb_2DMap.Height)
                    //    continue;

                    // Important to ensure each scan line begins at a pixel, not halfway into a pixel, e.g.
                    PixelData* pPixel = (perPixelY > 0) ? PixelAt(0, yMax - y - 1) :
                            PixelAt(0, ((yMax - y) * -perPixelY) - 1);
                    pos_X = 0;
                    for (int x = 0; (x < new_data2D.Length) && (pos_X - perPixelX < this.pb_2DMap.Width); x++)
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
                        for (int i = 1; (i < -perPixelX) && (pos_X < this.pb_2DMap.Width); i++)
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
                            if ((yMax - y) * -perPixelY - i < this.pb_2DMap.Height)
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

            UnlockBitmap();
        }

        private PixelData* PixelAt(int x, int y)
        {
            return (PixelData*)(pBase + (y * pixel_width) + (x * sizeof(PixelData)));
        }

        private void LockBitmap()
        {

            Rectangle bounds = new Rectangle(0, 0, this.pb_2DMap.Width, this.pb_2DMap.Height);
            // MessageBox.Show("this.plot_Width: " + this.plot_Width.ToString());

            // Figure out the number of bytes in a row
            // This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length. 
            pixel_width = this.pb_2DMap.Width * sizeof(PixelData);
            if (pixel_width % 4 != 0)
            {
                pixel_width = 4 * (pixel_width / 4 + 1);
            }
            //MessageBox.Show("pixel_width: " + pixel_width.ToString());

            tmp_Bitmap = new Bitmap(this.pb_2DMap.Width, this.pb_2DMap.Height);
            bitmapData = tmp_Bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        private void UnlockBitmap()
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

            this.pb_2DMap.Image = this.bitmap;
            // this.pb_2DMap.Refresh();
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

#if false
        private void DrawInterpolation(Graphics g)
        {
            if (_interpolation_points.Count == 4)
            {
                // Create the convex shape out of the points:

            }
        }
#endif

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

            this.progress_ReadingFile.Top = this.pb_2DMap.Top + this.pb_2DMap.Height / 2;
            this.progress_ReadingFile.Left = this.pb_2DMap.Left;
            this.progress_ReadingFile.Width = this.pb_2DMap.Width;
            this.progress_ReadingFile.Maximum = (this.uimf_GlobalParameters.NumFrames / Convert.ToInt32(this.num_FrameCompression.Value)) + 1;
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
                //	plot_Mobility.Width = pb_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;

                if (current_valuesPerPixelX < -5)
                {
                    if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                        this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.None;
                    else
                        this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.EmptyCircle;
                }
                else
                    this.waveform_MobilityPlot.PointStyle = NationalInstruments.UI.PointStyle.None;

                plot_Mobility.XMax = this.pb_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;

                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    if (this.minFrame_Chromatogram < 1)
                    {
                        this.maxFrame_Chromatogram -= this.minFrame_Chromatogram;
                        this.minFrame_Chromatogram = 1;
                    }

                    this.flag_enterMobilityRange = true;
#if !NEEDS_WORK
                    this.maxFrame_Chromatogram = this.UIMF_DataReader.load_Frame((int)this.slide_FrameSelect.Range.Maximum);
                    this.num_maxMobility.Value = this.num_maxMobility.Maximum = this.maxFrame_Chromatogram;
#else // needs work
                    if (this.minFrame_Chromatogram < 0)
                        this.minFrame_Chromatogram = 0;

                    this.num_minMobility.Value = Convert.ToDecimal(this.minFrame_Chromatogram);
                    if (this.num_minMobility.Value + this.pb_2DMap.Width > this.num_FrameSelect.Maximum)
                    {
                        this.minFrame_Chromatogram = Convert.ToInt32(this.num_FrameSelect.Maximum) - this.pb_2DMap.Width;
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
                        this.num_maxMobility.Value = this.num_minMobility.Value + (this.pb_2DMap.Width / -this.chromatogram_valuesPerPixelX); // Convert.ToDecimal(tic_Mobility.Length);
                    else
                        this.num_maxMobility.Value = this.num_minMobility.Value + this.pb_2DMap.Width; // Convert.ToDecimal(tic_Mobility.Length);
#endif
                    this.flag_enterMobilityRange = false;

                    //MessageBox.Show("OK");

                    // MessageBox.Show(this.uimf_FrameParameters.Accumulations.ToString());
                    if (this.mean_TOFScanTime == 0)
                    {
                        this.plot_Mobility.PlotY(tic_Mobility, (double)0, 1.0 * Convert.ToDouble(this.num_FrameCompression.Value));
                    }
                    else
                    {
                        increment_MobilityValue = this.mean_TOFScanTime * (this.maximum_Mobility + 1) * this.UIMF_DataReader.m_frameParameters.Accumulations / 1000000.0 / 1000.0;
                        this.plot_Mobility.PlotY(tic_Mobility, (double)this.minFrame_Chromatogram * increment_MobilityValue, increment_MobilityValue);
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
                        this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F0");
                        this.plot_Mobility.PlotY(tic_Mobility, 0, this.current_maxMobility - this.current_minMobility + 1, min_MobilityValue, increment_MobilityValue);
                    }
                    else
                    {
                        // these values are used to prevent the values from changing during the plotting... yikes!
                        min_MobilityValue = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
                        increment_MobilityValue = mean_TOFScanTime / 1000000.0;
                        this.xAxis_Mobility.MajorDivisions.LabelFormat = new NationalInstruments.UI.FormatString(NationalInstruments.UI.FormatStringMode.Numeric, "F2");
                        this.plot_Mobility.PlotY(tic_Mobility, min_MobilityValue, increment_MobilityValue);
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

        private void plot_TOF_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenu_TOF.Show(this, new Point(e.X + plot_TOF.Left, e.Y + plot_TOF.Top));
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
#if false
                //   _tof_max = int.MinValue;
                //    _tof_min = int.MaxValue;

                for (int i = 0; i < tof.Length; i++)
                {
                    s_data[i] = tof[i];
                    /*     if(s_data[i] > _tof_max)
                             _tof_max = (int)tof[i];
                         else if(s_data[i] < _tof_min)
                             _tof_min = (int)tof[i];
                     */
                }
#endif
                this.flag_enterBinRange = true;

                if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
                {
                    if (this.minMobility_Chromatogram < 0)
                        this.minMobility_Chromatogram = 0;
                    this.num_minBin.Value = Convert.ToDecimal(this.minMobility_Chromatogram);

                    if (this.maxMobility_Chromatogram > this.UIMF_DataReader.m_frameParameters.Scans - 1)
                        this.maxMobility_Chromatogram = this.UIMF_DataReader.m_frameParameters.Scans - 1;
                    this.num_maxBin.Value = Convert.ToDecimal(this.maxMobility_Chromatogram);

                    if (this.flag_viewMobility)
                        this.plot_TOF.PlotX(tic_TOF, this.minMobility_Chromatogram, 1.0);
                    else
                        this.plot_TOF.PlotX(tic_TOF, this.minMobility_Chromatogram, this.UIMF_DataReader.m_frameParameters.AverageTOFLength / 1000000.0);
                }
                else
                {
                    if (flag_display_as_TOF)
                    {
                        double min_TOF = (this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double max_TOF = (this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double increment_TOF = (max_TOF - min_TOF) / (double)(this.pb_2DMap.Height);
                        if (current_valuesPerPixelY < 0)
                            increment_TOF *= (double)-current_valuesPerPixelY;

                        this.num_maxBin.Value = Convert.ToDecimal(max_TOF);
                        this.num_minBin.Value = Convert.ToDecimal(min_TOF);
                        this.num_minBin.Increment = this.num_maxBin.Increment = Convert.ToDecimal((max_TOF - min_TOF) / 3);

                        min_BinValue = min_TOF;
                        increment_BinValue = increment_TOF;

                        // this.plot_TOF.Update();
                        // this.plot_TOF.Enabled = false;
                        this.plot_TOF.PlotX(tic_TOF, min_BinValue, increment_BinValue); //wfd
                    }
                    else
                    {
                        // Confirmed working... 061213
                        // Much more difficult to find where the mz <-> TOF index correlation
                        double mzMin = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_minBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);
                        double mzMax = this.UIMF_DataReader.mz_Calibration.TOFtoMZ(this.current_maxBin * this.UIMF_DataReader.TenthsOfNanoSecondsPerBin);

                        double increment_TOF = (mzMax - mzMin) / (double)this.pb_2DMap.Height;
                        if (current_valuesPerPixelY < 0)
                            increment_TOF *= (double)-current_valuesPerPixelY;

                        this.num_maxBin.Value = Convert.ToDecimal(mzMax);
                        this.num_minBin.Value = Convert.ToDecimal(mzMin);

                        min_BinValue = mzMin;
                        increment_BinValue = increment_TOF;

                        //  this.plot_TOF.Update();
                        //  this.plot_TOF.Enabled = false;
                        this.plot_TOF.PlotX(tic_TOF, min_BinValue, increment_BinValue); //wfd
                    }
                }
                //   this.plot_TOF.Enabled = true;
#if false
                if (Math.Abs(this.plot_TOF.PlotAreaBounds.Height- this.pb_2DMap.Height) > 5)
                {
                    this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
                    this.flag_update2DGraph = true;
                }
#endif
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
            if (this.slide_FrameSelect.Value <= this.slide_FrameSelect.Range.Minimum) // frame index starts at 0
                return;

            this.pb_PlayLeftOut.Hide();
            this.pb_PlayRightOut.Show();

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = -(Convert.ToInt32(this.num_FrameRange.Value) / 3) - 1;
            this.slide_FrameSelect.Value += this.Cinemaframe_DataChange;
        }

        private void pb_PlayRightOut_Click(object sender, EventArgs e)
        {
            if (this.slide_FrameSelect.Value >= this.slide_FrameSelect.Range.Maximum)
                return;

            this.pb_PlayRightOut.Hide();
            this.pb_PlayLeftOut.Show();

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = (Convert.ToInt32(this.num_FrameRange.Value) / 3) + 1;
            if (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange > Convert.ToInt32(this.slide_FrameSelect.Range.Maximum))
                this.slide_FrameSelect.Value = this.slide_FrameSelect.Range.Maximum - Convert.ToInt32(this.num_FrameRange.Value);
            else
            {
                if (this.slide_FrameSelect.Value + this.Cinemaframe_DataChange > this.slide_FrameSelect.Range.Maximum)
                    this.slide_FrameSelect.Value = this.slide_FrameSelect.Range.Maximum - this.Cinemaframe_DataChange;
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

#if SCROLLBAR_BUSY
        private void enter_Scrollbar(object sender, EventArgs e)
        {
            this.flag_ScrollbarBusy = true;
        }
        private void leave_Scrollbar(object sender, EventArgs e)
        {
            this.flag_ScrollbarBusy = false;
        }
#endif
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
                this.plot_TOF.PlotAreaColor = Color.AntiqueWhite;
            else
                this.plot_TOF.PlotAreaColor = Color.White;

            this.flag_update2DGraph = true;
        }

#if false
        // /////////////////////////////////////////////////////////////
        // Drag-Drop IMF file onto the graph
        //
        private void IonMobilityDataView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int i;
            int j;
            string temp;

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
                if (File.Exists(files[i]))
                    this.lb_DragDropFiles.Items.Add(files[i]);
            }

            this.lb_DragDropFiles.Show();
            this.lb_DragDropFiles.Text = "Plot DragDrop:  " + Path.GetFileName(files[0]);

            this.cb_Exclusive.Show();
            this.pb_PlayDownOut.Show();
            this.pb_PlayDownIn.Show();
            this.pb_PlayUpOut.Show();
            this.pb_PlayUpIn.Show();
        }

        private void IonMobilityDataView_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
#endif

        private void lb_DragDropFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.flag_update2DGraph = true;
        }

        private void btn_Refresh_Click(object sender, EventArgs e)
        {
            this.tab_DataViewer.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));

            this.plot_Mobility.Dispose();
            this.plot_TOF.Dispose();
            this.waveform_MobilityPlot.Dispose();
            this.xAxis_Mobility.Dispose();
            this.yAxis_Mobility.Dispose();
            this.xyCursor2.Dispose();
            this.waveform_MobilityPlot.Dispose();
            this.waveform_TOFPlot.Dispose();
            this.xAxis_TOF.Dispose();
            this.yAxis_TOF.Dispose();

            this.xyCursor2 = new NationalInstruments.UI.XYCursor();
            this.waveform_MobilityPlot = new NationalInstruments.UI.WaveformPlot();
            this.xAxis_Mobility = new NationalInstruments.UI.XAxis();
            this.yAxis_Mobility = new NationalInstruments.UI.YAxis();
            this.waveform_MobilityPlot = new NationalInstruments.UI.WaveformPlot();
            this.waveform_TOFPlot = new NationalInstruments.UI.WaveformPlot();
            this.xAxis_TOF = new NationalInstruments.UI.XAxis();
            this.yAxis_TOF = new NationalInstruments.UI.YAxis();
            this.plot_Mobility = new Utilities.PointAnnotationGraph();
            this.plot_TOF = new NationalInstruments.UI.WindowsForms.WaveformGraph();
            // 
            // xyCursor2
            // 
            this.xyCursor2.HorizontalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            this.xyCursor2.Plot = this.waveform_MobilityPlot;
            this.xyCursor2.PointStyle = NationalInstruments.UI.PointStyle.Plus;
            this.xyCursor2.VerticalCrosshairMode = NationalInstruments.UI.CursorCrosshairMode.None;
            // 
            // waveform_MobilityPlot
            // 
            this.waveform_MobilityPlot.LineColor = System.Drawing.Color.Crimson;
            this.waveform_MobilityPlot.PointColor = System.Drawing.Color.Salmon;
            this.waveform_MobilityPlot.XAxis = this.xAxis_Mobility;
            this.waveform_MobilityPlot.YAxis = this.yAxis_Mobility;
            // 
            // xAxis_Mobility
            // 
            this.xAxis_Mobility.Mode = NationalInstruments.UI.AxisMode.AutoScaleExact;

            // 
            // waveform_TOFPlot
            // 
            this.waveform_TOFPlot.LineColor = System.Drawing.Color.DarkBlue;
            this.waveform_TOFPlot.PointColor = System.Drawing.Color.DarkTurquoise;
            this.waveform_TOFPlot.XAxis = this.xAxis_TOF;
            this.waveform_TOFPlot.YAxis = this.yAxis_TOF;
            // 
            // xAxis_TOF
            // 
            this.xAxis_TOF.Inverted = true;
            this.xAxis_TOF.MinorDivisions.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.xAxis_TOF.MinorDivisions.GridLineStyle = NationalInstruments.UI.LineStyle.Dot;
            this.xAxis_TOF.MinorDivisions.GridVisible = true;
            this.xAxis_TOF.MinorDivisions.TickVisible = true;
            this.xAxis_TOF.Position = NationalInstruments.UI.XAxisPosition.Top;
            // 
            // yAxis_TOF
            // 
            this.yAxis_TOF.Mode = NationalInstruments.UI.AxisMode.AutoScaleExact;
            // 
            // plot_DriftPlot
            // 
            this.plot_Mobility.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_Mobility.Border = NationalInstruments.UI.Border.RaisedLite;
            this.plot_Mobility.Cursors.AddRange(new NationalInstruments.UI.XYCursor[] {
            this.xyCursor2});
            this.plot_Mobility.Location = new System.Drawing.Point(242, 572);
            this.plot_Mobility.Name = "plot_DriftPlot";
            this.plot_Mobility.PlotAreaColor = System.Drawing.Color.White;
            this.plot_Mobility.Plots.AddRange(new NationalInstruments.UI.WaveformPlot[] {
            this.waveform_MobilityPlot});
            this.plot_Mobility.Size = new System.Drawing.Size(510, 111);
            this.plot_Mobility.TabIndex = 24;
            this.plot_Mobility.XAxes.AddRange(new NationalInstruments.UI.XAxis[] {
            this.xAxis_Mobility});
            this.plot_Mobility.YAxes.AddRange(new NationalInstruments.UI.YAxis[] {
            this.yAxis_Mobility});
            this.plot_Mobility.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_Mobility_MouseDown);
            this.plot_Mobility.RangeChanged += new Utilities.RangeEventHandler(this.OnPlotTICRangeChanged);
            // 
            // plot_TOF
            // 
            this.plot_TOF.BackColor = System.Drawing.Color.Gainsboro;
            this.plot_TOF.Border = NationalInstruments.UI.Border.RaisedLite;
            this.plot_TOF.Location = new System.Drawing.Point(14, 52);
            this.plot_TOF.Name = "plot_TOF";
            this.plot_TOF.PlotAreaColor = System.Drawing.Color.White;
            this.plot_TOF.Plots.AddRange(new NationalInstruments.UI.WaveformPlot[] {
            this.waveform_TOFPlot});
            this.plot_TOF.SelectionColor = System.Drawing.Color.Lavender;
            this.plot_TOF.Size = new System.Drawing.Size(204, 511);
            this.plot_TOF.TabIndex = 20;
            this.plot_TOF.XAxes.AddRange(new NationalInstruments.UI.XAxis[] {
            this.xAxis_TOF});
            this.plot_TOF.YAxes.AddRange(new NationalInstruments.UI.YAxis[] {
            this.yAxis_TOF});
            this.plot_TOF.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plot_TOF_MouseDown);
            // label the axis'
            //
            // left plot
            this.yAxis_TOF.Caption = "Time of Flight";
            this.yAxis_TOF.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));

            // bottom plot
            this.xAxis_Mobility.Caption = "Mobility - Scans";
            this.xAxis_Mobility.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.yAxis_Mobility.Caption = "Drift Intensity";
            this.yAxis_Mobility.CaptionFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.yAxis_Mobility.Position = NationalInstruments.UI.YAxisPosition.Right;

            this.tab_DataViewer.Controls.Add(this.plot_TOF);
            this.tab_DataViewer.Controls.Add(this.plot_Mobility);
            this.plot_TOF.Show();

            this.plot_TOF.Width = 200;
            this.plot_Mobility.Height = 150;

            this.plot_axisMobility(this.data_driftTIC);
            this.plot_axisTOF(this.data_tofTIC);

            // MessageBox.Show("refresh");
            this.IonMobilityDataView_Resize((object)null, (EventArgs)null);
            this.btn_Refresh.Enabled = true;
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
                for (int i = 1; i <= ((this.uimf_GlobalParameters.NumFrames - merge) / step) + 1; i++)
                {
                    //  MessageBox.Show((i * step).ToString());
                    //  continue;
                    this.slide_FrameSelect.Value = ((i - 1) * step) + merge;

                    this.Graph_2DPlot((int) this.slide_FrameSelect.Value);
                    this.Update();

                    file_merge_IMF = Path.Combine(directory, name + ".Accum_" + i.ToString() + ".IMF");
                    this.save_MergedFrame(file_merge_IMF, true, i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            this.uimf_GlobalParameters.NumFrames = ((this.uimf_GlobalParameters.NumFrames - merge) / step) + 1;
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

        private void cb_MaxScanValue_CheckedChanged(object sender, EventArgs e)
        {
            this.flag_update2DGraph = true;
        }

        private void btn_ShowChromatogram_Click(object sender, EventArgs e)
        {
#if false
            if (this.ptr_Experiment.NumFrames < 2)
            {
                if (!((this.cb_Chromatogram.CheckState == CheckState.Checked) || (this.cb_Chromatogram.CheckState == CheckState.Indeterminate)))
                    return;

                MessageBox.Show("Chromatogram's are not available with less than 2 frames");

                this.cb_Chromatogram.Checked = false;
                this.cb_Chromatogram.Enabled = false;
                return;
            }
#endif
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

            this.flag_update2DGraph = true;

            this.current_frame_index = (int)this.slide_FrameSelect.Value;
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

                this.pb_2DMap.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            else
            {
                this.rb_CompleteChromatogram.Enabled = true;

                this.pb_2DMap.SizeMode = PictureBoxSizeMode.Normal;
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

                this.pb_2DMap.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            else
            {
                this.rb_PartialChromatogram.Enabled = true;

                this.pb_2DMap.SizeMode = PictureBoxSizeMode.Normal;
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
            if (this.uimf_GlobalParameters.NumFrames < 2)
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

                this.current_frame_index = (int)this.slide_FrameSelect.Value;
                this.plot_Mobility.StopAnnotating(true);

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                this.pb_PlayLeftIn.Hide();
                this.pb_PlayLeftOut.Hide();
                this.pb_PlayRightIn.Hide();
                this.pb_PlayRightOut.Hide();

                this.slide_FrameSelect.Hide();
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
                if (this.uimf_GlobalParameters.NumFrames > 1)
                {
                    this.slide_FrameSelect.Show();

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
#if false
            int bin_compression; // due to dropping down to .25 nsec bins
#endif

            file_accum_IMF = Path.Combine(Path.GetDirectoryName(this.UIMF_DataFile), Path.GetFileNameWithoutExtension(this.UIMF_DataFile) + ".Accum_" + this.UIMF_DataReader.m_frameParameters.FrameNum.ToString() + ".IMF");

            num_BinTICs = new int[this.UIMF_DataReader.m_frameParameters.Scans];
            bytes_Bin = new int[this.UIMF_DataReader.m_frameParameters.Scans];

#if false
            if (this.uimf_GlobalParameters.BinWidth == .25)
            {
                bin_width = 1;
                bin_compression = 4;
            }
            else
            {
                bin_width = this.uimf_GlobalParameters.BinWidth;
                bin_compression = 1;
            }
#else
            bin_width = this.uimf_GlobalParameters.BinWidth;
#endif
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
            sw_IMF.WriteLine("TOFSpectra: " + this.UIMF_DataReader.m_frameParameters.Scans.ToString());
#if false
            sw_IMF.WriteLine("NumBins: " + (this.uimf_GlobalParameters.Bins / bin_compression).ToString());
#else
            sw_IMF.WriteLine("NumBins: " + this.uimf_GlobalParameters.Bins.ToString());
#endif
            sw_IMF.WriteLine("BinWidth: " + bin_width.ToString("0.00") + " ns");
            sw_IMF.WriteLine("Accumulations: " + this.UIMF_DataReader.m_frameParameters.Accumulations.ToString());
            sw_IMF.WriteLine("TimeOffset: " + this.uimf_GlobalParameters.TimeOffset.ToString());

            sw_IMF.WriteLine("CalibrationSlope: " + this.UIMF_DataReader.m_frameParameters.CalibrationSlope);
            sw_IMF.WriteLine("CalibrationIntercept: " + this.UIMF_DataReader.m_frameParameters.CalibrationIntercept);

            sw_IMF.WriteLine("FrameNumber: " + this.UIMF_DataReader.m_frameParameters.FrameNum.ToString());
            sw_IMF.WriteLine("AverageTOFLength: " + this.UIMF_DataReader.m_frameParameters.AverageTOFLength.ToString("0.00") + " ns");

            if ((this.UIMF_DataReader.m_frameParameters.IMFProfile == null) || (this.UIMF_DataReader.m_frameParameters.IMFProfile.Length == 0))
            {
                MessageBox.Show("menuitem_SaveIMF_Click - putting in IMFProfile...");
                sw_IMF.WriteLine("MultiplexingProfile: 4Bit_24OS.txt"); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");
            }
            else
                sw_IMF.WriteLine("MultiplexingProfile: " + this.UIMF_DataReader.m_frameParameters.IMFProfile); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");

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
            bw_IMF.Write((int)this.UIMF_DataReader.m_frameParameters.Scans);

            // Write counter_TIC values and the channel data size (Nodes * sizeof(Node values)]for each channel
            // Each record is made up of [Int32 TOFValue, Int16 Count]
            for (i = 0; i < this.UIMF_DataReader.m_frameParameters.Scans * 2; i++)
                bw_IMF.Write(Convert.ToInt32(0));

            // MessageBox.Show("Collected data: "+accumulated_data.GetLength(0).ToString() + " " + accumulated_data.GetLength(1).ToString());

            List<int> spectrum = new List<int>();
            List<int> bins = new List<int>();

            int[] spectrum_array;
            int[] bins_array;

            num_BinTICs = new int[this.UIMF_DataReader.m_frameParameters.Scans];
            bytes_Bin = new int[this.UIMF_DataReader.m_frameParameters.Scans];

            //MessageBox.Show(this.uimf_FrameParameters.FrameNum.ToString());
            for (k = 0; k < this.UIMF_DataReader.m_frameParameters.Scans; k++)
            {
                counter_TIC = 0;
                counter_bin = 0;

                spectrum.Clear();
                bins.Clear();
                try
                {
                    this.UIMF_DataReader.GetSpectrum(this.UIMF_DataReader.get_FrameIndex(this.UIMF_DataReader.m_frameParameters.FrameNum), k, spectrum, bins);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("menuitem_SaveIMF_Click UIMF_DataReader: " + ex.ToString());
                }

               // for (j = 0; j < full_spectrum.Length; j++)
               //     full_spectrum[j] = 0;

                spectrum_array = spectrum.ToArray();
                bins_array = bins.ToArray();

                for (j = 0; j < spectrum_array.Length; j++)
                {
                    counter_bin++;
                    counter_TIC += bins_array[j];
                    bw_IMF.Write((spectrum_array[j] - this.uimf_GlobalParameters.TimeOffset) * 10); // * binWidth);
                    bw_IMF.Write(bins_array[j]);
                }

                num_BinTICs[k] = counter_TIC;
                bytes_Bin[k] = counter_bin;
            }


            // Go back to the Escape Position, then pass the number of TOFSpectraPerFrame
            bw_IMF.Seek((int)escape_position + 4, SeekOrigin.Begin);

            for (k = 0; k < this.UIMF_DataReader.m_frameParameters.Scans; k++)
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
                this.UIMF_DataReader.mz_Calibration.k = (float)Convert.ToDouble(this.tb_CalA.Text);
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
                this.UIMF_DataReader.mz_Calibration.t0 = (float)Convert.ToDouble(this.tb_CalT0.Text);
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
            if ((Convert.ToDouble(this.tb_CalA.Text) != this.UIMF_DataReader.mz_Calibration.k) ||
                (Convert.ToDouble(this.tb_CalT0) != this.UIMF_DataReader.mz_Calibration.t0))
            {
               // this.UIMF_DataReader.m_frameParameters.CalibrationSlope = Convert.ToDouble(this.tb_CalA.Text); //this.UIMF_DataReader.mz_Calibration.k * 10000.0;
              //  this.UIMF_DataReader.m_frameParameters.CalibrationIntercept = Convert.ToDouble(this.tb_CalT0.Text); // this.UIMF_DataReader.mz_Calibration.t0 / 10000.0;
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
            this.tb_CalA.Text = this.UIMF_DataReader.mz_Calibration.k.ToString("E");
            this.tb_CalT0.Text = this.UIMF_DataReader.mz_Calibration.t0.ToString("E");
            this.lbl_CalibratorType.Text = this.UIMF_DataReader.mz_Calibration.Description;

            this.pnl_postProcessing.set_ExperimentalCoefficients(this.UIMF_DataReader.mz_Calibration.k*10000.0, this.UIMF_DataReader.mz_Calibration.t0/10000.0);
        }

        private void btn_setCalDefaults_Click(object sender, System.EventArgs e)
        {

            this.Enabled = false;

            this.UIMF_DataReader.updateAll_CalibrationCoefficients((float)(Convert.ToSingle(this.tb_CalA.Text) * 10000.0), (float)(Convert.ToSingle(this.tb_CalT0.Text) / 10000.0));

            this.update_CalibrationCoefficients();

            this.Enabled = true;
            this.flag_update2DGraph = true;

            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();
        }

        private void btn_revertCalDefaults_Click(object sender, System.EventArgs e)
        {
            this.UIMF_DataReader.reset_FrameParameters();

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
            this.UIMF_DataReader.update_CalibrationCoefficients(this.current_frame_index, (float)this.pnl_postProcessing.Calculated_Slope, 
                (float)this.pnl_postProcessing.Calculated_Intercept);

            this.update_CalibrationCoefficients();

            this.pnl_postProcessing.InitializeCalibrants(this.uimf_GlobalParameters.BinWidth, this.pnl_postProcessing.Calculated_Slope, this.pnl_postProcessing.Calculated_Intercept);

            this.flag_update2DGraph = true;
        }
         
        private void btn_ApplyCalibration_Experiment_Click(object sender, EventArgs e)
        {
            //MessageBox.Show((Convert.ToDouble(this.tb_CalA.Text) * 10000.0).ToString() + "  " + this.pnl_postProcessing.Experimental_Slope.ToString());
            this.UIMF_DataReader.updateAll_CalibrationCoefficients((float)this.pnl_postProcessing.get_Experimental_Slope(), 
                (float)this.pnl_postProcessing.get_Experimental_Intercept());

            this.update_CalibrationCoefficients();

            this.pnl_postProcessing.InitializeCalibrants(this.uimf_GlobalParameters.BinWidth, this.pnl_postProcessing.get_Experimental_Slope(), this.pnl_postProcessing.get_Experimental_Intercept());

            this.flag_update2DGraph = true;
        }

        private void btn_CalibrateFrames_Click(object sender, EventArgs e)
        {
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

        public void AutoCalibrateExperiment()
        {
            int num_calibration_frames;
            int i;
            double[] cal_a;
            double[] cal_t0;
            int[] total_calibrants_matched;
            int max_calibrants = 0;
            int index_maxcalibrants = -1;

            this.cb_FrameType.SelectedIndex = 3;

            num_calibration_frames = this.UIMF_DataReader.get_NumFrames(this.cb_FrameType.SelectedIndex);
            if (num_calibration_frames == 0)
                return;

            cal_a = new double[num_calibration_frames];
            cal_t0 = new double[num_calibration_frames];
            total_calibrants_matched = new int[num_calibration_frames];

            for (i = 0; i < num_calibration_frames; i++)
            {
                cal_a[i] = 0.0;
                cal_t0[i] = 0.0;
                total_calibrants_matched[i] = 0;

                this.current_frame_index = i;
                this.Graph_2DPlot(this.current_frame_index);

                this.Calibrate_Frame(i, out cal_a[i], out cal_t0[i], out total_calibrants_matched[i]);
            }

            for (i = 0; i < num_calibration_frames; i++)
            {
                if (total_calibrants_matched[i] > max_calibrants)
                {
                    max_calibrants = total_calibrants_matched[i];
                    index_maxcalibrants = i;
                }
            }

            if (max_calibrants > 4)
                this.UIMF_DataReader.updateAll_CalibrationCoefficients((float) cal_a[index_maxcalibrants], (float) cal_t0[index_maxcalibrants]);
        }

        private void tick_Calibrate()
        {
            double slope;
            double intercept;
            int total_calibrants_matched;

            bool flag_CalibrateExperiment = false;

            this.Update();

            this.slide_FrameSelect.Value = this.current_frame_index;
            this.Update();

            this.Calibrate_Frame(this.current_frame_index, out slope, out intercept, out total_calibrants_matched);

            if (double.IsNaN(slope) || double.IsNaN(intercept))
            {
                DialogResult dr = MessageBox.Show(this, "Calibration failed.\n\nShould I continue?", "Calibration failed", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                    return;
            }
            else if (flag_CalibrateExperiment)
            {
                this.UIMF_DataReader.update_CalibrationCoefficients(this.current_frame_index, (float) slope, (float) intercept);
            }
            else if (slope <= 0)
            {
                MessageBox.Show(this, "Calibration Failed");
                return;
            }
            else
            {
                this.UIMF_DataReader.mz_Calibration.k = slope / 10000.0;
                this.UIMF_DataReader.mz_Calibration.t0 = intercept * 10000.0;
                this.update_CalibrationCoefficients();
            }

            this.Update();

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
            int[] spectrum = new int[this.uimf_GlobalParameters.Bins];
            int[] max_spectrum = new int[this.uimf_GlobalParameters.Bins];
            int[] bins = new int[this.uimf_GlobalParameters.Bins];

            double slope = this.UIMF_DataReader.m_frameParameters.CalibrationSlope;
            double intercept = this.UIMF_DataReader.m_frameParameters.CalibrationIntercept;

            int CalibrantCountMatched = 100;
            int CalibrantCountValid = 0;
            double AverageAbsoluteValueMassError = 0.0;
            double AverageMassError = 0.0;

#if COMPRESS_TO_100K
            if (this.uimf_GlobalParameters.BinWidth == .25)
                compression = 4;
            else
#endif
                compression = 1;

            calibration_slope = -1.0;
            calibration_intercept = -1.0;
            total_calibrants_matched = 0;

            summed_spectrum = new double[this.uimf_GlobalParameters.Bins / compression];
            flag_above_noise = new bool[this.uimf_GlobalParameters.Bins / compression];

            while (CalibrantCountMatched > 4)
            {
                // clear arrays
                for (i = 0; i < this.uimf_GlobalParameters.Bins / compression; i++)
                {
                    flag_above_noise[i] = false;
                    max_spectrum[i] = 0;
                    summed_spectrum[i] = 0;
                    max_spectrum[i] = 0;
                }

                // sum spectrum and get max bin
                for (scans = 0; scans < this.UIMF_DataReader.m_frameParameters.Scans; scans++)
                {
                    uimf_bins = this.UIMF_DataReader.GetSpectrum(frame_index, scans, spectrum, bins);
                    for (j = 1; (j < uimf_bins); j++)
                    {
                        if (bins[j] >= this.uimf_GlobalParameters.Bins)
                            break;
                        summed_spectrum[bins[j] / compression] += spectrum[j];

                        if (max_spectrum[bins[j] / compression] < summed_spectrum[bins[j] / compression])
                        {
                            max_spectrum[bins[j] / compression] = (int)summed_spectrum[bins[j] / compression];

                            if (maximum_spectrum < summed_spectrum[bins[j] / compression])
                                maximum_spectrum = (int)summed_spectrum[bins[j] / compression];
                        }
                    }
                }

                // determine noise level and filter summed spectrum
                for (j = NOISE_REGION / 2; (j < (this.uimf_GlobalParameters.Bins / compression) - NOISE_REGION); j++)
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
                for (i = 1; i < this.uimf_GlobalParameters.Bins / compression; i++)
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
                for (i = 0; (i < (this.uimf_GlobalParameters.Bins / compression) - 1) && (compressed_bins < above_noise_bins + added_zeros); i++)
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
                this.pnl_postProcessing.CreateArrays(summed_spectrum, nonzero_intensities, nonzero_bins, this.uimf_GlobalParameters.BinWidth * (double)compression, this.uimf_GlobalParameters.Bins / compression, slope, intercept);

                try
                {
                    this.pnl_postProcessing.OnInternalCalibration(CalibrationType.STANDARD, Instrument.AGILENT_TDC, this.UIMF_DataReader.m_frameParameters.Scans);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

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
                    break;
                }
                else if (CalibrantCountMatched > 4)
                    this.pnl_postProcessing.disable_CalibrantMaxPPMError();
            }
        }

        private void btn_Clean_Click(object sender, EventArgs e)
        {
            MessageBox.Show("not sure what this does.  Needs work.  wfd 02/22/11");

            string filename = "c:\\IonMobilityData\\Gordon\\Calibration\\QC\\8pep_10fr_600scans_01_0000\\"+Path.GetFileNameWithoutExtension(this.UIMF_DataFile) + "_clean.UIMF";

            if (File.Exists(filename))
                File.Delete(filename);

            DataWriter uimf_writer = new DataWriter();
            FrameParameters fp = new FrameParameters();
            GlobalParameters gp = new GlobalParameters();
            int uimf_bins;
           
            uimf_writer.OpenUIMF(filename);
            uimf_writer.CreateTables("int");


            gp = this.UIMF_DataReader.GetGlobalParameters();
            MessageBox.Show("gp: " + gp.NumFrames.ToString());

            for (int i = 1; i <= gp.NumFrames; i++)
            {
                fp = this.UIMF_DataReader.GetFrameParameters(i);

                uimf_writer.InsertFrame(fp);

                for (int j = 0; j < this.UIMF_DataReader.m_frameParameters.Scans; j++)
                {
                    int[] binList = new int[410000];
                    int[] intensityList = new int[410000];

                    uimf_bins = this.UIMF_DataReader.GetSpectrum(i, j, binList, intensityList);
                    int[] new_bins = new int[uimf_bins];
                    int[] new_intensities = new int[uimf_bins];

                    for (int k = 0; k < uimf_bins; k++)
                    {
                        new_bins[k] = binList[k] - 10000;
                        new_intensities[k] = intensityList[k];
                    }

                    uimf_writer.InsertScan(fp, j, new_bins, new_intensities, gp.BinWidth, 0);
                }
            }

            uimf_writer.CloseUIMF(filename);
            MessageBox.Show("created " + filename);
        }

        // ///////////////////////////////////////////////////////////////
        // Select FrameType
        //
        private void cb_FrameType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.flag_FrameTypeChanged = true;
            this.flag_update2DGraph = true;
        }

        private int current_frame_type = -1;
        private void Filter_FrameType(int frame_type)
        {
            if (this.current_frame_type == frame_type)
                return;

            int frame_count = 0;
            object[] read_values = new object[0];

            frame_count = this.UIMF_DataReader.set_FrameType(frame_type);
            this.current_frame_type = frame_type;
            this.current_frame_index = -1;

            Invoke(new ThreadStart(format_Screen));

            // Reinitialize
            _zoomX.Clear();
            _zoomBin.Clear();

            this.new_minBin = 0;
            this.new_minMobility = 0;

            this.new_maxBin = this.maximum_Bins = this.uimf_GlobalParameters.Bins-1;
            this.new_maxMobility = this.maximum_Mobility = this.UIMF_DataReader.m_frameParameters.Scans - 1;
           
            if (frame_count == 0)
                return;

            if (this.UIMF_DataReader.get_NumFrames(frame_type) > DESIRED_WIDTH_CHROMATOGRAM)
                this.num_FrameCompression.Value = this.UIMF_DataReader.get_NumFrames(frame_type) / DESIRED_WIDTH_CHROMATOGRAM;
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
            int frame_count = UIMF_DataReader.get_NumFrames(this.current_frame_type);

            if (frame_count == 0)
            {
                this.pb_2DMap.Visible = false;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = false;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = false;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = false;
                this.slide_FrameSelect.Visible = false;

                this.plot_TOF.ClearData();
                this.plot_Mobility.ClearData();

                this.lbl_FrameRange.Visible = false;
                this.num_FrameRange.Visible = false;

                return;
            }
            else
            {
                this.pb_2DMap.Visible = true;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = true;
                this.pb_PlayLeftIn.Visible = this.pb_PlayLeftOut.Visible = true;
                this.pb_PlayRightIn.Visible = this.pb_PlayRightOut.Visible = true;

                this.pb_2DMap.Visible = true;

                if (frame_count == 1)
                {
                    this.slide_FrameSelect.Hide();
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
                    if (!this.slide_FrameSelect.Visible)
                        this.slide_FrameSelect.Visible = true;
                    this.slide_FrameSelect.Range = new NationalInstruments.UI.Range(0, frame_count - 1);

                    this.lbl_FrameRange.Visible = false;
                    this.num_FrameRange.Visible = false;

                    this.pb_PlayLeftIn.Show();
                    this.pb_PlayLeftOut.Show();
                    this.pb_PlayRightIn.Show();
                    this.pb_PlayRightOut.Show();
                    this.num_FrameRange.Show();
                    this.lbl_FrameRange.Show();

                    this.slide_FrameSelect.Refresh();
                }
            }
        }

        private void RegistrySave(RegistryKey key)
        {
            using (RegistryKey sk = key.CreateSubKey(this.Name))
            {
            }
        }

        private void RegistryLoad(RegistryKey key)
        {
            try
            {
                using (RegistryKey sk = key.OpenSubKey(this.Name))
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

        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Decode Multiplexed UIMF File
        //
        UIMF_Demultiplexer.UIMF_Demultiplexer uimf_Demultiplexer;
        private void btn_DecodeMultiplexing_Click(object sender, EventArgs e)
        {
            this.Hide();
            if ((this.frame_progress == null) || this.frame_progress.IsDisposed)
            {
                this.frame_progress = new UIMF_File.Utilities.progress_Processing();
                this.frame_progress.btn_Cancel.Click += new EventHandler(btn_ProgressDecodeCancel_Click);
                this.frame_progress.Show();
            }

            this.uimf_Demultiplexer = new UIMF_Demultiplexer.UIMF_Demultiplexer(this.UIMF_DataReader, 
                Path.Combine(this.pnl_postProcessing.tb_SaveDecodeDirectory.Text, this.pnl_postProcessing.tb_SaveDecodeFilename.Text),
                this.frame_progress, true);
        }

        private void btn_ProgressDecodeCancel_Click(object obj, System.EventArgs e)
        {
            if (!this.Visible)
            {
                this.Show();
                if (this.uimf_Demultiplexer != null)
                    this.uimf_Demultiplexer.flag_Stopped = true;
            }
        }

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
            if (this.uimf_Demultiplexer != null)
                this.uimf_Demultiplexer.flag_Stopped = true;
        }

        private void Compress4GHzUIMF()
        {
            UIMFLibrary.GlobalParameters gp = this.UIMF_DataReader.GetGlobalParameters();
            UIMFLibrary.FrameParameters fp;
            string name_instrument; 
            int i;
            int j;
            int k;
            int current_frame;
            int[] current_intensities = new int[gp.Bins/4];

            List<int> list_Bins = new List<int>();
            List<int> list_Intensity = new List<int>();
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

            UIMFLibrary.DataWriter UIMF_Writer = new UIMFLibrary.DataWriter();
            UIMF_Writer = new UIMFLibrary.DataWriter();
            UIMF_Writer.OpenUIMF(UIMF_filename);
            UIMF_Writer.CreateTables(null);

            gp.BinWidth = 1;
            gp.Bins /= 4;
            UIMF_Writer.InsertGlobal(gp);

            // make sure the instrument name is in the right format - either QTOF or TOF
            name_instrument = gp.InstrumentName;
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
            for (current_frame = 0; ((current_frame < this.UIMF_DataReader.get_NumFramesCurrentFrameType()) && !this.frame_progress.flag_Stop); current_frame++)
            //for (current_frame = 0; ((current_frame < 100) && !this.frame_progress.flag_Stop); current_frame++)
            {
                this.frame_progress.SetValue(current_frame, (int)stop_watch.ElapsedMilliseconds);

                stop_watch.Reset();
                stop_watch.Start();

                fp = this.UIMF_DataReader.GetFrameParameters(current_frame);
                UIMF_Writer.InsertFrame(fp);

                for (i = 0; i < fp.Scans; i++)
                {
                    list_Bins.Clear();
                    list_Intensity.Clear();
                    for (j = 0; j < gp.Bins; j++)
                    {
                        current_intensities[j] = 0;
                    }

                    this.UIMF_DataReader.GetSpectrum(current_frame, i, list_Bins, list_Intensity);

                    for (j = 0; j < list_Bins.Count; j++)
                        current_intensities[list_Bins[j] / 4] += list_Intensity[j];

                    list_Bins.Clear();
                    list_Intensity.Clear();
                    for (j=0; j<gp.Bins; j++)
                    {
                        if (current_intensities[j] > 0)
                        {
                            list_Bins.Add(j);
                            list_Intensity.Add(current_intensities[j]);
                        }
                    }

                    UIMF_Writer.InsertScan(fp, i, list_Bins, list_Intensity, 1, gp.TimeOffset/4);
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

            UIMF_Writer.FlushUIMF();
            UIMF_Writer.CloseUIMF();
        }
    }
}

namespace UIMF_File
{
    public interface IRegistryPersist
    {
        void RegistrySave(RegistryKey key);
        void RegistryLoad(RegistryKey key);
    }
}
