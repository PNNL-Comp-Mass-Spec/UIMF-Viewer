using System;
using System.Collections.Generic;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Concurrent;

namespace IMSDemultiplexer
{
	/// <summary>
	/// Class for demultiplexing Ion Mobility Mass Spectrometry data.
	/// </summary>
	public class Demultiplexer
	{
		private DenseMatrix m_multiplierMatrix;
		private int m_segmentLength;
		private int m_numSegments;
		private int m_minIntensityToProcessSegment;
		private Dictionary<int, int> m_indexToScanReverseMap;
		private double EPSILON = Math.Pow(10, -3);
		private ParallelOptions m_parallelOptions;
		private DeconToolsPeakDetectorV2 m_peakDetector;
		private List<int> m_symmetricPairLocations;
		private List<int> m_encodingPRS;
		private int m_totalSize;
		private int m_peakDelta;
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="demultiplexerOptions"></param>
		/// <param name="numberOfCoresToUse"></param>
		public Demultiplexer(DemultiplexerOptions demultiplexerOptions, int numberOfCoresToUse)
		{
			Initialize(demultiplexerOptions, numberOfCoresToUse);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="demultiplexerOptions"></param>
		public Demultiplexer(DemultiplexerOptions demultiplexerOptions)
		{
			int processorCount = Environment.ProcessorCount;
			Initialize(demultiplexerOptions, processorCount);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="demultiplexerOptions"></param>
		/// <param name="numberOfCoresToUse">Number of cores to use; -1 to use all cores</param>
		private void Initialize(DemultiplexerOptions demultiplexerOptions, int numberOfCoresToUse)
		{
			if (numberOfCoresToUse <= 0)
			{
				// -1 indicates "use all cores"
				numberOfCoresToUse = -1;
			}

			m_parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = numberOfCoresToUse };
			m_multiplierMatrix = demultiplexerOptions.MultiplierMatrix;
			m_segmentLength = demultiplexerOptions.SegmentLength;
			m_numSegments = demultiplexerOptions.NumberOfSegments;
			m_symmetricPairLocations = demultiplexerOptions.SymmetricPairLocations;
			m_encodingPRS = demultiplexerOptions.BitSequence.Select(x => int.Parse(x.ToString())).ToList();
			m_totalSize = m_numSegments * m_segmentLength;
			m_minIntensityToProcessSegment = 1;

			// TODO: Do not hard code?
			m_peakDetector = new DeconToolsPeakDetectorV2 { PeakToBackgroundRatio = 0.005, SignalToNoiseThreshold = 0, IsDataThresholded = true };
			m_peakDelta = 3;
		}

		public int SegmentLength { get { return m_segmentLength; }}

		/// <summary>
		/// Demultiplexes a single frame of data.
		/// </summary>
		/// <param name="intensityListsForSingleFrame">The data for the frame.</param>
		/// <param name="isRedordered">True if the data has already been re-ordered in preparation for demultiplexing.</param>
		/// <returns>An enumerable collection of ScanData objects.</returns>
		public IEnumerable<ScanData> DemultiplexFrame(double[][] intensityListsForSingleFrame, bool isRedordered)
		{
			// If the data has not been re-ordered by segments, then preform the re-ordering before continuing
			if (!isRedordered)
			{
				// TODO: Write function for re-ordering
				throw new NotSupportedException();
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

			List<Dictionary<int, double>> demultiplexedData = new List<Dictionary<int, double>>(numBins);
			for (int i = 0; i < numBins; i++)
			{
				Dictionary<int, double> dictionary = new Dictionary<int, double>();
				demultiplexedData.Add(dictionary);
			}

			Parallel.For(0, numBins, m_parallelOptions, delegate(int binIndex)
			{
				double[] intensitiesForSingleBin = intensityArraysForSingleFrame[binIndex];
				Dictionary<int, double> dataForSingleBin = demultiplexedData[binIndex];
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
				// Save the data to the appropriate bin
				demultiplexedData[binIndex] = NumericalAlgorithm(dataForSingleBin, intensitiesForSingleBin);
				//demultiplexedData[binIndex] = dataForSingleBin;
			});

			// Re-sort the data to be grouped by scans instead of grouped by bins
			IEnumerable<ScanData> scanDataEnumerable = ConvertDictionaryListToScanData(demultiplexedData);

			return scanDataEnumerable;
		}

		/// <summary>
		/// The algorithm where the entirity of the work to remove data artifacts is actually performed.
		/// </summary>
		/// <param name="dataForSingleBin"></param>
		/// <param name="intensitiesForSingleBin"></param>
		/// <returns></returns>
		private Dictionary<int, double> NumericalAlgorithm(Dictionary<int, double> dataForSingleBin, double[] intensitiesForSingleBin)
		{
			dataForSingleBin = SymmetricPairElimination(dataForSingleBin);
			List<Peak> demultiplexedPeaks = FindPeaksPostProcess(dataForSingleBin).ToList();
			if (demultiplexedPeaks.Count > 0)
			{
				double[] scanOrderedIntensitiesForSingleBin = MatrixCreator.ReOrderArray(intensitiesForSingleBin, m_indexToScanReverseMap);
				List<int> indicesToKeep = ValidatePeaksWithEncodedData(scanOrderedIntensitiesForSingleBin, m_encodingPRS, demultiplexedPeaks);

				if (indicesToKeep.Count > 0)
				{
					dataForSingleBin = KeepTruePeaks(indicesToKeep, dataForSingleBin);
				}
				else
				{
					//	m_binIndexDeleted.Add(binIndex);
					dataForSingleBin.Clear();
				}
			}
			else
			{
				dataForSingleBin.Clear();
			}

			return dataForSingleBin;
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
			Parallel.For(0, numScans, m_parallelOptions, scanIndex =>
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
			});

			return scanDataBag;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="intensitiesForASingleBin"></param>
		/// <returns></returns>
		private List<Peak> SmoothAndPeakFind(double[] intensitiesForASingleBin)
		{
			XYData xyData = new XYData();
			List<double> xvals = new List<double>();
			List<double> yvals = new List<double>();

			for (int i = 0; i < intensitiesForASingleBin.Length; i++)
			{
				xvals.Add(i);
				yvals.Add(intensitiesForASingleBin[i]);
			}
		
			int count = xvals.Count;
			for (int i = count; i < count + 6; i++)
			{
				xvals.Add(i);
				yvals.Add(intensitiesForASingleBin[MathMod(i,m_totalSize)]);
			}

			if (xyData.Xvalues.Length != xyData.Yvalues.Length)
			{
				throw new Exception("There must be the same number of x values as y values.");
			}
			xyData.Yvalues = yvals.ToArray();
			xyData.Xvalues = xvals.ToArray();

			XYData smoothedXYData = SavGolSmoother.sgfilt(2, 9, xyData);
			List<Peak> peaks = m_peakDetector.FindPeaks(smoothedXYData, 0, m_totalSize - 1);

			foreach (Peak peak in peaks)
			{
				peak.XValue = MathMod((int)Math.Round(peak.XValue), m_totalSize);
			}

			return peaks;
		}

		private IEnumerable<Peak> SmoothAndPeakFind(Dictionary<int, double> intensitiesForASingleBin)
		{
			List<Peak> peaks = new List<Peak>();
			if (intensitiesForASingleBin.Count == 0)
			{
				return peaks;
			}

			// Convert Dictionary to array
			double[] intensitiesForSingleBin = new double[m_totalSize];

			foreach (KeyValuePair<int, double> keyValuePair in intensitiesForASingleBin)
			{
				intensitiesForSingleBin[keyValuePair.Key] = keyValuePair.Value;
			}

			peaks = SmoothAndPeakFind(intensitiesForSingleBin);
			return peaks;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="intensitiesForASingleBin"></param>
		/// <param name="encodingPRS"></param>
		/// <param name="demultiplexedPeakIndices"></param>
		/// <returns></returns>
		private List<int> ValidatePeaksWithEncodedData(double[] intensitiesForASingleBin, List<int> encodingPRS, IEnumerable<Peak> demultiplexedPeaks)
		{
			List<Peak> peaks = SmoothAndPeakFind(intensitiesForASingleBin);
			
			// Grab all indexes of encoded peak apexes
			List<int> encodedPeakIndices = peaks.Select(peak => (int) Math.Round(peak.XValue)).ToList();

			HashSet<int> indicesToKeep = new HashSet<int>();

			foreach (Peak peak in demultiplexedPeaks)
			{
				double peakXValue = peak.XValue;
				double peakHeight = peak.Height;

				int currentPeak = (int)Math.Round(peakXValue);
				bool isValidPeak = true;

				for (int i = 0; i < m_segmentLength; i++)
				{
					double totalPeakValue = 0;

					// Do 2 binary searches to test whether a peak index is found in the encoded list or not
					int possPeakLeft = encodedPeakIndices.BinarySearch(MathMod(currentPeak - (m_peakDelta), m_totalSize));
					int possPeakRight = encodedPeakIndices.BinarySearch(MathMod(currentPeak + (m_peakDelta), m_totalSize));

					possPeakLeft = possPeakLeft < 0 ? ~possPeakLeft : possPeakLeft;
					possPeakRight = possPeakRight < 0 ? ~possPeakRight : possPeakRight;

					// 0 if no peak found, 1 if peak found
					int sequenceValue = possPeakLeft != possPeakRight ? 1 : 0;

					int prsSequenceValue = encodingPRS[i];

					if(prsSequenceValue == 1)
					{
						// If the PRS has a 1 and the current sequence value is not 1, then this peak is invalid
						if(sequenceValue != 1)
						{
							isValidPeak = false;
							break;
						}

						if(i == 0)
						{
							// Find the height of the current matched peak
							for (int j = possPeakLeft; j < possPeakRight; j++)
							{
								totalPeakValue += peaks[j].Height;
							}

							double heightRatio = peakHeight/totalPeakValue/(m_segmentLength/2.0);

							// If the first peak is way too small, or way too big, we don't want to consider it
							if (heightRatio > 4 || heightRatio < 0.2)
							{
								isValidPeak = false;
								break;
							}
						}
					}

					// Move on to the next possible segment
					currentPeak = MathMod(currentPeak + m_numSegments, m_totalSize);
				}

				// If the peak is not valid, then move on without keeping this peak
				if (!isValidPeak)
				{
					continue;
				}
				
				// If we make it this far, then the peak is valid
				indicesToKeep.Add((int)Math.Round(peakXValue));
			}

			return indicesToKeep.ToList();
		}

		/// <summary>
		/// Evaluates the pseudo random sequence that was constructed in order to verify whether it was a valid PRS. If even one bit is different, false is returned.
		/// Else, it returns true.
		/// </summary>
		/// <param name="referencePseudoRandomSequence"></param>
		/// <param name="pseudoRandomSequenceToTest"></param>
		/// <returns></returns>
		private static bool IsValidPseudoRandomSequence(IList<int> referencePseudoRandomSequence, IList<int> pseudoRandomSequenceToTest)
		{
			for (int i = 0; i < referencePseudoRandomSequence.Count; i++)
			{
				// Not a valid PRS if we expect a 1, but saw a 0
				if (referencePseudoRandomSequence[i] == 1 && pseudoRandomSequenceToTest[i] == 0)
				{
					return false;
				}
			}

			return true;
		}

		private Dictionary<int, double> KeepTruePeaks(IEnumerable<int> indicesToKeep, Dictionary<int, double> dataForSingleBin)
		{
			if (dataForSingleBin.Count == 0) return dataForSingleBin;

			Dictionary<int, double> newDictionary = new Dictionary<int, double>();

			foreach (int index in indicesToKeep)
			{
				double dummyValue;
				if (dataForSingleBin.TryGetValue(index, out dummyValue))
				{
					if (!newDictionary.ContainsKey(index)) newDictionary.Add(index, dummyValue);
				}
				else
				{
					// If we cannot even find the apex of the peak we are looking for, then do not continue
					continue;
				}

				// Look on the right side of the peak
				for (int currentIndex = index + 1;; currentIndex++)
				{
					currentIndex = MathMod(currentIndex, m_totalSize);

					// If this index exists in the dictionary, that means we have non-0 data here, so add it to the new dictionary
					if (dataForSingleBin.TryGetValue(currentIndex, out dummyValue))
					{
						if (!newDictionary.ContainsKey(currentIndex)) newDictionary.Add(currentIndex, dummyValue);
					}
					// Otherwise, the intensity is 0 for this spot, so get out!
					else
					{
						break;
					}
				}

				// Look on the left side of the peak
				for (int currentIndex = index - 1;; currentIndex--)
				{
					currentIndex = MathMod(currentIndex, m_totalSize);

					// If this index exists in the dictionary, that means we have non-0 data here, so add it to the new dictionary
					if (dataForSingleBin.TryGetValue(currentIndex, out dummyValue))
					{
						if (!newDictionary.ContainsKey(currentIndex)) newDictionary.Add(currentIndex, dummyValue);
					}
					// Otherwise, the intensity is 0 for this spot, so get out!
					else
					{
						break;
					}
				}
			}

			return newDictionary;
		}


		private IEnumerable<Peak> FindPeaksPostProcess(Dictionary<int, double> intensitiesForASingleBin)
		{
			List<int> demultiplexedPeaks = new List<int>();

			IEnumerable<Peak> peaks = SmoothAndPeakFind(intensitiesForASingleBin);

			return peaks;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="intensitiesForASingleBin"></param>
		/// <returns></returns>
		public Dictionary<int, double> SymmetricPairElimination(Dictionary<int, double> intensitiesForASingleBin)
		{
			// If empty, nothing to do, so return the empty Dictionary
			if (intensitiesForASingleBin.Count == 0) return intensitiesForASingleBin;

			// Convert Dictionary to array
			double[] array = new double[m_totalSize];

			foreach (KeyValuePair<int, double> keyValuePair in intensitiesForASingleBin)
			{
				array[keyValuePair.Key] = keyValuePair.Value;
			}
			return SymmetricPairElimination(array);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="doubleList"></param>
		/// <returns></returns>
		private int MaxIndex(IEnumerable<double> doubleList)
		{
			int maxIndex = -1;
			double maxValue = double.MinValue;

			int index = 0;
			foreach (double value in doubleList)
			{
				if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
				{
					maxIndex = index;
					maxValue = value;
				}
				index++;
			}
			return maxIndex;
		}

		/// <summary>
		/// Systematically eliminates data artifacts due to modulation defects in the IMS-QTOF instrument due to the particular encoding scheme used to encode the data.
		/// 
		/// </summary>
		/// <param name="intensitiesForASingleBin"></param>
		/// <returns name="processedDictionary"></returns>
		public Dictionary<int, double> SymmetricPairElimination(double[] intensitiesForASingleBin)
		{
			Dictionary<int, double> scanValuesToKeep = new Dictionary<int, double>();

			int indexOfApex = MaxIndex(intensitiesForASingleBin);

			RemoveReflectedDataPoints(intensitiesForASingleBin, indexOfApex, m_symmetricPairLocations, ref scanValuesToKeep);

			return scanValuesToKeep;
		}

		/// <summary>
		/// This function takes the intensities for the bin, the number of points on each side of the maximum peak that were kept, the index of the apex, the list of search values,
		/// and the dictionary of scan values to keep. The function then moves through generating a candidate value and potential matching (negation of the candidate value) value. 
		/// If the axis of reflection for the symmetric values is not y = 0, then another attempt at finding that axis and shifting it back to y = 0 is made. Otherwise, the values will not be
		/// set to 0. If they are symmetric, then their values will be set to 0.
		/// </summary>
		/// <param name="intensitiesForASingleBin">Array of doubles for intensities for the bin.</param>
		/// <param name="indexOfApex">The calculated index of the apex in the bin</param>
		/// <param name="searchValues">The list of search values based on the pseudo-random sequence (1s)</param>
		/// <param name="scanValuesToKeep">Dictionary of values to be returned for scan values to keep (the goal: only real data)</param>
		private void RemoveReflectedDataPoints(double[] intensitiesForASingleBin, int indexOfApex, List<int> searchValues, ref Dictionary<int, double> scanValuesToKeep)
		{
			// can go up to 24 before the oversampled "chunk region" has been covered.
			int chunkOffset = 0;
			// index of the list of scan values
			int searchIndex = 0;
			// number of search values in the list (for calculating loops, logical expressions)
			int numOfSearchValues = searchValues.Count;
			// assists in the logical expressions involving the location of the search for candidate / target matching "pair" values.
			bool resetChunkOffset = true;

			for (int i = 0; i < intensitiesForASingleBin.Length; i++)
			{
				if (chunkOffset >= m_numSegments)
				{
					searchIndex++;
					chunkOffset = 0;
					resetChunkOffset = true;
				}
				chunkOffset++;

				// This means have searched through all chunks, so we should exit
				if (searchIndex >= searchValues.Count)
				{
					// Since we are exiting, we need to make sure to grab the left values and put them into the dictionary
					for (int j = i; j < intensitiesForASingleBin.Length; j++)
					{
						int indexToAdd = (j + indexOfApex) % m_totalSize;
						double valueToAdd = intensitiesForASingleBin[indexToAdd];
						if (valueToAdd > 0 && !scanValuesToKeep.ContainsKey(indexToAdd)) scanValuesToKeep.Add(indexToAdd, valueToAdd);
					}

					// Now we are ready to exit
					break;
				}

				int currentIndex = (i + indexOfApex) % m_totalSize;
				double currentValue = intensitiesForASingleBin[currentIndex];

				// We want to skip the index if it is 0 to avoid unecessary computation
				if (Math.Abs(currentValue - 0) < EPSILON)
				{
					continue;
				}

				int searchValue = searchValues[searchIndex];
				int checkIndex = MathMod(currentIndex + searchValue, m_totalSize);
				double possibleMatch = (-1) * intensitiesForASingleBin[checkIndex];
				if (Math.Abs(currentValue - possibleMatch) < EPSILON)
				{
					if (resetChunkOffset)
					{
						chunkOffset = 0;
						resetChunkOffset = false;
					}
					intensitiesForASingleBin[currentIndex] = 0;
					intensitiesForASingleBin[checkIndex] = 0;
					continue;
				}

				if (searchIndex + 1 < numOfSearchValues)
				{
					searchValue = searchValues[searchIndex + 1];
					checkIndex = MathMod(currentIndex + searchValue, m_totalSize);
					possibleMatch = (-1) * intensitiesForASingleBin[checkIndex];
					if (Math.Abs(currentValue - possibleMatch) < EPSILON)
					{
						intensitiesForASingleBin[currentIndex] = 0;
						intensitiesForASingleBin[checkIndex] = 0;
						continue;
					}
				}

				// If we get here, then the value should be kept
				if (currentValue > 0) scanValuesToKeep.Add(currentIndex, currentValue);
			}
		}

		/// <summary>
		/// True Math Modulo based on http://stackoverflow.com/questions/2691025/mathematical-modulus-in-c-sharp and
		/// http://en.wikipedia.org/wiki/Modular_arithmetic
		/// Prevents negative values for the mod (which should not be allowed). C#'s % operator is actually a remainder 
		/// operator that can return a negative value.
		/// </summary>
		/// <param name="valueToMod"></param>
		/// <param name="modValue"></param>
		/// <returns></returns>
		private int MathMod(int valueToMod, int modValue)
		{
			return (Math.Abs(valueToMod * modValue) + valueToMod) % modValue;
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
