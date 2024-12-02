namespace AoC2023._2024;

public class Day01 : Adventer {
    private Problem problem = null!;

    private const string Test = """
                                3   4
                                4   3
                                2   5
                                1   3
                                3   9
                                3   3
                                """;

    private const int TestResult = 11;

    private class Problem {
        private List<int> leftList;
        private List<int> rightList;

        private Problem() {
            leftList = new();
            rightList = new();
        }

        public static Problem Create(string[] lines) {
            var problem = new Problem();
            foreach (var line in lines) {
                var parts = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                problem.leftList.Add(int.Parse(parts[0]));
                problem.rightList.Add(int.Parse(parts[1]));
            }

            return problem;
        }

        public Problem Clone() {
            var clone = new Problem();
            clone.leftList.AddRange(leftList);
            clone.rightList.AddRange(rightList);
            return clone;
        }

        public int Part1() {
            leftList.Sort();
            rightList.Sort();
            return leftList.Zip(rightList)
                .Select(ab => Math.Abs(ab.First - ab.Second))
                .Sum();
        }

        public int Part2() {
            var appearanceCounts = rightList.GroupBy(i => i)
                .ToDictionary(grp => grp.Key, grp => grp.Count());
            return leftList
                .Select(x => appearanceCounts.GetValueOrDefault(x) * x)
                .Sum();
        }
    }

    public Day01() {
        Bag["test"] = Test;
    }

    protected override void InternalOnLoad() {
        problem = Problem.Create(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Clone().Part1();
    }

    protected override object InternalPart2() {
        return problem.Part2();
    }
}