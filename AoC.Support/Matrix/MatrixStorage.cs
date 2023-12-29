#region license
// AoC2023 - AoC.Support - MatrixStorage.cs
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

using System.Numerics;
using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Runtime;

namespace AoC.Support.Matrix;

public abstract class MatrixStorage<T> where T: unmanaged, INumber<T> {
    
    public abstract T this[int row, int column] {
        get;
        set;
    }
    
    public int RowCount { get; }
    public int ColumnCount { get; }
    
    protected MatrixStorage(int rowCount, int columnCount) {
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount, nameof(rowCount));
        ArgumentOutOfRangeException.ThrowIfNegative(columnCount, nameof(columnCount));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long) rowCount * (long) columnCount, int.MaxValue, "rowCount and columnCount");
        
        RowCount = rowCount;
        ColumnCount = columnCount;
    }
    
    public abstract bool IsMutableAt(int row, int column);
    public abstract MatrixStorage<T> Clone();
    
    // public 
}
