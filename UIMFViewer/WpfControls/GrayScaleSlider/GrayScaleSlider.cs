using System.Windows;
using System.Windows.Controls;

namespace UIMFViewer.WpfControls.GrayScaleSlider
{
    public class GrayScaleSlider : Slider
    {
        static GrayScaleSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GrayScaleSlider), new FrameworkPropertyMetadata(typeof(GrayScaleSlider)));
        }

        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StringFormat.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", typeof(string), typeof(GrayScaleSlider), new PropertyMetadata(""));
        public static readonly DependencyProperty StringFormatProperty = ValueThumb.StringFormatProperty.AddOwner(typeof(GrayScaleSlider));
    }
}
