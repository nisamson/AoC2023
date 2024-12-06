using AoC.Support;

namespace AoC2023._2024;

public class Day06 : Adventer {
    private readonly record struct Square {
        public bool Visited { get; init; }
        public bool IsObstacle { get; init; }

        public override string ToString() {
            if (!Visited) {
                return IsObstacle ? "#" : ".";
            }

            return "X";
        }

        public static Square FromChar(char c) {
            return c switch {
                '#' => new Square { IsObstacle = true },
                '^' => new Square { Visited = true },
                _ => new Square()
            };
        }
    }

    private class ProblemState {
        private Grid<Square> Grid { get; }
        private Vertex<int> GuardPosition { get; set; }
        private Direction GuardDirection { get; set; } = Direction.Up;
        
        private IReadOnlySet<Vertex<int>> Obstacles { get; }


        public ProblemState(string[] input) {
            var width = input[0].Length;
            var height = input.Length;
            Grid = new Grid<Square>(width, height,
                input.SelectMany(l => l.AsEnumerable())
                    .Select(Square.FromChar));
            GuardPosition = Grid.EnumerateIndexed().First(e => e.Item.Visited).Coords;
            Obstacles = Grid.EnumerateIndexed().Where(e => e.Item.IsObstacle).Select(e => e.Coords).ToHashSet();
        }

        private ProblemState(ProblemState original) {
            Grid = original.Grid.Clone();
            GuardPosition = original.GuardPosition;
            GuardDirection = original.GuardDirection;
            Obstacles = original.Obstacles;
        }

        public ProblemState Clone() {
            return new ProblemState(this);
        }

        public bool MoveGuard() {
            var nextPosition = GuardPosition.GetNeighbor(GuardDirection);
            Grid[GuardPosition] = Grid[GuardPosition] with { Visited = true };
            if (!Grid.IsInBounds(nextPosition)) {
                return false;
            }
            if (Grid[nextPosition].IsObstacle) {
                GuardDirection = GuardDirection.TurnRight();
            } else {
                GuardPosition = nextPosition;
            }

            return true;
        }

        public bool HasCycle() {
            var turns = new HashSet<(Vertex<int>, Direction)>();
            var previousDirection = GuardDirection;
            while (MoveGuard()) {
                if (previousDirection != GuardDirection) {
                    if (!turns.Add((GuardPosition, GuardDirection))) {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public bool HasCycleWithObstacleAt(Vertex<int> obstacle) {
            var copy = Clone();
            copy.Grid[obstacle] = copy.Grid[obstacle] with { IsObstacle = true };
            if (DebugPrint) {
                Console.WriteLine("Checking obstacle at " + obstacle);
            }
            var hasCycle = copy.HasCycle();
            if (DebugPrint) {
                Console.WriteLine("Has cycle: " + hasCycle);
                if (hasCycle) {
                    copy.PrintGrid();
                }
            }
            
            return hasCycle;
        }

        public int FindCyclePositions() {
            var candidateCoords = Grid.EnumerateIndexed()
                .Where(t => t.Item is { IsObstacle: false, Visited: false });
            return candidateCoords.Count(c => HasCycleWithObstacleAt(c.Coords));
        }
        
        private void PrintGrid() {
            Console.Clear();
            for (var y = 0; y < Grid.Height; y++) {
                for (var x = 0; x < Grid.Width; x++) {
                    var coords = new Vertex<int>(x, y);
                    if (coords == GuardPosition) {
                        Console.Write(GuardDirection.ToChar());
                    } else {
                        Console.Write(Grid[x, y]);
                    }
                }
                Console.WriteLine();
            }
        }

        public int Part1(bool debug = false) {
            var previousDirection = GuardDirection;
            var debugCounter = 0;
            while (MoveGuard()) {
                if (debug && GuardDirection != previousDirection) {
                    // Console.Clear();
                    // Console.WriteLine(Grid);
                    Console.WriteLine(++debugCounter);
                    Console.WriteLine(GuardPosition);
                    PrintGrid();
                    previousDirection = GuardDirection;
                }
            }
            
            if (debug) {
                // Console.Clear();
                // Console.WriteLine(Grid);
                Console.WriteLine(++debugCounter);
                Console.WriteLine(GuardPosition);
                PrintGrid();
            }

            return Grid.RowMajorItems().Count(s => s.Visited);
        }

        public int Part2(bool debug = false) {
            return FindCyclePositions();
        }
    }

    public Day06() {
        Bag["test"] = """
                      ....#.....
                      .........#
                      ..........
                      ..#.......
                      .......#..
                      ..........
                      .#..^.....
                      ........#.
                      #.........
                      ......#...
                      """;
    }

    private const bool DebugPrint = false;
    private ProblemState problem;

    protected override void InternalOnLoad() {
        problem = new ProblemState(Input.Lines);
    }

    protected override object InternalPart1() {
        var localProblem = problem.Clone();
        return localProblem.Part1(DebugPrint);
    }

    protected override object InternalPart2() {
        var localProblem = problem.Clone();
        return localProblem.Part2(DebugPrint);
    }
}