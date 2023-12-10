#region license

// AoC2023 - AoC2023 - Day10.cs
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

using NetTopologySuite.Geometries;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Search;

namespace AoC2023._2023;

internal enum PipeDirection {
    Up,
    Down,
    Left,
    Right
}

internal readonly record struct Vertex(int X, int Y) {
    public override string ToString() {
        return $"({X}, {Y})";
    }
    
    public void Deconstruct(out int x, out int y) {
        x = X;
        y = Y;
    }
    
    public bool ExistsInGrid(int width, int height) {
        return X >= 0 && X < width && Y >= 0 && Y < height;
    }
    
    public Vertex GetNeighbor(PipeDirection direction) {
        return direction switch {
            PipeDirection.Up => new Vertex(X, Y - 1),
            PipeDirection.Down => new Vertex(X, Y + 1),
            PipeDirection.Left => new Vertex(X - 1, Y),
            PipeDirection.Right => new Vertex(X + 1, Y),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
        };
    }
}

class PipeGrid {
    public Vertex Start { get; }
    
    public int VertexToCoords(Vertex vertex) {
        return vertex.Y * Height + vertex.X;
    }

    public int Height { get; }
    public int Width { get; }
    public int VertexCount => Height * Width;

    private BidirectionalGraph<Vertex, Edge<Vertex>> Graph { get; }

    public PipeGrid(IReadOnlyList<string> input) {
        Height = input.Count;
        Width = input[0].Length;
        Graph = new BidirectionalGraph<Vertex, Edge<Vertex>>(false, VertexCount);
        for (var y = 0; y < Height; y++) {
            var line = input[y];
            for (var x = 0; x < Width; x++) {
                var vertex = new Vertex(x, y);
                var c = line[x];
                if (c == 'S') {
                    Start = vertex;
                }

                foreach (var vertexNeighbor in ConnectedDirections(c).Select(vertex.GetNeighbor)) {
                    if (vertexNeighbor.ExistsInGrid(Width, Height)) {
                        Graph.AddVerticesAndEdge(new Edge<Vertex>(vertex, vertexNeighbor));
                    }
                }
            }
        }

        var startInEdges = Graph.InEdges(Start);
        foreach (var edge in startInEdges) {
            var source = edge.Source;
            Graph.AddEdge(new Edge<Vertex>(Start, source));
        }
    }

    public static IEnumerable<PipeDirection> ConnectedDirections(char c) {
        // Up
        if (c is '|' or 'L' or 'J') {
            yield return PipeDirection.Up;
        }
        
        // Down
        if (c is '|' or 'F' or '7') {
            yield return PipeDirection.Down;
        }
        
        // Left
        if (c is '-' or 'J' or '7') {
            yield return PipeDirection.Left;
        }
        
        // Right
        if (c is '-' or 'L' or 'F') {
            yield return PipeDirection.Right;
        }
    }

    public int DistanceToFarthestFromStart() {
        var distances = DistancesFromStart();
        
        return distances.Values.Max();
    }

    private Dictionary<Vertex, int> DistancesFromStart() {
        var distances = new Dictionary<Vertex, int>();
        var frontier = new Queue<Vertex>();
        frontier.Enqueue(Start);
        distances[Start] = 0;
        while (frontier.Count > 0) {
            var current = frontier.Dequeue();
            foreach (var neighbor in Graph.OutEdges(current).Select(edge => edge.Target)) {
                if (distances.ContainsKey(neighbor)) {
                    continue;
                }

                frontier.Enqueue(neighbor);
                distances[neighbor] = distances[current] + 1;
            }
        }

        return distances;
    }

    public IEnumerable<Vertex> GetMainLoopRing() {
        var prev = Start;
        yield return Start;
        var current = Graph.OutEdges(Start).Select(edge => edge.Target).First();
        while (current != Start) {
            yield return current;
            var next = Graph.OutEdges(current).Select(edge => edge.Target).First(neighbor => neighbor != prev);
            prev = current;
            current = next;
        }
    }
    
    public Polygon GetMainLoopPolygon() {
        var ring = GetMainLoopRing().Append(Start).Select(vertex => new Coordinate(vertex.X, vertex.Y)).ToArray();
        return new Polygon(new LinearRing(ring));
    }
    
    public IEnumerable<Vertex> VerticesInsideLoop() {
        var loop = GetMainLoopPolygon();
        for (var y = 0; y < Height; y++) {
            for (var x = 0; x < Width; x++) {
                var vertex = new Vertex(x, y);
                if (loop.Contains(new Point(vertex.X, vertex.Y))) {
                    yield return vertex;
                }
            }
        }
    }
}

public class Day10 : Adventer {
    public Day10() {
        Bag["test"] = """
                      FF7FSF7F7F7F7F7F---7
                      L|LJ||||||||||||F--J
                      FL-7LJLJ||||||LJL-77
                      F--JF--7||LJLJ7F7FJ-
                      L---JF-JLJ.||-FJLJJ7
                      |F|F-JF---7F7-L7L|7|
                      |FFJF7L7F-JF7|JL---7
                      7-L-JL7||F7|L7F-7F7|
                      L.L7LFJ|||||FJL7||LJ
                      L7JLJL-JLJLJL--JLJ.L
                      """;
    }

    private PipeGrid grid;

    protected override void InternalOnLoad() {
        grid = new PipeGrid(Input.Lines);
    }

    protected override object InternalPart1() {
        return grid.DistanceToFarthestFromStart();
    }

    protected override object InternalPart2() {
        return grid.VerticesInsideLoop().Count();
    }
}
