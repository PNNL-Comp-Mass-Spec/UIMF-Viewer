using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMSDemultiplexer
{
	public class TOFBinDescriptiveStatistics
	{
		public double mean { get; set; }

		public double standardDeviation { get; set; }

		public int tofBinNumber { get; set; }

		public double kurtosis { get; set; }

		public double variance { get; set; }

		public double median { get; set; }
	}
}
