#region license
// AoC2023 - AoC2023 - Day13.cs
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

namespace AoC2023._2023;

public class Day13: Adventer {
    public class Problem {
        private List<Grid<char>> grids = [];
        
        public Problem(string input) {
            foreach (var grp in input.Split("\n\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
                var lines = grp.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var grid = new Grid<char>(lines[0].Length, lines.Length, lines.SelectMany(l => l.AsEnumerable()));
                grids.Add(grid);
            }
        }

        public int HorizontalReflections(Grid<char> grid) {
            throw new NotImplementedException();
        }
    }
    
    protected override void InternalOnLoad() {
        base.InternalOnLoad();
    }

    protected override object InternalPart1() {
        throw new NotImplementedException();
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }
}
