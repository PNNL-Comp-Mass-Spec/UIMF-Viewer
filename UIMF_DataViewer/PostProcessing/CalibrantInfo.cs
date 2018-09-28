using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using ReactiveUI;

namespace UIMF_DataViewer.PostProcessing
{
    public class CalibrantInfo : ReactiveObject, IEquatable<CalibrantInfo>
    {
        private bool enabled;
        private string name;
        private double mz;
        private int charge;
        private double tof;
        private double bins;
        private double errorPpm;
        private double mzExperimental;
        private double tofExperimental;
        private bool notFound;

        public bool Enabled
        {
            get => enabled;
            set => this.RaiseAndSetIfChanged(ref enabled, value);
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public double Mz
        {
            get => mz;
            set => this.RaiseAndSetIfChanged(ref mz, value);
        }

        public int Charge
        {
            get => charge;
            set => this.RaiseAndSetIfChanged(ref charge, value);
        }

        public double TOF
        {
            get => tof;
            set => this.RaiseAndSetIfChanged(ref tof, value);
        }

        public double Bins
        {
            get => bins;
            set => this.RaiseAndSetIfChanged(ref bins, value);
        }

        public double ErrorPPM
        {
            get => errorPpm;
            set => this.RaiseAndSetIfChanged(ref errorPpm, value);
        }

        public double MzExperimental
        {
            get => mzExperimental;
            set => this.RaiseAndSetIfChanged(ref mzExperimental, value);
        }

        public double TOFExperimental
        {
            get => tofExperimental;
            set => this.RaiseAndSetIfChanged(ref tofExperimental, value);
        }

        public bool NotFound
        {
            get => notFound;
            set => this.RaiseAndSetIfChanged(ref notFound, value);
        }

        public CalibrantInfo() { }

        public CalibrantInfo(string name, double mz, int charge)
        {
            Name = name;
            Mz = mz;
            Charge = charge;
        }

        public bool Equals(CalibrantInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(name, other.name) && mz.Equals(other.mz) && charge == other.charge;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CalibrantInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ mz.GetHashCode();
                hashCode = (hashCode * 397) ^ charge;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Name} {Charge}+: {Mz:F4}";
        }

        public static IEnumerable<CalibrantInfo> GetDefaultCalibrants()
        {
            // agilent tune mix
            yield return new CalibrantInfo(mz: 622.02896, name: "agilent_tune_1", charge: 1);
            yield return new CalibrantInfo(mz: 922.009798, name: "agilent_tune_2", charge: 1);
            yield return new CalibrantInfo(mz: 1221.990637, name: "agilent_tune_3", charge: 1);
            yield return new CalibrantInfo(mz: 1521.971475, name: "agilent_tune_4", charge: 1);
            yield return new CalibrantInfo(mz: 1821.952313, name: "agilent_tune_5", charge: 1);

            // Angiotensin_I
            yield return new CalibrantInfo(mz: 648.845996, name: "Angiotensin_I", charge: 2);
            yield return new CalibrantInfo(mz: 432.89975, name: "Angiotensin_I", charge: 3);

            // Bradykinin
            yield return new CalibrantInfo(mz: 530.78795, name: "Bradykinin", charge: 2);
            yield return new CalibrantInfo(mz: 354.1943928, name: "Bradykinin", charge: 3);

            // Neurotensin
            yield return new CalibrantInfo(mz: 836.962074, name: "Neurotensin", charge: 2);
            yield return new CalibrantInfo(mz: 558.310475, name: "Neurotensin", charge: 3);

            // Fibrinopeptide
            yield return new CalibrantInfo(mz: 768.8498483, name: "Fibrinopeptide_A", charge: 2);
            yield return new CalibrantInfo(mz: 512.90229, name: "Fibrinopeptide_A", charge: 3);

            // Renin
            yield return new CalibrantInfo(mz: 1025.556667, name: "Renin", charge: 1);
            yield return new CalibrantInfo(mz: 513.281968, name: "Renin", charge: 2);

            yield return new CalibrantInfo(mz: 674.37132, name: "Substance_P", charge: 2);

            // bsa
            yield return new CalibrantInfo(mz: 820.472489, name: "KVPQVSTPTLVEVSR", charge: 2);
            yield return new CalibrantInfo(mz: 547.317418, name: "KVPQVSTPTLVEVSR", charge: 3);
            yield return new CalibrantInfo(mz: 571.860788, name: "KQTALVELLK", charge: 2);
            yield return new CalibrantInfo(mz: 653.361684, name: "HLVDEPQNLIK", charge: 2);
            yield return new CalibrantInfo(mz: 480.6087469, name: "RHPEYAVSVLLR", charge: 3);
            yield return new CalibrantInfo(mz: 417.211886, name: "FKDLGEEHFK", charge: 3);
            yield return new CalibrantInfo(mz: 363.007718, name: "LCVLHEKTPVSEKVTK", charge: 5);
            yield return new CalibrantInfo(mz: 454.895578, name: "SLHTLFGDELCK", charge: 3);
            yield return new CalibrantInfo(mz: 693.813909, name: "YICDNQDTISSK", charge: 2);
        }
    }
}
