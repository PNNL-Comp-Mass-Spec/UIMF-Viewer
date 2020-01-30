using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UIMFLibrary;
using UIMFViewer.ChromatogramControl;
using UIMFViewer.FrameControl;
using UIMFViewer.FrameInfo;
using UIMFViewer.Utilities;
using ZedGraph;

namespace UIMFViewer
{
    public partial class DataViewer
    {
        private void MainTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabpages_Main.SelectedTab == tab_PostProcessing)
            {
                tabpages_Main.Width = Width;
            }
        }

        private void MainTabsDrawItem(object sender, DrawItemEventArgs e)
        {
            Brush bshFore;
            Brush bshBack;
            var tabFont = new Font("Comic Sans MS", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);

            if (e.Index != tabpages_Main.SelectedIndex)
            {
                bshFore = Brushes.Ivory;
                bshBack = new LinearGradientBrush(e.Bounds, Color.RoyalBlue, Color.DimGray, LinearGradientMode.BackwardDiagonal);
            }
            else
            {
                bshFore = Brushes.Black;
                bshBack = new LinearGradientBrush(e.Bounds, Color.White, Color.WhiteSmoke, LinearGradientMode.BackwardDiagonal);
            }

            e.Graphics.FillRectangle(bshBack, e.Bounds);

            var tabName = tabpages_Main.TabPages[e.Index].Text;
            var s = e.Graphics.MeasureString(tabName, tabFont);

            e.Graphics.RotateTransform(270.0f);
            e.Graphics.TranslateTransform(-s.Width, 0);
            e.Graphics.DrawString(tabName, tabFont, bshFore, -e.Bounds.Top - 28, e.Bounds.Left + 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // moving the application on and off the screen causes the
            // pb_2DGraph to rewrite - while the other paint event locks the bits.
            // ignore the system paint.
        }

        private void Zoom(Point p1, Point p2)
        {
            lock (plot2DChangeLock)
            {
                selectingMobilityRange = false;
                plot_Mobility.ClearRange();

                // Prep variables
                var minPtX = Math.Min(p1.X, p2.X);
                var maxPtX = Math.Max(p1.X, p2.X);
                var minPtY = pnl_2DMap.Height - Math.Max(p1.Y, p2.Y);
                var maxPtY = pnl_2DMap.Height - Math.Min(p1.Y, p2.Y);

                // don't zoom if the user mistakenly presses the mouse button
                if ((maxPtX - minPtX < -currentValuesPerPixelX) && (maxPtY - minPtY < -currentValuesPerPixelY))
                    return;

                // Calculate the data enclosing boundaries
                // Need to do new_maxMobility first since new_minMobility changes beforehand
                if (currentValuesPerPixelX <= 1)
                {
                    newMaxMobility = (int) (newMinMobility + ((double)maxPtX / -currentValuesPerPixelX));
                    newMinMobility = (int) (newMinMobility + ((double)minPtX / -currentValuesPerPixelX));
                }
                else
                {
                    newMaxMobility = (int) (newMinMobility + (double)maxPtX * currentValuesPerPixelX);
                    newMinMobility = (int) (newMinMobility + (double)minPtX * currentValuesPerPixelX);
                }


                if (newMaxMobility - newMinMobility < MinGraphedMobility)
                {
                    newMinMobility -= (MinGraphedMobility - (newMaxMobility - newMinMobility)) / 2;
                    newMaxMobility = newMinMobility + MinGraphedMobility;
                }

                // MessageBox.Show(new_maxMobility.ToString()+", "+new_minMobility.ToString());
                if ((minPtY != 0) || (maxPtY != pnl_2DMap.Height))
                {
                    if (currentValuesPerPixelY < 0)
                    {
                        newMaxTofBin = (int)uimfReader.GetBinForPixel(maxPtY / -currentValuesPerPixelY);
                        newMinTofBin = (int)uimfReader.GetBinForPixel(minPtY / -currentValuesPerPixelY);
                    }
                    else
                    {
                        newMaxTofBin = (int)uimfReader.GetBinForPixel(maxPtY);
                        newMinTofBin = (int)uimfReader.GetBinForPixel(minPtY);
                    }
                }

                if (newMaxMobility - newMinMobility < MinGraphedMobility)
                {
                    newMaxMobility = ((newMaxMobility + newMinMobility) / 2) + (MinGraphedMobility / 2);
                    newMinMobility = ((newMaxMobility + newMinMobility) / 2) - (MinGraphedMobility / 2);
                }

                if (newMinMobility < 0)
                    newMinMobility = 0;
                if (newMaxMobility > frameMaximumMobility)
                    newMaxMobility = frameMaximumMobility;

                if (newMinTofBin < 0)
                    newMinTofBin = 0;
                if (newMaxTofBin > frameMaximumTofBins)
                    newMaxTofBin = frameMaximumTofBins;

                // save new zoom...
                SaveZoom(newMinMobility, newMaxMobility, newMinTofBin, newMaxTofBin);

                currentMaxTofBin = newMaxTofBin;
                currentMinTofBin = newMinTofBin;

                needToUpdate2DPlot = true;
            }
        }

        private void SaveZoom(int minMobility, int maxMobility, int minBin, int maxBin)
        {
            var newZoom = new ZoomInfo(minMobility, maxMobility, minBin, maxBin);

            if (zoomHistory.Count > 0 && newZoom.Equals(zoomHistory[zoomHistory.Count - 1]))
            {
                return;
            }

            zoomHistory.Add(newZoom);
        }

        #region 2DMap Events

        //wfd
        private void Plot2DMouseDown(object sender, MouseEventArgs e)
        {
            if ((disableMouseControls) || // if plotting the plot, prevent zooming!
                (playingCinemaPlot))
                return;

            // Graphics g = pnl_2DMap.CreateGraphics();
            // g.DrawString(e.X.ToString(), new Font(FontFamily.GenericSerif, 10, FontStyle.Regular), new SolidBrush(Color.Yellow), 10, 50);

            if (menuItem_SelectionCorners.Checked && (e.Button == MouseButtons.Middle))
                MessageBox.Show("Mouse at " + e.X.ToString() + ", " + e.Y.ToString() + (InsidePolygonPixel(e.X, pnl_2DMap.Height - e.Y) ? " is inside" : " is outside"));

            // Starting a zoom process
            if (e.Button == MouseButtons.Left)
            {
                if ((e.X > pnl_2DMap.Width - 17) && (e.Y < 17))
                {
                    is2DPlotFullScreen = !is2DPlotFullScreen;
                    if (is2DPlotFullScreen)
                    {
                        max2DPlotHeight = tab_DataViewer.ClientSize.Height - 400;
                        max2DPlotWidth = tab_DataViewer.ClientSize.Width - 100;
                    }
                    else
                    {
                        max2DPlotWidth = tab_DataViewer.ClientSize.Width;
                        max2DPlotHeight = tab_DataViewer.ClientSize.Height;
                    }

                    viewerNeedsResizing = true;
                    needToUpdate2DPlot = true;
                }

                mouseDragging = true;

                Cursor = Cursors.Cross;
                mouseDownPoint = new Point(e.X, e.Y);
                mouseMovePoint = new Point(e.X, e.Y);
            }

            // Pop-up Menu
            if (e.Button == MouseButtons.Right)
                contextMenu_pb_2DMap.Show(this, new Point(e.X + pnl_2DMap.Left, e.Y + pnl_2DMap.Top));

            for (var i = 0; i < 4; i++)
            {
                if ((Math.Abs(e.X - plot2DSelectionCorners[i].X) <= 6) && (Math.Abs(e.Y - plot2DSelectionCorners[i].Y) <= 6))
                {
                    isMovingSelectionCorners = i;
                    return;
                }
            }

            // this section draws the intensities on the different pixels if they are big
            // enough.  Lot of waste; but it only occurs when the mouse is pressed down.
            //
            // wfd:  this will do for now.  I am sure there is a much more efficient method of
            // handling this. For now, the race is on.
            if ((currentValuesPerPixelY < -10) && (currentValuesPerPixelX < -20))
            {
                pnl_2DMap_Extensions = pnl_2DMap.CreateGraphics();
                var mapFont = new Font("Verdana", 7);
                var foreBrush = new SolidBrush(Color.White);
                var backBrush = new SolidBrush(Color.DimGray);
                for (var i = 0; i <= data_2D.Length - 1; i++)
                    for (var j = 0; j <= data_2D[0].Length - 1; j++)
                    {
                        if (data_2D[i][j] != 0)
                        {
                            pnl_2DMap_Extensions.DrawString(data_2D[i][j].ToString("#"), mapFont, backBrush, (i * -currentValuesPerPixelX) + 1, pnl_2DMap.Height - ((j + 1) * -currentValuesPerPixelY) - 1);
                            pnl_2DMap_Extensions.DrawString(data_2D[i][j].ToString("#"), mapFont, foreBrush, (i * -currentValuesPerPixelX), pnl_2DMap.Height - ((j + 1) * -currentValuesPerPixelY));
                        }
                    }
            }
        }

        private int prevCursorX;
        private int prevCursorY;
        private void Plot2DMouseMove(object sender, MouseEventArgs e)
        {
            if (disableMouseControls) // if plotting the plot, prevent zooming!
                return;

            if ((Math.Abs(prevCursorX - e.X) > 3) || (Math.Abs(prevCursorY - e.Y) > 3))
            {
                prevCursorX = e.X;
                prevCursorY = e.Y;
                UpdateCursorReading(e);
            }
            else
                return;

            if (isMovingSelectionCorners >= 0)
            {
                if (e.X < 0)
                    plot2DSelectionCorners[isMovingSelectionCorners].X = 0;
                else if (e.X > pnl_2DMap.Width)
                    plot2DSelectionCorners[isMovingSelectionCorners].X = pnl_2DMap.Width;
                else
                    plot2DSelectionCorners[isMovingSelectionCorners].X = e.X;

                if (e.Y < 0)
                    plot2DSelectionCorners[isMovingSelectionCorners].Y = 0;
                else if (e.Y > pnl_2DMap.Height)
                    plot2DSelectionCorners[isMovingSelectionCorners].Y = pnl_2DMap.Height;
                else
                    plot2DSelectionCorners[isMovingSelectionCorners].Y = e.Y;

                pnl_2DMap.Invalidate();
                return;
            }

            // Draw a rectangle along with the dragging mouse.
            if (mouseDragging) //&& !toolBar1.Buttons[0].Pushed)
            {
                // Ensure that the mouse point does not overstep its bounds
                var x = Math.Min(e.X, pnl_2DMap.Width - 1);
                x = Math.Max(x, 0);
                var y = Math.Min(e.Y, pnl_2DMap.Height - 1);
                y = Math.Max(y, 0);

                mouseMovePoint = new Point(x, y);

                pnl_2DMap.Invalidate();
            }
        }

        private void Plot2DMouseUp(object sender, MouseEventArgs e)
        {
            if (disableMouseControls)  // if plotting the plot, prevent zooming!
                return;

            if (isMovingSelectionCorners >= 0)
            {
                ConvexPolygon();

                // ensure there are no negative angles.
                isMovingSelectionCorners = -1; // no more moving corner

                needToUpdate2DPlot = true;
            }

            if (pnl_2DMap_Extensions != null)
            {
                pnl_2DMap.Refresh();
                pnl_2DMap_Extensions = null;
            }

            Cursor = Cursors.Default;

            // Zoom the image in...
            if (e.Button == MouseButtons.Left)
            {
                // most likely a double click
                if ((Math.Abs(mouseDownPoint.X - mouseMovePoint.X) < 3) &&
                    (Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y) < 3))
                {
                    mouseMovePoint = mouseDownPoint;
                    mouseDragging = false;
                    return;
                }

                if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                {
                    chromatogramMinFrame = 0;
                    chromatogramMaxFrame = uimfReader.SetCurrentFrameType(currentFrameType) - 1;

                    // select the range of frames
                    int minFrameDataNumber;
                    int maxFrameDataNumber;
                    if (chromatogramValuesPerPixelX < 0)
                    {
                        if (mouseDownPoint.X > mouseMovePoint.X)
                        {
                            minFrameDataNumber = chromatogramMinFrame + (mouseMovePoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            maxFrameDataNumber = chromatogramMinFrame + (mouseDownPoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);

                            //minFrameDataNumber = minFrame_Chromatogram + (_mouseMovePoint.X / -chromatogram_valuesPerPixelX) + 1;
                            //maxFrameDataNumber = minFrame_Chromatogram + (_mouseDownPoint.X / -chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                            minFrameDataNumber = chromatogramMinFrame + (mouseDownPoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            maxFrameDataNumber = chromatogramMinFrame + (mouseMovePoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            //minFrameDataNumber = minFrame_Chromatogram + (_mouseDownPoint.X / -chromatogram_valuesPerPixelX) + 1;
                            //maxFrameDataNumber = minFrame_Chromatogram + (_mouseMovePoint.X / -chromatogram_valuesPerPixelX) + 1;
                        }
                    }
                    else // we have compressed the chromatogram
                    {
                        if (mouseDownPoint.X > mouseMovePoint.X)
                        {
                            minFrameDataNumber = chromatogramMinFrame + (mouseMovePoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            maxFrameDataNumber = chromatogramMinFrame + (mouseDownPoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            //minFrameDataNumber = minFrame_Chromatogram + (_mouseMovePoint.X * chromatogram_valuesPerPixelX) + 1;
                            //maxFrameDataNumber = minFrame_Chromatogram + (_mouseDownPoint.X * chromatogram_valuesPerPixelX) + 1;
                        }
                        else
                        {
                            minFrameDataNumber = chromatogramMinFrame + (mouseDownPoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            maxFrameDataNumber = chromatogramMinFrame + (mouseMovePoint.X * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);
                            //minFrameDataNumber = minFrame_Chromatogram + (_mouseDownPoint.X * chromatogram_valuesPerPixelX) + 1;
                            //maxFrameDataNumber = minFrame_Chromatogram + (_mouseMovePoint.X * chromatogram_valuesPerPixelX) + 1;
                        }
                    }

                    if (minFrameDataNumber < 1)
                        minFrameDataNumber = 1;
                    if (maxFrameDataNumber > uimfReader.UimfGlobalParams.NumFrames)
                        maxFrameDataNumber = uimfReader.UimfGlobalParams.NumFrames;

                    frameControlView.Dispatcher.Invoke(() => {
                        frameControlVm.CurrentFrameNumber = maxFrameDataNumber;
                    });

                    plot_Mobility.StopAnnotating(false);

                    // select the mobility highlight
                    // select the range of frames
                    int minSelectMobility;
                    int maxSelectMobility;
                    if (currentValuesPerPixelY < 0)
                    {
                        if (mouseDownPoint.Y > mouseMovePoint.Y)
                        {
                            minSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseMovePoint.Y) / -chromatogramValuesPerPixelY);
                            maxSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseDownPoint.Y) / -chromatogramValuesPerPixelY);

                            mobilitySelectionMinimum = minSelectMobility;
                            mobilitySelectionMaximum = maxSelectMobility;
                        }
                        else
                        {
                            minSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseDownPoint.Y) / -chromatogramValuesPerPixelY);
                            maxSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseMovePoint.Y) / -chromatogramValuesPerPixelY);

                            mobilitySelectionMinimum = maxSelectMobility;
                            mobilitySelectionMaximum = minSelectMobility;
                        }
                    }
                    else
                    {
                        if (mouseDownPoint.Y > mouseMovePoint.Y)
                        {
                            minSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseMovePoint.Y) * chromatogramValuesPerPixelY);
                            maxSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseDownPoint.Y) * chromatogramValuesPerPixelY);

                            mobilitySelectionMinimum = minSelectMobility;
                            mobilitySelectionMaximum = maxSelectMobility;
                        }
                        else
                        {
                            minSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseDownPoint.Y) * chromatogramValuesPerPixelY);
                            maxSelectMobility = chromatogramMinMobility + ((pnl_2DMap.Height - mouseMovePoint.Y) * chromatogramValuesPerPixelY);

                            mobilitySelectionMinimum = maxSelectMobility;
                            mobilitySelectionMaximum = minSelectMobility;
                        }
                    }

                    selectingMobilityRange = true;
                    //plot_Mobility.SetRange(min_select_mobility, max_select_mobility);

                    chromatogramControlVm.PartialPeakChromatogramChecked = false;
                    chromatogramControlVm.CompletePeakChromatogramChecked = false;

                    newMinTofBin = 0;
                    newMinMobility = 0;
                    newMaxTofBin = frameMaximumTofBins;
                    newMaxMobility = frameMaximumMobility;

                    AutoScrollPosition = new Point(0, 0);
                    hsb_2DMap.Value = 0;
                    uimfReader.CurrentFrameIndex = frameControlVm.CurrentFrameNumber;

                    ChromatogramCheckedChanged();
                }
                else if (mouseDragging)
                {
                    Zoom(mouseMovePoint, mouseDownPoint);
                }
            }

            // This will erase the rectangle
            mouseMovePoint = mouseDownPoint;
            mouseDragging = false;
            needToUpdate2DPlot = true;
        }

        private bool isCurrentlyPainting;
        private void Plot2DPaint(object sender, PaintEventArgs e)
        {
            if (pnl_2DMap.BackgroundImage == null)
                return;

            if (isCurrentlyPainting)
                return;
            isCurrentlyPainting = true;

            // DrawImage seems to make the selection box more responsive.
            if (!chromatogramControlVm.CompletePeakChromatogramChecked && !chromatogramControlVm.PartialPeakChromatogramChecked)
                e.Graphics.DrawImage(pnl_2DMap.BackgroundImage, 0, 0);

            if (mouseDragging)
                DrawRectangle(e.Graphics, mouseDownPoint, mouseMovePoint);

            // this section draws the highlight on the plot.
            if (selectingMobilityRange)
            {
                int xl;
                int xWidth;
                if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                {
                    var w = pnl_2DMap.Width / uimfReader.UimfGlobalParams.NumFrames;
                    xl = (mobilitySelectionMinimum * w);

                    // if (current_valuesPerPixelX < 0)
                    //     xl += current_valuesPerPixelX;
                    xWidth = (mobilitySelectionMaximum - mobilitySelectionMinimum + 1) * w;
                }
                else
                {
                    int minMobility;
                    if (showMobilityScanNumber)
                        minMobility = newMinMobility;
                    else
                        minMobility = (int)(newMinMobility * (averageDriftScanDuration / 1000000));

                    var w = pnl_2DMap.Width / (currentMaxMobility - currentMinMobility + 1);
                    xl = ((mobilitySelectionMinimum - minMobility) * w);

                    // if (current_valuesPerPixelX < 0)
                    //     xl += current_valuesPerPixelX;
                    xWidth = (mobilitySelectionMaximum - mobilitySelectionMinimum + 1) * w;
                    //e.Graphics.DrawString("(" + selection_min_drift.ToString() + " - " + min_mobility.ToString() + ") * " + w.ToString() + " = " + xl.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 10);
                    //e.Graphics.DrawString((mean_TOFScanTime / 1000000).ToString() + " ... " + min_mobility.ToString() + "    (" + selection_max_drift.ToString() + " - " + selection_min_drift.ToString() + " + 1) * " + w.ToString() + " = " + xWidth.ToString() + " ... " + new_minMobility.ToString() + ", " + new_maxMobility.ToString(), new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), new SolidBrush(Color.White), 10, 50);
                }
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(145, 111, 111, 126)), xl, 0, xWidth, pnl_2DMap.Height);
            }

            if (!playingCinemaPlot)
            {
                if (is2DPlotFullScreen)
                    e.Graphics.DrawImage(pb_Shrink.BackgroundImage, pnl_2DMap.Width - 17, 2);
                else
                    e.Graphics.DrawImage(pb_Expand.BackgroundImage, pnl_2DMap.Width - 17, 2);
            }

            DrawSelectionCorners(e.Graphics);

            isCurrentlyPainting = false;
        }

        private void Plot2DDoubleClick(object sender, EventArgs e)
        {
            if (playingCinemaPlot)
            {
                StopCinema();
                return;
            }

            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                chromatogramControlVm.PartialPeakChromatogramChecked = false;
                chromatogramControlVm.CompletePeakChromatogramChecked = false;

                plot_Mobility.StopAnnotating(false);

                ChromatogramCheckedChanged();

                var frameNumber = chromatogramMinFrame + (prevCursorX * (chromatogramMaxFrame - chromatogramMinFrame) / pnl_2DMap.Width);

                if (frameNumber < 1)
                    frameNumber = 1;
                if (frameNumber > uimfReader.GetNumberOfFrames(currentFrameType))
                    frameNumber = uimfReader.GetNumberOfFrames(currentFrameType) - 1;

                frameControlView.Dispatcher.Invoke(() => frameControlVm.CurrentFrameNumber = frameNumber);

                uimfReader.CurrentFrameIndex = frameControlVm.CurrentFrameNumber;
                plot_Mobility.ClearRange();

                vsb_2DMap.Show();  // gets hidden with Chromatogram
                hsb_2DMap.Show();

                // imf_ReadFrame(new_frame_index, out frame_Data);
                max2DPlotWidth = uimfReader.UimfFrameParams.Scans;
                needToUpdate2DPlot = true;
            }
            else
            {
                // Reinitialize
                zoomHistory.Clear();

                newMinTofBin = 0;
                newMinMobility = 0;
                newMaxTofBin = frameMaximumTofBins;
                newMaxMobility = frameMaximumMobility;

                num_minMobility.Value = 0;
                num_maxMobility.Value = frameMaximumMobility;

                selectingMobilityRange = false;
                plot_Mobility.ClearRange();
                needToUpdate2DPlot = true;
                hsb_2DMap.Value = 0;

                AutoScrollPosition = new Point(0, 0);
                // ResizeThis();
            }
        }

        #endregion

        #region Polygon checking

        /* Taken from Robert Sedgewick, Algorithms in C++ */
        /*  returns whether, in traveling from the first to the second
    	to the third point, we turn counterclockwise (+1) or not (-1) */
        private static int GetTurnDirection(Point p0, Point p1, Point p2)
        {
            var dx1 = p1.X - p0.X;
            var dy1 = p1.Y - p0.Y;

            var dx2 = p2.X - p0.X;
            var dy2 = p2.Y - p0.Y;

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

        private bool InsidePolygon(int scan, int bin)
        {
            int xPixel;
            if (currentValuesPerPixelX == 1)
                xPixel = scan;
            else
                xPixel = (scan - currentMinMobility) * -currentValuesPerPixelX;

            int yPixel;
            if (currentValuesPerPixelY > 0)
                yPixel = ((bin - currentMinTofBin) / currentValuesPerPixelY);
            else
                yPixel = ((bin - currentMinTofBin) * -currentValuesPerPixelY);
            return InsidePolygonPixel(xPixel, yPixel);
        }

        private bool InsidePolygon(int scan, double mz)
        {
            int xPixel;
            if (currentValuesPerPixelX == 1)
                xPixel = scan;
            else
                xPixel = (scan - currentMinMobility) * -currentValuesPerPixelX;

            // to find the y_pixel, the mz is linearized vertically.
            var height = pnl_2DMap.Height;
            var mzMax = uimfReader.MzCalibration.TOFtoMZ(currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
            var mzMin = uimfReader.MzCalibration.TOFtoMZ(currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
            var yPixel = (int)(height * (mz - mzMin) / (mzMax - mzMin));

            return InsidePolygonPixel(xPixel, yPixel);
        }

        private bool InsidePolygonPixel(int xPixel, int yPixel)
        {
            if (menuItem_SelectionCorners.Checked)
            {
                // in situations where you are zoomed in to where the points are larger than a pixel, you need to compensate.
                if (currentValuesPerPixelX < 0)
                    xPixel *= -currentValuesPerPixelX;
                if (currentValuesPerPixelY < 0)
                    yPixel *= -currentValuesPerPixelY;

                yPixel = pnl_2DMap.Height - yPixel;

                // due to strange shapes, I have to split this into triangles for results to be correct.
                var pt = new Point(xPixel, yPixel);
                if ((GetTurnDirection(plot2DSelectionCorners[0], plot2DSelectionCorners[1], pt) > 0) &&
                    (GetTurnDirection(plot2DSelectionCorners[1], plot2DSelectionCorners[2], pt) > 0) &&
                    (GetTurnDirection(plot2DSelectionCorners[2], plot2DSelectionCorners[0], pt) > 0))
                    return true;

                if ((GetTurnDirection(plot2DSelectionCorners[2], plot2DSelectionCorners[3], pt) > 0) &&
                    (GetTurnDirection(plot2DSelectionCorners[3], plot2DSelectionCorners[0], pt) > 0) &&
                    (GetTurnDirection(plot2DSelectionCorners[0], plot2DSelectionCorners[2], pt) > 0)) // counter clockwise
                    return true;

                return false;
            }
            else
                return true;
        }

        private void SwapCorners(int index1, int index2)
        {
            var tmpPoint = plot2DSelectionCorners[index1];
            plot2DSelectionCorners[index1] = plot2DSelectionCorners[index2];
            plot2DSelectionCorners[index2] = tmpPoint;
        }

        // make the points for the most outer points starting in the upper left corner
        private void ConvexPolygon()
        {
            if (GetTurnDirection(plot2DSelectionCorners[0], plot2DSelectionCorners[1], plot2DSelectionCorners[2]) < 0) // counter clockwise
                SwapCorners(1, 2);
            if (GetTurnDirection(plot2DSelectionCorners[1], plot2DSelectionCorners[2], plot2DSelectionCorners[3]) < 0) // counter clockwise
                SwapCorners(2, 3);
            if (GetTurnDirection(plot2DSelectionCorners[2], plot2DSelectionCorners[3], plot2DSelectionCorners[0]) < 0) // counter clockwise
                SwapCorners(3, 0);
            if (GetTurnDirection(plot2DSelectionCorners[3], plot2DSelectionCorners[0], plot2DSelectionCorners[1]) < 0) // counter clockwise
                SwapCorners(0, 1);

            var upperLeft = 0;
            var smallestDist = 1000000000;
            for (var i = 0; i < 4; i++)
            {
                var dist = (plot2DSelectionCorners[i].Y ^ 2) + (plot2DSelectionCorners[i].X ^ 2);
                if (dist < smallestDist)
                {
                    upperLeft = i;
                    smallestDist = dist;
                }
            }

            for (var i = 0; i < upperLeft; i++) // if upper left is 0, then we are good to go
            {
                var tmpPoint = plot2DSelectionCorners[0];
                plot2DSelectionCorners[0] = plot2DSelectionCorners[1];
                plot2DSelectionCorners[1] = plot2DSelectionCorners[2];
                plot2DSelectionCorners[2] = plot2DSelectionCorners[3];
                plot2DSelectionCorners[3] = tmpPoint;
            }
        }

        #endregion

        #region Context Menu Events

        // Handler for the pb_2DMap's ContextMenu
        private void ZoomContextMenu(object sender, EventArgs e)
        {
            // Who sent you?
            if (sender == menuItemZoomFull)
            {
                // Reinitialize
                zoomHistory.Clear();

                newMinTofBin = 0;
                newMinMobility = 0;
                newMaxTofBin = frameMaximumTofBins;
                newMaxMobility = frameMaximumMobility;

                selectingMobilityRange = false;
                plot_Mobility.ClearRange();

                AutoScrollPosition = new Point(0, 0);
                hsb_2DMap.Value = 0;

                needToUpdate2DPlot = true;
            }
            else if (sender == menuItemZoomPrevious)
            {
                if (zoomHistory.Count < 2)
                {
                    Plot2DDoubleClick(this, EventArgs.Empty);
                    return;
                }

                var newZoom = zoomHistory[zoomHistory.Count - 2];
                newMinMobility = newZoom.XMin;
                newMaxMobility = newZoom.XMax;

                newMinTofBin = newZoom.YMin;
                newMaxTofBin = newZoom.YMax;

                zoomHistory.RemoveAt(zoomHistory.Count - 1);

                needToUpdate2DPlot = true;
            }
            else if (sender == menuItemZoomOut) // double the view window
            {
                int temp = currentMaxMobility - currentMinMobility + 1;
                newMinMobility = currentMinMobility - (temp / 3) - 1;
                if (newMinMobility < 0)
                    newMinMobility = 0;
                newMaxMobility = currentMaxMobility + (temp / 3) + 1;
                if (newMaxMobility > frameMaximumMobility)
                    newMaxMobility = frameMaximumMobility - 1;

                temp = currentMaxTofBin - currentMinTofBin + 1;
                newMinTofBin = currentMinTofBin - temp - 1;
                if (newMinTofBin < 0)
                    newMinTofBin = 0;
                newMaxTofBin = currentMaxTofBin + temp + 1;
                if (newMaxTofBin > frameMaximumTofBins)
                    newMaxTofBin = frameMaximumTofBins - 1;

                SaveZoom(newMinMobility, newMaxMobility, newMinTofBin, newMaxTofBin);

                needToUpdate2DPlot = true;

                //Zoom(new System.Drawing.Point(new_minMobility, new_maxBin), new System.Drawing.Point(new_maxMobility, new_minBin));
            }
        }

        private void MobilityShowScanNumberClick(object sender, EventArgs e)
        {
            showMobilityScanNumber = true;

            needToUpdate2DPlot = true;

            menuItem_Mobility.Checked = true;
            menuItem_ScanTime.Checked = false;
        }

        private void MobilityShowScanTimeClick(object sender, EventArgs e)
        {
            if (averageDriftScanDuration.Equals(-1.0))
            {
                MessageBox.Show(this, "The mean scan time is not available for this frame.");
                return;
            }
            showMobilityScanNumber = false;

            needToUpdate2DPlot = true;

            menuItem_Mobility.Checked = false;
            menuItem_ScanTime.Checked = true;
        }

        private void ConvertContextMenu(object sender, EventArgs e)
        {
            if (sender == menuItemConvertToMZ)
            {
                menuItemConvertToMZ.Checked = true;
                menuItemConvertToTOF.Checked = false;

                if (!chromatogramControlVm.CompletePeakChromatogramChecked && !chromatogramControlVm.PartialPeakChromatogramChecked)
                {
                    displayTofValues = false;
                    needToUpdate2DPlot = true;
                }
            }
            else if (sender == menuItemConvertToTOF)
            {
                displayTofValues = true;
                menuItemConvertToMZ.Checked = false;
                menuItemConvertToTOF.Checked = true;

                if (!chromatogramControlVm.CompletePeakChromatogramChecked && !chromatogramControlVm.PartialPeakChromatogramChecked)
                    needToUpdate2DPlot = true;
            }
        }

        private void SaveExperimentGuiClick(object sender, EventArgs e)
        {
            var folder = Path.GetDirectoryName(uimfReader.UimfDataFile);
            var expName = Path.GetFileNameWithoutExtension(uimfReader.UimfDataFile);
            var filename = folder + "\\" + expName + ".Frame_" + uimfReader.CurrentFrameIndex.ToString("0000") + ".BMP";
            SaveExperimentGui(filename);

            MessageBox.Show(this, "Image capture for Frame saved to Desktop in file: \n" + filename);
        }

        public void SaveExperimentGui(string thumbnailPath)
        {
            var saveWidth = tabpages_Main.Width;
            var saveHeight = tabpages_Main.Height;

            Update();
            using (var g1 = CreateGraphics())
            {
                Image experimentImage = new Bitmap(saveWidth, saveHeight, g1);
                using (var g2 = Graphics.FromImage(experimentImage))
                {
                    var dc1 = g1.GetHdc();
                    var dc2 = g2.GetHdc();
                    const int rasterOpSourceCopy = 0xCC0020;
                    BitBlt(dc2, 0, 0, saveWidth, saveHeight, dc1, 0, 0, rasterOpSourceCopy);
                    g2.ReleaseHdc(dc2);
                    g1.ReleaseHdc(dc1);
                }

                experimentImage.Save(thumbnailPath, ImageFormat.Bmp);
            }
        }

        private void MobilityChromatogramPlotShowFrameClick(object sender, EventArgs e)
        {
            menuItem_Time_driftTIC.Checked = false;
            menuItem_Frame_driftTIC.Checked = true;

            showMobilityChromatogramFrameNumber = true;
        }

        private void MobilityChromatogramPlotShowTimeClick(object sender, EventArgs e)
        {
            menuItem_Time_driftTIC.Checked = true;
            menuItem_Frame_driftTIC.Checked = false;

            showMobilityChromatogramFrameNumber = false;
        }

        private void ExportDriftTicClick(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = "Select a file to export data to...",
                Filter = "Comma-separated variables (*.csv)|*.csv",
                FilterIndex = 1
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                using (var writer = new StreamWriter(saveDialog.FileName))
                {
                    if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                    {
                        var incrementMobilityValue = averageDriftScanDuration * (frameMaximumMobility + 1) * uimfReader.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations) / 1000000.0 / 1000.0;
                        for (var i = 0; i < mobilityPlotData.Length; i++)
                        {
                            writer.WriteLine("{0},{1}", (i * incrementMobilityValue) + chromatogramMinFrame, mobilityPlotData[i]);
                        }
                    }
                    else
                    {
                        var incrementMobilityValue = averageDriftScanDuration / 1000000.0;
                        var minMobilityValue = currentMinMobility * averageDriftScanDuration / 1000000.0;
                        var xCompressionMultiplier = currentValuesPerPixelX > 1 ? currentValuesPerPixelX : 1;
                        // TODO: Maybe just use waveform_mobilityPlot points for output?
                        for (var i = 0; i < mobilityPlotData.Length; i++)
                        {
                            writer.WriteLine("{0},{1}", (i * incrementMobilityValue * xCompressionMultiplier) + minMobilityValue, mobilityPlotData[i]);
                        }
                    }
                    writer.Close();
                }
            }
        }

        private void ExportCompressedIntensityMatrixClick(object sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = "Select a file to export data to...",
                    Filter = "Comma-separated values (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 1
                };


                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                        ExportChromatogramIntensityMatrix(saveDialog.FileName);
                    else
                        ExportCurrentIntensityMatrix(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ExportCompleteIntensityMatrixClick(object sender, EventArgs e)
        {
            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                MessageBox.Show(this, "This viewer is not prepared to export the chromatogram.  Please request it.");
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = "Select a file to export data to...",
                    Filter = "Comma-separated values (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 1
                };


                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    ExportCompleteIntensityMatrix(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // mike wants complete dump.
        private void ExportChromatogramIntensityMatrix(string filename)
        {
            var framesWidth = uimfReader.GetNumberOfFrames(uimfReader.CurrentFrameType);
            var framesAxis = new double[framesWidth];
            var mobHeight = uimfReader.UimfFrameParams.Scans;
            var driftAxis = new double[mobHeight];

            var dumpChromatogram = new int[framesWidth][];
            for (var i = 0; i < framesWidth; i++)
            {
                dumpChromatogram[i] = uimfReader.GetDriftChromatogram(i);
            }

            for (var i = 1; i < framesWidth; i++)
                framesAxis[i] = i;
            for (var i = 1; i < mobHeight; i++)
                driftAxis[i] = i;

            var tex = new Utilities.TextExport();
            tex.Export(filename, "scans/frame", dumpChromatogram, framesAxis, driftAxis);
        }

        private void ExportCurrentIntensityMatrix(string filename)
        {
            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                MessageBox.Show("ExportCurrentIntensityMatrix needs work chromatogram");
                return;
            }

            Generate2DIntensityArray();

            var mobWidth = data_2D.Length;
            var driftAxis = new double[mobWidth];

            var tofHeight = data_2D[0].Length;
            var tofAxis = new double[tofHeight];

            var increment = averageDriftScanDuration / 1000000.0;

            //increment = (((double)(current_maxMobility - current_minMobility)) * mean_TOFScanTime) / mob_width / 1000000.0;
            //drift_axis[0] = current_minMobility * mean_TOFScanTime / mob_width / 1000000.0;
            var xCompressionMultiplier = currentValuesPerPixelX > 1 ? currentValuesPerPixelX : 1;
            driftAxis[0] = currentMinMobility * averageDriftScanDuration / 1000000.0;
            for (var i = 1; i < mobWidth; i++)
                driftAxis[i] = (driftAxis[i - 1] + increment * xCompressionMultiplier);

            if (displayTofValues)
            {
                var minTof = (currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                var maxTof = (currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                var incrementTof = (maxTof - minTof) / pnl_2DMap.Height;
                for (var i = 0; i < tofHeight; i++)
                {
                    tofAxis[i] = i * incrementTof + minTof;
                }
            }
            else
            {
                // linearize the mz and find the bin.
                // calculate the mz, then convert to TOF for all the values.
                var mzMax = Convert.ToDouble(num_maxBin.Value);
                var mzMin = Convert.ToDouble(num_minBin.Value);

                increment = (mzMax - mzMin) / (tofHeight - 1.0);

                tofAxis[0] = mzMin;
                for (var i = 1; i < tofHeight; i++)
                {
                    tofAxis[i] = (tofAxis[i - 1] + increment);
                }
            }

            var tex = new Utilities.TextExport();
            if (displayTofValues)
                tex.Export(filename, "bin", data_2D, driftAxis, tofAxis);
            else
                tex.Export(filename, "m/z", data_2D, driftAxis, tofAxis);
        }

        private void ExportCompleteIntensityMatrix(string filename)
        {
            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                MessageBox.Show("ExportCompleteIntensityMatrix needs work chromatogram");
                return;
            }

            //string points = "";
            var minTofBin = currentMinTofBin;
            var maxTofBin = currentMaxTofBin;
            var minMobility = currentMinMobility;
            var maxMobility = currentMaxMobility;

            Generate2DIntensityArray();
            if (menuItem_SelectionCorners.Checked)
            {
                var largestX = plot2DSelectionCorners[0].X;
                var largestY = pnl_2DMap.Height - plot2DSelectionCorners[0].Y;
                var smallestX = plot2DSelectionCorners[0].X;
                var smallestY = pnl_2DMap.Height - plot2DSelectionCorners[0].Y;
                for (var i = 1; i < 4; i++)
                {
                    if (largestX < plot2DSelectionCorners[i].X)
                        largestX = plot2DSelectionCorners[i].X;
                    if (largestY < pnl_2DMap.Height - plot2DSelectionCorners[i].Y)
                        largestY = pnl_2DMap.Height - plot2DSelectionCorners[i].Y;

                    if (smallestX > plot2DSelectionCorners[i].X)
                        smallestX = plot2DSelectionCorners[i].X;
                    if (smallestY > pnl_2DMap.Height - plot2DSelectionCorners[i].Y)
                        smallestY = pnl_2DMap.Height - plot2DSelectionCorners[i].Y;
                }

                var xPos = currentMinMobility + (largestX * (currentMaxMobility - currentMinMobility) / pnl_2DMap.Width) + 1;
                var yPos = currentMinTofBin + (largestY * (currentMaxTofBin - currentMinTofBin) / pnl_2DMap.Height);
                if (xPos < maxMobility)
                    maxMobility = xPos;
                if (yPos < maxTofBin)
                    maxTofBin = yPos;

                xPos = currentMinMobility + (smallestX * (currentMaxMobility - currentMinMobility) / pnl_2DMap.Width) + 1;
                yPos = currentMinTofBin + (smallestY * (currentMaxTofBin - currentMinTofBin) / pnl_2DMap.Height);
                if (xPos > minMobility)
                    minMobility = xPos;
                if (yPos > minTofBin)
                    minTofBin = yPos;
            }

            var totalScans = maxMobility - minMobility + 1;
            var totalBins = maxTofBin - minTofBin + 1;
            var driftAxis = new double[totalScans];
            var tofAxis = new double[totalBins];

            var increment = (uimfReader.UimfFrameParams.Scans * averageDriftScanDuration) / uimfReader.UimfFrameParams.Scans / 1000000.0;

            driftAxis[0] = minMobility * increment;

            for (var i = 1; i < totalScans; i++)
                driftAxis[i] = (driftAxis[i - 1] + increment);

            if (displayTofValues)
            {
                for (var i = minTofBin; i <= maxTofBin; i++)
                {
                    tofAxis[i - minTofBin] = i * uimfReader.TenthsOfNanoSecondsPerBin * 1.0e-4;
                }
            }
            else
            {
                // linearize the mz and find the bin.
                // calculate the mz, then convert to TOF for all the values.
                for (var i = minTofBin; i <= maxTofBin; i++)
                {
                    tofAxis[i - minTofBin] = uimfReader.MzCalibration.TOFtoMZ(i * uimfReader.TenthsOfNanoSecondsPerBin);
                }
            }

            var exportData = uimfReader.AccumulateFrameData(uimfReader.CurrentFrameNum, uimfReader.CurrentFrameNum, displayTofValues, minMobility, maxMobility, minTofBin, maxTofBin);

            // if masking, clear everything outside of mask to zero.
            if (menuItem_SelectionCorners.Checked)
            {
                var tic = 0;
                for (var i = 0; i < totalScans; i++)
                {
                    for (var j = 0; j < totalBins; j++)
                    {
                        tic += exportData[i][j];
                        //if (!inside_Polygon((minMobility + i) * pnl_2DMap.Width / (current_maxMobility - current_minMobility), (minTofBin + j) * pnl_2DMap.Height / (current_maxBin - current_minBin)))
                        //    exportData[i][j] = 0;
                    }
                }
            }

            var tex = new Utilities.TextExport();
            if (displayTofValues)
                tex.Export(filename, "bin", exportData, driftAxis, tofAxis);
            else
                tex.Export(filename, "m/z", exportData, driftAxis, tofAxis);
        }

        private void CopyImageToClipboard(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(pnl_2DMap.BackgroundImage);
        }

        private void TofExportDataClick(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = "Select a file to export data to...",
                Filter = "Comma-separated variables (*.csv)|*.csv",
                FilterIndex = 1
            };


            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                using (var writer = new StreamWriter(saveDialog.FileName))
                {
                    if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                    {
                        const double incrementTofValue = 1.0;
                        for (var i = 0; i < tofMzPlotData.Length; i++)
                        {
                            writer.WriteLine("{0},{1}", (i * incrementTofValue) + chromatogramMinMobility, tofMzPlotData[i]);
                        }
                    }
                    else
                    {
                        if (displayTofValues)
                        {
                            var minTof = (currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                            var maxTof = (currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                            var incrementTof = (maxTof - minTof) / pnl_2DMap.Height;
                            for (var i = 0; i < tofMzPlotData.Length; i++)
                            {
                                writer.WriteLine("{0},{1}", (i * incrementTof) + minTof, tofMzPlotData[i]);
                            }
                        }
                        else
                        {
                            var savedIntensities = new int[uimfReader.UimfGlobalParams.Bins];

                            for (var i = uimfReader.CurrentFrameIndex - uimfReader.FrameWidth + 1; i <= uimfReader.CurrentFrameIndex; i++)
                            {
                                var frameIntensities = uimfReader.GetSumScans(i, currentMinMobility, currentMaxMobility);

                                for (var j = 0; j < uimfReader.UimfGlobalParams.Bins; j++)
                                    savedIntensities[j] += frameIntensities[j];
                            }

                            var mzMax = uimfReader.MzCalibration.TOFtoMZ(currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
                            var mzMin = uimfReader.MzCalibration.TOFtoMZ(currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
                            for (var i = 0; i < savedIntensities.Length; i++)
                            {
                                var mz = uimfReader.MzCalibration.TOFtoMZ(i * uimfReader.TenthsOfNanoSecondsPerBin);
                                if ((mz >= mzMin) && (mz <= mzMax))
                                    writer.WriteLine("{0},{1}", mz, savedIntensities[i]);
                            }
                        }
                    }

                    writer.Close();
                }
            }
        }

        private void WriteUimfClick(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CheckFileExists = false,
                Title = "Save merged frame to UIMF file...",
                Filter = "Comma-separated variables (*.uimf)|*.uimf",
                FilterIndex = 1
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                using (var uimfWriter = new DataWriter(saveDialog.FileName))
                {
                    GlobalParams globalParams;
                    if (File.Exists(saveDialog.FileName))
                    {
                        globalParams = uimfWriter.GetGlobalParams().Clone();

                        globalParams.AddUpdateValue(GlobalParamKeyType.NumFrames, globalParams.NumFrames + 1);
                    }
                    else
                    {
                        uimfWriter.CreateTables(null);

                        globalParams = uimfReader.GetGlobalParams().Clone();

                        var experimentStartDate = new DateTime(1970, 1, 1);
                        globalParams.AddUpdateValue(GlobalParamKeyType.DateStarted, experimentStartDate.ToLocalTime().ToShortDateString() + " " + experimentStartDate.ToLocalTime().ToLongTimeString());
                        globalParams.AddUpdateValue(GlobalParamKeyType.NumFrames, 1);
                        globalParams.AddUpdateValue(GlobalParamKeyType.TimeOffset, 0);
                        globalParams.AddUpdateValue(GlobalParamKeyType.InstrumentName, "MergeFrames");
                    }

                    uimfWriter.InsertGlobal(globalParams);

                    AppendUimfFrame(uimfWriter, globalParams.NumFrames - 1);
                }
            }
        }

        public void AppendUimfFrame(DataWriter uimfWriter, int frameNumber)
        {
            var fp = uimfReader.UimfFrameParams.Clone();
            var totalBins = uimfReader.UimfGlobalParams.Bins;

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

            uimfWriter.InsertFrame(frameNumber, fp);

            //MessageBox.Show(current_valuesPerPixelX.ToString() + ", " + current_valuesPerPixelY.ToString());

            var mappedBins = new int[totalBins];
            var mappedIntercept = uimfReader.UimfFrameParams.CalibrationIntercept;
            var mappedSlope = uimfReader.UimfFrameParams.CalibrationSlope;
            int scan;
            for (scan = 0; scan < fp.Scans; scan++)
            {
                // zero out the mapped bins
                for (var i = 0; i < totalBins; i++)
                    mappedBins[i] = 0;

                // we need to do a scan at a time, map and sum bins.
                var startIndex = uimfReader.CurrentFrameIndex - (uimfReader.FrameWidth - 1);
                var endIndex = uimfReader.CurrentFrameIndex;

                // collect the data
                for (var frames = startIndex; (frames <= endIndex) && !viewerIsClosing; frames++)
                {
                    // this is in bin resolution.
                    var scanData = uimfReader.GetSumScans(frames, scan, scan);

                    // convert to mz resolution then map into bin resolution - sum into mapped_bins[]
                    for (var i = 0; i < scanData.Length; i++)
                    {
                        var newTofBin = uimfReader.MapBinCalibration(i, mappedSlope, mappedIntercept);

                        if (newTofBin < mappedBins.Length)
                        {
                            if (displayTofValues)
                            {
                                if (InsidePolygon(scan, newTofBin))
                                    mappedBins[newTofBin] += scanData[i];
                            }
                            else
                            {
                                var newMz = uimfReader.MzCalibration.TOFtoMZ(i * uimfReader.TenthsOfNanoSecondsPerBin);
                                if (InsidePolygon(scan, newMz))
                                    mappedBins[newTofBin] += scanData[i];
                            }
                        }
                    }
                }

                var nonZeroBins = mappedBins.Count(bin => bin != 0);
                var nzValues = new Tuple<int, int>[nonZeroBins];

                // collect the data
                var b = 0;
                const int timeOffset = 0;
                for (var i = timeOffset; (i < totalBins) && (b < nonZeroBins); i++)
                    if (mappedBins[i] != 0)
                    {
                        nzValues[b] = new Tuple<int, int>(i - timeOffset, mappedBins[i]);

                        b++;
                    }

                uimfWriter.InsertScan(frameNumber, fp, scan, nzValues, uimfReader.UimfGlobalParams.BinWidth, 0);
            }
        }

        // ////////////////////////////////////////////////////////////////
        //
        //
        private void SelectionCornersClick(object sender, EventArgs e)
        {
            menuItem_SelectionCorners.Checked = !menuItem_SelectionCorners.Checked;

            if (menuItem_SelectionCorners.Checked)
                ResetSelectionCorners();

            needToUpdate2DPlot = true;
        }

        public void ResetSelectionCorners()
        {
            if (menuItem_SelectionCorners.Checked)
            {
                plot2DSelectionCorners[0] = new Point((int)(pnl_2DMap.Width * .15), (int)(pnl_2DMap.Height * .15));
                plot2DSelectionCorners[1] = new Point((int)(pnl_2DMap.Width * .85), (int)(pnl_2DMap.Height * .15));
                plot2DSelectionCorners[2] = new Point((int)(pnl_2DMap.Width * .85), (int)(pnl_2DMap.Height * .85));
                plot2DSelectionCorners[3] = new Point((int)(pnl_2DMap.Width * .15), (int)(pnl_2DMap.Height * .85));
            }
        }

        public void DrawSelectionCorners(Graphics g)
        {
            if (!menuItem_SelectionCorners.Checked)
                return;

            // shade the outside of the selected polygon
            var points = new Point[4][];
            for (var i = 0; i < 4; i++)
                points[i] = new Point[4];

            points[0][0] = points[3][0] = plot2DSelectionCorners[0]; // top left
            points[0][1] = points[1][0] = plot2DSelectionCorners[1]; // top right
            points[2][0] = points[1][1] = plot2DSelectionCorners[2]; // bot right
            points[2][1] = points[3][1] = plot2DSelectionCorners[3]; // bot left

            points[0][3] = points[3][3] = new Point(0, 0); // top left
            points[0][2] = points[1][3] = new Point(pnl_2DMap.Width, 0); // top right
            points[2][3] = points[1][2] = new Point(pnl_2DMap.Width, pnl_2DMap.Height); // bot right
            points[2][2] = points[3][2] = new Point(0, pnl_2DMap.Height); // bot left

            for (var i = 0; i < 4; i++)
                g.FillPolygon(new SolidBrush(Color.FromArgb(144, 111, 11, 111)), points[i]);

            g.DrawLine(thick_pen, plot2DSelectionCorners[0].X, plot2DSelectionCorners[0].Y, plot2DSelectionCorners[1].X, plot2DSelectionCorners[1].Y);
            g.DrawLine(thick_pen, plot2DSelectionCorners[1].X, plot2DSelectionCorners[1].Y, plot2DSelectionCorners[2].X, plot2DSelectionCorners[2].Y);
            g.DrawLine(thick_pen, plot2DSelectionCorners[2].X, plot2DSelectionCorners[2].Y, plot2DSelectionCorners[3].X, plot2DSelectionCorners[3].Y);
            g.DrawLine(thick_pen, plot2DSelectionCorners[3].X, plot2DSelectionCorners[3].Y, plot2DSelectionCorners[0].X, plot2DSelectionCorners[0].Y);

            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), plot2DSelectionCorners[0].X - 2, plot2DSelectionCorners[0].Y - 2, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), plot2DSelectionCorners[1].X - 5, plot2DSelectionCorners[1].Y - 2, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), plot2DSelectionCorners[2].X - 5, plot2DSelectionCorners[2].Y - 5, 8, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(145, 200, 200, 200)), plot2DSelectionCorners[3].X - 2, plot2DSelectionCorners[3].Y - 5, 8, 8);
        }

        // ////////////////////////////////////////////////////////////////
        // Show only the maximum values - do not sum bins.
        //
        private void OnlyShowMaximumIntensitiesClick(object sender, EventArgs e)
        {
            menuItem_TOFMaximum.Checked = !menuItem_TOFMaximum.Checked;
            menuItem_MaxIntensities.Checked = menuItem_TOFMaximum.Checked;

            if (menuItem_TOFMaximum.Checked)
                plot_TOF.BackColor = Color.AntiqueWhite;
            else
                plot_TOF.BackColor = Color.White;

            needToUpdate2DPlot = true;
        }

        #endregion

        #region Mobility Plot and Controls

        private void MobilityPlotSelectionRangeChanged(object sender, Utilities.RangeEventArgs e)
        {
            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
                return;

            mobilitySelectionMinimum = e.Min;
            mobilitySelectionMaximum = e.Max;

            selectingMobilityRange = e.Selecting;

            needToUpdate2DPlot = true;
        }

        private void MobilityLimitsChanged(object sender, EventArgs e)
        {
            if (applyingMobilityRangeChange)
                return;
            applyingMobilityRangeChange = true;

            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                chromatogramMinFrame = Convert.ToInt32(num_minMobility.Value);
                chromatogramMaxFrame = Convert.ToInt32(num_maxMobility.Value);

                completeChromatogramCollected = false;
                partialChromatogramCollected = false;

                needToUpdate2DPlot = true;
                applyingMobilityRangeChange = false;
                return;
            }

            var min = Convert.ToInt32(num_minMobility.Value);
            var max = Convert.ToInt32(num_maxMobility.Value);

            num_minMobility.Increment = num_maxMobility.Increment = Convert.ToDecimal((Convert.ToDouble(num_maxMobility.Value) - Convert.ToDouble(num_minMobility.Value)) / 4.0);

            newMaxMobility = max;
            newMinMobility = min;

            SaveZoom(newMinMobility, newMaxMobility, newMinTofBin, newMaxTofBin);

            needToUpdate2DPlot = true;

            applyingMobilityRangeChange = false;
        }

        #endregion

        #region TOF Plot and Controls

        // ////////////////////////////////////////////////////////////////////////////////
        // This needs some more work.
        //
        private void MinTofBinChanged(object sender, EventArgs e)
        {
            if (applyingTofBinRangeChange)
                return;
            applyingTofBinRangeChange = true;

            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                chromatogramMinMobility = Convert.ToInt32(num_minBin.Value);

                if (chromatogramMaxMobility - chromatogramMinMobility < 10)
                {
                    chromatogramMaxMobility = chromatogramMinMobility + 10;
                    num_maxBin.Value = chromatogramMaxMobility;
                }
                if (chromatogramMaxMobility > uimfReader.UimfFrameParams.Scans - 1)
                {
                    chromatogramMaxMobility = uimfReader.UimfFrameParams.Scans - 1;
                    chromatogramMinMobility = chromatogramMaxMobility - 10;

                    num_minBin.Value = chromatogramMinMobility;
                    num_maxBin.Value = chromatogramMaxMobility;
                }

                needToUpdate2DPlot = true;
                applyingTofBinRangeChange = false;
                return;
            }

            if (num_minBin.Value >= num_maxBin.Value)
                num_maxBin.Value = Convert.ToDecimal(Convert.ToDouble(num_minBin.Value) + 1.0);

            try
            {
                double min;
                double max;
                if (displayTofValues)
                {
                    min = (Convert.ToDouble(num_minBin.Value) / (uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(num_maxBin.Value) / (uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(num_minBin.Value)) / uimfReader.TenthsOfNanoSecondsPerBin;
                    max = uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(num_maxBin.Value)) / uimfReader.TenthsOfNanoSecondsPerBin;
                }

                var binDiff = ((max - min + 1.0) / pnl_2DMap.Height);
                newMinTofBin = (int)min + 1;
                if (binDiff > 0.0)
                    newMaxTofBin = newMinTofBin + (int)(binDiff * pnl_2DMap.Height);
                else
                    newMaxTofBin = (int)max;

                SaveZoom(newMinMobility, newMaxMobility, newMinTofBin, newMaxTofBin);

                // lbl_ExperimentDate.Text = (new_maxBin * (TenthsOfNanoSecondsPerBin * 1e-4)).ToString() + " < " + new_maxBin.ToString();
                needToUpdate2DPlot = true;

                num_minBin.Increment = num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(num_maxBin.Value) - Convert.ToDouble(num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TRAPPED:  " + ex);
            }

            applyingTofBinRangeChange = false;
        }

        private void NaxTofBinChanged(object sender, EventArgs e)
        {
            if (applyingTofBinRangeChange)
                return;
            applyingTofBinRangeChange = true;

            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                chromatogramMaxMobility = Convert.ToInt32(num_maxBin.Value);
                if (chromatogramMaxMobility > uimfReader.UimfFrameParams.Scans - 1)
                    chromatogramMaxMobility = uimfReader.UimfFrameParams.Scans - 1;

                if (chromatogramMaxMobility - chromatogramMinMobility < 10)
                {
                    chromatogramMinMobility = chromatogramMaxMobility - 10;
                    num_minBin.Value = chromatogramMinMobility;
                }
                if (chromatogramMinMobility < 0)
                {
                    chromatogramMinMobility = 0;
                    chromatogramMaxMobility = 10;

                    num_minBin.Value = chromatogramMinMobility;
                    num_maxBin.Value = chromatogramMaxMobility;
                }

                needToUpdate2DPlot = true;
                applyingTofBinRangeChange = false;
                return;
            }

            if (num_minBin.Value >= num_maxBin.Value)
                num_minBin.Value = Convert.ToDecimal(Convert.ToDouble(num_maxBin.Value) - 1.0);

            try
            {
                double min;
                double max;
                if (displayTofValues)
                {
                    min = (Convert.ToDouble(num_minBin.Value) / (uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(num_maxBin.Value) / (uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(num_minBin.Value)) / uimfReader.TenthsOfNanoSecondsPerBin;
                    max = uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(num_maxBin.Value)) / uimfReader.TenthsOfNanoSecondsPerBin;
                }

                var binDiff = ((max - min + 1.0) / pnl_2DMap.Height);
                newMaxTofBin = (int)max + 1;
                if (binDiff > 0)
                    newMinTofBin = newMaxTofBin - (int)(binDiff * pnl_2DMap.Height);
                else
                    newMinTofBin = (int)min;

                SaveZoom(newMinMobility, newMaxMobility, newMinTofBin, newMaxTofBin);

                // lbl_ExperimentDate.Text = (new_maxBin * (TenthsOfNanoSecondsPerBin * 1e-4)).ToString() + " < " + new_maxBin.ToString();
                needToUpdate2DPlot = true;

                num_minBin.Increment = num_maxBin.Increment = Convert.ToDecimal((Convert.ToDouble(num_maxBin.Value) - Convert.ToDouble(num_minBin.Value)) / 4.0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TRAPPED:  " + ex);
            }

            applyingTofBinRangeChange = false;
        }

        #endregion

        #region Frame Selection and Controls

        private void FrameControlVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(FrameControlViewModel.CurrentFrameNumber)))
            {
                SelectedFrameChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameControlViewModel.SummedFrames)))
            {
                FrameSumRangeChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameControlViewModel.SelectedFrameType)))
            {
                SelectedFrameTypeChanged();
            }
        }

        // ////////////////////////////////////////////////////////////////////
        // Select Frame Range
        //
        private void FrameSumRangeChanged()
        {
            if ((double)frameControlVm.SummedFrames > frameControlVm.MaximumFrameNumber + 1)
            {
                frameControlVm.SummedFrames = (int)Convert.ToDecimal(frameControlVm.MaximumFrameNumber + 1);
                return;
            }
            uimfReader.FrameWidth = Convert.ToInt32(frameControlVm.SummedFrames);

            if (frameControlVm.CurrentFrameNumber < Convert.ToDouble(frameControlVm.SummedFrames))
            {
                frameControlVm.CurrentFrameNumber = (int)(Convert.ToDouble(frameControlVm.SummedFrames) - 1);
            }

            if (frameControlVm.SummedFrames > 1)
            {
                if (frameCinemaDataInterval > 0)
                    frameCinemaDataInterval = Convert.ToInt32(frameControlVm.SummedFrames / 3) + 1;
                else
                    frameCinemaDataInterval = -(Convert.ToInt32(frameControlVm.SummedFrames / 3) + 1);
            }

            needToUpdate2DPlot = true;
        }

        private void SelectedFrameChanged()
        {
            needToUpdate2DPlot = true;
        }

        // ///////////////////////////////////////////////////////////////
        // Select FrameType
        //
        private void SelectedFrameTypeChanged()
        {
            playingCinemaPlot = false;

            var frameTypeEnum = frameControlVm.SelectedFrameType;
            FilterFramesByType(frameTypeEnum);

            frameTypeChanged = true;
            needToUpdate2DPlot = true;

        }

        private void FilterFramesByType(UIMFDataWrapper.ReadFrameType frameType)
        {
            if (currentFrameType == frameType)
                return;

            var frameCount = uimfReader.SetCurrentFrameType(frameType);
            currentFrameType = frameType;
            uimfReader.CurrentFrameIndex = -1;

            Invoke(new MethodInvoker(() =>
            {
                var frameCount2 = uimfReader.GetNumberOfFrames(currentFrameType);

                if (frameCount2 == 0)
                {
                    pnl_2DMap.Visible = false;
                    hsb_2DMap.Visible = vsb_2DMap.Visible = false;

                    waveform_TOFPlot.Points = new BasicArrayPointList(new double[0], new double[0]);
                    waveform_MobilityPlot.Points = new BasicArrayPointList(new double[0], new double[0]);

                    frameControlVm.MinimumFrameNumber = 0;
                    frameControlVm.MaximumFrameNumber = 0;
                }
                else
                {
                    pnl_2DMap.Visible = true;
                    hsb_2DMap.Visible = vsb_2DMap.Visible = true;

                    pnl_2DMap.Visible = true;

                    frameControlVm.CurrentFrameNumber = 0;
                    frameControlVm.MinimumFrameNumber = 0;
                    frameControlVm.MaximumFrameNumber = frameCount2 - 1;

                    elementHost_FrameControl.Refresh();
                }
            }));

            // Reinitialize
            zoomHistory.Clear();

            newMinTofBin = 0;
            newMinMobility = 0;

            newMaxTofBin = frameMaximumTofBins = uimfReader.UimfGlobalParams.Bins - 1;
            newMaxMobility = frameMaximumMobility = uimfReader.UimfFrameParams.Scans - 1;

            if (frameCount == 0)
                return;

            if (uimfReader.GetNumberOfFrames(frameType) > DesiredChromatogramWidth)
                chromatogramControlVm.FrameCompression = uimfReader.GetNumberOfFrames(frameType) / DesiredChromatogramWidth;
            else
            {
                chromatogramControlVm.CanCreatePartialChromatogram = false;
                chromatogramControlVm.FrameCompression = 1;
            }
            currentFrameCompression = chromatogramControlVm.FrameCompression;

            selectingMobilityRange = false;
            plot_Mobility.ClearRange();

            frameControlVm.SummedFrames = 1;
            frameControlVm.MaximumFrameNumber = frameCount - 1;
            frameControlVm.CurrentFrameNumber = 0;

            // MessageBox.Show(array_FrameNum.Length.ToString());

            if (frameCount < 2)
            {
                chromatogramControlVm.ChromatogramAllowed = false;
            }
            else
            {
                chromatogramControlVm.ChromatogramAllowed = true;
            }

            needToUpdate2DPlot = true;
        }

        #endregion

        #region Plot Area Formatting Events

        // ////////////////////////////////////////////////////////////////////////////
        // change the background color
        //
        private void BackgroundSliderValueChanged()
        {
            if (pnl_2DMap != null)
            {
                needToUpdate2DPlot = true;

                if (plotAreaFormattingVm.BackgroundGrayValue >= 250)
                {
                    Opacity = .75;
                    TopMost = true;
                }
                else if (!Opacity.Equals(1.0))
                {
                    Opacity = 1.0;
                    TopMost = false;
                }
            }
        }

        private void PlotAreaFormattingVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(plotAreaFormattingVm.ThresholdSliderValue)))
            {
                needToUpdate2DPlot = true;
            }
            else if (e.PropertyName.Equals(nameof(plotAreaFormattingVm.BackgroundGrayValue)))
            {
                BackgroundSliderValueChanged();
            }
        }

        private void ColorMapOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(plotAreaFormattingVm.ColorMap.ShowMaxIntensity)))
            {
                ShowMaxIntensity(plotAreaFormattingVm.ColorMap.ShowMaxIntensity);
            }
        }

        // //////////////////////////////////////////////////////////////////////////
        // Display Settings
        //
        private void ColorSelectorChanged(object sender, EventArgs e)
        {
            needToUpdate2DPlot = true;
        }

        private void ShowMaxIntensity(bool show)
        {
            if (show)
            {
                ShowMaxIntensity();
            }
            else
            {
                needToUpdate2DPlot = true;
            }
        }

        private void ShowMaxIntensity()
        {
            int topX;
            int topY;
            int widthX;
            int widthY;

            if (currentValuesPerPixelX < 0)
            {
                topX = (plot2DMaxIntensityX * (-currentValuesPerPixelX)) - 15;
                widthX = (-currentValuesPerPixelX) + 30;
            }
            else
            {
                topX = plot2DMaxIntensityX - 15;
                widthX = 30;
            }

            if (currentValuesPerPixelY < 0)
            {
                topY = pnl_2DMap.Height - 15 - ((plot2DMaxIntensityY + 1) * (-currentValuesPerPixelY));
                widthY = (-currentValuesPerPixelY) + 30;
            }
            else
            {
                topY = pnl_2DMap.Height - 15 - plot2DMaxIntensityY;
                widthY = 30;
            }

            var g = pnl_2DMap.CreateGraphics();
            var p1 = new Pen(new SolidBrush(Color.Black), 3);
            g.DrawEllipse(p1, topX, topY, widthX, widthY);
            var p2 = new Pen(new SolidBrush(Color.White), 1);
            g.DrawEllipse(p2, topX, topY, widthX, widthY);
        }

        private void PlotAreaFormattingReset(object sender, EventArgs e)
        {
            // redraw everything.
            needToUpdate2DPlot = true;
        }

        #endregion

        // /////////////////////////////////////////////////////////////////////
        // UpdateCursorReading()
        //
        private void UpdateCursorReading(MouseEventArgs e)
        {
            if ((chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked) || !frameInfoVm.CursorTabSelected)
                return;

            double mobility = (currentValuesPerPixelX >= 1 ? e.X * currentValuesPerPixelX : currentMinMobility + (e.X / -currentValuesPerPixelX));

            frameInfoVm.CursorMobilityScanNumber = mobility;
            if (!averageDriftScanDuration.Equals(-1.0))
                frameInfoVm.CursorMobilityScanTime = (mobility * averageDriftScanDuration);
            else
                frameInfoVm.CursorMobilityScanTime = -1;

            if (data_2D == null)
                return;
            // time_offset = imfReader.Experiment_Properties.TimeOffset;

            try
            {
                if (displayTofValues)
                {
                    // TOF is quite easy.  Using the current_valuesPerPixelY which is TOF related.
                    var tofBin = ((currentValuesPerPixelY > 0) ? currentMinTofBin + ((pnl_2DMap.Height - e.Y - 1) * currentValuesPerPixelY) : currentMinTofBin + ((pnl_2DMap.Height - e.Y - 1) / -currentValuesPerPixelY));

                    // this is required to match with the MZ values
                    // TODO: Is this really required?
                    tofBin--;   // wfd:  This is a Cheat!!! not sure what side of this belongs MZ or TOF

                    frameInfoVm.CursorTOFValue = tofBin * uimfReader.TenthsOfNanoSecondsPerBin * 1e-4; // convert to usec
                    frameInfoVm.CursorMz = uimfReader.MzCalibration.TOFtoMZ((float) (tofBin * uimfReader.TenthsOfNanoSecondsPerBin));
                }
                else
                {
                    // Much more difficult to find where the mz <-> TOF index correlation
                    //
                    // linearize the mz and find the cursor.
                    // calculate the mz, then convert to TOF for all the values.
                    var mzMax = uimfReader.MzCalibration.TOFtoMZ(currentMaxTofBin * uimfReader.TenthsOfNanoSecondsPerBin);
                    var mzMin = uimfReader.MzCalibration.TOFtoMZ(currentMinTofBin * uimfReader.TenthsOfNanoSecondsPerBin);

                    var diffMz = mzMax - mzMin;
                    var rangeTof = currentMaxTofBin - currentMinTofBin;
                    var indexY = (currentValuesPerPixelY > 0) ? (pnl_2DMap.Height - e.Y - 1) * currentValuesPerPixelY : (pnl_2DMap.Height - e.Y - 1) / (-currentValuesPerPixelY);
                    var mz = ((double)indexY / rangeTof) * diffMz + mzMin;
                    var tofValue = uimfReader.MzCalibration.MZtoTOF(mz);

                    frameInfoVm.CursorMz = mz;
                    frameInfoVm.CursorTOFValue = tofValue * 1e-4; // convert to microseconds
                }

                frameInfoVm.TimeOffsetNs = uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0);

                if (currentValuesPerPixelY < 0)
                {
                    plot_TOF.Refresh();

                    // TODO: This is where the intensity is drawn on the TOF plot with a red line.
                    var g = plot_TOF.CreateGraphics();
                    var yStep = ((e.Y / currentValuesPerPixelY) * currentValuesPerPixelY) + (int)plot_TOF.GraphPane.Chart.Rect.Top;
                    var dp = new Pen(new SolidBrush(Color.Red), 1) { DashStyle = DashStyle.Dot };
                    g.DrawLine(dp, plot_TOF.GraphPane.Chart.Rect.Left, yStep, plot_TOF.GraphPane.Chart.Rect.Left + plot_TOF.GraphPane.Chart.Rect.Width, yStep);
                    var ampIndex = (pnl_2DMap.Height - e.Y - 1) / (-currentValuesPerPixelY);
                    var amplitude = tofTicData[ampIndex].ToString();
                    var ampFont = new Font("Lucida", 8, FontStyle.Regular);
                    var leftStr = (int)plot_TOF.GraphPane.Chart.Rect.Left - (int)g.MeasureString(amplitude, ampFont).Width - 10;

                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), leftStr, yStep - 7, plot_TOF.GraphPane.Chart.Rect.Left - 1, yStep - 7);
                    g.DrawLine(new Pen(new SolidBrush(Color.DimGray), 1), leftStr, yStep - 7, leftStr, yStep + 6);
                    g.FillRectangle(new SolidBrush(Color.GhostWhite), leftStr + 1, yStep - 6, plot_TOF.GraphPane.Chart.Rect.Left - leftStr - 1, 13);
                    g.DrawLine(new Pen(new SolidBrush(Color.White), 1), leftStr + 1, yStep + 7, plot_TOF.GraphPane.Chart.Rect.Left - 1, yStep + 7);

                    g.DrawString(amplitude, ampFont, new SolidBrush(Color.Red), leftStr + 5, yStep - 6);
                }
            }
            catch (Exception)
            {
                // This occurs when you are zooming into the plot and go off the edge to the
                // top.  Try it...  perfect place to ignore an error
            }
        }

        #region Play through frames

        private bool playingCinemaPlot;
        private int frameCinemaDataInterval;

        private void FramesStopPlayingClick(object sender, EventArgs e)
        {
            StopCinema();
        }

        private void FramesPlayLeftClick(object sender, EventArgs e)
        {
            if (frameControlVm.CurrentFrameNumber <= frameControlVm.MinimumFrameNumber) // frame index starts at 0
                return;

            playingCinemaPlot = true;
            frameCinemaDataInterval = -(Convert.ToInt32(frameControlVm.SummedFrames) / 3) - 1;
            frameControlVm.CurrentFrameNumber += frameCinemaDataInterval;
        }

        private void FramesPlayRightClick(object sender, EventArgs e)
        {
            if (frameControlVm.CurrentFrameNumber >= frameControlVm.MaximumFrameNumber)
                return;

            playingCinemaPlot = true;
            frameCinemaDataInterval = (Convert.ToInt32(frameControlVm.SummedFrames) / 3) + 1;
            if (frameControlVm.CurrentFrameNumber + frameCinemaDataInterval > Convert.ToInt32(frameControlVm.MaximumFrameNumber))
                frameControlVm.CurrentFrameNumber = frameControlVm.MaximumFrameNumber - Convert.ToInt32(frameControlVm.SummedFrames);
            else
            {
                if (frameControlVm.CurrentFrameNumber + frameCinemaDataInterval > frameControlVm.MaximumFrameNumber)
                    frameControlVm.CurrentFrameNumber = frameControlVm.MaximumFrameNumber - frameCinemaDataInterval;
                else
                    frameControlVm.CurrentFrameNumber += frameCinemaDataInterval;

            }
        }

        private void StopCinema()
        {
            frameControlView.Dispatcher.Invoke(() =>
            {
                frameControlVm.PlayingFramesBackward = false;
                frameControlVm.PlayingFramesForward = false;
            });

            playingCinemaPlot = false;
            frameCinemaDataInterval = 0;

            needToUpdate2DPlot = true;
        }

        #endregion

        #region 2DMap Scrollbar

        private void Map2DHorizontalScroll(object sender, ScrollEventArgs e)
        {
            int diff = frameMaximumMobility - hsb_2DMap.Maximum;
            if (zoomHistory.Count > 0)
            {
                diff = zoomHistory[zoomHistory.Count - 1].XDiff;
            }

            newMinMobility = Math.Min(hsb_2DMap.Value, hsb_2DMap.Maximum);
            newMaxMobility = newMinMobility + diff;

            needToUpdate2DPlot = true;
        }

        private void Map2DVerticalScroll(object sender, ScrollEventArgs e)
        {
            int diff = frameMaximumTofBins - vsb_2DMap.Maximum;
            if (zoomHistory.Count > 0)
            {
                diff = zoomHistory[zoomHistory.Count - 1].YDiff;
            }

            newMinTofBin = vsb_2DMap.Maximum - vsb_2DMap.Value;
            newMaxTofBin = newMinTofBin + diff;

            needToUpdate2DPlot = true;
        }

        #endregion

        private void RefreshClick(object sender, EventArgs e)
        {
            tab_DataViewer.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);

            plot_Mobility.Dispose();
            plot_TOF.Dispose();

            SetupPlots();

            SetMobilityPlotData(mobilityTicData);
            SetTofMzPlotData(tofTicData);

            // MessageBox.Show("refresh");
            //IonMobilityDataView_Resize((object)null, (EventArgs)null);
            viewerNeedsResizing = true;
            btn_Refresh.Enabled = true;
        }

        #region Chromatogram

        private void ChromatogramControlVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ChromatogramControlViewModel.PartialPeakChromatogramChecked)))
            {
                PartialChromatogramCheckedChanged();
            }
            else if (e.PropertyName.Equals(nameof(ChromatogramControlViewModel.CompletePeakChromatogramChecked)))
            {
                CompleteChromatogramCheckedChanged();
            }
            else if (e.PropertyName.Equals(nameof(ChromatogramControlViewModel.FrameCompression)))
            {
                FrameCompressionChanged();
            }
        }

        private void PartialChromatogramCheckedChanged()
        {
            if (chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                if (chromatogramControlVm.FrameCompression == 1)
                {
                    chromatogramControlVm.CompletePeakChromatogramChecked = true;
                    return;
                }

                ChromatogramCheckedChanged();

                pnl_2DMap.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                pnl_2DMap.BackgroundImageLayout = ImageLayout.None;
            }
        }

        private void CompleteChromatogramCheckedChanged()
        {
            if (chromatogramControlVm.CompletePeakChromatogramChecked)
            {
                ChromatogramCheckedChanged();

                pnl_2DMap.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                pnl_2DMap.BackgroundImageLayout = ImageLayout.None;
            }
        }

        private void FrameCompressionChanged()
        {
            if (chromatogramControlVm.FrameCompression != currentFrameCompression)
            {
                chromatogramControlVm.NoChromatogramChecked = true;

                completeChromatogramCollected = false;
                partialChromatogramCollected = false;
            }
            else
            {
                if (completeChromatogramCollected)
                {
                    chromatogramControlVm.CompletePeakChromatogramChecked = true;

                    completeChromatogramCollected = true;
                    partialChromatogramCollected = false;
                }
                else if (partialChromatogramCollected)
                {
                    chromatogramControlVm.PartialPeakChromatogramChecked = true;

                    partialChromatogramCollected = true;
                    completeChromatogramCollected = false;
                }
                else
                {
                    completeChromatogramCollected = false;
                    partialChromatogramCollected = false;
                }
            }
        }

        public void ChromatogramCheckedChanged()
        {
            if (uimfReader.UimfGlobalParams.NumFrames < 2)
            {
                if (!chromatogramControlVm.CompletePeakChromatogramChecked && !chromatogramControlVm.PartialPeakChromatogramChecked)
                    return;

                MessageBox.Show("Chromatograms are not available with less than 2 frames");

                chromatogramControlVm.CompletePeakChromatogramChecked = false;
                chromatogramControlVm.PartialPeakChromatogramChecked = false;
                chromatogramControlVm.NoChromatogramChecked = true;
                chromatogramControlVm.ChromatogramAllowed = false;

                return;
            }

            if (chromatogramControlVm.CompletePeakChromatogramChecked || chromatogramControlVm.PartialPeakChromatogramChecked)
            {
                if (chromatogramControlVm.CompletePeakChromatogramChecked)
                    partialChromatogramCollected = false;
                else
                    completeChromatogramCollected = false;

                hsb_2DMap.Value = 0;

                uimfReader.CurrentFrameIndex = frameControlVm.CurrentFrameNumber;
                plot_Mobility.StopAnnotating(true);

                selectingMobilityRange = false;
                plot_Mobility.ClearRange();

                frameControlVm.ShowChromatogramLabel = true;

                vsb_2DMap.Hide();
                // hsb_2DMap.Hide();

                displayTofValues = false;

                num_minBin.DecimalPlaces = 0;
                num_maxBin.DecimalPlaces = 0;
                num_minBin.Increment = 1;
                num_maxBin.Increment = 1;

                num_minMobility.DecimalPlaces = 0;
                num_minMobility.Increment = 1;
                num_maxMobility.DecimalPlaces = 0;
                num_maxMobility.Increment = 1;
            }
            else
            {
                if (menuItemConvertToTOF.Checked)
                    displayTofValues = true;
                else
                    displayTofValues = false;

                vsb_2DMap.Show();
                hsb_2DMap.Show();

                frameControlVm.ShowChromatogramLabel = false;

                Update();

                num_minBin.DecimalPlaces = 4;
                num_maxBin.DecimalPlaces = 4;
                num_minBin.Increment = 20;
                num_maxBin.Increment = 20;

                num_minMobility.DecimalPlaces = 2;
                num_minMobility.Increment = 20;
                num_maxMobility.DecimalPlaces = 2;
                num_maxMobility.Increment = 20;
            }

            plotAreaFormattingVm.SafeReset();
            GC.Collect();

            needToUpdate2DPlot = true;
        }

        #endregion

        #region Calibration

        // /////////////////////////////////////////////////////////////
        // Set Calibration.
        //
        // the trick here is to mess with the settings without messing with the file until
        // it is requested.
        //

        private void FrameInfoVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(FrameInfoViewModel.CalibrationK)))
            {
                CalibratorAChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameInfoViewModel.CalibrationT0)))
            {
                CalibratorT0Changed();
            }
        }

        private void CalibratorAChanged()
        {
            // modify the view; but not the file.
            try
            {
                uimfReader.MzCalibration.K = frameInfoVm.CalibrationK;
                CalibratorChanged();
                frameInfoVm.ChangeCalibrationKFailed = false;
            }
            catch (Exception)
            {
                frameInfoVm.ChangeCalibrationKFailed = true;
                frameInfoVm.CanRevertCalDefaults = true;
            }
        }

        private void CalibratorT0Changed()
        {
            try
            {
                uimfReader.MzCalibration.T0 = frameInfoVm.CalibrationT0;
                CalibratorChanged();
                frameInfoVm.ChangeCalibrationT0Failed = false;
            }
            catch (Exception)
            {
                frameInfoVm.ChangeCalibrationT0Failed = true;
                frameInfoVm.CanRevertCalDefaults = true;
            }
        }

        public void CalibratorChanged()
        {
            if (!frameInfoVm.CalibrationK.Equals(uimfReader.MzCalibration.K) ||
                !frameInfoVm.CalibrationT0.Equals(uimfReader.MzCalibration.T0))
            {
                // m_frameParameters.CalibrationSlope = frameInfoVm.CalibrationK; //UIMF_DataReader.mz_Calibration.k * 10000.0;
                // m_frameParameters.CalibrationIntercept = frameInfoVm.CalibrationT0; // UIMF_DataReader.mz_Calibration.t0 / 10000.0;
                // ReloadCalibrationCoefficients();

                frameInfoVm.CalibrationDate = DateTime.Now;

                // TODO: Does this switch the tabs, as desired?
                frameInfoVm.CalibrationTabSelected = true;

                frameInfoVm.ShowCalibrationButtons();
            }

            // Redraw
            // Save old scroll value to move there after conversion
            needToUpdate2DPlot = true;
        }

        private void PostProcessingCalibrationChanged(object sender, EventArgs e)
        {
            ReloadCalibrationCoefficients();
            needToUpdate2DPlot = true;
        }

        private void ReloadCalibrationCoefficients()
        {
            frameInfoVm.CalibrationK = uimfReader.MzCalibration.K;
            frameInfoVm.CalibrationT0 = uimfReader.MzCalibration.T0;
            frameInfoVm.CalibratorType = uimfReader.MzCalibration.Description;

            frameInfoVm.ChangeCalibrationKFailed = false;
            frameInfoVm.ChangeCalibrationT0Failed = false;
            frameInfoVm.HideCalibrationButtons();

            postProcessingVm.SetExperimentalCoefficients(uimfReader.MzCalibration.K * 10000.0, uimfReader.MzCalibration.T0 / 10000.0);
        }

        private void SetCalDefaultsClick(object sender, EventArgs e)
        {

            Enabled = false;

            uimfReader.UpdateAllCalibrationCoefficients(frameInfoVm.CalibrationK * 10000.0, frameInfoVm.CalibrationT0 / 10000.0);

            ReloadCalibrationCoefficients();

            Enabled = true;
            needToUpdate2DPlot = true;

            frameInfoVm.HideCalibrationButtons();
            frameInfoVm.ChangeCalibrationKFailed = false;
            frameInfoVm.ChangeCalibrationT0Failed = false;
        }

        private void RevertCalDefaultsClick(object sender, EventArgs e)
        {
            uimfReader.ReloadFrameParameters();

            ReloadCalibrationCoefficients();

            needToUpdate2DPlot = true;

            frameInfoVm.HideCalibrationButtons();
            frameInfoVm.ChangeCalibrationKFailed = false;
            frameInfoVm.ChangeCalibrationT0Failed = false;
        }

        #endregion

        #region m/z Range Selection controls

        private void MzRangeCheckedChanged(object sender, EventArgs e)
        {
            completeChromatogramCollected = false;
            partialChromatogramCollected = false;

            chromatogramControlVm.NoChromatogramChecked = true;

            needToUpdate2DPlot = true;
        }

        private void MzRangeMzChanged(object sender, EventArgs e)
        {
            completeChromatogramCollected = false;
            partialChromatogramCollected = false;

            chromatogramControlVm.NoChromatogramChecked = true;

            needToUpdate2DPlot = true;
        }

        private void MzRangePpmChanged(object sender, EventArgs e)
        {
            completeChromatogramCollected = false;
            partialChromatogramCollected = false;

            chromatogramControlVm.NoChromatogramChecked = true;

            needToUpdate2DPlot = true;
        }

        #endregion
    }
}
