#region license

// AoC2023 - AoC.Support - BitMatrix.cs
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

using System.Diagnostics;
using System.Numerics;
using AoC.Support.Collections;
using CommunityToolkit.HighPerformance;
using ILGPU;

namespace AoC.Support.Matrix;

public class BitMatrix : ICloneable {
    private readonly BitArray[] data;
    public int RowCount => data.Length;
    public int ColumnCount => data[0].Count;

    public Index2D Size => new(RowCount, ColumnCount);

    public BitMatrix(int rows, int columns, bool defaultValue = false) {
        data = new BitArray[rows];
        for (var i = 0; i < rows; i++) {
            data[i] = new BitArray(columns, defaultValue);
        }
    }

    public static BitMatrix FromArray<TNumeric>(TNumeric[,] arr) where TNumeric : INumber<TNumeric> {
        var rows = arr.GetLength(0);
        var columns = arr.GetLength(1);
        var result = new BitMatrix(rows, columns);
        for (var i = 0; i < rows; i++) {
            for (var j = 0; j < columns; j++) {
                result[i, j] = arr[i, j] != TNumeric.Zero;
            }
        }
        
        return result;
    }

    public BitMatrix(Index2D size, bool defaultValue = false) : this(size.Y, size.X, defaultValue) { }

    public bool this[int row, int column] {
        get => data[row][column];
        set => data[row][column] = value;
    }

    public BitArray GetRow(int row) {
        return data[row];
    }

    public BitArray GetColumn(int column) {
        var result = new BitArray(RowCount);
        for (var i = 0; i < RowCount; i++) {
            result[i] = data[i][column];
        }

        return result;
    }

    public void CopyColumnInto(int column, BitArray bits) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(bits.Count, RowCount);
        for (var i = 0; i < RowCount; i++) {
            bits[i] = data[i][column];
        }
    }
    
    public void CopyRowInto(int row, BitArray bits) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(bits.Count, ColumnCount);
        for (var i = 0; i < ColumnCount; i++) {
            bits[i] = data[row][i];
        }
    }

    public void SetRow(int row, BitArray bits) {
        data[row] = bits;
    }

    public void SetColumn(int column, BitArray bits) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(bits.Count, RowCount);
        for (var i = 0; i < RowCount; i++) {
            data[i][column] = bits[i];
        }
    }

    private bool RowValid(int row) {
        return row >= 0 && row < RowCount;
    }

    private bool ColumnValid(int column) {
        return column >= 0 && column < ColumnCount;
    }

    public void SetRow(int row, bool value) {
        if (!RowValid(row)) {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        data[row].SetAll(value);
    }

    public void SetColumn(int column, bool value) {
        if (!ColumnValid(column)) {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        for (var i = 0; i < RowCount; i++) {
            data[i][column] = value;
        }
    }

    public void SetAll(bool value) {
        foreach (var row in data) {
            row.SetAll(value);
        }
    }

    public BitMatrix And(BitMatrix other) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Size, Size);
        for (var i = 0; i < RowCount; i++) {
            data[i].And(other.data[i]);
        }

        return this;
    }

    public BitMatrix Or(BitMatrix other) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Size, Size);
        for (var i = 0; i < RowCount; i++) {
            data[i].Or(other.data[i]);
        }

        return this;
    }

    public BitMatrix Xor(BitMatrix other) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Size, Size);
        for (var i = 0; i < RowCount; i++) {
            data[i].Xor(other.data[i]);
        }

        return this;
    }

    public BitMatrix Not() {
        for (var i = 0; i < RowCount; i++) {
            data[i].Not();
        }

        return this;
    }

    object ICloneable.Clone() {
        return Clone();
    }

    public BitMatrix Clone() {
        var result = new BitMatrix(RowCount, ColumnCount);
        for (var i = 0; i < RowCount; i++) {
            result.data[i].Or(data[i]);
        }

        return result;
    }

    public BitMatrix Transposed() {
        var result = new BitMatrix(ColumnCount, RowCount);
        for (var i = 0; i < RowCount; i++) {
            for (var j = 0; j < ColumnCount; j++) {
                result[j, i] = this[i, j];
            }
        }

        return result;
    }

    public BitMatrix Multiply(BitMatrix other) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(RowCount, other.ColumnCount);
        var yCol = new BitArray(other.RowCount);
        var dest = new BitMatrix(RowCount, other.ColumnCount);
        for (var x = 0; x < RowCount; x++) {
            var row = GetRow(x);
            for (var y = 0; y < other.ColumnCount; y++) {
                other.CopyColumnInto(y, yCol);
                dest[x, y] = row.DotProduct(yCol);
            }
        }

        return dest;
    }

    public override string ToString() {
        return $"BitMatrix({RowCount}, {ColumnCount})";
    }

    public int CountSetBits() {
        var result = 0;
        foreach (var row in data) {
            result += row.CountSetBits();
        }

        return result;
    }

    public void Clear() {
        foreach (var row in data) {
            row.Clear();
        }
    }

    public void ClearRow(int row) {
        data[row].Clear();
    }

    public void ClearColumn(int col) {
        for (var i = 0; i < RowCount; i++) {
            data[i][col] = false;
        }
    }
    
    public byte[,] ToBytes() {
        var result = new byte[RowCount, ColumnCount];
        for (var i = 0; i < RowCount; i++) {
            for (var j = 0; j < ColumnCount; j++) {
                result[i, j] = this[i, j].ToByte();
            }
        }

        return result;
    }

    public IEnumerable<(int row, int column, bool value)> EnumerateIndexed() {
        for (var i = 0; i < RowCount; i++) {
            for (var j = 0; j < ColumnCount; j++) {
                yield return (i, j, this[i, j]);
            }
        }
    }
}
