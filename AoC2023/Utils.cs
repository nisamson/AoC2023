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
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AoC2023;

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
            
            if (!comparer.Equals(item, first)) {
                return false;
            }
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
        foreach (var item in source) {
            action(item);
        }
    }
    
    public static void Consume<T>(this IEnumerable<T> source, Action<T, int> action) {
        var i = 0;
        foreach (var item in source) {
            action(item, i++);
        }
    }
    
    public static TNumeric Product<TNumeric>(this IEnumerable<TNumeric> source) where TNumeric: INumber<TNumeric> {
        return source.Aggregate(TNumeric.One, (current, item) => current * item);
    }
    
    public static IEnumerable<long> Range(long start, long count) {
        var max = start + count - 1;
        switch (count) {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count));
            case 0:
                yield break;
        }

        for (var i = start; i <= max; i++) {
            yield return i;
        }
    }
    
    public static IDictionary<T, U> ToDictionary<T, U>(this IEnumerable<(T, U)> source) where T : notnull {
        return source.ToDictionary(x => x.Item1, x => x.Item2);
    }
    
    public static T Identity<T>(T x) => x;
    
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
        
        while (i < source.Length) {
            yield return Splitter(source);
        }
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
}

public class Grid<TItem> {
    
    // Row-major order
    private TItem[] items;
    public int Width { get; }
    public int Height { get; }
    public int Size => Width * Height;
    
    public Grid(int width, int height, IEnumerable<TItem>? items = null) {
        Width = width;
        Height = height;
        this.items = new TItem[Size];
        if (items == null) {
            return;
        }

        var i = 0;
        foreach (var item in items) {
            this.items[i++] = item;
        }
    }
    
    public TItem this[int x, int y] {
        get => items[x + y * Width];
        set => items[x + y * Width] = value;
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
}

public readonly struct StrideSpan<TItem>: IEnumerable<TItem> {
    private readonly TItem[] items;
    private readonly int stride;
    private readonly int offset;
    private readonly int count;
    public StrideSpan(TItem[] items, int stride, int offset, int count) {
        if (stride < 1) {
            throw new ArgumentOutOfRangeException(nameof(stride));
        }
        
        if (offset < 0 || offset >= items.Length) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        
        if (count < 0 || offset + count * stride > items.Length) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        
        this.items = items;
        this.stride = stride;
        this.offset = offset;
        this.count = count;
    }

    public TItem this[int index] {
        get => items[CheckBounds(offset + index * stride)];
        set => items[CheckBounds(offset + index * stride)] = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CheckBounds(int index) {
        if (index < 0 || index >= count) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index;
    }
    
    public int Length => count;
    public IEnumerator<TItem> GetEnumerator() {
        for (var i = 0; i < count; i++) {
            yield return this[i];
        }
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