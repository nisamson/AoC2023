using System.Collections.Immutable;
using System.Text.RegularExpressions;
using AoC.Support;
using QuikGraph;

namespace AoC2023._2024;

public  class Day04 : Adventer {

    private class Problem {
        private readonly Grid<char> grid;
        private readonly ImmutableArray<Vertex<int>> starts;
        private readonly BidirectionalGraph<Vertex<int>, Edge<Vertex<int>>> graph;
        
        public Problem(string[] lines) {
            var width = lines[0].Length;
            var height = lines.Length;
            var starts = ImmutableArray.CreateBuilder<Vertex<int>>();
            grid = new Grid<char>(width, height);
            for (var y = 0; y < height; y++) {
                var line = lines[y];
                for (var x = 0; x < width; x++) {
                    grid[x, y] = line[x];
                    if (line[x] == 'X') {
                        starts.Add(new(x, y));
                    } 
                }
            }
            
            graph = new BidirectionalGraph<Vertex<int>, Edge<Vertex<int>>>();
            this.starts = starts.ToImmutable();
        }

        public int Part1() {
            throw new NotImplementedException();
        }
        
        private bool InBounds(Vertex<int> vertex) {
            return grid.IsInBounds(vertex);
        }

        private IEnumerable<Vertex<int>> EnumerateNeighbors(Vertex<int> vertex) {
            return vertex.GetNeighborsWithDiagonals().Where(v => grid.IsInBounds(v));
        }
        
        private IEnumerable<Vertex<int>> EnumerateNeighbors(Vertex<int> vertex, char c) {
            return EnumerateNeighbors(vertex).Where(v => grid[v] == c);
        }
    }

    private Problem problem;

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }

    public Day04() {
        Bag["test"] = """
                      MMMSXXMASM
                      MSAMXMSMSA
                      AMXSXMAAMM
                      MSAMASMSMX
                      XMASAMXAMM
                      XXAMMXXAMA
                      SMSMSASXSS
                      SAXAMASAAA
                      MAMMMXMMMM
                      MXMXAXMASX
                      """; // 18
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }
}