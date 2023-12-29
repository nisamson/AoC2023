#region license

// AoC2023 - AoC.Support.Test - MathAcceleratorTest.cs
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

using AoC.Support;

namespace AoC.Support.Test;

[TestFixture]
[TestOf(typeof(MathAccelerator))]
public class MathAcceleratorTest {
    [Test]
    public void OptimalSize(
        [Random(5, 10, 5)] int maxThreadsPow,
        [Random(10, 16, 5)] int maxItemsPow
    ) {
        var maxThreads = 1 << maxThreadsPow;
        var maxItems = 1 << maxItemsPow;
        var calc = new MathAccelerator.GroupSizeCalculator(maxItems, maxThreads);
        var idx = calc.OptimalGroupSize();
        Console.WriteLine($"maxThreads: {maxThreads}, maxItems: {maxItems}, idx: {idx}");
        Assert.Multiple(() => { Assert.That(idx.Y + idx.X, Is.LessThanOrEqualTo(maxItems)); });
    }
}
