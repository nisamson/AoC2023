#region license

// AoC2023 - AoC.Support - DenseRowMajorMatrixStorage.cs
// Copyright (C) 2023 Nicholas
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using CommunityToolkit.HighPerformance;

namespace AoC.Support.Matrix;

public class DenseRowMajorMatrixStorage<T> : MathNet.Numerics.LinearAlgebra.Storage.MatrixStorage<T> where T : struct, IEquatable<T>, IFormattable {
    private readonly T[,] data;

    public DenseRowMajorMatrixStorage(int rowCount, int columnCount, T v = default) : base(rowCount, columnCount) {
        data = new T[rowCount, columnCount];
        if (v.Equals(default)) return;

        Span2D<T> span = data;
        span.Fill(v);
    }

    private DenseRowMajorMatrixStorage(T[,] data) : base(data.GetLength(0), data.GetLength(1)) {
        this.data = data;
    }

    public override bool IsDense => true;
    public override bool IsFullyMutable => true;

    public static DenseRowMajorMatrixStorage<T> OfStorage(MathNet.Numerics.LinearAlgebra.Storage.MatrixStorage<T> matrix) {
        // using Clone skips initialization and just copies the data directly
        return new DenseRowMajorMatrixStorage<T>(matrix.AsArray().Clone() as T[,] ?? matrix.ToArray());
    }

    public static DenseRowMajorMatrixStorage<T> OfMatrix(MathNet.Numerics.LinearAlgebra.Matrix<T> matrix) {
        return OfStorage(matrix.Storage);
    }

    public override bool IsMutableAt(int row, int column) {
        return true;
    }

    public override T At(int row, int column) {
        return data[row, column];
    }

    public override void At(int row, int column, T value) {
        data[row, column] = value;
    }

    public override void Clear() {
        Array.Clear(data);
    }

    public override T[,] AsArray() {
        return data;
    }

    public override T[,] ToArray() {
        return data.Clone() as T[,] ?? throw new InvalidOperationException();
    }
}

public static class DenseRowMajorMatrixStorage {
    public static DenseRowMajorMatrixStorage<T> OfMatrix<T>(MathNet.Numerics.LinearAlgebra.Matrix<T> matrix)
        where T : struct, IEquatable<T>, IFormattable {
        return DenseRowMajorMatrixStorage<T>.OfMatrix(matrix);
    }

    public static DenseRowMajorMatrixStorage<T> OfStorage<T>(MathNet.Numerics.LinearAlgebra.Storage.MatrixStorage<T> matrix)
        where T : struct, IEquatable<T>, IFormattable {
        return DenseRowMajorMatrixStorage<T>.OfStorage(matrix);
    }
}