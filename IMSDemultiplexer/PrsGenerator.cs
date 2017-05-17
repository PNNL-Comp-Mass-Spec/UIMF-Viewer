using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMSDemultiplexer
{
	public class PrsGenerator
	{
		/// <summary>
		///  Construction of S-Matrices using maximal length shift-register sequences detailed in the Appendix of Hadamard Transform Optics.
		///  First row of S_n is taken to be a maximal length shift-register sequence of length 2^m - 1.
		///  Utilizes "binary primitive polynomials" of degree m.
		///  The book details a means to obtain S matrices of up to 2^20 - 1. 
		///  This program only considers 2^8-1.
		/// </summary>
		/// <param name="bitShift"></param>
		/// <returns name="List<int>"></returns>
		public static List<int> SequenceGenerator(int bitShift)
		{
			int sequenceLength = (int)Math.Pow(2, bitShift) - 1;
			List<int> prs1D = new List<int>(sequenceLength);
			for (int i = 0; i < sequenceLength; i++)
			{
				prs1D.Insert(i, 0);
			}


			prs1D[bitShift - 1] = 1;

			for (int i = bitShift; i < sequenceLength; i++)
			{
				switch (bitShift)
				{
					case 3:
						prs1D[i] = (prs1D[i - bitShift]) ^ (prs1D[i - bitShift + 1]);
						break;
					case 4: prs1D[i] = (prs1D[i - bitShift]) ^ (prs1D[i - bitShift + 1]);
						break;
					case 5:
						prs1D[i] = (prs1D[i - bitShift]) ^ (prs1D[i - bitShift + 2]);
						break;
					case 6:
						prs1D[i] = (prs1D[i - bitShift]) ^ (prs1D[i - bitShift + 1]);
						break;
					case 7:
						prs1D[i] = (prs1D[i - bitShift]) ^ (prs1D[i - bitShift + 1]);
						break;
					case 8:
						prs1D[i] = (prs1D[i - bitShift]) ^ ((prs1D[i - bitShift + 1]) ^ ((prs1D[i - bitShift + 5]) ^
								   (prs1D[i - bitShift + 6])));
						break;
					default:
						break;
				}

			}

			return prs1D;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitShift"></param>
		/// <param name="oversampling"></param>
		/// <returns></returns>
		public static List<int> GenerateOversampledPRS(int bitShift, int oversampling)
		{
			int sequenceLength = (int)Math.Pow(2, bitShift) - 1;
			int overSampleLength = sequenceLength * oversampling;
			List<int> prs = new List<int>(sequenceLength); // pseudo random sequence vector.
			List<int> prsOS = new List<int>(overSampleLength); // vector containing the pseudorandom sequence multiplexed.
			int n = 0;

			for (int i = 0; i < prsOS.Capacity; i++)
			{
				prsOS.Insert(i, 0);
			}

			prs = SequenceGenerator(bitShift);

			for (int i = 0; i < overSampleLength - 1; i++)
			{
				if ((i % oversampling) == 0)
				{
					prsOS[i + (oversampling - 1)] = prs[n];
					n += 1;
				}
			}

			return prsOS;
		}

	}
}
