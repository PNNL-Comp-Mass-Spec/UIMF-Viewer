﻿using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using UIMFViewer.ChromatogramControl;
using UIMFViewer.FrameControl;
using UIMFViewer.FrameInfo;
using UIMFViewer.PostProcessing;
using UIMFViewer.PlotAreaFormatting;

namespace UIMFViewer
{
    partial class DataViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        #region WinForms ContextMenus
        private System.Windows.Forms.ContextMenu contextMenu_pb_2DMap;
        private System.Windows.Forms.MenuItem menuItem_SelectionCorners;
        protected System.Windows.Forms.MenuItem menuItemZoomFull;
        protected System.Windows.Forms.MenuItem menuItemZoomPrevious;
        protected System.Windows.Forms.MenuItem menuItemZoomOut;
        private System.Windows.Forms.MenuItem menuItem_MaxIntensities;
        private System.Windows.Forms.MenuItem menuItem5;
        private System.Windows.Forms.MenuItem menuItemConvertToMZ;
        private System.Windows.Forms.MenuItem menuItemConvertToTOF;
        private System.Windows.Forms.ContextMenu contextMenu_driftTIC;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.MenuItem menuItem8;
        private System.Windows.Forms.MenuItem menuItem_driftTIC_ShowBPI;
        private System.Windows.Forms.MenuItem menuItem_driftTIC_Sep1;
        private System.Windows.Forms.MenuItem menuItem_Frame_driftTIC;
        private System.Windows.Forms.MenuItem menuItem_Time_driftTIC;
        private System.Windows.Forms.MenuItem menuItem9;
        private System.Windows.Forms.MenuItem menuItem_ExportDriftTIC_Displayed;
        private System.Windows.Forms.MenuItem menuItem_ExportDriftTIC_Complete;
        private System.Windows.Forms.MenuItem menuItem_ExportCompressed;
        private System.Windows.Forms.MenuItem menuItem_ExportComplete;
        private System.Windows.Forms.MenuItem menuItem_CopyToClipboard;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem3a;
        private System.Windows.Forms.MenuItem menuItem_Mobility;
        private System.Windows.Forms.MenuItem menuItem_ScanTime;
        private System.Windows.Forms.ContextMenu contextMenu_TOF;
        private System.Windows.Forms.MenuItem menuItem_TOFExport;
        private System.Windows.Forms.MenuItem menuItem_TOFMaximum;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem_CaptureExperimentFrame;
        private MenuItem menuItem_WriteUIMF;
        #endregion

        #region WinForms SubItems
        protected System.Windows.Forms.Panel pnl_2DMap;
        protected ZedGraph.ZedGraphControl plot_TOF;
        protected UIMFViewer.Utilities.PointAnnotationGraph plot_Mobility;
        protected System.Windows.Forms.NumericUpDown num_minMobility;
        protected System.Windows.Forms.NumericUpDown num_maxMobility;
        private Label lbl_TIC;
        protected System.Windows.Forms.NumericUpDown num_minBin;
        protected System.Windows.Forms.NumericUpDown num_maxBin;
        protected ZedGraph.LineItem waveform_TOFPlot;
        protected ZedGraph.LineItem waveform_MobilityPlot;

        protected System.Windows.Forms.Label lbl_ExperimentDate;

        protected HScrollBar hsb_2DMap;
        protected VScrollBar vsb_2DMap;

        private ProgressBar progress_ReadingFile;

        protected Button btn_Refresh;  // while plotting, prevent zooming!

        private System.Drawing.Graphics pnl_2DMap_Extensions;
        private Pen thick_pen = new Pen(new SolidBrush(Color.Fuchsia), 1);

        protected TabControl tabpages_Main;
        protected TabPage tab_DataViewer;
        protected TabPage tab_PostProcessing;

        private PictureBox pb_Expand;
        private PictureBox pb_Shrink;
        #endregion

        #region WPF ElementHosts
        public ElementHost elementHost_PlotAreaFormatting;
        private ElementHost elementHost_ChromatogramControls;
        private ElementHost elementHost_PostProcessing;
        private ElementHost elementHost_FrameControl;
        private ElementHost elementHost_FrameInfo;
        private ElementHost elementHost_MzRange;
        #endregion

        #region WPF SubItems
        private PlotAreaFormattingViewModel plotAreaFormattingVm;
        private PlotAreaFormattingView plotAreaFormattingView;

        private ChromatogramControlView chromatogramControlView;
        private ChromatogramControlViewModel chromatogramControlVm;

        private PostProcessingView postProcessingView;

        private FrameControlView frameControlView;
        public FrameControlViewModel frameControlVm;

        private FrameInfoView frameInfoView;
        private FrameInfoViewModel frameInfoVm;

        private MzRangeView mzRangeView;
        private MzRangeViewModel mzRangeVm;
        #endregion

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
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem_CopyToClipboard = new System.Windows.Forms.MenuItem();
            this.menuItem_CaptureExperimentFrame = new System.Windows.Forms.MenuItem();
            this.menuItem_WriteUIMF = new System.Windows.Forms.MenuItem();
            this.contextMenu_driftTIC = new System.Windows.Forms.ContextMenu();
            this.menuItem_driftTIC_ShowBPI = new System.Windows.Forms.MenuItem();
            this.menuItem_driftTIC_Sep1 = new System.Windows.Forms.MenuItem();
            this.menuItem_Frame_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem_Time_driftTIC = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportDriftTIC_Displayed = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportDriftTIC_Complete = new System.Windows.Forms.MenuItem();
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
            this.hsb_2DMap = new System.Windows.Forms.HScrollBar();
            this.vsb_2DMap = new System.Windows.Forms.VScrollBar();
            this.btn_Refresh = new System.Windows.Forms.Button();
            this.elementHost_ChromatogramControls = new ElementHost();
            this.chromatogramControlView = new ChromatogramControlView();
            this.chromatogramControlVm = new ChromatogramControlViewModel();
            this.tabpages_Main = new System.Windows.Forms.TabControl();
            this.tab_DataViewer = new System.Windows.Forms.TabPage();
            this.elementHost_FrameControl = new ElementHost();
            this.frameControlView = new FrameControlView();
            this.frameControlVm = new FrameControlViewModel();
            this.elementHost_MzRange = new ElementHost();
            this.mzRangeView = new MzRangeView();
            this.mzRangeVm = new MzRangeViewModel();
            this.pnl_2DMap = new System.Windows.Forms.Panel();
            this.tab_PostProcessing = new System.Windows.Forms.TabPage();
            this.elementHost_PostProcessing = new ElementHost();
            this.postProcessingView = new PostProcessingView();
            this.pb_Shrink = new System.Windows.Forms.PictureBox();
            this.pb_Expand = new System.Windows.Forms.PictureBox();
            this.elementHost_FrameInfo = new ElementHost();
            this.frameInfoView = new FrameInfoView();
            this.frameInfoVm = new FrameInfoViewModel();
            ((System.ComponentModel.ISupportInitialize)(this.num_minMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxMobility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_maxBin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_minBin)).BeginInit();
            this.tabpages_Main.SuspendLayout();
            this.tab_DataViewer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Shrink)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Expand)).BeginInit();
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
            this.menuItem1,
            this.menuItem_CopyToClipboard,
            this.menuItem_CaptureExperimentFrame,
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
            // menuItem1
            //
            this.menuItem1.Index = 14;
            this.menuItem1.Text = "-";
            //
            // menuItem_CopyToClipboard
            //
            this.menuItem_CopyToClipboard.Index = 15;
            this.menuItem_CopyToClipboard.Text = "Copy Image to Clipboard";
            //
            // menuItem_CaptureExperimentFrame
            //
            this.menuItem_CaptureExperimentFrame.Index = 16;
            this.menuItem_CaptureExperimentFrame.Text = "Save Experiment GUI";
            //
            // menuItem_WriteUIMF
            //
            this.menuItem_WriteUIMF.Index = 17;
            this.menuItem_WriteUIMF.Text = "Write Frame to UIMF file";
            //
            // contextMenu_driftTIC
            //
            this.contextMenu_driftTIC.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_driftTIC_ShowBPI,
            this.menuItem_driftTIC_Sep1,
            this.menuItem_Frame_driftTIC,
            this.menuItem_Time_driftTIC,
            this.menuItem6,
            this.menuItem_ExportDriftTIC_Displayed,
            this.menuItem_ExportDriftTIC_Complete,
            this.menuItem9,
            this.menuItem8});
            //
            // menuItem_driftTIC_ShowBPI
            //
            this.menuItem_driftTIC_ShowBPI.Index = 0;
            this.menuItem_driftTIC_ShowBPI.Text = "Show BPI";
            //
            // menuItem_driftTIC_Sep1
            //
            this.menuItem_driftTIC_Sep1.Index = 1;
            this.menuItem_driftTIC_Sep1.Text = "-";
            //
            // menuItem_Frame_driftTIC
            //
            this.menuItem_Frame_driftTIC.Index = 2;
            this.menuItem_Frame_driftTIC.Text = "Chromatogram units - Frames";
            //
            // menuItem_Time_driftTIC
            //
            this.menuItem_Time_driftTIC.Index = 3;
            this.menuItem_Time_driftTIC.Text = "Chromatogram units - Time";
            //
            // menuItem6
            //
            this.menuItem6.Index = 4;
            this.menuItem6.Text = "-";
            //
            // menuItem_ExportDriftTIC_Displayed
            //
            this.menuItem_ExportDriftTIC_Displayed.Index = 5;
            this.menuItem_ExportDriftTIC_Displayed.Text = "Export Data to File (as shown)...";
            //
            // menuItem_ExportDriftTIC_Complete
            //
            this.menuItem_ExportDriftTIC_Complete.Index = 6;
            this.menuItem_ExportDriftTIC_Complete.Text = "Export Data to File (uncompressed)...";
            //
            // menuItem9
            //
            this.menuItem9.Index = 7;
            this.menuItem9.Text = "Copy Current TIC Image to Clipboard";
            //
            // menuItem8
            //
            this.menuItem8.Index = 8;
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
            // elementHost_FrameInfo
            //
            this.elementHost_FrameInfo.Location = new System.Drawing.Point(10, 672);
            this.elementHost_FrameInfo.Size = new System.Drawing.Size(208, 164);
            this.frameInfoView.DataContext = this.frameInfoVm;
            this.elementHost_FrameInfo.Child = this.frameInfoView;
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
            this.btn_Refresh.Click += new System.EventHandler(this.RefreshClick);
            //
            // elementHost_ChromatogramControls
            //
            this.elementHost_ChromatogramControls.BackColor = System.Drawing.Color.DarkGray;
            this.elementHost_ChromatogramControls.Location = new System.Drawing.Point(14, 580);
            this.elementHost_ChromatogramControls.Size = new System.Drawing.Size(208, 68);
            this.chromatogramControlView.DataContext = this.chromatogramControlVm;
            this.elementHost_ChromatogramControls.Child = this.chromatogramControlView;
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
            this.tab_DataViewer.Controls.Add(this.elementHost_MzRange);
            this.tab_DataViewer.Controls.Add(this.btn_Refresh);
            this.tab_DataViewer.Controls.Add(this.elementHost_ChromatogramControls);
            this.tab_DataViewer.Controls.Add(this.pnl_2DMap);
            this.tab_DataViewer.Controls.Add(this.num_minMobility);
            this.tab_DataViewer.Controls.Add(this.lbl_TIC);
            this.tab_DataViewer.Controls.Add(this.num_maxMobility);
            this.tab_DataViewer.Controls.Add(this.num_maxBin);
            this.tab_DataViewer.Controls.Add(this.num_minBin);
            this.tab_DataViewer.Controls.Add(this.elementHost_PlotAreaFormatting);
            this.tab_DataViewer.Controls.Add(this.lbl_ExperimentDate);
            this.tab_DataViewer.Controls.Add(this.elementHost_FrameInfo);
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
            // elementHost_MzRange
            //
            this.elementHost_MzRange.Location = new System.Drawing.Point(726, 784);
            this.elementHost_MzRange.Size = new System.Drawing.Size(240, 76);
            this.mzRangeView.DataContext = this.mzRangeVm;
            this.elementHost_MzRange.Child = this.mzRangeView;
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
            this.pb_Shrink.BackgroundImage = global::UIMFViewer.Properties.Resources.shrink_button;
            this.pb_Shrink.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_Shrink.Location = new System.Drawing.Point(834, 456);
            this.pb_Shrink.Name = "pb_Shrink";
            this.pb_Shrink.Size = new System.Drawing.Size(14, 14);
            this.pb_Shrink.TabIndex = 99;
            this.pb_Shrink.TabStop = false;
            //
            // pb_Expand
            //
            this.pb_Expand.BackgroundImage = global::UIMFViewer.Properties.Resources.expand_button;
            this.pb_Expand.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pb_Expand.Location = new System.Drawing.Point(812, 456);
            this.pb_Expand.Name = "pb_Expand";
            this.pb_Expand.Size = new System.Drawing.Size(14, 14);
            this.pb_Expand.TabIndex = 98;
            this.pb_Expand.TabStop = false;
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
            this.tabpages_Main.ResumeLayout(false);
            this.tab_DataViewer.ResumeLayout(false);
            this.tab_DataViewer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Shrink)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Expand)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}