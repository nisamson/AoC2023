#region license

// AoC2023 - AoC.Support.Test - KnownPopulationSetTest.cs
// Copyright (C) 2024 Nicholas
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

using AoC.Support.Collections;

namespace AoC.Support.Test.Collections;

[TestFixture]
[TestOf(typeof(KnownPopulationSet<>))]
public class KnownPopulationSetTest {
    [Test]
    public void TestProperSubset() {
        var population = new[] { "a", "b", "c", "d", "e" };
        var gen = KnownPopulationSet.CreateGenerator(population);
        var set = gen.CreateSet();
        Assert.That(set, Is.SubsetOf(population)); // empty set is subset of all sets
        set.UnionWith(population);
        Assert.That(set.IsProperSubsetOf(population), Is.False); // full set is not proper subset of itself
        Assert.That(set.IsProperSubsetOf(population.Take(3)), Is.False);
        set.Remove("a");
        Assert.That(set.IsProperSubsetOf(population), Is.True);

        set.Add("a");
        var set2 = gen.CreateSet();
        set2.UnionWith(population.Take(3));
        Assert.That(set2.IsProperSubsetOf(population), Is.True);
        Assert.That(set2.IsProperSubsetOf(set), Is.True);
        Assert.That(set.IsProperSubsetOf(set2), Is.False);
    }

    [Test]
    public void TestCount() {
        var population = new[] { "a", "b", "c", "d", "e" };
        var gen = KnownPopulationSet.CreateGenerator(population);
        var set2 = gen.CreateSet(population);
        var set = gen.CreateSet();
        Assert.That(set.Count, Is.Zero);
        set.UnionWith(population);
        Assert.That(set.Count, Is.EqualTo(population.Length));
        set.Remove("a");
        Assert.That(set.Count, Is.EqualTo(population.Length - 1));
        set.Add("a");
        Assert.That(set.Count, Is.EqualTo(population.Length));
        set.ExceptWith(population);
        Assert.That(set.Count, Is.Zero);
        set.UnionWith(population);
        set.ExceptWith(population);
        Assert.That(set.Count, Is.Zero);
        Assert.That(set2.Count, Is.EqualTo(population.Length));

        Assert.That(set2.SetEquals(population));
        Assert.That(set.IsProperSubsetOf(set2));
        set.Add("a");
        Assert.That(set, Contains.Item("a"));
        Assert.That(set, Has.Count.EqualTo(1));
    }
}