
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ZedGraph;

namespace UIMF_File.Utilities
{
    public class PointAnnotationGraph : ZedGraphControl
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

        public PointAnnotationGraph() : base()
        {
            base.MouseDownEvent += OnPlotAreaMouseDown;
            base.MouseMove += OnPlotAreaMouseMove;
            base.MouseUpEvent += OnPlotAreaMouseUp;
            base.Paint += OnAfterDrawPlot;
            base.IsShowCursorValues = true;
            base.CursorValueEvent += OnCursorValueEvent;
            base.GraphPane.IsFontsScaled = false;
        }

        private string OnCursorValueEvent(ZedGraphControl sender, GraphPane pane, Point mousept)
        {
            // Regardless of the cursor location, show the value corresponding to the displayed curve.
            var curve = ((LineItem) pane.CurveList[0]).Line.BuildPointsArray(pane, pane.CurveList[0], out var points, out int count);
            var poss = points.FirstOrDefault(x => (int) x.X == mousept.X);
            var index = points.ToList().IndexOf(poss);
            if (index < 0 || index >= pane.CurveList[0].Points.Count)
            {
                return $"{poss.X:F0}, {poss.Y:F0}";
            }

            var point = pane.CurveList[0].Points[index];
            // TODO: draw markers on the line?
            // Might be possible by adding a new line with just the single point...
            if (pane.CurveList.Count == 1)
            {
                pane.CurveList.Add(new LineItem("", new BasicArrayPointList(new [] {point.X}, new [] {point.Y}), Color.Crimson, SymbolType.XCross));
            }
            else
            {
                pane.CurveList[1] = new LineItem("", new BasicArrayPointList(new[] { point.X }, new[] { point.Y }), Color.Crimson, SymbolType.XCross);
            }

           sender.Refresh();

            return $"{point.X:F0}, {point.Y:F0}";
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

        protected bool OnPlotAreaMouseDown(ZedGraphControl sender, MouseEventArgs e)
        {
          //  if (this.flag_StopAnnotating)
           //     return;

            if(e.Button == MouseButtons.Left)
            {
                //Invalidate();
                //XYPlot plot = this.Plots[0];
                double[] xData;
                if (this.GraphPane.CurveList[0].Points is BasicArrayPointList xyData)
                {
                    xData = xyData.x;
                }
                else
                {
                    var xyData2 = (PointPairList)this.GraphPane.CurveList[0].Points;
                    xData = xyData2.Select(x => x.X).ToArray();
                }

                if (xData.Length <= 1)
                    return true;

                int tick_width = ((int)this.GraphPane.Chart.Rect.Width / (xData.Length-1));
                int hitX = e.X + (tick_width/2) - (int)this.GraphPane.Chart.Rect.Left;

                //p2 = ((p2-(int)xData[0]) * w) - (w/2) + this.PlotAreaBounds.Left;

                if (tick_width < 1)
                    tick_width = 1;
                x1 = (int) ((hitX / tick_width) + xData[0]);
                x2 = -1;
            }
            this.flag_Selecting = true;
            return true;
        }

        protected void OnPlotAreaMouseMove(object sender, MouseEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if(this.flag_Selecting)
                {
                    //XYPlot plot = this.Plots[0];
                    double[] xData;
                    if (this.GraphPane.CurveList[0].Points is BasicArrayPointList xyData)
                    {
                        xData = xyData.x;
                    }
                    else
                    {
                        var xyData2 = (PointPairList)this.GraphPane.CurveList[0].Points;
                        xData = xyData2.Select(x => x.X).ToArray();
                    }

                    int tick_width = ((int)this.GraphPane.Chart.Rect.Width / (xData.Length-1));
                    int hitX = e.X + (tick_width/2) - (int)this.GraphPane.Chart.Rect.Left;

                    if (tick_width < 1)
                        tick_width = 1;
                    x2 = (int) ((hitX / tick_width) + xData[0]);

                    Invalidate();
                    OnRangeChange(false);
                }
            }
        }

        protected bool OnPlotAreaMouseUp(ZedGraphControl sender, MouseEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return true;

            if(e.Button == MouseButtons.Left)
            {
                if(this.flag_Selecting)
                {
                    this.flag_Selecting = false;
                    //x2 = e.X-this.PlotAreaBounds.Left;
                    //XYPlot plot = this.Plots[0];
                    double[] xData;
                    if (this.GraphPane.CurveList[0].Points is BasicArrayPointList xyData)
                    {
                        xData = xyData.x;
                    }
                    else
                    {
                        var xyData2 = (PointPairList)this.GraphPane.CurveList[0].Points;
                        xData = xyData2.Select(x => x.X).ToArray();
                    }

                    int tick_width = ((int)this.GraphPane.Chart.Rect.Width / (xData.Length-1));
                    int hitX = e.X + (tick_width/2) - (int)this.GraphPane.Chart.Rect.Left;;
                    if (tick_width == 0)
                        tick_width = 1;

                    //   MessageBox.Show(this, "this.plotarea.left:  "+this.PlotAreaBounds.Left.ToString());
                    x2 = (int) ((hitX / tick_width) + xData[0]);
                    Invalidate();
                    OnRangeChange(true);
                }
            }

            return true;
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
            // TODO: //this.Cursors[0].Visible = true;

            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            base.OnMouseLeave(e);

            if (this.GraphPane.CurveList.Count > 1)
            {
                this.GraphPane.CurveList.RemoveAt(1);
                this.Refresh();
            }

            xPosition = -1000;
            yPosition = -1000;
            // TODO: //this.Cursors[0].Visible = false;
            //this.Invalidate();
        }

        protected void OnAfterDrawPlot(object sender, PaintEventArgs e)
        {
            if (this.flag_StopAnnotating)
                return;

            //base.OnAfterDrawPlot(e);

            Line plot = ((LineItem)this.GraphPane.CurveList[0]).Line;
            double[] xData;
            double[] yData;
            if (this.GraphPane.CurveList[0].Points is BasicArrayPointList xyData)
            {
                xData = xyData.x;
                yData = xyData.y;
            }
            else
            {
                var xyData2 = (PointPairList) this.GraphPane.CurveList[0].Points;
                xData = xyData2.Select(x => x.X).ToArray();
                yData = xyData2.Select(x => x.Y).ToArray();
            }

            if(xData.Length > 0)
            {
                Rectangle bounds = e.ClipRectangle;
                PointF[] points; // = plot.MapDataPoints(bounds, xData, yData, false);
                int count;
                plot.BuildPointsArray(this.GraphPane, this.GraphPane.CurveList[0], out points, out count);
                DisplayToolTip(points, xData, yData, e.Graphics, bounds);
            }

            if(x2 > 0)
            {
                int p1, p2;
                p1 = Math.Min(x1, x2);
                p2 = Math.Max(x1, x2);

                // Find left boundary of p1:
                int w = (int)this.GraphPane.Chart.Rect.Width / (xData.Length-1);
                p1 = ((p1-(int)xData[0]) * w) - (w/2) + (int)this.GraphPane.Chart.Rect.Left;
                // Find right boundary of p2:
                p2 = ((p2-(int)xData[0]+1) * w) - (w/2) + (int)this.GraphPane.Chart.Rect.Left;

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(115, 200, 200, 200)), p1, e.ClipRectangle.Top, p2-p1, e.ClipRectangle.Height);
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

                        // TODO: //this.Cursors[0].XPosition = xData[x];
                        // TODO: //this.Cursors[0].YPosition = yData[x];

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
