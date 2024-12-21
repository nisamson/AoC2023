using System.Runtime.CompilerServices;

namespace AoC2023._2018.Chronal;

[InlineArray(4)]
public struct Registers {
    private int r0;
}

public record MachineState {

    private Registers registers;

    public ReadOnlySpan<int> Registers => registers;
    
    public MachineState(MachineState state) {
        registers = state.registers;
    }
    
    public MachineState(ReadOnlySpan<int> registers) {
        var r = (Span<int>)this.registers;
        registers.CopyTo(r);
    }

    public void Apply(Instruction instruction) {
        registers[instruction.C] = instruction.Kind switch {
            InstructionKind.Addr => registers[instruction.A] + registers[instruction.B],
            InstructionKind.Addi => registers[instruction.A] + instruction.B,
            InstructionKind.Mulr => registers[instruction.A] * registers[instruction.B],
            InstructionKind.Muli => registers[instruction.A] * instruction.B,
            InstructionKind.Banr => registers[instruction.A] & registers[instruction.B],
            InstructionKind.Bani => registers[instruction.A] & instruction.B,
            InstructionKind.Borr => registers[instruction.A] | registers[instruction.B],
            InstructionKind.Bori => registers[instruction.A] | instruction.B,
            InstructionKind.Setr => registers[instruction.A],
            InstructionKind.Seti => instruction.A,
            InstructionKind.Gtrr => registers[instruction.A] > registers[instruction.B] ? 1 : 0,
            InstructionKind.Gtri => registers[instruction.A] > instruction.B ? 1 : 0,
            InstructionKind.Gtir => instruction.A > registers[instruction.B] ? 1 : 0,
            InstructionKind.Eqrr => registers[instruction.A] == registers[instruction.B] ? 1 : 0,
            InstructionKind.Eqri => registers[instruction.A] == instruction.B ? 1 : 0,
            InstructionKind.Eqir => instruction.A == registers[instruction.B] ? 1 : 0,
            _ => throw new ArgumentException($"{instruction.Kind} is not a valid instruction.")
        };
    }

    public override string ToString() {
        return $"[{registers[0]}, {registers[1]}, {registers[2]}, {registers[3]}]";
    }
}