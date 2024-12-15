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

    public Instruction(InstructionKind kind, int a, int b, int c) {
        
    }

    public static InstructionKind FromOpcode(int opCode) {
        throw new NotImplementedException();
    }

    private static readonly InstructionKind[] ValidInstructions = [
        InstructionKind.Addr,
        InstructionKind.Addi,
        InstructionKind.Mulr,
        InstructionKind.Muli,
        InstructionKind.Banr,
        InstructionKind.Bani,
        InstructionKind.Borr,
        InstructionKind.Bori,
        InstructionKind.Setr,
        InstructionKind.Seti,
        InstructionKind.Gtrr,
        InstructionKind.Gtri,
        InstructionKind.Gtir,
        InstructionKind.Eqrr,
        InstructionKind.Eqri,
        InstructionKind.Eqir
    ];
    
    public static ReadOnlyMemory<InstructionKind> ValidInstructionKinds => ValidInstructions;
}

[Flags]
public enum InstructionKind {
    None = 0,
    RegisterB = 1,
    ImmediateB = 2,
    RegisterA = 4,
    ImmediateA = 8,
    Add = 16,
    Mul = 32,
    Ban = 64,
    Bor = 128,
    Set = 256,
    Gt = 512,
    Eq = 1024,
    Addr = RegisterA | RegisterB | Add,
    Addi = RegisterA | ImmediateB | Add,
    Mulr = RegisterA | RegisterB | Mul,
    Muli = RegisterA | ImmediateB | Mul,
    Banr = RegisterA | RegisterB | Ban,
    Bani = RegisterA | ImmediateB | Ban,
    Borr = RegisterA | RegisterB | Bor,
    Bori = RegisterA | ImmediateB | Bor,
    Setr = RegisterA | Set,
    Seti = ImmediateA | Set,
    Gtrr = RegisterA | RegisterB | Gt,
    Gtri = RegisterA | ImmediateB | Gt,
    Gtir = ImmediateA | RegisterB | Gt,
    Eqrr = RegisterA | RegisterB | Eq,
    Eqri = RegisterA | ImmediateB | Eq,
    Eqir = ImmediateA | RegisterB | Eq,
    Test = Gt | Eq,
}

public static class InstructionHelpers {
    public static bool IsRegister(this InstructionKind kind) => kind.HasFlag(InstructionKind.RegisterA | InstructionKind.RegisterB);
    public static bool IsTest(this InstructionKind kind) => (kind & InstructionKind.Test) != InstructionKind.None;

    public static string ToString(this InstructionKind kind) {
        return kind switch {
            InstructionKind.Addr => "addr",
            InstructionKind.Addi => "addi",
            InstructionKind.Mulr => "mulr",
            InstructionKind.Muli => "muli",
            InstructionKind.Banr => "banr",
            InstructionKind.Bani => "bani",
            InstructionKind.Borr => "borr",
            InstructionKind.Bori => "bori",
            InstructionKind.Setr => "setr",
            InstructionKind.Seti => "seti",
            InstructionKind.Gtrr => "gtrr",
            InstructionKind.Gtri => "gtri",
            InstructionKind.Gtir => "gtir",
            InstructionKind.Eqrr => "eqrr",
            InstructionKind.Eqri => "eqri",
            InstructionKind.Eqir => "eqir",
            _ => "<invalid>"
        };
    }
}