using System.Collections.Immutable;
using AoC.Support;
using AoC2023._2018.Chronal;
using Farkle;
using Farkle.Builder;

namespace AoC2023._2018;

public class Day16 : Adventer {
    public readonly record struct Sample(MachineState Before, MachineState After, ImmutableArray<int> Instruction) {
        public List<InstructionKind> PossibleInstructions() {
            var before = Before;
            var after = After;
            var arguments = Instruction.AsSpan()[1..];
            var possibleInstructions = new List<InstructionKind>();
            foreach (var instructionKind in Chronal.Instruction.ValidInstructionKinds.AsReadOnlySpan()) {
                var state = new MachineState(before);
                var instruction = new Instruction(instructionKind, arguments[0], arguments[1], arguments[2]);
                state.Apply(instruction);
                if (state.Registers.SequenceEqual(after.Registers)) {
                    possibleInstructions.Add(instructionKind);
                }
            }

            return possibleInstructions;
        }
    }

    private static readonly PrecompilableDesigntimeFarkle<InputFile> DesigntimeFileParser;
    private static readonly RuntimeFarkle<InputFile> FileParser;

    static Day16() {
        var num = Terminals.Int32("num");
        var before = Terminal.Literal("Before:");
        var after = Terminal.Literal("After:");
        var lbrace = Terminal.Literal("[");
        var rbrace = Terminal.Literal("]");
        var comma = Terminal.Literal(",");

        var machineState = Nonterminal.Create("Machine state",
            lbrace.Appended().Extend(num).Append(comma)
                .Extend(num).Append(comma)
                .Extend(num).Append(comma)
                .Extend(num).Append(rbrace)
                .Finish((a, b, c, d) => new MachineState([a, b, c, d]))
        );
        var instruction = Nonterminal.Create(
            "Instruction",
            num.Extended().Extend(num).Extend(num).Extend(num).Finish((a, b, c, d) => ImmutableArray.ToImmutableArray([
                a, b, c, d
            ]))
        );
        var sample = Nonterminal.Create(
            "Sample",
            before.Appended().Extend(machineState)
                .Extend(instruction)
                .Append(after)
                .Extend(machineState)
                .Finish((b, i, a) => new Sample(b, a, i))
        );

        var sampleList = Nonterminal.Create<List<Sample>>("Sample list");
        sampleList.SetProductions(
            sampleList.Extended().Extend(sample).Finish((l, s) => {
                l.Add(s);
                return l;
            }),
            sample.Finish(s => new List<Sample> { s })
        );
        
        var instructionList = Nonterminal.Create<List<ImmutableArray<int>>>("Instruction list");
        instructionList.SetProductions(
            instructionList.Extended().Extend(instruction).Finish((l, i) => {
                l.Add(i);
                return l;
            }),
            instruction.Finish(i => new List<ImmutableArray<int>> { i })
        );

        var inputFile = Nonterminal.Create("Input file",
                sampleList.Extended().Extend(instructionList).Finish((s, i) => new InputFile {Samples = [..s], Instructions = [..i]}))
            .AutoWhitespace(true)
            .MarkForPrecompile();
        DesigntimeFileParser = inputFile;
        FileParser = inputFile.Build();
    }

    private readonly record struct InputFile {
        public ImmutableArray<Sample> Samples { get; init; }
        public ImmutableArray<ImmutableArray<int>> Instructions { get; init; }
    }

    private InputFile file;
    
    protected override void InternalOnLoad() {
        var res = FileParser.Parse(Input.Text);
        if (res.IsError) {
            throw new FormatException($"Invalid input format: {res.ErrorValue}");
        }
        
        file = res.ResultValue;
    }

    protected override object InternalPart1() {
        var count = 0;
        foreach (var sample in file.Samples) {
            var possibilities = sample.PossibleInstructions();
            if (possibilities.Count >= 3) {
                count++;
            }
        }

        return count;
    }

    public Day16() {
        Bag["test"] = """
                      Before: [3, 2, 1, 1]
                      9 2 1 2
                      After:  [3, 2, 2, 1]
                      
                      0 0 0 0
                      """;
    }

    private Dictionary<int, InstructionKind> CalculateInstructionMap() {
        var instructionPossibilities = new Dictionary<int, HashSet<InstructionKind>>();
        foreach (var sample in file.Samples) {
            var current = instructionPossibilities.GetOrNew(sample.Instruction[0]);
            switch (current.Count) {
                case 1:
                    continue;
                case 0:
                    current.UnionWith(sample.PossibleInstructions());
                    break;
                default:
                    current.IntersectWith(sample.PossibleInstructions());
                    break;
            }
        }

        while (instructionPossibilities.Values.Any(v => v.Count != 1)) {
            foreach (var (key, value) in instructionPossibilities.Where((pair => pair.Value.Count == 1))) {
                var instruction = value.Single();
                foreach (var (k, v) in instructionPossibilities) {
                    if (k == key) {
                        continue;
                    }

                    v.Remove(instruction);
                }
            }
        }

        return instructionPossibilities
            .ToDictionary(k => k.Key, v => v.Value.Single());
    }
    
    protected override object InternalPart2() {
        var instructionMap = CalculateInstructionMap();
        var instructions = file.Instructions.Select(
            i => new Instruction(instructionMap[i[0]], i[1], i[2], i[3])
        );
        var state = new MachineState([0,0,0,0]);
        foreach (var instruction in instructions) {
            state.Apply(instruction);
        }

        return state.Registers[0];
    }
}