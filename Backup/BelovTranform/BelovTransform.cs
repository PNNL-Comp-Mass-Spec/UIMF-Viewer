using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Concurrent;

namespace BelovTransform
{
	/// <summary>
	/// Class for demultiplexing Ion Mobility Mass Spectrometry data.
	/// </summary>
	public class BelovTransform
	{
		private DenseMatrix m_multiplierMatrix;
		private bool m_doBoxCarFilter;
		private bool m_doSpuriousPeakRemoval;
		private bool m_doSpuriousNoiseRemoval;
		private int m_segmentLength;
		private int m_numSegments;
		private int m_minIntensityToProcessSegment;
		private int m_boxCarLength;
		private int m_boxCarMinValue;
		private int m_spuriousPeakMinGap;
		private int m_spuriousNoiseChunkSizeinNanoSeconds;
		private int m_spuriousNoiseMinNumPoints;
		private double m_minDriftTime;
		private double m_maxDriftTime;
		private double m_minValueToKeep;
		private double m_binWidth;
		private Dictionary<int, int> m_indexToScanReverseMap;

        private FilterControl filterControl;

		/// <summary>
		/// Constructor for Demultiplexing class.
		/// </summary>
		/// <param name="multiplierMatrix">The matrix that will be used for demultiplexing. Must be a square matrix.</param>
		/// <param name="numSegments">The number of segments in the multiplexing scheme.</param>
		/// <param name="filterControl">Object that contains all the filter parameters that should be used.</param>
		public BelovTransform(DenseMatrix multiplierMatrix, int numSegments, FilterControl filter_control, double binWidth)
		{
            this.filterControl = filter_control;

			if (multiplierMatrix.ColumnCount != multiplierMatrix.RowCount)
			{
				throw new Exception("A non-square matrix was passed in. Only a square matrix may be used.");
			}

			m_multiplierMatrix = multiplierMatrix;
			m_segmentLength = multiplierMatrix.RowCount;
			m_numSegments = numSegments;
            m_minIntensityToProcessSegment = this.filterControl.MinIntensityToProcessSegment;
            m_doBoxCarFilter = this.filterControl.DoBoxCarFilter;
            m_boxCarLength = this.filterControl.BoxCarLength;
            m_boxCarMinValue = this.filterControl.BoxCarMinValue;
            m_doSpuriousPeakRemoval = this.filterControl.DoSpuriousPeakRemoval;
            m_spuriousPeakMinGap = this.filterControl.SpuriousPeakMinGap;
            m_doSpuriousNoiseRemoval = this.filterControl.DoSpuriousNoiseRemoval;
            m_spuriousNoiseChunkSizeinNanoSeconds = this.filterControl.SpuriousNoiseChunkSizeinNanoSeconds;
            m_spuriousNoiseMinNumPoints = this.filterControl.SpuriousNoiseMinNumPoints;
            m_minValueToKeep = this.filterControl.MinValueToKeep;
			m_binWidth = binWidth;
		}

		/// <summary>
		/// Demultiplexes a single frame of data.
		/// </summary>
		/// <param name="intensityListsForSingleFrame">The data for the frame.</param>
		/// <param name="isRedordered">True if the data has already been re-ordered in preparation for demultiplexing.</param>
		/// <returns>An enumerable collection of ScanData objects.</returns>
		public IEnumerable<ScanData> DemultiplexFrame(double[][] intensityListsForSingleFrame, bool isRedordered, double averageTOFLength)
		{
            m_minDriftTime = this.filterControl.MinDriftTimeInNanoSeconds / averageTOFLength;
            m_maxDriftTime = this.filterControl.MaxDriftTimeInNanoSeconds / averageTOFLength;

			// If the data has not been re-ordered by segments, then preform the re-ordering before continuing
			if (!isRedordered)
			{
				// TODO: Write function for re-ordering
			}

			return DemultiplexFrame(intensityListsForSingleFrame);
		}

		/// <summary>
		/// Demultiplexes a single frame of data. The frame data should already be re-ordered in preparation for demultiplexing.
		/// </summary>
		/// <param name="intensityArraysForSingleFrame">The data for the frame.</param>
		/// <returns>An enumerable collection of ScanData objects.</returns>
		private IEnumerable<ScanData> DemultiplexFrame(double[][] intensityArraysForSingleFrame)
		{
			int numBins = intensityArraysForSingleFrame.Count();
			int numScans = m_numSegments * m_segmentLength;

			List<Dictionary<int, double>> demultiplexedData = new List<Dictionary<int, double>>(numBins);
			for (int i = 0; i < numBins; i++)
			{
				Dictionary<int, double> dictionary = new Dictionary<int, double>();
				demultiplexedData.Add(dictionary);
			}

			for (int binIndex = 0; binIndex < numBins; binIndex++)
			{
				double[] intensitiesForSingleBin = intensityArraysForSingleFrame[binIndex];
				Dictionary<int, double> dataForSingleBin = demultiplexedData[(int)binIndex];

				for (int i = 0; i < m_numSegments; i++)
				{
					int segmentStartIndex = i * m_segmentLength;

					// Build the array of the segment
					int arrayIndex = 0;
					bool processSegment = false;
					double[] intensityArrayOfSegment = new double[m_segmentLength];
					for (int j = segmentStartIndex; j < segmentStartIndex + m_segmentLength; j++)
					{
						double currentValue = intensitiesForSingleBin[j];

						// Only process the segment if at least 1 value is at least as large as a given threshold
						if (currentValue >= m_minIntensityToProcessSegment)
						{
							processSegment = true;
						}

						intensityArrayOfSegment[arrayIndex++] = intensitiesForSingleBin[j];
					}

					// If we should progress this segment, then move on
					if (!processSegment) continue;

					// Turn the segment into a Vector
					DenseVector vectorOfSegment = new DenseVector(intensityArrayOfSegment);

					// Multiply the vector by the matrix.
					vectorOfSegment *= m_multiplierMatrix;

					// Push the new intensity values into the dictionary, using their original scan values
					int index = 0;
					for (int j = segmentStartIndex; j < segmentStartIndex + m_segmentLength; j++)
					{
						dataForSingleBin.Add(m_indexToScanReverseMap[j], vectorOfSegment[index]);
						index++;
					}
				}

				if (m_doBoxCarFilter)
				{
					BoxCarFilter(dataForSingleBin);
				}

				if (m_minValueToKeep > 0)
				{
					dataForSingleBin = FilterByThreshold(dataForSingleBin);
				}

				if (m_doSpuriousPeakRemoval)
				{
					dataForSingleBin = SpuriousPeakRemoval(dataForSingleBin, numScans);
				}

				// Save the data to the appropriate bin
				demultiplexedData[(int)binIndex] = dataForSingleBin;
			}

			// Re-sort the data to be grouped by scans instead of grouped by bins
			IEnumerable<ScanData> scanDataEnumerable = ConvertDictionaryListToScanData(demultiplexedData);

			scanDataEnumerable = FilterByDriftTime(scanDataEnumerable);

			if (m_doSpuriousNoiseRemoval)
			{
				SpuriousNoiseRemoval(scanDataEnumerable);
			}

			return scanDataEnumerable;
		}

		/// <summary>
		/// Build a Map between the scan number and the index that should be used for demultiplexing.
		/// This map provides the mapping necessary to re-order the data to prepare it for demultiplexing.
		/// </summary>
		/// <returns>A Dictionary with keys of scan number and values of indices.</returns>
		public Dictionary<int, int> BuildScanToIndexMap()
		{
			int maxScanNumber = (m_numSegments * m_segmentLength) - 1;
			Dictionary<int, int> scanToIndexMap = new Dictionary<int, int>();
			m_indexToScanReverseMap = new Dictionary<int, int>();

			for (int currentScan = 0; currentScan <= maxScanNumber; currentScan++)
			{
				int index = ConvertScanToIndex(currentScan);
				scanToIndexMap.Add(currentScan, index);
				m_indexToScanReverseMap.Add(index, currentScan);
			}

			return scanToIndexMap;
		}

		/// <summary>
		/// Converts the demultiplexed frame into an enumerable collection of ScanData objects.
		/// Converting to ScanData objects allows for easy writing to UIMF files.
		/// </summary>
		/// <param name="demultiplexedFrame">The data for the demultiplexed frame.</param>
		/// <returns>An enumerable collection of ScanData objects.</returns>
		private IEnumerable<ScanData> ConvertDictionaryListToScanData(IList<Dictionary<int, double>> demultiplexedFrame)
		{
			int numScans = m_numSegments * m_segmentLength;
			int numBins = demultiplexedFrame.Count;

			ConcurrentBag<ScanData> scanDataBag = new ConcurrentBag<ScanData>();

            for(int scanIndex = 0; scanIndex < numScans; scanIndex++) 
			//Parallel.For(0, numScans, scanIndex =>
			{
				ScanData scanData = new ScanData(scanIndex);
				double intensity;

				for (int bin = 0; bin < numBins; bin++)
				{
					// Grab the intensity dictionary for this bin
					Dictionary<int, double> intensitiesForSingleBin = demultiplexedFrame[bin];

					// If the dictionary is empty, then continue
					if (intensitiesForSingleBin.Count == 0) continue;

					// If we do not find an intensity value for this bin + scan, then move on
					if (!intensitiesForSingleBin.TryGetValue(scanIndex, out intensity)) continue;

					// If we do find an intensity value, add the bin and intensity data to the ScanData object
					scanData.BinsToIntensitiesMap.Add(bin, (int)Math.Round(intensity));
				}

				// Only include the ScanData object if it actually had intensity values
				if (scanData.BinsToIntensitiesMap.Count > 0)
				{
					scanDataBag.Add(scanData);
				}
			}
            //);

			return scanDataBag;
		}

		/// <summary>
		/// Filters out any data outside the min and max drift time range.
		/// </summary>
		/// <param name="scanDataEnumerable">The enumerable collection of ScanData objects to be filtered.</param>
		/// <returns>The filtered enumerable collection of ScanData objects.</returns>
		private IEnumerable<ScanData> FilterByDriftTime(IEnumerable<ScanData> scanDataEnumerable)
		{
			var filterByDriftTimeQuery = from scanData in scanDataEnumerable
										 where scanData.ScanNumber >= m_minDriftTime && scanData.ScanNumber <= m_maxDriftTime
			                             select scanData;

			return filterByDriftTimeQuery.AsEnumerable();
		}

		/// <summary>
		/// This filter breaks up the bin data into chunks and filters out any chunks that have less than a given number of data points greater than 0.
		/// </summary>
		/// <param name="scanDataEnumerable">The enumerable collection of ScanData objects to be filtered. This collection will be modified directly.</param>
		private void SpuriousNoiseRemoval(IEnumerable<ScanData> scanDataEnumerable)
		{
			foreach (ScanData scanData in scanDataEnumerable)
			{
				// Groups the bins into sections that are the size of m_spuriousNoiseChunkSizeinNanoSeconds and only returns the group if it contains less that minimum points required
				var groupByBinsQuery = from key in scanData.BinsToIntensitiesMap.Keys
				                       group key by (key / (int)(m_spuriousNoiseChunkSizeinNanoSeconds / m_binWidth)) into g
									   where g.Count() < (m_spuriousNoiseMinNumPoints / m_binWidth)
				                       select g;

				// Remove each group of bins that should be filtered out
				foreach (var group in groupByBinsQuery)
				{
					// Iterate over the group and remove each key found in that group
					foreach (int key in group)
					{
						scanData.BinsToIntensitiesMap.Remove(key);
					}
				}
			}
		} 

		/// <summary>
		/// This filter searches for the most abundant peak in a single TOF Bin and filters out less abudnant peaks if there is a gap between peaks of a given size.
		/// </summary>
		/// <param name="intensitiesForSingleBin">The scan:intensity Map for a single bin.</param>
		/// <param name="numScans">
		/// The total number of 0 or non-0 scans. 
		/// Since the Map does not contain 0-intensity data, the number of scans cannot be inferred by the map size.
		/// </param>
		/// <returns>A new filtered scan:intensity map.</returns>
		private Dictionary<int, double> SpuriousPeakRemoval(IDictionary<int, double> intensitiesForSingleBin, int numScans)
		{
			// If we are working with empty data, just return the empty data
			if(intensitiesForSingleBin.Count == 0)
			{
				return (Dictionary<int, double>)intensitiesForSingleBin;
			}

			Dictionary<int, double> newDictionary = new Dictionary<int, double>();

			// We want to get the most intense peak
			double maxValue = 0;
			KeyValuePair<int, double> kvpOfMaxIntensity = new KeyValuePair<int, double>();
			foreach (KeyValuePair<int, double> kvp in intensitiesForSingleBin)
			{
				if(kvp.Value > maxValue)
				{
					maxValue = kvp.Value;
					kvpOfMaxIntensity = kvp;
				}
			}

			// Get the index of the most intense peak
			int indexOfMaxIntensity = kvpOfMaxIntensity.Key;

			// Add the most intense peak to the new dictionary
			newDictionary.Add(kvpOfMaxIntensity.Key, kvpOfMaxIntensity.Value);

			int count = 0;
			double dummyValue;

			// Look on the right side of the peak
			for (int currentIndex = indexOfMaxIntensity + 1; count < m_spuriousPeakMinGap && currentIndex < numScans; currentIndex++)
			{
				// If this index exists in the dictionary, that means we have non-0 data here, so add it to the new dictionary and reset the count
				if (intensitiesForSingleBin.TryGetValue(currentIndex, out dummyValue))
				{
					newDictionary.Add(currentIndex, dummyValue);
					count = 0;
				}
				// Otherwise, the intensity is 0 for this spot, so increment the count
				else
				{
					count++;
				}
			}

			// Reset the count
			count = 0;

			// Look on the left side of the peak
			for (int currentIndex = indexOfMaxIntensity - 1; count < m_spuriousPeakMinGap && currentIndex >= 0; currentIndex--)
			{
				// If this index exists in the dictionary, that means we have non-0 data here, so add it to the new dictionary and reset the count
				if (intensitiesForSingleBin.TryGetValue(currentIndex, out dummyValue))
				{
					newDictionary.Add(currentIndex, dummyValue);
					count = 0;
				}
				// Otherwise, the intensity is 0 for this spot, so increment the count
				else
				{
					count++;
				}
			}

			return newDictionary;
		}

		/// <summary>
		/// Filters out any intensity values less than a given threshold.
		/// </summary>
		/// <param name="intensitiesForSingleBin">The scan:intensity Map for a single bin.</param>
		/// <returns>A new filtered scan:intensity map.</returns>
		private Dictionary<int, double> FilterByThreshold(IEnumerable<KeyValuePair<int, double>> intensitiesForSingleBin)
		{
			var filterByThresholdQuery = from kvp in intensitiesForSingleBin
										 where kvp.Value >= m_minValueToKeep
										 select kvp;

			return filterByThresholdQuery.ToDictionary(v => v.Key, v => v.Value);
		}

		/// <summary>
		/// This filter breaks up the data of a single bin into "boxcar" chunks of a given size.
		/// If the boxcar contains a value of less than a given threshold, then the entire boxcar will be filtered out.
		/// </summary>
		/// <param name="intensitiesForSingleBin">The scan:intensity Map for a single bin. This map will be directly modified.</param>
		private void BoxCarFilter(IDictionary<int, double> intensitiesForSingleBin)
		{
			HashSet<int> keysToRemove = new HashSet<int>();

			var findIntensitiesLessThanBoxCarMinQuery = from kvp in intensitiesForSingleBin
														where kvp.Value < m_boxCarMinValue
														select kvp;

			foreach (KeyValuePair<int, double> kvp in findIntensitiesLessThanBoxCarMinQuery)
			{
				int currentScan = kvp.Key;
				int currentBoxCarIndex = currentScan % m_boxCarLength;

				// Find each key we will want to remove and add them to the Set of keys to be removed
				for (int keyToRemove = (currentScan - currentBoxCarIndex); keyToRemove < (currentScan - currentBoxCarIndex + m_boxCarLength); keyToRemove++)
				{
					keysToRemove.Add(keyToRemove);
				}
			}

			// Remove all the entries of this boxcar from the Dictionary
			foreach (int keyToRemove in keysToRemove)
			{
				intensitiesForSingleBin.Remove(keyToRemove);
			}
		}

		/// <summary>
		/// Converts a scan number to an index value based on the number of segments and segment length for the demultiplexing scheme.
		/// </summary>
		/// <param name="scanNumber">The scan number to be used for the conversion.</param>
		/// <returns>The converted index value.</returns>
		private int ConvertScanToIndex(int scanNumber)
		{
			int modValue = scanNumber % m_numSegments;
			int indexValue = (scanNumber / m_numSegments) + (modValue * m_segmentLength);
			return indexValue;
		}
	}
}
