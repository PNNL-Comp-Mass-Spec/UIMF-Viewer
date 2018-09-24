using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace UIMF_DataViewer.WpfControls
{
    public class TickBarLabeled : TickBar
    {
        public string StringFormat
        {
            get => (string)GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public double TickLength
        {
            get => (double)GetValue(TickLengthProperty);
            set => SetValue(TickLengthProperty, value);
        }

        // Using a DependencyProperty as the backing store for StringFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", typeof(string), typeof(TickBarLabeled), new FrameworkPropertyMetadata("F0", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        //public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(TickBarLabeled), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(TickBarLabeled), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(TickBarLabeled));
        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TickBarLabeled));
        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(TickBarLabeled));
        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(TickBarLabeled));
        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(TickBarLabeled));
        public static readonly DependencyProperty TickLengthProperty = DependencyProperty.Register("TickLength", typeof(double), typeof(TickBarLabeled), new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        protected override Size MeasureOverride(Size availableSize)
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var text = Maximum.ToString(StringFormat);
            var formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, this.FlowDirection, typeface,
                FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            switch (this.Placement)
            {
                case TickBarPlacement.Left:
                case TickBarPlacement.Right:
                    return new Size(TickLength + 6 + formattedText.Width, 0);
                case TickBarPlacement.Top:
                case TickBarPlacement.Bottom:
                    goto default;
                default:
                    return new Size(0, TickLength + 4 + formattedText.Height);
            }
        }

        private void DrawLabel(double labelValue, double x, double y, Typeface typeface, DrawingContext dc)
        {
            var gap = 4.0;
            var text = labelValue.ToString(StringFormat);
            var formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, this.FlowDirection, typeface,
                FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var yOffset = 0.0;
            var xOffset = 0.0;
            if (this.Placement == TickBarPlacement.Left || this.Placement == TickBarPlacement.Right)
            {
                gap += 2;
                xOffset = this.Placement == TickBarPlacement.Left ? -formattedText.Width - gap : gap;
                yOffset = -(formattedText.Height / 2.0);
            }
            else
            {
                xOffset = -(formattedText.Width / 2.0);
                yOffset = this.Placement == TickBarPlacement.Top ? -formattedText.Height - gap : gap;
            }
            dc.DrawText(formattedText, new Point(x + xOffset, y + yOffset));
        }

        /// <summary>Draws the tick marks for a <see cref="T:System.Windows.Controls.Slider" /> control. </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> that is used to draw the ticks.</param>
        protected override void OnRender(DrawingContext dc)
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var size = new Size(this.ActualWidth, this.ActualHeight);
            var numberOfTicks = this.Maximum - this.Minimum;
            var maxFormattedText = new FormattedText(Maximum.ToString(StringFormat), CultureInfo.CurrentUICulture, this.FlowDirection, typeface,
                FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var maxDisplayedLabels = numberOfTicks;
            var tickMaxLength = 0.0;
            var tickOffset = 1.0;
            var tickDirection = 1.0;
            var startPoint = new Point(0.0, 0.0);
            var endPoint = new Point(0.0, 0.0);
            var reservedPadding = this.ReservedSpace * 0.5;
            var textMinPadding = 15;
            switch (this.Placement)
            {
                case TickBarPlacement.Left:
                    if (DoubleUtil.GreaterThanOrClose(this.ReservedSpace, size.Height))
                        return;
                    size.Height -= this.ReservedSpace;
                    tickMaxLength = -TickLength;
                    maxDisplayedLabels = size.Height / (maxFormattedText.Height + textMinPadding);
                    startPoint = new Point(size.Width, size.Height + reservedPadding);
                    endPoint = new Point(size.Width, reservedPadding);
                    tickOffset = size.Height / numberOfTicks * -1.0;
                    tickDirection = -1.0;
                    break;
                case TickBarPlacement.Top:
                    if (DoubleUtil.GreaterThanOrClose(this.ReservedSpace, size.Width))
                        return;
                    size.Width -= this.ReservedSpace;
                    tickMaxLength = -TickLength;
                    maxDisplayedLabels = size.Width / (maxFormattedText.Width + textMinPadding);
                    startPoint = new Point(reservedPadding, size.Height);
                    endPoint = new Point(reservedPadding + size.Width, size.Height);
                    tickOffset = size.Width / numberOfTicks;
                    tickDirection = 1.0;
                    break;
                case TickBarPlacement.Right:
                    if (DoubleUtil.GreaterThanOrClose(this.ReservedSpace, size.Height))
                        return;
                    size.Height -= this.ReservedSpace;
                    tickMaxLength = TickLength;
                    maxDisplayedLabels = size.Height / (maxFormattedText.Height + textMinPadding);
                    startPoint = new Point(0.0, size.Height + reservedPadding);
                    endPoint = new Point(0.0, reservedPadding);
                    tickOffset = size.Height / numberOfTicks * -1.0;
                    tickDirection = -1.0;
                    break;
                case TickBarPlacement.Bottom:
                    if (DoubleUtil.GreaterThanOrClose(this.ReservedSpace, size.Width))
                        return;
                    size.Width -= this.ReservedSpace;
                    tickMaxLength = TickLength;
                    maxDisplayedLabels = size.Width / (maxFormattedText.Width + textMinPadding);
                    startPoint = new Point(reservedPadding, 0.0);
                    endPoint = new Point(reservedPadding + size.Width, 0.0);
                    tickOffset = size.Width / numberOfTicks;
                    tickDirection = 1.0;
                    break;
            }

            var tickLength = tickMaxLength * 0.75;
            if (this.IsDirectionReversed)
            {
                tickDirection = -tickDirection;
                tickOffset *= -1.0;
                var swap = startPoint;
                startPoint = endPoint;
                endPoint = swap;
            }

            var minTickFrequencyText = (this.Maximum - this.Minimum) / maxDisplayedLabels + 1; // +1 to leave a larger gap near the max, rather than overlapping
            if ((this.TickFrequency % 1).Equals(0))
            {
                minTickFrequencyText = (int)Math.Round(minTickFrequencyText, MidpointRounding.AwayFromZero);
            }

            var maxDynamicTick = Maximum - minTickFrequencyText;

            var pen = new Pen(this.Fill, 1.0);
            var snapsToDevicePixels = this.SnapsToDevicePixels;
            var visualXSnappingGuidelines = snapsToDevicePixels ? new DoubleCollection() : null;
            var visualYSnappingGuidelines = snapsToDevicePixels ? new DoubleCollection() : null;
            if (this.Placement == TickBarPlacement.Left || this.Placement == TickBarPlacement.Right)
            {
                var tickFrequency = this.TickFrequency;
                if (tickFrequency > 0.0)
                {
                    var minTickFrequency = (this.Maximum - this.Minimum) / size.Height;
                    if (tickFrequency < minTickFrequency)
                        tickFrequency = minTickFrequency;
                }

                if (tickFrequency < minTickFrequencyText)
                {
                    tickFrequency = minTickFrequencyText;
                }

                dc.DrawLine(pen, startPoint, new Point(startPoint.X + tickMaxLength, startPoint.Y));
                DrawLabel(Minimum, startPoint.X, startPoint.Y, typeface, dc);
                dc.DrawLine(pen, new Point(startPoint.X, endPoint.Y), new Point(startPoint.X + tickMaxLength, endPoint.Y));
                DrawLabel(Maximum, startPoint.X, endPoint.Y, typeface, dc);
                if (snapsToDevicePixels)
                {
                    visualXSnappingGuidelines.Add(startPoint.X);
                    visualYSnappingGuidelines.Add(startPoint.Y - 0.5);
                    visualXSnappingGuidelines.Add(startPoint.X + tickMaxLength);
                    visualYSnappingGuidelines.Add(endPoint.Y - 0.5);
                    visualXSnappingGuidelines.Add(startPoint.X + tickLength);
                }

                if (Ticks != null && Ticks.Count > 0)
                {
                    for (var index = 0; index < Ticks.Count; ++index)
                    {
                        if (!DoubleUtil.LessThanOrClose(Ticks[index], this.Minimum) &&
                            !DoubleUtil.GreaterThanOrClose(Ticks[index], this.Maximum))
                        {
                            var y = (Ticks[index] - this.Minimum) * tickOffset + startPoint.Y;
                            dc.DrawLine(pen, new Point(startPoint.X, y), new Point(startPoint.X + tickLength, y));
                            DrawLabel(Ticks[index], startPoint.X, y, typeface, dc);
                            if (snapsToDevicePixels)
                                visualYSnappingGuidelines.Add(y - 0.5);
                        }
                    }
                }
                else if (tickFrequency > 0.0)
                {
                    for (var i = tickFrequency; i < numberOfTicks && i <= maxDynamicTick; i += tickFrequency)
                    {
                        var y = i * tickOffset + startPoint.Y;
                        dc.DrawLine(pen, new Point(startPoint.X, y), new Point(startPoint.X + tickLength, y));
                        DrawLabel(i + Minimum, startPoint.X, y, typeface, dc);
                        if (snapsToDevicePixels)
                            visualYSnappingGuidelines.Add(y - 0.5);
                    }
                }

                if (this.IsSelectionRangeEnabled)
                {
                    var y1 = (this.SelectionStart - this.Minimum) * tickOffset + startPoint.Y;
                    var point2 = new Point(startPoint.X, y1);
                    var start = new Point(startPoint.X + tickLength, y1);
                    var point3 = new Point(startPoint.X + tickLength, y1 + Math.Abs(tickLength) * tickDirection);
                    var pathSegmentArray1 = new PathSegment[2]
                    {
                        new LineSegment(point3, true),
                        new LineSegment(point2, true)
                    };
                    var pathGeometry1 = new PathGeometry(new PathFigure[1]
                    {
                        new PathFigure(start, pathSegmentArray1, true)
                    });
                    dc.DrawGeometry(this.Fill, pen, pathGeometry1);
                    var y2 = (this.SelectionEnd - this.Minimum) * tickOffset + startPoint.Y;
                    point2 = new Point(startPoint.X, y2);
                    start = new Point(startPoint.X + tickLength, y2);
                    point3 = new Point(startPoint.X + tickLength, y2 - Math.Abs(tickLength) * tickDirection);
                    var pathSegmentArray2 = new PathSegment[2]
                    {
                        new LineSegment(point3, true),
                        new LineSegment(point2, true)
                    };
                    var pathGeometry2 = new PathGeometry(new PathFigure[1]
                    {
                        new PathFigure(start, pathSegmentArray2, true)
                    });
                    dc.DrawGeometry(this.Fill, pen, pathGeometry2);
                }
            }
            else
            {
                var tickFrequency = this.TickFrequency;
                if (tickFrequency > 0.0)
                {
                    var minTickFrequency = (this.Maximum - this.Minimum) / size.Width;
                    if (tickFrequency < minTickFrequency)
                        tickFrequency = minTickFrequency;
                }

                if (tickFrequency < minTickFrequencyText)
                {
                    tickFrequency = minTickFrequencyText;
                }

                dc.DrawLine(pen, startPoint, new Point(startPoint.X, startPoint.Y + tickMaxLength));
                DrawLabel(Minimum, startPoint.X, startPoint.Y, typeface, dc);
                dc.DrawLine(pen, new Point(endPoint.X, startPoint.Y), new Point(endPoint.X, startPoint.Y + tickMaxLength));
                DrawLabel(Maximum, endPoint.X, startPoint.Y, typeface, dc);
                if (snapsToDevicePixels)
                {
                    visualXSnappingGuidelines.Add(startPoint.X - 0.5);
                    visualYSnappingGuidelines.Add(startPoint.Y);
                    visualXSnappingGuidelines.Add(startPoint.X - 0.5);
                    visualYSnappingGuidelines.Add(endPoint.Y + tickMaxLength);
                    visualYSnappingGuidelines.Add(endPoint.Y + tickLength);
                }

                if (Ticks != null && Ticks.Count > 0)
                {
                    for (var index = 0; index < Ticks.Count; ++index)
                    {
                        if (!DoubleUtil.LessThanOrClose(Ticks[index], this.Minimum) &&
                            !DoubleUtil.GreaterThanOrClose(Ticks[index], this.Maximum))
                        {
                            var x = (Ticks[index] - this.Minimum) * tickOffset + startPoint.X;
                            dc.DrawLine(pen, new Point(x, startPoint.Y), new Point(x, startPoint.Y + tickLength));
                            DrawLabel(Ticks[index], x, startPoint.Y, typeface, dc);
                            if (snapsToDevicePixels)
                                visualXSnappingGuidelines.Add(x - 0.5);
                        }
                    }
                }
                else if (tickFrequency > 0.0)
                {
                    for (var i = tickFrequency; i < numberOfTicks && i <= maxDynamicTick; i += tickFrequency)
                    {
                        var x = i * tickOffset + startPoint.X;
                        dc.DrawLine(pen, new Point(x, startPoint.Y), new Point(x, startPoint.Y + tickLength));
                        DrawLabel(i + Minimum, x, startPoint.Y, typeface, dc);
                        if (snapsToDevicePixels)
                            visualXSnappingGuidelines.Add(x - 0.5);
                    }
                }

                if (this.IsSelectionRangeEnabled)
                {
                    var x1 = (this.SelectionStart - this.Minimum) * tickOffset + startPoint.X;
                    var point2 = new Point(x1, startPoint.Y);
                    var start = new Point(x1, startPoint.Y + tickLength);
                    var point3 = new Point(x1 + Math.Abs(tickLength) * tickDirection, startPoint.Y + tickLength);
                    var pathSegmentArray1 = new PathSegment[2]
                    {
                        new LineSegment(point3, true),
                        new LineSegment(point2, true)
                    };
                    var pathGeometry1 = new PathGeometry(new PathFigure[1]
                    {
                        new PathFigure(start, pathSegmentArray1, true)
                    });
                    dc.DrawGeometry(this.Fill, pen, pathGeometry1);
                    var x2 = (this.SelectionEnd - this.Minimum) * tickOffset + startPoint.X;
                    point2 = new Point(x2, startPoint.Y);
                    start = new Point(x2, startPoint.Y + tickLength);
                    point3 = new Point(x2 - Math.Abs(tickLength) * tickDirection, startPoint.Y + tickLength);
                    var pathSegmentArray2 = new PathSegment[2]
                    {
                        new LineSegment(point3, true),
                        new LineSegment(point2, true)
                    };
                    var pathGeometry2 = new PathGeometry(new PathFigure[1]
                    {
                        new PathFigure(start, pathSegmentArray2, true)
                    });
                    dc.DrawGeometry(this.Fill, pen, pathGeometry2);
                }
            }

            if (!snapsToDevicePixels)
                return;
            visualXSnappingGuidelines.Add(this.ActualWidth);
            visualYSnappingGuidelines.Add(this.ActualHeight);
            this.VisualXSnappingGuidelines = visualXSnappingGuidelines;
            this.VisualYSnappingGuidelines = visualYSnappingGuidelines;
        }

        internal static class DoubleUtil
        {
            public static bool AreClose(double value1, double value2)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value1 == value2)
                    return true;
                double num1 = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.22044604925031E-16;
                double num2 = value1 - value2;
                if (-num1 < num2)
                    return num1 > num2;
                return false;
            }

            public static bool LessThanOrClose(double value1, double value2)
            {
                if (value1 >= value2)
                    return DoubleUtil.AreClose(value1, value2);
                return true;
            }

            public static bool GreaterThanOrClose(double value1, double value2)
            {
                if (value1 <= value2)
                    return DoubleUtil.AreClose(value1, value2);
                return true;
            }
        }
    }
}
