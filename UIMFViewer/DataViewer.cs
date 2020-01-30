using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using UIMFLibrary;
using UIMFViewer.PostProcessing;
using UIMFViewer.Utilities;
using ZedGraph;

namespace UIMFViewer
{
    public partial class DataViewer : Form
    {
        [DllImport("gdi32.dll")]
        // Used to capture a screenshot of the GUI
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        #region Fields

        // For drawing on the pb_2DMap
        private bool mouseDragging;
        private Point mouseDownPoint;
        private Point mouseMovePoint;

        // elements used for Fast Pixellation
        private Bitmap bitmap;
        private readonly Point[] plot2DSelectionCorners = new Point[4];

        // Variables for mapping
        private int currentValuesPerPixelX;
        private int currentValuesPerPixelY;
        private int newMinMobility;
        private int newMaxMobility;
        private int currentMinMobility;
        private int currentMaxMobility;
        private int newMinTofBin;
        private int newMaxTofBin;
        private int currentMinTofBin;
        private int currentMaxTofBin;

        private int chromatogramValuesPerPixelX;
        private int chromatogramValuesPerPixelY;
        private double[] chromatogramMobilityTicData;
        private double[] chromatogramTofTicData;

        private int mobilitySelectionMinimum;
        private int mobilitySelectionMaximum;

        private double averageDriftScanDuration;

        private const int MinGraphedBins = 20;
        private const int MinGraphedMobility = 10;
        private const int DesiredChromatogramWidth = 1500;
        private const int MobilityPlotHeight = 150;

        private int frameMaximumMobility;
        private int frameMaximumTofBins;

        private int chromatogramMinMobility;
        private int chromatogramMaxMobility = 599;
        private int chromatogramMinFrame;
        private int chromatogramMaxFrame = 499;

        private int plot2DMaxIntensityX;
        private int plot2DMaxIntensityY;

        private int[][] data_2D;

        private double[] mobilityTicData;
        private double[] tofTicData;
        private int current2DPlotMaxIntensity;

        private int[][] chromatogramData;
        private int chromatogramMax;

        private readonly object plot2DChangeLock = new object();

        private int isMovingSelectionCorners = -1;

        private int max2DPlotWidth = 200;
        private int max2DPlotHeight = 200;

        private int currentFrameCompression;

        private bool displayTofValues;
        private bool currentlyReadingData;
        private bool selectingMobilityRange;
        private bool applyingMobilityRangeChange = true;
        private bool applyingTofBinRangeChange = true;
        private bool showMobilityScanNumber = true;
        private bool needToUpdate2DPlot;
        private bool showMobilityChromatogramFrameNumber;
        private bool partialChromatogramCollected;
        private bool completeChromatogramCollected;
        private bool viewerKeepAlive = true;
        private bool disableMouseControls;
        private bool viewerIsClosing;
        private bool frameTypeChanged;
        private bool viewerNeedsResizing;
        private bool viewerIsResizing;
        private bool is2DPlotFullScreen;
        private readonly bool isTImsData;

        // Save previous zoom points
        private readonly List<ZoomInfo> zoomHistory = new List<ZoomInfo>();

        private Thread graphFrameThread;

        private readonly UIMFDataWrapper uimfReader;
        private readonly PostProcessingViewModel postProcessingVm;

        private UIMFDataWrapper.ReadFrameType currentFrameType;

        #endregion

        #region Construct and Dispose

        public DataViewer()
        {
            try
            {
                postProcessingVm = new PostProcessingViewModel();
                BuildInterface();

                frameControlVm.SelectedFrameType = UIMFDataWrapper.ReadFrameType.AllFrames;

                hsb_2DMap.Visible = vsb_2DMap.Visible = false;
                frameControlVm.MinimumFrameNumber = 0;
                frameControlVm.MaximumFrameNumber = 0;

                // TODO: //plot_TOF.ClearData();
                // TODO: //plot_Mobility.ClearData();

                viewerNeedsResizing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("DataViewer(): " + ex);
            }
        }

        public DataViewer(string uimfFile)
        {
            try
            {
                uimfReader = new UIMFDataWrapper(uimfFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            postProcessingVm = new PostProcessingViewModel(uimfReader);

            currentMinTofBin = 0;
            currentMaxTofBin = frameMaximumTofBins = uimfReader.UimfGlobalParams.Bins;

            try
            {
                BuildInterface();
            }
            catch (Exception ex)
            {
                MessageBox.Show("failed to build interface()\n\n" + ex);
            }

            frameControlVm.MinimumFrameNumber = 0;
            frameControlVm.MaximumFrameNumber = uimfReader.UimfGlobalParams.NumFrames;

            currentMinTofBin = 0;
            currentMaxTofBin = 10;

            frameControlVm.UimfFile = Path.GetFileName(uimfReader.UimfDataFile);

            frameControlVm.SelectedFrameType = uimfReader.CurrentFrameType;
            FilterFramesByType(uimfReader.CurrentFrameType);
            uimfReader.CurrentFrameIndex = 0;

            uimfReader.SetCurrentFrameType(currentFrameType, true);

            Generate2DIntensityArray();
            LoadGraphFrame();

            if (!string.IsNullOrWhiteSpace(uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.InstrumentName, "")))
            {
                isTImsData = uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.InstrumentName, "").StartsWith("TIMS");
                if (isTImsData)
                    plot_Mobility.set_TIMSRamp(uimfReader.UimfFrameParams.MassCalibrationCoefficients.a2, uimfReader.UimfFrameParams.MassCalibrationCoefficients.b2,
                        uimfReader.UimfFrameParams.MassCalibrationCoefficients.c2, uimfReader.UimfFrameParams.Scans,
                        (int)(7500000.0 / uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength))); // msec gap
            }
            else
                isTImsData = false;

            if (uimfReader.UimfGlobalParams.NumFrames > DesiredChromatogramWidth)
                chromatogramControlVm.FrameCompression = uimfReader.UimfGlobalParams.NumFrames / DesiredChromatogramWidth;
            else
                chromatogramControlVm.FrameCompression = 1;
            currentFrameCompression = chromatogramControlVm.FrameCompression;

            // Do some math, prevent the viewer from expanding across multiple screens when first opened.
            if (pnl_2DMap.Left + uimfReader.UimfFrameParams.Scans + 170 < Screen.FromControl(this).Bounds.Width)
            {
                Width = pnl_2DMap.Left + uimfReader.UimfFrameParams.Scans + 170;
            }
            else
            {
                var maxMapWidth = Screen.FromControl(this).Bounds.Width - pnl_2DMap.Left - 170;
                var xCompression = (int) (uimfReader.UimfFrameParams.Scans / (double) maxMapWidth + 0.99999); // Round up
                Width = ((int) (uimfReader.UimfFrameParams.Scans / (double) xCompression)) + 30 + pnl_2DMap.Left + 170;
            }

            postProcessingVm.InitializeCalibrants(1, uimfReader.UimfFrameParams.CalibrationSlope, uimfReader.UimfFrameParams.CalibrationIntercept);

            frameInfoVm.CursorTabSelected = true;
            frameInfoVm.HideCalibrationButtons();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            viewerKeepAlive = false;
            viewerIsClosing = true;

            if (playingCinemaPlot)
            {
                StopCinema();
                Thread.Sleep(300);
            }

            AllowDrop = false;
            needToUpdate2DPlot = false;

            while (currentlyReadingData)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (disposing)
            {
                uimfReader.Dispose();
                components?.Dispose();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            base.Dispose(disposing);
        }

        #endregion

        #region UI Setup

        private void BuildInterface()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            pb_Shrink.Hide();
            pb_Expand.Hide();

            tabpages_Main.Top = (tab_DataViewer.ClientSize.Height - tabpages_Main.Height)/2;

            postProcessingVm.CalibrationChanged += PostProcessingCalibrationChanged;

            postProcessingView.DataContext = postProcessingVm;

            AutoScroll = false;

            SetupPlots();

            plot_TOF.Left = 0;
            plot_TOF.Top = 0;

            plotAreaFormattingVm.PropertyChanged += PlotAreaFormattingVmOnPropertyChanged;

            // starts with the mobility view
            showMobilityScanNumber = true;
            menuItem_Mobility.Checked = true;
            menuItem_ScanTime.Checked = false;

            // start the heartbeat
            frameControlVm.CurrentFrameNumber = 0;

            // default values in the calibration require no interface
            frameInfoVm.HideCalibrationButtons();

            //AllowDrop = true;

            Thread.Sleep(200);
            Show();
            menuItem_ScanTime.PerformClick();

            menuItem_Time_driftTIC.Checked = true;
            menuItem_Frame_driftTIC.Checked = false;

            menuItem_SelectionCorners.Click += SelectionCornersClick;
            menuItem_ScanTime.Click += MobilityShowScanTimeClick;
            menuItem_Mobility.Click += MobilityShowScanNumberClick;
            menuItem_ExportCompressed.Click += ExportCompressedIntensityMatrixClick;
            menuItem_ExportComplete.Click += ExportCompleteIntensityMatrixClick;
            menuItem_CopyToClipboard.Click += CopyImageToClipboard;
            menuItem_CaptureExperimentFrame.Click += SaveExperimentGuiClick;
            menuItem_WriteUIMF.Click += WriteUimfClick;
            menuItem_Exportnew_driftTIC.Click += ExportDriftTicClick;
            menuItem_Frame_driftTIC.Click += MobilityChromatogramPlotShowFrameClick;
            menuItem_Time_driftTIC.Click += MobilityChromatogramPlotShowTimeClick;
            menuItem_TOFExport.Click += TofExportDataClick;
            menuItem_TOFMaximum.Click += OnlyShowMaximumIntensitiesClick;
            menuItemZoomFull.Click += ZoomContextMenu;
            menuItemZoomPrevious.Click += ZoomContextMenu;
            menuItemZoomOut.Click += ZoomContextMenu;
            menuItem_MaxIntensities.Click += OnlyShowMaximumIntensitiesClick;
            menuItemConvertToMZ.Click += ConvertContextMenu;
            menuItemConvertToTOF.Click += ConvertContextMenu;

            pnl_2DMap.DoubleClick += Plot2DDoubleClick;
            pnl_2DMap.MouseMove += Plot2DMouseMove;
            pnl_2DMap.MouseDown += Plot2DMouseDown;
            pnl_2DMap.Paint += Plot2DPaint;
            pnl_2DMap.MouseUp += Plot2DMouseUp;

            plot_Mobility.ContextMenu = contextMenu_driftTIC;
            plot_Mobility.RangeChanged += MobilityPlotSelectionRangeChanged;

            frameControlVm.PlayLeft += FramesPlayLeftClick;
            frameControlVm.PlayRight += FramesPlayRightClick;
            frameControlVm.StopCinema += FramesStopPlayingClick;

            frameControlVm.PropertyChanged += FrameControlVmOnPropertyChanged;

            mzRangeVm.PropertyChanged += MzRangeChanged;
            frameInfoVm.SetCalDefaults += SetCalDefaultsClick;
            frameInfoVm.RevertCalDefaults += RevertCalDefaultsClick;

            num_minMobility.ValueChanged += MobilityLimitsChanged;
            num_maxMobility.ValueChanged += MobilityLimitsChanged;
            num_maxBin.ValueChanged += NaxTofBinChanged;
            num_minBin.ValueChanged += MinTofBinChanged;
            plot_TOF.ContextMenu = contextMenu_TOF;

            chromatogramControlVm.PropertyChanged += ChromatogramControlVmOnPropertyChanged;

            vsb_2DMap.Scroll += Map2DVerticalScroll;
            hsb_2DMap.Scroll += Map2DHorizontalScroll;

            frameInfoVm.PropertyChanged += FrameInfoVmOnPropertyChanged;

            plotAreaFormattingVm.ValuesReset += PlotAreaFormattingReset;

            tabpages_Main.DrawItem += MainTabsDrawItem;
            tabpages_Main.SelectedIndexChanged += MainTabsSelectedIndexChanged;

            plotAreaFormattingVm.ColorMap.ColorPositionChanged += ColorSelectorChanged;
            plotAreaFormattingVm.ColorMap.PropertyChanged += ColorMapOnPropertyChanged;

            Resize += (sender, args) => viewerNeedsResizing = true;

            tabpages_Main.Width = ClientSize.Width + ((tabpages_Main.Height - tab_DataViewer.ClientSize.Height) / 2);
            tabpages_Main.Height = ClientSize.Height + (tabpages_Main.Height - tab_DataViewer.ClientSize.Height);
            tabpages_Main.Left = 0;
            tabpages_Main.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            viewerIsResizing = true;
            Invoke(new MethodInvoker(ResizeThis));
        }

        private void SetupPlots()
        {
            plot_TOF = new ZedGraphControl();
            waveform_TOFPlot = new LineItem("TOF");

            plot_Mobility = new Utilities.PointAnnotationGraph();
            waveform_MobilityPlot = new LineItem("Mobility");

            // https://sourceforge.net/p/zedgraph/bugs/81/
            // ZedGraph does not handle the font size quite properly; scale the numbers to get what we want
            var zedGraphFontScaleFactor = 96F / 72F;

            //
            // plot_TOF
            //
            plot_TOF.Anchor = AnchorStyles.Left;
            plot_TOF.BackColor = Color.Gainsboro;
            plot_TOF.BackgroundImageLayout = ImageLayout.None;
            plot_TOF.BorderStyle = BorderStyle.Fixed3D;
            plot_TOF.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            plot_TOF.IsEnableHEdit = false;
            plot_TOF.IsEnableHPan = false;
            plot_TOF.IsEnableHZoom = false;
            plot_TOF.IsEnableSelection = false;
            plot_TOF.IsEnableVEdit = false;
            plot_TOF.IsEnableVPan = false;
            plot_TOF.IsEnableVZoom = false;
            plot_TOF.IsEnableZoom = false;
            plot_TOF.IsEnableWheelZoom = false;
            plot_TOF.Location = new Point(18, 102);
            plot_TOF.Name = "plot_TOF";
            plot_TOF.GraphPane.Chart.Fill.Color = Color.White;
            plot_TOF.GraphPane.CurveList.Add(waveform_TOFPlot);
            plot_TOF.Size = new Size(204, 440);
            plot_TOF.TabIndex = 20;
            plot_TOF.TabStop = false;
            plot_TOF.GraphPane.Title.IsVisible = false;
            plot_TOF.GraphPane.Legend.IsVisible = false;
            plot_TOF.GraphPane.XAxis.Scale.IsReverse = true;
            plot_TOF.GraphPane.XAxis.Scale.IsLabelsInside = true;
            plot_TOF.GraphPane.XAxis.MajorGrid.Color = Color.FromArgb(224, 224, 224);
            plot_TOF.GraphPane.XAxis.MajorGrid.IsVisible = true;
            plot_TOF.GraphPane.XAxis.CrossAuto = false;
            plot_TOF.GraphPane.XAxis.Cross = 1000000; // TODO: Set automatically
            plot_TOF.GraphPane.IsFontsScaled = false; // TODO:
            plot_TOF.GraphPane.XAxis.Scale.MaxAuto = true;
            plot_TOF.GraphPane.XAxis.Scale.Mag = 0;
            plot_TOF.GraphPane.XAxis.Scale.Format = "0.0E00";
            plot_TOF.GraphPane.XAxis.Scale.LabelGap = 0;
            plot_TOF.GraphPane.YAxis.Scale.Mag = 0;
            plot_TOF.GraphPane.YAxis.MinorTic.IsInside = false;
            plot_TOF.GraphPane.YAxis.MinorTic.IsCrossInside = false;
            plot_TOF.GraphPane.YAxis.MinorTic.IsOpposite = false;
            plot_TOF.GraphPane.YAxis.MajorTic.IsInside = false;
            plot_TOF.GraphPane.YAxis.MajorTic.IsCrossInside = false;
            plot_TOF.GraphPane.YAxis.MajorTic.IsOpposite = false;
            plot_TOF.GraphPane.YAxis.Scale.MaxAuto = true; // TODO:
            plot_TOF.GraphPane.XAxis.Scale.FontSpec.Family = "Verdana";
            plot_TOF.GraphPane.XAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_TOF.GraphPane.YAxis.Scale.FontSpec.Family = "Verdana";
            plot_TOF.GraphPane.YAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_TOF.GraphPane.Margin.Left -= 5;
            plot_TOF.GraphPane.Margin.Top = 25;
            plot_TOF.GraphPane.Margin.Right = 5;
            plot_TOF.GraphPane.Margin.Bottom = 5;
            plot_TOF.ContextMenu = contextMenu_TOF;
            //
            // waveform_TOFPlot
            //
            waveform_TOFPlot.Color = Color.DarkBlue;
            waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.Transparent);

            // Label the axis
            plot_TOF.GraphPane.XAxis.Title.Text = "Time of Flight";
            plot_TOF.GraphPane.XAxis.Title.FontSpec.Family = "Verdana";
            plot_TOF.GraphPane.XAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_TOF.GraphPane.XAxis.Title.IsVisible = false;

            //
            // plot_Mobility
            //
            plot_Mobility.BackColor = Color.Gainsboro;
            plot_Mobility.BorderStyle = BorderStyle.Fixed3D;
            plot_Mobility.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            plot_Mobility.Location = new Point(242, 572);
            plot_Mobility.Name = "plot_DriftPlot";
            plot_Mobility.GraphPane.Chart.Fill.Color = Color.White;
            plot_Mobility.GraphPane.CurveList.Add(waveform_MobilityPlot);
            plot_Mobility.Size = new Size(510, 111);
            plot_Mobility.TabIndex = 24;
            plot_Mobility.ContextMenu = contextMenu_driftTIC;
            plot_Mobility.RangeChanged += MobilityPlotSelectionRangeChanged;
            plot_Mobility.GraphPane.Title.IsVisible = false;
            plot_Mobility.GraphPane.Legend.IsVisible = false;
            plot_Mobility.GraphPane.XAxis.Scale.Mag = 0;
            plot_Mobility.GraphPane.XAxis.MinorTic.IsInside = false;
            plot_Mobility.GraphPane.XAxis.MinorTic.IsCrossInside = false;
            plot_Mobility.GraphPane.XAxis.MinorTic.IsOpposite = false;
            plot_Mobility.GraphPane.XAxis.MajorTic.IsInside = false;
            plot_Mobility.GraphPane.XAxis.MajorTic.IsCrossInside = false;
            plot_Mobility.GraphPane.XAxis.MajorTic.IsOpposite = false;
            plot_Mobility.GraphPane.XAxis.Scale.MaxAuto = true; // TODO:
            plot_Mobility.GraphPane.XAxis.Scale.FontSpec.Family = "Verdana";
            plot_Mobility.GraphPane.XAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_Mobility.GraphPane.YAxis.Scale.FontSpec.Family = "Verdana";
            plot_Mobility.GraphPane.YAxis.Scale.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_Mobility.GraphPane.YAxis.Scale.MaxAuto = true;
            plot_Mobility.GraphPane.YAxis.Scale.LabelGap = 0;
            plot_Mobility.IsEnableHEdit = false;
            plot_Mobility.IsEnableHPan = false;
            plot_Mobility.IsEnableHZoom = false;
            plot_Mobility.IsEnableSelection = false;
            plot_Mobility.IsEnableVEdit = false;
            plot_Mobility.IsEnableVPan = false;
            plot_Mobility.IsEnableVZoom = false;
            plot_Mobility.IsEnableZoom = false;
            plot_Mobility.IsEnableWheelZoom = false;
            plot_Mobility.GraphPane.XAxis.Scale.Format = "F2";
            plot_Mobility.GraphPane.XAxis.Scale.MaxAuto = true;
            plot_Mobility.GraphPane.YAxis.Scale.IsLabelsInside = true;
            plot_Mobility.GraphPane.IsFontsScaled = false; // TODO:
            plot_Mobility.GraphPane.YAxis.Scale.Mag = 0;
            plot_Mobility.GraphPane.YAxis.Scale.Format = "0.0E00";
            plot_Mobility.GraphPane.Margin.Left = -5;
            plot_Mobility.GraphPane.Margin.Top = 5;
            plot_Mobility.GraphPane.Margin.Right = 40;
            plot_Mobility.GraphPane.Margin.Bottom -= 5;
            //
            // waveform_MobilityPlot
            //
            waveform_MobilityPlot.Color = Color.Crimson;
            waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, Color.Salmon);

            // Label the axes
            plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Scans";
            plot_Mobility.GraphPane.XAxis.Title.FontSpec.Family = "Verdana";
            plot_Mobility.GraphPane.XAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_Mobility.GraphPane.YAxis.Title.Text = "Drift Intensity";
            plot_Mobility.GraphPane.YAxis.Title.FontSpec.Family = "Verdana";
            plot_Mobility.GraphPane.YAxis.Title.FontSpec.Size = 8.25F * zedGraphFontScaleFactor;
            plot_Mobility.GraphPane.YAxis.Title.IsVisible = false;
            plot_Mobility.GraphPane.YAxis.Cross = 1000000;

            // Add the controls
            tab_DataViewer.Controls.Add(plot_TOF);
            tab_DataViewer.Controls.Add(plot_Mobility);
            plot_TOF.Show();

            plot_TOF.Width = 200;
            plot_Mobility.Height = 150;
        }

        #endregion

        private void ResizeThis()
        {
            if (is2DPlotFullScreen)
            {
                pnl_2DMap.Left = 0;
                pnl_2DMap.Top = 0;

                max2DPlotHeight = tab_DataViewer.ClientSize.Height;
                max2DPlotWidth = tab_DataViewer.ClientSize.Width;

                pnl_2DMap.BringToFront();

                // --------------------------------------------------------------------------------------------------
                // middle top
                elementHost_FrameControl.Left = pnl_2DMap.Left + 20;
                elementHost_FrameControl.Width = pnl_2DMap.Width - 40;
                elementHost_FrameControl.Height = 100;

                viewerIsResizing = false;

                return;
            }

            // Start at the top!
            //
            // --------------------------------------------------------------------------------------------------
            // Far left column
            btn_Refresh.Top = 4;
            btn_Refresh.Left = 4;

            lbl_ExperimentDate.Top = 4;
            lbl_ExperimentDate.Left = btn_Refresh.Left + btn_Refresh.Width + 10; // pnl_2DMap.Left + pnl_2DMap.Width - lbl_ExperimentDate.Width;

            num_maxBin.Top = elementHost_FrameControl.Top + elementHost_FrameControl.Height - num_maxBin.Height - 6;

            elementHost_FrameInfo.Top = tab_DataViewer.Height - elementHost_FrameInfo.Height - 6;
            elementHost_ChromatogramControls.Top = elementHost_FrameInfo.Top - elementHost_ChromatogramControls.Height - 6;

            num_minBin.Left = num_maxBin.Left = 20;
            plot_TOF.Left = 20;

            elementHost_FrameInfo.Left = 5;
            elementHost_ChromatogramControls.Left = 5;

            // max_plot_height ************************************************
            max2DPlotHeight = tab_DataViewer.Height - 420;

            // --------------------------------------------------------------------------------------------------
            // middle top
            elementHost_FrameControl.Left = pnl_2DMap.Left;
            elementHost_FrameControl.Width = tab_DataViewer.ClientSize.Width - elementHost_FrameControl.Left - 10;

            // --------------------------------------------------------------------------------------------------
            // Right
            elementHost_PlotAreaFormatting.Height = max2DPlotHeight;
            elementHost_PlotAreaFormatting.Top = elementHost_FrameControl.Top + elementHost_FrameControl.Height + 10;
            elementHost_PlotAreaFormatting.Left = tab_DataViewer.Width - elementHost_PlotAreaFormatting.Width - 10;

            // Middle Bottom
            num_minMobility.Top = plot_Mobility.Top + MobilityPlotHeight + 5;
            num_maxMobility.Top = num_minMobility.Top;
            lbl_TIC.Top = num_minMobility.Top;

            // pb_2DMap Size
            // max_plot_width *********************************************
            max2DPlotWidth = elementHost_PlotAreaFormatting.Left - pnl_2DMap.Left - 20;

            // --------------------------------------------------------------------------------------------------
            // selection corners
            if (menuItem_SelectionCorners.Checked)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (plot2DSelectionCorners[i].X < 0)
                        plot2DSelectionCorners[i].X = (int)(pnl_2DMap.Width * .05);
                    else if (plot2DSelectionCorners[i].X > pnl_2DMap.Width)
                        plot2DSelectionCorners[i].X = (int)(pnl_2DMap.Width * .95);

                    if (plot2DSelectionCorners[i].Y < 0)
                        plot2DSelectionCorners[i].Y = (int)(pnl_2DMap.Height * .05);
                    else if (plot2DSelectionCorners[i].Y > pnl_2DMap.Height)
                        plot2DSelectionCorners[i].Y = (int)(pnl_2DMap.Height * .95);
                }
                pnl_2DMap.Invalidate();
                return;
            }

            elementHost_MzRange.Left = tabpages_Main.Left + tabpages_Main.Width - elementHost_MzRange.Width - 45;
            elementHost_MzRange.Top = tabpages_Main.Top + tabpages_Main.Height - elementHost_MzRange.Height - 15;

            // redraw
            viewerIsResizing = false;
            needToUpdate2DPlot = true;
        }

        private void LoadGraphFrame()
        {
            lock (plot2DChangeLock)
            {
                selectingMobilityRange = false;

                lbl_ExperimentDate.Text = uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.DateStarted, "");
                ReloadCalibrationCoefficients();

                // Initialize boundaries
                newMinMobility = 0;
                newMaxMobility = uimfReader.UimfFrameParams.Scans - 1; //  imfReader.Experiment_Properties.TOFSpectraPerFrame-1;
                newMinTofBin = 0;
                newMaxTofBin = uimfReader.UimfGlobalParams.Bins - 1;

                frameMaximumMobility = newMaxMobility;
                frameMaximumTofBins = newMaxTofBin;

                num_minMobility.Minimum = -100;
                num_maxMobility.Maximum = 10000000;

                // set min and max here, they will not adjust to zooming
                applyingMobilityRangeChange = true; // prevent events form occurring.
                num_minMobility.Value = Convert.ToDecimal(newMinMobility);
                num_maxMobility.Value = Convert.ToDecimal(newMaxMobility);
                applyingMobilityRangeChange = false; // OK, clear this flag to make the controls usable

                // flag_enterBinRange = true;
                // num_minBin.Minimum = -100; //Convert.ToDecimal(new_minBin);
                // num_maxBin.Maximum = Convert.ToDecimal(new_maxBin);
                // flag_enterBinRange = false; // OK, clear this flag to make the controls usable

                try
                {
                    averageDriftScanDuration = uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                }
                catch (Exception)
                {
                    // ignore the error, can't find the file with the meanTOFscan.
                }

                currentMinTofBin = newMinTofBin;
                currentMaxTofBin = newMaxTofBin;
                currentMinMobility = newMinMobility;
                currentMaxMobility = newMaxMobility;

                // frame is created, allow frame cycling.
                needToUpdate2DPlot = true;

                vsb_2DMap.Minimum = 0;
                vsb_2DMap.Maximum = frameMaximumTofBins;
                //vsb_2DMap.SmallChange = current_valuesPerPixelY * 1000;

                hsb_2DMap.Minimum = 0;
                hsb_2DMap.Maximum = 0;
                //hsb_2DMap.SmallChange = current_valuesPerPixelX * 1000;

                num_maxMobility.Minimum = Convert.ToDecimal(0);
                num_maxMobility.Maximum = Convert.ToDecimal(frameMaximumMobility);
                num_minMobility.Minimum = Convert.ToDecimal(0);
                num_minMobility.Maximum = Convert.ToDecimal(frameMaximumMobility);

                Text = Path.GetFileNameWithoutExtension(uimfReader.UimfDataFile);

                AutoScrollPosition = new Point(0, 0);

                Show();

                // thread GraphFrame
                graphFrameThread = new Thread(GraphFrameThreadWork) { Priority = ThreadPriority.Normal };
                graphFrameThread.Start();
            }
        }

        // Generate a map out of the data, whether TOF or m/z
        //
        // wfd:  there may be a problem in here dealing with the differences between the
        //       mz plot and the TOF plot.  in the loop, you will see that the y's are going
        //       to different limits.  While it appears to work, it can not be trusted.
        private void Generate2DIntensityArray()
        {
            bool isNewFrame = false;
            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                MessageBox.Show("ERROR:  should not be here");
                return;
            }

            var frameSelectValue = frameControlVm.CurrentFrameNumber;

            // Determine the frame size
            if (uimfReader.CurrentFrameIndex != frameSelectValue)
            {
                isNewFrame = true;
                uimfReader.CurrentFrameIndex = frameSelectValue;
            }

            if (showMobilityScanNumber)
                plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Scans";
            else
                plot_Mobility.GraphPane.XAxis.Title.Text = "Mobility - Time (msec)";

            if (displayTofValues)
                plot_TOF.GraphPane.YAxis.Title.Text = "Time of Flight (usec)";
            else
                plot_TOF.GraphPane.YAxis.Title.Text = "m/z";

            ReadViewableIntensityData();

            if (isNewFrame && isTImsData)
                plot_Mobility.set_TIMSRamp(uimfReader.UimfFrameParams.MassCalibrationCoefficients.a2, uimfReader.UimfFrameParams.MassCalibrationCoefficients.b2,
                    uimfReader.UimfFrameParams.MassCalibrationCoefficients.c2, uimfReader.UimfFrameParams.Scans,
                    (int) (7500000.0/uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength))); // msec gap

            if (viewerIsClosing)
            {
                return;
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void ReadViewableIntensityData()
        {
            if (currentlyReadingData)
                return;
            currentlyReadingData = true;

            int maxMzRangeBin;
            int minMzRangeBin;
            if (mzRangeVm.RangeEnabled)
            {
                var selectMz = mzRangeVm.Mz;
                var selectPpm = mzRangeVm.ComputedTolerance;
                minMzRangeBin = (int)(uimfReader.MzCalibration.MZtoTOF(selectMz - selectPpm) / uimfReader.TenthsOfNanoSecondsPerBin);
                maxMzRangeBin = (int)(uimfReader.MzCalibration.MZtoTOF(selectMz + selectPpm) / uimfReader.TenthsOfNanoSecondsPerBin);

                currentMinTofBin = (int)(uimfReader.MzCalibration.MZtoTOF(selectMz - (selectPpm * 1.5)) / uimfReader.TenthsOfNanoSecondsPerBin);
                currentMaxTofBin = (int)(uimfReader.MzCalibration.MZtoTOF(selectMz + (selectPpm * 1.5)) / uimfReader.TenthsOfNanoSecondsPerBin);
            }
            else
            {
                minMzRangeBin = 0;
                maxMzRangeBin = uimfReader.UimfGlobalParams.Bins;
            }

            if (currentMaxTofBin < currentMinTofBin)
            {
                MessageBox.Show("(current_maxBin < current_minBin): (" + currentMaxTofBin.ToString() + " < " + currentMinTofBin.ToString() + ")" + frameMaximumTofBins.ToString());

                var temp = currentMinTofBin;
                currentMinTofBin = currentMaxTofBin;
                currentMaxTofBin = temp;
            }
            var totalBins = (currentMaxTofBin - currentMinTofBin) + 1;

            if (currentMaxMobility < currentMinMobility)
            {
                var temp = currentMinMobility;
                currentMinMobility = currentMaxMobility;
                currentMaxMobility = temp;
            }
            var totalMobility = (currentMaxMobility - currentMinMobility) + 1;

            // resize data to fit screen
            if (max2DPlotHeight < totalBins)
            {
                currentValuesPerPixelY = (totalBins / max2DPlotHeight);

                currentMaxTofBin = currentMinTofBin + (currentValuesPerPixelY * max2DPlotHeight);

                if (currentMaxTofBin > frameMaximumTofBins)
                {
                    currentMinTofBin -= (currentMaxTofBin - frameMaximumTofBins);
                    currentMaxTofBin = frameMaximumTofBins - 1;
                }
                if (currentMinTofBin < 0)
                {
                    MessageBox.Show("Bill " + "(" + currentMaxTofBin.ToString() + " < " + currentMinTofBin.ToString() + ")\n\n" + max2DPlotHeight.ToString() + " < " + totalBins.ToString() + "\n\nget_ViewableIntensities: current_maxBin is already maximum_Bins  -- should never happen");
                    currentMinTofBin = 0;
                }

                totalBins = (currentMaxTofBin - currentMinTofBin) + 1;
                currentValuesPerPixelY = (totalBins / max2DPlotHeight);
            }
            else // the pixels get taller...
            {
                currentValuesPerPixelY = -(max2DPlotHeight / totalBins);
                if (currentValuesPerPixelY >= 0)
                    currentValuesPerPixelY = -1;

                // create calibration table
                currentMaxTofBin = currentMinTofBin + (max2DPlotHeight / -currentValuesPerPixelY);

                if (currentMaxTofBin > frameMaximumTofBins)
                {
                    currentMaxTofBin = frameMaximumTofBins;
                    currentMinTofBin = frameMaximumTofBins - (max2DPlotHeight / -currentValuesPerPixelY);
                }
                if (currentMinTofBin < 0)
                {
                    currentMinTofBin = 0;
                    currentMaxTofBin = (max2DPlotHeight / -currentValuesPerPixelY);
                }

                if ((currentMaxTofBin - currentMinTofBin) < MinGraphedBins)
                {
                    currentMinTofBin = ((currentMaxTofBin + currentMinTofBin) - MinGraphedBins) / 2;
                    currentMaxTofBin = currentMinTofBin + MinGraphedBins;
                }

                totalBins = (currentMaxTofBin - currentMinTofBin) + 1;
                currentValuesPerPixelY = -(max2DPlotHeight / totalBins);

                // OK, make sure we have a good fit on the screen.
                if (currentValuesPerPixelY >= 0)
                {
                    currentValuesPerPixelY = -1;
                    if ((totalBins * -currentValuesPerPixelY) + 1 > max2DPlotHeight)
                    {
                        currentMaxTofBin = currentMinTofBin + max2DPlotHeight;
                        totalBins = (currentMaxTofBin - currentMinTofBin) + 1;
                    }
                }
                else
                {
                    // good enough- just awful.
                    while (((totalBins + 1) * -currentValuesPerPixelY) + 1 < max2DPlotHeight)
                    {
                        //int offset_fit = (max_plot_height - ((total_bins+1) * -current_valuesPerPixelY))/2;
                        currentMinTofBin--;
                        currentMaxTofBin++;
                        totalBins = (currentMaxTofBin - currentMinTofBin) + 1;
                    }
                }
            }

            int new2DMapHeight;
            if (currentValuesPerPixelY > 0)
                new2DMapHeight = (totalBins / currentValuesPerPixelY) + 1;
            else
                new2DMapHeight = (totalBins * -currentValuesPerPixelY) + 1;

            if (pnl_2DMap.Height != new2DMapHeight)
            {
                if (pnl_2DMap.InvokeRequired)
                {
                    pnl_2DMap.Invoke(new MethodInvoker(() => { pnl_2DMap.Height = new2DMapHeight; }));
                }
                else
                {
                    pnl_2DMap.Height = new2DMapHeight;
                }
                viewerNeedsResizing = true;
            }

            if (max2DPlotWidth < totalMobility)
            {
                currentValuesPerPixelX = (totalMobility / max2DPlotWidth) + 1;

                currentMaxMobility = currentMinMobility + (max2DPlotWidth * currentValuesPerPixelX);
                if (currentMinMobility < 0)
                {
                    currentMinMobility = 0;
                    currentMaxMobility = (max2DPlotWidth * currentValuesPerPixelY);
                }

                if (currentMaxMobility > frameMaximumMobility)
                    currentMaxMobility = frameMaximumMobility;
            }
            else
            {
                currentValuesPerPixelX = -(max2DPlotWidth / totalMobility);
                // MessageBox.Show("max_plot_width=" + max_plot_width + ", current_valuesPerPixelX=" + current_valuesPerPixelX.ToString());

#if false // erin did not like my attempt at extending out the plot.  Aug 2, 2010
                    current_maxMobility = current_minMobility + (max_plot_width / -current_valuesPerPixelX) - 1;

                    if (current_maxMobility > maximum_Mobility)
                    {
                        current_maxMobility = maximum_Mobility;
                        current_minMobility = maximum_Mobility - (max_plot_width / -current_valuesPerPixelX);
                    }
                    if (current_minMobility < 0)
                    {
                        current_minMobility = 0;
                        current_maxMobility = (max_plot_width / -current_valuesPerPixelX);
                    }
                    if (current_maxMobility > maximum_Mobility)
                        current_maxMobility = maximum_Mobility;
#endif
            }

            totalMobility = (currentMaxMobility - currentMinMobility) + 1;

            // calculate width of data
            int new2DMapWidth;
            if (currentValuesPerPixelX > 0)
                new2DMapWidth = (totalMobility / currentValuesPerPixelX) + 1;
            else
                new2DMapWidth = (totalMobility * -currentValuesPerPixelX) + 1;

            if (pnl_2DMap.Width != new2DMapWidth)
            {
                viewerNeedsResizing = true;
                if (pnl_2DMap.InvokeRequired)
                {
                    pnl_2DMap.Invoke(new MethodInvoker(() => { pnl_2DMap.Width = new2DMapWidth; }));
                }
                else
                {
                    pnl_2DMap.Width = new2DMapWidth;
                }
            }

            // create array to store visual data
            int dataWidth;
            if (currentValuesPerPixelX < 0)
                dataWidth = totalMobility;
            else
                dataWidth = pnl_2DMap.Width;

            int dataHeight;
            if (currentValuesPerPixelY < 0)
                dataHeight = totalBins;
            else
                dataHeight = pnl_2DMap.Height;

#if OLD // TODO:
            data_2D = new int[data_width][];
            for (int n = 0; n < data_width; n++)
                data_2D[n] = new int[data_height];
#endif

            // show frame range
            var frameSelectValue = frameControlVm.CurrentFrameNumber;
            frameControlView.Dispatcher.Invoke(() =>
            {
                if ((frameSelectValue - frameControlVm.SummedFrames + 1) < 0)
                    frameControlVm.MinimumSummedFrame = 0;
                else
                    frameControlVm.MinimumSummedFrame = (((frameSelectValue - frameControlVm.SummedFrames + 1)));
                frameControlVm.MaximumSummedFrame = frameSelectValue;
            });

            var startIndex = uimfReader.CurrentFrameIndex - (uimfReader.FrameWidth - 1);
            var endIndex = uimfReader.CurrentFrameIndex;

            // collect the data
#if OLD // TODO:
            for (frames = start_index; (frames <= end_index) && !flag_Closing; frames++)
            {
                // lbl_ExperimentDate.Text = "accumulate_FrameData: " + (++count_times).ToString() + "  "+start_index.ToString()+"<"+end_index.ToString();

                try
                {
                    if (data_2D == null)
                        MessageBox.Show("null");
                    data_2D = uimfReader.AccumulateFrameData(frames, flag_display_as_TOF, current_minMobility, current_minBin, min_MZRange_bin, max_MZRange_bin, data_2D, current_valuesPerPixelY);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("accumulate_FrameData:  " + ex.ToString());
                }
            }
#endif
            /*/
            data_2D = uimfReader.AccumulateFrameData(uimfReader.ArrayFrameNum[start_index], uimfReader.ArrayFrameNum[end_index], flag_display_as_TOF,
                current_minMobility, current_minMobility + data_width, current_minBin, current_minBin + (data_height * current_valuesPerPixelY),
                current_valuesPerPixelY, data_2D, min_MZRange_bin, max_MZRange_bin);
            /*/
            data_2D = uimfReader.AccumulateFrameDataByCount(uimfReader.ArrayFrameNum[startIndex], uimfReader.ArrayFrameNum[endIndex], displayTofValues,
                currentMinMobility, dataWidth, currentMinTofBin, dataHeight, currentValuesPerPixelY, /*data_2D*/ null, minMzRangeBin, maxMzRangeBin, xCompression: currentValuesPerPixelX);
            /**/

            try
            {
                int selMin;
                int selMax;
                if (showMobilityScanNumber)
                {
                    selMin = mobilitySelectionMinimum - currentMinMobility;
                    selMax = mobilitySelectionMaximum - currentMinMobility;
                }
                else
                {
                    selMin = mobilitySelectionMinimum - (int)(currentMinMobility * (averageDriftScanDuration / 1000000));
                    selMax = mobilitySelectionMaximum - (int)(currentMinMobility * (averageDriftScanDuration / 1000000));
                }

                current2DPlotMaxIntensity = 0;
                mobilityTicData = new double[dataWidth];
                tofTicData = new double[dataHeight];

                for (var currentScan = 0; currentScan < dataWidth; currentScan++)
                {
                    for (var binValue = 0; binValue < dataHeight; binValue++)
                    {
                        if (InsidePolygonPixel(currentScan, binValue))
                        {
                            mobilityTicData[currentScan] += data_2D[currentScan][binValue];

                            if (!selectingMobilityRange || ((currentScan >= selMin) && (currentScan <= selMax)))
                                tofTicData[binValue] += data_2D[currentScan][binValue];

                            if (data_2D[currentScan][binValue] > current2DPlotMaxIntensity)
                            {
                                current2DPlotMaxIntensity = data_2D[currentScan][binValue];
                                plot2DMaxIntensityX = currentScan;
                                plot2DMaxIntensityY = binValue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            ReloadCalibrationCoefficients();

            // point to the selected experiment whether it is enabled or not

            if (!is2DPlotFullScreen)
            {
                SetMobilityPlotData(mobilityTicData);
                SetTofMzPlotData(tofTicData);

                plot_TOF.Invoke(new MethodInvoker(() => {
                    // align everything
                    if (currentValuesPerPixelY > 0)
                    {
                        plot_TOF.Height = pnl_2DMap.Height + plot_TOF.Height - (int)plot_TOF.GraphPane.Chart.Rect.Height;
                        plot_TOF.Top = num_maxBin.Top + num_maxBin.Height + 4;
                    }
                    else
                    {
                        plot_TOF.Height = pnl_2DMap.Height + plot_TOF.Height - (int)plot_TOF.GraphPane.Chart.Rect.Height + currentValuesPerPixelY;
                        plot_TOF.Top = num_maxBin.Top + num_maxBin.Height + 4 - currentValuesPerPixelY / 2;
                    }

                    num_minBin.Top = plot_TOF.Top + plot_TOF.Height + 4;
                    vsb_2DMap.Height = pnl_2DMap.Height;

                    pnl_2DMap.Top = num_maxBin.Top + num_maxBin.Height + 4 + (int)plot_TOF.GraphPane.Chart.Rect.Top;
                    hsb_2DMap.Top = pnl_2DMap.Top - hsb_2DMap.Height;
                    vsb_2DMap.Top = pnl_2DMap.Top;

                    if ((plot_TOF.Top + plot_TOF.Height) < (pnl_2DMap.Top + pnl_2DMap.Height + 16))
                        plot_Mobility.Top = pnl_2DMap.Top + pnl_2DMap.Height + 16;
                    else
                        plot_Mobility.Top = plot_TOF.Top + plot_TOF.Height;
                    num_minMobility.Top = num_maxMobility.Top = plot_Mobility.Top + plot_Mobility.Height + 4;

                    if (currentValuesPerPixelX > 0)
                    {
                        plot_Mobility.Left = plot_TOF.Left + plot_TOF.Width;
                        plot_Mobility.Width = pnl_2DMap.Width + plot_Mobility.Width - (int)plot_Mobility.GraphPane.Chart.Rect.Width;
                    }
                    else
                    {
                        plot_Mobility.Width = pnl_2DMap.Width + plot_Mobility.Width - (int)plot_Mobility.GraphPane.Chart.Rect.Width + currentValuesPerPixelX;
                        plot_Mobility.Left = plot_Mobility.Left = plot_TOF.Left + plot_TOF.Width - currentValuesPerPixelX / 2;
                    }

                    num_minMobility.Left = plot_Mobility.Left;
                    num_maxMobility.Left = plot_Mobility.Left + plot_Mobility.Width - num_maxMobility.Width; //- (plot_Mobility.PlotAreaBounds.Width - pnl_2DMap.Width)
                    lbl_TIC.Top = num_minMobility.Top;
                    lbl_TIC.Left = (num_maxMobility.Left - num_minMobility.Left) / 2 + num_minMobility.Left;

                    pnl_2DMap.Left = plot_TOF.Left + plot_TOF.Width + (int)plot_Mobility.GraphPane.Chart.Rect.Left;
                    hsb_2DMap.Left = pnl_2DMap.Left;

                    hsb_2DMap.Width = pnl_2DMap.Width;
                    vsb_2DMap.Left = pnl_2DMap.Left + pnl_2DMap.Width;
                }));

                CalcTicAndDisplay();
            }

            currentlyReadingData = false;
        }

        private void CalcTicAndDisplay()
        {
            var tic = 0L;
            if (data_2D != null && data_2D.Length > 0 && data_2D[0].Length > 0)
            {
                tic = data_2D.Aggregate(tic, (current, t) => current + t.Sum());
            }

            if (lbl_TIC.InvokeRequired)
            {
                lbl_TIC.Invoke(new MethodInvoker(() => lbl_TIC.Text = $"TIC: {tic:0.00 E+00}"));
            }
            else
            {
                lbl_TIC.Text = $"TIC: {tic:0.00 E+00}";
            }
        }

        private void Generate2DIntensityArrayForChromatogram()
        {
            var totalFrames = uimfReader.GetNumberOfFrames(uimfReader.CurrentFrameType);
            var totalScans = uimfReader.UimfFrameParams.Scans;
            var dataWidth = totalFrames / chromatogramControlVm.FrameCompression;

            int maxMzRangeBin;
            int minMzRangeBin;
            if (mzRangeVm.RangeEnabled)
            {
                var selectMz = mzRangeVm.Mz;
                var selectPpm = mzRangeVm.ComputedTolerance;
                minMzRangeBin = (int) (uimfReader.MzCalibration.MZtoTOF(selectMz - selectPpm) / uimfReader.TenthsOfNanoSecondsPerBin);
                maxMzRangeBin = (int) (uimfReader.MzCalibration.MZtoTOF(selectMz + selectPpm) / uimfReader.TenthsOfNanoSecondsPerBin);
            }
            else
            {
                minMzRangeBin = 0;
                maxMzRangeBin = uimfReader.UimfGlobalParams.Bins;
            }

            if (!completeChromatogramCollected && !partialChromatogramCollected)
            {
                CreateProgressBar();

                // only collect this one time.
                chromatogramData = new int[totalFrames / chromatogramControlVm.FrameCompression][];
                for (var mobilityIndex = 0; mobilityIndex < totalFrames / chromatogramControlVm.FrameCompression; mobilityIndex++)
                    chromatogramData[mobilityIndex] = new int[totalScans + 1];

                currentlyReadingData = true;

                int compressionCollection;
                if (chromatogramControlVm.PartialPeakChromatogramChecked)
                    compressionCollection = 1;
                else
                    compressionCollection = chromatogramControlVm.FrameCompression;

                for (var mobilityIndex = 0; (mobilityIndex < dataWidth) && viewerKeepAlive; mobilityIndex++) // wfd
                {
                    int compression;
                    for (compression = 0; compression < compressionCollection; compression++)
                    {
                        progress_ReadingFile.Value = mobilityIndex;
                        progress_ReadingFile.Update();

                        var frameIndex = (mobilityIndex * chromatogramControlVm.FrameCompression) + compression;
                        //MessageBox.Show(frame_index.ToString());

                        var mobilityData = uimfReader.GetDriftChromatogram(frameIndex, minMzRangeBin, maxMzRangeBin);
                        for (var i = 0; i < mobilityData.Length; i++)
                            chromatogramData[mobilityIndex][i] += mobilityData[i];
                    }
                }

                progress_ReadingFile.Dispose();

                currentlyReadingData = false;

                if (chromatogramControlVm.CompletePeakChromatogramChecked)
                    completeChromatogramCollected = true;
                else
                    partialChromatogramCollected = true;

                if (!viewerKeepAlive)
                    return;
            }

            // -------------------------------------------------------------------------
            // data collected put it into the data_2d array, compress for viewing.
            //
            // allow the chromatogram to compress vertically; but not horizontally.
            //
            currentMinMobility = hsb_2DMap.Value;
            chromatogramValuesPerPixelX = -1;

            //  MessageBox.Show("("+max_plot_width.ToString()+" < "+total_frames.ToString()+")"+data_width.ToString());

            if (max2DPlotWidth < totalFrames)
            {
                currentMaxMobility = totalFrames;

                // in this case we will not overlap pixels.  We can create another scrollbar to handle too wide plots
                chromatogramValuesPerPixelX = -1;

                currentMinMobility = hsb_2DMap.Value;
                currentMaxMobility = currentMinMobility + max2DPlotWidth;
            }
            else
            {
                currentMaxMobility = max2DPlotWidth;

                chromatogramValuesPerPixelX = -(max2DPlotWidth / totalFrames);

                currentMaxMobility = currentMinMobility + (max2DPlotWidth / -chromatogramValuesPerPixelX) - 1;
                if (currentMaxMobility > frameMaximumMobility)
                {
                    currentMaxMobility = frameMaximumMobility;
                    currentMinMobility = frameMaximumMobility - (max2DPlotWidth / -chromatogramValuesPerPixelX);
                }
                if (currentMinMobility < 0)
                {
                    currentMinMobility = 0;
                    currentMaxMobility = (max2DPlotWidth / -chromatogramValuesPerPixelX);
                }
                if (currentMaxMobility > frameMaximumMobility)
                    currentMaxMobility = frameMaximumMobility;
            }

            // total_frames = (current_maxMobility - current_minMobility) + 1;
            int new2DMapWidth;
            if (chromatogramValuesPerPixelX > 0)
                new2DMapWidth = (dataWidth / chromatogramValuesPerPixelX) + 1;
            else
                new2DMapWidth = (dataWidth * -chromatogramValuesPerPixelX) + 1;

            if (new2DMapWidth > elementHost_PlotAreaFormatting.Left - pnl_2DMap.Left)
                tab_DataViewer.Width = pnl_2DMap.Left + new2DMapWidth + 175;
            else
            {
                chromatogramValuesPerPixelX = -((((elementHost_PlotAreaFormatting.Left - pnl_2DMap.Left) / new2DMapWidth) * new2DMapWidth) / dataWidth);
                new2DMapWidth = (dataWidth * -chromatogramValuesPerPixelX) + 1;
            }

            if (pnl_2DMap.Width != new2DMapWidth)
            {
                pnl_2DMap.Width = new2DMapWidth;
                viewerNeedsResizing = true;
            }

            if (currentMaxMobility > totalFrames)
            {
                currentMaxMobility = totalFrames - 1;// -pnl_2DMap.Width - 1;
                currentMinMobility = currentMaxMobility - pnl_2DMap.Width;
            }

            chromatogramValuesPerPixelY = 1; //(total_scans / max_plot_height);
            currentMinTofBin = 0;
            if (max2DPlotHeight > totalScans - 1)
                currentMaxTofBin = currentMinTofBin + totalScans - 1;
            else
                currentMaxTofBin = currentMinTofBin + max2DPlotHeight;

            totalScans = (currentMaxTofBin - currentMinTofBin);
            chromatogramValuesPerPixelY = 1; //(total_scans / max_plot_height);

            var new2DMapHeight = (totalScans / chromatogramValuesPerPixelY) + 1;
            if (pnl_2DMap.Height != new2DMapHeight)
            {
                pnl_2DMap.Height = new2DMapHeight;
            }

            //-----------------------------------------------------------------------------------------
            // create array to store visual data
            int dataHeight;
            if (chromatogramValuesPerPixelY < 0)
                dataHeight = totalScans;
            else
                dataHeight = pnl_2DMap.Height;

            var newData_2D = new int[dataWidth][];
            for (int n = 0; n < dataWidth; n++)
                newData_2D[n] = new int[dataHeight];

            //-----------------------------------------------------------------------------------------
            // collect the data for viewing.
            chromatogramMax = 0;

            if (dataWidth > pnl_2DMap.Width)
            {
                hsb_2DMap.SmallChange = pnl_2DMap.Width / 5;
                hsb_2DMap.LargeChange = pnl_2DMap.Width * 4 / 5;

                hsb_2DMap.Maximum = dataWidth; // -hsb_2DMap.LargeChange - 1;
                // MessageBox.Show(total_frames.ToString());
                num_maxMobility.Maximum = totalFrames;
                chromatogramMinFrame = currentMinMobility; //  hsb_2DMap.Value;
                //  lbl_ExperimentDate.Text = hsb_2DMap.Maximum.ToString() + ", " + minFrame_Chromatogram.ToString();
            }
            else
            {
                hsb_2DMap.Maximum = totalFrames - dataWidth;
                chromatogramMinFrame = 0;
            }

            chromatogramMaxFrame = chromatogramMinFrame + pnl_2DMap.Width;
            // MessageBox.Show("0 "+chromatogram_valuesPerPixelY.ToString());

            // ok, making chromatogram_valuesPerPixelX always negative.
            if (chromatogramValuesPerPixelY < 0)
            {
                //MessageBox.Show("here");
                // pixel_y = 1;

                for (var frameIndex = 0; frameIndex < dataWidth; frameIndex++)
                {
                    for (var mobilityIndex = 0; mobilityIndex < dataHeight; mobilityIndex++)
                    {
                        newData_2D[frameIndex][mobilityIndex] += chromatogramData[frameIndex + chromatogramMinFrame][mobilityIndex];

                        if (newData_2D[frameIndex][mobilityIndex] > current2DPlotMaxIntensity)
                        {
                            chromatogramMax = newData_2D[frameIndex][mobilityIndex];

                            plot2DMaxIntensityX = frameIndex;
                            plot2DMaxIntensityY = mobilityIndex;
                        }
                    }
                }
                MessageBox.Show("max: " + current2DPlotMaxIntensity.ToString());
            }
            else
            {
                // MessageBox.Show("height: " + data_height.ToString() + ", " + chromatogram_data[0].Length.ToString());
                // MessageBox.Show("width: " + data_width.ToString() + ", " + chromatogram_data.Length.ToString());
                for (var frameIndex = 0; (frameIndex < dataWidth); frameIndex++)
                    for (var mobilityIndex = 0; mobilityIndex < dataHeight; mobilityIndex++)
                    {
                        newData_2D[frameIndex][mobilityIndex] = chromatogramData[frameIndex + chromatogramMinFrame][mobilityIndex];

                        if (newData_2D[frameIndex][mobilityIndex] > chromatogramMax)
                        {
                            chromatogramMax = newData_2D[frameIndex][mobilityIndex];
                            plot2DMaxIntensityX = frameIndex;
                            plot2DMaxIntensityY = mobilityIndex;
                        }
                    }

                current2DPlotMaxIntensity = chromatogramMax;
                //  MessageBox.Show("done: "+pnl_2DMap.Width.ToString());
            }

            //   MessageBox.Show("1");

            // ------------------------------------------------------------------------------
            // create the side plots
            chromatogramMobilityTicData = new double[dataWidth];
            chromatogramTofTicData = new double[dataHeight];
            for (var frameIndex = 0; frameIndex < dataWidth; frameIndex++)
                for (var mobilityIndex = 0; mobilityIndex < dataHeight; mobilityIndex++)
                {
                    // peak chromatogram
                    if (newData_2D[frameIndex][mobilityIndex] > chromatogramMobilityTicData[frameIndex])
                        chromatogramMobilityTicData[frameIndex] = newData_2D[frameIndex][mobilityIndex];

                    chromatogramTofTicData[mobilityIndex] += newData_2D[frameIndex][mobilityIndex];
                    if (newData_2D[frameIndex][mobilityIndex] > current2DPlotMaxIntensity)
                    {
                        current2DPlotMaxIntensity = newData_2D[frameIndex][mobilityIndex];
                        plot2DMaxIntensityX = frameIndex;
                        plot2DMaxIntensityY = mobilityIndex;
                    }
                }

            if (showMobilityScanNumber)
                plot_TOF.GraphPane.YAxis.Title.Text = "Mobility - Scans";
            else
                plot_TOF.GraphPane.YAxis.Title.Text = "Mobility - Time (msec)";

            SetTofMzPlotData(chromatogramTofTicData);
            SetMobilityPlotData(chromatogramMobilityTicData);

            // align everything
            plot_TOF.Top = num_maxBin.Top + num_maxBin.Height + 4;
            plot_TOF.Height = elementHost_ChromatogramControls.Top - plot_TOF.Top - 30;

            num_minBin.Top = plot_TOF.Top + plot_TOF.Height + 4;

            plot_Mobility.Top = plot_TOF.Top + plot_TOF.Height;
            num_minMobility.Top = num_maxMobility.Top = plot_Mobility.Top + plot_Mobility.Height + 4;
            vsb_2DMap.Height = pnl_2DMap.Height;

            pnl_2DMap.Top = num_maxBin.Top + num_maxBin.Height + 4 + (int)plot_TOF.GraphPane.Chart.Rect.Top;
            hsb_2DMap.Top = pnl_2DMap.Top - hsb_2DMap.Height;
            vsb_2DMap.Top = pnl_2DMap.Top;
            // MessageBox.Show("3");

            if (chromatogramValuesPerPixelX > 0)
            {
                plot_Mobility.Left = plot_TOF.Left + plot_TOF.Width + chromatogramValuesPerPixelX/2;
                plot_Mobility.Width = pnl_2DMap.Width + plot_Mobility.Width - (int)plot_Mobility.GraphPane.Chart.Rect.Width - chromatogramValuesPerPixelX;
            }
            else
            {
                plot_Mobility.Width = pnl_2DMap.Width + plot_Mobility.Width - (int)plot_Mobility.GraphPane.Chart.Rect.Width + chromatogramValuesPerPixelX;
                plot_Mobility.Left = plot_TOF.Left + plot_TOF.Width + (-chromatogramValuesPerPixelX / 2);
            }

            num_minMobility.Left = plot_Mobility.Left;
            num_maxMobility.Left = plot_Mobility.Left + plot_Mobility.Width - num_maxMobility.Width; //- ((int)plot_Mobility.GraphPane.Chart.Rect.Width - pnl_2DMap.Width)
            lbl_TIC.Top = num_minMobility.Top;
            lbl_TIC.Left = (num_maxMobility.Left - num_minMobility.Left) / 2 + num_minMobility.Left;

            pnl_2DMap.Left = plot_TOF.Left + plot_TOF.Width + (int)plot_Mobility.GraphPane.Chart.Rect.Left;
            hsb_2DMap.Left = pnl_2DMap.Left;

            hsb_2DMap.Width = pnl_2DMap.Width;
            vsb_2DMap.Left = pnl_2DMap.Left + pnl_2DMap.Width;
            data_2D = newData_2D;
            ResizeThis();

            currentlyReadingData = false;
        }

        /**********************************************************************
        * This is where the work is done
        */
        [STAThread]
        private void GraphFrameThreadWork()
        {
            // Initial values
            var newFrameNumber = 0;
            frameControlView.Dispatcher.Invoke(() => frameControlVm.CurrentFrameNumber = 0);

            // Run in a loop until flag_Alive is false
            while (viewerKeepAlive)
            {
                if (!pnl_2DMap.Visible && !frameTypeChanged)
                {
                    Thread.Sleep(200);
                    continue;
                }

                if (viewerNeedsResizing && !viewerIsResizing)
                {
                    viewerIsResizing = true;
                    viewerNeedsResizing = false;
                    Invoke(new MethodInvoker(ResizeThis));
                }

                try
                {
                    while (needToUpdate2DPlot && viewerKeepAlive)
                    {
                        needToUpdate2DPlot = false;

                        if (frameTypeChanged)
                        {
                            frameTypeChanged = false;
                            FilterFramesByType(uimfReader.CurrentFrameType);
                            uimfReader.CurrentFrameIndex = 0;
                        }

                        if (uimfReader.GetNumberOfFrames(uimfReader.CurrentFrameType) <= 0)
                        {
                            needToUpdate2DPlot = false;
                            break;
                        }

                        if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                        {
                            Graph2DPlot();
                            needToUpdate2DPlot = false;
                            break;
                        }

                        var currentFrameNumber = uimfReader.LoadFrame(uimfReader.CurrentFrameIndex);
                        if (newFrameNumber != currentFrameNumber)
                        {
                            newFrameNumber = currentFrameNumber;

                            ReloadCalibrationCoefficients();
                        }

                        if (uimfReader.CurrentFrameIndex < uimfReader.GetNumberOfFrames(uimfReader.CurrentFrameType))
                        {
                            //#if false
                            if (menuItem_ScanTime.Checked)
                            {
                                // MessageBox.Show("tof scan time: " + mean_TOFScanTime.ToString());
                                // Get the mean TOF scan time
                                averageDriftScanDuration = uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                                if (averageDriftScanDuration <= 0)
                                {
                                    menuItem_Mobility.PerformClick();
                                }
                            }

                            if ((currentMinMobility != newMinMobility) ||
                                (currentMaxMobility != newMaxMobility) ||
                                (currentMaxTofBin != newMaxTofBin) ||
                                (currentMinTofBin != newMinTofBin))
                            {
                                if (newMinMobility < 0)
                                    currentMinMobility = 0;
                                else
                                    currentMinMobility = newMinMobility;

                                if (newMaxMobility > frameMaximumMobility)
                                    currentMaxMobility = frameMaximumMobility;
                                else
                                    currentMaxMobility = newMaxMobility;

                                if (newMaxTofBin > frameMaximumTofBins)
                                    currentMaxTofBin = frameMaximumTofBins;
                                else
                                    currentMaxTofBin = newMaxTofBin;
                                if (newMinTofBin < 0)
                                    currentMinTofBin = 0;
                                else
                                    currentMinTofBin = newMinTofBin;
                            }

                            try
                            {
                               //  MessageBox.Show(this, "slide_FrameSelect.Value: " + slide_FrameSelect.Value.ToString()+"("+current_frame_index.ToString()+")");
                                Graph2DPlot();
                            }
                            catch (NullReferenceException)
                            {
                                BackColor = Color.White;
                                Thread.Sleep(100);
                                needToUpdate2DPlot = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("tick_GraphFrame Graph2DPlot: " + ex + "\n\n" + ex.StackTrace);
                            }

                            if (playingCinemaPlot)
                            {
                                frameControlView.Dispatcher.Invoke(() =>
                                {
                                    if ((frameControlVm.CurrentFrameNumber + frameCinemaDataInterval >= 0) &&
                                        (frameControlVm.CurrentFrameNumber + frameCinemaDataInterval <= frameControlVm.MaximumFrameNumber))
                                    {
                                        frameControlVm.CurrentFrameNumber += frameCinemaDataInterval;
                                    }
                                    else
                                    {
                                        if (frameCinemaDataInterval > 0)
                                        {
                                            StopCinema();
                                            frameControlVm.CurrentFrameNumber = frameControlVm.MaximumFrameNumber;
                                        }
                                        else
                                        {
                                            StopCinema();
                                            frameControlVm.CurrentFrameNumber = frameControlVm.CurrentFrameNumber - 1;
                                        }
                                    }
                                });

                                needToUpdate2DPlot = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Invoke(new MethodInvoker(() =>
                    {
                        MessageBox.Show(this, "cycle_GraphFrame: " + ex + "\n\n" + ex.StackTrace);
                    }));
                }

                Thread.Sleep(500);
            }
        }

        /***************************************************************
         * The sections below only display and do not set the following values
         *
         *      current_minBin, current_maxBin
         *      current_minMobility, current_maxMobility
         */

        // ///////////////////////////////////////////////////////////////
        // Graph2DPlot()
        //
        public void Graph2DPlot()
        {
            var frameIndex = uimfReader.CurrentFrameIndex;
            if (frameIndex >= uimfReader.GetNumberOfFrames(uimfReader.CurrentFrameType))
            {
                MessageBox.Show("Graph2DPlot: " + frameIndex+"\n\nAttempting to graph frame beyond list");
                return;
            }

            if (WindowState == FormWindowState.Minimized)
                return;

            if (data_2D == null)
            {
                MessageBox.Show("Graph2DPlot(): data for frame is null");
                return;
            }

            disableMouseControls = true;

            lock (plot2DChangeLock)
            {
                try
                {
                    currentMaxMobility = newMaxMobility;
                    currentMinMobility = newMinMobility;
                    currentMaxTofBin = newMaxTofBin;
                    currentMinTofBin = newMinTofBin;

                    currentValuesPerPixelX = (currentMaxMobility - currentMinMobility + 1 < pnl_2DMap.Width) ?
                        -(pnl_2DMap.Width / (currentMaxMobility - currentMinMobility + 1)) : 1;

                    // For initial viz., don't want to expand widths of datasets with few TOFs
                    // if(current_maxMobility == imfReader.Experiment_Properties.TOFSpectraPerFrame-1 && current_minMobility== 0)
                    if (currentMaxMobility == uimfReader.UimfFrameParams.Scans - 1 && currentMinMobility == 0)
                        currentValuesPerPixelX = 1;

                    currentValuesPerPixelY = ((currentMaxTofBin - currentMinTofBin + 1 < pnl_2DMap.Height) ?
                        -(pnl_2DMap.Height / (currentMaxTofBin - currentMinTofBin + 1)) : ((currentMaxTofBin - currentMinTofBin + 1) / pnl_2DMap.Height));

                    // In case current_maxBin - current_minBin + 1 is not evenly divisible by current_valuesPerPixelY, we need to adjust one of
                    // these quantities to make it so.
                    if (currentValuesPerPixelY > 0)
                    {
                        currentMaxTofBin = currentMinTofBin + (pnl_2DMap.Height * currentValuesPerPixelY) - 1;
                        waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.DarkBlue);
                    }
                    else
                    {
                        if (currentValuesPerPixelY < -5)
                        {
                            waveform_TOFPlot.Symbol = new Symbol(SymbolType.Circle, Color.DarkBlue);
                            waveform_TOFPlot.Symbol.Fill.Color = Color.Transparent;
                        }
                        else
                        {
                            waveform_TOFPlot.Symbol = new Symbol(SymbolType.None, Color.DarkBlue);
                        }
                    }

                    if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                    {
                        try
                        {
                            Generate2DIntensityArrayForChromatogram();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Graph2DPlot chromatogram:  " + ex);
                        }

                        if (viewerIsClosing)
                            return;

                        pnl_2DMap.Size = new Size(pnl_2DMap.Width, pnl_2DMap.Height);

                        // Identify the picture frame with my new Bitmap.
                        if (pnl_2DMap.BackgroundImage == null)
                        {
                            pnl_2DMap.BackgroundImage = new Bitmap(pnl_2DMap.Width, pnl_2DMap.Height);
                            bitmap = new Bitmap(pnl_2DMap.Width, pnl_2DMap.Height);
                        }

                        // Spit out the data to screen
                        DrawBitmap(data_2D, current2DPlotMaxIntensity);

                        pnl_2DMap.Size = new Size(pnl_2DMap.Width, (int)plot_TOF.GraphPane.Chart.Rect.Height);
                    }
                    else
                    {
                        try
                        {
                            Generate2DIntensityArray();
                        }
                        catch (Exception ex)
                        {
                            BackColor = Color.Black;
                            MessageBox.Show("Graph2DPlot() Generate2DIntensityArray(): " + ex+"\n\n"+ex.StackTrace);
                        }

                        if (viewerIsClosing)
                            return;

                        if (data_2D == null)
                            MessageBox.Show("no data");
                        // pnl_2DMap.Width = data_2D.Length;
                        // pnl_2DMap.Height = data_2D[0].Length;

                        pnl_2DMap.Size = new Size(pnl_2DMap.Width, pnl_2DMap.Height);

                        // Identify the picture frame with my new Bitmap.
                        if (pnl_2DMap.BackgroundImage == null)
                        {
                            pnl_2DMap.BackgroundImage = new Bitmap(pnl_2DMap.Width, pnl_2DMap.Height);
                            bitmap = new Bitmap(pnl_2DMap.Width, pnl_2DMap.Height);
                        }

                        // Spit out the data to screen
                        DrawBitmap(data_2D, current2DPlotMaxIntensity);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        MessageBox.Show("Graph2DPlot:  " + ex.InnerException + "\n" + ex);
                    }
                    else
                    {
                        MessageBox.Show("Graph2DPlot:  " + ex);
                    }
                    Console.WriteLine(ex.ToString());
                    needToUpdate2DPlot = true;
                }
            }

            if (!is2DPlotFullScreen)
            {
                elementHost_PlotAreaFormatting.Invalidate();
            }

            disableMouseControls = false;
        }

        #region Drawing

        // Create an image out of the data array
        private unsafe void DrawBitmap(IReadOnlyList<IReadOnlyList<int>> newData2D, int newMaxIntensity)
        {
            if (currentlyReadingData)
            {
                return;
            }

            int perPixelX; // current_valuesPerPixelX;
            if (newData2D.Count > pnl_2DMap.Width)
                perPixelX = 1;
            else
                perPixelX = -(pnl_2DMap.Width / newData2D.Count);

            int perPixelY; // current_valuesPerPixelY;
            if (currentValuesPerPixelY >= 0)
                perPixelY = 1;
            else
                perPixelY = currentValuesPerPixelY;

            var bitmapData = LockBitmap(out var pixelWidth, out var tempBitmap);
            var pBase = (byte*) bitmapData.Scan0.ToPointer();

            var thresholdValue = plotAreaFormattingVm.ThresholdSliderValue;

            var threshold = Convert.ToInt32(thresholdValue) - 1;
            var divisorRange = (float)(newMaxIntensity - threshold);
            if (divisorRange <= 0)
                divisorRange = newMaxIntensity; // clears out everything anyway...
            //wfd
            //perPixelY = 1;
            // Start drawing
            try
            {
                // MessageBox.Show("data2d: " + new_data2D[0].Length.ToString());
                var yMax = newData2D[0].Count;

                for (var y = 0; (y < yMax); y++)
                {
                    // problem with flashing colors.  This fixes it.  Got to figure out how it happened
                    //if ((((yMax - y) * -perPixelY) - 1) > pnl_2DMap.Height)
                    //    continue;

                    // Important to ensure each scan line begins at a pixel, not halfway into a pixel, e.g.
                    var pPixel = (perPixelY > 0) ? PixelAt(pBase, pixelWidth, 0, yMax - y - 1) : PixelAt(pBase, pixelWidth, 0, ((yMax - y) * -perPixelY) - 1);
                    var posX = 0;
                    for (var x = 0; (x < newData2D.Count) && (posX - perPixelX < pnl_2DMap.Width); x++)
                    {
                        try
                        {
                            if (newData2D[x][y] > threshold)
                            {
                                try
                                {
                                    var color = plotAreaFormattingVm.ColorMap.GetColorForIntensity(((newData2D[x][y] - threshold)) / divisorRange);
                                    pPixel->Red = color.R;
                                    pPixel->Green = color.G;
                                    pPixel->Blue = color.B;
                                }
                                catch (Exception)
                                {
                                    //MessageBox.Show(ex.ToString());
                                    BackColor = Color.Red;
                                    Update();
                                    // MessageBox.Show(pos_X.ToString()+", "+y.ToString()+"  "+ex.ToString());
                                }
                            }
                            else
                            {
                                try
                                {
                                    // this will make the background white - doesn't work if the continue; statement is below
                                    pPixel->Red = pPixel->Green = pPixel->Blue = (byte)plotAreaFormattingVm.BackgroundGrayValue;
                                }
                                catch (Exception)
                                {
                                    BackColor = Color.Blue;
                                    Update();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("ERROR: " + (pPixel == null ? "null" : "not null") + "\nX=" + x + ", y=" + y + "\n" + ex.StackTrace + "\n\n" + ex);
                        }

                        var copyPixel = pPixel;
                        pPixel++;
                        posX += -perPixelX;
                        for (var i = 1; (i < -perPixelX) && (posX < pnl_2DMap.Width); i++)
                        {
                            try
                            {
                                pPixel->Blue = copyPixel->Blue;
                                pPixel->Green = copyPixel->Green;
                                pPixel->Red = copyPixel->Red;
                                pPixel++;
                            }
                            catch (Exception)
                            {
                                // something
                            }
                        }
                    }

                    try
                    {
                        // this section thickens the squares vertically
                        // Copy the scan line if we have to do many pixels per value
                        for (var i = 1; i < -perPixelY; i++)
                        {
                            if ((yMax - y) * -perPixelY - i < pnl_2DMap.Height)
                            {
                                PixelData* copyPixel;
                                try
                                {
                                    copyPixel = PixelAt(pBase, pixelWidth, 0, (yMax - y) * -perPixelY - 1);
                                    pPixel = PixelAt(pBase, pixelWidth, 0, (yMax - y) * -perPixelY - 1 - i);
                                }
                                catch (Exception)
                                {
                                    MessageBox.Show("arg!:  PixelAt problem");
                                    return;
                                }

                                var verticalThickness = newData2D.Count * Math.Abs(perPixelX);
                                for (int x = 0; x < verticalThickness; x++)
                                {
                                    pPixel->Blue = copyPixel->Blue;
                                    pPixel->Green = copyPixel->Green;
                                    pPixel->Red = copyPixel->Red;
                                    pPixel++;
                                    copyPixel++;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //MessageBox.Show("ERROR 2: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DrawBitmap: " + ex);
                // wfd this is a cheat!!!!  Must fix.  Problem with zooming!
                UnlockBitmap(bitmapData, tempBitmap);

                // imf_ReadFrame(new_frame_index, out frame_Data);
                needToUpdate2DPlot = true;

                BackColor = Color.Yellow;

                needToUpdate2DPlot = true;
                return;
            }
            BackColor = Color.Silver;
            //slider_ColorMap.set_MaxIntensity(new_maxIntensity); TODO: Did nothing, but if ColorMapSlider is changed to scale by intensity, that would get set here.

            //Width = pnl_2DMap.Left + pnl_2DMap.Width + 170;

            UnlockBitmap(bitmapData, tempBitmap);
        }

        private unsafe PixelData* PixelAt(byte* pBase, int pixelWidth, int x, int y)
        {
            return (PixelData*)(pBase + (y * pixelWidth) + (x * sizeof(PixelData)));
        }

        public struct PixelData
        {
            public byte Blue;
            public byte Green;
            public byte Red;
        }

        private unsafe BitmapData LockBitmap(out int pixelWidth, out Bitmap tempBitmap)
        {

            Rectangle bounds = new Rectangle(0, 0, pnl_2DMap.Width, pnl_2DMap.Height);
            // MessageBox.Show("plot_Width: " + plot_Width.ToString());

            // Figure out the number of bytes in a row
            // This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length.
            pixelWidth = pnl_2DMap.Width * sizeof(PixelData);
            if (pixelWidth % 4 != 0)
            {
                pixelWidth = 4 * (pixelWidth / 4 + 1);
            }
            //MessageBox.Show("pixelWidth: " + pixelWidth.ToString());

            tempBitmap = new Bitmap(pnl_2DMap.Width, pnl_2DMap.Height);
            return tempBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        }

        private void UnlockBitmap(BitmapData bitmapData, Bitmap tempBitmap)
        {
            try
            {
                tempBitmap.UnlockBits(bitmapData);

                bitmap = tempBitmap;
            }
            catch (Exception)
            {
                BackColor = Color.AliceBlue;
                //  MessageBox.Show("TRAPPED:  unlocking bitmap, destroying and retrying!");
                // this is caused from zooming, changing the max and min values of the axis, etc
                // multiple areas attempting to access the plot.
                needToUpdate2DPlot = true;
            }

            pnl_2DMap.BackgroundImage = bitmap;
            // pnl_2DMap.Refresh();
        }

        private static void DrawRectangle(Graphics g, Point p1, Point p2)
        {
            if (p1 == p2)
                return;
            var p = new Pen(Color.LemonChiffon, 1.0f);
            var pts = new Point[5];
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
                Invoke(new MethodInvoker(() =>
                {
                    progress_ReadingFile = new ProgressBar
                    {
                        BackColor = Color.SlateGray,
                        ForeColor = Color.DeepSkyBlue,
                        Location = new Point(244, 728),
                        Name = "progress_ReadingFile",
                        Size = new Size(512, 12),
                        Style = ProgressBarStyle.Continuous,
                        TabIndex = 55,
                        Value = 11,
                        Visible = false
                    };

                    //
                    // progress_ReadingFile
                    //
                    tab_DataViewer.Controls.Add(progress_ReadingFile);

                    progress_ReadingFile.Top = pnl_2DMap.Top + pnl_2DMap.Height / 2;
                    progress_ReadingFile.Left = pnl_2DMap.Left;
                    progress_ReadingFile.Width = pnl_2DMap.Width;
                    progress_ReadingFile.Maximum = (uimfReader.UimfGlobalParams.NumFrames / chromatogramControlVm.FrameCompression) + 1;
                    progress_ReadingFile.Show();

                    progress_ReadingFile.BringToFront();
                }));
            }
            catch (Exception)
            {
                // Something...
            }
        }

        /* ***************************************************************
         * The Axis plots
         */
        private double[] mobilityPlotData;
        private void SetMobilityPlotData(double[] mobilityData)
        {
            if (viewerIsClosing || (mobilityData == null) || (mobilityData.Length < 5))
            {
                return;
            }

            try
            {
                mobilityPlotData = new double[mobilityData.Length];
                mobilityData.CopyTo(mobilityPlotData, 0);
                Invoke(new MethodInvoker(() =>
                {
                    //plot_Mobility.ClearRange();

                    try
                    {
                        plot_Mobility.HitSize = (currentValuesPerPixelX >= 1) ? new SizeF(1.0f, 2 * MobilityPlotHeight) : new SizeF(-currentValuesPerPixelX, 2 * plot_Mobility.Height);

                        //	plot_Mobility.Width = pnl_2DMap.Width + DRIFT_PLOT_WIDTH_DIFF;

                        if (currentValuesPerPixelX < -5)
                        {
                            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                            {
                                waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, Color.Salmon);
                            }
                            else
                            {
                                waveform_MobilityPlot.Symbol = new Symbol(SymbolType.Circle, Color.Salmon);
                                waveform_MobilityPlot.Symbol.Fill.Color = Color.Transparent;
                            }
                        }
                        else
                        {
                            waveform_MobilityPlot.Symbol = new Symbol(SymbolType.None, Color.Salmon);
                        }



                        const int mobilityPlotMaxHorizontalLocation = 12;
                        plot_Mobility.XMax = pnl_2DMap.Width + mobilityPlotMaxHorizontalLocation;
                        double minX;
                        double maxX;
                        var xCompressionMultiplier = currentValuesPerPixelX > 1 ? currentValuesPerPixelX : 1;

                        if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                        {
                            if (chromatogramMinFrame < 1)
                            {
                                chromatogramMaxFrame -= chromatogramMinFrame;
                                chromatogramMinFrame = 1;
                            }

                            applyingMobilityRangeChange = true;
#if !NEEDS_WORK
                            chromatogramMaxFrame = uimfReader.LoadFrame(frameControlVm.MaximumFrameNumber);
                            num_maxMobility.Value = num_maxMobility.Maximum = chromatogramMaxFrame;
#else // needs work
                            if (minFrame_Chromatogram < 0)
                                minFrame_Chromatogram = 0;

                            num_minMobility.Value = Convert.ToDecimal(minFrame_Chromatogram);
                            if (num_minMobility.Value + pnl_2DMap.Width > num_FrameSelect.Maximum)
                            {
                                minFrame_Chromatogram = Convert.ToInt32(num_FrameSelect.Maximum) - pnl_2DMap.Width;
                                if (minFrame_Chromatogram < 0)
                                    minFrame_Chromatogram = 0;
                                num_minMobility.Value = Convert.ToDecimal(minFrame_Chromatogram);
                            }

                            //MessageBox.Show("hsb: " + hsb_2DMap.Value.ToString() + " " + minFrame_Chromatogram.ToString());

                            maxFrame_Chromatogram = Convert.ToInt32(num_FrameSelect.Maximum);

                            num_minMobility.Maximum = num_maxMobility.Maximum = num_FrameSelect.Maximum;

                            //MessageBox.Show(num_FrameSelect.Maximum.ToString() + "  " + minFrame_Chromatogram.ToString()+"  "+(minFrame_Chromatogram + tic_Mobility.Length).ToString());
                            //       num_maxMobility.Value = Convert.ToDecimal(minFrame_Chromatogram + tic_Mobility.Length);
                            //    else

                            if (chromatogram_valuesPerPixelX < 0)
                                num_maxMobility.Value = num_minMobility.Value + (pnl_2DMap.Width / -chromatogram_valuesPerPixelX); // Convert.ToDecimal(tic_Mobility.Length);
                            else
                                num_maxMobility.Value = num_minMobility.Value + pnl_2DMap.Width; // Convert.ToDecimal(tic_Mobility.Length);
#endif
                            applyingMobilityRangeChange = false;

                            //MessageBox.Show("OK");

                            // MessageBox.Show(uimf_FrameParameters.Accumulations.ToString());
                            if (averageDriftScanDuration.Equals(0) || showMobilityChromatogramFrameNumber)
                            {
                                //plot_Mobility.PlotY(tic_Mobility, (double)0, 1.0 * Convert.ToDouble(chromatogramControlVm.FrameCompression));
                                waveform_MobilityPlot.Points = new BasicArrayPointList(Enumerable.Range(0, mobilityPlotData.Length).Select(x => x * Convert.ToDouble(chromatogramControlVm.FrameCompression) * xCompressionMultiplier).ToArray(), mobilityPlotData);

                                //xAxis_Mobility.Caption = "Frame Number";
                                plot_Mobility.GraphPane.XAxis.Title.Text = "Frame Number";

                                minX = 0;
                                //maxX = (tic_Mobility.Length - 1) * Convert.ToDouble(chromatogramControlVm.FrameCompression) * xCompressionMultiplier;
                                maxX = waveform_MobilityPlot.Points[waveform_MobilityPlot.Points.Count - 1].X;
                            }
                            else
                            {
                                var incrementMobilityValue = averageDriftScanDuration * (frameMaximumMobility + 1) * uimfReader.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations) / 1000000.0 / 1000.0;
                                //plot_Mobility.PlotY(tic_Mobility, (double)minFrame_Chromatogram * increment_MobilityValue, increment_MobilityValue);
                                waveform_MobilityPlot.Points = new BasicArrayPointList(Enumerable.Range(0, mobilityPlotData.Length).Select(x => x * incrementMobilityValue * xCompressionMultiplier + chromatogramMinFrame * incrementMobilityValue).ToArray(), mobilityPlotData);

                                //xAxis_Mobility.Caption = "Frames - Time (sec)";
                                plot_Mobility.GraphPane.XAxis.Title.Text = "Frames - Time (sec)";

                                minX = chromatogramMinFrame * incrementMobilityValue;
                                //maxX = (tic_Mobility.Length - 1) * increment_MobilityValue * xCompressionMultiplier + minFrame_Chromatogram * increment_MobilityValue;
                                maxX = waveform_MobilityPlot.Points[waveform_MobilityPlot.Points.Count - 1].X;
                            }
                        }
                        else
                        {
                            if (currentMinMobility < 0)
                            {
                                currentMaxMobility -= currentMinMobility;
                                currentMinMobility = 0;
                            }

                            if (showMobilityScanNumber)
                            {
                                // these values are used to prevent the values from changing during the plotting... yikes!
                                var minMobilityValue = currentMinMobility;
                                var incrementMobilityValue = 1.0;
                                plot_Mobility.GraphPane.XAxis.Scale.Format = "F0";
                                //plot_Mobility.PlotY(tic_Mobility, 0, current_maxMobility - current_minMobility + 1, min_MobilityValue, increment_MobilityValue);
                                waveform_MobilityPlot.Points = new BasicArrayPointList(Enumerable.Range(0, mobilityPlotData.Length).Select(x => x * incrementMobilityValue * xCompressionMultiplier + minMobilityValue).ToArray(),
                                    mobilityPlotData.Take(currentMaxMobility - currentMinMobility + 1).ToArray());

                                minX = minMobilityValue;
                                //maxX = (tic_Mobility.Length - 1) * increment_MobilityValue * xCompressionMultiplier + min_MobilityValue;
                                maxX = waveform_MobilityPlot.Points[waveform_MobilityPlot.Points.Count - 1].X;
                            }
                            else
                            {
                                // these values are used to prevent the values from changing during the plotting... yikes!
                                var minMobilityValue = currentMinMobility * averageDriftScanDuration / 1000000.0;
                                var incrementMobilityValue = averageDriftScanDuration / 1000000.0;
                                plot_Mobility.GraphPane.XAxis.Scale.Format = "F2";
                                //plot_Mobility.PlotY(tic_Mobility, min_MobilityValue, increment_MobilityValue);
                                waveform_MobilityPlot.Points = new BasicArrayPointList(Enumerable.Range(0, mobilityPlotData.Length).Select(x => x * incrementMobilityValue * xCompressionMultiplier + minMobilityValue).ToArray(), mobilityPlotData);

                                minX = minMobilityValue;
                                //maxX = (tic_Mobility.Length - 1) * increment_MobilityValue * xCompressionMultiplier + min_MobilityValue;
                                maxX = waveform_MobilityPlot.Points[waveform_MobilityPlot.Points.Count - 1].X;
                            }

                            // set min and max here, they will not adjust to zooming
                            applyingMobilityRangeChange = true; // prevent events form occurring.
                            num_minMobility.Value = Convert.ToDecimal(currentMinMobility);

                            hsb_2DMap.Maximum = frameMaximumMobility - (currentMaxMobility - currentMinMobility);
                            vsb_2DMap.Maximum = frameMaximumTofBins - (currentMaxTofBin - currentMinTofBin);
                            hsb_2DMap.Minimum = 0;
                            vsb_2DMap.Minimum = 0;

                            hsb_2DMap.Value = currentMinMobility;
                            if (vsb_2DMap.Maximum > currentMinTofBin)
                                vsb_2DMap.Value = vsb_2DMap.Maximum - currentMinTofBin;
                            else
                                vsb_2DMap.Value = 0;

                            hsb_2DMap.SmallChange = 30; // (current_maxMobility - current_minMobility) / 5;
                            hsb_2DMap.LargeChange = 60; // (current_maxMobility - current_minMobility) * 4 / 5;
                            vsb_2DMap.SmallChange = (currentMaxTofBin - currentMinTofBin) / 5;
                            vsb_2DMap.LargeChange = (currentMaxTofBin - currentMinTofBin) * 4 / 5;

                            num_minMobility.Maximum = num_maxMobility.Maximum = frameMaximumMobility;
                            if (currentMaxMobility > frameMaximumMobility)
                                currentMaxMobility = frameMaximumMobility;
                            num_maxMobility.Value = Convert.ToDecimal(currentMaxMobility);
                            num_minMobility.Increment = num_maxMobility.Increment = Convert.ToDecimal((currentMaxMobility - currentMinMobility) / 3);
                        }

                        plot_Mobility.GraphPane.XAxis.Scale.Min = minX;// - 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                        plot_Mobility.GraphPane.XAxis.Scale.Max = maxX;// + 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                        plot_Mobility.GraphPane.AxisChange();
                        plot_Mobility.Refresh();
                        plot_Mobility.Update();
                        applyingMobilityRangeChange = false; // OK, clear this flag to make the controls usable
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Plot Axis Mobility: " + ex.StackTrace + "\n\n" + ex);
                        // plot_Mobility.PlotAreaColor = Color.Orange;
                        Thread.Sleep(100);
                        needToUpdate2DPlot = true;
                    }
                }));
            }
            catch (Exception ex)
            {
                needToUpdate2DPlot = true;
                MessageBox.Show("catch mobility" + ex);
            }
        }

        private double[] tofMzPlotData;
        private void SetTofMzPlotData(double[] tof)
        {
            if (viewerIsClosing || (tof == null) || (tof.Length < 5))
                return;

            try
            {
                tofMzPlotData = new double[tof.Length];
                tof.CopyTo(tofMzPlotData, 0);
                Invoke(new MethodInvoker(() =>
                {
                    try
                    {
                        // s_data = new double[tof.Length];
                        // Array.Copy(tof, s_data, tof.Length);
                        applyingTofBinRangeChange = true;
                        double minY;
                        double maxY;

                        if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                        {
                            if (chromatogramMinMobility < 0)
                                chromatogramMinMobility = 0;
                            num_minBin.Value = Convert.ToDecimal(chromatogramMinMobility);

                            if (chromatogramMaxMobility > uimfReader.UimfFrameParams.Scans - 1)
                                chromatogramMaxMobility = uimfReader.UimfFrameParams.Scans - 1;
                            num_maxBin.Value = Convert.ToDecimal(chromatogramMaxMobility);

                            if (showMobilityScanNumber)
                            {
                                //plot_TOF.PlotX(mobilityPlotData, minMobility_Chromatogram, 1.0);
                                waveform_TOFPlot.Points = new BasicArrayPointList(tofMzPlotData,
                                    Enumerable.Range(chromatogramMinMobility, tofMzPlotData.Length).Select(x => (double) x).ToArray());

                                minY = chromatogramMinMobility;
                                maxY = (tofMzPlotData.Length - 1) + chromatogramMinMobility;
                            }
                            else
                            {
                                //plot_TOF.PlotX(mobilityPlotData, minMobility_Chromatogram, uimfReader.UIMF_FrameParameters.AverageTOFLength / 1000000.0);
                                waveform_TOFPlot.Points = new BasicArrayPointList(tofMzPlotData,
                                    Enumerable.Range(0, tofMzPlotData.Length).Select(x =>
                                        uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000.0 * x +
                                        chromatogramMinMobility).ToArray());

                                minY = chromatogramMinMobility;
                                maxY = uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000.0 *
                                       (tofMzPlotData.Length - 1) + chromatogramMinMobility;
                            }
                        }
                        else
                        {
                            if (displayTofValues)
                            {
                                var minTof = (currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                                var maxTof = (currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                                var incrementTof = (maxTof - minTof) / pnl_2DMap.Height;
                                if (currentValuesPerPixelY < 0)
                                    incrementTof *= -currentValuesPerPixelY;

                                num_maxBin.Value = Convert.ToDecimal(maxTof);
                                num_minBin.Value = Convert.ToDecimal(minTof);
                                num_minBin.Increment = num_maxBin.Increment = Convert.ToDecimal((maxTof - minTof) / 3);

                                var minBinValue = minTof;
                                var incrementBinValue = incrementTof;

                                //plot_TOF.Update();
                                //plot_TOF.Enabled = false;
                                //plot_TOF.PlotX(mobilityPlotData, min_BinValue, increment_BinValue); //wfd
                                waveform_TOFPlot.Points = new BasicArrayPointList(tofMzPlotData,
                                    Enumerable.Range(0, tofMzPlotData.Length).Select(x => incrementBinValue * x + minBinValue).ToArray());

                                minY = minBinValue;
                                maxY = incrementBinValue * (tofMzPlotData.Length - 1) + minBinValue;
                            }
                            else
                            {
                                // Confirmed working... 061213
                                // Much more difficult to find where the mz <-> TOF index correlation
                                var mzMin = uimfReader.MzCalibration.TOFtoMZ(currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
                                var mzMax = uimfReader.MzCalibration.TOFtoMZ(currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin);

                                var incrementTof = (mzMax - mzMin) / pnl_2DMap.Height;
                                if (currentValuesPerPixelY < 0)
                                    incrementTof *= -currentValuesPerPixelY;

                                num_maxBin.Value = Convert.ToDecimal(mzMax);
                                num_minBin.Value = Convert.ToDecimal(mzMin);

                                var minBinValue = mzMin;
                                var incrementBinValue = incrementTof;

                                //plot_TOF.Update();
                                //plot_TOF.Enabled = false;
                                //plot_TOF.PlotX(mobilityPlotData, min_BinValue, increment_BinValue); //wfd
                                waveform_TOFPlot.Points = new BasicArrayPointList(tofMzPlotData,
                                    Enumerable.Range(0, tofMzPlotData.Length).Select(x => incrementBinValue * x + minBinValue).ToArray());

                                minY = minBinValue;
                                maxY = incrementBinValue * (tofMzPlotData.Length - 1) + minBinValue;
                            }
                        }

                        plot_TOF.GraphPane.YAxis.Scale.Min =
                            minY; // - 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                        plot_TOF.GraphPane.YAxis.Scale.Max =
                            maxY; // + 0.5; // Adding/subtracting 0.5 to keep outer positions the same messes up other computations.
                        plot_TOF.GraphPane.AxisChange();
                        plot_TOF.Refresh();
                        //plot_TOF.Enabled = true;
                        applyingTofBinRangeChange = false;
                    }
                    catch (Exception)
                    {
                        plot_TOF.BackColor = Color.OrangeRed;
                        Thread.Sleep(100);
                        needToUpdate2DPlot = true;
                    }
                }));
            }
            catch (Exception)
            {
                BackColor = Color.Pink;
                needToUpdate2DPlot = true;
            }
        }

        #endregion
    }
}
