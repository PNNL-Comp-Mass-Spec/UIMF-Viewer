using System.Drawing;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using UIMF_DataViewer.FrameControl;
using UIMF_DataViewer.PostProcessing;
using UIMF_DataViewer.PlotAreaFormatting;
using UIMF_DataViewer.WpfControls;
using UIMF_DataViewer.WpfControls.LabeledSlider;

namespace UIMF_File
{
    partial class DataViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
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
        protected System.Windows.Forms.Panel pnl_2DMap;
        private System.Windows.Forms.ContextMenu contextMenu_HorizontalAxis;
        private System.Windows.Forms.MenuItem menuItem_UseScans;
        private System.Windows.Forms.MenuItem menuItem_UseDriftTime;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.ContextMenu contextMenu_driftTIC;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.MenuItem menuItem8;
        private System.Windows.Forms.MenuItem menuItem_Frame_driftTIC;
        private System.Windows.Forms.MenuItem menuItem_Time_driftTIC;
        private System.Windows.Forms.MenuItem menuItem9;
        private System.Windows.Forms.MenuItem menuItem_Exportnew_driftTIC;
        protected ZedGraph.ZedGraphControl plot_TOF;
        protected System.Windows.Forms.Label label2;
        private System.Windows.Forms.MenuItem menuItem_ExportCompressed;
        private System.Windows.Forms.MenuItem menuItem_ExportComplete;
        private System.Windows.Forms.MenuItem menuItem_ExportAll;
        private System.Windows.Forms.MenuItem menuItem_CopyToClipboard;
        private System.Windows.Forms.MenuItem menuItem_SuperFrame;
        private System.Windows.Forms.MenuItem menuItem_SuperExperiment;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem3a;
        private System.Windows.Forms.MenuItem menuItem_Mobility;
        private System.Windows.Forms.MenuItem menuItem_ScanTime;
        protected UIMF_File.Utilities.PointAnnotationGraph plot_Mobility;
        private System.Windows.Forms.ContextMenu contextMenu_TOF;
        private System.Windows.Forms.MenuItem menuItem_TOFExport;
        private System.Windows.Forms.MenuItem menuItem_TOFMaximum;
        protected System.Windows.Forms.Label lbl_TimeOffset;
        protected System.Windows.Forms.NumericUpDown num_minMobility;
        protected System.Windows.Forms.NumericUpDown num_maxMobility;
        private Label lbl_TIC;
        protected System.Windows.Forms.NumericUpDown num_minBin;
        protected System.Windows.Forms.NumericUpDown num_maxBin;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label lbl_CursorMobility;
        protected System.Windows.Forms.Label lbl_CursorTOF;
        protected System.Windows.Forms.Label lbl_CursorMZ;
        protected ZedGraph.LineItem waveform_TOFPlot;
        protected ZedGraph.LineItem waveform_MobilityPlot;
        #endregion

        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label lbl_CursorScanTime;
        public ElementHost elementHost_PlotAreaFormatting;
        private PlotAreaFormattingViewModel plotAreaFormattingVm;
        private PlotAreaFormattingView plotAreaFormattingView;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem_CaptureExperimentFrame;

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

        protected HScrollBar hsb_2DMap;
        protected VScrollBar vsb_2DMap;
        private TextBox tb_CalT0;
        private TextBox tb_CalA;

        private ProgressBar progress_ReadingFile;

        protected Button btn_Refresh;  // while plotting, prevent zooming!

        private System.Drawing.Graphics pnl_2DMap_Extensions;
        private Pen thick_pen = new Pen(new SolidBrush(Color.Fuchsia), 1);

        public NumericUpDown num_FrameCompression;

        private Label lbl_FrameCompression;
        public RadioButton rb_CompleteChromatogram;
        public RadioButton rb_PartialChromatogram;
        protected Panel pnl_Chromatogram;

        private MenuItem menuItem_SaveIMF;
        private MenuItem menuItem_WriteUIMF;
        protected TabControl tabpages_Main;
        protected TabPage tab_DataViewer;
        protected TabPage tab_PostProcessing;
        private ElementHost elementHost_PostProcessing;
        private PostProcessingView postProcessingView;
        protected GroupBox gb_MZRange;
        private Label lbl_PPM;
        private Label lbl_MZ;
        protected NumericUpDown num_PPM;
        private Label label1;
        protected NumericUpDown num_MZ;
        protected CheckBox cb_EnableMZRange;
        protected ListBox lb_DragDropFiles;
        private PictureBox pb_PlayDownIn;
        private PictureBox pb_PlayDownOut;
        private PictureBox pb_PlayUpIn;
        private PictureBox pb_PlayUpOut;
        protected CheckBox cb_Exclusive;

        private ElementHost elementHost_FrameControl;
        private FrameControlView frameControlView;
        public FrameControlViewModel frameControlVm;
        private PictureBox pb_Expand;
        private PictureBox pb_Shrink;

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
            this.menuItem_ExportCompressed = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportComplete = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportAll = new System.Windows.Forms.MenuItem();
            this.menuItem_SuperFrame = new System.Windows.Forms.MenuItem();
            this.menuItem_SuperExperiment = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem_CopyToClipboard = new System.Windows.Forms.MenuItem();
            this.menuItem_CaptureExperimentFrame = new System.Windows.Forms.MenuItem();
            this.menuItem_SaveIMF = new System.Windows.Forms.MenuItem();
            this.menuItem_WriteUIMF = new System.Windows.Forms.MenuItem();
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
            this.contextMenu_driftTIC = new System.Windows.Forms.ContextMenu();
            this.menuItem_Frame_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem_Time_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuItem_Exportnew_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem9 = new System.Windows.Forms.MenuItem();
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this.contextMenu_TOF = new System.Windows.Forms.ContextMenu();
            this.menuItem_TOFExport = new System.Windows.Forms.MenuItem();
            this.menuItem_TOFMaximum = new System.Windows.Forms.MenuItem();
            this.num_minMobility = new System.Windows.Forms.NumericUpDown();
            this.num_maxMobility = new System.Windows.Forms.NumericUpDown();
            this.lbl_TIC = new System.Windows.Forms.Label();
            this.num_maxBin = new System.Windows.Forms.NumericUpDown();
            this.num_minBin = new System.Windows.Forms.NumericUpDown();
            this.elementHost_PlotAreaFormatting = new ElementHost();
            this.plotAreaFormattingVm = new PlotAreaFormattingViewModel();
            this.plotAreaFormattingView = new PlotAreaFormattingView();
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
            this.btn_Refresh = new System.Windows.Forms.Button();
            this.num_FrameCompression = new System.Windows.Forms.NumericUpDown();
            this.lbl_FrameCompression = new System.Windows.Forms.Label();
            this.rb_CompleteChromatogram = new System.Windows.Forms.RadioButton();
            this.rb_PartialChromatogram = new System.Windows.Forms.RadioButton();
            this.pnl_Chromatogram = new System.Windows.Forms.Panel();
            this.tabpages_Main = new System.Windows.Forms.TabControl();
            this.tab_DataViewer = new System.Windows.Forms.TabPage();
            this.elementHost_FrameControl = new ElementHost();
            this.frameControlView = new FrameControlView();
            this.frameControlVm = new FrameControlViewModel();
            this.cb_Exclusive = new System.Windows.Forms.CheckBox();
            this.lb_DragDropFiles = new System.Windows.Forms.ListBox();
            this.gb_MZRange = new System.Windows.Forms.GroupBox();
            this.lbl_PPM = new System.Windows.Forms.Label();
            this.lbl_MZ = new System.Windows.Forms.Label();
            this.num_PPM = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.num_MZ = new System.Windows.Forms.NumericUpDown();
            this.cb_EnableMZRange = new System.Windows.Forms.CheckBox();
            this.pnl_2DMap = new System.Windows.Forms.Panel();
            this.tab_PostProcessing = new System.Windows.Forms.TabPage();
            this.elementHost_PostProcessing = new ElementHost();
            this.postProcessingView = new PostProcessingView();
            this.pb_Shrink = new System.Windows.Forms.PictureBox();
            this.pb_Expand = new System.Windows.Forms.PictureBox();
            this.pb_PlayDownIn = new System.Windows.Forms.PictureBox();
            this.pb_PlayDownOut = new System.Windows.Forms.PictureBox();
            this.pb_PlayUpIn = new System.Windows.Forms.PictureBox();
            this.pb_PlayUpOut = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.num_minMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxBin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minBin)).BeginInit();
            this.tabpages_FrameInfo.SuspendLayout();
            this.tabPage_Cursor.SuspendLayout();
            this.tabPage_Calibration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameCompression)).BeginInit();
            this.pnl_Chromatogram.SuspendLayout();
            this.tabpages_Main.SuspendLayout();
            this.tab_DataViewer.SuspendLayout();
            this.gb_MZRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_PPM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Shrink)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Expand)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayDownIn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayDownOut)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayUpIn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayUpOut)).BeginInit();
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
            this.menuItem_ExportCompressed,
            this.menuItem_ExportComplete,
            this.menuItem_ExportAll,
            this.menuItem_SuperFrame,
            this.menuItem_SuperExperiment,
            this.menuItem1,
            this.menuItem_CopyToClipboard,
            this.menuItem_CaptureExperimentFrame,
            this.menuItem_SaveIMF,
            this.menuItem_WriteUIMF});
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
            // menuItem_ExportCompressed
            //
            this.menuItem_ExportCompressed.Index = 12;
            this.menuItem_ExportCompressed.Text = "Export Intensity Values (Compressed Pixel Resolution)";
            //
            // menuItem_ExportComplete
            //
            this.menuItem_ExportComplete.Index = 13;
            this.menuItem_ExportComplete.Text = "Export Intensity Values (Complete Bin Resolution)";
            //
            // menuItem_ExportAll
            //
            this.menuItem_ExportAll.Index = 14;
            this.menuItem_ExportAll.Text = "Export All Frames Intensity Values";
            //
            // menuItem_SuperFrame
            //
            this.menuItem_SuperFrame.Index = 15;
            this.menuItem_SuperFrame.Text = "Export Superframe IMF file";
            //
            // menuItem_SuperExperiment
            //
            this.menuItem_SuperExperiment.Index = 16;
            this.menuItem_SuperExperiment.Text = "Create Merged IMF Frame Experiment...";
            //
            // menuItem1
            //
            this.menuItem1.Index = 17;
            this.menuItem1.Text = "-";
            //
            // menuItem_CopyToClipboard
            //
            this.menuItem_CopyToClipboard.Index = 18;
            this.menuItem_CopyToClipboard.Text = "Copy Image to Clipboard";
            //
            // menuItem_CaptureExperimentFrame
            //
            this.menuItem_CaptureExperimentFrame.Index = 19;
            this.menuItem_CaptureExperimentFrame.Text = "Save Experiment GUI";
            //
            // menuItem_SaveIMF
            //
            this.menuItem_SaveIMF.Index = 20;
            this.menuItem_SaveIMF.Text = "Save Frame IMF";
            //
            // menuItem_WriteUIMF
            //
            this.menuItem_WriteUIMF.Index = 21;
            this.menuItem_WriteUIMF.Text = "Write Frame to UIMF file";
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
            // contextMenu_driftTIC
            //
            this.contextMenu_driftTIC.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_Frame_driftTIC,
            this.menuItem_Time_driftTIC,
            this.menuItem6,
            this.menuItem_Exportnew_driftTIC,
            this.menuItem9,
            this.menuItem8});
            //
            // menuItem_Frame_driftTIC
            //
            this.menuItem_Frame_driftTIC.Index = 0;
            this.menuItem_Frame_driftTIC.Text = "Chromatogram units - Frames";
            //
            // menuItem_Time_driftTIC
            //
            this.menuItem_Time_driftTIC.Index = 1;
            this.menuItem_Time_driftTIC.Text = "Chromatogram units - Time";
            //
            // menuItem6
            //
            this.menuItem6.Index = 2;
            this.menuItem6.Text = "-";
            //
            // menuItem_Exportnew_driftTIC
            //
            this.menuItem_Exportnew_driftTIC.Index = 3;
            this.menuItem_Exportnew_driftTIC.Text = "Export Data to File...";
            //
            // menuItem9
            //
            this.menuItem9.Index = 4;
            this.menuItem9.Text = "Copy Current TIC Image to Clipboard";
            //
            // menuItem8
            //
            this.menuItem8.Index = 5;
            this.menuItem8.Text = "Copy Full TIC Image to Clipboard";
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
            this.num_minMobility.Location = new System.Drawing.Point(244, 732);
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
            this.num_maxMobility.Location = new System.Drawing.Point(664, 732);
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
            // lbl_TIC
            //
            this.lbl_TIC.AutoSize = true;
            this.lbl_TIC.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.lbl_TIC.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_TIC.Location = new System.Drawing.Point(24, 80);
            this.lbl_TIC.Name = "lbl_TIC";
            this.lbl_TIC.Size = new System.Drawing.Size(86, 14);
            this.lbl_TIC.TabIndex = 53;
            this.lbl_TIC.Text = "TIC:";
            //
            // num_maxBin
            //
            this.num_maxBin.DecimalPlaces = 4;
            this.num_maxBin.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_maxBin.Location = new System.Drawing.Point(16, 72);
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
            this.num_minBin.Location = new System.Drawing.Point(18, 552);
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
            // plotAreaFormatting
            //
            this.plotAreaFormattingView.DataContext = this.plotAreaFormattingVm;
            this.elementHost_PlotAreaFormatting.Location = new System.Drawing.Point(834, 4);
            this.elementHost_PlotAreaFormatting.Size = new System.Drawing.Size(90, 300);
            this.elementHost_PlotAreaFormatting.Child = this.plotAreaFormattingView;
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
            this.tabpages_FrameInfo.Location = new System.Drawing.Point(10, 672);
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
            this.hsb_2DMap.Location = new System.Drawing.Point(244, 112);
            this.hsb_2DMap.Name = "hsb_2DMap";
            this.hsb_2DMap.Size = new System.Drawing.Size(500, 12);
            this.hsb_2DMap.TabIndex = 48;
            //
            // vsb_2DMap
            //
            this.vsb_2DMap.Location = new System.Drawing.Point(742, 124);
            this.vsb_2DMap.Name = "vsb_2DMap";
            this.vsb_2DMap.Size = new System.Drawing.Size(12, 492);
            this.vsb_2DMap.TabIndex = 49;
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
            this.pnl_Chromatogram.Location = new System.Drawing.Point(14, 580);
            this.pnl_Chromatogram.Name = "pnl_Chromatogram";
            this.pnl_Chromatogram.Size = new System.Drawing.Size(208, 68);
            this.pnl_Chromatogram.TabIndex = 79;
            //
            // tabpages_Main
            //
            this.tabpages_Main.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.tabpages_Main.Controls.Add(this.tab_DataViewer);
            this.tabpages_Main.Controls.Add(this.tab_PostProcessing);
            this.tabpages_Main.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabpages_Main.Font = new System.Drawing.Font("Comic Sans MS", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabpages_Main.Location = new System.Drawing.Point(0, 0);
            this.tabpages_Main.Multiline = true;
            this.tabpages_Main.Name = "tabpages_Main";
            this.tabpages_Main.SelectedIndex = 0;
            this.tabpages_Main.Size = new System.Drawing.Size(1016, 880);
            this.tabpages_Main.TabIndex = 81;
            //
            // tab_DataViewer
            //
            this.tab_DataViewer.BackColor = System.Drawing.Color.Silver;
            this.tab_DataViewer.Controls.Add(this.pb_Shrink);
            this.tab_DataViewer.Controls.Add(this.pb_Expand);
            this.tab_DataViewer.Controls.Add(this.elementHost_FrameControl);
            this.tab_DataViewer.Controls.Add(this.cb_Exclusive);
            this.tab_DataViewer.Controls.Add(this.pb_PlayDownIn);
            this.tab_DataViewer.Controls.Add(this.pb_PlayDownOut);
            this.tab_DataViewer.Controls.Add(this.pb_PlayUpIn);
            this.tab_DataViewer.Controls.Add(this.pb_PlayUpOut);
            this.tab_DataViewer.Controls.Add(this.lb_DragDropFiles);
            this.tab_DataViewer.Controls.Add(this.gb_MZRange);
            this.tab_DataViewer.Controls.Add(this.cb_EnableMZRange);
            this.tab_DataViewer.Controls.Add(this.btn_Refresh);
            this.tab_DataViewer.Controls.Add(this.pnl_Chromatogram);
            this.tab_DataViewer.Controls.Add(this.pnl_2DMap);
            this.tab_DataViewer.Controls.Add(this.num_minMobility);
            this.tab_DataViewer.Controls.Add(this.lbl_TIC);
            this.tab_DataViewer.Controls.Add(this.num_maxMobility);
            this.tab_DataViewer.Controls.Add(this.num_maxBin);
            this.tab_DataViewer.Controls.Add(this.num_minBin);
            this.tab_DataViewer.Controls.Add(this.elementHost_PlotAreaFormatting);
            this.tab_DataViewer.Controls.Add(this.lbl_ExperimentDate);
            this.tab_DataViewer.Controls.Add(this.tabpages_FrameInfo);
            this.tab_DataViewer.Controls.Add(this.vsb_2DMap);
            this.tab_DataViewer.Controls.Add(this.hsb_2DMap);
            this.tab_DataViewer.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_DataViewer.Location = new System.Drawing.Point(32, 4);
            this.tab_DataViewer.Name = "tab_DataViewer";
            this.tab_DataViewer.Padding = new System.Windows.Forms.Padding(3);
            this.tab_DataViewer.Size = new System.Drawing.Size(980, 872);
            this.tab_DataViewer.TabIndex = 0;
            this.tab_DataViewer.Text = "   Data Viewer    ";
            //
            // elementHost_FrameControl
            //
            this.elementHost_FrameControl.Location = new System.Drawing.Point(240, 8);
            this.elementHost_FrameControl.Size = new System.Drawing.Size(700, 100);
            this.frameControlView.DataContext = this.frameControlVm;
            this.elementHost_FrameControl.Child = this.frameControlView;
            //
            // cb_Exclusive
            //
            this.cb_Exclusive.AutoSize = true;
            this.cb_Exclusive.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cb_Exclusive.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.cb_Exclusive.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Exclusive.ForeColor = System.Drawing.Color.Blue;
            this.cb_Exclusive.Location = new System.Drawing.Point(406, 748);
            this.cb_Exclusive.Name = "cb_Exclusive";
            this.cb_Exclusive.Size = new System.Drawing.Size(143, 18);
            this.cb_Exclusive.TabIndex = 95;
            this.cb_Exclusive.Text = "Exclusive Viewing";
            this.cb_Exclusive.UseVisualStyleBackColor = true;
            //
            // lb_DragDropFiles
            //
            this.lb_DragDropFiles.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lb_DragDropFiles.FormattingEnabled = true;
            this.lb_DragDropFiles.Location = new System.Drawing.Point(278, 772);
            this.lb_DragDropFiles.Name = "lb_DragDropFiles";
            this.lb_DragDropFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lb_DragDropFiles.Size = new System.Drawing.Size(420, 43);
            this.lb_DragDropFiles.TabIndex = 90;
            //
            // gb_MZRange
            //
            this.gb_MZRange.BackColor = System.Drawing.Color.Transparent;
            this.gb_MZRange.Controls.Add(this.lbl_PPM);
            this.gb_MZRange.Controls.Add(this.lbl_MZ);
            this.gb_MZRange.Controls.Add(this.num_PPM);
            this.gb_MZRange.Controls.Add(this.label1);
            this.gb_MZRange.Controls.Add(this.num_MZ);
            this.gb_MZRange.Location = new System.Drawing.Point(746, 784);
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
            this.cb_EnableMZRange.Location = new System.Drawing.Point(818, 764);
            this.cb_EnableMZRange.Name = "cb_EnableMZRange";
            this.cb_EnableMZRange.Size = new System.Drawing.Size(138, 18);
            this.cb_EnableMZRange.TabIndex = 87;
            this.cb_EnableMZRange.Text = "Enable MZ Range";
            this.cb_EnableMZRange.UseVisualStyleBackColor = false;
            //
            // pnl_2DMap
            //
            this.pnl_2DMap.BackColor = System.Drawing.Color.Black;
            this.pnl_2DMap.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pnl_2DMap.Location = new System.Drawing.Point(242, 124);
            this.pnl_2DMap.Name = "pnl_2DMap";
            this.pnl_2DMap.Size = new System.Drawing.Size(500, 484);
            this.pnl_2DMap.TabIndex = 2;
            //
            // tab_PostProcessing
            //
            this.tab_PostProcessing.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_PostProcessing.Location = new System.Drawing.Point(32, 4);
            this.tab_PostProcessing.Name = "tab_PostProcessing";
            this.tab_PostProcessing.Padding = new System.Windows.Forms.Padding(3);
            this.tab_PostProcessing.Size = new System.Drawing.Size(980, 872);
            this.tab_PostProcessing.TabIndex = 2;
            this.tab_PostProcessing.Text = "   Post Processing    ";
            this.tab_PostProcessing.UseVisualStyleBackColor = true;
            //
            // elementHost_PostProcessing
            //
            this.elementHost_PostProcessing.Name = "elementHost_PostProcessing";
            this.elementHost_PostProcessing.Size = new System.Drawing.Size(1072, 847);
            this.elementHost_PostProcessing.Dock = DockStyle.Fill;
            this.elementHost_PostProcessing.AutoSize = true;
            this.elementHost_PostProcessing.Child = this.postProcessingView;
            this.tab_PostProcessing.Controls.Add(this.elementHost_PostProcessing);
            //
            // pb_Shrink
            //
            this.pb_Shrink.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.shrink_button;
            this.pb_Shrink.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_Shrink.Location = new System.Drawing.Point(834, 456);
            this.pb_Shrink.Name = "pb_Shrink";
            this.pb_Shrink.Size = new System.Drawing.Size(14, 14);
            this.pb_Shrink.TabIndex = 99;
            this.pb_Shrink.TabStop = false;
            //
            // pb_Expand
            //
            this.pb_Expand.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.expand_button;
            this.pb_Expand.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_Expand.Location = new System.Drawing.Point(812, 456);
            this.pb_Expand.Name = "pb_Expand";
            this.pb_Expand.Size = new System.Drawing.Size(14, 14);
            this.pb_Expand.TabIndex = 98;
            this.pb_Expand.TabStop = false;
            //
            // pb_PlayDownIn
            //
            this.pb_PlayDownIn.BackColor = System.Drawing.Color.Transparent;
            this.pb_PlayDownIn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pb_PlayDownIn.BackgroundImage")));
            this.pb_PlayDownIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_PlayDownIn.Location = new System.Drawing.Point(356, 824);
            this.pb_PlayDownIn.Name = "pb_PlayDownIn";
            this.pb_PlayDownIn.Size = new System.Drawing.Size(24, 24);
            this.pb_PlayDownIn.TabIndex = 94;
            this.pb_PlayDownIn.TabStop = false;
            //
            // pb_PlayDownOut
            //
            this.pb_PlayDownOut.BackColor = System.Drawing.Color.Transparent;
            this.pb_PlayDownOut.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pb_PlayDownOut.BackgroundImage")));
            this.pb_PlayDownOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_PlayDownOut.Location = new System.Drawing.Point(228, 816);
            this.pb_PlayDownOut.Name = "pb_PlayDownOut";
            this.pb_PlayDownOut.Size = new System.Drawing.Size(24, 24);
            this.pb_PlayDownOut.TabIndex = 93;
            this.pb_PlayDownOut.TabStop = false;
            //
            // pb_PlayUpIn
            //
            this.pb_PlayUpIn.BackColor = System.Drawing.Color.Transparent;
            this.pb_PlayUpIn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pb_PlayUpIn.BackgroundImage")));
            this.pb_PlayUpIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_PlayUpIn.Location = new System.Drawing.Point(254, 788);
            this.pb_PlayUpIn.Name = "pb_PlayUpIn";
            this.pb_PlayUpIn.Size = new System.Drawing.Size(22, 24);
            this.pb_PlayUpIn.TabIndex = 92;
            this.pb_PlayUpIn.TabStop = false;
            //
            // pb_PlayUpOut
            //
            this.pb_PlayUpOut.BackColor = System.Drawing.Color.Transparent;
            this.pb_PlayUpOut.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pb_PlayUpOut.BackgroundImage")));
            this.pb_PlayUpOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_PlayUpOut.Location = new System.Drawing.Point(230, 788);
            this.pb_PlayUpOut.Name = "pb_PlayUpOut";
            this.pb_PlayUpOut.Size = new System.Drawing.Size(22, 24);
            this.pb_PlayUpOut.TabIndex = 91;
            this.pb_PlayUpOut.TabStop = false;
            //
            // DataViewer
            //
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(1040, 887);
            this.Controls.Add(this.tabpages_Main);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Location = new System.Drawing.Point(30, 30);
            this.MinimumSize = new System.Drawing.Size(700, 600);
            this.Name = "DataViewer";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Experiment name";
            ((System.ComponentModel.ISupportInitialize)(this.num_minMobility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxMobility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxBin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minBin)).EndInit();
            this.tabpages_FrameInfo.ResumeLayout(false);
            this.tabPage_Cursor.ResumeLayout(false);
            this.tabPage_Calibration.ResumeLayout(false);
            this.tabPage_Calibration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_FrameCompression)).EndInit();
            this.pnl_Chromatogram.ResumeLayout(false);
            this.pnl_Chromatogram.PerformLayout();
            this.tabpages_Main.ResumeLayout(false);
            this.tab_DataViewer.ResumeLayout(false);
            this.tab_DataViewer.PerformLayout();
            this.gb_MZRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.num_PPM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Shrink)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Expand)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayDownIn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayDownOut)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayUpIn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PlayUpOut)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion
    }
}