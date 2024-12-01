#region license

// AoC2023 - AoC2023 - Day01.cs
// Copyright (C) 2023 Nicholas Samson
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

using System.Text.RegularExpressions;

namespace AoC2023._2023;

public partial class Day01 : Adventer {
    protected override object InternalPart1() {
        return Input.Lines.Select(x => {
                var digits = x.Where(char.IsDigit).ToArray();
                return int.Parse($"{digits.First()}{digits.Last()}");
            })
            .Sum();
    }

    [GeneratedRegex(@"^((one)|(two)|(three)|(four)|(five)|(six)|(seven)|(eight)|(nine)|(\d))")]
    private static partial Regex MatchDigits();

    private static int GetDigit(string input) {
        return input switch {
            "one" or "1" => 1,
            "two" or "2" => 2,
            "three" or "3" => 3,
            "four" or "4" => 4,
            "five" or "5" => 5,
            "six" or "6" => 6,
            "seven" or "7" => 7,
            "eight" or "8" => 8,
            "nine" or "9" => 9,
            "0" => 0,
            _ => throw new ArgumentException("Invalid digit", nameof(input))
        };
    }


    private static IEnumerable<int> GetDigits(string input) {
        for (var i = 0; i < input.Length; i++) {
            var inp = input[i..];
            var match = MatchDigits().Match(inp);
            if (!match.Success) continue;

            var digit = GetDigit(match.Value);
            yield return digit;
        }
    }

    protected override object InternalPart2() {
        return Input.Lines.Select(x => {
                var digits = GetDigits(x).ToArray();
                var o = digits[0] * 10 + digits[^1];
                return o;
            })
            .Sum();
    }

    public void PrintNumbers() {
        foreach (var l in Input.Lines) {
            var digits = GetDigits(l).ToArray();
            var o = digits[0] * 10 + digits[^1];
            Console.WriteLine($"{l} -> {o}");
        }
    }
}