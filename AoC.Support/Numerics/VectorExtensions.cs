#region license

// AoC2023 - AoC.Support - VectorExtensions.cs
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
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CommunityToolkit.HighPerformance;

namespace AoC.Support.Numerics;

public static class VectorExtensions {
    private const int LoopUnroll = 8;

    private static readonly byte[] Avx2LookupArray = [
        0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4
    ];

    private static readonly byte[] Lookup8Bit =
        Enumerable.Range(0, byte.MaxValue + 1)
            .Select(byte.CreateTruncating)
            .Select(byte.PopCount)
            .ToArray();

    private static readonly Vector256<byte> Avx2Lookup = Vector256.Create(Avx2LookupArray);
    private static readonly Vector256<byte> Avx2LowMask = Vector256.Create((byte)0xf);
    private static readonly int Vector256UlongCount = Vector256<ulong>.Count;
    private static readonly int Vector256ByteCount = Vector256<byte>.Count;

    private static readonly Vector128<byte> Ssse3Lookup = Vector128.Create(Avx2LookupArray);
    private static readonly Vector128<byte> Ssse3LowMask = Vector128.Create((byte)0xf);
    private static readonly int Vector128UlongCount = Vector128<ulong>.Count;
    private static readonly int Vector128ByteCount = Vector128<byte>.Count;

    public static ulong PopCount<TNumeric>(this Span<TNumeric> span)
        where TNumeric : unmanaged, IBinaryInteger<TNumeric> {
        return PopCount((ReadOnlySpan<TNumeric>)span);
    }

    public static ulong PopCount<TNumeric>(this ReadOnlySpan<TNumeric> span)
        where TNumeric : unmanaged, IBinaryInteger<TNumeric> {
        if (Avx2.IsSupported) {
            Debug.WriteLine("Using AVX2");
            if (span.AsBytes().Length > 512) {
                Debug.WriteLine("Using AVX2 alternate as it has slightly better performance for large spans");
                return PopCountAvx2Alternate(span.AsBytes());
            }

            return PopCountAvx2(span.AsBytes());
        }

        if (Ssse3.IsSupported) {
            Debug.WriteLine("Using SSE3S");
            return PopCountSse3s(span.AsBytes());
        }

        Debug.WriteLine("Using boring fallback");
        return span.BoringPopCount();
    }

    public static ulong BoringPopCount<TNumeric>(this ReadOnlySpan<TNumeric> span)
        where TNumeric : unmanaged, IBinaryInteger<TNumeric> {
        var count = 0ul;
        foreach (var value in span) count += ulong.CreateSaturating(TNumeric.PopCount(value));

        return count;
    }

    public static ulong PopCountAvx2(this ReadOnlySpan<byte> span) {
        var count = 0ul;

        var accumulator = Vector256<ulong>.Zero;

        // from https://github.com/WojciechMula/sse-popcount/blob/master/popcnt-avx2-lookup.cpp
        while (span.Length >= Vector256ByteCount) {
            var local = Vector256<byte>.Zero;
            var vec = Vector256.Create(span);
            var low = Avx2.And(vec, Avx2LowMask);
            var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
            var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
            var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
            local = Avx2.Add(popcnt1, local);
            local = Avx2.Add(popcnt2, local);
            accumulator = Avx2.Add(accumulator, Avx2.SumAbsoluteDifferences(local, Vector256<byte>.Zero).AsUInt64());
            span = span[Vector256ByteCount..];
        }

        for (var i = 0; i < Vector256UlongCount; i++) count += accumulator.GetElement(i);

        foreach (var b in span) count += Lookup8Bit[b];

        return count;
    }

    public static ulong PopCountAvx2Alternate(this ReadOnlySpan<byte> span) {
        var count = 0ul;

        var accumulator = Vector256<ulong>.Zero;

        // from https://github.com/WojciechMula/sse-popcount/blob/master/popcnt-avx2-lookup.cpp
        while (span.Length - LoopUnroll * Vector256ByteCount >= Vector256ByteCount) {
            var local = Vector256<byte>.Zero;
            for (var i = 0; i < LoopUnroll; i++) {
                var vec = Vector256.Create(span);
                var low = Avx2.And(vec, Avx2LowMask);
                var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
                var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
                var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
                local = Avx2.Add(popcnt1, local);
                local = Avx2.Add(popcnt2, local);
                span = span[Vector256ByteCount..];
            }

            accumulator = Avx2.Add(accumulator, Avx2.SumAbsoluteDifferences(local, Vector256<byte>.Zero).AsUInt64());
        }

        while (span.Length >= Vector256ByteCount) {
            var local = Vector256<byte>.Zero;
            var vec = Vector256.Create(span);
            var low = Avx2.And(vec, Avx2LowMask);
            var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
            var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
            var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
            local = Avx2.Add(popcnt1, local);
            local = Avx2.Add(popcnt2, local);
            span = span[Vector256ByteCount..];
            accumulator = Avx2.Add(accumulator, Avx2.SumAbsoluteDifferences(local, Vector256<byte>.Zero).AsUInt64());
        }

        for (var i = 0; i < Vector256UlongCount; i++) count += accumulator.GetElement(i);

        foreach (var b in span) count += Lookup8Bit[b];

        return count;
    }

    public static ulong PopCountAvx2ManualUnroll(this ReadOnlySpan<byte> span) {
        var count = 0ul;

        var accumulator = Vector256<ulong>.Zero;

        // from https://github.com/WojciechMula/sse-popcount/blob/master/popcnt-avx2-lookup.cpp
        while (span.Length - 8 * Vector256ByteCount >= Vector256ByteCount) {
            var local = Vector256<byte>.Zero;
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            Avx2Iter(ref span, ref local);
            accumulator = Avx2.Add(accumulator, Avx2.SumAbsoluteDifferences(local, Vector256<byte>.Zero).AsUInt64());
        }

        while (span.Length >= Vector256ByteCount) {
            var local = Vector256<byte>.Zero;
            var vec = Vector256.Create(span);
            var low = Avx2.And(vec, Avx2LowMask);
            var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
            var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
            var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
            local = Avx2.Add(popcnt1, local);
            local = Avx2.Add(popcnt2, local);
            span = span[Vector256ByteCount..];
            accumulator = Avx2.Add(accumulator, Avx2.SumAbsoluteDifferences(local, Vector256<byte>.Zero).AsUInt64());
        }

        for (var i = 0; i < Vector256UlongCount; i++) count += accumulator.GetElement(i);

        foreach (var b in span) count += Lookup8Bit[b];

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Avx2Iter(ref ReadOnlySpan<byte> span, ref Vector256<byte> local) {
        var vec = Vector256.Create(span);
        var low = Avx2.And(vec, Avx2LowMask);
        var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
        var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
        var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
        local = Avx2.Add(popcnt1, local);
        local = Avx2.Add(popcnt2, local);
        span = span[Vector256ByteCount..];
    }

    private static void Avx2IterNoInline(ref ReadOnlySpan<byte> span, ref Vector256<byte> local) {
        var vec = Vector256.Create(span);
        var low = Avx2.And(vec, Avx2LowMask);
        var hi = Avx2.And(Avx2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Avx2LowMask);
        var popcnt1 = Avx2.Shuffle(Avx2Lookup, low);
        var popcnt2 = Avx2.Shuffle(Avx2Lookup, hi);
        local = Avx2.Add(popcnt1, local);
        local = Avx2.Add(popcnt2, local);
        span = span[Vector256ByteCount..];
    }

    public static ulong PopCountSse3s(this ReadOnlySpan<byte> span) {
        var count = 0ul;

        var acc = Vector128<ulong>.Zero;

        while (span.Length < Vector128ByteCount) {
            var vec = Vector128.Create(span);
            var low = Sse2.And(vec, Ssse3LowMask);
            var hi = Sse2.And(Sse2.ShiftRightLogical(vec.AsInt16(), 4).AsByte(), Ssse3LowMask);
            var popcnt1 = Ssse3.Shuffle(Ssse3Lookup, low);
            var popcnt2 = Ssse3.Shuffle(Ssse3Lookup, hi);
            var local = Sse2.Add(Vector128<byte>.Zero, popcnt1);
            local = Sse2.Add(local, popcnt2);
            acc = Sse2.Add(acc, Sse2.SumAbsoluteDifferences(local, Vector128<byte>.Zero).AsUInt64());
            span = span[Vector128ByteCount..];
        }

        for (var i = 0; i < Vector128UlongCount; i++) count += acc.GetElement(i);

        foreach (var b in span) count += Lookup8Bit[b];

        return count;
    }
}