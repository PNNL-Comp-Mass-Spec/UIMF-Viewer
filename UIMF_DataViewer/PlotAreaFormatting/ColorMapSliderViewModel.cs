using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ReactiveUI;
using UIMF_DataViewer.WpfControls.ColorMapSlider;

namespace UIMF_DataViewer.PlotAreaFormatting
{
    public class ColorMapSliderViewModel : ReactiveObject
    {
        private readonly IReadOnlyList<double> minsDefault = new double[] { 0.4, 0.75, 0.86, 0.91, 0.975, 0.995 };

        private readonly Color[] colors = new Color[]
            { Colors.Red, Colors.Yellow, Colors.GreenYellow, Colors.Lime, Colors.SkyBlue, Colors.Blue };

        private bool showMaxIntensity;
        private readonly ReactiveList<ColorData> colorPositions;

        public IReadOnlyReactiveList<ColorData> ColorPositions => colorPositions;

        public Color MinColor => Colors.Purple;
        public Color MaxColor => Colors.DarkBlue;
        public double MinPosition => 0.0;
        public double MaxPosition => 1.0;

        private readonly ColorData minColorData;
        private readonly ColorData maxColorData;

        public bool ShowMaxIntensity
        {
            get => showMaxIntensity;
            set => this.RaiseAndSetIfChanged(ref showMaxIntensity, value);
        }

        public ColorMapSliderViewModel()
        {
            colorPositions = new ReactiveList<ColorData>(colors.Select((x, i) => new ColorData(x, i + 1)));
            foreach (var item in colorPositions)
            {
                item.PropertyChanged += ItemOnPropertyChanged;
            }

            minColorData = new ColorData(MinColor, int.MinValue, MinPosition);
            maxColorData = new ColorData(MaxColor, int.MaxValue, MaxPosition);

            Reset();
        }

        public void Reset()
        {
            for (var i = 0; i < minsDefault.Count; i++)
            {
                ColorPositions[i].Position = minsDefault[i];
            }
        }

        public event EventHandler ColorPositionChanged;

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ColorData.Position)))
            {
                ColorPositionChanged?.Invoke(sender, e);
            }
        }

        public Color GetColorForIntensity(double intensity)
        {
            var less1 = minColorData;
            var current = maxColorData;
            var found = false;
            foreach (var item in ColorPositions)
            {
                current = item;
                if ((1.0 - intensity) <= current.Position)
                {
                    found = true;
                    break;
                }

                less1 = current;
            }

            if (!found)
            {
                current = maxColorData;
            }

            if ((1.0 - intensity) <= current.Position)
            {
                var interp = ((1.0 - intensity) - less1.Position) / (current.Position - less1.Position);

                var red = (int)(((current.Color.R - less1.Color.R)) * interp) + less1.Color.R;
                var green = (int)(((current.Color.G - less1.Color.G) * interp)) + less1.Color.G;
                var blue = (int)(((current.Color.B - less1.Color.B) * interp)) + less1.Color.B;

                return Color.FromRgb((byte)red, (byte)green, (byte)blue);
            }

            // should never get here.
            MessageBox.Show("Pixel Failure in ION_ColorMap.cs:  " + intensity.ToString("0.000"));
            return Color.FromRgb(0, 0xFF, 0);
        }
    }
}
