#region license

// AoC2023 - AoC.Support.Test - VertexTest.cs
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

using MathNet.Numerics.LinearAlgebra.Single;

namespace AoC.Support.Test;

using Vertex = Vertex<int>;

[TestFixture]
[TestOf(typeof(Vertex))]
public class VertexTest {
    public const int MaxArraySideLength = 50;

    [Test]
    public void ColumnMajorOrderIndexing(
        [Random(1, MaxArraySideLength + 1, 10)]
        int height,
        [Random(1, MaxArraySideLength + 1, 10)]
        int width
    ) {
        var x = TestContext.CurrentContext.Random.Next(width);
        var y = TestContext.CurrentContext.Random.Next(height);
        var v = new Vertex(x, y);
        var index = v.ColumnMajorIndex(height);
        var (calcx, calcy) = Vertex.FromColumnMajorIndex(index, height);
        Assert.Multiple(
            () => {
                Assert.That(calcx, Is.EqualTo(x));
                Assert.That(calcy, Is.EqualTo(y));
                Assert.That(calcx, Is.InRange(0, width - 1));
                Assert.That(calcy, Is.InRange(0, height - 1));
            }
        );
    }

    [Test]
    public void RowMajorOrderIndexing(
        [Random(1, MaxArraySideLength + 1, 10)]
        int height,
        [Random(1, MaxArraySideLength + 1, 10)]
        int width
    ) {
        var x = TestContext.CurrentContext.Random.Next(width);
        var y = TestContext.CurrentContext.Random.Next(height);
        var v = new Vertex(x, y);
        var index = v.RowMajorIndex(width);
        var (calcx, calcy) = Vertex.FromRowMajorIndex(index, width);
        Assert.Multiple(
            () => {
                Assert.That(calcx, Is.EqualTo(x));
                Assert.That(calcy, Is.EqualTo(y));
                Assert.That(calcx, Is.InRange(0, width - 1));
                Assert.That(calcy, Is.InRange(0, height - 1));
            }
        );
    }

    [Test]
    public void ColumnMajorOrderMatrixIndexing(
        [Random(1, MaxArraySideLength + 1, 10)]
        int height,
        [Random(1, MaxArraySideLength + 1, 10)]
        int width
    ) {
        var x = TestContext.CurrentContext.Random.Next(width);
        var y = TestContext.CurrentContext.Random.Next(height);
        var v = new Vertex(x, y);
        var index = v.ColumnMajorIndex(height);
        var matrix = SparseMatrix.Create(height, width, 0);
        matrix[y, x] = 1;
        var columnMajor = matrix.ToColumnMajorArray();

        Assume.That(columnMajor.Length, Is.EqualTo(height * width));

        Assert.Multiple(
            () => {
                Assert.That(index, Is.InRange(0, columnMajor.Length - 1));
                Assert.That(columnMajor[index], Is.EqualTo(1));
            }
        );
    }

    [Test]
    public void RowMajorOrderMatrixIndexing(
        [Random(1, MaxArraySideLength + 1, 10)]
        int height,
        [Random(1, MaxArraySideLength + 1, 10)]
        int width
    ) {
        var x = TestContext.CurrentContext.Random.Next(width);
        var y = TestContext.CurrentContext.Random.Next(height);
        var v = new Vertex(x, y);
        var index = v.RowMajorIndex(width);
        var matrix = SparseMatrix.Create(height, width, 0);
        matrix[y, x] = 1;
        var rowMajor = matrix.ToRowMajorArray();

        Assume.That(rowMajor.Length, Is.EqualTo(height * width));

        Assert.Multiple(
            () => {
                Assert.That(index, Is.InRange(0, rowMajor.Length - 1));
                Assert.That(rowMajor[index], Is.EqualTo(1));
            }
        );
    }
}