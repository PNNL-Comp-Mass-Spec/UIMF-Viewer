using System.Windows;
using System.Windows.Controls.Primitives;

namespace UIMF_DataViewer.WpfControls.GrayScaleSlider
{
    public class ValueThumb : Thumb
    {
        static ValueThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValueThumb), new FrameworkPropertyMetadata(typeof(ValueThumb)));
        }

        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StringFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register("StringFormat", typeof(string), typeof(ValueThumb), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.Inherits));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(ValueThumb), new PropertyMetadata(0.0));
    }
}
