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

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

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
