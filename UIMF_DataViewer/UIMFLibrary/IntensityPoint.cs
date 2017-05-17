using System;

namespace UIMFLibrary
{
	public class IntensityPoint : IComparable<IntensityPoint>
	{
		public int ScanLc { get; private set; }
		public int ScanIms { get; private set; }
		public double Intensity { get; set; }

		public IntensityPoint(int scanLc, int scanIms, double intensity)
		{
			ScanLc = scanLc;
			ScanIms = scanIms;
			Intensity = intensity;
		}

		public int CompareTo(IntensityPoint other)
		{
			return this.ScanLc != other.ScanLc ? this.ScanLc.CompareTo(other.ScanLc) : this.ScanIms.CompareTo(other.ScanIms);
		}

		public bool Equals(IntensityPoint other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ScanLc == ScanLc && other.ScanIms == ScanIms;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (IntensityPoint)) return false;
			return Equals((IntensityPoint) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (ScanLc*397) ^ ScanIms;
			}
		}

		public static bool operator ==(IntensityPoint left, IntensityPoint right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(IntensityPoint left, IntensityPoint right)
		{
			return !Equals(left, right);
		}
	}
}
