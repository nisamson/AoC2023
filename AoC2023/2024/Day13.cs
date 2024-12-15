
using System.Buffers;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Vector2 = System.Numerics.Vector2;

namespace AoC2023._2024;

public class Day13 : Adventer {

    private class Problem {
        
    }

    private record Machine {
        private readonly Matrix<float> buttons;
        private readonly Vector<float> prize;
        private static readonly Vector2 ButtonCost = new Vector2(3, 1);

        public Machine(Vector2 buttonA, Vector2 buttonB, Vector2 prize) {
            var storage = new[] {
                buttonA.X, buttonB.X, buttonA.Y, buttonB.Y
            };
            buttons = Matrix.Build.Dense(2, 2, storage);
            this.prize = Vector.Build.Dense([prize.X, prize.Y]);
        }

        private Vector2 CalculatePresses() {
            var result = buttons.Solve(prize);
            return new Vector2(result[0], result[1]);
        }

        public Vector2 TokenCost() {
            var presses = CalculatePresses();
            return presses * ButtonCost;
        }


    }
    
    protected override object InternalPart1() {
        throw new NotImplementedException();
    }
    protected override object InternalPart2() {
        throw new NotImplementedException();
    }
}