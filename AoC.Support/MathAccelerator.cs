#region license

// AoC2023 - AoC.Support - Math.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ILGPU;
using ILGPU.Backends.PTX;
using ILGPU.IR;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Algorithms;
using ILGPU.Algorithms.PTX;
using ILGPU.Runtime.OpenCL;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace AoC.Support;

public class MathAccelerator : IDisposable {
    private Context context;
    private Accelerator accelerator;

    public MathAccelerator(MathMode mode = MathMode.Default, bool cpu = false, bool opencl = false) {
#if DEBUG
        var optLevel = OptimizationLevel.Release;
#else
        var optLevel = OptimizationLevel.O2;
#endif

        context = Context.Create()
            .AllAccelerators()
#if DEBUG
            .AutoAssertions()
            .AutoIOOperations()
#endif
            .Optimize(optLevel)
            .Math(mode)
            .EnableAlgorithms()
            .ToContext();

        if (!opencl) {
            accelerator = context.GetPreferredDevice(cpu)
                .CreateAccelerator(context);
        } else {
            var clDevs = context.GetCLDevices();
            foreach (var clDev in clDevs) {
                // Nvidia OpenCL is broken
                if (clDev.Vendor == CLDeviceVendor.Nvidia) {
                    continue;
                }

                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                if ((clDev.DeviceType & CLDeviceType.CL_DEVICE_TYPE_CPU) == 0) {
                    if (cpu) {
                        continue;
                    }

                    accelerator = clDev.CreateAccelerator(context);
                    break;
                }

                accelerator = clDev.CreateAccelerator(context);
                break;
            }

            if (accelerator is null) {
                throw new InvalidOperationException("No OpenCL device found");
            }

            Debug.WriteLine("Using OpenCL device: " + accelerator);
        }

        Debug.WriteLine(accelerator);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        accelerator.Dispose();
        context.Dispose();
    }

    public string GetDeviceInfo() {
        return accelerator.ToString();
    }

    public Matrix<float> MultiplyMatrixRowMajor(Matrix<float> a, Matrix<float> b) {
        var m = a.RowCount;
        var n = a.ColumnCount;
        var p = b.ColumnCount;
        var bn = b.RowCount;

        if (n != bn) {
            throw new ArgumentException("Matrix dimensions must agree");
        }

        var aBuf = a.ToArray();
        float[,] bBuf;
        bBuf = ReferenceEquals(b, a) ? aBuf : b.ToArray();
        var cBuf = new float[m, p];

        using var accA = accelerator.Allocate2DDenseX<float>(new LongIndex2D(n, m));
        using var accB = ReferenceEquals(b, a) ? accA : accelerator.Allocate2DDenseX<float>(new LongIndex2D(p, n));
        using var accC = accelerator.Allocate2DDenseX<float>(new LongIndex2D(p, m));
        accA.CopyFromCPU(aBuf);
        if (!ReferenceEquals(b, a)) {
            accB.CopyFromCPU(bBuf);
        }

        var kernel = accelerator.LoadAutoGroupedStreamKernel<
            Index2D, ArrayView2D<float, Stride2D.DenseX>, ArrayView2D<float, Stride2D.DenseX>,
            ArrayView2D<float, Stride2D.DenseX>>(MultiplyMatrixRowMajorKernel);

        kernel(new Index2D(p, m), accA.View, accB.View, accC.View);

        accelerator.Synchronize();
        accC.CopyToCPU(cBuf);

        return DenseMatrix.OfArray(cBuf);
    }

    public void MultiplyMatrixRowMajorTiling(Matrix<float> a, Matrix<float> b, [NotNull] ref Matrix<float>? dest) {
        var m = a.RowCount;
        var ka = a.ColumnCount;
        var kb = b.RowCount;
        var n = b.ColumnCount;

        if (ka != kb) {
            throw new ArgumentException("Matrix dimensions must agree");
        }

        var aBuf = a.AsArray() ?? a.ToArray();
        var bBuf = ReferenceEquals(b, a) ? aBuf : b.AsArray() ?? b.ToArray();
        dest ??= new DenseMatrix(m, n);
        if (dest.RowCount != m || dest.ColumnCount != n) {
            throw new ArgumentException("Destination matrix must be of size MxN");
        }

        if (ReferenceEquals(dest, a) || ReferenceEquals(dest, b)) {
            throw new ArgumentException("Destination matrix must not be the same as either input matrix");
        }

        var cBuf = dest.AsArray() ?? dest.ToArray();

        using var accA = accelerator.Allocate2DDenseX<float>(new LongIndex2D(m, ka));
        using var accB = ReferenceEquals(b, a) ? accA : accelerator.Allocate2DDenseX<float>(new LongIndex2D(ka, n));
        using var accC = accelerator.Allocate2DDenseX<float>(new LongIndex2D(m, n));
        accA.CopyFromCPU(aBuf);
        if (!ReferenceEquals(b, a)) {
            accB.CopyFromCPU(bBuf);
        }

        var kernel = accelerator.LoadStreamKernel<
            ArrayView2D<float, Stride2D.DenseX>, ArrayView2D<float, Stride2D.DenseX>,
            ArrayView2D<float, Stride2D.DenseX>>(MatrixMultiplyTiledKernel);

        var groupSize = new Index2D(TileSize, TileSize);
        var numGroups = new Index2D((m + TileSize - 1) / TileSize, (n + TileSize - 1) / TileSize);


        kernel((numGroups, groupSize), accA.View, accB.View, accC.View);

        accelerator.Synchronize();
        accC.CopyToCPU(cBuf);
    }

    private static void MultiplyMatrixRowMajorKernel(
        Index2D index,
        ArrayView2D<float, Stride2D.DenseX> a,
        ArrayView2D<float, Stride2D.DenseX> b,
        ArrayView2D<float, Stride2D.DenseX> c) {
        var row = index.Y;
        var col = index.X;
        var sum = 0.0f;

        for (var i = 0; i < a.IntExtent.Y; i++) {
            sum += a[row, i] * b[i, col];
        }

        c[row, col] = sum;
    }

    private static void MakeBooleanKernelInplace(
        Index2D index,
        ArrayView2D<float, Stride2D.DenseX> a
    ) {
        var row = index.Y;
        var col = index.X;
        a[row, col] = a[row, col] == 0 ? 0 : 1;
    }

    public byte[,] MultiplyByteBooleanMatrixRowMajor(byte[,] a, byte[,] b) {
        var rowsA = a.GetLength(0);
        var colsA = a.GetLength(1);
        var colsB = b.GetLength(1);
        var rowsB = b.GetLength(0);

        if (colsA != rowsB) {
            throw new ArgumentException("Matrix dimensions must agree");
        }

        var cBuf = new byte[rowsA, colsB];

        var accA = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(colsA, rowsA));
        var accB = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(colsB, rowsB));
        var accC = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(colsB, rowsA));
        accA.CopyFromCPU(a);
        accB.CopyFromCPU(b);

        var kernel = accelerator.LoadAutoGroupedStreamKernel<
            Index2D, ArrayView2D<byte, Stride2D.DenseX>, ArrayView2D<byte, Stride2D.DenseX>,
            ArrayView2D<byte, Stride2D.DenseX>>(MultipleByteBooleanMatrixRowMajorKernel);

        kernel(new Index2D(colsB, rowsA), accA.View, accB.View, accC.View);
        accelerator.Synchronize();
        accC.CopyToCPU(cBuf);
        return cBuf;
    }

    private static void MultipleByteBooleanMatrixRowMajorKernel(
        Index2D index,
        ArrayView2D<byte, Stride2D.DenseX> a,
        ArrayView2D<byte, Stride2D.DenseX> b,
        ArrayView2D<byte, Stride2D.DenseX> c) {
        var row = index.Y;
        var col = index.X;
        byte sum = 0;
        for (var i = 0; i < a.IntExtent.Y; i++) {
            sum |= (byte) (a[row, i] & b[i, col]);
        }

        c[row, col] = sum;
    }

    /// <summary>
    /// Size of the tile (NxN).
    /// </summary>
    const int TileSize = 2;

    /// <summary>
    /// Multiplies two dense matrices and returns the resultant matrix (using tiling).
    /// </summary>
    /// <param name="accelerator">The Accelerator to run the multiplication on</param>
    /// <param name="a">A dense MxK matrix</param>
    /// <param name="b">A dense KxN matrix</param>
    /// <returns>A dense MxN matrix</returns>
    private static float[,] MatrixMultiplyTiled(Accelerator accelerator, float[,] a, float[,] b) {
        var m = a.GetLength(0);
        var ka = a.GetLength(1);
        var kb = b.GetLength(0);
        var n = b.GetLength(1);

        if (ka != kb)
            throw new ArgumentException($"Cannot multiply {m}x{ka} matrix by {n}x{kb} matrix", nameof(b));

        var kernel = accelerator.LoadStreamKernel<
            ArrayView2D<float, Stride2D.DenseX>,
            ArrayView2D<float, Stride2D.DenseX>,
            ArrayView2D<float, Stride2D.DenseX>>(MatrixMultiplyTiledKernel);
        var groupSize = new Index2D(TileSize, TileSize);
        var numGroups = new Index2D((m + TileSize - 1) / TileSize, (n + TileSize - 1) / TileSize);

        using var aBuffer = accelerator.Allocate2DDenseX<float>(new Index2D(m, ka));
        using var bBuffer = accelerator.Allocate2DDenseX<float>(new Index2D(ka, n));
        using var cBuffer = accelerator.Allocate2DDenseX<float>(new Index2D(m, n));
        aBuffer.CopyFromCPU(a);
        bBuffer.CopyFromCPU(b);

        kernel((numGroups, groupSize), aBuffer, bBuffer, cBuffer);

        // Reads data from the GPU buffer into a new CPU array.
        // Implicitly calls accelerator.DefaultStream.Synchronize() to ensure
        // that the kernel and memory copy are completed first.
        return cBuffer.GetAsArray2D();
    }

    /// <summary>
    /// The tiled matrix multiplication kernel that runs on the accelerated device.
    /// </summary>
    /// <param name="aView">An input matrix of size MxK</param>
    /// <param name="bView">An input matrix of size KxN</param>
    /// <param name="cView">An output matrix of size MxN</param>
    static void MatrixMultiplyTiledKernel(
        ArrayView2D<float, Stride2D.DenseX> aView,
        ArrayView2D<float, Stride2D.DenseX> bView,
        ArrayView2D<float, Stride2D.DenseX> cView) {
        var global = Grid.GlobalIndex.XY;
        var x = Group.IdxX;
        var y = Group.IdxY;

        var aTile = SharedMemory.Allocate2D<float, Stride2D.DenseX>(new Index2D(TileSize, TileSize), new Stride2D.DenseX(TileSize));
        var bTile = SharedMemory.Allocate2D<float, Stride2D.DenseX>(new Index2D(TileSize, TileSize), new Stride2D.DenseX(TileSize));
        var sum = 0.0f;

        for (var i = 0; i < aView.IntExtent.X; i += TileSize) {
            if (global.X < aView.IntExtent.X && y + i < aView.IntExtent.Y)
                aTile[x, y] = aView[global.X, y + i];
            else
                aTile[x, y] = 0;

            if (x + i < bView.IntExtent.X && global.Y < bView.IntExtent.Y)
                bTile[x, y] = bView[x + i, global.Y];
            else
                bTile[x, y] = 0;
            Group.Barrier();

            for (var k = 0; k < TileSize; k++)
                sum += aTile[new Index2D(x, k)] * bTile[new Index2D(k, y)];
            Group.Barrier();
        }

        if (global.X < cView.IntExtent.X && global.Y < cView.IntExtent.Y)
            cView[global] = sum;
    }

    // public float[,] WarshallReachabilityMatrix(float[,] a, bool reuse = true) {
    //     if (a.GetLength(0) != a.GetLength(1)) {
    //         throw new ArgumentException("Matrix must be square");
    //     }
    //
    //     var n = a.GetLength(0);
    //     using var rkm1 = accelerator.Allocate2DDenseX<float>(new LongIndex2D(n, n));
    //     using var rk = accelerator.Allocate2DDenseX<float>(new LongIndex2D(n, n));
    //
    //     var kernel = accelerator.LoadAutoGroupedStreamKernel<
    //         Index2D, int, ArrayView2D<float, Stride2D.DenseX>, ArrayView2D<float, Stride2D.DenseX>>(WarshallStepKernel);
    //
    //     rkm1.CopyFromCPU(a);
    //     for (var k = 0; k < n; k++) {
    //         kernel(new Index2D(n, n), k, rk.View, rkm1.View);
    //         accelerator.Synchronize();
    //         rkm1.CopyFrom(rk);
    //     }
    //
    //     if (!reuse) {
    //         return rk.GetAsArray2D();
    //     }
    //
    //     rk.CopyToCPU(a);
    //     return a;
    // }

    private static void WarshallStepKernel(
        Index2D index,
        int k,
        ArrayView2D<float, Stride2D.DenseX> rk,
        ArrayView2D<float, Stride2D.DenseX> rkm1) {
        var x = index.X;
        var y = index.Y;
        var prev = rkm1[x, y];
        // rk[x, y] = Math.Min(rkm1[x, k] * rkm1[k, y] + prev, 1);
        
        if (prev != 0) {
            rk[x, y] = 1;
        } else {
            rk[x, y] = rkm1[x, k] * rkm1[k, y];
        }
    }

    public byte[,] WarshallReachabilityMatrix(byte[,] a, bool reuse = true) {
        if (a.GetLength(0) != a.GetLength(1)) {
            throw new ArgumentException("Matrix must be square");
        }

        var n = a.GetLength(0);

        using var rkm1 = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(n, n));
        using var rk = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(n, n));

        var kernel = accelerator.LoadAutoGroupedStreamKernel<
            Index2D, int, ArrayView2D<byte, Stride2D.DenseX>, ArrayView2D<byte, Stride2D.DenseX>>(WarshallIntegerStepKernel);
        // if (kernel.GetCompiledKernel() is PTXCompiledKernel ptxKernel) {
        //     Console.WriteLine(ptxKernel.PTXAssembly);
        // }

        rkm1.CopyFromCPU(a);
        for (var k = 0; k < n; k++) {
            kernel(new Index2D(n, n), k, rk.View, rkm1.View);
            accelerator.Synchronize();
            rkm1.CopyFrom(rk);
        }

        var bufOut = reuse switch {
            true  => a,
            false => new byte[n, n],
        };
        rk.CopyToCPU(bufOut);

        return bufOut;
    }
    
    public byte[,] WarshallReachabilityMatrixGrouping(byte[,] a, bool reuse = true) {
        if (a.GetLength(0) != a.GetLength(1)) {
            throw new ArgumentException("Matrix must be square");
        }

        var n = a.GetLength(0);

        using var rkm1 = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(n, n));
        using var rk = accelerator.Allocate2DDenseX<byte>(new LongIndex2D(n, n));
        
        var groupSize = new Index2D(accelerator.MaxNumThreadsPerGroup, 1);
        var numGroups = new Index2D(1, n);

        var kernel = accelerator.LoadStreamKernel<
            int, ArrayView2D<byte, Stride2D.DenseX>, ArrayView2D<byte, Stride2D.DenseX>>(WarshallIntegerSimpleStepKernel);
        // if (kernel.GetCompiledKernel() is PTXCompiledKernel ptxKernel) {
        //     Console.WriteLine(ptxKernel.PTXAssembly);
        // }

        rkm1.CopyFromCPU(a);
        for (var k = 0; k < n; k++) {
            kernel(new KernelConfig(numGroups, groupSize), k, rk.View, rkm1.View);
            accelerator.Synchronize();
            rkm1.CopyFrom(rk);
        }

        var bufOut = reuse switch {
            true  => a,
            false => new byte[n, n],
        };
        rk.CopyToCPU(bufOut);

        return bufOut;
    }

    private static void WarshallIntegerStepKernel(
        Index2D index,
        int k,
        ArrayView2D<byte, Stride2D.DenseX> rk,
        ArrayView2D<byte, Stride2D.DenseX> rkm1) {
        var x = index.X;
        var y = index.Y;
        rk[x, y] = (byte) (rkm1[x, y] | (rkm1[x, k] & rkm1[k, y]));
    }

    private static void WarshallIntegerSimpleStepKernel(
        int k,
        ArrayView2D<byte, Stride2D.DenseX> rk,
        ArrayView2D<byte, Stride2D.DenseX> rkm1) {
        ref var ky = ref SharedMemory.Allocate<byte>();
        if (Group.IsFirstThread) {
            ky = rkm1[k, Grid.GlobalIndex.Y];
        }
        Group.Barrier();
        var locKy = ky;
        
        for (var offsetX = 0; offsetX < rk.IntExtent.X; offsetX += Group.DimX) {
            var globX = offsetX + Group.IdxX;
            if (globX >= rk.IntExtent.X) {
                break;
            }

            var idx = new Index2D(globX, Grid.GlobalIndex.Y);
            rk[idx] = (byte) (rkm1[idx] | (rkm1[globX, k] & locKy));
        }
    }

    // Each group has a fixed Y column, working across the X axis
    private static void WarshallStepGroupingKernel(
        int k,
        SpecializedValue<int> groupSize,
        ArrayView2D<int, Stride2D.DenseX> rk,
        ArrayView2D<int, Stride2D.DenseX> rkm1) {
        var globY = Grid.GlobalIndex.Y;
        ref var ky = ref SharedMemory.Allocate<int>();

        if (Group.IsFirstThread) {
            ky = rkm1[k, globY];
        }

        Group.Barrier();
        var groupX = Group.IdxX;
        for (var x = 0; x < rk.IntExtent.X; x += groupSize) {
            var globX = x + groupX;
            if (globX >= rk.IntExtent.X) {
                break;
            }

            var idx = new Index2D(globX, globY);
            rk[idx] = rkm1[idx] | (rkm1[globX, k] & ky);
        }

        Group.Barrier();
    }

    public class GroupSizeCalculator {
        public int MaxItemsPerGroup { get; }
        public int MaxThreadsPerGroup { get; }
        public int MaxThreadsPerDimension { get; }
        
        public GroupSizeCalculator(int maxItemsPerGroup, int maxThreadsPerGroup) {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerGroup, 1, nameof(maxItemsPerGroup));
            ArgumentOutOfRangeException.ThrowIfLessThan(maxThreadsPerGroup, 1, nameof(maxThreadsPerGroup));
            MaxItemsPerGroup = maxItemsPerGroup;
            MaxThreadsPerGroup = maxThreadsPerGroup;
            MaxThreadsPerDimension = (int) Math.Ceiling(Math.Sqrt(MaxThreadsPerGroup));
        }
        
        private IEnumerable<Index2D> EnumerateDims() {
            var step = (int)MathUtils.Gcd(MaxThreadsPerDimension, MaxItemsPerGroup);
            for (var x = step; x <= MaxItemsPerGroup; x += step) {
                for (var y = step; x + y <= MaxItemsPerGroup; y += step) {
                    yield return new Index2D(x, y);
                }
            }
        }

        private Index2D? optimal;

        [MemberNotNull(nameof(optimal))]
        public Index2D OptimalGroupSize() {
            if (optimal is not null) {
                return optimal.Value;
            }
            optimal = EnumerateDims().MaxBy(Score);
            return optimal.Value;
        }
        
        public double DimScore(Index2D idx) {
            return ((long) idx.X) * ((long)idx.Y) / ((double) MaxItemsPerGroup * MaxItemsPerGroup);
        }
        
        public double DimScore(int x, int y) {
            return DimScore(new Index2D(x, y));
        }
        
        public double SpaceScore(Index2D idx) {
            var diff = (double)Math.Abs(idx.X - idx.Y);
            // Reward groups that are closer to square
            // To scale to 1, we divide by the maximum difference
            return (idx.X + idx.Y) / (Math.Max(diff, 1) * MaxItemsPerGroup);
        }
        
        public double SpaceScore(int x, int y) {
            return SpaceScore(new Index2D(x, y));
        }
        
        public double ModuloThreadsScore(Index2D idx) {
            var x = idx.X;
            var y = idx.Y;
            var xMod = x % MaxThreadsPerDimension;
            var xModD = Math.Abs(xMod - MaxThreadsPerDimension/2.0);
            var yMod = y % MaxThreadsPerDimension;
            var yModD = Math.Abs(yMod - MaxThreadsPerDimension/2.0);
            var xScore = xMod == 0 ? 1.0 : 1.0 / xModD;
            var yScore = yMod == 0 ? 1.0 : 1.0 / yModD;
            return Math.Max(xScore, yScore);
        }
        
        public double ModuloThreadsScore(int x, int y) {
            return ModuloThreadsScore(new Index2D(x, y));
        }

        public double Score(Index2D xy) {
            var dimScore = DimScore(xy);
            var spaceScore = SpaceScore(xy);
            // var moduloThreadsScore = ModuloThreadsScore(xy);
            ReadOnlySpan<double> scores = [dimScore, spaceScore];
            var geometricMean = MathUtils.GeometricMean(scores);
            return geometricMean;
        }

        public double Score(int x, int y) {
            return Score(new Index2D(x, y));
        }
    }

    public static Index2D OptimalGroupSize(int maxItemsPerGroup, int maxThreadsPerGroup) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerGroup, 1, nameof(maxItemsPerGroup));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxThreadsPerGroup, 1, nameof(maxThreadsPerGroup));
        var maxItemsDouble = (double) maxItemsPerGroup;
        return EnumerateDims().MaxBy(Score);

        IEnumerable<Index2D> EnumerateDims() {
            var step = (int)MathUtils.Gcd(maxThreadsPerGroup, maxItemsPerGroup);
            for (var x = step; x <= maxItemsPerGroup; x += step) {
                for (var y = step; x + y <= maxItemsPerGroup; y += step) {
                    yield return new Index2D(x, y);
                }
            }
        }

        double DimScore(Index2D idx) {
            checked {
                return idx.X * idx.Y / maxItemsDouble;
            }
        }

        double SpaceScore(Index2D idx) {
            var diff = (double)Math.Abs(idx.X - idx.Y);

            if (diff == 0) {
                return 1.0;
            }
            
            return (idx.X + idx.Y) / diff;
        }

        double ModuloThreadsScore(Index2D idx) {
            var x = idx.X;
            var y = idx.Y;
            var xMod = x % maxThreadsPerGroup;
            var xModD = Math.Abs(xMod - maxThreadsPerGroup/2.0);
            var yMod = y % maxThreadsPerGroup;
            var yModD = Math.Abs(yMod - maxThreadsPerGroup/2.0);
            var xScore = xMod == 0 ? 1.0 : 1.0 / xModD;
            var yScore = yMod == 0 ? 1.0 : 1.0 / yModD;
            return Math.Max(xScore, yScore);
        }

        double Score(Index2D idx) {
            var dimScore = DimScore(idx);
            var spaceScore = SpaceScore(idx);
            var moduloThreadsScore = ModuloThreadsScore(idx);
            ReadOnlySpan<double> scores = [dimScore, spaceScore, moduloThreadsScore];
            var geometricMean = MathUtils.GeometricMean(scores);
            return geometricMean;
        }
    }

    public float[,] WarshallReachabilityMatrixGrouping(float[,] a, bool reuse = false) {
        if (a.GetLength(0) != a.GetLength(1)) {
            throw new ArgumentException("Matrix must be square");
        }

        var n = a.GetLength(0);
        using var rkm1 = accelerator.Allocate2DDenseX<float>(new LongIndex2D(n, n));
        using var rk = accelerator.Allocate2DDenseX<float>(new LongIndex2D(n, n));

        // calculate X and Y such that X * Y <= maxItemsPerGroup and X + Y <= maxSharedMemoryPerGroup
        // argmin(X, Y) ||YX - YY|| s.t. 0 < X * Y <= maxItemsPerGroup and 0 < X + Y <= maxSharedMemoryPerGroup
        var maxSharedMemory = accelerator.MaxSharedMemoryPerGroup;
        var maxGroupSize = accelerator.MaxNumThreadsPerGroup;
        var maxItemsPerGroup = maxSharedMemory / sizeof(float);
        var calc = new GroupSizeCalculator(maxItemsPerGroup, maxGroupSize);
        var itemsPerDim = Math.Min(maxItemsPerGroup / 2, calc.MaxThreadsPerDimension.Pow(2));
        itemsPerDim = Math.Min(itemsPerDim, n);
        var kGroupSize = new Index2D(calc.MaxThreadsPerDimension, calc.MaxThreadsPerDimension);
        var numGroups = new Index2D((n + itemsPerDim - 1) / itemsPerDim, (n + itemsPerDim - 1) / itemsPerDim);


        var kernel = accelerator.LoadStreamKernel<
            int,
            SpecializedValue<int>,
            ArrayView2D<float, Stride2D.DenseX>,
            ArrayView2D<float, Stride2D.DenseX>>(WarshallStepGroupingFloatKernel);

        rkm1.CopyFromCPU(a);
        for (var k = 0; k < n; k++) {
            Console.WriteLine($"{k} of {n-1}");
            kernel(
                new KernelConfig(numGroups, kGroupSize),
                k,
                SpecializedValue.New(itemsPerDim),
                rk.View,
                rkm1.View
            );
            accelerator.Synchronize();
            rkm1.CopyFrom(rk);
        }

        if (!reuse) {
            return rk.GetAsArray2D();
        }

        rk.CopyToCPU(a);
        return a;
    }

    private const int CudaType = (int) AcceleratorType.Cuda;
    private const int CpuType = (int) AcceleratorType.CPU;

    // ReSharper disable once InvertIf
    // Each group will calculate blockSize^2 elements
    // Precondition: dataSize = Group.Dimension.Size;
    private static void WarshallStepGroupingFloatKernel(
        int k,
        SpecializedValue<int> dataSize,
        ArrayView2D<float, Stride2D.DenseX> rk,
        ArrayView2D<float, Stride2D.DenseX> rkm1) {
        var kys = SharedMemory.Allocate1D<float>(dataSize.Value);
        var xks = SharedMemory.Allocate1D<float>(dataSize.Value);
        var grpIdx = Group.Index.XY;
        var blockPos = Grid.Index.XY;
        var blockOffset = blockPos * Group.Dimension.XY * Group.Dimension.XY;

        {
            var locIdx = Group.LinearIndex;
            var globIdxY = blockOffset.Y + locIdx;
            var globIdxX = blockOffset.X + locIdx;
            if (locIdx < dataSize) {
                if (globIdxY < rkm1.IntExtent.Y) {
                    kys[locIdx] = rkm1[k, globIdxY];
                }

                if (globIdxX < rkm1.IntExtent.X) {
                    xks[locIdx] = rkm1[globIdxX, k];
                }
            }
        }

        Group.Barrier();

        var dataSizeBounds = new Index2D(dataSize);
        for (var xOffset = 0; xOffset < dataSize; xOffset += Group.DimX) {
            if (xOffset + blockOffset.X >= rkm1.IntExtent.X) {
                break;
            }
            for (var yOffset = 0; yOffset < dataSize; yOffset += Group.DimY) {
                var locIdx = new Index2D(xOffset + grpIdx.X, yOffset + grpIdx.Y);
                var globIdxInner = locIdx + blockOffset;
                var proceed = globIdxInner.InBounds(rkm1.IntExtent);
                proceed &= locIdx.InBounds(dataSizeBounds);
                if (proceed) {
                    var newrk = xks[grpIdx.X] * kys[grpIdx.Y] + rkm1[globIdxInner];
                    rk[globIdxInner] = Math.Min(newrk, 1);
                }
            }
        }

        Group.Barrier();
    }
}
