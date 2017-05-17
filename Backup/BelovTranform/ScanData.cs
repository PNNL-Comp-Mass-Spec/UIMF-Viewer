using System.Collections.Generic;

namespace BelovTransform
{
	public class ScanData
	{
		public int ScanNumber { get; set; }
		//public List<int> Bins { get; set; }
		//public List<int> Intensities { get; set; }
		public Dictionary<int, int> BinsToIntensitiesMap { get; set; }

		public ScanData(int scanNumber)
		{
			this.ScanNumber = scanNumber;
			//this.Bins = new List<int>();
			//this.Intensities = new List<int>();
			this.BinsToIntensitiesMap = new Dictionary<int, int>();
		}
	}
}
