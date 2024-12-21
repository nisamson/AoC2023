using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AoC2023._2018.Chronal;

[method: SetsRequiredMembers]
public readonly partial record struct Instruction(InstructionKind Kind, int A, int B, int C) : ISpanParsable<Instruction> {

    [GeneratedRegex(@"(\d+) (\d+) (\d+) (\d+)")]
    private static partial Regex InstructionRegex();
    
    public required InstructionKind Kind { get; init;  } = Kind;
    public required int A { get; init; } = A;
    public required int B { get; init; } = B;
    public required int C { get; init; } = C;

    [SetsRequiredMembers]
    public Instruction(int opCode, int a, int b, int c) : this(FromOpcode(opCode), a, b, c) { }

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

    public override string ToString() {
        return $"{Kind.AsString()} {A} {B} {C}";
    }

    public static Instruction Parse(string s, IFormatProvider? provider = null) {
        return Parse(s.AsSpan());
    }
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Instruction result) {
        return TryParse(s.AsSpan(), provider, out result);
    }
    public static Instruction Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
        if (!InstructionRegex().IsMatch(s)) {
            throw new FormatException("Invalid instruction format");
        }
        
        Span<Range> ranges = stackalloc Range[4];
        s.Split(ranges, ' ');
        var opCode = int.Parse(s[ranges[0]]);
        var a = int.Parse(s[ranges[1]]);
        var b = int.Parse(s[ranges[2]]);
        var c = int.Parse(s[ranges[3]]);
        return new Instruction(opCode, a, b, c);
    }
    
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Instruction result) {
        try {
            result = Parse(s, provider);
            return true;
        } catch {
            result = default;
            return false;
        }
    }
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

    public static string AsString(this InstructionKind kind) {
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