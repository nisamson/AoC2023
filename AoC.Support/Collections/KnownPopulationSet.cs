#region license

// AoC2023 - AoC.Support - KnownPopulationSet.cs
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

using System.Collections;
using System.Collections.Frozen;

namespace AoC.Support.Collections;

public class KnownPopulationSet<T> : ISet<T> where T : notnull {
    private readonly BitArray data;
    private readonly Generator generator;

    private KnownPopulationSet(Generator generator, BitArray? data = null) {
        this.generator = generator;
        this.data = data ?? new BitArray(generator.Population.Count);
        UpdateCount();
    }

    private bool this[T item] {
        get => data[generator.PopulationIndices[item]];
        set => data[generator.PopulationIndices[item]] = value;
    }

    public IEnumerator<T> GetEnumerator() {
        return data.EnumerateSetBits()
            .Select(i => generator[i])
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    void ICollection<T>.Add(T item) {
        this[item] = true;
        UpdateCount();
    }

    public void ExceptWith(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) {
            ExceptWith(kps);
            return;
        }

        foreach (var o in other) this[o] = false;
        UpdateCount();
    }

    public void IntersectWith(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) {
            IntersectWith(kps);
            return;
        }

        var otherSet = generator.CreateSet(other);
        IntersectWith(otherSet);
    }

    public bool IsProperSubsetOf(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return IsProperSubsetOf(kps);

        var otherSet = generator.CreateSet(other);
        return IsProperSubsetOf(otherSet);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return kps.IsProperSubsetOf(this);

        var otherSet = generator.CreateSet(other);
        return otherSet.IsProperSubsetOf(this);
    }

    public bool IsSubsetOf(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return IsSubsetOf(kps);

        var otherSet = generator.CreateSet(other);
        return IsSubsetOf(otherSet);
    }

    public bool IsSupersetOf(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return kps.IsSubsetOf(this);

        var otherSet = generator.CreateSet(other);
        return otherSet.IsSubsetOf(this);
    }

    public bool Overlaps(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return Overlaps(kps);

        return other.Any(o => this[o]);
    }

    public bool SetEquals(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) return SetEquals(kps);

        var otherSet = generator.CreateSet(other);
        return SetEquals(otherSet);
    }

    public void SymmetricExceptWith(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) {
            SymmetricExceptWith(kps);
            return;
        }

        foreach (var o in other) this[o] = !this[o];
        UpdateCount();
    }

    public void UnionWith(IEnumerable<T> other) {
        if (other is KnownPopulationSet<T> kps && kps.generator == generator) {
            UnionWith(kps);
            return;
        }

        // Can't convert here because CreateSet uses UnionWith
        foreach (var o in other) this[o] = true;
        UpdateCount();
    }

    public bool Add(T item) {
        if (!this[item]) {
            this[item] = true;
            UpdateCount();
            return true;
        }

        return false;
    }

    public void Clear() {
        data.Clear();
        UpdateCount();
    }

    public bool Contains(T item) {
        return this[item];
    }

    public void CopyTo(T[] array, int arrayIndex) {
        for (var i = 0; i < data.Count; i++)
            if (data[i])
                array[arrayIndex++] = generator[i];
    }

    public bool Remove(T item) {
        if (!this[item]) return false;

        this[item] = false;
        UpdateCount();
        return true;
    }

    public int Count { get; private set; }
    public bool IsReadOnly => false;

    public void ExceptWith(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot except with a set from a different generator");

        data.AndNot(other.data);
        UpdateCount();
    }

    public void IntersectWith(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot intersect with a set from a different generator");

        data.And(other.data);
        UpdateCount();
    }

    public bool IsProperSubsetOf(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot check subset with a set from a different generator");

        // If this is a proper subset of other, then there must be at least one element in other that is not in this
        // and all elements in other must be in this.
        if (other.Count <= Count) return false;

        // other.Count > Count
        var tmp = other.data.Clone();
        tmp.Or(data);
        // ensure tmp had no bits set that were not already set
        return tmp.Equals(other.data);
    }

    public bool IsSubsetOf(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot check subset with a set from a different generator");

        // If this is a subset of other, then all elements in this must be in other.
        if (Count > other.Count) return false;

        // Count <= other.Count
        var tmp = other.data.Clone();
        tmp.And(data);
        // ensure tmp had no bits set that were not already set
        return tmp.Equals(data);
    }

    public bool Overlaps(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot check overlap with a set from a different generator");

        return !data.IntersectionIsEmpty(other.data);
    }

    public bool SetEquals(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot check set equality with a set from a different generator");

        return data.Equals(other.data);
    }

    public void SymmetricExceptWith(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot symmetric except with a set from a different generator");

        data.Xor(other.data);
        UpdateCount();
    }

    public void UnionWith(KnownPopulationSet<T> other) {
        if (other.generator != generator)
            throw new ArgumentException("Cannot union with a set from a different generator");

        data.Or(other.data);
        UpdateCount();
    }

    private void UpdateCount() {
        Count = data.CountSetBits();
    }

    public KnownPopulationSet<T> Clone() {
        return new KnownPopulationSet<T>(generator, data.Clone());
    }

    public class Generator {
        private readonly T[] population;
        private readonly FrozenDictionary<T, int> populationIndices;

        public Generator(IEnumerable<T> population, IEqualityComparer<T> comparer) {
            this.population = population.ToArray();
            populationIndices = this.population.Select((v, i) => (v, i))
                .ToFrozenDictionary(x => x.v, x => x.i, comparer);
            this.Comparer = comparer;
        }

        public IReadOnlyDictionary<T, int> PopulationIndices => populationIndices;
        public IReadOnlyList<T> Population => population;

        public IEqualityComparer<T> Comparer { get; }

        public T this[int index] => population[index];
        public int this[T item] => populationIndices[item];

        public KnownPopulationSet<T> CreateSet() {
            return new KnownPopulationSet<T>(this);
        }

        public KnownPopulationSet<T> CreateSet(IEnumerable<T> data) {
            var set = new KnownPopulationSet<T>(this);
            set.UnionWith(data);
            return set;
        }
    }
}

public static class KnownPopulationSet {
    public static KnownPopulationSet<T>.Generator CreateGenerator<T>(IEnumerable<T> population,
        IEqualityComparer<T>? comparer = null)
        where T : notnull {
        return new KnownPopulationSet<T>.Generator(population, comparer ?? EqualityComparer<T>.Default);
    }
}