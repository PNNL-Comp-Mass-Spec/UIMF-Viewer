using System;

namespace UIMF_DataViewer.Utilities
{
    internal struct ZoomInfo : IEquatable<ZoomInfo>
    {
        public int XMin { get; }
        public int XMax { get; }
        public int YMin { get; }
        public int YMax { get; }

        public int XDiff => XMax - XMin;
        public int YDiff => YMax - YMin;

        public ZoomInfo(int xMin, int xMax, int yMin, int yMax)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
        }

        #region Equality

        public bool Equals(ZoomInfo other)
        {
            return XMin == other.XMin && XMax == other.XMax && YMin == other.YMin && YMax == other.YMax;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ZoomInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = XMin;
                hashCode = (hashCode * 397) ^ XMax;
                hashCode = (hashCode * 397) ^ YMin;
                hashCode = (hashCode * 397) ^ YMax;
                return hashCode;
            }
        }

        #endregion
    }
}
