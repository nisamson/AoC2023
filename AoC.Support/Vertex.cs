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

using System.Collections.Immutable;
using System.Numerics;
using NetTopologySuite.Geometries;

namespace AoC.Support;

public enum Direction {
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}

public static class DirectionExtensions {
    public static char ToChar(this Direction direction) {
        return direction switch {
            Direction.Up => '\u2191',
            Direction.Down => '\u2193',
            Direction.Left => '\u2190',
            Direction.Right => '\u2192',
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
        };
    }

    private static readonly Direction[] Directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];
    private static readonly Direction[] Left = [Direction.Left, Direction.Right, Direction.Down, Direction.Up];
    private static readonly Direction[] Right = [Direction.Right, Direction.Left, Direction.Up, Direction.Down];

    public static Direction TurnLeft(this Direction direction) {
        return Left[(int)direction];
    }

    public static Direction TurnRight(this Direction direction) {
        return Right[(int)direction];
    }

    public static Vertex<TNumber> ToVertex<TNumber>(this Direction direction) where TNumber : INumber<TNumber> {
        return direction switch {
            Direction.Up => new Vertex<TNumber>(TNumber.Zero, -TNumber.One),
            Direction.Down => new Vertex<TNumber>(TNumber.Zero, TNumber.One),
            Direction.Left => new Vertex<TNumber>(-TNumber.One, TNumber.Zero),
            Direction.Right => new Vertex<TNumber>(TNumber.One, TNumber.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
        };
    }

    public static Direction ParseRLUD(char c) {
        return c switch {
            'R' => Direction.Right,
            'L' => Direction.Left,
            'U' => Direction.Up,
            'D' => Direction.Down,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, "Invalid direction")
        };
    }

    public static Direction Opposite(this Direction dir) {
        return dir switch {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, "Invalid direction")
        };
    }
}

public readonly record struct Vertex<TNumber>(TNumber X, TNumber Y) where TNumber : INumber<TNumber> {
    public static readonly Vertex<TNumber> Zero = new(TNumber.Zero, TNumber.Zero);
    public static readonly Vertex<TNumber> One = new(TNumber.One, TNumber.One);

    public override string ToString() {
        return $"({X}, {Y})";
    }

    public void Deconstruct(out TNumber x, out TNumber y) {
        x = X;
        y = Y;
    }

    public static implicit operator (TNumber, TNumber)(Vertex<TNumber> vertex) {
        return (vertex.X, vertex.Y);
    }

    public static implicit operator Vertex<TNumber>((TNumber, TNumber) tuple) {
        return new Vertex<TNumber>(tuple.Item1, tuple.Item2);
    }

    // Y + X * height
    public TNumber ColumnMajorIndex(TNumber height) {
        return Y + X * height;
    }

    // Y * width + X
    public TNumber RowMajorIndex(TNumber width) {
        return X + Y * width;
    }

    public static Vertex<TNumber> FromColumnMajorIndex(TNumber index, TNumber height) {
        return new Vertex<TNumber>(index / height, index % height);
    }

    public static Vertex<TNumber> FromRowMajorIndex(TNumber index, TNumber width) {
        return new Vertex<TNumber>(index % width, index / width);
    }

    public static Vertex<TNumber> operator +(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new Vertex<TNumber>(left.X + right.X, left.Y + right.Y);
    }

    public static Vertex<TNumber> operator -(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new Vertex<TNumber>(left.X - right.X, left.Y - right.Y);
    }

    public static Vertex<TNumber> operator *(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new Vertex<TNumber>(left.X * right.X, left.Y * right.Y);
    }

    public static Vertex<TNumber> operator /(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new Vertex<TNumber>(left.X / right.X, left.Y / right.Y);
    }

    public static Vertex<TNumber> operator %(Vertex<TNumber> left, Vertex<TNumber> right) {
        return new Vertex<TNumber>(left.X % right.X, left.Y % right.Y);
    }

    public static Vertex<TNumber> operator +(Vertex<TNumber> left, TNumber right) {
        return new Vertex<TNumber>(left.X + right, left.Y + right);
    }

    public static Vertex<TNumber> operator -(Vertex<TNumber> left, TNumber right) {
        return new Vertex<TNumber>(left.X - right, left.Y - right);
    }

    public static Vertex<TNumber> operator -(Vertex<TNumber> left) {
        return new Vertex<TNumber>(-left.X, -left.Y);
    }

    public static Vertex<TNumber> operator *(Vertex<TNumber> left, TNumber right) {
        return new Vertex<TNumber>(left.X * right, left.Y * right);
    }

    public static Vertex<TNumber> operator /(Vertex<TNumber> left, TNumber right) {
        return new Vertex<TNumber>(left.X / right, left.Y / right);
    }

    public static Vertex<TNumber> operator %(Vertex<TNumber> left, TNumber right) {
        return new Vertex<TNumber>(left.X % right, left.Y % right);
    }

    public bool ExistsInGrid<U>(U width, U height) where U : INumber<U> {
        var w = TNumber.CreateChecked(width);
        var h = TNumber.CreateChecked(height);
        return X >= TNumber.Zero && X < w && Y >= TNumber.Zero && Y < h;
    }

    public Vertex<TNumber> GetNeighbor(Direction direction) {
        return direction switch {
            Direction.Up => this with { Y = Y - TNumber.One },
            Direction.Down => this with { Y = Y + TNumber.One },
            Direction.Left => this with { X = X - TNumber.One },
            Direction.Right => this with { X = X + TNumber.One },
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
        };
    }

    public TNumber ManhattanDistanceTo(Vertex<TNumber> other) {
        return TNumber.Abs(X - other.X) + TNumber.Abs(Y - other.Y);
    }

    public static IEnumerable<Vertex<TNumber>> TopEdgePoints(TNumber width) {
        for (var x = TNumber.Zero; x < width; x++) yield return new Vertex<TNumber>(x, TNumber.Zero);
    }

    public static IEnumerable<Vertex<TNumber>> BottomEdgePoints(TNumber width, TNumber height) {
        for (var x = TNumber.Zero; x < width; x++) yield return new Vertex<TNumber>(x, height - TNumber.One);
    }

    public static IEnumerable<Vertex<TNumber>> LeftEdgePoints(TNumber height) {
        for (var y = TNumber.Zero; y < height; y++) yield return new Vertex<TNumber>(TNumber.Zero, y);
    }

    public static IEnumerable<Vertex<TNumber>> RightEdgePoints(TNumber width, TNumber height) {
        for (var y = TNumber.Zero; y < height; y++) yield return new Vertex<TNumber>(width - TNumber.One, y);
    }


    public Vertex<UNumber> Convert<UNumber>() where UNumber : INumber<UNumber> {
        return new Vertex<UNumber>(UNumber.CreateChecked(X), UNumber.CreateChecked(Y));
    }

    public static implicit operator Coordinate(Vertex<TNumber> vertex) {
        return new Coordinate(double.CreateChecked(vertex.X), double.CreateChecked(vertex.Y));
    }

    public IEnumerable<Vertex<TNumber>> GetNeighbors() {
        yield return GetNeighbor(Direction.Up);
        yield return GetNeighbor(Direction.Down);
        yield return GetNeighbor(Direction.Left);
        yield return GetNeighbor(Direction.Right);
    }


    public IEnumerable<Vertex<TNumber>> GetDiagonals() {
        yield return new Vertex<TNumber>(X - TNumber.One, Y - TNumber.One);
        yield return new Vertex<TNumber>(X - TNumber.One, Y + TNumber.One);
        yield return new Vertex<TNumber>(X + TNumber.One, Y - TNumber.One);
        yield return new Vertex<TNumber>(X + TNumber.One, Y + TNumber.One);
    }

    public IEnumerable<Vertex<TNumber>> GetNeighborsWithDiagonals() {
        return GetNeighbors().Concat(GetDiagonals());
    }

    public static TNumber RowMajorIndex(Vertex<TNumber> v, TNumber width) {
        return v.RowMajorIndex(width);
    }

    public static TNumber ColumnMajorIndex(Vertex<TNumber> v, TNumber height) {
        return v.ColumnMajorIndex(height);
    }

    public record RowMajorIndexComparer(TNumber Rows) : IComparer<Vertex<TNumber>> {
        public int Compare(Vertex<TNumber> x, Vertex<TNumber> y) {
            return x.RowMajorIndex(Rows).CompareTo(y.RowMajorIndex(Rows));
        }
    }

    public record ColumnMajorIndexComparer(TNumber Columns) : IComparer<Vertex<TNumber>> {
        public int Compare(Vertex<TNumber> x, Vertex<TNumber> y) {
            return x.ColumnMajorIndex(Columns).CompareTo(y.ColumnMajorIndex(Columns));
        }
    }
}