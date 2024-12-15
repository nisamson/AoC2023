using System.Collections.Immutable;
using System.Text;
using AoC.Support;
using CommunityToolkit.HighPerformance;
using ILGPU.IR;

namespace AoC2023._2024;

using Frequency = char;

public class Day08 : Adventer {

    private static bool IsStation(Frequency b) => b != '.';

    private class Problem {
        private readonly Grid<Frequency> stations;
        private readonly Dictionary<Frequency, List<Vertex<int>>> stationLocations = new();
        
        public Problem(string[] stations) {
            var width = stations[0].Length;
            var height = stations.Length;
            this.stations = new Grid<Frequency>(width, height);
            for (var y = 0; y < height; y++) {
                var line = stations[y];
                for (var x = 0; x < width; x++) {
                    var freq = line[x];
                    this.stations[x, y] = freq;
                    if (IsStation(freq)) {
                        stationLocations.GetOrNew(freq).Add(new Vertex<int>(x, y));
                    }
                }
            }
        }
        
        private const bool Debug = false;

        public int Part1() {
            var antinodes = new HashSet<Vertex<int>>();
            foreach (var (_, locations) in stationLocations) {
                foreach (var (a, b) in locations.CartesianProduct()) {
                    var sep = b - a;
                    var antisep = -sep;
                    var antinodeA = a + antisep;
                    var antinodeB = b + sep;
                    if (stations.IsInBounds(antinodeA)) {
                        antinodes.Add(antinodeA);
                    }
                    
                    if (stations.IsInBounds(antinodeB)) {
                        antinodes.Add(antinodeB);
                    }
                }
                
                if (Debug) {
                    Console.WriteLine(PrintWithAntinodes(antinodes));
                }
            }
            
            return antinodes.Count;
        }

        public int Part2() {
            var antinodes = new HashSet<Vertex<int>>();
            foreach (var (_, locations) in stationLocations) {
                foreach (var (a, b) in locations.CartesianProduct()) {
                    var sep = b - a;
                    var antisep = -sep;
                    var antinodeA = a + antisep;
                    var antinodeB = b + sep;
                    antinodes.Add(a);
                    antinodes.Add(b);
                    
                    while (stations.IsInBounds(antinodeA)) {
                        antinodes.Add(antinodeA);
                        antinodeA += antisep;
                    }
                    
                    while (stations.IsInBounds(antinodeB)) {
                        antinodes.Add(antinodeB);
                        antinodeB += sep;
                    }
                }
                
                if (Debug) {
                    Console.WriteLine(PrintWithAntinodes(antinodes));
                }
            }
            
            return antinodes.Count;
        
        }

        private string PrintWithAntinodes(ISet<Vertex<int>> antinodes) {
            var sb = new StringBuilder();
            for (var y = 0; y < stations.Height; y++) {
                for (var x = 0; x < stations.Width; x++) {
                    var loc = new Vertex<int>(x, y);
                    if (antinodes.Contains(loc)) {
                        sb.Append('#');
                    } else {
                        sb.Append(stations[x, y]);
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
    
    
    public Day08() {
        Bag["test"] = """
                      ............
                      ........0...
                      .....0......
                      .......0....
                      ....0.......
                      ......A.....
                      ............
                      ............
                      ........A...
                      .........A..
                      ............
                      ............
                      """;
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