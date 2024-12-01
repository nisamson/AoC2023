#region license

// AoC2023 - AoC2023 - Day06.cs
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
using AoC.Support;

namespace AoC2023._2023;

public class Day06 : Adventer {
    private ImmutableArray<Race> races;

    public Day06() {
        Bag["test"] = """
                      Time:      7  15   30
                      Distance:  9  40  200
                      """;
    }

    protected override void InternalOnLoad() {
        var times = Input.Lines[0][5..]
            .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse);
        var distances = Input.Lines[1][9..]
            .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse);
        races = times.Zip(distances).Select((tuple, i) => new Race { Distance = tuple.Second, Time = tuple.First })
            .ToImmutableArray();
    }

    protected override object InternalPart1() {
        return races.Select(r => r.GetRecordBreakingDistances().Count()).Product();
    }

    protected override object InternalPart2() {
        var race = new Race {
            Distance = long.Parse(races.Select(r => r.Distance).Aggregate("", (s, i) => $"{s}{i}")),
            Time = long.Parse(races.Select(r => r.Time).Aggregate("", (s, i) => $"{s}{i}"))
        };
        return race.GetRecordBreakingDistances().Count();
    }

    private class Race {
        public long Time { get; init; }
        public long Distance { get; init; }

        public IEnumerable<long> GetDistances() {
            for (var i = 0; i < Time; i++) yield return GetDistanceForChargeTime(i);
        }

        public IEnumerable<long> GetRecordBreakingDistancesPar() {
            return IterUtils.Range(0, Time).AsParallel().Select(GetDistanceForChargeTime).Where(d => d > Distance);
        }

        public IEnumerable<long> GetRecordBreakingDistances() {
            return GetDistances().Where(dist => dist > Distance);
        }

        public long GetDistanceForChargeTime(long chargeTime) {
            if (chargeTime > Time) return 0;

            return chargeTime * (Time - chargeTime);
        }
    }
}