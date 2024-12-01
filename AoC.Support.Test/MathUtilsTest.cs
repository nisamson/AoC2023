#region license

// AoC2023 - AoC.Support.Test - MathUtilsTest.cs
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

namespace AoC.Support.Test;

[TestFixture]
[TestOf(typeof(MathUtils))]
public class MathUtilsTest {
    [Test]
    public void GeometricMeanZero() {
        Assert.Multiple(() => {
            Assert.That(() => MathUtils.GeometricMean<double>(),
                Throws.InstanceOf(typeof(ArgumentOutOfRangeException)));

            Assert.That(() => {
                    ReadOnlySpan<int> span = stackalloc int[0];
                    MathUtils.GeometricMean(span);
                },
                Throws.InstanceOf(typeof(ArgumentOutOfRangeException)));

            Assert.That(() => { Enumerable.Empty<int>().GeometricMean(); },
                Throws.InstanceOf(typeof(ArgumentOutOfRangeException)));
        });
    }
}