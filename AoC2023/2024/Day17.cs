using System.Collections.Immutable;
using AoC2023._2024.ThreeBit;
using CommunityToolkit.HighPerformance;
using Pidgin;
using static Pidgin.Parser<char>;
using static Pidgin.Parser;

namespace AoC2023._2024;

public class Day17 : Adventer {
    public static readonly Parser<char, Machine> MachineParser;

    static Day17() {
        var instructions = Num.Separated(Char(','))
            .Map(i => i.Select(l => (Instruction)l).ToImmutableArray());
        var registerA = String("Register A: ").Then(Num);
        var registerB = String("Register B: ").Then(Num);
        var registerC = String("Register C: ").Then(Num);
        var registers = Map(
            (a, b, c) => {
                var r = new Registers();
                r[0] = a;
                r[1] = b;
                r[2] = c;
                return r;
            },
            registerA.Before(Whitespaces),
            registerB.Before(Whitespaces),
            registerC
        );
        MachineParser = Map(
            (r, i) => new Machine(r, i),
            registers.Before(Whitespaces),
            String("Program: ").Then(instructions).Before(Whitespaces)
        );
    }
    
    private Machine machine;

    public Day17() {
        Bag["test"] = """
                      Register A: 729
                      Register B: 0
                      Register C: 0

                      Program: 0,1,5,4,3,0
                      """;
    }
    
    protected override void InternalOnLoad() {
        machine = MachineParser.ParseOrThrow(Input.Text);
    }

    protected override object InternalPart1() {
        var localMachine = new Machine(machine);
        var output = new List<int>();
        localMachine.OnOutput += output.Add;
        localMachine.Execute();
        return string.Join(',', output);
    }

    protected override object InternalPart2() {
        return Enumerable.Range(0, int.MaxValue).Select(
            i => {
                var localMachine = new Machine(machine) {
                    A = i
                };
                var output = new List<int>();
                localMachine.OnOutput += output.Add;
                localMachine.Execute();
                // Console.WriteLine("i: {0}, ib: {0:B}, output: {1}", i, string.Join(',', output));
                return output;
            })
            .AsParallel()
            .AsOrdered()
            .First(x => x.Select(i => (Instruction)i).SequenceEqual(machine.Instructions));
    }
}