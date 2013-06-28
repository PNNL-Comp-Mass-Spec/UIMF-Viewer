using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace BelovTransform
{
	public class MatrixCreator
	{
		public static DenseMatrix CreateMatrixForDemultiplexing(String inputString)
		{
			int matrixLength = inputString.Length;
			DenseMatrix matrix = new DenseMatrix(matrixLength, matrixLength);
			List<double> inputAsList = inputString.Select(c => double.Parse(c.ToString())).ToList();

			for (int i = 0; i < matrixLength; i++)
			{
				for(int j = 0; j < matrixLength; j++)
				{
					int index = j - i;
					if (index < 0)
					{
						index += matrixLength;
					}

					matrix[i, j] = inputAsList[index];
				}
			}

			return matrix;
		}

		public static void PrintMatrix(DenseMatrix matrix, String fileLocation)
		{
			TextWriter textWriter = new StreamWriter(fileLocation);
			int rowCount = matrix.RowCount;
			int columnCount = matrix.ColumnCount;

			for(int i = 0; i < rowCount; i++)
			{
				for(int j = 0; j < columnCount; j++)
				{
					textWriter.Write(matrix[i, j] + ",");
				}
				textWriter.Write("\n");
			}

			textWriter.Close();
		}
	}
}
