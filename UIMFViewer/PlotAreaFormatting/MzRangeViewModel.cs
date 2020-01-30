using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;

namespace UIMFViewer.PlotAreaFormatting
{
    public class MzRangeViewModel : ReactiveObject
    {
        private bool rangeEnabled;
        private double mz;
        private double tolerance;
        private ToleranceUnit toleranceType;
        private readonly ObservableAsPropertyHelper<double> toleranceIncrement;
        private readonly ObservableAsPropertyHelper<double> toleranceMinimum;

        public MzRangeViewModel()
        {
            RangeEnabled = false;
            Mz = 1000;
            Tolerance = 150;
            ToleranceType = ToleranceUnit.PPM;
            toleranceIncrement = this.WhenAnyValue(x => x.ToleranceType).Select(x => x == ToleranceUnit.PPM ? 10 : 0.2).ToProperty(this, x => x.ToleranceIncrement);
            toleranceMinimum = this.WhenAnyValue(x => x.ToleranceType).Select(x => x == ToleranceUnit.PPM ? 1 : 0.001).ToProperty(this, x => x.ToleranceMinimum);
            ToleranceUnits = new ReadOnlyObservableCollection<ToleranceUnit>(new ObservableCollection<ToleranceUnit>(Enum.GetValues(typeof(ToleranceUnit)).Cast<ToleranceUnit>()));
        }

        public ReadOnlyObservableCollection<ToleranceUnit> ToleranceUnits { get; }

        public bool RangeEnabled
        {
            get => rangeEnabled;
            set => this.RaiseAndSetIfChanged(ref rangeEnabled, value);
        }

        public double Mz
        {
            get => mz;
            set => this.RaiseAndSetIfChanged(ref mz, value);
        }

        public double Tolerance
        {
            get => tolerance;
            set => this.RaiseAndSetIfChanged(ref tolerance, value);
        }

        public double ToleranceIncrement => toleranceIncrement.Value;
        public double ToleranceMinimum => toleranceMinimum.Value;

        public ToleranceUnit ToleranceType
        {
            get => toleranceType;
            set => this.RaiseAndSetIfChanged(ref toleranceType, value);
        }

        public double ComputedTolerance
        {
            get
            {
                if (ToleranceType == ToleranceUnit.Mz)
                {
                    return Tolerance;
                }

                return Mz * Tolerance / 1e6;
            }
        }
    }

    public enum ToleranceUnit
    {
        [Description("m/z")]
        Mz,

        [Description("ppm")]
        PPM
    }
}
