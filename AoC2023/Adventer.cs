#region license

// AoC2023 - AoC2023 - Adventer.cs
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

using AdventOfCodeSupport;

namespace AoC2023;

public abstract class Adventer : AdventBase, IAdvent {
    public object DoPart1() {
        return InternalPart1();
    }

    public object DoPart2() {
        return InternalPart2();
    }

    public void DoLoad() {
        InternalOnLoad();
    }
}