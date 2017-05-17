using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Algorithms.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Double.Factorization;
using MathNet.Numerics.LinearAlgebra.Double.Solvers.Iterative;
using DeconTools.Backend;
using DeconTools.Backend.Core;

namespace IMSDemultiplexer
{
	public class SavGolSmoother
	{

		public SavGolSmoother()
		{
			//MathNet.Numerics.Control.LinearAlgebraProvider =
			//	new MathNet.Numerics.Algorithms.LinearAlgebra.Mkl.MklLinearAlgebraProvider();
		}

		private static void sg(int polynomialOrder, int filterLength, out DenseMatrix smoothingFilters, out DenseMatrix s)
		{
			int m = (filterLength - 1)/2;
			s = new DenseMatrix(filterLength, polynomialOrder + 1);
			
			for (int i = -m; i <= m; i++)
			{
				for (int j = 0; j <= polynomialOrder; j++)
				{
					s[i + m, j] = Math.Pow(i, j);
				}
			}

			DenseMatrix sTranspose = (DenseMatrix) s.ConjugateTranspose();
			DenseMatrix f = sTranspose*s;
			DenseMatrix fInverse = (DenseMatrix) f.LU().Solve(DenseMatrix.Identity(f.ColumnCount));
			smoothingFilters = s*fInverse*sTranspose;
		}

		public static XYData sgfilt(int polynomialOrder, int filterLength, XYData xyData)
		{
			int m = (filterLength - 1)/2;
			double[] yValues = xyData.Yvalues;
			int colCount = yValues.Length;
			double[] returnYValues = new double[colCount];

			DenseMatrix smoothingFilters;
			DenseMatrix s;
		    sg(polynomialOrder, filterLength, out smoothingFilters, out s);

			var conjTransposeMatrix = smoothingFilters.ConjugateTranspose();

			for (int i = 0; i <= m ; i++)
			{
				var conjTransposeColumn = conjTransposeMatrix.Column(i);

				double multiplicationResult = 0;
				for (int z = 0; z < filterLength; z++)
				{
					multiplicationResult += (conjTransposeColumn[z] * yValues[z]);
				}
				returnYValues[i] = multiplicationResult;
			}

			var conjTransposeColumnResult = conjTransposeMatrix.Column(m);

			for (int i = m + 1; i < colCount - m - 1; i++)
			{
				double multiplicationResult = 0;
				for (int z = 0; z < filterLength; z++)
				{
					multiplicationResult += (conjTransposeColumnResult[z] * yValues[i - m + z]);
				}
				returnYValues[i] = multiplicationResult;
			}

			for (int i = 0; i <= m; i++)
			{
				var conjTransposeColumn = conjTransposeMatrix.Column(m + i);

				double multiplicationResult = 0;
				for (int z = 0; z < filterLength; z++)
				{
					multiplicationResult += (conjTransposeColumn[z] * yValues[colCount - filterLength + z]);
				}
				returnYValues[colCount - m - 1 + i] = multiplicationResult;
			}

			xyData.Yvalues = returnYValues;
			return xyData;
		}
	}
}
