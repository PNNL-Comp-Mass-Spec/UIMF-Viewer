using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace UIMFViewer.WpfControls.ColorMapSlider
{
    [TemplatePart(Name = "PART_MaxText", Type = typeof(Border))]
    [TemplatePart(Name = "PART_Track", Type = typeof(ColorThumbSlider))]
    [TemplatePart(Name = "PART_Thumbs", Type = typeof(ItemsControl))]
    public class ColorMapSlider : Control
    {
        static ColorMapSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorMapSlider), new FrameworkPropertyMetadata(typeof(ColorMapSlider)));
        }

        public ColorMapSlider()
        {
        }

        private const string MaxTextName = "PART_MaxText";

        internal Border MaxText { get; set; }

        /// <summary>Builds the visual tree for the <see cref="T:System.Windows.Controls.Slider" /> control.</summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MaxText = this.GetTemplateChild("PART_MaxText") as Border;
            if (MaxText != null)
            {
                MaxText.MouseEnter += MaxTextOnMouseEnter;
                MaxText.MouseLeave += MaxTextOnMouseLeave;
            }
        }

        private void MaxTextOnMouseEnter(object sender, MouseEventArgs e)
        {
            MaxMouseHovers = true;
        }

        private void MaxTextOnMouseLeave(object sender, MouseEventArgs e)
        {
            MaxMouseHovers = false;
        }

        public bool MaxMouseHovers
        {
            get { return (bool)GetValue(MaxMouseHoversProperty); }
            set { SetValue(MaxMouseHoversProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxMouseHovers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxMouseHoversProperty =
            DependencyProperty.Register("MaxMouseHovers", typeof(bool), typeof(ColorMapSlider), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public LinearGradientBrush ColorRangeBrush
        {
            get { return (LinearGradientBrush)GetValue(ColorRangeBrushProperty); }
            set { SetValue(ColorRangeBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ColorRangeBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorRangeBrushProperty = DependencyProperty.Register("ColorRangeBrush", typeof(LinearGradientBrush), typeof(ColorMapSlider), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private void GenerateColorRangeBrush()
        {
            if (ColorRangeBrush == null)
            {
                ColorRangeBrush = new LinearGradientBrush();
            }

            if (Orientation == Orientation.Vertical)
            {
                ColorRangeBrush.StartPoint = new Point(0, Minimum);
                ColorRangeBrush.EndPoint = new Point(0, Maximum);
            }
            else
            {
                // TODO: ColorRangeBrush.StartPoint = new Point(Minimum, 0);
                // TODO: ColorRangeBrush.EndPoint = new Point(Maximum, 0);
                ColorRangeBrush.StartPoint = new Point(Maximum, 0);
                ColorRangeBrush.EndPoint = new Point(Minimum, 0);
            }

            if (ColorRangeBrush.GradientStops == null)
            {
                ColorRangeBrush.GradientStops = new GradientStopCollection();
            }
            else
            {
                ColorRangeBrush.GradientStops.Clear();
            }

            ColorRangeBrush.GradientStops.Add(new GradientStop(MinimumColor, Minimum));
            if (Values != null)
            {
                foreach (var value in Values)
                {
                    var gradient = new GradientStop(value.Color, value.Position);
                    var binding = new Binding(nameof(value.Position))
                    {
                        Source = value,
                    };

                    BindingOperations.SetBinding(gradient, GradientStop.OffsetProperty, binding);

                    ColorRangeBrush.GradientStops.Add(gradient);
                }
            }

            ColorRangeBrush.GradientStops.Add(new GradientStop(MaximumColor, Maximum));

            OnPropertyChanged(new DependencyPropertyChangedEventArgs(ColorRangeBrushProperty, ColorRangeBrush, ColorRangeBrush));
        }

        private static void GenerateColorRangeBrush(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ColorMapSlider cms))
            {
                return;
            }

            cms.GenerateColorRangeBrush();
        }

        private static void ValuesChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ColorMapSlider cms))
            {
                return;
            }

            if (e.NewValue != null && e.NewValue is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += cms.OnValuesCollectionChanged;
                foreach (var item in cms.Values)
                {
                    item.PropertyChanged += cms.ColorDataPropertyChanged;
                }
            }
        }

        private void OnValuesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in Values)
            {
                item.PropertyChanged += ColorDataPropertyChanged;
            }
            GenerateColorRangeBrush();
        }

        private void ColorDataPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!(sender is ColorData cd) || !e.PropertyName.Equals(nameof(ColorData.Position)))
            {
                return;
            }

            var ordered = Values.OrderBy(x => x.Order).ToList();
            var index = ordered.IndexOf(cd);
            if (cd.Position < index * MinimumDistance + Minimum)
            {
                cd.Position = index * MinimumDistance + Minimum;
                return;
            }

            if (cd.Position > Maximum - (ordered.Count - index - 1) * MinimumDistance)
            {
                cd.Position = Maximum - (ordered.Count - index - 1) * MinimumDistance;
                return;
            }

            if (index - 1 >= 0)
            {
                // Check lower
                var lower = ordered[index - 1];
                if (lower.Position + MinimumDistance > cd.Position)
                {
                    lower.Position = cd.Position - MinimumDistance;
                    return;
                }
            }

            if (index + 1 < Values.Count)
            {
                // Check higher
                var higher = ordered[index + 1];
                if (higher.Position - MinimumDistance < cd.Position)
                {
                    higher.Position = cd.Position + MinimumDistance;
                    return;
                }
            }
        }

        public IList<ColorData> Values
        {
            get { return (IList<ColorData>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Values.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register("Values", typeof(IList<ColorData>), typeof(ColorMapSlider), new FrameworkPropertyMetadata(new List<ColorData>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ValuesChangedCallback));

        public double MinimumDistance
        {
            get { return (double)GetValue(MinimumDistanceProperty); }
            set { SetValue(MinimumDistanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinimumDistance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumDistanceProperty = DependencyProperty.Register("MinimumDistance", typeof(double), typeof(ColorMapSlider), new PropertyMetadata(0.02));

        public Color MinimumColor
        {
            get { return (Color)GetValue(MinimumColorProperty); }
            set { SetValue(MinimumColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinimumColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumColorProperty = DependencyProperty.Register("MinimumColor", typeof(Color), typeof(ColorMapSlider), new PropertyMetadata(Colors.White, GenerateColorRangeBrush));

        public Color MaximumColor
        {
            get { return (Color)GetValue(MaximumColorProperty); }
            set { SetValue(MaximumColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaximumColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumColorProperty = DependencyProperty.Register("MaximumColor", typeof(Color), typeof(ColorMapSlider), new PropertyMetadata(Colors.Black, GenerateColorRangeBrush));

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(ColorMapSlider), new PropertyMetadata(0.0, GenerateColorRangeBrush));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(ColorMapSlider), new PropertyMetadata(1.0, GenerateColorRangeBrush));

        public bool IsSnapToTickEnabled
        {
            get { return (bool)GetValue(IsSnapToTickEnabledProperty); }
            set { SetValue(IsSnapToTickEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSnapToTickEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSnapToTickEnabledProperty = DependencyProperty.Register("IsSnapToTickEnabled", typeof(bool), typeof(ColorMapSlider), new PropertyMetadata(false));

        public double TickFrequency
        {
            get { return (double)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TickFrequency.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register("TickFrequency", typeof(double), typeof(ColorMapSlider), new PropertyMetadata(0.1));

        public TickPlacement TickPlacement
        {
            get { return (TickPlacement)GetValue(TickPlacementProperty); }
            set { SetValue(TickPlacementProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TickPlacement.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TickPlacementProperty = DependencyProperty.Register("TickPlacement", typeof(TickPlacement), typeof(ColorMapSlider), new FrameworkPropertyMetadata(TickPlacement.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public DoubleCollection Ticks
        {
            get { return (DoubleCollection)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Ticks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TicksProperty = DependencyProperty.Register("Ticks", typeof(DoubleCollection), typeof(ColorMapSlider), new PropertyMetadata(null));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ColorMapSlider), new PropertyMetadata(Orientation.Vertical, GenerateColorRangeBrush));
    }
}
