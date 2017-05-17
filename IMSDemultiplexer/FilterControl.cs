namespace IMSDemultiplexer
{
	public class FilterControl
	{
		public bool DoBoxCarFilter { get; set; }
		public bool DoSpuriousPeakRemoval { get; set; }
		public bool DoSpuriousNoiseRemoval { get; set; }

		public int MinIntensityToProcessSegment { get; set; }
		public int BoxCarLength { get; set; }
		public int BoxCarMinValue { get; set; }
		public int SpuriousPeakMinGap { get; set; }
		public int SpuriousNoiseChunkSizeinNanoSeconds { get; set; }
		public int SpuriousNoiseMinNumPoints { get; set; }
		public int MinDriftTimeInNanoSeconds { get; set; }
		public int MaxDriftTimeInNanoSeconds { get; set; }

		public double MinValueToKeep { get; set; }

		public FilterControl()
		{
			DoBoxCarFilter = true;
			DoSpuriousPeakRemoval = true;
			DoSpuriousNoiseRemoval = true;
			MinIntensityToProcessSegment = 4;
			BoxCarLength = 4;
			BoxCarMinValue = -100;
			SpuriousPeakMinGap = 5;
			
			SpuriousNoiseChunkSizeinNanoSeconds = 50;
			SpuriousNoiseMinNumPoints = 3;
			MinValueToKeep = 0.01;
			MinDriftTimeInNanoSeconds = 0;
			MaxDriftTimeInNanoSeconds = 60000000;
		}
	}
}
