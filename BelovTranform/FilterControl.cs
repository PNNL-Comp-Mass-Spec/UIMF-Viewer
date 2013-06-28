namespace BelovTransform
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
			this.DoBoxCarFilter = true;
			this.DoSpuriousPeakRemoval = true;
			this.DoSpuriousNoiseRemoval = true;
			this.MinIntensityToProcessSegment = 4;
			this.BoxCarLength = 4;
			this.BoxCarMinValue = -100;
			this.SpuriousPeakMinGap = 5;
			this.SpuriousNoiseChunkSizeinNanoSeconds = 50;
			this.SpuriousNoiseMinNumPoints = 3;
			this.MinValueToKeep = 0.01;
			this.MinDriftTimeInNanoSeconds = 8000000;
			this.MaxDriftTimeInNanoSeconds = 60000000;
		}
	}
}
