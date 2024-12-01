#region license

// AoC2023 - AoC2023 - Range.cs
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

using System.Collections;
using System.Numerics;

namespace AoC.Support;

public struct Range<TNumeric> : IEnumerable<TNumeric> where TNumeric : INumber<TNumeric> {
    public TNumeric Start { get; }
    public TNumeric End { get; }

    public bool InclusiveEnd { get; }

    public Range(TNumeric start, TNumeric end, bool inclusiveEnd = false) {
        Start = start;
        End = end;
        InclusiveEnd = inclusiveEnd;
        var cmp = start.CompareTo(end);
        switch (cmp) {
            case > 0 when inclusiveEnd:
                throw new ArgumentException("Start must be less than or equal to end", nameof(start));
            case >= 0 when !inclusiveEnd:
                throw new ArgumentException("Start must be less than end", nameof(start));
        }
    }

    public IEnumerator<TNumeric> GetEnumerator() {
        return GetEnumerator(TNumeric.One);
    }

    public IEnumerator<TNumeric> GetEnumerator(TNumeric step) {
        if (step.CompareTo(TNumeric.Zero) <= 0)
            throw new ArgumentException("Step must be greater than zero", nameof(step));
        var current = Start;
        var cmp = current.CompareTo(End);
        while (cmp < 0) {
            yield return current;
            current += step;
            cmp = current.CompareTo(End);
        }

        if (InclusiveEnd) yield return End;
    }

    public bool Contains(TNumeric value) {
        var cmp = value.CompareTo(Start);
        if (cmp < 0) return false;
        cmp = value.CompareTo(End);
        switch (cmp) {
            case > 0:
            case 0 when !InclusiveEnd:
                return false;
            default:
                return true;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}