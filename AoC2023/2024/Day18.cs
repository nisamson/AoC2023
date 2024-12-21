using System.Collections.Immutable;
using System.Text;
using AoC.Support;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.ShortestPath;

namespace AoC2023._2024;

public class Day18 : Adventer {
    
    public Day18() {
        Bag["test"] = """
                      5,4
                      4,2
                      4,5
                      3,0
                      2,1
                      6,3
                      2,4
                      1,5
                      0,6
                      3,3
                      2,6
                      5,1
                      1,2
                      5,5
                      2,5
                      6,5
                      1,4
                      0,4
                      6,4
                      1,1
                      6,1
                      1,0
                      0,5
                      1,6
                      2,0
                      """;
    }

    private enum Space {
        Empty,
        Wall
    }

    private class Problem {
        private static readonly Vertex<int> Origin = new(0, 0);
        private readonly Vertex<int> Goal;
        private readonly Grid<Space> grid;
        private readonly BidirectionalGraph<Vertex<int>, Edge<Vertex<int>>> graph;
        private readonly ImmutableArray<Vertex<int>> remaining;

        public Problem(ReadOnlySpan<string> lines, int width, int height, int steps = 1) {
            Goal = new Vertex<int>(width - 1, height - 1);
            grid = new Grid<Space>(width, height);
            Span<Range> ranges = stackalloc Range[2];
            foreach (var line in lines[..steps]) {
                line.AsSpan().Split(ranges, ',');
                var x = int.Parse(line[ranges[0]]);
                var y = int.Parse(line[ranges[1]]);
                grid[x, y] = Space.Wall;
            }

            var builder = ImmutableArray.CreateBuilder<Vertex<int>>(lines.Length - steps);
            foreach (var line in lines[steps..]) {
                line.AsSpan().Split(ranges, ',');
                var x = int.Parse(line[ranges[0]]);
                var y = int.Parse(line[ranges[1]]);
                builder.Add(new Vertex<int>(x, y));
            } 
            remaining = builder.ToImmutable();

            graph = new BidirectionalGraph<Vertex<int>, Edge<Vertex<int>>>(false);
            foreach (var (vertex, space) in grid.EnumerateIndexed()) {
                if (space == Space.Wall) {
                    continue;
                }

                graph.AddVertex(vertex);
                foreach (var neighbor in vertex.GetNeighbors()) {
                    if (!grid.IsInBounds(neighbor) || grid[neighbor] == Space.Wall) {
                        continue;
                    }
                    graph.AddVerticesAndEdge(new Edge<Vertex<int>>(vertex, neighbor));
                    graph.AddVerticesAndEdge(new Edge<Vertex<int>>(neighbor, vertex));
                }
            }

            graph.RemoveOutEdgeIf(Goal, _ => true);
            graph.RemoveInEdgeIf(Origin, _ => true);
        }

        public int Part1() {
            var pathFunc = graph.ShortestPathsAStar(_ => 1, v => Origin.ManhattanDistanceTo(v), Origin);
            var hasPath = pathFunc(Goal, out var path);

            path ??= Array.Empty<Edge<Vertex<int>>>();
            var vertices = path.SelectMany(edge => new[] { edge.Source, edge.Target }).ToHashSet();
            vertices.Remove(Origin);
            Console.WriteLine(PrintGridWithPath(vertices));
            if (!hasPath) {
                throw new ("No path found");
            }
            return vertices.Count;
        }

        public string Part2() {
            var cloned = graph.Clone();
            foreach (var vertex in remaining) {
                cloned.RemoveVertex(vertex);
                // var pathAlg = new AStarShortestPathAlgorithm<Vertex<int>, Edge<Vertex<int>>>(cloned, _ => 1,
                //     v => Origin.ManhattanDistanceTo(v), DistanceRelaxers.ShortestDistance);
                // pathAlg.Compute(Origin);
                // if (!pathAlg.TryGetDistance(Goal, out _)) {
                //     return $"{vertex.X},{vertex.Y}";
                // }
                
                var pathFunc = cloned.ShortestPathsAStar(_ => 1, v => Origin.ManhattanDistanceTo(v), Origin);
                var hasPath = pathFunc(Goal, out var path);

                // path ??= Array.Empty<Edge<Vertex<int>>>();
                // var vertices = path.SelectMany(edge => new[] { edge.Source, edge.Target }).ToHashSet();
                // vertices.Remove(Origin);
                
                if (!hasPath) {
                    return $"{vertex.X},{vertex.Y}";
                }
                //
                // Console.WriteLine("Removed " + vertex);
                // Console.WriteLine(PrintGridWithPath(vertices));
            }
            
            throw new ("No path found");
        }
        
        private string PrintGridWithPath(ISet<Vertex<int>>? path = null) {
            path ??= new HashSet<Vertex<int>>();
            var sb = new StringBuilder();
            for (var y = 0; y < grid.Height; y++) {
                for (var x = 0; x < grid.Width; x++) {
                    var vertex = new Vertex<int>(x, y);
                    if (vertex == Origin) {
                        sb.Append('@');
                    } else if (vertex == Goal) {
                        sb.Append('X');
                    } else if (path.Contains(vertex)) {
                        sb.Append('O');
                    } else {
                        sb.Append(grid[x, y] == Space.Empty ? '.' : '#');
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    private Problem problem;
    
    protected override void InternalOnLoad() {
        if (Input.Text == Bag["test"]) {
            problem = new Problem(Input.Lines, 7, 7, 12);
        } else {
            problem = new Problem(Input.Lines, 71, 71, 1024);
        }
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }
    protected override object InternalPart2() {
        return problem.Part2();
    }
}