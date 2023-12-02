#region license

// AoC2023 - AoC2023 - Day02.cs
// Copyright (C) 2023 Nicholas
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Formats.Asn1;
using AdventOfCodeSupport;
    using Farkle;
    using Farkle.Builder;

    namespace AoC2023._2023;

    internal enum CubeKind {
        Red,
        Green,
        Blue
    }

    internal static class CubeKindExtensions {
        public static CubeKind GetCubeKind(this string input) {
            return input.AsSpan().GetCubeKind();
        }

        public static CubeKind GetCubeKind(this ReadOnlySpan<char> input) {
            return input switch {
                "red"   => CubeKind.Red,
                "green" => CubeKind.Green,
                "blue"  => CubeKind.Blue,
                _       => throw new ArgumentException("Invalid cube kind", nameof(input))
            };
        }
    }

    internal class Game {
        public Game(int id, IList<Round> rounds) {
            Id = id;
            Rounds = rounds;
        }

        public int Id { get; }
        public IList<Round> Rounds { get; }

        public bool IsPossibleFor(IDictionary<CubeKind, int> cubes) {
            return Rounds.All(r => r.Cubes.All(c => cubes[c.Key] >= c.Value));
        }

        public IEnumerable<(CubeKind, int)> MinimumRequiredCubes() {
            return Rounds.SelectMany(r => r.Cubes)
                .GroupBy(kv => kv.Key)
                .Select(g => g.MaxBy(kv => kv.Value))
                .Select(kv => (kv.Key, kv.Value));
        }
    }

    internal class Grab {
        public Grab(CubeKind kind, int count) {
            Kind = kind;
            Count = count;
        }

        public CubeKind Kind { get; }
        public int Count { get; }
    }

    internal class GameLang {
        public static readonly PrecompilableDesigntimeFarkle<Game> Designtime;
        public static readonly RuntimeFarkle<Game> Runtime;

        static GameLang() {
            var cubeKind = Terminal.Create(
                "cubeKind",
                (_, data) => data.GetCubeKind(),
                Regex.Choice(
                    Regex.Literal("red"),
                    Regex.Literal("green"),
                    Regex.Literal("blue")
                )
            );

            var number = Terminals.Int32("number");

            var grab = Nonterminal.Create(
                "grab",
                number.Extended().Extend(cubeKind).Finish((i, kind) => new Grab(kind, i))
            );

            var round = Nonterminal.Create(
                "round",
                grab.SeparatedBy<Grab, List<Grab>>(Terminal.Literal(","), true).Finish(list => new Round(list))
            );

            var game = Nonterminal.Create(
                "game",
                Terminal.Literal("Game").Appended().Extend(number).Append(":")
                    .Extend(round.SeparatedBy<Round, List<Round>>(Terminal.Literal(";"), true))
                    .Finish((id, list) => new Game(id, list))
            );

            Designtime = game
                .AutoWhitespace(true)
                .MarkForPrecompile();
            Runtime = Designtime.Build();
        }
    }

    internal class Round {
        public IDictionary<CubeKind, int> Cubes { get; } = Enum.GetValues<CubeKind>()
            .Select(c => (c, 0))
            .ToDictionary(x => x.c, x => x.Item2);

        public Round(IEnumerable<Grab> grabs) {
            foreach (var grab in grabs) {
                Cubes[grab.Kind] += grab.Count;
            }
        }
    }

    public class Day02 : AdventBase {
        private RuntimeFarkle<Game> parser;
        private IDictionary<CubeKind, int> part1Max = new Dictionary<CubeKind, int>() {
            { CubeKind.Red, 12 },
            { CubeKind.Green, 13 },
            { CubeKind.Blue, 14 },
        };

        protected override void InternalOnLoad() {
            base.InternalOnLoad();
            parser = GameLang.Runtime;
        }

        protected override object InternalPart1() {
            var total = 0;
            foreach (var line in Input.Lines) {
                var result = parser.Parse(line);
                var game = result.ResultValue;
                if (game.IsPossibleFor(part1Max)) {
                    total += game.Id;
                }
            }

            return total;
        }

        protected override object InternalPart2() {
            var total = 0;
            foreach (var line in Input.Lines) {
                var result = parser.Parse(line);
                var game = result.ResultValue;
                total += game.MinimumRequiredCubes().Select(kv => kv.Item2).Aggregate(1, (x, y) => x * y);
            }

            return total;
        }
    }
