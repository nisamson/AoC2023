using System.Collections.Immutable;

namespace AoC2023._2024;

public class Day02 : Adventer {
    private readonly record struct Report {

        public Report(string line) {
            Levels = [..line.Split().Select(int.Parse)];
        }
        
        public ImmutableArray<int> Levels { get; private init;  }

        public bool Safe => Monotonic && ChangingSafely;
        public bool Monotonic {
            get {
                bool? increasing = default;
                foreach (var (a, b) in Levels.Zip(Levels[1..])) {
                    var diff = b - a;
                    if (!increasing.HasValue) {
                        if (diff > 0) {
                            increasing = true;
                        } else if (diff < 0) {
                            increasing = false;
                        }
                    }
                    
                    if (increasing is false && diff > 0) {
                        return false;
                    }

                    if (increasing is true && diff < 0) {
                        return false;
                    }
                }
                
                return true;
            }
        }

        public Report WithSkipped(int index) {
            return new Report { Levels = Levels.RemoveAt(index) };
        }
        
        public bool ChangingSafely {
            get {
                foreach (var (a, b) in Levels.Zip(Levels[1..])) {
                    var absDiff = Math.Abs(b - a);
                    if (absDiff is < 1 or > 3) {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsSafeTolerant() {
            if (Safe) {
                return true;
            }
            
            for (var i = 0; i < Levels.Length; i++) {
                if (WithSkipped(i).Safe) {
                    return true;
                }
            }

            return false;
        }
    }
    
    
    
    private readonly record struct Problem {
        public ImmutableArray<Report> Reports { get; }
        
        public Problem(string[] lines) {
            Reports = [..lines.Select(l => new Report(l))];
        }
        
        public int Part1() {
            return Reports.Count(r => r.Safe);
        }
        
        public int Part2() {
            return Reports.Count(r => r.IsSafeTolerant());
        }
    }
    
    private Problem problem;
    
    public Day02() {
        Bag["test"] = """
                      7 6 4 2 1
                      1 2 7 8 9
                      9 7 6 2 1
                      1 3 2 4 5
                      8 6 4 4 1
                      1 3 6 7 9
                      """;
    }

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