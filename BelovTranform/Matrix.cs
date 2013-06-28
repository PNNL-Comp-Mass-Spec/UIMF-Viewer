using System;
using System.Collections.Generic;
using System.Text;

namespace Belov_Transform
{
    class Matrix
    {
        public int nRow_, nCol_;  // Number of rows, columns
        public double[] data_;     // Pointer used to allocate memory for data.

        public Matrix()
        {
            nRow_ = 1; nCol_ = 1;
            data_ = new double[1];  // Allocate memory
            set(0.0);                // Set value of data_[0] to 0.0
        }

        // Regular Constructor. Creates an nR by nC matrix; sets values to zero.
        // If number of columns is not specified, it is set to 1.
        public Matrix(int nR)
        {
            nRow_ = nR;
            nCol_ = 1;
            data_ = new double[nR];  // Allocate memory

            set(0.0);                    // Set values of data_[] to 0.0
        }

        public Matrix(int nR, int nC)
        {
            nRow_ = nR;
            nCol_ = nC;
            data_ = new double[nR * nC];  // Allocate memory

            set(0.0);                    // Set values of data_[] to 0.0
        }


        public void Reallocate()
        {
            data_ = new double[nRow_ * nCol_];  // Allocate memory

            set(0.0);
        }

#if false
	    // Copy Constructor.
	    // Used when a copy of an object is produced 
	    // (e.g., passing to a function by value)
	    Matrix(const Matrix& mat) {
	      this->copy(mat);   // Call private copy function.
	    }

	    // Destructor. Called when a Matrix object goes out of scope.
	    ~Matrix() {
	      delete [] data_;   // Release allocated memory
	    }

	    // Assignment operator function.
	    // Overloads the equal sign operator to work with
	    // Matrix objects.
	    Matrix& operator=(const Matrix& mat) {
	      if( this == &mat ) return *this;  // If two sides equal, do nothing.
	      delete [] data_;                  // Delete data on left hand side
	      this->copy(mat);                  // Copy right hand side to l.h.s.
	      return *this;
	    }
#endif

        // Simple "get" functions. Return number of rows or columns.
        public int nRow
        {
            get { return nRow_; }
        }
        public int nCol
        {
            get { return nCol_; }
        }

        // Set function. Sets all elements of a matrix to a given value.
        public void set(double value)
        {
            int i;
            int iData = nRow_ * nCol_;

            for (i = 0; i < iData; i++)
                data_[i] = value;
        }
        	    
        void set_entry(int i, double value) 
        {
            data_[ nCol_*(i-1)] = value;  // Access appropriate value
	    }

        void set_entry(int i, int j, double value)
        {
            data_[nCol_ * (i - 1) + (j - 1)] = value;  // Access appropriate value
        }


        double get_entry(int i, int j)
        {
            return data_[nCol_ * (i - 1) + (j - 1)];  // Access appropriate value
        }

        double get_entry(int i)
        {
            return data_[nCol_ * (i - 1)];
        }

#if false
	    // Parenthesis operator function.
	    // Allows access to values of Matrix via (i,j) pair.
	    // Example: a(1,1) = 2*b(2,3); 
	    // If column is unspecified, take as 1.
	    double& operator() (int i, int j = 1) {
	      assert(i > 0 && i <= nRow_);          // Bounds checking for rows
	      assert(j > 0 && j <= nCol_);          // Bounds checking for columns
	      return data_[ nCol_*(i-1) + (j-1) ];  // Access appropriate value
	    }

	    // Parenthesis operator function (const version).
	    const double& operator() (int i, int j = 1) const{
	      assert(i > 0 && i <= nRow_);          // Bounds checking for rows
	      assert(j > 0 && j <= nCol_);          // Bounds checking for columns
	      return data_[ nCol_*(i-1) + (j-1) ];  // Access appropriate value
	    }

	    // Private copy function.
	    // Copies values from one Matrix object to another.
	    void copy(const Matrix& mat) {
	      nRow_ = mat.nRow_;
	      nCol_ = mat.nCol_;
	      int i, iData = nRow_*nCol_;
	      data_ = new double [iData];
	      for(i = 0; i<iData; i++ )
		    data_[i] = mat.data_[i];
	    }
#endif

    // Compute inverse of matrix
        public double inv(Matrix A, out Matrix Ainv)
        // Input
        //    A    -    Matrix A (N by N)
            //
        // Outputs
        //   Ainv  -    Inverse of matrix A (N by N)
        //  determ -    Determinant of matrix A	(return value)
        {
            int N = A.nRow;

            int i, j, k;
            Matrix scale = new Matrix(N);
            Matrix b = new Matrix(N, N);	 // Scale factor and work array
            int[] index = new int[N + 1];

            Ainv = new Matrix(A.nRow, A.nCol);
            int iData = nRow_ * nCol_;
            for (i = 0; i < iData; i++)
                Ainv.data_[i] = A.data_[i];

            //* Matrix b is initialized to the identity matrix
            b.set(0.0);
            for (i = 1; i <= N; i++)
                b.set_entry(i, i, 1.0);

            //* Set scale factor, scale(i) = max( |a(i,j)| ), for each row

            for (i = 1; i <= N; i++)
            {
                index[i] = i;			  // Initialize row index list
                double scalemax = 0.0;
                for (j = 1; j <= N; j++)
                    scalemax = (scalemax > Math.Abs(A.get_entry(i, j))) ? scalemax : Math.Abs(A.get_entry(i, j));
                scale.set_entry(i, scalemax);
            }

            //* Loop over rows k = 1, ..., (N-1)

            int signDet = 1;
            for (k = 1; k <= N - 1; k++)
            {
                //* Select pivot row from max( |a(j,k)/s(j)| )
                double ratiomax = 0.0;
                int jPivot = k;
                for (i = k; i <= N; i++)
                {
                    double ratio = Math.Abs(A.get_entry(index[i], k)) / scale.get_entry(index[i]);
                    if (ratio > ratiomax)
                    {
                        jPivot = i;
                        ratiomax = ratio;
                    }
                }

                //* Perform pivoting using row index list
                int indexJ = index[k];
                if (jPivot != k)
                {   // Pivot
                    indexJ = index[jPivot];
                    index[jPivot] = index[k];   // Swap index jPivot and k
                    index[k] = indexJ;
                    signDet *= -1;			  // Flip sign of determinant
                }

                //* Perform forward elimination
                for (i = k + 1; i <= N; i++)
                {
                    double coeff = A.get_entry(index[i], k) / A.get_entry(indexJ, k);
                    for (j = k + 1; j <= N; j++)
                        A.set_entry(index[i], j, A.get_entry(index[i], j) - coeff * A.get_entry(indexJ, j));
                    A.set_entry(index[i], k, coeff);
                    for (j = 1; j <= N; j++)
                        b.set_entry(index[i], j, b.get_entry(index[i], j) - A.get_entry(index[i], k) * b.get_entry(indexJ, j));
                }
            }

            //* Compute determinant as product of diagonal elements
            double determ = signDet;	   // Sign of determinant
            for (i = 1; i <= N; i++)
                determ *= A.get_entry(index[i], i);

            //* Perform backsubstitution
            for (k = 1; k <= N; k++)
            {
                Ainv.set_entry(N, k, b.get_entry(index[N], k) / A.get_entry(index[N], N));
                for (i = N - 1; i >= 1; i--)
                {
                    double sum = b.get_entry(index[i], k);
                    for (j = i + 1; j <= N; j++)
                        sum -= A.get_entry(index[i], j) * Ainv.get_entry(j, k);
                    Ainv.set_entry(i, k, sum / A.get_entry(index[i], i));
                }
            }

            return (determ);
        }
    }
}
