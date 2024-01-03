#region license
// AoC2023 - AoC.Support.Test - VectorExtensionsTest.cs
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

using AoC.Support.Numerics;
using CommunityToolkit.HighPerformance;

namespace AoC.Support.Test.Numerics;

[TestFixture]
[TestOf(typeof(VectorExtensions))]
public class VectorExtensionsTest {

    [Test]
    public void PopCount(
        [Random(1, 256 + 1, 10)] int count
        ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = numbers.AsSpan().PopCount();
        Assert.That(actual, Is.EqualTo(expected));

    }
    
    [Test]
    public void PopCountAvx2(
        [Random(1, 256 + 1, 10)] int count
        ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = ((ReadOnlySpan<byte>)numbers.AsSpan().AsBytes()).PopCountAvx2();
        Assert.That(actual, Is.EqualTo(expected));

    }
    
    [Test]
    public void PopCountSse3s(
        [Random(1, 256 + 1, 10)] int count
    ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = ((ReadOnlySpan<byte>)numbers.AsSpan().AsBytes()).PopCountSse3s();
        Assert.That(actual, Is.EqualTo(expected));

    }
    
    [Test]
    public void PopCountFallback(
        [Random(1, 256 + 1, 10)] int count
    ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = ((ReadOnlySpan<byte>)numbers.AsSpan().AsBytes()).BoringPopCount();
        Assert.That(actual, Is.EqualTo(expected));

    }
    
    [Test]
    public void PopCountAvx2Alternate(
        [Random(1, 256 + 1, 10)] int count
    ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = ((ReadOnlySpan<byte>)numbers.AsSpan().AsBytes()).PopCountAvx2Alternate();
        Assert.That(actual, Is.EqualTo(expected));

    }
    
    [Test]
    public void PopCountAvx2Unroll(
        [Random(1, 256 + 1, 10)] int count
    ) {

        var numbers = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.NextULong()).ToArray();
        var expected = numbers.Select(ulong.PopCount).Aggregate((a, b) => a + b);
        var actual = ((ReadOnlySpan<byte>)numbers.AsSpan().AsBytes()).PopCountAvx2ManualUnroll();
        Assert.That(actual, Is.EqualTo(expected));

    }
}
