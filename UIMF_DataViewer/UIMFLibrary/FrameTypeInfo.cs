using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UIMFLibrary
{
	class FrameTypeInfo
	{
		public int NumFrames { get; private set; }
		public int[] FrameIndexes { get; private set; }

		public FrameTypeInfo(int numFramesInFile)
		{
			this.NumFrames = 0;
			this.FrameIndexes = new int[numFramesInFile + 1];
		}

		public void AddFrame(int frameNumber)
		{
			this.FrameIndexes[frameNumber] = this.NumFrames;
			this.NumFrames++;
		}
	}
}
