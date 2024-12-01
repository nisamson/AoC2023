#region license

// AoC2023 - AoC2023 - Day03.cs
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

using AoC.Support;

namespace AoC2023._2023;

using Vertex = Vertex<int>;

public class Day03 : Adventer {
    protected override object InternalPart1() {
        throw new NotImplementedException();
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }

    public struct Number {
        public int Value { get; set; }
        public Vertex Coords { get; set; }
        public long X => Coords.X;
        public long Y => Coords.Y;

        public Number(int value, int x, int y) {
            Value = value;
        }

        public override string ToString() {
            return $"{Value} ({X}, {Y})";
        }

        public IEnumerable<int> DecimalDigits() {
            var v = Value;
            while (v > 0) {
                yield return v % 10;
                v /= 10;
            }
        }

        public int DecimalLength => (int)Math.Log10(Value) + 1;
    }

    public class Problem { }
}