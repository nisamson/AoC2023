using Farkle;
using Farkle.Builder;
using Pidgin;

namespace AoC2023._2019.Chronal;

public readonly record struct Instruction {
    //
    // public static readonly RuntimeFarkle<Instruction> Parser;
    //
    // private static readonly PrecompilableDesigntimeFarkle<Instruction> Designtime;
    
    static Instruction() {
        
    }
    
    public required InstructionKind Kind { get; init;  }
    public required int A { get; init; }
    public required int B { get; init; }
    public required int C { get; init; }

    public Instruction() { }

    public Instruction(int opCode, int a, int b, int c) {
        Kind = FromOpcode(opCode);
        A = a;
        B = b;
        C = c;
    }

    public static InstructionKind FromOpcode(int opCode) {
        throw new NotImplementedException();
    }
}

[Flags]
public enum InstructionKind {
    Register = 0,
    Immediate = 1,
    Add = 2,
    Mul = 4,
    Ban = 8,
    Bor = 16,
    Set = 32,
    Gt = 64,
    Eq = 128
}

public static class InstructionHelpers {
    public static bool IsRegister(this InstructionKind kind) => kind.HasFlag(InstructionKind.Register);
    public static bool IsImmediate(this InstructionKind kind) => kind.HasFlag(InstructionKind.Immediate);
}