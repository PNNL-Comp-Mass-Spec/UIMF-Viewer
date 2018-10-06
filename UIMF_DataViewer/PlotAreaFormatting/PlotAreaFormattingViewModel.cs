using System;
using System.Reactive;
using System.Reactive.Concurrency;
using ReactiveUI;

namespace UIMF_DataViewer.PlotAreaFormatting
{
    public class PlotAreaFormattingViewModel : ReactiveObject
    {
        private double thresholdSliderValue = 1;
        private double backgroundGrayValue = 30;
        public ColorMapSliderViewModel ColorMap { get; } = new ColorMapSliderViewModel();

        public double ThresholdSliderValue
        {
            get => thresholdSliderValue;
            set => this.RaiseAndSetIfChanged(ref thresholdSliderValue, value);
        }

        public double BackgroundGrayValue
        {
            get => backgroundGrayValue;
            set => this.RaiseAndSetIfChanged(ref backgroundGrayValue, value);
        }

        public ReactiveCommand<Unit, Unit> ResetCommand { get; }

        public PlotAreaFormattingViewModel()
        {
            ResetCommand = ReactiveCommand.Create(Reset);
        }

        public event EventHandler ValuesReset;

        public void Reset()
        {
            ColorMap.Reset();
            ThresholdSliderValue = 1;
            BackgroundGrayValue = 30;

            ValuesReset?.Invoke(this, EventArgs.Empty);
        }

        public void SafeReset()
        {
            RxApp.MainThreadScheduler.Schedule(Reset);
        }
    }
}
