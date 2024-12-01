#region license

// AoC2023 - AoC2023 - Day15.cs
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

using System.Collections;
using System.Text;

namespace AoC2023._2023;

public class Lens(string name, uint focalLength) {
    public string Name { get; } = name;
    public uint FocalLength { get; } = focalLength;

    public override string ToString() {
        return $"[{Name} {FocalLength}]";
    }
}

public class Day15AssocList : IEnumerable<Lens> {
    private readonly List<Lens> lenses = new();

    public int Count => lenses.Count;


    public IEnumerator<Lens> GetEnumerator() {
        return lenses.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void Upsert(Lens lens) {
        for (var i = 0; i < lenses.Count; i++) {
            if (lenses[i].Name != lens.Name) continue;

            lenses[i] = lens;
            return;
        }

        lenses.Add(lens);
    }

    public void Remove(string name) {
        Remove(name.AsSpan());
    }

    public void Remove(ReadOnlySpan<char> name) {
        for (var i = 0; i < lenses.Count; i++) {
            if (!lenses[i].Name.AsSpan().Equals(name, StringComparison.Ordinal)) continue;

            lenses.RemoveAt(i);
            return;
        }
    }

    public ulong Value() {
        return (ulong)lenses.Select((l, i) => l.FocalLength * (i + 1)).Sum();
    }

    public override string ToString() {
        var builder = new StringBuilder();
        builder.AppendJoin(" ", lenses);
        return builder.ToString();
    }
}

public class Day15HashMap {
    private readonly Day15AssocList[] buckets = new Day15AssocList[256];

    public Day15HashMap() {
        for (var i = 0; i < buckets.Length; i++) buckets[i] = new Day15AssocList();
    }

    public void Upsert(Lens lens) {
        var idx = Hasher.Hash(lens.Name);
        buckets[idx].Upsert(lens);
    }

    public void Remove(ReadOnlySpan<char> name) {
        var idx = Hasher.Hash(name);
        buckets[idx].Remove(name);
    }

    public ulong CalculateValue() {
        var total = 0ul;
        for (var i = 0; i < buckets.Length; i++) {
            var bucket = buckets[i];
            var bucketTotal = bucket.Value() * (ulong)(i + 1);
            total += bucketTotal;
        }

        return total;
    }

    public override string ToString() {
        var builder = new StringBuilder();
        for (var i = 0; i < buckets.Length; i++) {
            var bucket = buckets[i];
            if (bucket.Count == 0) continue;

            builder.AppendLine($"Box {i}: {bucket}");
        }

        // Remove trailing newline
        if (builder.Length > 0) builder.Length--;

        return builder.ToString();
    }
}

public class Hasher {
    public const byte Mult = 17;

    public static IEnumerable<byte> HashSeq(string input) {
        byte hash = 0;
        foreach (var c in input)
            switch (c) {
                case ',':
                    yield return hash;
                    hash = 0;
                    break;
                case '\n':
                    break;
                default:
                    hash += (byte)c;
                    hash *= Mult;
                    break;
            }

        yield return hash;
    }

    public static byte Hash(ReadOnlySpan<char> input) {
        byte hash = 0;
        foreach (var c in input)
            switch (c) {
                case '\n':
                    break;
                default:
                    hash += (byte)c;
                    hash *= Mult;
                    break;
            }

        return hash;
    }

    public static byte Hash(string input) {
        return Hash(input.AsSpan());
    }
}

public class Day15 : Adventer {
    public Day15() {
        Bag["test"] = """
                      rn=1,cm-,qp=3,cm=2,qp-,pc=4,ot=9,ab=5,pc-,pc=6,ot=7
                      """;
    }

    protected override object InternalPart1() {
        // Console.WriteLine(string.Join(", ", Hasher.Hash(Input.Text)));
        return Hasher.HashSeq(Input.Text).Aggregate(0, (a, b) => a + b);
    }

    protected override object InternalPart2() {
        var map = new Day15HashMap();

        var span = Input.Text.AsSpan();
        while (span.Length > 0) {
            var idx = span.IndexOf(',');
            if (idx == -1) idx = span.Length;

            var lens = span[..idx];

            if (idx + 1 >= span.Length)
                span = span[span.Length..];
            else
                span = span[(idx + 1)..];

            var idx2 = lens.IndexOfAny("=-".AsSpan());
            if (lens[idx2] == '=') {
                var name = lens[..idx2].ToString();
                var focalLength = uint.Parse(lens[(idx2 + 1)..]);
                map.Upsert(new Lens(name, focalLength));
            }
            else {
                var name = lens[..idx2];
                map.Remove(name);
            }
            // Console.WriteLine($"After {lens}:");
            // Console.WriteLine(map);
            // Console.WriteLine();
        }

        return map.CalculateValue();
    }
}