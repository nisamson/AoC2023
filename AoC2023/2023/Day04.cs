#region license

// AoC2023 - AoC2023 - Day04.cs
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

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using AdventOfCodeSupport;
using Farkle;
using Farkle.Builder;
using Regex = System.Text.RegularExpressions.Regex;

namespace AoC2023._2023;

internal record ScratchGame {
    private int? winCount;

    public ScratchGame(int id, IEnumerable<int> cards, IEnumerable<int> winningCards) {
        Id = id;
        Cards = new HashSet<int>(cards);
        WinningCards = new HashSet<int>(winningCards);
    }

    public int Id { get; init; }
    public IReadOnlySet<int> Cards { get; init; } = ImmutableHashSet<int>.Empty;
    public IReadOnlySet<int> WinningCards { get; init; } = ImmutableHashSet<int>.Empty;

    public int WinCount {
        get {
            if (winCount.HasValue) return winCount.Value;

            winCount = PossessedWinningCards().Count();
            return winCount.Value;
        }
    }

    public IEnumerable<int> PossessedWinningCards() {
        return Cards.Where(WinningCards.Contains);
    }

    public int Score() {
        var winners = WinCount;
        if (winners == 0) return 0;

        var score = 1 << (winners - 1);
        return score;
    }
}

internal static partial class ScratchLang {
    public static readonly PrecompilableDesigntimeFarkle<ScratchGame> DesignTime;
    public static readonly RuntimeFarkle<ScratchGame> Runtime;
    public static readonly PrecompilableDesigntimeFarkle<List<ScratchGame>> DesignTimeArray;
    public static readonly RuntimeFarkle<List<ScratchGame>> RuntimeArray;

    static ScratchLang() {
        var number = Terminals.Int32("number");
        var card = Nonterminal.Create(
            "scratchCard",
            Terminal.Literal("Card").Appended()
                .Extend(number)
                .Append(Terminal.Literal(":"))
                .Extend(number.Many<int, List<int>>())
                .Append(Terminal.Literal("|"))
                .Extend(number.Many<int, List<int>>())
                .Finish((i, winners, present) => new ScratchGame(i, winners, present))
        );
        DesignTime = card.MarkForPrecompile();
        Runtime = DesignTime.Build();
        var multiple = card.Many<ScratchGame, List<ScratchGame>>();
        DesignTimeArray = multiple.MarkForPrecompile();
        RuntimeArray = DesignTimeArray.Build();
    }

    [GeneratedRegex(@"Card +(?<id>\d+): +(?<winners>(\d+ +)+)\| +(?<cards>(\d+ +)+)")]
    private static partial Regex GameRegex();

    public static ScratchGame ParseOne(string line) {
        var matches = GameRegex().Match(line);
        if (!matches.Success) throw new ArgumentException($"Invalid game, {line}");

        var id = int.Parse(matches.Groups["id"].Value);
        var winners = matches.Groups["winners"]
            .Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse);
        var cards = matches.Groups["cards"]
            .Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse);
        return new ScratchGame(id, cards, winners);
    }
}

public class Day04 : AdventBase, IAdvent {
    private readonly List<ScratchGame> games = new();

    public Day04() {
        Bag["example"] = """
                         Card 1: 41 48 83 86 17 | 83 86  6 31 17  9 48 53
                         Card 2: 13 32 20 16 61 | 61 30 68 82 17 32 24 19
                         Card 3:  1 21 53 59 44 | 69 82 63 72 16 21 14  1
                         Card 4: 41 92 73 84 69 | 59 84 76 51 58  5 54 83
                         Card 5: 87 83 26 28 32 | 88 30 70 12 93 22 82 36
                         Card 6: 31 18 13 56 72 | 74 77 10 23 35 67 36 11
                         """;
        Bag["exampleResult"] = "13";
    }

    public object DoPart1() {
        return InternalPart1();
    }

    public object DoPart2() {
        return InternalPart2();
    }

    public void DoLoad() { }

    protected override void InternalOnLoad() {
        games.Clear();
        var result = ScratchLang.RuntimeArray.Parse(Input.Text);
        if (result.IsError) throw new ArgumentException($"Invalid games, {result.ErrorValue}");

        games.AddRange(result.ResultValue);
    }

    protected override object InternalPart1() {
        var localGames = games.ToArray();
        return localGames.Sum(g => g.Score());
    }


    protected override object InternalPart2() {
        var localGames = games.ToArray();
        return new GameScorer(localGames).ScoreGames();
    }

    private class GameScorer {
        private readonly ScratchGame[] games;
        private readonly int[] gameScores;
        private Dictionary<(int, int), int> scoreCache = new();

        public GameScorer(ScratchGame[] games) {
            this.games = games;
            gameScores = new int[games.Length];
            Array.Fill(gameScores, -1);
        }

        public int ScoreGame(int index) {
            if (gameScores[index] >= 0) return gameScores[index];

            gameScores[index] = 1 + ScoreGames(index + 1, games[index].WinCount);
            return gameScores[index];
        }

        public int ScoreGames(int startIndex, int count) {
            var total = 0;
            for (var i = startIndex + count - 1; i >= startIndex; i--) total += ScoreGame(i);

            return total;
        }

        public int ScoreGames() {
            return ScoreGames(0, games.Length);
        }
    }
}