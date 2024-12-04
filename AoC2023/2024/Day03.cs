using System.Collections.Immutable;
using System.Text.RegularExpressions;
using AoC.Support;
using Farkle;
using Farkle.Builder;
using Farkle.Parser;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using Regex = System.Text.RegularExpressions.Regex;
using String = System.String;
using FarkleRegex = Farkle.Builder.Regex;

namespace AoC2023._2024;

public partial class Day03 : Adventer {
    [GeneratedRegex(@"mul\(\d{1,3},\d{1,3}\)")]
    private static partial Regex Mul();

    private string text = null!;

    private const string Test = """
                                xmul(2,4)%&mul[3,7]!@^do_not_mul(5,5)+mul(32,64]then(mul(11,8)mul(8,5))
                                """;

    public Day03() {
        Bag["test"] = Test;
    }

    private interface IOperation;

    private static readonly Parser<char, int> Num1_3 = Digit.Then(Digit.Optional()).Then(Digit.Optional())
        .Slice((s, _) => int.Parse(s));

    private static readonly Parser<char, MulOp> MulOpParser = String("mul(")
        .Then(Map((a, _, b) => new MulOp(a, b), Num1_3, Char(','), Num1_3)).Before(Char(')'));

    private static readonly Parser<char, IEnumerable<IToken>> Tokenizer;

    private readonly record struct MulOp(int A, int B) : IOperation {
        public int Result => A * B;
    }

    private readonly record struct Do : IOperation;

    private readonly record struct Dont : IOperation;

    private static readonly RuntimeFarkle<ImmutableList<IToken>> RuntimeParser;
    private static readonly PrecompilableDesigntimeFarkle<ImmutableList<IToken>> DesigntimeParser;

    private interface IToken;

    private readonly record struct DoToken : IToken;

    private readonly record struct DontToken : IToken;

    private readonly record struct MulToken : IToken;

    private enum Dir {
        Left,
        Right
    }

    private readonly record struct ParensToken(Dir Dir) : IToken {
        public bool IsLeft => Dir == Dir.Left;
        public bool IsRight => Dir == Dir.Right;
    }

    private readonly record struct IntToken(int Value) : IToken;

    private readonly record struct CommaToken : IToken;

    private readonly record struct JunkToken : IToken;

    static Day03() {
        var @do = Terminal.Literal("do").FinishConstant(new DoToken() as IToken);
        var dont = Terminal.Literal("don't").FinishConstant(new DontToken() as IToken);
        var mul = Terminal.Literal("mul").FinishConstant(new MulToken() as IToken);
        var left = Terminal.Literal("(").FinishConstant(new ParensToken(Dir.Left) as IToken);
        var right = Terminal.Literal(")").FinishConstant(new ParensToken(Dir.Right) as IToken);
        var comma = Terminal.Literal(",").FinishConstant(new CommaToken() as IToken);
        var number = Terminal.Create("number", (_, s) => int.Parse(s), FarkleRegex.FromRegexString(@"\d{1,3}"))
            .Finish(i => new IntToken(i) as IToken);
        var junk = Terminal.Create("anythingElse", FarkleRegex.Any).FinishConstant(new JunkToken() as IToken);
        var partials = Terminal.Create("partials", FarkleRegex.FromRegexString("d|(don)|(don'')|(mu)|m")).FinishConstant(new JunkToken() as IToken);

        var token = Nonterminal.Create("token",
            @do,
            dont,
            mul,
            left,
            right,
            comma,
            number,
            junk,
            partials
        );

        var tokenListReversed = Nonterminal.Create<ImmutableList<IToken>>("tokenListReversed");
        tokenListReversed.SetProductions(tokenListReversed.Extended().Extend(token).Finish((l, t) => l.Add(t)),
            token.Finish(ImmutableList.Create));
        var tokenList = Nonterminal.Create("tokenList", tokenListReversed.AsIs(),
            ProductionBuilder.Empty.FinishConstant(ImmutableList<IToken>.Empty));
        DesigntimeParser = tokenList.CaseSensitive()
            .AutoWhitespace(false)
            .MarkForPrecompile();

        RuntimeParser = DesigntimeParser.Build();
    }

    private void DoWithMuls(ReadOnlySpan<char> input, Action<MulOp> action) {
        foreach (var match in Mul().EnumerateMatches(input)) {
            var mulOp = MulOpParser.ParseOrThrow(input[match.Index..(match.Index + match.Length)]);
            action(mulOp);
        }
    }

    private IList<IOperation> TransformOperations(ReadOnlySpan<IToken> tokens) {
        var operations = new List<IOperation>();
        while (tokens.IsNotEmpty()) {
            switch (tokens) {
                case [DoToken, ParensToken { IsLeft: true }, ParensToken { IsRight: true }, ..]: {
                    tokens = tokens[3..];
                    operations.Add(new Do());
                    break;
                }
                case [DontToken, ParensToken { IsLeft: true }, ParensToken { IsRight: true }, ..]: {
                    tokens = tokens[3..];
                    operations.Add(new Dont());
                    break;
                }
                case [
                    MulToken, ParensToken { IsLeft: true }, IntToken a, CommaToken, IntToken b,
                    ParensToken { IsRight: true }, ..
                ]: {
                    tokens = tokens[6..];
                    operations.Add(new MulOp(a.Value, b.Value));
                    break;
                }
                default: {
                    tokens = tokens[1..];
                    break;
                }
            }
        }

        return operations;
    }

    private int calculateWithSequence(IEnumerable<IOperation> operations, bool obeyInstructions) {
        var doMul = true;
        var sum = 0;
        foreach (var operation in operations) {
            switch (operation) {
                case Do _:
                    doMul = true;
                    break;
                case Dont _:
                    if (obeyInstructions) {
                        doMul = false;
                    }

                    break;
                case MulOp mulOp when doMul:
                    sum += mulOp.Result;
                    break;
            }

            ;
        }

        return sum;
    }

    protected override void InternalOnLoad() {
        text = Input.Text;
    }

    protected override object InternalPart1() {
        // var sum = 0;
        // DoWithMuls(text, op => sum += op.Result);
        // return sum;
        var parseResult = RuntimeParser.Parse(text);
        if (parseResult.IsError) {
            throw new FormatException(parseResult.ErrorValue.ToString());
        }

        var tokens = parseResult.ResultValue;
        var operations = TransformOperations(tokens.ToArray().AsReadOnlySpan());
        return calculateWithSequence(operations, false);
    }

    protected override object InternalPart2() {
        var parseResult = RuntimeParser.Parse(text);
        if (parseResult.IsError) {
            throw new FormatException(parseResult.ErrorValue.ToString());
        }

        var tokens = parseResult.ResultValue;
        var operations = TransformOperations(tokens.ToArray().AsReadOnlySpan());
        return calculateWithSequence(operations, true);
    }
}