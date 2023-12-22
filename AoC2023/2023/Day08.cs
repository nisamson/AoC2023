#region license

// AoC2023 - AoC2023 - Day08.cs
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
using Farkle;
using Farkle.Builder;

namespace AoC2023._2023;

public class Day08 : Adventer {
    internal enum Direction {
        Left,
        Right
    }

    internal static class DirectionExtensions {
        public static Direction Parse(string input) {
            return input switch {
                "L" => Direction.Left,
                "R" => Direction.Right,
                _   => throw new ArgumentOutOfRangeException(nameof(input), input, "Invalid direction")
            };
        }

        public static IEnumerable<Direction> ParseMany(string input) {
            return input.Select(Parse);
        }

        public static Direction Parse(char input) {
            return input switch {
                'L' => Direction.Left,
                'R' => Direction.Right,
                _   => throw new ArgumentOutOfRangeException(nameof(input), input, "Invalid direction")
            };
        }
    }

    internal record Node(string Name, string Left, string Right) {
        public string Name { get; } = Name;

        public string Left { get; } = Left;
        public string Right { get; } = Right;

        public override string ToString() {
            return $"{Name} = ({Left}, {Right})";
        }

        public static Node Parse(string s) {
            var parts = s.Split("=", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var name = parts[0];
            var lr = parts[1].Trim('(', ')').Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return new Node(name, lr[0], lr[1]);
        }
    }

    internal class Day08Problem(IReadOnlyList<Direction> directions, IReadOnlyDictionary<string, Node> nodes) {
        private IReadOnlyList<Direction> Directions { get; } = directions;
        private IReadOnlyDictionary<string, Node> Nodes { get; } = nodes;

        public static Day08Problem Parse(string[] input) {
            var nonEmpty = input.Where(s => !string.IsNullOrWhiteSpace(s));
            var directions = DirectionExtensions.ParseMany(input[0]).ToList();
            var nodes = nonEmpty.Skip(1).Select(Node.Parse).ToDictionary<Node, string>(n => n.Name);
            return new Day08Problem(directions, nodes);
        }

        public int NavigatePart1() {
            var curr = "AAA";
            var stepsTaken = 0;
            while (curr != "ZZZ") {
                var node = Nodes[curr];
                var dir = Directions[stepsTaken % Directions.Count];
                curr = dir switch {
                    Direction.Left  => node.Left,
                    Direction.Right => node.Right,
                    _               => throw new ArgumentOutOfRangeException(nameof(dir), dir, "Invalid direction")
                };
                stepsTaken++;
            }

            return stepsTaken;
        }

        public string Step(string curr, int step) {
            var node = Nodes[curr];
            var dir = Directions[step % Directions.Count];
            return dir switch {
                Direction.Left  => node.Left,
                Direction.Right => node.Right,
                _               => throw new ArgumentOutOfRangeException(nameof(dir), dir, "Invalid direction"),
            };
        }

        public int NavigatePart2Single(string start) {
            var curr = start;
            var stepsTaken = 0;
            while (!curr.EndsWith('Z')) {
                var node = Nodes[curr];
                var dir = Directions[stepsTaken % Directions.Count];
                curr = dir switch {
                    Direction.Left  => node.Left,
                    Direction.Right => node.Right,
                    _               => throw new ArgumentOutOfRangeException(nameof(dir), dir, "Invalid direction"),
                };
                stepsTaken++;
            }

            return stepsTaken;
        }

        public long NavigatePart2() {
            return Nodes.Keys
                .Where(s => s.EndsWith('A'))
                .AsParallel()
                .Select(NavigatePart2Single)
                .Select(i => (long) i)
                .Aggregate(MathUtils.Lcm);
        }
    }

    class Day08Lang {
        public static readonly PrecompilableDesigntimeFarkle<Day08Problem> Designtime;
        public static readonly RuntimeFarkle<Day08Problem> Runtime;

        public static IReadOnlyList<Direction> ParseDirections(ReadOnlySpan<char> span) {
            var list = new List<Direction>();
            foreach (var c in span) {
                list.Add(
                    c switch {
                        'L' => Direction.Left,
                        'R' => Direction.Right,
                        _   => throw new ArgumentOutOfRangeException(nameof(span), c, "Invalid direction")
                    }
                );
            }

            return list;
        }

        static Day08Lang() {
            var directionTerm = Terminal.Create("directionsToken", (_, data) => ParseDirections(data), Regex.FromRegexString("[LR]+"))
                .Extended().AsIs();

            var nodeName = Terminal.Create("nodeName", (_, data) => new string(data), Regex.FromRegexString("[A-Z]{3}"));
            var directions = Nonterminal.Create(
                "directions",
                directionTerm,
                nodeName.Finish(s => ParseDirections(s.AsSpan()))
            );

            var node = Nonterminal.Create(
                "node",
                nodeName
                    .Extended()
                    .Append("=")
                    .Append("(")
                    .Extend(nodeName)
                    .Append(",")
                    .Extend(nodeName)
                    .Append(")")
                    .Finish((name, left, right) => new Node(name, left, right))
            );

            var nodes = node.Many<Node, List<Node>>();
            var problem = Nonterminal.Create(
                "problem",
                directions.Extended().Extend(nodes).Finish(
                    (directions, nodes) => new Day08Problem(directions, nodes.ToDictionary<Node, string>(n => n.Name))
                )
            );
            Designtime = problem
                .CaseSensitive(true)
                .MarkForPrecompile();
            Runtime = Designtime.Build();
        }
    }

    public Day08() {
        Bag["test"] = """
                      LR

                      HHA = (HHB, XXX)
                      HHB = (XXX, HHZ)
                      HHZ = (HHB, XXX)
                      JJA = (JJB, XXX)
                      JJB = (JJC, JJC)
                      JJC = (JJZ, JJZ)
                      JJZ = (JJB, JJB)
                      XXX = (XXX, XXX)
                      """;
    }

    private Day08Problem problem;

    protected override void InternalOnLoad() {
        // var res = Day08Lang.Runtime.Parse(Input.Text);
        // if (res.IsOk) {
        //     problem = res.ResultValue;
        // } else {
        //     throw new Exception(res.ErrorValue.ToString());
        // }
        problem = Day08Problem.Parse(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.NavigatePart1();
    }

    protected override object InternalPart2() {
        return problem.NavigatePart2();
    }
}
