using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using UIMFLibrary;
using UIMF_DataViewer.FrameControl;
using ZedGraph;

namespace UIMF_File
{
    public partial class DataViewer
    {
        private void tabpages_Main_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (this.tabpages_Main.SelectedTab == this.tab_PostProcessing)
            {
                if (this.Width < this.elementHost_PostProcessing.Left + this.elementHost_PostProcessing.Width + 70)
                {
                    this.Width = this.elementHost_PostProcessing.Left + this.elementHost_PostProcessing.Width + 70;
                    this.tabpages_Main.Width = this.Width;
                }
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
                if (current_valuesPerPixelX <= 1)
                {
                    new_maxMobility = (int) (new_minMobility + (max_Px / -current_valuesPerPixelX));
                    new_minMobility = (int) (new_minMobility + (min_Px / -current_valuesPerPixelX));
                }
                else
                {
                    new_maxMobility = (int) (new_minMobility + max_Px * current_valuesPerPixelX);
                    new_minMobility = (int) (new_minMobility + min_Px * current_valuesPerPixelX);
                }


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
                        new_maxBin = (int)this.uimfReader.GetBinForPixel((int)max_Py / -this.current_valuesPerPixelY);
                        new_minBin = (int)this.uimfReader.GetBinForPixel((int)min_Py / -this.current_valuesPerPixelY);
                    }
                    else
                    {
                        new_maxBin = (int)this.uimfReader.GetBinForPixel((int)max_Py);
                        new_minBin = (int)this.uimfReader.GetBinForPixel((int)min_Py);
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
                this.SaveZoom(new_minMobility, new_maxMobility, new_minBin, new_maxBin);

                this.current_maxBin = this.new_maxBin;
                this.current_minBin = this.new_minBin;

                this.flag_update2DGraph = true;
            }
        }

        private void SaveZoom(int minMobility, int maxMobility, int minBin, int maxBin)
        {
            var newZoom = new ZoomInfo(minMobility, maxMobility, minBin, maxBin);

            if (_zoom.Count > 0 && newZoom.Equals(_zoom[_zoom.Count - 1]))
            {
                return;
            }

            _zoom.Add(newZoom);
        }

        #region 2DMap Events

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
                    this.maxFrame_Chromatogram = this.uimfReader.SetCurrentFrameType(this.current_frame_type) - 1;

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
                    if (maxframe_Data_number > this.uimfReader.UimfGlobalParams.NumFrames)
                        maxframe_Data_number = this.uimfReader.UimfGlobalParams.NumFrames;

                    this.frameControlView.Dispatcher.Invoke(() => {
                        this.frameControlVm.CurrentFrameNumber = maxframe_Data_number;
                    });

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
                    this.hsb_2DMap.Value = 0;
                    this.uimfReader.CurrentFrameIndex = (int) this.frameControlVm.CurrentFrameNumber;

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
                    w = this.pnl_2DMap.Width / this.uimfReader.UimfGlobalParams.NumFrames;
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

        private void pnl_2DMap_MouseLeave(object sender, System.EventArgs e)
        {
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
                this.Width = this.pnl_2DMap.Left + this.uimfReader.UimfFrameParams.Scans + 170;

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
                if (frame_number > this.uimfReader.GetNumberOfFrames(this.current_frame_type))
                    frame_number = this.uimfReader.GetNumberOfFrames(this.current_frame_type) - 1;

                this.frameControlView.Dispatcher.Invoke(() => this.frameControlVm.CurrentFrameNumber = frame_number);

                this.uimfReader.CurrentFrameIndex = (int)this.frameControlVm.CurrentFrameNumber;
                this.plot_Mobility.ClearRange();

                this.vsb_2DMap.Show();  // gets hidden with Chromatogram
                this.hsb_2DMap.Show();

                // this.imf_ReadFrame(this.new_frame_index, out frame_Data);
                this.max_plot_width = this.uimfReader.UimfFrameParams.Scans;
                this.flag_update2DGraph = true;
            }
            else
            {
                // Reinitialize
                _zoom.Clear();

                this.new_minBin = 0;
                this.new_minMobility = 0;
                this.new_maxBin = this.maximum_Bins;
                this.new_maxMobility = this.maximum_Mobility;

                this.num_minMobility.Value = 0;
                this.num_maxMobility.Value = this.maximum_Mobility;

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();
                this.flag_update2DGraph = true;
                this.hsb_2DMap.Value = 0;

                this.AutoScrollPosition = new Point(0, 0);
                // this.ResizeThis();
            }
        }

        #endregion

        #region Polygon checking

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
            double mzMax = this.uimfReader.MzCalibration.TOFtoMZ(this.current_maxBin * this.uimfReader.TenthsOfNanoSecondsPerBin);
            double mzMin = this.uimfReader.MzCalibration.TOFtoMZ(this.current_minBin * this.uimfReader.TenthsOfNanoSecondsPerBin);
            y_pixel = (int)(height * (mz - mzMin) / (mzMax - mzMin));

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

        #endregion

        #region Context Menu Events

        // Handler for the pb_2DMap's ContextMenu
        protected virtual void ZoomContextMenu(object sender, System.EventArgs e)
        {
            // Who sent you?
            if (sender == this.menuItemZoomFull)
            {
                // Reinitialize
                _zoom.Clear();

                this.new_minBin = 0;
                this.new_minMobility = 0;
                this.new_maxBin = this.maximum_Bins;
                this.new_maxMobility = this.maximum_Mobility;

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                this.AutoScrollPosition = new Point(0, 0);
                this.hsb_2DMap.Value = 0;

                this.flag_update2DGraph = true;
            }
            else if (sender == this.menuItemZoomPrevious)
            {
                if (_zoom.Count < 2)
                {
                    this.pnl_2DMap_DblClick((object)null, (System.EventArgs)null);
                    return;
                }

                var newZoom = _zoom[_zoom.Count - 2];
                new_minMobility = newZoom.XMin;
                new_maxMobility = newZoom.XMax;

                new_minBin = newZoom.YMin;
                new_maxBin = newZoom.YMax;

                _zoom.RemoveAt(_zoom.Count - 1);

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

                SaveZoom(new_minMobility, new_maxMobility, new_minBin, new_maxBin);

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
            string folder = Path.GetDirectoryName(this.uimfReader.UimfDataFile);
            string exp_name = Path.GetFileNameWithoutExtension(this.uimfReader.UimfDataFile);
            string filename = folder + "\\" + exp_name + ".Accum_" + this.uimfReader.CurrentFrameIndex.ToString("0000") + ".BMP";
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
                    double increment_MobilityValue = this.mean_TOFScanTime * (this.maximum_Mobility + 1) * this.uimfReader.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations) / 1000000.0 / 1000.0;
                    for (int i = 0; i < tic_Mobility.Length; i++)
                    {
                        w.WriteLine("{0},{1}", (i * increment_MobilityValue) + this.minFrame_Chromatogram, tic_Mobility[i]);
                    }
                }
                else
                {
                    double increment_MobilityValue = mean_TOFScanTime / 1000000.0;
                    double min_MobilityValue = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
                    int xCompressionMultiplier = current_valuesPerPixelX > 1 ? current_valuesPerPixelX : 1;
                    // TODO: Maybe just use waveform_mobilityPlot points for output?
                    for (int i = 0; i < tic_Mobility.Length; i++)
                    {
                        w.WriteLine("{0},{1}", (i * increment_MobilityValue * xCompressionMultiplier) + min_MobilityValue, tic_Mobility[i]);
                    }
                }
                w.Close();
            }
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
            int frames_width = this.uimfReader.GetNumberOfFrames(this.uimfReader.CurrentFrameType);
            double[] frames_axis = new double[frames_width];
            int mob_height = this.uimfReader.UimfFrameParams.Scans;
            double[] drift_axis = new double[mob_height];

            int[][] dump_chromatogram = new int[frames_width][];
            for (int i = 0; i < frames_width; i++)
            {
                dump_chromatogram[i] = this.uimfReader.GetDriftChromatogram(i);
            }

            for (int i = 1; i < frames_width; i++)
                frames_axis[i] = i;
            for (int i = 1; i < mob_height; i++)
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
            int xCompressionMultiplier = current_valuesPerPixelX > 1 ? current_valuesPerPixelX : 1;
            drift_axis[0] = this.current_minMobility * this.mean_TOFScanTime / 1000000.0;
            for (i = 1; i < mob_width; i++)
                drift_axis[i] = (drift_axis[i - 1] + (double)increment * xCompressionMultiplier);

            if (flag_display_as_TOF)
            {
                double min_TOF = (this.current_minBin * this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                double max_TOF = (this.current_maxBin * this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
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
            int i, j;
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

            //double mob_width = this.uimfReader.UIMF_FrameParams.Scans;
            double[] drift_axis = new double[total_scans];

            //double tof_height = this.uimfReader.UIMF_GlobalParams.Bins;
            double[] tof_axis = new double[total_bins];

            double increment;
            //int bin_value;

            increment = (((double)(this.uimfReader.UimfFrameParams.Scans)) * this.mean_TOFScanTime) / this.uimfReader.UimfFrameParams.Scans / 1000000.0;

            drift_axis[0] = ((double)minmobility) * increment;

            for (i = 1; i < total_scans; i++)
                drift_axis[i] = (drift_axis[i - 1] + (double)increment);

            if (flag_display_as_TOF)
            {
                for (i = minbin; i <= maxbin; i++)
                {
                    tof_axis[i - minbin] = ((double)i) * this.uimfReader.TenthsOfNanoSecondsPerBin * 1.0e-4;
                }
            }
            else
            {
                // linearize the mz and find the bin.
                // calculate the mz, then convert to TOF for all the values.
                for (i = minbin; i <= maxbin; i++)
                {
                    tof_axis[i - minbin] = this.uimfReader.MzCalibration.TOFtoMZ(((double)i) * this.uimfReader.TenthsOfNanoSecondsPerBin);
                }
            }

            // MessageBox.Show(minbin.ToString() + "  mz " + this.uimfReader.mzCalibration.TOFtoMZ(((double)i) * this.uimfReader.TenthsOfNanoSecondsPerBin).ToString());
            var export_data = this.uimfReader.AccumulateFrameData(this.uimfReader.CurrentFrameNum, this.uimfReader.CurrentFrameNum,
                this.flag_display_as_TOF, minmobility, maxmobility, minbin, maxbin);
#if false // TODO: OLD
            int[][] export_data = new int[total_scans][];
            for (i = 0; i < total_scans; i++)
            {
                export_data[i] = new int[total_bins];
            }
            export_data = this.uimfReader.AccumulateFrameDataUncompressed(this.uimfReader.CurrentFrameIndex, this.flag_display_as_TOF, minmobility, minbin, export_data);
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
                        sw_TOF.WriteLine("{0},{1}", (i * increment_TOFValue) + this.minMobility_Chromatogram, tic_TOF[i]);
                    }
                }
                else
                {
                    if (flag_display_as_TOF)
                    {
                        double min_TOF = (this.current_minBin * this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double max_TOF = (this.current_maxBin * this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4);
                        double increment_TOF = (max_TOF - min_TOF) / (double)(this.pnl_2DMap.Height);
                        for (int i = 0; i < this.tic_TOF.Length; i++)
                        {
                            sw_TOF.WriteLine("{0},{1}", ((double)i * increment_TOF) + min_TOF, tic_TOF[i]);
                        }
                    }
                    else
                    {
                        int[] saved_intensities = new int[this.uimfReader.UimfGlobalParams.Bins];
                        int[] frame_intensities;
                        double mz = 0.0;

                        for (int i = this.uimfReader.CurrentFrameIndex - this.uimfReader.FrameWidth + 1; i <= this.uimfReader.CurrentFrameIndex; i++)
                        {
                            frame_intensities = this.uimfReader.GetSumScans(i, this.current_minMobility, this.current_maxMobility);

                            for (int j = 0; j < this.uimfReader.UimfGlobalParams.Bins; j++)
                                saved_intensities[j] += frame_intensities[j];
                        }

                        double mzMax = this.uimfReader.MzCalibration.TOFtoMZ(this.current_maxBin * this.uimfReader.TenthsOfNanoSecondsPerBin);
                        double mzMin = this.uimfReader.MzCalibration.TOFtoMZ(this.current_minBin * this.uimfReader.TenthsOfNanoSecondsPerBin);
                        for (int i = 0; i < saved_intensities.Length; i++)
                        {
                            mz = this.uimfReader.MzCalibration.TOFtoMZ((double)i * this.uimfReader.TenthsOfNanoSecondsPerBin);
                            if ((mz >= mzMin) && (mz <= mzMax))
                                sw_TOF.WriteLine("{0},{1}", mz, saved_intensities[i]);
                        }
                    }
                }
                sw_TOF.Close();
            }
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

                    Global_Params = this.uimfReader.GetGlobalParams().Clone();

                    dt_StartExperiment = new DateTime(1970, 1, 1);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.DateStarted, dt_StartExperiment.ToLocalTime().ToShortDateString() + " " + dt_StartExperiment.ToLocalTime().ToLongTimeString());
                    Global_Params.AddUpdateValue(GlobalParamKeyType.NumFrames, 1);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.TimeOffset, 0);
                    Global_Params.AddUpdateValue(GlobalParamKeyType.InstrumentName, "MergeFrames");

                    UIMF_Writer.InsertGlobal(Global_Params);
                }

                AppendUIMFFrame(UIMF_Writer, Global_Params.NumFrames - 1);

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

            fp = this.uimfReader.UimfFrameParams.Clone();
            total_bins = this.uimfReader.UimfGlobalParams.Bins;

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
            mapped_intercept = this.uimfReader.UimfFrameParams.CalibrationIntercept;
            mapped_slope = this.uimfReader.UimfFrameParams.CalibrationSlope;
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
                        this.uimfReader = this.experimentsList[exp_index];

                        start_index = this.uimfReader.CurrentFrameIndex - (this.uimfReader.FrameWidth - 1);
                        end_index = this.uimfReader.CurrentFrameIndex;

                        // collect the data
                        for (frames = start_index; (frames <= end_index) && !this.flag_Closing; frames++)
                        {
                            // this is in bin resolution.
                            scan_data = this.uimfReader.GetSumScans(frames, scan, scan);

                            // convert to mz resolution then map into bin resolution - sum into mapped_bins[]
                            for (i = 0; i < scan_data.Length; i++)
                            {
                                new_bin = this.uimfReader.MapBinCalibration(i, mapped_slope, mapped_intercept);

                                if (new_bin < mapped_bins.Length)
                                {
                                    if (flag_display_as_TOF)
                                    {
                                        if (this.inside_Polygon(scan, new_bin))
                                            mapped_bins[new_bin] += scan_data[i];
                                    }
                                    else
                                    {
                                        new_mz = this.uimfReader.MzCalibration.TOFtoMZ((double)i * this.uimfReader.TenthsOfNanoSecondsPerBin);
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

                UIMF_Writer.InsertScan(frame_number, fp, scan, nzVals, this.uimfReader.UimfGlobalParams.BinWidth, 0);
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
            this.frameControlVm.CurrentFrameNumber = merge;

            this.Enabled = false;
            //this.flag_Halt = true;
            try
            {
                for (int i = 1; i <= ((this.uimfReader.UimfGlobalParams.NumFrames - merge) / step) + 1; i++)
                {
                    //  MessageBox.Show((i * step).ToString());
                    //  continue;
                    this.frameControlVm.CurrentFrameNumber = ((i - 1) * step) + merge;

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

            //this.uimfReader.UIMF_GlobalParams.NumFrames = ((this.uimfReader.UIMF_GlobalParams.NumFrames - merge) / step) + 1;
            this.uimfReader.UimfGlobalParams.AddUpdateValue(GlobalParamKeyType.NumFrames, ((this.uimfReader.UimfGlobalParams.NumFrames - merge) / step) + 1);
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

            file_accum_IMF = Path.Combine(Path.GetDirectoryName(this.uimfReader.UimfDataFile), Path.GetFileNameWithoutExtension(this.uimfReader.UimfDataFile) + ".Accum_" + this.uimfReader.CurrentFrameNum.ToString() + ".IMF");

            num_BinTICs = new int[this.uimfReader.UimfFrameParams.Scans];
            bytes_Bin = new int[this.uimfReader.UimfFrameParams.Scans];
            bin_width = this.uimfReader.UimfGlobalParams.BinWidth;
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
            sw_IMF.WriteLine("TOFSpectra: " + this.uimfReader.UimfFrameParams.Scans.ToString());
            sw_IMF.WriteLine("NumBins: " + this.uimfReader.UimfGlobalParams.Bins.ToString());
            sw_IMF.WriteLine("BinWidth: " + bin_width.ToString("0.00") + " ns");
            sw_IMF.WriteLine("Accumulations: " + this.uimfReader.UimfFrameParams.GetValueInt32(FrameParamKeyType.Accumulations).ToString());
            sw_IMF.WriteLine("TimeOffset: " + this.uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0).ToString());

            sw_IMF.WriteLine("CalibrationSlope: " + this.uimfReader.UimfFrameParams.CalibrationSlope);
            sw_IMF.WriteLine("CalibrationIntercept: " + this.uimfReader.UimfFrameParams.CalibrationIntercept);

            sw_IMF.WriteLine("FrameNumber: " + this.uimfReader.CurrentFrameNum.ToString());
            sw_IMF.WriteLine("AverageTOFLength: " + this.uimfReader.UimfFrameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength).ToString("0.00") + " ns");

            if (string.IsNullOrWhiteSpace(this.uimfReader.UimfFrameParams.GetValue(FrameParamKeyType.MultiplexingEncodingSequence, "")))
            {
                MessageBox.Show("menuitem_SaveIMF_Click - putting in IMFProfile...");
                sw_IMF.WriteLine("MultiplexingProfile: 4Bit_24OS.txt"); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");
            }
            else
                sw_IMF.WriteLine("MultiplexingProfile: " + this.uimfReader.UimfFrameParams.GetValue(FrameParamKeyType.MultiplexingEncodingSequence, "")); //this.uimf_FrameParameters.MPBitOrder + "BitOrder");

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
            bw_IMF.Write((int)this.uimfReader.UimfFrameParams.Scans);

            // Write counter_TIC values and the channel data size (Nodes * sizeof(Node values)]for each channel
            // Each record is made up of [Int32 TOFValue, Int16 Count]
            for (i = 0; i < this.uimfReader.UimfFrameParams.Scans * 2; i++)
                bw_IMF.Write(Convert.ToInt32(0));

            double[] spectrum_array = new double[0];
            int[] bins_array = new int[0];

            num_BinTICs = new int[this.uimfReader.UimfFrameParams.Scans];
            bytes_Bin = new int[this.uimfReader.UimfFrameParams.Scans];

            //MessageBox.Show(this.uimf_FrameParameters.FrameNum.ToString());
            for (k = 0; k < this.uimfReader.UimfFrameParams.Scans; k++)
            {
                counter_TIC = 0;
                counter_bin = 0;

                try
                {
                    this.uimfReader.GetSpectrum(this.uimfReader.CurrentFrameNum, this.uimfReader.CurrentFrameNumFrameType, k, out spectrum_array, out bins_array);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("menuitem_SaveIMF_Click UIMF_DataReader: " + ex.ToString());
                }

                for (j = 0; j < spectrum_array.Length; j++)
                {
                    counter_bin++;
                    counter_TIC += bins_array[j];
                    bw_IMF.Write((spectrum_array[j] - this.uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0)) * 10); // * binWidth);
                    bw_IMF.Write(bins_array[j]);
                }

                num_BinTICs[k] = counter_TIC;
                bytes_Bin[k] = counter_bin;
            }

            // Go back to the Escape Position, then pass the number of TOFSpectraPerFrame
            bw_IMF.Seek((int)escape_position + 4, SeekOrigin.Begin);

            for (k = 0; k < this.uimfReader.UimfFrameParams.Scans; k++)
            {
                bw_IMF.Write(num_BinTICs[k]);
                bw_IMF.Write(bytes_Bin[k] * 8);
            }

            bw_IMF.Flush();

            bw_IMF.Close();
            fs_IMF.Close();
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

        #endregion

        #region Mobility Plot and Controls

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

            SaveZoom(new_minMobility, new_maxMobility, new_minBin, new_maxBin);

            this.flag_update2DGraph = true;

            this.flag_enterMobilityRange = false;
        }

        #endregion

        #region TOF Plot and Controls

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
                if (this.maxMobility_Chromatogram > this.uimfReader.UimfFrameParams.Scans - 1)
                {
                    this.maxMobility_Chromatogram = this.uimfReader.UimfFrameParams.Scans - 1;
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
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.uimfReader.TenthsOfNanoSecondsPerBin;
                    max = this.uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.uimfReader.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = ((max - min + 1.0) / this.pnl_2DMap.Height);
                new_minBin = (int)min + 1;
                if (bin_diff > 0.0)
                    this.new_maxBin = this.new_minBin + (int)(bin_diff * this.pnl_2DMap.Height);
                else
                    this.new_maxBin = (int)max;

                SaveZoom(new_minMobility, new_maxMobility, new_minBin, new_maxBin);

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
                if (this.maxMobility_Chromatogram > this.uimfReader.UimfFrameParams.Scans - 1)
                    this.maxMobility_Chromatogram = this.uimfReader.UimfFrameParams.Scans - 1;

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
                    min = (Convert.ToDouble(this.num_minBin.Value) / (this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                    max = (Convert.ToDouble(this.num_maxBin.Value) / (this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4));
                }
                else
                {
                    min = this.uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_minBin.Value)) / this.uimfReader.TenthsOfNanoSecondsPerBin;
                    max = this.uimfReader.MzCalibration.MZtoTOF(Convert.ToDouble(this.num_maxBin.Value)) / this.uimfReader.TenthsOfNanoSecondsPerBin;
                }

                bin_diff = (int)((max - min + 1) / this.pnl_2DMap.Height);
                new_maxBin = (int)max + 1;
                if (bin_diff > 0)
                    this.new_minBin = new_maxBin - (bin_diff * this.pnl_2DMap.Height);
                else
                    this.new_minBin = (int)min;

                SaveZoom(new_minMobility, new_maxMobility, new_minBin, new_maxBin);

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

        #endregion

        #region Frame Selection and Controls

        private void FrameControlVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(FrameControlViewModel.CurrentFrameNumber)))
            {
                slide_FrameSelect_ValueChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameControlViewModel.SummedFrames)))
            {
                num_FrameRange_ValueChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameControlViewModel.SelectedFrameType)))
            {
                cb_FrameType_SelectedIndexChanged();
            }
            else if (e.PropertyName.Equals(nameof(FrameControlViewModel.SelectedUimfFile)))
            {
                cb_ExperimentControlled_SelectedIndexChanged();
            }
        }

        // //////////////////////////////////////////////////////////////////////////////
        // Frame Selection
        //
        private void slide_FrameSelect_MouseDown(object obj, MouseEventArgs e)
        {
            // TODO: Wire this up somehow, or at least an alternative. Use "repeaterButtons" to only play while the mouse is held down?
            this.StopCinema();
        }

        // ////////////////////////////////////////////////////////////////////
        // Select Frame Range
        //
        private void num_FrameRange_ValueChanged()
        {
            if ((double)this.frameControlVm.SummedFrames > this.frameControlVm.MaximumFrameNumber + 1)
            {
                this.frameControlVm.SummedFrames = (int)Convert.ToDecimal(this.frameControlVm.MaximumFrameNumber + 1);
                return;
            }
            this.uimfReader.FrameWidth = Convert.ToInt32(this.frameControlVm.SummedFrames);

            if (this.frameControlVm.CurrentFrameNumber < Convert.ToDouble(this.frameControlVm.SummedFrames))
            {
                this.frameControlVm.CurrentFrameNumber = (int)(Convert.ToDouble(this.frameControlVm.SummedFrames) - 1);
            }

            if (this.frameControlVm.SummedFrames > 1)
            {
                if (this.Cinemaframe_DataChange > 0)
                    this.Cinemaframe_DataChange = Convert.ToInt32(this.frameControlVm.SummedFrames / 3) + 1;
                else
                    this.Cinemaframe_DataChange = -(Convert.ToInt32(this.frameControlVm.SummedFrames / 3) + 1);
            }

            this.flag_update2DGraph = true;
        }

        private void slide_FrameSelect_ValueChanged()
        {
            this.flag_update2DGraph = true;
        }

        // ///////////////////////////////////////////////////////////////
        // Select FrameType
        //
        private void cb_FrameType_SelectedIndexChanged()
        {
            this.flag_CinemaPlot = false;

            var frameTypeEnum = this.frameControlVm.SelectedFrameType;
            this.Filter_FrameType(frameTypeEnum);

            this.flag_FrameTypeChanged = true;
            this.flag_update2DGraph = true;

        }

        private void Filter_FrameType(UIMFDataWrapper.ReadFrameType frame_type)
        {
            if (this.current_frame_type == frame_type)
                return;

            int frame_count = 0;
            object[] read_values = new object[0];

            frame_count = this.uimfReader.SetCurrentFrameType(frame_type);
            this.current_frame_type = frame_type;
            this.uimfReader.CurrentFrameIndex = -1;

            Invoke(new ThreadStart(format_Screen));

            // Reinitialize
            _zoom.Clear();

            this.new_minBin = 0;
            this.new_minMobility = 0;

            this.new_maxBin = this.maximum_Bins = this.uimfReader.UimfGlobalParams.Bins - 1;
            this.new_maxMobility = this.maximum_Mobility = this.uimfReader.UimfFrameParams.Scans - 1;

            if (frame_count == 0)
                return;

            if (this.uimfReader.GetNumberOfFrames(frame_type) > DESIRED_WIDTH_CHROMATOGRAM)
                this.num_FrameCompression.Value = this.uimfReader.GetNumberOfFrames(frame_type) / DESIRED_WIDTH_CHROMATOGRAM;
            else
            {
                this.rb_PartialChromatogram.Enabled = false;
                this.num_FrameCompression.Value = 1;
            }
            this.current_frame_compression = Convert.ToInt32(this.num_FrameCompression.Value);

            this.flag_selection_drift = false;
            this.plot_Mobility.ClearRange();

            this.frameControlVm.SummedFrames = 1;
            this.frameControlVm.MaximumFrameNumber = frame_count - 1;
            this.frameControlVm.CurrentFrameNumber = 0;

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

        #endregion

        #region Plot Area Formatting Events

        // ////////////////////////////////////////////////////////////////////////////
        // change the background color
        //
        private void BackgroundSliderValueChanged()
        {
            if (this.pnl_2DMap != null)
            {
                //this.elementHost_PlotAreaFormatting.Update(); // TODO: This shouldn't be needed.
                this.flag_update2DGraph = true;

                if (this.plotAreaFormattingVm.BackgroundGrayValue >= 250)
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



        private void PlotAreaFormattingVmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(this.plotAreaFormattingVm.ThresholdSliderValue)))
            {
                this.flag_update2DGraph = true;
            }
            else if (e.PropertyName.Equals(nameof(this.plotAreaFormattingVm.BackgroundGrayValue)))
            {
                BackgroundSliderValueChanged();
            }
        }

        private void ColorMapOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(this.plotAreaFormattingVm.ColorMap.ShowMaxIntensity)))
            {
                ShowMaxIntensity(this.plotAreaFormattingVm.ColorMap.ShowMaxIntensity);
            }
        }

        // //////////////////////////////////////////////////////////////////////////
        // Display Settings
        //
        private void ColorSelector_Change(object sender, EventArgs e)
        {
            this.flag_update2DGraph = true;
        }

        protected virtual void ShowMaxIntensity(bool show)
        {
            if (show)
            {
                ShowMaxIntensity();
            }
            else
            {
                this.flag_update2DGraph = true;
            }
        }

        protected virtual void ShowMaxIntensity()
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

        private void PlotAreaFormattingReset(object sender, System.EventArgs e)
        {
            // redraw everything.
            this.flag_update2DGraph = true;
        }

        #endregion

        // /////////////////////////////////////////////////////////////////////
        // UpdateCursorReading()
        //
        protected virtual void UpdateCursorReading(System.Windows.Forms.MouseEventArgs e)
        {
            if ((this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked) ||
                (this.tabpages_FrameInfo.SelectedTab != this.tabPage_Cursor))
                return;

            double mobility = (current_valuesPerPixelX >= 1 ? e.X * current_valuesPerPixelX : this.current_minMobility + (e.X / -this.current_valuesPerPixelX));

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

                    this.lbl_CursorTOF.Text = (tof_bin * this.uimfReader.TenthsOfNanoSecondsPerBin * 1e-4).ToString();
                    this.lbl_CursorMZ.Text = this.uimfReader.MzCalibration.TOFtoMZ((float)(tof_bin * this.uimfReader.TenthsOfNanoSecondsPerBin)).ToString();
                }
                else
                {
                    // Much more difficult to find where the mz <-> TOF index correlation
                    //
                    // linearize the mz and find the cursor.
                    // calculate the mz, then convert to TOF for all the values.
                    double mzMax = this.uimfReader.MzCalibration.TOFtoMZ(this.current_maxBin * this.uimfReader.TenthsOfNanoSecondsPerBin);
                    double mzMin = this.uimfReader.MzCalibration.TOFtoMZ(this.current_minBin * this.uimfReader.TenthsOfNanoSecondsPerBin);

                    double diffMZ = mzMax - mzMin;
                    double rangeTOF = this.current_maxBin - this.current_minBin;
                    double indexY = (current_valuesPerPixelY > 0) ? (this.pnl_2DMap.Height - e.Y - 1) * current_valuesPerPixelY : (this.pnl_2DMap.Height - e.Y - 1) / (-current_valuesPerPixelY);
                    double mz = (indexY / rangeTOF) * diffMZ + mzMin;
                    double tof_value = this.uimfReader.MzCalibration.MZtoTOF(mz);

                    this.lbl_CursorMZ.Text = mz.ToString();
                    this.lbl_CursorTOF.Text = (tof_value * 1e-4).ToString(); // convert to usec
                }

                this.lbl_TimeOffset.Text = "Time Offset = " + this.uimfReader.UimfGlobalParams.GetValue(GlobalParamKeyType.TimeOffset, 0).ToString() + " nsec";

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

        #region Play through frames

        private bool flag_CinemaPlot = false;
        private int Cinemaframe_DataChange = 0;

        private void pb_StopPlaying_Click(object sender, EventArgs e)
        {
            this.StopCinema();
        }

        private void pb_PlayLeftOut_Click(object sender, EventArgs e)
        {
            if (this.frameControlVm.CurrentFrameNumber <= this.frameControlVm.MinimumFrameNumber) // frame index starts at 0
                return;

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = -(Convert.ToInt32(this.frameControlVm.SummedFrames) / 3) - 1;
            this.frameControlVm.CurrentFrameNumber += this.Cinemaframe_DataChange;
        }

        private void pb_PlayRightOut_Click(object sender, EventArgs e)
        {
            if (this.frameControlVm.CurrentFrameNumber >= this.frameControlVm.MaximumFrameNumber)
                return;

            this.flag_CinemaPlot = true;
            this.Cinemaframe_DataChange = (Convert.ToInt32(this.frameControlVm.SummedFrames) / 3) + 1;
            if (this.frameControlVm.CurrentFrameNumber + this.Cinemaframe_DataChange > Convert.ToInt32(this.frameControlVm.MaximumFrameNumber))
                this.frameControlVm.CurrentFrameNumber = this.frameControlVm.MaximumFrameNumber - Convert.ToInt32(this.frameControlVm.SummedFrames);
            else
            {
                if (this.frameControlVm.CurrentFrameNumber + this.Cinemaframe_DataChange > this.frameControlVm.MaximumFrameNumber)
                    this.frameControlVm.CurrentFrameNumber = this.frameControlVm.MaximumFrameNumber - this.Cinemaframe_DataChange;
                else
                    this.frameControlVm.CurrentFrameNumber += this.Cinemaframe_DataChange;

            }
        }

        private void StopCinema()
        {
            this.frameControlView.Dispatcher.Invoke(() =>
            {
                this.frameControlVm.PlayingFramesBackward = false;
                this.frameControlVm.PlayingFramesForward = false;
            });

            this.flag_CinemaPlot = false;
            this.Cinemaframe_DataChange = 0;

            this.flag_update2DGraph = true;
        }

        #endregion

        #region 2DMap Scrollbar

        protected virtual void hsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int diff = this.maximum_Mobility - this.hsb_2DMap.Maximum;
            if (_zoom.Count > 0)
            {
                diff = _zoom[_zoom.Count - 1].XDiff;
            }

            this.new_minMobility = Math.Min(this.hsb_2DMap.Value, this.hsb_2DMap.Maximum);
            this.new_maxMobility = this.new_minMobility + diff;

            this.flag_update2DGraph = true;
        }

        protected virtual void vsb_2DMap_Scroll(object sender, ScrollEventArgs e)
        {
            int diff = this.maximum_Bins - this.vsb_2DMap.Maximum;
            if (_zoom.Count > 0)
            {
                diff = _zoom[_zoom.Count - 1].YDiff;
            }

            this.new_minBin = this.vsb_2DMap.Maximum - this.vsb_2DMap.Value;
            this.new_maxBin = this.new_minBin + diff;

            this.flag_update2DGraph = true;
        }

        #endregion

        #region Drag Drop Files

        private void pb_PlayDownOut_MOUSEDOWN(object obj, MouseEventArgs e)
        {
            int selected_row = (int)this.lb_DragDropFiles.SelectedIndices[0];
            this.pb_PlayDownOut.Hide();
        }

        private void pb_PlayDownOut_MOUSEUP(object obj, MouseEventArgs e)
        {
            this.pb_PlayDownOut.Show();
        }

        private void pb_PlayUpOut_MOUSEDOWN(object obj, MouseEventArgs e)
        {
            int selected_row = (int)this.lb_DragDropFiles.SelectedIndices[0];
            this.pb_PlayUpOut.Hide();

            //MessageBox.Show("Selected Row: "+this.dg_ExperimentList.SelectedRows[0].ToString());
            if (selected_row - 1 < 0)
                return;
        }

        private void pb_PlayUpOut_MOUSEUP(object obj, MouseEventArgs e)
        {
            this.pb_PlayUpOut.Show();
        }

        // /////////////////////////////////////////////////////////////
        // Drag-Drop IMF file onto the graph
        //
        private void cb_ExperimentControlled_SelectedIndexChanged()
        {
            if (this.flag_CinemaPlot)
            {
                this.flag_Closing = true; // halt cinema frame processing asap.
                this.StopCinema();
                Thread.Sleep(100);
                this.flag_Closing = false; // we are not closing
            }

            this.index_CurrentExperiment = this.frameControlVm.UimfFiles.IndexOf(this.frameControlVm.SelectedUimfFile);
            this.lb_DragDropFiles.ClearSelected();
            this.lb_DragDropFiles.SetSelected(this.index_CurrentExperiment, true);

            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Width = this.pnl_2DMap.Left + this.uimfReader.UimfFrameParams.Scans + 170;

                this.rb_PartialChromatogram.Checked = false;
                this.rb_CompleteChromatogram.Checked = false;

                this.plot_Mobility.StopAnnotating(false);

                this.Chromatogram_CheckedChanged();

                this.uimfReader.CurrentFrameIndex = (int)this.frameControlVm.CurrentFrameNumber;
                this.plot_Mobility.ClearRange();
                this.frameControlVm.SummedFrames = 1;

                this.vsb_2DMap.Show();  // gets hidden with Chromatogram
                this.hsb_2DMap.Show();
            }

            this.uimfReader = this.experimentsList[this.index_CurrentExperiment];

            this.vsb_2DMap.Value = 0;

            if (this.uimfReader.CurrentFrameIndex < this.uimfReader.UimfGlobalParams.NumFrames - 1)
                this.frameControlVm.CurrentFrameNumber = 0;
            this.frameControlVm.MaximumFrameNumber = this.uimfReader.UimfGlobalParams.NumFrames - 1;
            this.frameControlVm.CurrentFrameNumber = this.uimfReader.CurrentFrameIndex;

            if (this.frameControlVm.MaximumFrameNumber > 0)
            {
                this.frameControlVm.MinimumFrameNumber = 0;
            }

            this.frameControlVm.SelectedFrameType = this.uimfReader.CurrentFrameType;

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
                        this.uimfReader = new UIMFDataWrapper(files[i]);
                        this.experimentsList.Add(this.uimfReader);

                        this.lb_DragDropFiles.Items.Add(files[i]);
                        this.lb_DragDropFiles.ClearSelected();

                        this.frameControlVm.UimfFiles.Add(Path.GetFileNameWithoutExtension(files[i]));
                        this.frameControlVm.SelectedUimfFile = this.frameControlVm.UimfFiles.Last();

                        this.Filter_FrameType(this.uimfReader.CurrentFrameType);
                        this.uimfReader.CurrentFrameIndex = 0;
                        this.uimfReader.SetCurrentFrameType(current_frame_type, true);

                        Generate2DIntensityArray();
                        this.GraphFrame(this.data_2D, true);

                        this.frameControlVm.SelectedFrameType = this.uimfReader.CurrentFrameType;
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

        #endregion

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

        #region Chromatogram

        private void lbl_FramesShown_Click(object sender, EventArgs e)
        {
            if (this.frameControlVm.SummedFrames > 1)
                return;

            this.frameControlVm.ShowTICItems = true;
        }

        private void btn_TIC_Click(object sender, EventArgs e)
        {
            this.frameControlVm.ShowTICItems = false;

            this.calc_TIC();
        }

        // TODO: Was not wired up before removal of NI Libraries.
        private void btn_ShowChromatogram_Click(object sender, EventArgs e)
        {
            if (this.rb_CompleteChromatogram.Checked || this.rb_PartialChromatogram.Checked)
            {
                this.Chromatogram_GUI_Settings();
            }

            this.plotAreaFormattingVm.SafeReset();

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

            this.uimfReader.CurrentFrameIndex = (int)this.frameControlVm.CurrentFrameNumber;
            this.plot_Mobility.StopAnnotating(true);

            this.flag_selection_drift = false;
            this.plot_Mobility.ClearRange();

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
            if (this.uimfReader.UimfGlobalParams.NumFrames < 2)
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

                this.uimfReader.CurrentFrameIndex = (int)this.frameControlVm.CurrentFrameNumber;
                this.plot_Mobility.StopAnnotating(true);

                this.flag_selection_drift = false;
                this.plot_Mobility.ClearRange();

                this.frameControlVm.ShowChromatogramLabel = true;

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

                this.vsb_2DMap.Show();
                this.hsb_2DMap.Show();

                this.frameControlVm.ShowChromatogramLabel = false;

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

            this.plotAreaFormattingVm.SafeReset();
            GC.Collect();

            this.flag_update2DGraph = true;
        }

        #endregion

        #region Calibration

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
                this.uimfReader.MzCalibration.K = (float)Convert.ToDouble(this.tb_CalA.Text);
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
                this.uimfReader.MzCalibration.T0 = (float)Convert.ToDouble(this.tb_CalT0.Text);
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
            if ((Convert.ToDouble(this.tb_CalA.Text) != this.uimfReader.MzCalibration.K) ||
                (Convert.ToDouble(this.tb_CalT0) != this.uimfReader.MzCalibration.T0))
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

        private void pnl_postProcessing_CalibrationChanged(object sender, EventArgs e)
        {
            this.update_CalibrationCoefficients();
            this.flag_update2DGraph = true;
        }

        private void update_CalibrationCoefficients()
        {
            this.tb_CalA.Text = this.uimfReader.MzCalibration.K.ToString("E");
            this.tb_CalT0.Text = this.uimfReader.MzCalibration.T0.ToString("E");
            this.lbl_CalibratorType.Text = this.uimfReader.MzCalibration.Description;

            this.pnl_postProcessing.set_ExperimentalCoefficients(this.uimfReader.MzCalibration.K * 10000.0, this.uimfReader.MzCalibration.T0 / 10000.0);
        }

        private void btn_setCalDefaults_Click(object sender, System.EventArgs e)
        {

            this.Enabled = false;

            this.uimfReader.UpdateAllCalibrationCoefficients((float)(Convert.ToSingle(this.tb_CalA.Text) * 10000.0), (float)(Convert.ToSingle(this.tb_CalT0.Text) / 10000.0));

            this.update_CalibrationCoefficients();

            this.Enabled = true;
            this.flag_update2DGraph = true;

            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();
        }

        private void btn_revertCalDefaults_Click(object sender, System.EventArgs e)
        {
            this.uimfReader.ReloadFrameParameters();

            this.update_CalibrationCoefficients();

            this.flag_update2DGraph = true;

            this.btn_revertCalDefaults.Hide();
            this.btn_setCalDefaults.Hide();
        }

        #endregion

        private void btn_Clean_Click(object sender, EventArgs e)
        {
            MessageBox.Show("not sure what this does.  Needs work.  wfd 02/22/11");

            string filename = "c:\\IonMobilityData\\Gordon\\Calibration\\QC\\8pep_10fr_600scans_01_0000\\" + Path.GetFileNameWithoutExtension(this.uimfReader.UimfDataFile) + "_clean.UIMF";

            if (File.Exists(filename))
                File.Delete(filename);

            DataWriter uimf_writer = new DataWriter(filename);
            FrameParams fp = new FrameParams();
            GlobalParams gp = new GlobalParams();
            int uimf_bins;

            uimf_writer.CreateTables("int");

            gp = this.uimfReader.GetGlobalParams();
            MessageBox.Show("gp: " + gp.NumFrames.ToString());

            for (int i = 1; i <= gp.NumFrames; i++)
            {
                fp = this.uimfReader.GetFrameParams(i);

                uimf_writer.InsertFrame(i, fp);

                for (int j = 0; j < this.uimfReader.UimfFrameParams.Scans; j++)
                {
                    double[] binList = new double[410000];
                    int[] intensityList = new int[410000];

                    uimf_bins = this.uimfReader.GetSpectrum(this.uimfReader.ArrayFrameNum[i], this.uimfReader.FrameTypeDict[this.uimfReader.ArrayFrameNum[i]], j, out binList, out intensityList);
                    var nzVals = new Tuple<int, int>[uimf_bins];

                    for (int k = 0; k < uimf_bins; k++)
                    {
                        nzVals[k] = new Tuple<int, int>((int)binList[k] - 10000, intensityList[k]);
                    }

                    uimf_writer.InsertScan(i, fp, j, nzVals, this.uimfReader.UimfGlobalParams.BinWidth, 0);
                }
            }

            uimf_writer.Dispose();
            MessageBox.Show("created " + filename);
        }

        private void format_Screen()
        {
            int frame_count = this.uimfReader.GetNumberOfFrames(this.current_frame_type);

            if (frame_count == 0)
            {
                this.pnl_2DMap.Visible = false;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = false;

                this.waveform_TOFPlot.Points = new BasicArrayPointList(new double[0], new double[0]);
                this.waveform_MobilityPlot.Points = new BasicArrayPointList(new double[0], new double[0]);

                this.frameControlVm.MinimumFrameNumber = 0;
                this.frameControlVm.MaximumFrameNumber = 0;

                return;
            }
            else
            {
                this.pnl_2DMap.Visible = true;
                this.hsb_2DMap.Visible = this.vsb_2DMap.Visible = true;

                this.pnl_2DMap.Visible = true;

                this.frameControlVm.CurrentFrameNumber = 0;
                this.frameControlVm.MinimumFrameNumber = 0;
                this.frameControlVm.MaximumFrameNumber = frame_count - 1;

                this.elementHost_FrameControl.Refresh();
            }
        }

        #region m/z Range Selection controls

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

        #endregion
    }
}
