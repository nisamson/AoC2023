#region license
// AoC2023 - AoC2023 - Tests.cs
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

using AdventOfCodeSupport;
using AdventOfCodeSupport.Testing;
using Xunit;

namespace AoC2023;

public class Tests {
    private readonly AdventSolutions _solutions = new();

    private const string Day1TestInput = 
        """
        two1nine
        eightwothree
        abcone2threexyz
        xtwone3four
        4nineeightseven2
        zoneight234
        7pqrstsixteen
        """;
    
    [Fact]
    public void Day1() {
        var day = _solutions.GetDay(2023, 1);
        day.SetTestInput(Day1TestInput);
        Assert.Equal("281", day.Part2Answer);
    }
}
