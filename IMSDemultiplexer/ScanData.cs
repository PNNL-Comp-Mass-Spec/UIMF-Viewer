using System.Collections.Generic;

namespace IMSDemultiplexer
{
	public class ScanData
	{
		public int ScanNumber { get; set; }
		public Dictionary<int, int> BinsToIntensitiesMap { get; set; }

		public ScanData(int scanNumber)
		{
			ScanNumber = scanNumber;
			BinsToIntensitiesMap = new Dictionary<int, int>();
		}
	}
}
