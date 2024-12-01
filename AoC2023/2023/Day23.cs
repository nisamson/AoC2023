#region license

// AoC2023 - AoC2023 - Day23.cs
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
using MathNet.Numerics;
using QuikGraph;
using QuikGraph.Algorithms;

namespace AoC2023._2023;

using Vertex = Vertex<int>;

public class Day23 : Adventer {
    public Day23() {
        Bag["test"] = """
                      #.#####################
                      #.......#########...###
                      #######.#########.#.###
                      ###.....#.>.>.###.#.###
                      ###v#####.#v#.###.#.###
                      ###.>...#.#.#.....#...#
                      ###v###.#.#.#########.#
                      ###...#.#.#.......#...#
                      #####.#.#.#######.#.###
                      #.....#.#.#.......#...#
                      #.#####.#.#.#########v#
                      #.#...#...#...###...>.#
                      #.#.#v#######v###.###v#
                      #...#.>.#...>.>.#.###.#
                      #####v#.#.###v#.#.###.#
                      #.....#...#...#.#.#...#
                      #.#########.###.#.#.###
                      #...###...#...#...#.###
                      ###.###.#.###v#####v###
                      #...#...#.#.>.>.#.>.###
                      #.###.###.#.###.#.#v###
                      #.....###...###...#...#
                      #####################.#
                      """;
    }

    protected override object InternalPart1() {
        var mountain = new Mountain(Input.Lines);
        var path = mountain.LongestHike();
        return path.Count() - 1;
    }

    protected override object InternalPart2() {
        var mountain = new Mountain(Input.Lines, false);
        var path = mountain.LongestHike();
        return path.Count() - 1;
    }

    public abstract record Tile {
        public abstract bool Traversable { get; }
        public abstract IEnumerable<Direction> ValidExitDirections();

        public static Tile Parse(char c) {
            return c switch {
                '.' => Path.Instance,
                '#' => Forest.Instance,
                '^' => Slope.Up,
                'v' => Slope.Down,
                '<' => Slope.Left,
                '>' => Slope.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, "Invalid tile character")
            };
        }
    }

    public record Path : Tile {
        public static readonly Path Instance = new();
        private Path() { }
        public override bool Traversable => true;

        public override IEnumerable<Direction> ValidExitDirections() {
            return Enum.GetValues<Direction>();
        }
    }

    public record Forest : Tile {
        public static readonly Forest Instance = new();
        private Forest() { }
        public override bool Traversable => false;

        public override IEnumerable<Direction> ValidExitDirections() {
            return Array.Empty<Direction>();
        }
    }

    public record Slope : Tile {
        public static readonly Slope Up = new(Direction.Up);
        public static readonly Slope Down = new(Direction.Down);
        public static readonly Slope Left = new(Direction.Left);
        public static readonly Slope Right = new(Direction.Right);

        private Slope(Direction direction) {
            Direction = direction;
        }

        public Direction Direction { get; }
        public override bool Traversable => true;

        public override IEnumerable<Direction> ValidExitDirections() {
            return Direction.Once();
        }
    }

    public class Mountain {
        private readonly BidirectionalMatrixGraph<Vertex, Edge<Vertex>> graph;

        private readonly Tile[,] map;

        static Mountain() {
            Control.UseBestProviders();
        }

        public Mountain(IReadOnlyList<string> input, bool directedSlopes = true) {
            var height = input.Count;
            var width = input[0].Length;
            map = new Tile[height, width];
            for (var y = 0; y < height; y++) {
                var line = input[y];
                for (var x = 0; x < width; x++) {
                    var t = Tile.Parse(line[x]);
                    map[y, x] = t switch {
                        Slope _ when !directedSlopes => Path.Instance,
                        _ => t
                    };
                }
            }

            graph = BidirectionalMatrixGraph<Vertex, Edge<Vertex>>.ForVertices(Height, Width);
            for (var y = 0; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    var tile = map[y, x];
                    var v = new Vertex(x, y);
                    if (!tile.Traversable) continue;

                    foreach (var direction in tile.ValidExitDirections()) {
                        var neighbor = v.GetNeighbor(direction);
                        if (!neighbor.ExistsInGrid(Width, Height)) continue;

                        // if that tile is pointing back at us, we can't go there
                        if (map[neighbor.Y, neighbor.X] is Slope s && s.Direction == direction.Opposite()) continue;

                        var neighborTile = map[neighbor.Y, neighbor.X];
                        if (!neighborTile.Traversable) continue;

                        graph.AddEdge(new Edge<Vertex>(v, neighbor));
                    }
                }
            }

            Start = map.EnumerateRow(0)
                .Select((t, x) => (new Vertex(x, 0), t))
                .Where(tuple => tuple.t.Traversable)
                .Select(tuple => tuple.Item1)
                .First();
            End = map.EnumerateRow(Height - 1)
                .Select((t, x) => (new Vertex(x, Height - 1), t))
                .Where(t => t.t.Traversable)
                .Select(t => t.Item1)
                .First();
        }

        public int Width => map.GetLength(1);
        public int Height => map.GetLength(0);
        public Vertex Start { get; }
        public Vertex End { get; }

        public IEnumerable<Vertex> LongestHike() {
            if (IsCyclic()) return LongestHikeNoCycles();

            var alg =
                new LongestSimplePathAlgorithm<Vertex, Edge<Vertex>, BidirectionalMatrixGraph<Vertex, Edge<Vertex>>>(
                    graph,
                    Start,
                    End,
                    (s, t) => new Edge<Vertex>(s, t),
                    null,
                    _ => 1
                );
            return alg.Compute();
        }

        private bool IsCyclic() {
            return graph.IsDirectedAcyclicGraph();
        }

        private IEnumerable<Vertex> LongestHikeNoCycles() {
            var alg = graph.ShortestPathsDijkstra(e => -1, Start);
            if (!alg.Invoke(End, out var path)) throw new Exception("No path found");

            return path.Select(e => e.Source).Append(End);
        }
    }
}