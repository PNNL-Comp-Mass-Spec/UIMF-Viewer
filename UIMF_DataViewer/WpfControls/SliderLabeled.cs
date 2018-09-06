using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace UIMF_DataViewer.WpfControls
{
    public class SliderLabeled : Slider
    {
        static SliderLabeled()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderLabeled), new FrameworkPropertyMetadata(typeof(SliderLabeled)));
            MinimumProperty.OverrideMetadata(typeof(SliderLabeled), new FrameworkPropertyMetadata(LogarithmicScalePropertyChangedCallback));
            MaximumProperty.OverrideMetadata(typeof(SliderLabeled), new FrameworkPropertyMetadata(LogarithmicScalePropertyChangedCallback));
        }

        private const string DefaultLogScaleFormatString = @"\1\e0";

        public SliderLabeled()
        {
            SetWrapperBindings();
        }

        // TODO: Add a 'StringFormatter' option?
        public string StringFormat
        {
            get => (string)GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        /// <summary>
        /// IsLogarithmicScale: if true, <see cref="Value"/>, <see cref="Minimum"/>, and <see cref="Maximum"/> are wrapped with a value conversion
        /// </summary>
        public bool IsLogarithmicScale
        {
            get { return (bool)GetValue(IsLogarithmicScaleProperty); }
            set { SetValue(IsLogarithmicScaleProperty, value); }
        }

        /// <summary>
        /// Value property - Value, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public new double Value
        {
            get { return (double)GetValue(WrappedValueProperty); }
            set { SetValue(WrappedValueProperty, value); }
        }

        /// <summary>
        /// Minimum property - Minimum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public new double Minimum
        {
            get { return (double)GetValue(WrappedMinimumProperty); }
            set { SetValue(WrappedMinimumProperty, value); }
        }

        /// <summary>
        /// Maximum property - Maximum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public new double Maximum
        {
            get { return (double)GetValue(WrappedMaximumProperty); }
            set { SetValue(WrappedMaximumProperty, value); }
        }

        private void SetWrapperBindings()
        {
            var wrappedValueBinding = new Binding(nameof(Value))
            {
                Source = this,
                Mode = BindingMode.TwoWay
            };
            var wrappedMinimumBinding = new Binding(nameof(Minimum))
            {
                Source = this,
                Mode = BindingMode.TwoWay
            };
            var wrappedMaximumBinding = new Binding(nameof(Maximum))
            {
                Source = this,
                Mode = BindingMode.TwoWay
            };

            if (IsLogarithmicScale)
            {
                wrappedValueBinding.Converter = new LogConverter();
                wrappedMinimumBinding.Converter = new LogConverter();
                wrappedMaximumBinding.Converter = new LogConverter();
            }

            SetBinding(ValueProperty, wrappedValueBinding);
            SetBinding(MinimumProperty, wrappedMinimumBinding);
            SetBinding(MaximumProperty, wrappedMaximumBinding);
        }

        private bool defaultLogTicks = false;

        private void SetDefaultTicks(string propertyName)
        {
            if (IsLogarithmicScale)
            {
                if (Ticks == null || Ticks.Count == 0 || defaultLogTicks)
                {
                    // Minimum and Maximum: add/subtract 0.5 to guarantee we get a value inside the range
                    var minInt = (int)Math.Round(base.Minimum - 0.5, MidpointRounding.AwayFromZero);
                    var maxInt = (int)Math.Round(base.Maximum + 0.5, MidpointRounding.AwayFromZero);
                    var count = Math.Max(maxInt - minInt + 1, 0);
                    Ticks = new DoubleCollection(Enumerable.Range(minInt, count).Select(x => (double)x));
                    defaultLogTicks = true;
                }
            }
            else if (defaultLogTicks && nameof(IsLogarithmicScale).Equals(propertyName))
            {
                Ticks = null;
                defaultLogTicks = false;
            }
        }

        private class LogConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double d)
                {
                    return Math.Log10(d);
                }

                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double d)
                {
                    return Math.Pow(10, d);
                }

                return 0;
            }
        }

        /// <summary>
        /// StringFormat
        /// </summary>
        //public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", typeof(string), typeof(SliderLabeled), new FrameworkPropertyMetadata("F0", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty StringFormatProperty = TickBarLabeled.StringFormatProperty.AddOwner(typeof(SliderLabeled));

        /// <summary>
        /// IsLogarithmicScale: if true, use <see cref="ActualValue"/>, <see cref="ActualMinimum"/>, and <see cref="ActualMaximum"/> to access those values appropriately
        /// </summary>
        public static readonly DependencyProperty IsLogarithmicScaleProperty =
            DependencyProperty.Register("IsLogarithmicScale", typeof(bool), typeof(SliderLabeled), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, LogarithmicScalePropertyChangedCallback));

        /// <summary>
        /// ActualValue property - Value, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public static readonly DependencyProperty WrappedValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, LogarithmicScalePropertyChangedCallback));

        /// <summary>
        /// ActualMinimum property - Minimum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public static readonly DependencyProperty WrappedMinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, LogarithmicScalePropertyChangedCallback));

        /// <summary>
        /// ActualMaximum property - Maximum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public static readonly DependencyProperty WrappedMaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, LogarithmicScalePropertyChangedCallback));

        //public Brush TickFill
        //{
        //    get => (Brush)GetValue(TickFillProperty);
        //    set => SetValue(TickFillProperty, value);
        //}
        //
        //// Using a DependencyProperty as the backing store for TickFill.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty TickFillProperty = DependencyProperty.Register("TickFill", typeof(Brush), typeof(SliderLabeled), TickBar.FillProperty.DefaultMetadata);

        private static void LogarithmicScalePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is SliderLabeled sl))
            {
                return;
            }

            if (e.Property.Name.Equals(nameof(IsLogarithmicScale)))
            {
                if ((bool)e.NewValue)
                {
                    sl.SetWrapperBindings();
                    sl.SetDefaultTicks(e.Property.Name);
                    if (StringFormatProperty.DefaultMetadata.DefaultValue.Equals(sl.StringFormat))
                    {
                        sl.StringFormat = DefaultLogScaleFormatString;
                    }
                }
                else
                {
                    sl.SetWrapperBindings();
                    sl.SetDefaultTicks(e.Property.Name);
                    if (DefaultLogScaleFormatString.Equals(sl.StringFormat))
                    {
                        sl.StringFormat = (string)StringFormatProperty.DefaultMetadata.DefaultValue;
                    }
                }
            }
            else if (e.Property.Name.Equals(nameof(Minimum)) || e.Property.Name.Equals(nameof(Maximum)))
            {
                sl.SetDefaultTicks(e.Property.Name);
            }
        }

        /// <summary>Raises the <see cref="SliderLabeled.ValueChanged" /> routed event. </summary>
        /// <param name="oldValue">Old value of the <see cref="SliderLabeled.Value" /> property</param>
        /// <param name="newValue">New value of the <see cref="SliderLabeled.Value" /> property</param>
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            if (IsLogarithmicScale)
            {
                oldValue = Math.Pow(10, oldValue);
                newValue = Math.Pow(10, newValue);
            }
            base.OnValueChanged(oldValue, newValue);
        }
    }
}
