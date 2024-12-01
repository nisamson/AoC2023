#region license

// AoC2023 - AoC2023 - Day21.cs
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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AoC.Support;

namespace AoC2023._2023;

using Vertex = Vertex<int>;

public class Day21 : Adventer {
    public enum Tile {
        Plot = '.',
        Rock = '#'
    }

    private int part1Steps;

    private Problem problem;

    public Day21() {
        Bag["test"] = """
                      ...........
                      ....####.#.
                      .###.##..#.
                      ..#.#...#..
                      ....#.#....
                      .##..S####.
                      .##..#...#.
                      .......##..
                      .##.#.####.
                      .##..##.##.
                      ...........
                      """;
        Bag["part1Steps"] = "64";
    }

    [MemberNotNull(nameof(problem))]
    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
        part1Steps = int.Parse(Bag["part1Steps"]);
    }

    protected override object InternalPart1() {
        var p = new Problem(problem);
        Console.WriteLine(p.PrintDistances());
        Console.WriteLine(p.PrintReachableInExactly(part1Steps));
        return p.ReachableInExactly(part1Steps).Count();
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }

    public sealed class Problem {
        private readonly UniformDistanceMatrixGraph graphMap;
        private readonly Tile[,] map;
        private readonly UniformDistanceMatrixGraph reachableFromStart;

        public Problem(string[] input) {
            map = new Tile[input.Length, input[0].Length];
            for (var y = 0; y < input.Length; y++) {
                var line = input[y];
                for (var x = 0; x < line.Length; x++) {
                    var c = line[x];
                    if (c == 'S') Start = new Vertex(x, y);

                    map[y, x] = c switch {
                        '.' or 'S' => Tile.Plot,
                        '#' => Tile.Rock,
                        _ => throw new ArgumentOutOfRangeException(nameof(input), c,
                            "Invalid character at position " + x + "," + y)
                    };
                }
            }

            graphMap = new UniformDistanceMatrixGraph(Height, Width, TraversableTiles());
            foreach (var v in TraversableTiles()) {
                foreach (var n in GetTraversableNeighbors(v)) graphMap.AddEdge(v, n);
            }

            reachableFromStart = graphMap.SccStartingAt(Start);
        }

        public Problem(Problem p) {
            Start = p.Start;
            map = (Tile[,])p.map.Clone();
            graphMap = p.graphMap.Clone() as UniformDistanceMatrixGraph ?? throw new UnreachableException();
            reachableFromStart = p.reachableFromStart.Clone() as UniformDistanceMatrixGraph ??
                                 throw new UnreachableException();
        }

        public Vertex Start { get; }
        private int Width => map.GetLength(1);
        private int Height => map.GetLength(0);

        private IEnumerable<Vertex> GetTraversableNeighbors(Vertex v) {
            if (!v.ExistsInGrid(Width, Height))
                throw new ArgumentOutOfRangeException(nameof(v), v, "Vertex is not in grid");

            return v.GetNeighbors().Where(n => n.ExistsInGrid(Width, Height) && map[n.Y, n.X] == Tile.Plot);
        }

        public IEnumerable<Vertex> TraversableTiles() {
            for (var y = 0; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    var v = new Vertex(x, y);
                    if (map[y, x] == Tile.Plot) yield return v;
                }
            }
        }

        public IEnumerable<Vertex> ReachableInExactly(int steps) {
            Func<int, bool> pred = (steps % 2) switch {
                0 => dist => dist <= steps && dist % 2 == 0,
                1 => dist => dist <= steps && dist % 2 == 1,
                _ => throw new UnreachableException("mod 2 is not 0 or 1")
            };

            return TraversableTiles().Where(v => pred(Distance(Start, v) ?? int.MaxValue));
        }

        public bool IsTraversable(Vertex v) {
            return v.ExistsInGrid(Width, Height) && map[v.Y, v.X] == Tile.Plot;
        }

        public int? Distance(Vertex from, Vertex to) {
            if (!reachableFromStart.ContainsVertex(from) || !reachableFromStart.ContainsVertex(to)) return null;

            return reachableFromStart.GetDistance(from, to);
        }

        public string PrettyPrint(Func<Vertex, Tile, char?>? overrideChar = null) {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    var coord = new Vertex(x, y);
                    var tile = map[y, x];
                    var o = overrideChar?.Invoke(coord, tile);
                    if (o != null) {
                        sb.Append(o);
                        continue;
                    }

                    if (coord == Start)
                        sb.Append('S');
                    else
                        switch (tile) {
                            case Tile.Plot:
                                sb.Append('.');
                                break;
                            case Tile.Rock:
                                sb.Append('#');
                                break;
                            default:
                                throw new UnreachableException();
                        }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string PrintDistances(Vertex? from = null) {
            var start = from ?? Start;
            return PrettyPrint(DistanceOverride);

            char? DistanceOverride(Vertex v, Tile t) {
                if (v == start) return 'S';
                var d = Distance(start, v);
                return d switch {
                    < 10 => (char)('0' + d),
                    < 36 => (char)('a' + d - 10),
                    < 62 => (char)('A' + d - 36),
                    >= 62 => '!',
                    _ => null
                };
            }
        }

        public string PrintReachableInExactly(int steps) {
            var reachableInSteps = ReachableInExactly(steps).ToHashSet();
            return PrettyPrint(ReachableOverride);

            char? ReachableOverride(Vertex v, Tile t) {
                if (reachableInSteps.Contains(v)) return 'O';

                return null;
            }
        }

        public override string ToString() {
            return PrettyPrint();
        }
    }
}