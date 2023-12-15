#region license

// AoC2023 - AoC2023 - Vertex.cs
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
using AoC2023._2023;

namespace AoC2023;

public enum Direction {
    Up,
    Down,
    Left,
    Right
}

public readonly record struct Vertex<TNumber>(TNumber X, TNumber Y) where TNumber: INumber<TNumber> {
    public override string ToString() {
        return $"({X}, {Y})";
    }
    
    public void Deconstruct(out TNumber x, out TNumber y) {
        x = X;
        y = Y;
    }
    
    public bool ExistsInGrid<U>(U width, U height) where U: INumber<U> {
        var w = TNumber.CreateChecked(width);
        var h = TNumber.CreateChecked(height);
        return X >= TNumber.Zero && X < w && Y >= TNumber.Zero && Y < h;
    }
    
    public Vertex<TNumber> GetNeighbor(Direction direction) {
        return direction switch {
            Direction.Up    => new Vertex<TNumber>(X, Y - TNumber.One),
            Direction.Down  => new Vertex<TNumber>(X, Y + TNumber.One),
            Direction.Left  => new Vertex<TNumber>(X - TNumber.One, Y),
            Direction.Right => new Vertex<TNumber>(X + TNumber.One, Y),
            _                   => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
        };
    }
    
    public TNumber ManhattanDistanceTo(Vertex<TNumber> other) {
        return TNumber.Abs(X - other.X) + TNumber.Abs(Y - other.Y);
    }
}
