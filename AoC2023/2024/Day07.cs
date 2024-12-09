using System.Collections.Immutable;
using AoC.Support.Numerics;
using Pidgin;
using static Pidgin.Parser<char>;
using static Pidgin.Parser;

namespace AoC2023._2024;

public class Day07 : Adventer {

    private readonly ref struct EquationSolver {
        public required long Current { get; init; }
        public required ReadOnlySpan<long> Remaining { get; init; }

        public bool IsSolvableWithRemainingOperands(ReadOnlySpan<Func<long, long, long>> operations,
            long expectedResult) {
            
            if (Remaining.IsEmpty) {
                return Current == expectedResult;
            }
            
            if (Current > expectedResult) {
                return false;
            }
            
            var curResult = Current;
            var front = Remaining[0];
            var back = Remaining[1..];
            foreach (var op in operations) {
                if (new EquationSolver { Current = op(curResult, front), Remaining = back }
                    .IsSolvableWithRemainingOperands(operations, expectedResult)) {
                    return true;
                }
            }
            
            return false;
        }
    }
    
    private readonly record struct Equation {
        public required long Result { get; init; }
        public required ImmutableArray<long> Operands { get; init; }

        public bool IsSolvableWithOperations(ReadOnlySpan<Func<long, long, long>> operations) {
            if (Operands.IsEmpty) {
                return false;
            }

            var start = new EquationSolver {
                Current = Operands[0],
                Remaining = Operands.AsSpan()[1..]
            };
            return start.IsSolvableWithRemainingOperands(operations, Result);
        }

        public override string ToString() {
            return $"{Result}: {string.Join(" ", Operands)}";
        }
    }

    private static readonly Parser<char, Equation> EquationParser;

    static Day07() {
        var number = Digit.AtLeastOnce()
            .Slice((span, _) => long.Parse(span));
        var equation = Map(
            (res, _, nums) => new Equation {
                Result = res,
                Operands = [..nums]
            },
            number,
            String(": "),
            number.SeparatedAndOptionallyTerminatedAtLeastOnce(Whitespace)
        ).Before(End);
        EquationParser = equation;
    }


    private class Problem {
        public ImmutableArray<Equation> Equations { get; private init; }

        public Problem(string[] input) {
            Equations = [..input.Select(l => EquationParser.ParseOrThrow(l))];
        }

        public static ImmutableArray<Func<long, long, long>> Operations { get; } = [
            (a, b) => a + b,
            (a, b) => a * b
        ];

        public static ImmutableArray<Func<long, long, long>> Part2Operations { get; } = [
            ..Operations,
            (a, b) => a.Concatenate(b, 10),
        ];

        public long Part1() {
            return Equations.AsParallel()
                .Where(eq => eq.IsSolvableWithOperations(Operations.AsSpan()))
                .Sum(eq => eq.Result);
        }

        public long Part2() {
            return Equations
                .AsParallel()
                .Where(eq => eq.IsSolvableWithOperations(Part2Operations.AsSpan()))
                .Sum(eq => eq.Result);
        }
    }

    private Problem problem = null!;

    public Day07() {
        Bag["test"] = """
                      190: 10 19
                      3267: 81 40 27
                      83: 17 5
                      156: 15 6
                      7290: 6 8 6 15
                      161011: 16 10 13
                      192: 17 8 14
                      21037: 9 7 18 13
                      292: 11 6 16 20
                      """; // 3749
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