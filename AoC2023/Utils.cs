#region license
// AoC2023 - AoC2023 - Utils.cs
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

using System.Numerics;

namespace AoC2023;

public static class IterUtils {
    public static TNumeric Product<TNumeric>(this IEnumerable<TNumeric> source) where TNumeric: INumber<TNumeric> {
        return source.Aggregate(TNumeric.One, (current, item) => current * item);
    }
    
    public static IEnumerable<long> Range(long start, long count) {
        var max = start + count - 1;
        switch (count) {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count));
            case 0:
                yield break;
        }

        for (var i = start; i <= max; i++) {
            yield return i;
        }
    }
}
