using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using AoC.Support;
using Rationals;

namespace AoC2023._2023;

// https://en.wikipedia.org/wiki/Lagrange_polynomial
internal class LagrangeInterpolator {
    public LagrangeInterpolator(IEnumerable<(Rational, Rational)> nodes) {
        var x = new List<Rational>();
        var y = new List<Rational>();
        foreach (var (xi, yi) in nodes) {
            x.Add(xi);
            y.Add(yi);
        }

        X = x;
        Y = y;
        W = Enumerable.Range(0, X.Count).Select(CalculateBarycentricWeight).ToList();
    }

    public LagrangeInterpolator(IEnumerable<(long, long)> nodes) :
        this(nodes.Select(x => (new Rational(x.Item1), new Rational(x.Item2)))) { }

    private List<Rational> X { get; }
    private List<Rational> Y { get; }
    private List<Rational> W { get; }

    // thank you wikipedia
    private Rational CalculateBarycentricWeight(int j) {
        var x = X[j];
        var w = Rational.One;
        for (var m = 0; m < X.Count; m++) {
            if (j == m) continue;

            w /= x - X[m];
        }

        return w;
    }

    private Rational PartialTerm(int j, Rational x) {
        return W[j] / (x - X[j]);
    }

    public Rational Interpolate(Rational x) {
        var idx = X.FindIndex(xi => xi == x);
        if (idx != -1) return Y[idx];

        var numerator = Rational.Zero;
        var denominator = Rational.Zero;
        for (var j = 0; j < X.Count; j++) {
            var term = PartialTerm(j, x);
            numerator += term * Y[j];
            denominator += term;
        }

        return (numerator / denominator).CanonicalForm;
    }

    public Rational Interpolate(long x) {
        return Interpolate(new Rational(x));
    }
}

internal class LagrangeInterpolator<T> where T : INumber<T> {
    public LagrangeInterpolator(IEnumerable<(T, T)> nodes) {
        var x = new List<T>();
        var y = new List<T>();
        foreach (var (xi, yi) in nodes) {
            x.Add(xi);
            y.Add(yi);
        }

        X = x;
        Y = y;
        W = Enumerable.Range(0, X.Count).Select(CalculateBarycentricWeight).ToList();
    }

    public LagrangeInterpolator(IEnumerable<(long, long)> nodes) :
        this(nodes.Select(x => (T.CreateChecked(x.Item1), T.CreateChecked(x.Item2)))) { }

    private List<T> X { get; }
    private List<T> Y { get; }
    private List<T> W { get; }

    // thank you wikipedia
    private T CalculateBarycentricWeight(int j) {
        var x = X[j];
        var w = T.One;
        for (var m = 0; m < X.Count; m++) {
            if (j == m) continue;

            w /= x - X[m];
        }

        return w;
    }

    private T PartialTerm(int j, T x) {
        return W[j] / (x - X[j]);
    }

    public T Interpolate(T x) {
        var idx = X.FindIndex(xi => xi == x);
        if (idx != -1) return Y[idx];

        var numerator = T.Zero;
        var denominator = T.Zero;
        for (var j = 0; j < X.Count; j++) {
            var term = PartialTerm(j, x);
            numerator += term * Y[j];
            denominator += term;
        }

        return numerator / denominator;
    }

    public T Interpolate<U>(U x) where U : INumber<U> {
        return Interpolate(T.CreateChecked(x));
    }
}

internal class Day09Problem(IReadOnlyList<long> numbers) {
    public IReadOnlyList<long> Numbers { get; } = numbers;

    public Func<long, long> GetSimpleInterpolator() {
        var c = Numbers[0];
        var degree = 0;
        var currentLevel = Numbers;
        while (!currentLevel.AllEqual()) {
            currentLevel = currentLevel.Zip(currentLevel.Skip(1)).Select(a => a.Second - a.First).ToArray();
            degree++;
        }

        var coefficient = currentLevel[0];

        return x => coefficient * x.Pow(degree) + c;
    }

    public Func<long, long> GetLagrangeInterpolator() {
        var interp = new LagrangeInterpolator(Numbers.Select((x, i) => ((long)i, x)));
        return x => (long)interp.Interpolate(x).WholePart;
    }

    public Func<long, long> GetLagrangeInterpolator<T>() where T : INumber<T> {
        var interp = new LagrangeInterpolator<T>(Numbers.Select((x, i) => ((long)i, x)));
        return x => long.CreateChecked(Math.Round(decimal.CreateChecked(interp.Interpolate(x))));
    }
}

public class Day09 : Adventer {
    private IReadOnlyList<Day09Problem> problems;

    public Day09() {
        Bag["test"] = """
                      0 3 6 9 12 15
                      1 3 6 10 15 21
                      10 13 16 21 30 45
                      """;
    }

    [MemberNotNull(nameof(problems))]
    protected override void InternalOnLoad() {
        problems = Input.Lines.Select(x => x.Split().Select(long.Parse))
            .Select(x => new Day09Problem(x.ToArray()))
            .ToArray();
    }

    protected override object InternalPart1() {
        long total = 0;
        foreach (var p in problems) total += p.GetLagrangeInterpolator()(p.Numbers.Count);
        // total += p.GetSimpleInterpolator()(p.Numbers.Count);
        // total += p.GetLagrangeInterpolator<decimal>()(p.Numbers.Count);
        return total;
    }

    protected override object InternalPart2() {
        long total = 0;
        foreach (var p in problems) total += p.GetLagrangeInterpolator()(-1);
        // total += p.GetSimpleInterpolator()(-1);
        // total += p.GetLagrangeInterpolator<decimal>()(-1);
        return total;
    }
}