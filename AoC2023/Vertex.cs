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
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}

public static class DirectionExtensions {
    public static char ToChar(this Direction direction) {
        return direction switch {
            Direction.Up    => '\u2191',
            Direction.Down  => '\u2193',
            Direction.Left  => '\u2190',
            Direction.Right => '\u2192',
            _                   => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction"),
        };
    }
    
    public static Vertex<TNumber> ToVertex<TNumber>(this Direction direction) where TNumber: INumber<TNumber> {
        return direction switch {
            Direction.Up    => new Vertex<TNumber>(TNumber.Zero, -TNumber.One),
            Direction.Down  => new Vertex<TNumber>(TNumber.Zero, TNumber.One),
            Direction.Left  => new Vertex<TNumber>(-TNumber.One, TNumber.Zero),
            Direction.Right => new Vertex<TNumber>(TNumber.One, TNumber.Zero),
            _                   => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction"),
        };
    }
}

public readonly record struct Vertex<TNumber>(TNumber X, TNumber Y) where TNumber: INumber<TNumber> {
    public override string ToString() {
        return $"({X}, {Y})";
    }
    
    public void Deconstruct(out TNumber x, out TNumber y) {
        x = X;
        y = Y;
    }
    
    public static implicit operator(TNumber, TNumber)(Vertex<TNumber> vertex) {
        return (vertex.X, vertex.Y);
    }
    
    public static implicit operator Vertex<TNumber>((TNumber, TNumber) tuple) {
        return new(tuple.Item1, tuple.Item2);
    }
    
    public static Vertex<TNumber> operator +(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new(left.X + right.X, left.Y + right.Y);
    }
    
    public static Vertex<TNumber> operator -(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new(left.X - right.X, left.Y - right.Y);
    }
    
    public static Vertex<TNumber> operator *(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new(left.X * right.X, left.Y * right.Y);
    }
    
    public static Vertex<TNumber> operator /(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new(left.X / right.X, left.Y / right.Y);
    }
    
    public static Vertex<TNumber> operator %(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new(left.X % right.X, left.Y % right.Y);
    }
    
    public static Vertex<TNumber> operator +(Vertex<TNumber> left, TNumber right) {
        return new(left.X + right, left.Y + right);
    }
    
    public static Vertex<TNumber> operator -(Vertex<TNumber> left, TNumber right) {
        return new(left.X - right, left.Y - right);
    }
    
    public static Vertex<TNumber> operator *(Vertex<TNumber> left, TNumber right) {
        return new(left.X * right, left.Y * right);
    }
    
    public static Vertex<TNumber> operator /(Vertex<TNumber> left, TNumber right) {
        return new(left.X / right, left.Y / right);
    }
    
    public static Vertex<TNumber> operator %(Vertex<TNumber> left, TNumber right) {
        return new(left.X % right, left.Y % right);
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
    
    public static IEnumerable<Vertex<TNumber>> TopEdgePoints(TNumber width) {
        for (var x = TNumber.Zero; x < width; x++) {
            yield return new Vertex<TNumber>(x, TNumber.Zero);
        }
    }
    
    public static IEnumerable<Vertex<TNumber>> BottomEdgePoints(TNumber width, TNumber height) {
        for (var x = TNumber.Zero; x < width; x++) {
            yield return new Vertex<TNumber>(x, height - TNumber.One);
        }
    }
    
    public static IEnumerable<Vertex<TNumber>> LeftEdgePoints(TNumber height) {
        for (var y = TNumber.Zero; y < height; y++) {
            yield return new Vertex<TNumber>(TNumber.Zero, y);
        }
    }
    
    public static IEnumerable<Vertex<TNumber>> RightEdgePoints(TNumber width, TNumber height) {
        for (var y = TNumber.Zero; y < height; y++) {
            yield return new Vertex<TNumber>(width - TNumber.One, y);
        }
    }
}
