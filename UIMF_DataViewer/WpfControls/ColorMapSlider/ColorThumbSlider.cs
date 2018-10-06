using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UIMF_DataViewer.WpfControls.ColorMapSlider
{
    public class ColorThumbSlider : Slider
    {
        static ColorThumbSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumbSlider), new FrameworkPropertyMetadata(typeof(ColorThumbSlider)));
        }

        public Brush ThumbFill
        {
            get { return (Brush)GetValue(ThumbFillProperty); }
            set { SetValue(ThumbFillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThumbFill.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty ThumbFillProperty = DependencyProperty.Register("ThumbFill", typeof(Brush), typeof(ColorThumbSlider), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty ThumbFillProperty = ColorThumb.ThumbFillProperty.AddOwner(typeof(ColorThumbSlider));
    }
}
