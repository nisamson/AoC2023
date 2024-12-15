using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AoC.Support;
using Pidgin;
using QuikGraph;
using QuikGraph.Algorithms;

namespace AoC2023._2024;

using static Pidgin.Parser;
using static Pidgin.Parser<char>;

public class Day05 : Adventer {
    private record Problem {
        public ImmutableArray<PageOrdering> Ordering { get; private init; }
        public ImmutableArray<Update> Updates { get; private init; }

        public Problem(string[] input) {
            var orderingBuilder = ImmutableArray.CreateBuilder<PageOrdering>();
            var updateBuilder = ImmutableArray.CreateBuilder<Update>();

            for (var i = 0; i < input.Length && !string.IsNullOrWhiteSpace(input[i]); i++) {
                orderingBuilder.Add(PageOrdering.Parse(input[i]));
            }

            for (var i = orderingBuilder.Count + 1; i < input.Length; i++) {
                updateBuilder.Add(Update.Parse(input[i]));
            }

            Ordering = orderingBuilder.ToImmutable();
            Updates = updateBuilder.ToImmutable();
        }

        private Update OrderUpdate(Update update, IComparer<int> comparer) {
            return new Update(update.Pages.Sort(comparer));
        }

        public bool UpdateIsOrdered(Update update, out Update? orderedUpdate, bool returnUpdated = false) {
            orderedUpdate = null;
            AdjacencyGraph<int, PageOrdering> orderingGraph = new(false);
            var relevantOrderings =
                Ordering.Where(ord => update.Pages.Contains(ord.First) && update.Pages.Contains(ord.Second));
            orderingGraph.AddVerticesAndEdgeRange(relevantOrderings);

            var tc = new TransitiveClosureAlgorithm<int, PageOrdering>(orderingGraph,
                (first, second) => new PageOrdering(first, second));
            tc.Compute();
            var transitiveClosure = tc.TransitiveClosure;

            for (var i = 0; i < update.Pages.Length - 1; i++) {
                if (!transitiveClosure.ContainsEdge(update.Pages[i], update.Pages[i + 1])) {
                    if (returnUpdated) {
                        var comparer = Comparer<int>.Create((a, b) => {
                            if (transitiveClosure.ContainsEdge(a, b)) {
                                return -1;
                            } else {
                                return 1;
                            }
                        });
                        orderedUpdate = OrderUpdate(update, comparer);
                    }

                    return false;
                }
            }

            return true;
        }

        public int Part1() {
            return Updates
                .Where(u => UpdateIsOrdered(u, out _))
                .Sum(update => update.Pages.MiddleElement());
        }

        public int Part2() {
            var sum = 0;
            foreach (var update in Updates) {
                if (!UpdateIsOrdered(update, out var orderedUpdate, true)) {
                    sum += orderedUpdate!.Pages.MiddleElement();
                }
            }

            return sum;
        }
    }

    private record PageOrdering(int First, int Second) : ISpanParsable<PageOrdering>, IEdge<int> {
        public static PageOrdering Parse(string s, IFormatProvider? provider = null) {
            return Parse(s.AsSpan(), provider);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PageOrdering result) {
            return TryParse(s.AsSpan(), provider, out result);
        }

        public static PageOrdering Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
            if (s.IsEmpty) {
                throw new ArgumentException("Input string was empty", nameof(s));
            }

            Span<Range> splits = stackalloc Range[2];

            s.Split(splits, '|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return new PageOrdering(
                int.Parse(s[splits[0]]),
                int.Parse(s[splits[1]])
            );
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PageOrdering result) {
            return TryParse(s, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out PageOrdering result) {
            try {
                result = Parse(s);
                return true;
            } catch {
                result = default;
                return false;
            }
        }

        public int Source => First;
        public int Target => Second;
        
        public override string ToString() {
            return $"{First}|{Second}";
        }
    }

    private record Update(ImmutableArray<int> Pages) : ISpanParsable<Update> {
        public static Update Parse(string s, IFormatProvider? provider = null) {
            return Parse(s.AsSpan(), provider);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Update result) {
            return TryParse(s.AsSpan(), provider, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out Update result) {
            return TryParse(s.AsSpan(), null, out result);
        }

        public static Update Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
            if (s.IsEmpty) {
                throw new ArgumentException("Input string was empty", nameof(s));
            }

            var builder = ImmutableArray.CreateBuilder<int>();

            var splitEnum = s.Split(',');
            foreach (var numRange in splitEnum) {
                builder.Add(int.Parse(s[numRange].Trim()));
            }

            return new Update(builder.ToImmutable());
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Update? result) {
            try {
                result = Parse(s, provider);
                return true;
            } catch {
                result = null;
                return false;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> s, out Update? result) {
            return TryParse(s, null, out result);
        }

        public override string ToString() {
            return string.Join(',', Pages);
        }
    }

    private Problem problem;

    public Day05() {
        Bag["test"] = """
                      47|53
                      97|13
                      97|61
                      97|47
                      75|29
                      61|13
                      75|53
                      29|13
                      97|29
                      53|29
                      61|53
                      97|53
                      61|29
                      47|13
                      75|47
                      97|75
                      47|61
                      75|61
                      47|29
                      75|13
                      53|13

                      75,47,61,53,29
                      97,61,53,29,13
                      75,29,13
                      75,97,47,61,53
                      61,13,29
                      97,13,75,29,47
                      """;
    }

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }

    protected override object InternalPart2() {
        return problem.Part2();
    }
}