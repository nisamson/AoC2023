using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace AoC2023._2024.ThreeBit;

[InlineArray(3)]
public struct Registers {
    private int r0;
}

public record Machine {
    private Registers registers;
    
    public Span<int> Registers => registers;
    
    public Machine(Machine state) {
        registers = state.registers;
        Instructions = state.Instructions;
    }
    
    public Machine(ReadOnlySpan<int> registers, ImmutableArray<Instruction> instructions) {
        var r = (Span<int>)this.registers;
        registers.CopyTo(r);
        Instructions = instructions;
    }
    
    public ref int A => ref registers[0];
    public ref int B => ref registers[1];
    public ref int C => ref registers[2];
    
    public int InstructionPointer { get; private set; }

    private int ConvertCombo(int comboOperand) {
        return comboOperand switch {
            >= 0 and <= 3 => comboOperand,
            4 => A,
            5 => B,
            6 => C,
            _ => throw new ArgumentException("Invalid combo operand")
        };
    }
    
    public ImmutableArray<Instruction> Instructions { get; set; }

    public void Execute() {
        while (InstructionPointer < Instructions.Length) {
            var instruction = Instructions[InstructionPointer];
            var operand = (byte)Instructions[InstructionPointer + 1];
            Apply(instruction, operand);
        }
    }

    private void Apply(Instruction instruction, byte operand) {
        switch (instruction) {
            case Instruction.Adv:
                A >>= ConvertCombo(operand);
                break;
            case Instruction.Bxl:
                B ^= operand;
                break;
            case Instruction.Bst:
                B = ConvertCombo(operand) & 0b111;
                break;
            case Instruction.Jnz:
                if (A != 0) {
                    InstructionPointer = operand;
                    return;
                }
                break;
            case Instruction.Bxc:
                B ^= C;
                break;
            case Instruction.Out:
                OnOutput(ConvertCombo(operand) & 0b111);
                break;
            case Instruction.Bdv:
                B = A >> ConvertCombo(operand);
                break;
            case Instruction.Cdv:
                C = A >> ConvertCombo(operand);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(instruction), instruction, null);
        }
        InstructionPointer += 2;
    }
    
    public event Action<int> OnOutput;

    public override string ToString() {
        return $"[{A}, {B}, {C}, ip: {InstructionPointer}]";
    }
}