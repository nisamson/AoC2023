using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Farkle;
using Farkle.Builder;
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
    
    private static readonly Parser<char, int> Num1_3 = Digit.Then(Digit.Optional()).Then(Digit.Optional()).Slice((s, _) => int.Parse(s));

    private static readonly Parser<char, MulOp> MulOpParser = String("mul(")
        .Then(Map((a, _, b) => new MulOp(a, b), Num1_3, Char(','), Num1_3)).Before(Char(')'));

    private readonly record struct MulOp(int A, int B) : IOperation {
        public int Result => A * B;
    }

    private readonly record struct Do : IOperation;

    private readonly record struct Dont : IOperation;

    private static readonly RuntimeFarkle<ImmutableList<IOperation>> RuntimeParser;

    static Day03() {
        var lPar = Terminal.Literal("(");
        var rPar = Terminal.Literal(")");
        var @do = Terminal.Literal("do");
        var dont = Terminal.Literal("dont");
        var mul = Terminal.Literal("mul");
        var comma = Terminal.Literal(",");
        var operand = Terminal.Create("operand",
            (_, data) => int.Parse(data),
            FarkleRegex.FromRegexString(@"\d{1,3}"));

        var mulOp = Nonterminal.Create(
            "mulOp",
            mul.Appended().Append(lPar).Extend(operand).Append(comma).Extend(operand).Append(rPar)
                .Finish((a, b) => new MulOp(a, b) as IOperation)
        );
        
        var doOp = Nonterminal.Create(
            "doOp",
            @do.Appended().Append(lPar).Append(rPar).FinishConstant(new Do() as IOperation)
        );
        
        var dontOp = Nonterminal.Create(
            "dontOp",
            dont.Appended().Append(lPar).Append(rPar).FinishConstant(new Dont() as IOperation)
        );
        
        var operation = Nonterminal.Create("operation", mulOp.AsIs(),
            doOp.AsIs(),
            dontOp.AsIs()
        );
        
        var anythingElse = Terminal.Create("anythingElse",
            FarkleRegex.FromRegexString(".*")
        );

        var inputSequence = Nonterminal.Create<ImmutableList<IOperation>>("input");
        inputSequence.SetProductions(
            operation.Extended().Extend(inputSequence).Finish((a, b) => b.Insert(0, a)),
            anythingElse.Appended().Extend(inputSequence).AsIs(),
            ProductionBuilder.Empty.FinishConstant(ImmutableList<IOperation>.Empty)
        );

        RuntimeParser = inputSequence
            .CaseSensitive()
            .AutoWhitespace(false)
            .MarkForPrecompile()
            .Build();

    }

    private void DoWithMuls(ReadOnlySpan<char> input, Action<MulOp> action) {
        foreach (var match in Mul().EnumerateMatches(input)) {
            var mulOp = MulOpParser.ParseOrThrow(input[match.Index..(match.Index + match.Length)]);
            action(mulOp);
        }
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
            };
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
        var operations = RuntimeParser.Parse(text).ResultValue;
        return calculateWithSequence(operations, false);
    }

    protected override object InternalPart2() {
        var operations = RuntimeParser.Parse(text).ResultValue;
        return calculateWithSequence(operations, true);
    }
}