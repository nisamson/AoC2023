#region license

// AoC2023 - AoC2023 - Utils.cs
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

using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using MathNet.Numerics.LinearAlgebra;

namespace AoC.Support;

public static class IterUtils {
    public static bool AllEqual<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) {
        comparer ??= EqualityComparer<T>.Default;
        T? first = default;
        var sawFirst = false;
        foreach (var item in source) {
            if (!sawFirst) {
                sawFirst = true;
                first = item;
                continue;
            }

            if (!comparer.Equals(item, first)) return false;
        }

        return true;
    }

    public static IEnumerable<T> Observe<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) {
            action(item);
            yield return item;
        }
    }

    public static IEnumerable<T> Observe<T>(this IEnumerable<T> source, Action<T, int> action) {
        var i = 0;
        foreach (var item in source) {
            action(item, i++);
            yield return item;
        }
    }

    public static void Consume<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) action(item);
    }

    public static void Consume<T>(this ParallelQuery<T> source, Action<T> action) {
        source.ForAll(action);
    }

    public static void Consume<T>(this IEnumerable<T> source, Action<T, int> action) {
        var i = 0;
        foreach (var item in source) action(item, i++);
    }

    public static TNumeric Product<TNumeric>(this IEnumerable<TNumeric> source) where TNumeric : INumber<TNumeric> {
        return source.Aggregate(TNumeric.One, (current, item) => current * item);
    }

    public static TNumeric Product<TSource, TNumeric>(this IEnumerable<TSource> source,
        Func<TSource, TNumeric> selector)
        where TNumeric : INumber<TNumeric> {
        return source.Select(selector).Product();
    }

    public static IEnumerable<long> Range(long start, long count) {
        var max = start + count - 1;
        switch (count) {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count));
            case 0:
                yield break;
        }

        for (var i = start; i <= max; i++) yield return i;
    }

    public static IDictionary<T, U> ToDictionary<T, U>(this IEnumerable<(T, U)> source) where T : notnull {
        return source.ToDictionary(x => x.Item1, x => x.Item2);
    }

    public static T Identity<T>(T x) {
        return x;
    }

    public static IEnumerable<(T, T)> CartesianProduct<T>(this IEnumerable<T> source) {
        var list = source.ToList();
        return CartesianProduct(list);
    }

    public static IEnumerable<(T, T)> CartesianProduct<T>(this IReadOnlyList<T> source) {
        for (var i = 0; i < source.Count; i++) {
            for (var j = i + 1; j < source.Count; j++) yield return (source[i], source[j]);
        }
    }

    public static IEnumerable<IEnumerable<string>> SplitByEmptyLines(this string[] source) {
        var i = 0;

        IEnumerable<string> Splitter(string[] source) {
            for (; i < source.Length; i++) {
                var s = source[i];
                if (string.IsNullOrEmpty(s)) {
                    i++;
                    yield break;
                }

                yield return s;
            }
        }

        while (i < source.Length) yield return Splitter(source);
    }

    public static IEnumerable<T> Once<T>(this T item) {
        yield return item;
    }

    public static IEnumerable<T> EnumerateRow<T>(this T[,] source, int row) {
        for (var i = 0; i < source.GetLength(1); i++) yield return source[row, i];
    }

    public static IEnumerable<T> EnumerateColumn<T>(this T[,] source, int column) {
        for (var i = 0; i < source.GetLength(0); i++) yield return source[i, column];
    }

    public static IEnumerable<IEnumerable<T>> EnumerateRows<T>(this T[,] source) {
        for (var i = 0; i < source.GetLength(0); i++) yield return source.EnumerateRow(i);
    }

    public static IEnumerable<IEnumerable<T>> EnumerateColumns<T>(this T[,] source) {
        for (var i = 0; i < source.GetLength(1); i++) yield return source.EnumerateColumn(i);
    }

    public static IEnumerable<T> EnumerateRowMajor<T>(this T[,] source) {
        for (var i = 0; i < source.GetLength(0); i++) {
            for (var j = 0; j < source.GetLength(1); j++) yield return source[i, j];
        }
    }

    public static IEnumerable<T> EnumerateColumnMajor<T>(this T[,] source) {
        for (var i = 0; i < source.GetLength(1); i++) {
            for (var j = 0; j < source.GetLength(0); j++) yield return source[j, i];
        }
    }

    public static bool IsEmpty<T>(this IEnumerable<T> source) {
        return !source.Any();
    }

    public static bool IsEmpty<T>(this ICollection<T> source) {
        return source.Count == 0;
    }

    public static bool IsEmpty<T>(this IList<T> source) {
        return source.Count == 0;
    }

    public static bool IsEmpty<T>(this IReadOnlyCollection<T> source) {
        return source.Count == 0;
    }

    public static bool IsEmpty<T>(this IReadOnlyList<T> source) {
        return source.Count == 0;
    }

    public static bool IsEmpty<T>(this T[] source) {
        return source.Length == 0;
    }

    public static bool IsEmpty<T>(this Span<T> source) {
        return source.IsEmpty;
    }

    public static bool IsEmpty<T>(this ReadOnlySpan<T> source) {
        return source.IsEmpty;
    }

    public static bool IsEmpty<T>(this Memory<T> source) {
        return source.IsEmpty;
    }

    public static bool IsEmpty<T>(this ReadOnlyMemory<T> source) {
        return source.IsEmpty;
    }

    public static bool IsNotEmpty<T>(this IEnumerable<T> source) {
        return source.Any();
    }

    public static bool IsNotEmpty<T>(this ICollection<T> source) {
        return source.Count != 0;
    }

    public static bool IsNotEmpty<T>(this IList<T> source) {
        return source.Count != 0;
    }

    public static bool IsNotEmpty<T>(this IReadOnlyCollection<T> source) {
        return source.Count != 0;
    }

    public static bool IsNotEmpty<T>(this IReadOnlyList<T> source) {
        return source.Count != 0;
    }

    public static bool IsNotEmpty<T>(this T[] source) {
        return source.Length != 0;
    }

    public static bool IsNotEmpty<T>(this Span<T> source) {
        return !source.IsEmpty;
    }

    public static bool IsNotEmpty<T>(this ReadOnlySpan<T> source) {
        return !source.IsEmpty;
    }

    public static bool IsNotEmpty<T>(this Memory<T> source) {
        return !source.IsEmpty;
    }

    public static bool IsNotEmpty<T>(this ReadOnlyMemory<T> source) {
        return !source.IsEmpty;
    }

    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Span<T> source) {
        return source;
    }

    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Memory<T> source) {
        return source.Span;
    }

    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this ReadOnlyMemory<T> source) {
        return source.Span;
    }

    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] source) {
        return source;
    }

    public static void EnqueueAll<T>(this Queue<T> queue, IEnumerable<T> items) {
        foreach (var item in items) queue.Enqueue(item);
    }
    
    public static T MiddleElement<T>(this IReadOnlyList<T> source) {
        return source[source.Count / 2];
    }
}

public static class MathUtils {
    public static long Gcd(long a, long b) {
        while (b != 0) {
            var t = b;
            b = a % b;
            a = t;
        }

        return a;
    }

    public static long Lcm(long a, long b) {
        return a / Gcd(a, b) * b;
    }

    public static T Pow<T>(this T x, int y) where T : INumber<T> {
        return Enumerable.Repeat(x, y).Product();
    }

    public static TOther IntoChecked<TOther, T>(this T x) where T : INumberBase<T> where TOther : INumberBase<TOther> {
        return TOther.CreateChecked(x);
    }

    public static TOther IntoTruncating<TOther, T>(this T x)
        where T : INumberBase<T> where TOther : INumberBase<TOther> {
        return TOther.CreateTruncating(x);
    }

    public static TOther IntoSaturating<TOther, T>(this T x)
        where T : INumberBase<T> where TOther : INumberBase<TOther> {
        return TOther.CreateSaturating(x);
    }

    public static Matrix<TNumeric> ToBoolean<TNumeric>(this Matrix<TNumeric> matrix)
        where TNumeric : struct, INumber<TNumeric> {
        return matrix.Map(x => x != TNumeric.Zero ? TNumeric.One : TNumeric.Zero);
    }

    public static Matrix<TNumeric> ToBooleanInplace<TNumeric>(this Matrix<TNumeric> matrix)
        where TNumeric : struct, INumber<TNumeric> {
        matrix.MapInplace(x => x != TNumeric.Zero ? TNumeric.One : TNumeric.Zero);
        return matrix;
    }

    public static byte[,] ToBytes<TNumeric>(this Matrix<TNumeric> matrix) where TNumeric : struct, INumber<TNumeric> {
        var res = new byte[matrix.RowCount, matrix.ColumnCount];
        matrix.EnumerateIndexed(Zeros.AllowSkip).AsParallel().ForAll(
            t => {
                var i = t.Item1;
                var j = t.Item2;
                var x = t.Item3;
                res[i, j] = byte.CreateChecked(x);
            }
        );

        return res;
    }

    public static Matrix<TNumeric> ToMatrix<TNumeric>(this byte[,] matrix) where TNumeric : struct, INumber<TNumeric> {
        var res = Matrix<TNumeric>.Build.Dense(matrix.GetLength(0), matrix.GetLength(1));
        for (var i = 0; i < matrix.GetLength(0); i++) {
            for (var j = 0; j < matrix.GetLength(1); j++) res[i, j] = TNumeric.CreateChecked(matrix[i, j]);
        }

        return res;
    }

    public static double HarmonicMean<TNumeric>(this IEnumerable<TNumeric> n) where TNumeric : INumber<TNumeric> {
        var info = n.GroupBy(_ => 1)
            .Select(grp => new { Count = grp.Count(), Sum = grp.Sum(i => 1f / double.CreateChecked(i)) });
        return info.Sum(i => i.Count / i.Sum);
    }

    public static double GeometricMean<TNumeric>(this IEnumerable<TNumeric> n) where TNumeric : INumber<TNumeric> {
        var info = n
            .Observe(
                n => { ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, TNumeric.Zero); }
            )
            .Select(double.CreateChecked)
            .GroupBy(_ => 1)
            .Select(grp => new { Count = grp.Count(), LogSum = grp.Sum(Math.Log) })
            .FirstOrDefault(new { Count = 0, LogSum = 0.0 });
        ArgumentOutOfRangeException.ThrowIfLessThan(info.Count, 1);
        return Math.Exp(info.LogSum / info.Count).CheckedNotNaN();
    }

    public static TNumeric CheckedNotNaN<TNumeric>(this TNumeric n) where TNumeric : INumber<TNumeric> {
        // ReSharper disable once EqualExpressionComparison
        if (n != n) throw new ArithmeticException("Found NaN");

        return n;
    }

    public static double GeometricMean<TNumeric>(params TNumeric[] n) where TNumeric : INumber<TNumeric> {
        return GeometricMean(new ReadOnlySpan<TNumeric>(n));
    }

    public static double GeometricMean<TNumeric>(ReadOnlySpan<TNumeric> n) where TNumeric : INumber<TNumeric> {
        var cnt = n.Length;
        var prod = TNumeric.One;
        ArgumentOutOfRangeException.ThrowIfLessThan(cnt, 1, "n.Length");
        foreach (var x in n) {
            if (x <= TNumeric.Zero) throw new ArgumentOutOfRangeException(nameof(n), x, "Must be positive");

            prod *= x;
        }

        return Math.Pow(double.CreateSaturating(prod), 1 / (double)cnt);
    }

    public static TNumeric BooleanToNumeric<TNumeric>(this bool b) where TNumeric : INumber<TNumeric> {
        return b ? TNumeric.One : TNumeric.Zero;
    }
}

public class Grid<TItem> {
    // Row-major order
    private readonly TItem[] items;

    public Grid(int width, int height, IEnumerable<TItem>? items = null) {
        Width = width;
        Height = height;
        this.items = new TItem[Size];
        if (items == null) return;

        var i = 0;
        foreach (var item in items) this.items[i++] = item;
    }

    public int Width { get; }
    public int Height { get; }
    public int Size => Width * Height;

    public TItem this[int x, int y] {
        get => items[x + y * Width];
        set => items[x + y * Width] = value;
    }
    
    public bool IsInBounds(int x, int y) {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    public bool IsInBounds(Vertex<int> coords) {
        return IsInBounds(coords.X, coords.Y);
    }
    
    public TItem this[Vertex<int> coords] {
        get => this[coords.X, coords.Y];
        set => this[coords.X, coords.Y] = value;
    }

    public Span<TItem> GetRow(int y) {
        return items.AsSpan(y * Width, Width);
    }

    public StrideSpan<TItem> GetColumn(int x) {
        return new StrideSpan<TItem>(items, Width, x, Height);
    }
    
    public Grid<TItem> Clone() {
        var clone = new Grid<TItem>(Width, Height);
        items.CopyTo(clone.items, 0);
        return clone;
    }
    
    public IEnumerable<(Vertex<int> Coords, TItem Item)> EnumerateIndexed() {
        for (var y = 0; y < Height; y++) {
            for (var x = 0; x < Width; x++) {
                yield return (new Vertex<int>(x, y), this[x, y]);
            }
        }
    }
    
    public ReadOnlyMemory2D<TItem> AsMemory2D() {
        return new(items, Height, Width);
    }
    
    public ReadOnlyMemory<TItem> AsMemory() {
        return items.AsMemory();
    }

    public IEnumerable<TItem> RowMajorItems() => items;

    public override string ToString() {
        var sb = new StringBuilder();
        for (var y = 0; y < Height; y++) {
            var row = GetRow(y);
            foreach (var item in row) {
                sb.Append(item);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public void CopyFrom(Grid<TItem> other) {
        if (other.Width != Width || other.Height != Height) {
            throw new ArgumentException("Grids must have the same dimensions");
        }
        
        other.items.CopyTo(items, 0);
    }
}

public readonly struct StrideSpan<TItem> : IEnumerable<TItem> {
    private readonly TItem[] items;
    private readonly int stride;
    private readonly int offset;

    public StrideSpan(TItem[] items, int stride, int offset, int count) {
        if (stride < 1) throw new ArgumentOutOfRangeException(nameof(stride));

        if (offset < 0 || offset >= items.Length) throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0 || offset + count * stride > items.Length) throw new ArgumentOutOfRangeException(nameof(count));

        this.items = items;
        this.stride = stride;
        this.offset = offset;
        this.Length = count;
    }

    public TItem this[int index] {
        get => items[CheckBounds(offset + index * stride)];
        set => items[CheckBounds(offset + index * stride)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CheckBounds(int index) {
        if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));

        return index;
    }

    public int Length { get; }

    public IEnumerator<TItem> GetEnumerator() {
        for (var i = 0; i < Length; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}

public readonly struct SingletonEnumerable<T>(T item) : IEnumerable<T>
    where T : notnull {
    public IEnumerator<T> GetEnumerator() {
        yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}