using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace UIMFViewer.WpfControls.ColorMapSlider
{
    public class ColorThumb : Thumb
    {
        static ColorThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumb), new FrameworkPropertyMetadata(typeof(ColorThumb)));
        }

        public Brush ThumbFill
        {
            get { return (Brush)GetValue(ThumbFillProperty); }
            set { SetValue(ThumbFillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThumbFill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThumbFillProperty =
            DependencyProperty.Register("ThumbFill", typeof(Brush), typeof(ColorThumb), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
    }
}
