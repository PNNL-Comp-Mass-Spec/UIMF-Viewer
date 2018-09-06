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
            SetActualsBindings();
        }

        // TODO: Add a 'StringFormatter' option?
        public string StringFormat
        {
            get => (string)GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        /// <summary>
        /// IsLogarithmicScale: if true, use <see cref="ActualValue"/>, <see cref="ActualMinimum"/>, and <see cref="ActualMaximum"/> to access those values appropriately
        /// </summary>
        public bool IsLogarithmicScale
        {
            get { return (bool)GetValue(IsLogarithmicScaleProperty); }
            set { SetValue(IsLogarithmicScaleProperty, value); }
        }

        /// <summary>
        /// ActualValue property - Value, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public double ActualValue
        {
            get { return (double)GetValue(ActualValueProperty); }
            set { SetValue(ActualValueProperty, value); }
        }

        /// <summary>
        /// ActualMinimum property - Minimum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public double ActualMinimum
        {
            get { return (double)GetValue(ActualMinimumProperty); }
            set { SetValue(ActualMinimumProperty, value); }
        }

        /// <summary>
        /// ActualMaximum property - Maximum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public double ActualMaximum
        {
            get { return (double)GetValue(ActualMaximumProperty); }
            set { SetValue(ActualMaximumProperty, value); }
        }

        private void SetActualsBindings()
        {
            var actualValueBinding = new Binding("ActualValue")
            {
                Source = this
            };
            var actualMinimumBinding = new Binding("ActualMinimum")
            {
                Source = this
            };
            var actualMaximumBinding = new Binding("ActualMaximum")
            {
                Source = this
            };

            if (IsLogarithmicScale)
            {
                actualValueBinding.Converter = new LogConverter();
                actualMinimumBinding.Converter = new LogConverter();
                actualMaximumBinding.Converter = new LogConverter();
            }

            SetBinding(ValueProperty, actualValueBinding);
            SetBinding(MinimumProperty, actualMinimumBinding);
            SetBinding(MaximumProperty, actualMaximumBinding);
        }

        private bool defaultLogTicks = false;

        private void SetDefaultTicks(string propertyName)
        {
            if (IsLogarithmicScale)
            {
                if (Ticks == null || Ticks.Count == 0 || defaultLogTicks)
                {
                    // Minimum and Maximum: add/subtract 0.5 to guarantee we get a value inside the range
                    var minInt = (int)Math.Round(Minimum + 0.5, MidpointRounding.AwayFromZero);
                    var maxInt = (int)Math.Round(Maximum - 0.5, MidpointRounding.AwayFromZero);
                    Ticks = new DoubleCollection(Enumerable.Range(minInt, maxInt - minInt + 1).Select(x => (double)x));
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
        public static readonly DependencyProperty ActualValueProperty =
            DependencyProperty.Register("ActualValue", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// ActualMinimum property - Minimum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public static readonly DependencyProperty ActualMinimumProperty =
            DependencyProperty.Register("ActualMinimum", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, LogarithmicScalePropertyChangedCallback));

        /// <summary>
        /// ActualMaximum property - Maximum, wrapped by a ValueConverter when <see cref="IsLogarithmicScale"/> is true.
        /// </summary>
        public static readonly DependencyProperty ActualMaximumProperty =
            DependencyProperty.Register("ActualMaximum", typeof(double), typeof(SliderLabeled), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, LogarithmicScalePropertyChangedCallback));

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
                    sl.SetActualsBindings();
                    sl.SetDefaultTicks(e.Property.Name);
                    if (StringFormatProperty.DefaultMetadata.DefaultValue.Equals(sl.StringFormat))
                    {
                        sl.StringFormat = DefaultLogScaleFormatString;
                    }
                }
                else
                {
                    sl.SetActualsBindings();
                    sl.SetDefaultTicks(e.Property.Name);
                    if (DefaultLogScaleFormatString.Equals(sl.StringFormat))
                    {
                        sl.StringFormat = (string)StringFormatProperty.DefaultMetadata.DefaultValue;
                    }
                }
            }
            else if (e.Property.Name.Equals(nameof(Minimum)) || e.Property.Name.Equals(nameof(Maximum)) || e.Property.Name.Equals(nameof(ActualMinimum)) || e.Property.Name.Equals(nameof(ActualMaximum)))
            {
                sl.SetDefaultTicks(e.Property.Name);
            }
        }
    }
}
