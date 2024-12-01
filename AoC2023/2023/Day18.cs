#region license

// AoC2023 - AoC2023 - Day18.cs
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

using System.Drawing;
using System.Globalization;
using AoC.Support;
using MathNet.Numerics.LinearAlgebra.Double;
using NetTopologySuite.Geometries;
using QuikGraph;
using Point = NetTopologySuite.Geometries.Point;

namespace AoC2023._2023;

using Vertex = Vertex<int>;

public class Day18 : Adventer {
    private Problem problem;

    public Day18() {
        Bag["test"] =
            """
            R 6 (#70c710)
            D 5 (#0dc571)
            L 2 (#5713f0)
            D 2 (#d2c081)
            R 2 (#59c680)
            D 2 (#411b91)
            L 5 (#8ceee2)
            U 2 (#caa173)
            L 1 (#1b58a2)
            U 2 (#caa171)
            R 2 (#7807d2)
            U 3 (#a77fa3)
            L 2 (#015232)
            U 2 (#7a21e3)
            """;
    }

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }

    public record Tag(Color Color) {
        public Color Color = Color;
    }

    public record Movement(Direction Direction, int Length, Color Color);

    public class Problem {
        private readonly BidirectionalGraph<Vertex, TaggedEdge<Vertex, Tag>> graph;
        private readonly List<Movement> movements;
        private readonly Polygon polygon;

        public Problem(string[] input) {
            graph = new BidirectionalGraph<Vertex, TaggedEdge<Vertex, Tag>>();
            var start = new Vertex(0, 0);
            graph.AddVertex(start);
            var current = start;
            movements = new List<Movement>(input.Length);

            var coordinates = new Coordinate[input.Length + 1];
            coordinates[0] = new Coordinate(0, 0);
            for (var idx = 0; idx < input.Length; idx++) {
                var span = input[idx].AsSpan();
                var dir = DirectionExtensions.ParseRLUD(span[0]);
                const int startIdx = 2;
                var endIdx = input[idx].IndexOf(' ', startIdx);
                var length = int.Parse(span[startIdx..endIdx]);

                var remainder = span[(endIdx + 1)..].Trim().Trim("(#)");
                var rawColor = int.Parse(remainder, NumberStyles.HexNumber);
                var color = Color.FromArgb(rawColor);
                color = Color.FromArgb(0xff, color);

                var next = current + dir.ToVertex<int>() * length;
                graph.AddVertex(next);
                graph.AddEdge(new TaggedEdge<Vertex, Tag>(current, next, new Tag(color)));
                current = next;
                coordinates[idx + 1] = new Coordinate(current.X, current.Y);
                movements.Add(new Movement(dir, length, color));
            }

            polygon = new Polygon(new LinearRing(coordinates));
        }

        private (Matrix, Vertex) GetBitmapAndOffset() {
            var minX = graph.Vertices.Min(v => v.X);
            var maxX = graph.Vertices.Max(v => v.X);
            var minY = graph.Vertices.Min(v => v.Y);
            var maxY = graph.Vertices.Max(v => v.Y);
            var width = maxX - minX;
            var height = maxY - minY;
            var matrix = new DenseMatrix(width, height);
            var offset = new Vertex(-minX, -minY);
            var start = new Vertex(0, 0);
            var current = start;
            foreach (var movement in movements) {
                var end = current + movement.Direction.ToVertex<int>() * movement.Length;
                while (current != end) {
                    var offsetCurrent = current + offset;
                    matrix[offsetCurrent.X, offsetCurrent.Y] = 1;
                    current += movement.Direction.ToVertex<int>();
                }
            }

            return (matrix, offset);
        }

        private IEnumerable<Vertex> GetBoundaryVertices() {
            var current = Vertex.Zero;
            foreach (var movement in movements) {
                var end = current + movement.Direction.ToVertex<int>() * movement.Length;
                while (current != end) {
                    yield return current;
                    current += movement.Direction.ToVertex<int>();
                }
            }
        }

        public int Part1() {
            var visited = new HashSet<Vertex>(GetBoundaryVertices());
            var insiders = visited
                .SelectMany(v => v.GetNeighbors())
                .Where(v => polygon.Contains(new Point(v)));
            var frontier = new HashSet<Vertex>(insiders);
            while (frontier.Count > 0) {
                var current = frontier.First();
                frontier.Remove(current);
                if (!visited.Add(current)) continue;

                foreach (var neighbor in current.GetNeighbors()) frontier.Add(neighbor);
            }

            return visited.Count;
        }
    }
}