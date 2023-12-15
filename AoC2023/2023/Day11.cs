#region license

// AoC2023 - AoC2023 - Day11.cs
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

using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace AoC2023._2023;
using Vertex = Vertex<long>;

public class Day11 : Adventer {

    public Day11() {
        Bag["test"] = """
                      ...#......
                      .......#..
                      #.........
                      ..........
                      ......#...
                      .#........
                      .........#
                      ..........
                      .......#..
                      #...#.....
                      """;
    }
    public record Galaxy {
        public int Id { get; init; }
        public Vertex Coords { get; set; }

        public long X {
            get => Coords.X;
            set => Coords = Coords with {X = value};
        }

        public long Y {
            get => Coords.Y;
            set => Coords = Coords with {Y = value};
        }

        public Galaxy(int id = default, Vertex coords = default) { 
            Id = id;
            Coords = coords;
        }
    }

    public class Problem {
        private List<Galaxy> galaxies = [];
        
        public IReadOnlyList<Galaxy> Galaxies => galaxies;

        public Problem(IReadOnlyList<string> input) {
            var width = input[0].Length;
            var height = input.Count;
            var id = 0;
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    if (input[y][x] == '#') {
                        galaxies.Add(new Galaxy(id++, new Vertex(x, y)));
                    }
                }
            }
        }
        
        private Problem() {}

        public void ExpandStep(int expansionFactor = 2) {
            var seenColumns = new SortedList<int, byte>();
            var seenRows = new SortedList<int, byte>();

            foreach (var galaxy in galaxies) {
                seenColumns[(int)galaxy.X] = 0;
                seenRows[(int)galaxy.Y] = 0;
            }

            foreach (var galaxy in galaxies) {
                var colIdx = seenColumns.IndexOfKey((int)galaxy.X);
                var rowIdx = seenRows.IndexOfKey((int)galaxy.Y);
                var movementX = (galaxy.X - colIdx) * (expansionFactor - 1);
                var newX = galaxy.X + movementX;
                var movementY = (galaxy.Y - rowIdx) * (expansionFactor - 1);
                var newY = galaxy.Y + movementY;
                galaxy.Coords = new Vertex(newX, newY);
            }
        }
        
        public Problem Clone() {
            var clone = new Problem {
                galaxies = galaxies.Select(galaxy => new Galaxy(galaxy.Id, galaxy.Coords)).ToList(),
            };
            return clone;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            var width = galaxies.Max(galaxy => galaxy.X) + 1;
            var height = galaxies.Max(galaxy => galaxy.Y) + 1;
            for (var y = 0; y < height; y++) {
                var line = new char[width];
                for (var x = 0; x < width; x++) {
                    line[x] = galaxies.Any(galaxy => galaxy.X == x && galaxy.Y == y) ? '#' : '.';
                }

                sb.AppendLine(new string(line));
            }

            return sb.ToString();
        }
    }
    
    private Problem problem;

    private Problem ClonedProblem() {
        lock (problem) {
            return problem.Clone();
        }
    }

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }


    protected override object InternalPart1() {
        var problem = ClonedProblem();
        problem.ExpandStep();
        var galaxies = problem.Galaxies;
        var totalDistances = (long) 0;
        for (var i = 0; i < galaxies.Count; i++) {
            var g1 = galaxies[i];
            totalDistances += galaxies.Skip(i + 1).Select(g2 => g1.Coords.ManhattanDistanceTo(g2.Coords)).Sum();
        }

        return totalDistances;
    }

    protected override object InternalPart2() {
        var problem = ClonedProblem();
        problem.ExpandStep(1000000);
        var galaxies = problem.Galaxies;
        var totalDistances = (long) 0;
        for (var i = 0; i < galaxies.Count; i++) {
            var g1 = galaxies[i];
            totalDistances += galaxies.Skip(i + 1).Select(g2 => g1.Coords.ManhattanDistanceTo(g2.Coords)).Sum();
        }

        return totalDistances;
    }
}
