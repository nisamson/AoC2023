using System.Collections.Immutable;

namespace AoC2023._2024;

public class Day11 : Adventer {

    private static readonly List<(long Limit, bool EvenDigits)> DigitMap = new();

    private static void LoadDigitMap(List<(long Limit, bool EvenDigits)> DigitMap) {
        try {
            var i = 10;
            var even = false;
            while (true) {
                checked {
                    DigitMap.Add(new (i, even));
                    i *= 10;
                    even = !even;
                }
            }
        } catch (ArithmeticException e) { }
    }
    
    private static bool IsEvenDigits(long n) {
        foreach (var (limit, even) in DigitMap) {
            if (n < limit) {
                return even;
            }
        }

        return false;
    }
    
    private static (long, long) Split(long n) {
        var s = n.ToString();
        var half = s.Length / 2;
        return (long.Parse(s[..half]), long.Parse(s[half..]));
    }

    private class Problem {
        private readonly ImmutableArray<long> digits;

        public Problem(string digits) {
            var builder = ImmutableArray.CreateBuilder<long>(digits.Length);
            var span = digits.AsSpan();
            foreach (var split in span.Split(' ')) {
                builder.Add(long.Parse(span[split]));
            }

            this.digits = builder.ToImmutable();
        }

        public List<long> Step(long steps) {
            var (a, b) = (digits.ToList(), new List<long>());
            for (var i = 0; i < steps; i++) {
                Console.WriteLine($"{i}: {a.Count}");
                foreach (var num in a) {
                    if (num == 0) {
                        b.Add(1);
                    } else if (IsEvenDigits(num)) {
                        var (x, y) = Split(num);
                        b.Add(x);
                        b.Add(y);
                    } else {
                        b.Add(num * 2024);
                    }
                }
                a.Clear();
                (a, b) = (b, a);
            }
            
            return a;
        }
        
        public List<long> Step(long steps, long start) {
            var (a, b) = (new List<long> {start}, new List<long>());
            for (var i = 0; i < steps; i++) {
                Console.WriteLine($"{i}: {a.Count}");
                foreach (var num in a) {
                    if (num == 0) {
                        b.Add(1);
                    } else if (IsEvenDigits(num)) {
                        var (x, y) = Split(num);
                        b.Add(x);
                        b.Add(y);
                    } else {
                        b.Add(num * 2024);
                    }
                }
                a.Clear();
                (a, b) = (b, a);
            }
            
            return a;
        }

        private static int IterStep(int steps, long start) {
            if (steps == 1) {
                return StepOnce(start).Count();
            }

            return StepOnce(start).Select(n => IterStep(steps - 1, n)).Sum();
        }

        public int IterStep(int steps) {
            return digits.Sum(i => IterStep(steps, i));
        }

        private static IEnumerable<long> StepOnce(long num) {
            if (num == 0) {
                yield return 1;
            } else if (IsEvenDigits(num)) {
                var (x, y) = Split(num);
                yield return x;
                yield return y;
            } else {
                yield return num * 2024;
            }
        }
    }

    public Day11() {
        Bag["test"] = "125 17";
    }

    private Problem problem;

    static Day11() {
        LoadDigitMap(DigitMap);
    }

    protected override void InternalOnLoad() {
        LoadDigitMap([]); // here for benchmarking
        problem = new Problem(Input.Text);
    }

    protected override object InternalPart1() {
        return problem.IterStep(25);
    }

    protected override object InternalPart2() {
        return problem.IterStep(75);
    }
}