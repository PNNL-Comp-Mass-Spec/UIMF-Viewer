using System.Windows.Media;
using ReactiveUI;

namespace UIMF_DataViewer.WpfControls.ColorMapSlider
{
    public class ColorData : ReactiveObject
    {
        private double position;

        public Color Color { get; }
        public int Order { get; }

        public double Position
        {
            get => position;
            set => this.RaiseAndSetIfChanged(ref position, value);
        }

        public Brush Brush { get; }

        public ColorData(Color color, int order, double position = 0)
        {
            Color = color;
            Order = order;
            Position = position;
            Brush = new SolidColorBrush(Color);
        }

        public override string ToString()
        {
            return $"{Color}: {Order}, {Position:F3}";
        }
    }
}
