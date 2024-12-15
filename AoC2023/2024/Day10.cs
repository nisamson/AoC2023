using AoC.Support;
using NetTopologySuite.IO;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.RankedShortestPath;

namespace AoC2023._2024;

public class Day10 : Adventer {

    public Day10() {
        Bag["test"] = """
                      89010123
                      78121874
                      87430965
                      96549874
                      45678903
                      32019012
                      01329801
                      10456732
                      """;
    }

    private class Problem {
        private readonly List<Vertex<int>> trailHeads = new();
        private readonly List<Vertex<int>> trailEnds = new();
        private readonly Grid<byte> topography;
        
        private readonly BidirectionalGraph<Vertex<int>, Edge<Vertex<int>>> walkability = new();

        public Problem(string[] topography) {
            var width = topography[0].Length;
            var height = topography.Length;
            this.topography = new Grid<byte>(width, height);
            for (var y = 0; y < height; y++) {
                var line = topography[y];
                for (var x = 0; x < width; x++) {
                    var elev = line[x];
                    this.topography[x, y] = (byte)(elev - '0');
                    if (elev == '0') {
                        trailHeads.Add(new Vertex<int>(x, y));
                    } else if (elev == '9') {
                        trailEnds.Add(new Vertex<int>(x, y));
                    }
                }
            }

            Func<Vertex<int>, bool> boundsCheck = v => this.topography.IsInBounds(v);

            foreach (var (vert, elev) in this.topography.EnumerateIndexed()) {
                var neighbors = vert.GetNeighbors().Where(boundsCheck);
                foreach (var neighbor in neighbors) {
                    var neighborElev = this.topography[neighbor];
                    if (elev - neighborElev == 1) {
                        walkability.AddVerticesAndEdge(new Edge<Vertex<int>>(neighbor, vert));
                    }
                }
            }
        }

        public int Part1() {
            var sum = 0;
            var transitiveClosure = walkability.ComputeTransitiveClosure((a, b) => new(a, b));
            foreach (var trailHead in trailHeads) {
                var trailDifficulty = 0;
                foreach (var trailEnd in trailEnds) {
                    if (transitiveClosure.ContainsEdge(trailHead, trailEnd)) {
                        trailDifficulty += 1;
                    }
                }
                sum += trailDifficulty;
            }
            return sum;
        }

        public int Part2() {
            var sum = 0;
            foreach (var trailHead in trailHeads) {
                var trailDifficulty = 0;
                foreach (var trailEnd in trailEnds) {
                    var alg = new HoffmanPavleyRankedShortestPathAlgorithm<Vertex<int>, Edge<Vertex<int>>>(walkability,
                        _ => 1.0) {
                        ShortestPathCount = 100
                    };
                    alg.Compute(trailHead, trailEnd);
                    trailDifficulty += alg.ComputedShortestPathCount;
                }
                sum += trailDifficulty;
            }
            return sum;
        }
    }

    private Problem problem;
    
    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }

    protected override object InternalPart2() {
        return problem.Part2();
    }
}