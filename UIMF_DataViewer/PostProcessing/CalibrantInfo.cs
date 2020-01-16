using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using ReactiveUI;

namespace UIMF_DataViewer.PostProcessing
{
    public class CalibrantInfo : ReactiveObject, IEquatable<CalibrantInfo>
    {
        private bool enabled;
        private string name;
        private double mz;
        private int charge;
        private double driftTubeCcs;
        private string formula;
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

        public double DriftTubeCCS
        {
            get => driftTubeCcs;
            set => this.RaiseAndSetIfChanged(ref driftTubeCcs, value);
        }

        public string Formula
        {
            get => formula;
            set => this.RaiseAndSetIfChanged(ref formula, value);
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

        public static List<CalibrantInfo> GetCalibrantSet(CalibrantSet calibrantSet, string customPath = null)
        {
            if ((uint) calibrantSet == 0)
            {
                calibrantSet = CalibrantSet.All;
            }

            var data = new List<CalibrantInfo>();
            // CalibrantSet.All: automatically handled.
            if (calibrantSet.HasFlag(CalibrantSet.AgilentTuneMixPositive))
            {
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos118", Mz = 118.086255, Charge = 1, DriftTubeCCS = 121.3 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos322", Mz = 322.048121, Charge = 1, DriftTubeCCS = 153.73 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos622", Mz = 622.02896, Charge = 1, DriftTubeCCS = 202.96 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos922", Mz = 922.009798, Charge = 1, DriftTubeCCS = 243.64 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos1222", Mz = 1221.990637, Charge = 1, DriftTubeCCS = 282.2 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos1522", Mz = 1521.971476, Charge = 1, DriftTubeCCS = 316.96 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos1822", Mz = 1821.952313, Charge = 1, DriftTubeCCS = 351.25 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos2122", Mz = 2121.933152, Charge = 1, DriftTubeCCS = 383.03 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos2422", Mz = 2421.91399, Charge = 1, DriftTubeCCS = 412.96 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Pos2722", Mz = 2721.894829, Charge = 1, DriftTubeCCS = 441.21 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.AgilentTuneMixNegative))
            {
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg302", Mz = 301.998139, Charge = 1, DriftTubeCCS = 140.04 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg602", Mz = 601.978977, Charge = 1, DriftTubeCCS = 180.77 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg1034", Mz = 1033.988109, Charge = 1, DriftTubeCCS = 255.34 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg1334", Mz = 1333.968947, Charge = 1, DriftTubeCCS = 284.76 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg1634", Mz = 1633.949786, Charge = 1, DriftTubeCCS = 319.03 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg1934", Mz = 1933.930624, Charge = 1, DriftTubeCCS = 352.55 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg2234", Mz = 2233.911463, Charge = 1, DriftTubeCCS = 380.74 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg2534", Mz = 2533.892301, Charge = 1, DriftTubeCCS = 412.99 });
                data.Add(new CalibrantInfo { Name = "Agilent Tune Neg2834", Mz = 2833.873139, Charge = 1, DriftTubeCCS = 432.62 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.HumanPeptides))
            {
                // Angiotensin_I
                data.Add(new CalibrantInfo { Name = "Angiotensin_I", Mz = 648.845996, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Angiotensin_I", Mz = 432.89975, Charge = 3, DriftTubeCCS = 0 });

                // Bradykinin
                data.Add(new CalibrantInfo { Name = "Bradykinin", Mz = 530.78795, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Bradykinin", Mz = 354.1943928, Charge = 3, DriftTubeCCS = 0 });

                // Neurotensin
                data.Add(new CalibrantInfo { Name = "Neurotensin", Mz = 836.962074, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Neurotensin", Mz = 558.310475, Charge = 3, DriftTubeCCS = 0 });

                // Fibrinopeptide
                data.Add(new CalibrantInfo { Name = "Fibrinopeptide_A", Mz = 768.8498483, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Fibrinopeptide_A", Mz = 512.90229, Charge = 3, DriftTubeCCS = 0 });

                // Renin
                data.Add(new CalibrantInfo { Name = "Renin", Mz = 1025.556667, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Renin", Mz = 513.281968, Charge = 2, DriftTubeCCS = 0 });

                data.Add(new CalibrantInfo { Name = "Substance_P", Mz = 674.37132, Charge = 2, DriftTubeCCS = 0 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.BSAPeptides))
            {
                data.Add(new CalibrantInfo { Name = "KVPQVSTPTLVEVSR", Mz = 820.472489, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "KVPQVSTPTLVEVSR", Mz = 547.317418, Charge = 3, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "KQTALVELLK", Mz = 571.860788, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "HLVDEPQNLIK", Mz = 653.361684, Charge = 2, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "RHPEYAVSVLLR", Mz = 480.6087469, Charge = 3, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "FKDLGEEHFK", Mz = 417.211886, Charge = 3, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "LCVLHEKTPVSEKVTK", Mz = 363.007718, Charge = 5, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "SLHTLFGDELCK", Mz = 454.895578, Charge = 3, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "YICDNQDTISSK", Mz = 693.813909, Charge = 2, DriftTubeCCS = 0 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.QuaternaryAminesPositive))
            {
                data.Add(new CalibrantInfo { Name = "Tetramethylammonium", Formula = "C4H12N", Mz = 74.1, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "Tetraethylammonium", Formula = "C8H20N", Mz = 130.1597, Charge = 1, DriftTubeCCS = 126.8333333 });
                data.Add(new CalibrantInfo { Name = "Tetrapropylammonium", Formula = "C12H28N", Mz = 186.2221628, Charge = 1, DriftTubeCCS = 148.1 });
                data.Add(new CalibrantInfo { Name = "Tetrabutylammonium", Formula = "C16H36N", Mz = 242.2847596, Charge = 1, DriftTubeCCS = 169.4333333 });
                data.Add(new CalibrantInfo { Name = "Tetrapentylammonium", Formula = "C20H44N", Mz = 298.3473564, Charge = 1, DriftTubeCCS = 192.9333333 });
                data.Add(new CalibrantInfo { Name = "Tetrahexylammonium", Formula = "C24H52N", Mz = 354.4099532, Charge = 1, DriftTubeCCS = 215.7 });
                data.Add(new CalibrantInfo { Name = "Tetraheptylammonium", Formula = "C28H60N", Mz = 410.47255, Charge = 1, DriftTubeCCS = 238.9333333 });
                data.Add(new CalibrantInfo { Name = "Tetraoctylammonium", Formula = "C32H68N", Mz = 466.5351468, Charge = 1, DriftTubeCCS = 0 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.PolyalaninesNegative))
            {
                data.Add(new CalibrantInfo { Name = "PolyAlanine_159", Formula = "", Mz = 159.0769636, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "PolyAlanine_372", Formula = "", Mz = 372.1882954, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "PolyAlanine_443", Formula = "", Mz = 443.2254064, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "PolyAlanine_514", Formula = "", Mz = 514.2625174, Charge = 1, DriftTubeCCS = 0 });
                data.Add(new CalibrantInfo { Name = "PolyAlanine_585", Formula = "", Mz = 585.2996284, Charge = 1, DriftTubeCCS = 0 });
            }

            if (calibrantSet.HasFlag(CalibrantSet.Custom) && !string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                // TODO: exception handling
                data.AddRange(ReadFromFile(customPath));
            }

            if (calibrantSet != CalibrantSet.All)
            {
                foreach (var calibrant in data)
                {
                    // TODO: probably don't want to do this.
                    calibrant.Enabled = true;
                }
            }

            return data;
        }

        public static IEnumerable<CalibrantInfo> ReadFromFile(string path)
        {
            using (var reader = new CsvReader(new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                var config = reader.Configuration;
                config.RegisterClassMap(new CalibrantInfoMap());
                if (path.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    config.Delimiter = ",";
                }
                else
                {
                    config.Delimiter = "\t";
                }

                // Allow missing columns and fields
                config.HeaderValidated = null;
                config.MissingFieldFound = null;
                config.PrepareHeaderForMatch = (header, index) => header?.Trim().ToLower();
                config.AllowComments = true;
                config.Comment = '#';

                var records = reader.GetRecords<CalibrantInfo>();

                // If I just return records, we exit the using before the records are read
                // Use a yield return on the foreach to keep the reader alive until the records are all read.
                foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        public const string FileFormatDescription = "Comma- or tab-separated file.\nRequired columns are 'm/z' and 'charge' (or 'z')\nOptional columns include 'Name' and 'Formula'";

        public class CalibrantInfoMap : ClassMap<CalibrantInfo>
        {
            public CalibrantInfoMap()
            {
                var index = 0;
                Map(x => x.Name).Name("Name").Index(index++).Default("");
                Map(x => x.Formula).Name("Formula").Index(index++).Default("");
                Map(x => x.Mz).Name("m/z", "mz").Index(index++).Default("");
                Map(x => x.Charge).Name("charge", "z").Index(index++).Default("");
                Map(x => x.DriftTubeCCS).Name("Drift Tube CCS", "CCS").Index(index++).Default("");
            }
        }
    }
}
