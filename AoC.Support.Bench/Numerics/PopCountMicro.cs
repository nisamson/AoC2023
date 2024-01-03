#region license
// AoC2023 - AoC.Support.Bench - PopCount.cs
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

using System.ComponentModel;
using AoC.Support.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace AoC.Support.Bench.Numerics;

[DisassemblyDiagnoser(printSource: true, maxDepth:2)]
[RyuJitX64Job]
public class PopCountMicro {

    [ParamsSource(nameof(Data))]
    public byte[] data;
    
    [Benchmark(Baseline = true)]
    [Description("PopCount using the builtin method")]
    public void SimplePopCount() {
        data.AsReadOnlySpan().BoringPopCount();
    }
    
    [Benchmark]
    [Description("PopCount using Ssse3")]
    public void PopCountSse3s() {
        data.AsReadOnlySpan().PopCountSse3s();
    }
    
    [Benchmark]
    [Description("PopCount using Avx2")]
    public void PopCountAvx2() {
        data.AsReadOnlySpan().PopCountAvx2();
    }
    
    [Benchmark]
    [Description("PopCount using Avx2 alternate")]
    public void PopCountAvx2Alternate() {
        data.AsReadOnlySpan().PopCountAvx2Alternate();
    }
    
    [Benchmark]
    [Description("PopCount using Avx2 alternate")]
    public void PopCountAvx2Unrolled() {
        data.AsReadOnlySpan().PopCountAvx2ManualUnroll();
    }

    public IEnumerable<byte[]> Data() {
        var randomSeed = Random.Shared.Next();
        var random = new Random(randomSeed);
        Console.WriteLine($"${nameof(PopCountMicro)}: Random seed: ${randomSeed}");
        foreach (var i in Enumerable.Range(10, 2)) {
            var jitter = random.Next(-32, 32);
            var data = new byte[Math.Max((1 << i) + jitter, 1)];
            random.NextBytes(data);
            yield return data;
        }
        
    }
}
