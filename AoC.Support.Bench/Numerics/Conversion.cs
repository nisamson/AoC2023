using System.ComponentModel;
using AoC.Support.Numerics;
using BenchmarkDotNet.Attributes;

namespace AoC.Support.Bench.Numerics;

[DisassemblyDiagnoser(printSource: true, maxDepth:2)]
[RyuJitX64Job]
public class Conversion {

    public const int PerInvoke = 100;
    private (int A, int B)[] data;
    
    [GlobalSetup]
    public void Setup() {
        data = Data();
    }
    
    public static long ConcatenateSlow(long a, long b) {
        return long.Parse($"{a}{b}");
    }
    
    [Benchmark(Baseline = true, OperationsPerInvoke = PerInvoke)]
    [Description("Concatenate using string concatenation")]
    public long Concatenate() {
        var sum = 0L;
        foreach (var (a, b) in data) {
            sum += ConcatenateSlow(a, b);
        }

        return sum;
    }
    
    [Benchmark(OperationsPerInvoke = PerInvoke)]
    [Description("Concatenate with stack allocation")]
    public long ConcatenateStack() {
        var sum = 0L;
        foreach (var (a, b) in data) {
            sum += ((long)a).ConcatenateDecimalChars(b);
        }

        return sum;
    }
    
    [Benchmark(OperationsPerInvoke = PerInvoke)]
    [Description("Concatenate with byte manipulation")]
    public long ConcatenateBytes() {
        var sum = 0L;
        foreach (var (a, b) in data) {
            sum += ((long)a).ConcatenateDecimalMagnitude(b);
        }

        return sum;
    }
    
    [Benchmark(OperationsPerInvoke = PerInvoke)]
    [Description("Concatenate with INumber")]
    public long ConcatenateNumber() {
        var sum = 0L;
        foreach (var (a, b) in data) {
            sum += a.Concatenate(b, 10);
        }

        return sum;
    }
    
    public (int A, int B)[] Data() {
        var randomSeed = Random.Shared.Next();
        var random = new Random(randomSeed);
        Console.WriteLine($"${nameof(Conversion)}: Random seed: ${randomSeed}");
        return Enumerable.Range(0, PerInvoke).Select(_ => (random.Next(100, 1000), random.Next(100, 1000))).ToArray();
    }
}