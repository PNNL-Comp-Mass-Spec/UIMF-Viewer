using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using UIMFLibrary;

namespace IMSDemultiplexer
{
	public class DemultiplexerOptions
	{
		public int NumberOfSegments { get; private set; }
		public int SegmentLength { get; private set; }
		public String BitSequence { get; private set; }
		public DenseMatrix MultiplierMatrix { get; private set; }
		public List<int> SymmetricPairLocations { get; private set; }

		public DemultiplexerOptions(string uimfFileLocation)
		{
			FileInfo uimfFileInfo = new FileInfo(uimfFileLocation);

			if(!uimfFileInfo.Exists)
			{
				throw new FileNotFoundException("UIMF File was not found at given location: " + uimfFileLocation);
			}

			DataReader uimfReader = new DataReader(uimfFileInfo.FullName);

			// Look for any frames with IMFProfile defined
			string imfProfile = string.Empty;
			foreach (KeyValuePair<int, DataReader.FrameType> item in uimfReader.GetMasterFrameList())
			{
				FrameParameters frameParameters = uimfReader.GetFrameParameters(item.Key);
				if (!string.IsNullOrWhiteSpace(frameParameters.IMFProfile) && frameParameters.IMFProfile.ToLower() != "continuous")
				{
					imfProfile = frameParameters.IMFProfile.ToLower();
					break;
				}
			}

			if (string.IsNullOrWhiteSpace(imfProfile))
			{
				throw new NotSupportedException("None of the frames has a bit sequence defined in the IMFProfile column of the Frame_Parameters table");
			}

			// The IMFProfile field may contain a path to a file; remove any directory information
			imfProfile = System.IO.Path.GetFileName(imfProfile);

			if(imfProfile.Contains("2bit"))
			{
				throw new NotSupportedException("2-bit sequence not yet implemented. Coming soon!");
			}
			else if (imfProfile.Contains("3bit"))
			{
				this.SegmentLength = 7;
				this.NumberOfSegments = 49;
				this.BitSequence = "1011100";
				this.SymmetricPairLocations = new List<int>(6) { 196, 49, 294, 98, 147, 245 };
			}
			else if (imfProfile.Contains("4bit"))
			{
				this.SegmentLength = 15;
				this.NumberOfSegments = 24;
				this.BitSequence = "100110101111000";
				this.SymmetricPairLocations = new List<int>(14) { 264, 168, 24, 336, 120, 48, 144, 312, 192, 240, 72, 96, 216, 288 };
			}
			else if (imfProfile.Contains("5bit"))
			{
				this.SegmentLength = 31;
				this.NumberOfSegments = 12;
				this.BitSequence = "1001011001111100011011101010000";
				this.SymmetricPairLocations = new List<int>(30) { 156, 312, 24, 252, 348, 48, 108, 132, 180, 324, 144, 96, 204, 216, 84, 264, 12, 360, 240, 276, 72, 288, 228, 192, 120, 36, 300, 60, 336, 168 };
			}
			else if (imfProfile.Contains("6bit"))
			{
				throw new NotSupportedException("6-bit sequence not yet implemented. Coming soon!");
			}
			else
			{
				throw new NotSupportedException("Invalid bit sequence detected: " + imfProfile);
			}

			// Create the S-Matrix
			DenseMatrix multiplierMatrix = MatrixCreator.CreateMatrixForDemultiplexing(this.BitSequence);
			DenseMatrix scaledMatrix = (DenseMatrix)multiplierMatrix.Multiply(2.0 / (this.SegmentLength + 1));
			DenseMatrix inversedScaledMatrix = (DenseMatrix)scaledMatrix.Inverse();
			this.MultiplierMatrix = inversedScaledMatrix;
		}

		public DemultiplexerOptions(int numberOfBits)
		{
			if (numberOfBits == 2)
			{
				throw new NotSupportedException("2-bit sequence not yet implemented. Coming soon!");
			}
			else if (numberOfBits == 3)
			{
				this.SegmentLength = 7;
				this.NumberOfSegments = 49;
				this.BitSequence = "1011100";
				this.SymmetricPairLocations = new List<int>(6) { 196, 49, 294, 98, 147, 245 };
			}
			else if (numberOfBits == 4)
			{
				this.SegmentLength = 15;
				this.NumberOfSegments = 24;
				this.BitSequence = "100110101111000";
				this.SymmetricPairLocations = new List<int>(14) { 264, 168, 24, 336, 120, 48, 144, 312, 192, 240, 72, 96, 216, 288 };
			}
			else if (numberOfBits == 5)
			{
				this.SegmentLength = 31;
				this.NumberOfSegments = 12;
				this.BitSequence = "1001011001111100011011101010000";
				this.SymmetricPairLocations = new List<int>(30) { 156, 312, 24, 252, 348, 48, 108, 132, 180, 324, 144, 96, 204, 216, 84, 264, 12, 360, 240, 276, 72, 288, 228, 192, 120, 36, 300, 60, 336, 168 };
			}
			else if (numberOfBits == 6)
			{
				throw new NotSupportedException("6-bit sequence not yet implemented. Coming soon!");
			}
			else
			{
				throw new NotSupportedException("Invalid number of bits detected: " + numberOfBits);
			}

			// Create the S-Matrix
			DenseMatrix multiplierMatrix = MatrixCreator.CreateMatrixForDemultiplexing(this.BitSequence);
			DenseMatrix scaledMatrix = (DenseMatrix)multiplierMatrix.Multiply(2.0 / (this.SegmentLength + 1));
			DenseMatrix inversedScaledMatrix = (DenseMatrix)scaledMatrix.Inverse();
			this.MultiplierMatrix = inversedScaledMatrix;
		}
	}
}
