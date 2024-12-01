#region license

// AoC2023 - AoC2023 - Day19.cs
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
using System.Linq.Expressions;
using AoC.Support;
using Farkle;
using Farkle.Builder;

namespace AoC2023._2023;

public class Day19 : Adventer {
    public delegate long BoundsFunc(Bounds bounds);

    public enum ComparisonKind {
        LessThan,
        GreaterThan
    }


    public enum ResultKind {
        Accept,
        Reject
    }

    private Func<Part, ResultKind> part1;

    private ModuleNode problem;

    protected override void InternalOnLoad() {
        var result = Lang.Runtime.Parse(Input.Text);
        if (result.IsError) throw new Exception(result.ErrorValue.ToString());

        problem = result.ResultValue;
        var compiler = new Part1Compiler(Lang.FromFile("19.txt"));
        part1 = compiler.Compile(problem).Compile();
    }

    protected override object InternalPart1() {
        return problem.Parts
            .Select(s => s.Part)
            .Where(s => part1(s) == ResultKind.Accept)
            .Sum(p => p.Value());
    }

    protected override object InternalPart2() {
        var bounds = Bounds.Start;
        var detector = new BoundsDetector();
        var boundsFunc = detector.Visit(problem);
        var total = boundsFunc(bounds);
        return total;
    }

    public readonly record struct Part(int X, int M, int A, int S) {
        public int Value() {
            return X + M + A + S;
        }
    }

    public abstract class AstVisitor<TResult> {
        public virtual TResult Visit(AstNode node) {
            return node.Accept(this);
        }

        public abstract TResult Visit(ModuleNode node);
        public abstract TResult Visit(TestNode node);
        public abstract TResult Visit(AcceptStatementNode node);
        public abstract TResult Visit(RejectStatementNode node);
        public abstract TResult Visit(ConditionTestNode node);
        public abstract TResult Visit(AccessorNode node);
        public abstract TResult Visit(LiteralNode node);
        public abstract TResult Visit(ActionNode node);
    }

    public class Part1Compiler : AstVisitor<Expression> {
        public Part1Compiler(SymbolDocumentInfo document) {
            Document = document;
        }

        private SymbolDocumentInfo Document { get; }

        private Dictionary<string, LabelTarget> Labels { get; } = new();
        private LabelTarget ReturnLabel { get; } = Expression.Label(typeof(ResultKind));
        private ParameterExpression Input { get; } = Expression.Parameter(typeof(Part), "part");

        private DebugInfoExpression DebugInfo(AstNode node) {
            return Expression.DebugInfo(
                Document,
                (int)node.Start.Line,
                (int)node.Start.Column,
                (int)node.End.Line,
                (int)node.End.Column
            );
        }

        private MemberExpression AccessPartField(string name) {
            return Expression.Property(Input, name.ToUpper());
        }

        private LabelTarget GetLabel(string name) {
            if (Labels.TryGetValue(name, out var label)) return label;

            label = Expression.Label(name);
            Labels.Add(name, label);

            return label;
        }

        public Expression<Func<Part, ResultKind>> Compile(ModuleNode node) {
            var debugInfo = DebugInfo(node);
            var statements = node.Tests.Values.Select(n => n.Accept(this));
            var entry = GetLabel("in");
            var blockExprs = debugInfo.Once()
                .Append<Expression>(Expression.Goto(entry))
                .Concat(statements)
                .Append(Expression.Label(ReturnLabel, Expression.Constant(ResultKind.Reject, typeof(ResultKind))));
            var block = Expression.Block(typeof(ResultKind), blockExprs);
            return Expression.Lambda<Func<Part, ResultKind>>(
                block,
                false,
                Input
            );
        }

        public override Expression Visit(ModuleNode node) {
            return Compile(node);
        }

        public override Expression Visit(TestNode node) {
            var debugInfo = DebugInfo(node);
            var label = GetLabel(node.Name);
            var statements = node.Statements.Select(s => s.Accept(this));
            var labelExpr = Expression.Label(label);
            var blockExprs = debugInfo.Once().Append<Expression>(labelExpr).Concat(statements);
            return Expression.Block(blockExprs);
        }

        public override Expression Visit(AcceptStatementNode node) {
            var debugInfo = DebugInfo(node);
            var reject = Expression.Return(ReturnLabel, Expression.Constant(ResultKind.Accept));
            return Expression.Block(debugInfo, reject);
        }

        public override Expression Visit(RejectStatementNode node) {
            var debugInfo = DebugInfo(node);
            var reject = Expression.Return(ReturnLabel, Expression.Constant(ResultKind.Reject));
            return Expression.Block(debugInfo, reject);
        }

        public override Expression Visit(ConditionTestNode node) {
            var debugInfo = DebugInfo(node);
            var left = node.left.Accept(this);
            var right = node.right.Accept(this);
            var op = node.op switch {
                ComparisonKind.LessThan => Expression.LessThan(left, right),
                ComparisonKind.GreaterThan => Expression.GreaterThan(left, right),
                _ => throw new UnreachableException()
            };

            var then = node.action.Accept(this);
            var ifThen = Expression.IfThen(op, then);
            return Expression.Block(debugInfo, ifThen);
        }

        public override Expression Visit(AccessorNode node) {
            var debugInfo = DebugInfo(node);
            var partField = AccessPartField(node.Name);
            return Expression.Block(debugInfo, partField);
        }

        public override Expression Visit(LiteralNode node) {
            return Expression.Constant(node.Value, typeof(int));
        }

        public override Expression Visit(ActionNode node) {
            var action = node.Action;
            var debugInfo = DebugInfo(node);
            Expression? retExpr = null;
            if (action.Result is not null)
                retExpr = Expression.Return(ReturnLabel, Expression.Constant(action.Result.Value, typeof(ResultKind)));

            if (action.NextTest is not null) retExpr = Expression.Goto(GetLabel(action.NextTest));

            if (retExpr is not null) return Expression.Block(debugInfo, retExpr);

            return debugInfo;
        }
    }

    public abstract record AstNode(Position Start, Position End) {
        public abstract TResult Accept<TResult>(AstVisitor<TResult> visitor);
    }

    public record ModuleNode : AstNode {
        public ModuleNode(Position start, Position end, List<TestNode> tests, List<PartNode> parts) : base(start, end) {
            Tests = tests.ToDictionary(t => t.Name, t => t);
            Parts = parts;
        }

        public IReadOnlyDictionary<string, TestNode> Tests { get; }
        public IReadOnlyList<PartNode> Parts { get; }

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }
    }

    public record TestNode(Position Start, Position End, string Name, IReadOnlyList<StatementNode> Statements)
        : AstNode(Start, End) {
        public bool IsTerminal => Statements.All(s => s.IsTerminal);

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public IEnumerable<string> References() {
            return Statements.SelectMany(s => s.References());
        }
    }

    public abstract record StatementNode(Position Start, Position End) : AstNode(Start, End) {
        public abstract bool IsTerminal { get; }
        public abstract IEnumerable<string> References();
    }

    public record AcceptStatementNode(Position Start, Position End) : StatementNode(Start, End) {
        public override bool IsTerminal => true;

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public override IEnumerable<string> References() {
            return Enumerable.Empty<string>();
        }
    }

    public record RejectStatementNode(Position Start, Position End) : StatementNode(Start, End) {
        public override bool IsTerminal => true;

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public override IEnumerable<string> References() {
            return Enumerable.Empty<string>();
        }
    }

    public record ConditionTestNode(
        Position Start,
        Position End,
        ComparisonKind op,
        AccessorNode left,
        LiteralNode right,
        ActionNode action)
        : StatementNode(Start, End) {
        public override bool IsTerminal => action.IsTerminal;

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public override IEnumerable<string> References() {
            return action.References();
        }

        public Bounds Accepted(Bounds b) {
            var relevantBound = left.AccessBound(b);
            var newBound = op switch {
                ComparisonKind.LessThan => relevantBound.Merge(Bound.AtMost(right.Value - 1)),
                ComparisonKind.GreaterThan => relevantBound.Merge(Bound.AtLeast(right.Value + 1)),
                _ => throw new UnreachableException()
            };
            return left.UpdateBound(b, newBound);
        }

        public Bounds Rejected(Bounds b) {
            var relevantBound = left.AccessBound(b);
            var newBound = op switch {
                ComparisonKind.LessThan => relevantBound.Merge(Bound.AtLeast(right.Value)),
                ComparisonKind.GreaterThan => relevantBound.Merge(Bound.AtMost(right.Value)),
                _ => throw new UnreachableException()
            };
            return left.UpdateBound(b, newBound);
        }
    }

    public abstract record AtomNode(Position Start, Position End) : AstNode(Start, End) { }

    public record AccessorNode(Position Start, Position End, string Name) : AtomNode(Start, End) {
        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public Func<Part, int> Accessor() {
            return Name switch {
                "x" => p => p.X,
                "m" => p => p.M,
                "a" => p => p.A,
                "s" => p => p.S,
                _ => throw new UnreachableException()
            };
        }

        public Bound AccessBound(Bounds b) {
            return Name switch {
                "x" => b.X,
                "m" => b.M,
                "a" => b.A,
                "s" => b.S,
                _ => throw new UnreachableException()
            };
        }

        public Bounds UpdateBound(Bounds bs, Bound b) {
            return Name switch {
                "x" => bs with { X = b },
                "m" => bs with { M = b },
                "a" => bs with { A = b },
                "s" => bs with { S = b },
                _ => throw new UnreachableException()
            };
        }
    }

    public record LiteralNode(Position Start, Position End, int Value) : AtomNode(Start, End) {
        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }
    }

    public record ActionNode(Position Start, Position End, TestResult Action) : StatementNode(Start, End) {
        public override bool IsTerminal => Action.NextTest is null;

        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }

        public override IEnumerable<string> References() {
            if (Action.NextTest is { } next) yield return next;
        }
    }

    public record PartNode(Position Start, Position End, Part Part) : AstNode(Start, End) {
        public override TResult Accept<TResult>(AstVisitor<TResult> visitor) {
            return visitor.Visit(this);
        }
    }

    public readonly record struct TestResult(ResultKind? Result, string? NextTest) {
        public static readonly TestResult Accept = new(ResultKind.Accept, null);
        public static readonly TestResult Reject = new(ResultKind.Reject, null);
        public static readonly TestResult Continue = new(null, null);

        public static TestResult JumpTo(string nextTest) {
            return new TestResult(null, nextTest);
        }
    }

    public static class Lang {
        public static readonly Guid LangId = Guid.Parse("1ff69912-aa91-4737-8975-b2864f403c49");
        public static readonly Guid VendorId = Guid.Parse("c39748dc-d92d-4f65-bf81-b2fc87911ac8");
        public static readonly PrecompilableDesigntimeFarkle<ModuleNode> Designtime;
        public static readonly RuntimeFarkle<ModuleNode> Runtime;

        static Lang() {
            var ident = Terminal.Create(
                "ident",
                (d, s) => new {
                    Start = d.StartPosition,
                    End = d.EndPosition,
                    Value = new string(s)
                },
                Regex.FromRegexString(@"[a-z][a-z]+")
            );
            var accept = Terminal.Create(
                "accept",
                (d, s) => new ActionNode(d.StartPosition, d.EndPosition, TestResult.Accept),
                Regex.Literal('A')
            );
            var reject = Terminal.Create(
                "reject",
                (d, s) => new ActionNode(d.StartPosition, d.EndPosition, TestResult.Reject),
                Regex.Literal('R')
            );

            var number = Terminal.Create(
                "number",
                (d, s) => new {
                    Start = d.StartPosition,
                    End = d.EndPosition,
                    Value = int.Parse(s)
                },
                Regex.FromRegexString("[0-9]+")
            );
            var lt = Terminal.Create(
                "lt",
                (d, s) => new {
                    Start = d.StartPosition,
                    End = d.EndPosition,
                    Value = ComparisonKind.LessThan
                },
                Regex.Literal('<')
            );
            var gt = Terminal.Create(
                "gt",
                (d, s) => new {
                    Start = d.StartPosition,
                    End = d.EndPosition,
                    Value = ComparisonKind.GreaterThan
                },
                Regex.Literal('>')
            );
            var conditionOp = Nonterminal.Create(
                "conditionOp",
                lt.Extended().Finish(a => a),
                gt.Extended().Finish(a => a)
            );

            var action = Nonterminal.Create(
                "action",
                accept.Extended().Finish(a => a),
                reject.Extended().Finish(a => a),
                ident.Extended().Finish(a => new ActionNode(a.Start, a.End, TestResult.JumpTo(a.Value)))
            );

            var field = Terminal.Create(
                "field",
                (d, s) => new {
                    Start = d.StartPosition,
                    End = d.EndPosition,
                    Value = new string(s)
                },
                Regex.OneOf("xmas")
            );

            var condition = Nonterminal.Create(
                "condition",
                field.Extended().Extend(conditionOp).Extend(number)
                    .Append(":")
                    .Extend(action)
                    .Finish(
                        (field, op, value, a) => {
                            var left = new AccessorNode(field.Start, value.End, field.Value);
                            var right = new LiteralNode(value.Start, value.End, value.Value);
                            return new ConditionTestNode(field.Start, a.End, op.Value, left, right, a);
                        }
                    )
            );

            var statement = Nonterminal.Create<StatementNode>(
                "statement",
                action.Finish(a => a),
                condition.Finish(c => c)
            );

            var testCond = Nonterminal.Create(
                "testCond",
                ident.Extended()
                    .Append(Terminal.Literal("{"))
                    .Extend(statement.SeparatedBy<StatementNode, List<StatementNode>>(Terminal.Literal(","), true))
                    .Append(Terminal.Literal("}"))
                    .Finish((name, stmts) => new TestNode(name.Start, stmts[^1].End, name.Value, stmts))
            );

            var part = Nonterminal.Create(
                "part",
                Terminal.Literal("{x=")
                    .Appended()
                    .Extend(number)
                    .Append(",m=")
                    .Extend(number)
                    .Append(",a=")
                    .Extend(number)
                    .Append(",s=")
                    .Extend(number)
                    .Append("}")
                    .Finish((x, m, a, s) => new PartNode(x.Start, s.End, new Part(x.Value, m.Value, a.Value, s.Value)))
            );

            var module = Nonterminal.Create(
                "module",
                testCond.Many<TestNode, List<TestNode>>(true)
                    .Extended()
                    .Extend(part.Many<PartNode, List<PartNode>>(true))
                    .Finish((tests, parts) => new ModuleNode(tests[0].Start, parts[^1].End, tests, parts))
            );

            Designtime = module
                .CaseSensitive()
                .MarkForPrecompile();
            Runtime = Designtime.Build();
        }

        public static SymbolDocumentInfo FromFile(string path) {
            return Expression.SymbolDocument(path, LangId, VendorId);
        }
    }

    public readonly record struct Bound(int Min, int Max) {
        public const int MaxValue = 4000;
        public const int MinValue = 1;
        public static readonly Bound Empty = new(0, 0);

        public static readonly Bound Start = new(1, 4000);
        public int Size => Math.Max(0, Max - Min + 1);

        public bool Contains(int value) {
            return value >= Min && value <= Max;
        }

        public static Bound AtLeast(int min) {
            return new Bound(min, MaxValue);
        }

        public static Bound AtMost(int max) {
            return new Bound(MinValue, max);
        }

        public Bound Merge(Bound that) {
            return new Bound(Math.Max(Min, that.Min), Math.Min(Max, that.Max));
        }
    }

    public readonly record struct Bounds(Bound X, Bound M, Bound A, Bound S) {
        public static readonly Bounds Start = new(Bound.Start, Bound.Start, Bound.Start, Bound.Start);
        public static readonly Bounds Empty = new(Bound.Empty, Bound.Empty, Bound.Empty, Bound.Empty);
        public long Size => X.Size * (long)M.Size * A.Size * S.Size;

        public Bounds Merge(Bounds that) {
            return new Bounds(X.Merge(that.X), M.Merge(that.M), A.Merge(that.A), S.Merge(that.S));
        }
    }

    public class BoundsDetector : AstVisitor<BoundsFunc> {
        private Dictionary<string, BoundsFunc> PartBounds { get; } = new();
        private IReadOnlyDictionary<string, TestNode> Tests { get; set; } = new Dictionary<string, TestNode>();

        private BoundsFunc GetBounds(string name) {
            if (PartBounds.TryGetValue(name, out var bounds)) return bounds;

            bounds = Tests[name].Accept(this);
            PartBounds.Add(name, bounds);
            return bounds;
        }

        public override BoundsFunc Visit(ModuleNode node) {
            Tests = node.Tests;
            return GetBounds("in");
        }

        public override BoundsFunc Visit(TestNode node) {
            if (PartBounds.TryGetValue(node.Name, out var boundsTest)) return boundsTest;

            return bounds => {
                var currentBounds = bounds;
                var total = 0L;
                foreach (var statement in node.Statements)
                    switch (statement) {
                        case ConditionTestNode c:
                            var accepted = c.Accepted(currentBounds);
                            var accf = c.action.Accept(this);
                            total += accf(accepted);
                            currentBounds = c.Rejected(currentBounds);
                            break;
                        default:
                            total += statement.Accept(this)(currentBounds);
                            break;
                    }

                return total;
            };
        }

        public override BoundsFunc Visit(AcceptStatementNode node) {
            return bounds => bounds.Size;
        }

        public override BoundsFunc Visit(RejectStatementNode node) {
            return _ => 0;
        }

        public override BoundsFunc Visit(ConditionTestNode node) {
            return b => {
                var accepted = node.Accepted(b);
                var bf = node.action.Accept(this);
                return bf(accepted);
            };
        }

        public override BoundsFunc Visit(AccessorNode node) {
            throw new NotImplementedException();
        }

        public override BoundsFunc Visit(LiteralNode node) {
            throw new NotImplementedException();
        }

        public override BoundsFunc Visit(ActionNode node) {
            if (node.Action.NextTest is not null) return GetBounds(node.Action.NextTest);

            return node.Action.Result switch {
                ResultKind.Accept => bounds => bounds.Size,
                ResultKind.Reject => _ => 0,
                _ => throw new UnreachableException()
            };
        }
    }
}