namespace AoC2023._2024;

public class Day01 : Adventer {

    private Problem problem = null!;
    
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
                var parts = line.Split(" ");
                problem.leftList.Add(int.Parse(parts[0]));
                problem.rightList.Add(int.Parse(parts[1]));
            }
            
            return problem;
        }
    }

    protected override void InternalOnLoad() {
        problem = Problem.Create(Input.Lines);
    }

    protected override object InternalPart1() {
        throw new NotImplementedException();
    }

    protected override object InternalPart2() {
        throw new NotImplementedException();
    }
}