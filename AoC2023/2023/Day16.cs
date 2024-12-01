#region license

// AoC2023 - AoC2023 - Day16.cs
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

using System.Collections.Immutable;
using System.Text;
using AoC.Support;

namespace AoC2023._2023;

using Vertex = Vertex<int>;

public class Day16 : Adventer {
    private Problem problem;

    public Day16() {
        Bag["test"] =
            """
            .|...\....
            |.-.\.....
            .....|-...
            ........|.
            ..........
            .........\
            ..../.\\..
            .-.-/..|..
            .|....-|.\
            ..//.|....
            """;
    }

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        // var tracer = new Problem.TraceObserver(problem);
        var visited = new HashSet<Vertex>();
        foreach (var photon in problem.Trace())
            // tracer.Observe(photon);
            visited.Add(photon.Position);
        // var str = tracer.ToString();
        // Console.WriteLine(str);
        // Console.WriteLine();
        return visited.Count;
    }

    protected override object InternalPart2() {
        return problem.PossibleStarts()
            .AsParallel()
            .Select(p => problem.Energized(p).Count)
            .Max();
    }

    public readonly record struct Photon(Vertex Position, Direction Direction) {
        public Photon Move() {
            return this with { Position = Position + Direction.ToVertex<int>() };
        }

        public Photon Move(Direction direction) {
            return (this with { Direction = direction }).Move();
        }

        public override string ToString() {
            return $"{Position} {Direction.ToChar()}";
        }
    }

    public abstract record Tile {
        public abstract IEnumerable<Direction> Transit(Direction incoming);

        public abstract override string ToString();
    }

    public sealed record Mirror : Tile {
        public enum Orientation {
            Forward = 0,
            Backward = 1
        }

        public static readonly Mirror Forward = new(Orientation.Forward);
        public static readonly Mirror Backward = new(Orientation.Backward);

        private static readonly Direction[,] TransitTable = CreateTransitTable();

        private Mirror(Orientation Kind) {
            this.Kind = Kind;
        }

        public Orientation Kind { get; }

        private static Direction[,] CreateTransitTable() {
            var table = new Direction[2, 4];
            table[(int)Orientation.Forward, (int)Direction.Up] = Direction.Right;
            table[(int)Orientation.Forward, (int)Direction.Down] = Direction.Left;
            table[(int)Orientation.Forward, (int)Direction.Left] = Direction.Down;
            table[(int)Orientation.Forward, (int)Direction.Right] = Direction.Up;
            table[(int)Orientation.Backward, (int)Direction.Up] = Direction.Left;
            table[(int)Orientation.Backward, (int)Direction.Down] = Direction.Right;
            table[(int)Orientation.Backward, (int)Direction.Left] = Direction.Up;
            table[(int)Orientation.Backward, (int)Direction.Right] = Direction.Down;

            return table;
        }

        public override Direction[] Transit(Direction incoming) {
            return new[] { TransitTable[(int)Kind, (int)incoming] };
        }

        public override string ToString() {
            return Kind switch {
                Orientation.Forward => "/",
                Orientation.Backward => "\\",
                _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Invalid orientation")
            };
        }
    }

    public sealed record Splitter : Tile {
        public enum Orientation {
            UpDown = 0,
            LeftRight = 1
        }

        private static readonly Direction[][][] TransitTable = CreateTransitTable();

        public static readonly Splitter UpDown = new(Orientation.UpDown);
        public static readonly Splitter LeftRight = new(Orientation.LeftRight);

        private Splitter(Orientation kind) {
            Kind = kind;
        }

        public Orientation Kind { get; }

        private static Direction[][][] CreateTransitTable() {
            var lrSplit = new[] { Direction.Left, Direction.Right };
            var udSplit = new[] { Direction.Up, Direction.Down };
            var left = new[] { Direction.Left };
            var right = new[] { Direction.Right };
            var up = new[] { Direction.Up };
            var down = new[] { Direction.Down };
            var table = new[] {
                new[] {
                    // UpDown
                    up, // Up
                    down, // Down
                    udSplit, // Left
                    udSplit // Right
                },
                new[] {
                    // LeftRight
                    lrSplit, // Up
                    lrSplit, // Down
                    left, // Left
                    right // Right
                }
            };
            return table;
        }


        public override IEnumerable<Direction> Transit(Direction incoming) {
            return TransitTable[(int)Kind][(int)incoming];
        }

        public override string ToString() {
            return Kind switch {
                Orientation.UpDown => "|",
                Orientation.LeftRight => "-",
                _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Invalid orientation")
            };
        }
    }

    public sealed record Empty : Tile {
        public static readonly Empty Instance = new();

        private static readonly Direction[][] TransitTable = {
            new[] { Direction.Up },
            new[] { Direction.Down },
            new[] { Direction.Left },
            new[] { Direction.Right }
        };

        public override IEnumerable<Direction> Transit(Direction incoming) {
            return TransitTable[(int)incoming];
        }

        public override string ToString() {
            return ".";
        }
    }

    public class Problem {
        public Problem(string[] input) {
            Height = input.Length;
            Width = input[0].Length;
            for (var y = 0; y < Height; y++) {
                var row = input[y];
                for (var x = 0; x < Width; x++) {
                    var c = row[x];
                    if (c == '.') continue;

                    Tiles.Add(new Vertex(x, y), ParseTile(c));
                }
            }
        }

        private Dictionary<Vertex, Tile> Tiles { get; } = new();
        public int Height { get; }
        public int Width { get; }

        public static Tile ParseTile(char c) {
            return c switch {
                '/' => Mirror.Forward,
                '\\' => Mirror.Backward,
                '|' => Splitter.UpDown,
                '-' => Splitter.LeftRight,
                '.' => Empty.Instance,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, "Invalid tile")
            };
        }

        private Tile GetTile(Vertex vertex) {
            return Tiles.TryGetValue(vertex, out var tile) ? tile : Empty.Instance;
        }

        public IEnumerable<Photon> Trace(Photon photon) {
            var visited = new HashSet<Photon>();
            var frontier = new Queue<Photon>();
            frontier.Enqueue(photon);
            while (frontier.Count > 0) {
                var current = frontier.Dequeue();
                if (!visited.Add(current)) continue;

                yield return current;

                var tile = GetTile(current.Position);
                foreach (var direction in tile.Transit(current.Direction)) {
                    var next = current.Move(direction);
                    if (next.Position.ExistsInGrid(Width, Height)) frontier.Enqueue(next);
                }
            }
        }

        public IEnumerable<Photon> Trace() {
            var start = new Photon((0, 0), Direction.Right);
            return Trace(start);
        }

        public ISet<Vertex> Energized() {
            return Energized(new Photon((0, 0), Direction.Right));
        }

        public ISet<Vertex> Energized(Photon start) {
            var set = new HashSet<Vertex>();
            foreach (var photon in Trace(start)) set.Add(photon.Position);

            return set;
        }

        public IEnumerable<Photon> PossibleStarts() {
            var top = Vertex.TopEdgePoints(Width).Select(v => new Photon(v, Direction.Down));
            var bottom = Vertex.BottomEdgePoints(Width, Height).Select(v => new Photon(v, Direction.Up));
            var left = Vertex.LeftEdgePoints(Height).Select(v => new Photon(v, Direction.Right));
            var right = Vertex.RightEdgePoints(Width, Height).Select(v => new Photon(v, Direction.Left));
            return top.Concat(bottom).Concat(left).Concat(right);
        }

        public override string ToString() {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++) {
                var row = new char[Width];
                for (var x = 0; x < Width; x++) {
                    var vertex = new Vertex(x, y);
                    var tile = GetTile(vertex);
                    row[x] = tile.ToString()[0];
                }

                sb.AppendLine(new string(row));
            }

            return sb.ToString();
        }

        public class TraceObserver {
            private readonly ImmutableArray<ImmutableArray<char>> baseGrid;

            public TraceObserver(Problem problem) {
                Problem = problem;
                var builder = ImmutableArray.CreateBuilder<ImmutableArray<char>>(problem.Height);
                for (var y = 0; y < problem.Height; y++) {
                    var lineBuilder = ImmutableArray.CreateBuilder<char>(problem.Width);
                    for (var x = 0; x < problem.Width; x++) {
                        var vertex = new Vertex(x, y);
                        var tile = problem.GetTile(vertex);
                        lineBuilder.Add(tile.ToString()[0]);
                    }

                    builder.Add(lineBuilder.ToImmutable());
                }

                baseGrid = builder.ToImmutable();
            }

            private Problem Problem { get; }
            private Dictionary<Vertex, HashSet<Direction>> Transits { get; } = new();

            private HashSet<Direction> GetTransits(Vertex location) {
                if (Transits.TryGetValue(location, out var tr)) return tr;

                var transits = new HashSet<Direction>();
                Transits.Add(location, transits);
                return transits;
            }

            public void Observe(Photon photon) {
                var transits = GetTransits(photon.Position);
                transits.Add(photon.Direction);
            }

            public override string ToString() {
                var sb = new StringBuilder();
                for (var y = 0; y < Problem.Height; y++) {
                    var line = baseGrid[y].ToArray();
                    for (var x = 0; x < Problem.Width; x++) {
                        if (baseGrid[y][x] != '.') {
                            line[x] = baseGrid[y][x];
                            continue;
                        }

                        var vertex = new Vertex(x, y);
                        var transits = GetTransits(vertex);
                        var cnt = transits.Count;
                        line[x] = cnt switch {
                            0 => '.',
                            1 => transits.First().ToChar(),
                            > 1 and < 10 => (char)(cnt + '0'),
                            _ => '!'
                        };
                    }

                    sb.AppendLine(new string(line));
                }

                return sb.ToString().TrimEnd();
            }
        }
    }
}