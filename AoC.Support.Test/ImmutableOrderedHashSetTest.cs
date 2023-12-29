#region license
// AoC2023 - AoC.Support.Test - ImmutableOrderedHashSetTest.cs
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
[TestOf(typeof(ImmutableOrderedHashSet<>))]
public class ImmutableOrderedHashSetTest {

    [Test]
    [Timeout(10000)]
    public void TestCreation(
        [Random(2, 100, 20)] int count
        ) {

        var values = Enumerable.Range(0, count).Select(_ => TestContext.CurrentContext.Random.Next()).ToHashSet();
        var set = ImmutableOrderedHashSet<int>.Empty.Union(values);
        Assert.That(set, Has.Count.EqualTo(count));
        Assert.That(set, Is.EquivalentTo(values));
    }

    [Test]
    [Timeout(10000)]
    public void TestInsertionOrder(
        [Random(2, 100, 20)] int count
    ) {

        var values = Enumerable.Range(0, count).ToList();
        var set = ImmutableOrderedHashSet<int>.Empty.Union(values);
        Assert.That(set, Is.EqualTo(values));
        Assert.That(set, Is.Ordered);

        set = set.Remove(count - 1);
        Assert.That(set, Is.Ordered);
    }
}
