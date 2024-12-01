#region license

// AoC2023 - AoC.Support - SimdUtils.cs
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

namespace AoC.Support;

public static class SimdUtils {
    public static readonly int VectorByteCount = Vector<byte>.Count;

    public static byte[,] CalculateWarshallAlgorithm(this byte[,] buf) {
        var n = buf.GetLength(0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(buf.GetLength(0), buf.GetLength(1));
        var rk = new byte[n, n];
        var rkm1 = new byte[n, n];
        Buffer.BlockCopy(buf, 0, rkm1, 0, buf.Length);

        var rkm1Span = new ReadOnlySpan2D<byte>(rkm1);
        var rkSpan = new Span2D<byte>(rk);

        for (var k = 0; k < n; k++) {
            for (var y = 0; y < n; y++) {
                var ySpan = rkm1Span.GetRowSpan(y);
                var ky = rkm1Span[k, y];
                var kyv = new Vector<byte>(ky);
                var kSpan = rkm1Span.GetRowSpan(k);
                var destSpan = rkSpan.GetRowSpan(y);
                var x = 0;
                for (; x < n - VectorByteCount; x += VectorByteCount) {
                    var xyv = new Vector<byte>(ySpan[x..(x + VectorByteCount)]);
                    var xkv = new Vector<byte>(kSpan[x..(x + VectorByteCount)]);

                    var newDest = xyv | (xkv & kyv);
                    newDest.CopyTo(destSpan[x..(x + VectorByteCount)]);
                }

                for (; x < n; x++) destSpan[x] = (byte)(ySpan[x] | (kSpan[x] & ky));
            }

            if (k < n - 1) Buffer.BlockCopy(rk, 0, rkm1, 0, rk.Length);
        }

        return rk;
    }

    public static byte[,] CalculateWarshallAlgorithmRowParallel(this byte[,] buf) {
        var n = buf.GetLength(0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(buf.GetLength(0), buf.GetLength(1));
        var rk = new byte[n, n];
        var rkm1 = new byte[n, n];
        Buffer.BlockCopy(buf, 0, rkm1, 0, buf.Length);

        for (var k = 0; k < n; k++) {
            var k1 = k;
            Enumerable.Range(0, n).AsParallel().AsUnordered().ForAll(
                y => {
                    var rkm1Span = new ReadOnlySpan2D<byte>(rkm1);
                    var rkSpan = new Span2D<byte>(rk);
                    var ySpan = rkm1Span.GetRowSpan(y);
                    var ky = rkm1Span[k1, y];
                    var kyv = new Vector<byte>(ky);
                    var kSpan = rkm1Span.GetRowSpan(k1);
                    var destSpan = rkSpan.GetRowSpan(y);
                    var x = 0;
                    for (; x < n - VectorByteCount; x += VectorByteCount) {
                        var xyv = new Vector<byte>(ySpan[x..(x + VectorByteCount)]);
                        var xkv = new Vector<byte>(kSpan[x..(x + VectorByteCount)]);

                        var newDest = xyv | (xkv & kyv);
                        newDest.CopyTo(destSpan[x..(x + VectorByteCount)]);
                    }

                    for (; x < n; x++) destSpan[x] = (byte)(ySpan[x] | (kSpan[x] & ky));
                }
            );

            if (k < n - 1) Buffer.BlockCopy(rk, 0, rkm1, 0, rk.Length);
        }

        return rk;
    }

    public static byte[,] CalculateWarshallAlgorithmParallel(this byte[,] buf) {
        var n = buf.GetLength(0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(buf.GetLength(0), buf.GetLength(1));
        var rk = new byte[n, n];
        var rkm1 = new byte[n, n];
        Buffer.BlockCopy(buf, 0, rkm1, 0, buf.Length);

        for (var k = 0; k < n; k++) {
            var k1 = k;
            Enumerable.Range(0, n).AsParallel().AsUnordered().ForAll(
                y => {
                    Enumerable.Range(0, n).AsParallel().AsUnordered().ForAll(
                        x => { rk[y, x] = (byte)(rkm1[y, x] | (rkm1[y, k1] & rkm1[k1, x])); });
                }
            );

            if (k < n - 1) Buffer.BlockCopy(rk, 0, rkm1, 0, rk.Length);
        }

        return rk;
    }
}