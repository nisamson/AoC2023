using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using AoC.Support;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using OpenCvSharp;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace AoC2023._2024;

public class Day14 : Adventer {
    private static readonly Vector128<long> FieldSize = Vector128.Create(101, 103);
    private static readonly Vector128<long> QuadrantDivision = (FieldSize - Vector128<long>.One) / 2;
    private static readonly Vector128<long> Lt = new System.Numerics.Vector<long>(-1).AsVector128();
    private static readonly Vector128<long> Gt = new System.Numerics.Vector<long>(1).AsVector128();

    private static Vector128<long>? PositionToQuadrant(Vector128<long> position) {
        var gt = Sse42.CompareGreaterThan(position, QuadrantDivision);
        var lt = Sse42.CompareGreaterThan(QuadrantDivision, position);
        var eq = Sse41.CompareEqual(position, QuadrantDivision);
        var mask = Sse2.MoveMask(eq.AsByte());
        if (mask != 0) {
            return null;
        }

        var quad = Sse2.And(gt, Gt) + Sse2.And(lt, Lt);
        return quad;
    }

    private readonly record struct Robot {
        public required Vector128<long> Position { get; init; }
        public required Vector128<long> Velocity { get; init; }

        public Vector128<long> Move(long seconds) {
            var n = Position + Velocity * Vector128.Create(seconds);
            var rem = n - n / FieldSize * FieldSize;
            // add if negative
            var mask = Sse42.CompareGreaterThan(Vector128<long>.Zero, rem);
            var add = Sse2.And(mask, FieldSize);
            var val = rem + add;
            return val;
        }
    }

    private static Vector128<long> QuadrantOffset(Vector128<long> quad) {
        var add = Sse42.CompareGreaterThan(Vector128<long>.Zero, quad);
        return Sse2.And(QuadrantDivision + Vector128<long>.One, add);
    }

    private static void ConvertToQuadrantCoords(Span<Vector128<long>> position, Vector128<long> quad) {
        var offset = QuadrantOffset(quad);
        for (var i = 0; i < position.Length; i++) {
            position[i] -= offset;
        }
    }

    private static readonly Parser<char, Robot> RobotParser;

    static Day14() {
        var number = Num;
        var xy = Map(
            (x, _, y) => Vector128.Create(x, y),
            number,
            Char(','),
            number
        );
        var position = String("p=").Then(xy);
        var velocity = String("v=").Then(xy);
        RobotParser = Map(
            (p, _, v) => new Robot { Position = p, Velocity = v },
            position,
            Char(' '),
            velocity
        );
    }

    public Day14() {
        Bag["test"] = """
                      p=0,4 v=3,-3
                      p=6,3 v=-1,-3
                      p=10,3 v=-1,2
                      p=2,0 v=2,-1
                      p=0,0 v=1,3
                      p=3,0 v=-2,-2
                      p=7,6 v=-1,-3
                      p=3,0 v=-1,-2
                      p=9,3 v=2,3
                      p=7,3 v=-1,2
                      p=2,4 v=2,-3
                      p=9,5 v=-3,-3
                      """;
    }

    private class Problem {
        private readonly ImmutableArray<Robot> robots;

        public Problem(string[] lines) {
            var builder = ImmutableArray.CreateBuilder<Robot>(lines.Length);

            foreach (var line in lines) {
                builder.Add(RobotParser.ParseOrThrow(line));
            }

            robots = builder.ToImmutable();
        }

        public int Part1() {
            return robots.Select(x => PositionToQuadrant(x.Move(100)))
                .Where(x => x is not null)
                .GroupBy(x => x!)
                .Product(grp => grp.Count());
        }

        public int Part2() {
            using var rt = new ResourcesTracker();
            var seen = new HashSet<UInt128>();
            var mat = CreateMatrix(rt);
            var best = CreateMatrix(rt);
            var bestSize = 0;
            var bestIdx = -1;
            Span<Vector128<long>> positions = stackalloc Vector128<long>[robots.Length];
            for (var i = 0; i < int.MaxValue; i++) {
                for (var idx = 0; idx < robots.Length; idx++) {
                    positions[idx] = robots[idx].Move(i);
                }

                DrawRobots(mat, positions);
                var hash = HashMat(mat);
                if (!seen.Add(hash)) {
                    break;
                }

                var size = SizeOfLargestContiguousArea(mat);
                if (size <= bestSize) {
                    continue;
                }

                bestSize = size;
                mat.CopyTo(best);
                bestIdx = i;
            }

            return bestIdx;
        }
    }

    private static void DrawRobots(Mat mat, ReadOnlySpan<Vector128<long>> positions) {
        mat.SetTo(new Scalar(0));
        foreach (var pos in positions) {
            mat.Set((int)pos.GetElement(0), (int)pos.GetElement(1), 255);
        }
    }

    private static int SizeOfLargestContiguousArea(Mat mat) {
        var cc = Cv2.ConnectedComponentsEx(mat, PixelConnectivity.Connectivity4);
        var largestBlob = cc.GetLargestBlob();
        return largestBlob.Area;
    }

    private static Mat CreateMatrix(ResourcesTracker rt) {
        var mat = rt.NewMat(new((int)FieldSize[0], (int)FieldSize[1]), MatType.CV_8U, new Scalar(0));
        return mat;
    }

    private static UInt128 HashMat(Mat mat) {
        var bytes = mat.AsSpan<byte>();
        var hash = XxHash128.HashToUInt128(bytes);
        return hash;
    }

    private Problem problem;

    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        return problem.Part1();
    }

    protected override object InternalPart2() {
        using var tracker = new ResourcesTracker();
        return problem.Part2();
    }
}