
using System;
using System.Drawing;
using System.Windows.Forms;

using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;

namespace UIMF_File.Utilities
{
    public class PointAnnotationGraph : WaveformGraph
    {
        public SizeF _HitSize = new SizeF(1, 10);
        public int XMax;

        private int xPosition;
        private int yPosition;

        private bool flag_Selecting = false;
        private int x1=-1, x2=-1;

        private bool flag_StopAnnotating = false;
        private bool flag_isTims = false;
        private double[] ramp_TIMS;

        public event RangeEventHandler RangeChanged;

        public PointAnnotationGraph()
        {
        }

        public void StopAnnotating(bool flag)
        {
            this.flag_StopAnnotating = flag;
        }

        public void ClearRange()
        {
            this.x1 = -1;
            this.x2 = -1;

            Invalidate();
        }

        public void set_TIMSRamp(double tims_start, double tims_end, double tims_duration, int tofs, int offset)
        {
            ramp_TIMS = new double[(int) tofs];

            for (int i=0; i<(int) tims_duration; i++)
                ramp_TIMS[(i-offset+tofs) % tofs] = (((tims_end - tims_start)/tims_duration) * i) + tims_start;
            for (int i = (int)tims_duration; i < tofs; i++)
                ramp_TIMS[(i - offset + tofs) % tofs] = tims_start;

            this.flag_isTims = true;
        }

        public void SetRange(int xpos1, int xpos2)
        {
            this.flag_StopAnnotating = false;

            this.x1 = xpos1;
            this.x2 = xpos2;

            Invalidate();
        }

        protected override void OnPlotAreaMouseDown(MouseEventArgs e)
        {
          //  if (this.flag_StopAnnotating)
           //     return;

            base.OnPlotAreaMouseDown (e);
            if(e.Button == MouseButtons.Left)
            {
                //Invalidate();
                XYPlot plot = this.Plots[0];
                double[] xData = plot.GetXData();
                if (xData.Length <= 1)
                    return;

                int tick_width = (this.PlotAreaBounds.Width / (xData.Length-1));
                int hitX = e.X + (tick_width/2) - this.PlotAreaBounds.Left;

                //p2 = ((p2-(int)xData[0]) * w) - (w/2) + this.PlotAreaBounds.Left;

                if (tick_width < 1)
                    tick_width = 1;
                x1 = (int) ((hitX / tick_width) + xData[0]);
                x2 = -1;
            }
            this.flag_Selecting = true;
        }

        protected override void OnPlotAreaMouseMove(MouseEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnPlotAreaMouseMove(e);

            if (e.Button == MouseButtons.Left)
            {
                if(this.flag_Selecting)
                {
                    XYPlot plot = this.Plots[0];
                    double[] xData = plot.GetXData();
                    int tick_width = (this.PlotAreaBounds.Width / (xData.Length-1));
                    int hitX = e.X + (tick_width/2) - this.PlotAreaBounds.Left;

                    if (tick_width < 1)
                        tick_width = 1;
                    x2 = (int) ((hitX / tick_width) + xData[0]);

                    Invalidate();
                    OnRangeChange(false);
                }
            }
        }

        protected override void OnPlotAreaMouseUp(MouseEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnPlotAreaMouseUp(e);
            if(e.Button == MouseButtons.Left)
            {
                if(this.flag_Selecting)
                {
                    this.flag_Selecting = false;
                    //x2 = e.X-this.PlotAreaBounds.Left;
                    XYPlot plot = this.Plots[0];
                    double[] xData = plot.GetXData();
                    int tick_width = (this.PlotAreaBounds.Width / (xData.Length-1));
                    int hitX = e.X + (tick_width/2) - this.PlotAreaBounds.Left;;
                    if (tick_width == 0)
                        tick_width = 1;

                    //   MessageBox.Show(this, "this.plotarea.left:  "+this.PlotAreaBounds.Left.ToString());
                    x2 = (int) ((hitX / tick_width) + xData[0]);
                    Invalidate();
                    OnRangeChange(true);
                }
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnKeyPress(e);

            if(e.KeyChar == Convert.ToChar(Keys.Escape))
            {
                ClearRange();
                OnRangeChange(true, false);
            }
        }

        protected virtual void OnRangeChange(bool done)
        {
            OnRangeChange(done, true);
        }

        protected virtual void OnRangeChange(bool done, bool selecting)
        {
            if(RangeChanged != null)
            {
                RangeChanged(this, new RangeEventArgs(Math.Min(x1, x2), Math.Max(x1, x2), selecting, done));
            }
        }

        public SizeF HitSize
        {
            set { _HitSize = value; }
        }

        protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnMouseMove(e);

            //Invalidate();

            xPosition = e.X;
            yPosition = e.Y;
            this.Cursors[0].Visible = true;

            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnMouseLeave(e);

            xPosition = -1000;
            yPosition = -1000;
            this.Cursors[0].Visible = false;
            //this.Invalidate();
        }

        protected override void OnAfterDrawPlot(AfterDrawXYPlotEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnAfterDrawPlot(e);

            XYPlot plot = e.Plot;
            double[] xData = plot.GetXData();
            double[] yData = plot.GetYData();

            if(xData.Length > 0)
            {
                Rectangle bounds = e.Bounds;
                PointF[] points = plot.MapDataPoints(bounds, xData, yData, false);
                DisplayToolTip(points, xData, yData, e.Graphics, bounds);
            }

            if(x2 > 0)
            {
                int p1, p2;
                p1 = Math.Min(x1, x2);
                p2 = Math.Max(x1, x2);

                // Find left boundary of p1:
                int w = this.PlotAreaBounds.Width / (xData.Length-1);
                p1 = ((p1-(int)xData[0]) * w) - (w/2) + this.PlotAreaBounds.Left;
                // Find right boundary of p2:
                p2 = ((p2-(int)xData[0]+1) * w) - (w/2) + this.PlotAreaBounds.Left;

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(115, 200, 200, 200)), p1, e.Bounds.Top, p2-p1, e.Bounds.Height);
            }
        }

        private void DisplayToolTip(PointF []points, double []xData, double []yData, Graphics g, Rectangle bounds)
        {
            if (this.flag_StopAnnotating)
                return;

            try
            {
                // Don't display tooltip for last point: hence the -1 in the range check.
                for(int x = 0; x < points.Length; x++)
                {
                    float xCoordinate = points[x].X - (_HitSize.Width / 2);
                    float yCoordinate = points[x].Y - (_HitSize.Height / 2);
                    RectangleF hitRectangle = new RectangleF(new PointF(xCoordinate, yCoordinate), _HitSize);

                    // if(hitRectangle.Contains(xPosition - PlotAreaBounds.X, yPosition - PlotAreaBounds.Y))
                    if(hitRectangle.Contains(xPosition, yPosition))
                    {
                        string data = string.Format("x={0:F3}, y={1}", xData[x], yData[x]);
                        if ((this.flag_isTims) && (x<this.ramp_TIMS.Length))
                            data += "\n         Ramp @ "+ this.ramp_TIMS[x]+" volts";

                        this.Cursors[0].XPosition = xData[x];
                        this.Cursors[0].YPosition = yData[x];

                        SizeF sizeString = g.MeasureString(data, Font);
                        int pos_left = ((int) (hitRectangle.Right+sizeString.Width) > bounds.Width ? (int) (bounds.Width - sizeString.Width) : (int) hitRectangle.Right);
                        Rectangle displayRectangle = new Rectangle(new Point(pos_left, (int)(hitRectangle.Top - (sizeString.Height / 2))), sizeString.ToSize());
                        int offsetX = 0;
                        int offsetY = 0;

                        if (displayRectangle.Top < bounds.Top)
                            offsetY = bounds.Top - displayRectangle.Top;
                        if (displayRectangle.Left < bounds.Left)
                            offsetX = bounds.Left - displayRectangle.Left;
                        if (displayRectangle.Bottom > bounds.Bottom)
                            offsetY = bounds.Bottom - displayRectangle.Bottom;
                        if (displayRectangle.Right > XMax)
                            offsetX = XMax - displayRectangle.Right;

                        displayRectangle.Offset(offsetX, offsetY);
                        using (Brush brush = new SolidBrush(Color.White))
                        {
                            //g.FillRectangle(brush, displayRectangle);
                            g.DrawString(data, Font, Brushes.Blue, displayRectangle.Location);
                        }
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
            }
        }

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        object stop_painting = new object();
        public new void OnPaint(PaintEventArgs e)
        {
            lock (this.stop_painting)
            {
            }
        }
    }

    public class RangeEventArgs : EventArgs
    {
        public int Min;
        public int Max;
        public bool Selecting;
        public bool Done;
        public RangeEventArgs(int min, int max, bool selecting, bool done) { Min = min; Max = max; Selecting = selecting; Done = done; }
    }

    public delegate void RangeEventHandler(object sender, RangeEventArgs e);
}
