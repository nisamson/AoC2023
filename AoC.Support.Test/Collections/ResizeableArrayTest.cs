#region license

// AoC2023 - AoC.Support.Test - ResizeableArrayTest.cs
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

using AoC.Support.Collections;

namespace AoC.Support.Test.Collections;

[TestFixture]
[TestOf(typeof(ResizeableArray<>))]
[Timeout(10000)]
public class ResizeableArrayTest {
    [Test]
    public void Creation() {
        var array = new ResizeableArray<int>();
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Is.Empty);
        Assert.That(array.Capacity, Is.Not.LessThan(0));
    }

    [Test]
    public void CreationWithInitialSize() {
        var array = new ResizeableArray<int>(10);
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Is.Empty);
        Assert.That(array.Capacity, Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    public void CreationWithEnumerable(
        [Random(10, 100, 5)] int numElements
    ) {
        var normalArray = Enumerable.Range(0, numElements).ToList();
        var array = new ResizeableArray<int>(normalArray.AsEnumerable());
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Is.Not.Empty);
        Assert.Multiple(
            () => {
                Assert.That(array.Capacity, Is.GreaterThanOrEqualTo(10));
                Assert.That(array.Sum(), Is.EqualTo(normalArray.Sum()));
            }
        );
        Assert.That(array, Is.EquivalentTo(normalArray));
    }

    [Test]
    public void CreationWithReadOnlyCollection(
        [Random(10, 100, 5)] int numElements
    ) {
        var normalArray = Enumerable.Range(0, numElements).ToList();
        var array = new ResizeableArray<int>(normalArray);
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Is.Not.Empty);
        Assert.Multiple(
            () => {
                Assert.That(array.Capacity, Is.GreaterThanOrEqualTo(10));
                Assert.That(array.Sum(), Is.EqualTo(normalArray.Sum()));
            }
        );
        Assert.That(array, Is.EquivalentTo(normalArray));
    }

    [Test]
    public void Clear() {
        var array = new ResizeableArray<int>(Enumerable.Range(0, 10));
        Assert.That(array, Is.Not.Empty);
        array.Clear();
        Assert.That(array, Is.Empty);
    }

    [Test]
    public void IndexOf()
    {
        var array = new ResizeableArray<int>(Enumerable.Range(0, 10));
        Assert.That(array.IndexOf(5), Is.EqualTo(5));
        array.RemoveAt(5);
        Assert.Multiple(() =>
        {
            Assert.That(array.IndexOf(5), Is.EqualTo(-1));
            Assert.That(array[^1], Is.EqualTo(9));
        });
        Assert.That(array.IndexOf(2), Is.EqualTo(2));
    }
    
    [Test]
    public void Insert() {
        var array = new ResizeableArray<int>(Enumerable.Range(0, 10));
        array.Insert(5, 5);
        Assert.Multiple(
            () => {
                Assert.That(array[5], Is.EqualTo(5));
                Assert.That(array[6], Is.EqualTo(5));
                Assert.That(array[7], Is.EqualTo(6));
            }
        );
    }
    
}
