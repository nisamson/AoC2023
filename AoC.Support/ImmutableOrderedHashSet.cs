#region license

// AoC2023 - AoC.Support - ImmutableLinkedHashSet.cs
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
using System.Collections.Immutable;

namespace AoC.Support;

public sealed class ImmutableOrderedHashSet<TItem> : IImmutableSet<TItem> {
    public static readonly ImmutableOrderedHashSet<TItem> Empty = new();
    private readonly ImmutableList<TItem> list;
    private readonly ImmutableHashSet<TItem> set;

    private ImmutableOrderedHashSet(IEqualityComparer<TItem>? equalityComparer = null) {
        list = ImmutableList<TItem>.Empty;
        set = ImmutableHashSet<TItem>.Empty.WithComparer(equalityComparer);
    }

    private ImmutableOrderedHashSet(ImmutableList<TItem> list, ImmutableHashSet<TItem> set) : this() {
        this.list = list;
        this.set = set;
    }

    public IEqualityComparer<TItem> EqualityComparer => set.KeyComparer;


    public IEnumerator<TItem> GetEnumerator() {
        return list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public int Count => list.Count;

    IImmutableSet<TItem> IImmutableSet<TItem>.Add(TItem value) {
        return Add(value);
    }

    public IImmutableSet<TItem> Clear() {
        return new ImmutableOrderedHashSet<TItem>(EqualityComparer);
    }

    public bool Contains(TItem value) {
        return set.Contains(value);
    }

    IImmutableSet<TItem> IImmutableSet<TItem>.Except(IEnumerable<TItem> other) {
        IReadOnlyCollection<TItem> otherList = other as IImmutableSet<TItem> ?? other.ToImmutableHashSet();
        var newSet = set.Except(otherList);
        if (newSet.Count == set.Count) return this;

        var newList = list.RemoveAll(otherList.Contains);
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    public IImmutableSet<TItem> Intersect(IEnumerable<TItem> other) {
        var newSet = set.Intersect(other);
        if (newSet.Count == set.Count) return this;

        var newList = list.RemoveAll(item => !newSet.Contains(item));
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    public bool IsProperSubsetOf(IEnumerable<TItem> other) {
        return set.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<TItem> other) {
        return set.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<TItem> other) {
        return set.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<TItem> other) {
        return set.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<TItem> other) {
        return set.Overlaps(other);
    }

    IImmutableSet<TItem> IImmutableSet<TItem>.Remove(TItem value) {
        return InternalRemove(value);
    }

    public bool SetEquals(IEnumerable<TItem> other) {
        return set.SetEquals(other);
    }

    public IImmutableSet<TItem> SymmetricExcept(IEnumerable<TItem> other) {
        var newSet = set.SymmetricExcept(other);
        if (newSet.Count == set.Count) return this;

        var newList = list.RemoveAll(item => !newSet.Contains(item));
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    public bool TryGetValue(TItem equalValue, out TItem actualValue) {
        return set.TryGetValue(equalValue, out actualValue);
    }

    IImmutableSet<TItem> IImmutableSet<TItem>.Union(IEnumerable<TItem> other) {
        return Union(other);
    }

    public ImmutableOrderedHashSet<TItem> WithComparer(IEqualityComparer<TItem>? equalityComparer) {
        return (ImmutableOrderedHashSet<TItem>)new ImmutableOrderedHashSet<TItem>(equalityComparer).Union(this);
    }

    private ImmutableOrderedHashSet<TItem> InternalAdd(TItem value) {
        var newSet = set.Add(value);
        if (newSet.Count == set.Count) return this;

        var newList = list.Add(value);
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    private ImmutableOrderedHashSet<TItem> InternalRemove(TItem value) {
        var newSet = set.Remove(value);
        if (newSet.Count == set.Count) return this;

        var newList = list.Remove(value, EqualityComparer);
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    public ImmutableOrderedHashSet<TItem> Add(TItem value) {
        return InternalAdd(value);
    }

    public ImmutableOrderedHashSet<TItem> Except(IEnumerable<TItem> other) {
        return (ImmutableOrderedHashSet<TItem>)((IImmutableSet<TItem>)this).Except(other);
    }

    public ImmutableOrderedHashSet<TItem> Remove(TItem value) {
        return InternalRemove(value);
    }

    public ImmutableOrderedHashSet<TItem> Union(IEnumerable<TItem> other) {
        var otherSet = other as IImmutableSet<TItem> ?? other.ToImmutableHashSet();
        var newSet = set.Union(otherSet);
        if (newSet.Count == set.Count) return this;

        var newList = list.AddRange(otherSet);
        return new ImmutableOrderedHashSet<TItem>(newList, newSet);
    }

    public bool SequenceEqual(IEnumerable<TItem> other) {
        return list.SequenceEqual(other);
    }

    public override bool Equals(object? obj) {
        return obj is ImmutableOrderedHashSet<TItem> other && Equals(other);
    }

    public bool Equals(ImmutableOrderedHashSet<TItem> immutableOrderedHashSet) {
        return set.Count == immutableOrderedHashSet.Count && set.SetEquals(immutableOrderedHashSet);
    }

    public override int GetHashCode() {
        return set.GetHashCode();
    }
}