#region license

// AoC2023 - AoC2023 - Day11.cs
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

using System.Linq.Expressions;
using AoC.Support;
using AoC2023._2023;
using Farkle;
using Farkle.Builder;

namespace AoC2023._2022;

using OperationExpr = Expression<Func<int, int>>;

public class Day11 : Adventer {
    public readonly record struct Throw(int Destination, int Item);

    public enum Op {
        Mul,
        Add,
    }

    public abstract record AstNode;

    public record MonkeyNode(int Id, IReadOnlyList<int> StartingItems, OperationNode op, TestNode test) : AstNode;

    public record OperationNode(Op Op, OperandNode Operand) : AstNode;

    public abstract record OperandNode : AstNode {
        public abstract Expression ToExpression(Expression param);
    };

    public record IntOperandNode(int Value) : OperandNode {
        public override Expression ToExpression(Expression param) {
            return Expression.Constant(Value);
        }
    }

    public record OldNode : OperandNode {
        public override Expression ToExpression(Expression param) {
            return param;
        }
    }

    public record TestNode(int TestNum, int TrueDest, int FalseDest) : AstNode;


    public record Monkey {
        public int Id { get; }
        private List<int> items;
        public IReadOnlyList<int> Items => items;
        public Func<int, Throw> OperateAndThrow { get; }

        private static OperationExpr DetermineThrow(TestNode testNode) {
            var testNum = testNode.TestNum;
            var trueDest = testNode.TrueDest;
            var falseDest = testNode.FalseDest;
            var param = Expression.Parameter(typeof(int), "item");
            var testExpr = Expression.Equal(Expression.Modulo(param, Expression.Constant(testNum)), Expression.Constant(0));
            var trueExpr = Expression.Constant(trueDest);
            var falseExpr = Expression.Constant(falseDest);
            var ifExpr = Expression.Condition(testExpr, trueExpr, falseExpr);
            var lambda = Expression.Lambda<Func<int, int>>(ifExpr, param);
            return lambda;
        }

        private static OperationExpr DoOperation(OperationNode opNode) {
            var op = opNode.Op;
            var operand = opNode.Operand;
            var param = Expression.Parameter(typeof(int), "item");
            var opExpr = op switch {
                Op.Mul => Expression.Multiply(param, operand.ToExpression(param)),
                Op.Add => Expression.Add(param, operand.ToExpression(param)),
                _      => throw new ArgumentOutOfRangeException(nameof(op), op, "Invalid operation"),
            };
            var divThree = Expression.Divide(opExpr, Expression.Constant(3));
            var lambda = Expression.Lambda<Func<int, int>>(divThree, param);
            return lambda;
        }

        private static Expression<Func<int, Throw>> CreateOperationAndThrow(TestNode testNode, OperationNode opNode) {
            var op = DoOperation(opNode);
            var throwOp = DetermineThrow(testNode);
            var param = Expression.Parameter(typeof(int), "item");
            var opExpr = Expression.Invoke(op, param);
            var worryVar = Expression.Variable(typeof(int), "newWorry");
            
            var worryAssign = Expression.Assign(worryVar, opExpr);
            var throwExpr = Expression.Invoke(throwOp, worryVar);
            var destVar = Expression.Variable(typeof(int), "dest");
            var destAssign = Expression.Assign(destVar, throwExpr);
            var consType = typeof(Throw).GetConstructor([typeof(int), typeof(int)]) ?? throw new InvalidOperationException();
            var newThrow = Expression.New(consType, destVar, worryVar);
            var body = Expression.Block(new[] {worryVar, destVar}, worryAssign, destAssign, newThrow);
            var lambda = Expression.Lambda<Func<int, Throw>>(body, param);
            return lambda;
        }

        public Monkey(MonkeyNode node) {
            Id = node.Id;
            items = [..node.StartingItems];
            OperateAndThrow = CreateOperationAndThrow(node.test, node.op).Compile();
        }

        public BusinessMonkey ToBusinessMonkey() {
            return new(Id, items, OperateAndThrow);
        }
    }

    public class BusinessMonkey(int id, IEnumerable<int> items, Func<int, Throw> operateAndThrow) {
        public int Id { get; } = id;
        private Queue<int> Items { get; } = new(items);
        private Func<int, Throw> OperateAndThrow { get; } = operateAndThrow;
        public int ItemsInspected { get; private set; }

        public void Catch(int item) {
            Items.Enqueue(item);
        }

        public Throw? NextItem() {
            if (!Items.TryDequeue(out var item)) {
                return null;
            }

            ItemsInspected++;
            var dest = OperateAndThrow(item);
            return dest;
        }
    }

    public class MonkeyBusiness(IReadOnlyList<BusinessMonkey> monkeys) {
        public IReadOnlyList<BusinessMonkey> Monkeys { get; } = monkeys.ToList();

        public void Run(int rounds) {
            while (rounds-- > 0) {
                foreach (var monkey in Monkeys) {
                    while (monkey.NextItem() is { } @throw) {
                        Monkeys[@throw.Destination].Catch(@throw.Item);
                    }
                }
            }
        }
    }

    public static class Lang {
        public static readonly PrecompilableDesigntimeFarkle<List<MonkeyNode>> Designtime;
        public static readonly RuntimeFarkle<List<MonkeyNode>> Runtime;

        static Lang() {
            var integer = Terminals.Int32("integer");
            var op = Nonterminal.Create(
                "op",
                Terminal.Literal("*").Appended().FinishConstant(Op.Mul),
                Terminal.Literal("+").Appended().FinishConstant(Op.Add)
            );
            var startingItems = Nonterminal.Create(
                "startingItems",
                Terminal.Literal("Starting items:").Appended()
                    .Extend(integer.SeparatedBy<int, List<int>>(Terminal.Literal(","), true))
                    .AsIs()
            );
            var old = Terminal.Literal("old");
            var operand = Nonterminal.Create<OperandNode>(
                "operand",
                integer.Finish(i => new IntOperandNode(i)),
                old.Finish(() => new OldNode())
            );
            var operation = Nonterminal.Create(
                "operation",
                Terminal.Literal("Operation:").Appended()
                    .Append("new")
                    .Append("=")
                    .Append(old)
                    .Extend(op)
                    .Extend(operand)
                    .Finish((op, operand) => new OperationNode(op, operand))
            );

            var trueFalse = Nonterminal.Create(
                "trueFalse",
                Terminal.Literal("true").FinishConstant(true),
                Terminal.Literal("false").FinishConstant(false)
            );

            var testBranch = Nonterminal.Create(
                "testBranch",
                Terminal.Literal("If")
                    .Appended()
                    .Append(trueFalse)
                    .Append(":")
                    .Append("throw")
                    .Append("to")
                    .Append("monkey")
                    .Extend(integer)
                    .AsIs()
            );

            var test = Nonterminal.Create(
                "test",
                Terminal.Literal("Test:").Appended()
                    .Append("divisible")
                    .Append("by")
                    .Extend(integer)
                    .Extend(testBranch)
                    .Extend(testBranch)
                    .Finish((testNum, trueDest, falseDest) => new TestNode(testNum, trueDest, falseDest))
            );

            var monkey = Nonterminal.Create(
                "monkey",
                Terminal.Literal("Monkey")
                    .Appended()
                    .Extend(integer)
                    .Append(":")
                    .Extend(startingItems)
                    .Extend(operation)
                    .Extend(test)
                    .Finish((id, startingItems, op, test) => new MonkeyNode(id, startingItems, op, test))
            );

            var monkeyList = Nonterminal.Create(
                "monkeyList",
                monkey.Many<MonkeyNode, List<MonkeyNode>>()
                    .AsIs()
            );

            Designtime = monkeyList.CaseSensitive().MarkForPrecompile();
            Runtime = Designtime.Build();
        }
    }

    private List<Monkey> monkeys = [];

    protected override void InternalOnLoad() {
        var res = Lang.Runtime.Parse(Input.Text);
        if (res.IsError) {
            throw new Exception($"Parse error: ${res.ErrorValue}");
        }

        monkeys = res.ResultValue.Select(m => new Monkey(m)).ToList();
    }

    protected override object InternalPart1() {
        var businessMonkeys = monkeys.Select(m => m.ToBusinessMonkey()).ToList();
        var monkeyBusiness = new MonkeyBusiness(businessMonkeys);
        monkeyBusiness.Run(20);
        return businessMonkeys.OrderByDescending(b => b.ItemsInspected)
            .Take(2)
            .Product(b => b.ItemsInspected);
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }

    public Day11() {
        Bag["test"] = """
                      Monkey 0:
                        Starting items: 79, 98
                        Operation: new = old * 19
                        Test: divisible by 23
                          If true: throw to monkey 2
                          If false: throw to monkey 3

                      Monkey 1:
                        Starting items: 54, 65, 75, 74
                        Operation: new = old + 6
                        Test: divisible by 19
                          If true: throw to monkey 2
                          If false: throw to monkey 0

                      Monkey 2:
                        Starting items: 79, 60, 97
                        Operation: new = old * old
                        Test: divisible by 13
                          If true: throw to monkey 1
                          If false: throw to monkey 3

                      Monkey 3:
                        Starting items: 74
                        Operation: new = old + 3
                        Test: divisible by 17
                          If true: throw to monkey 0
                          If false: throw to monkey 1
                      """;
    }
}
