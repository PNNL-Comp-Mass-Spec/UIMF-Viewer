using System.Windows;
using System.Windows.Controls;

namespace UIMF_DataViewer.WpfControls
{
    public class SliderLabeled : Slider
    {
        static SliderLabeled()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderLabeled), new FrameworkPropertyMetadata(typeof(SliderLabeled)));
        }

        public string StringFormat
        {
            get => (string)GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        // Using a DependencyProperty as the backing store for StringFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StringFormatProperty = TickBarLabeled.StringFormatProperty.AddOwner(typeof(SliderLabeled));

        //public Brush TickFill
        //{
        //    get => (Brush)GetValue(TickFillProperty);
        //    set => SetValue(TickFillProperty, value);
        //}
        //
        //// Using a DependencyProperty as the backing store for TickFill.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty TickFillProperty = DependencyProperty.Register("TickFill", typeof(Brush), typeof(SliderLabeled), TickBar.FillProperty.DefaultMetadata);
    }
}
