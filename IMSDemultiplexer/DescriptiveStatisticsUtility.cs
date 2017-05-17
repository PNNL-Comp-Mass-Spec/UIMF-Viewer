using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Statistics;

namespace IMSDemultiplexer
{
	class DescriptiveStatisticsUtility
	{
		public static TOFBinDescriptiveStatistics calculateDescriptiveStatisticsNegativeValues(int tofBinIndex, Dictionary<int, double> intensityForSingleBin )
		{
			TOFBinDescriptiveStatistics binStat = new TOFBinDescriptiveStatistics();

			double[] intensityArray = new double[360];
			foreach (KeyValuePair<int, double> keyValuePair in intensityForSingleBin)
			{
				intensityArray[keyValuePair.Key] = keyValuePair.Value;
			}
			var query =
				from score in intensityArray
				where score < 0
				select score;
			var q = query.ToList();
			for (int i = 0; i < q.Count; i++)
			{
				q[i] = Math.Abs(q[i]);
			}
			DescriptiveStatistics s = new DescriptiveStatistics(intensityArray);
			binStat.mean = s.Mean;
			binStat.standardDeviation = s.StandardDeviation;
			binStat.tofBinNumber = tofBinIndex;
			binStat.variance = s.Variance;
			binStat.kurtosis = s.Kurtosis;
			binStat.median = s.Median;

			return binStat;
		}

		public static TOFBinDescriptiveStatistics CalculateDescriptiveStatisticsEntireBin(int tofBinIndex, Dictionary<int, double> intensityForSingleBin)
		{
			double[] intensityArray = new double[360];
			foreach (KeyValuePair<int, double> keyValuePair in intensityForSingleBin)
			{
				intensityArray[keyValuePair.Key] = keyValuePair.Value;
			}

			TOFBinDescriptiveStatistics binStat = new TOFBinDescriptiveStatistics();
			DescriptiveStatistics s = new DescriptiveStatistics(intensityArray);
			binStat.mean = s.Mean;
			binStat.standardDeviation = s.StandardDeviation;
			binStat.tofBinNumber = tofBinIndex;
			binStat.variance = s.Variance;
			binStat.kurtosis = s.Kurtosis;
			binStat.median = s.Median;

			return binStat;
		}

	}
}
