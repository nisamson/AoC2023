#region license

// AoC2023 - AoC.Support - ResizeableArray.cs
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
using System.Runtime.Serialization;
using AoC.Support.Functional;

namespace AoC.Support.Collections;

[Serializable]
public class ResizeableArray<T>(int initialSize = 0) : IList<T>, IReadOnlyList<T>, ICloneable {
    public const int DefaultInitialSize = 16;
    private const int GrowthFactor = 2;
    private T[] data = initialSize == 0 ? Array.Empty<T>() : new T[initialSize];
    private int version;

    public ResizeableArray(IEnumerable<T> enumerable) : this() {
        AddRange(enumerable);
    }

    public ResizeableArray(IReadOnlyCollection<T> collection) : this(collection.Count) {
        AddRange(collection);
    }

    public int Capacity => data.Length;

    object ICloneable.Clone() {
        return Clone();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void Add(T item) {
        Insert(Count, item);
    }

    public void Clear() {
        TruncateCapacity(0);
    }

    public bool Contains(T item) {
        return IndexOf(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        data.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item) {
        var index = Array.IndexOf(data, item);
        if (index == -1) return false;

        RemoveAt(index);
        return true;
    }

    public int Count { get; private set; }
    public bool IsReadOnly => false;

    public int IndexOf(T item) {
        return Array.IndexOf(data, item);
    }

    public void Insert(int index, T item) {
        if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));

        if (Count == Capacity) EnsureCapacity(Capacity * GrowthFactor);

        version++;
        if (Count == index) {
            data[index] = item;
            Count++;
            return;
        }

        Array.Copy(data, index, data, index + 1, Count - index);
        data[index] = item;
        Count++;
    }

    public void RemoveAt(int index) {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

        Array.Copy(data, index + 1, data, index, Count - index - 1);
        Count--;
        version++;
    }

    public T this[int index] {
        get => data[index];
        set => data[index] = value;
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this);
    }

    public void AddRange(IEnumerable<T> enumerable) {
        foreach (var item in enumerable) Add(item);
    }

    public void AddRange(IReadOnlyCollection<T> collection) {
        var newCount = Count + collection.Count;
        if (newCount > Capacity) EnsureCapacity(newCount);

        var arr = collection switch {
            T[] a => new ReadOnlyMemory<T>(a).Some(),
            ResizeableArray<T> ra => new ReadOnlyMemory<T>(ra.data).Some(),
            _ => Option.None<ReadOnlyMemory<T>>()
        };

        if (arr.IsSome) {
            // FIXME
            arr.Value.CopyTo(data.AsMemory(Count));
            Count = newCount;
            return;
        }

        AddRange((IEnumerable<T>)collection);
    }

    /// <summary>
    ///     Resizes the array to the given size. If the new size is smaller than the current size, the array is truncated.
    ///     If the new size is larger than the current size, the array is extended with the given default value.
    ///     If the new size is greater than the current capacity,
    /// </summary>
    /// <param name="newSize">the size of the array after this function returns.</param>
    /// <param name="defaultValue">the default value to fill the array with if it grows.</param>
    /// <returns>true if the capacity of the array was changed.</returns>
    public bool Resize(int newSize, T? defaultValue = default) {
        ArgumentOutOfRangeException.ThrowIfNegative(newSize);
        var oldLength = Count;
        bool res;
        if (newSize > Capacity)
            res = EnsureCapacity(newSize);
        else if (newSize < Capacity)
            res = TruncateCapacity(newSize);
        else
            return false;

        if (EqualityComparer<T>.Default.Equals(defaultValue, default)) return res;

        Array.Fill(data, defaultValue, oldLength, newSize - oldLength);
        return res;
    }

    public bool EnsureCapacity(int capacity) {
        if (capacity < Capacity) return false;

        var newCapacity = Math.Max(DefaultInitialSize, Capacity * GrowthFactor);

        Array.Resize(ref data, newCapacity);
        return true;
    }

    public bool TruncateCapacity(int newSize) {
        if (newSize >= Capacity) return false;

        Array.Resize(ref data, newSize);
        Count = Math.Min(Capacity, Count);
        return true;
    }

    public Span<T> AsSpan() {
        return data.AsSpan(0, Count);
    }

    public ReadOnlySpan<T> AsReadOnlySpan() {
        return data.AsSpan(0, Count);
    }

    public Memory<T> AsMemory() {
        return data.AsMemory(0, Count);
    }

    public ReadOnlyMemory<T> AsReadOnlyMemory() {
        return data.AsMemory(0, Count);
    }

    public ResizeableArray<T> Clone() {
        return new ResizeableArray<T>(data);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) {
        info.AddValue(nameof(data), data);
    }

    public sealed class Enumerator : IEnumerator<T> {
        private readonly ResizeableArray<T> array;
        private readonly int startVersion;
        private int index = -1;

        internal Enumerator(ResizeableArray<T> array) {
            this.array = array;
            startVersion = array.version;
        }

        public bool MoveNext() {
            CheckVersion();
            index++;
            return index < array.Count;
        }

        public void Reset() {
            CheckVersion();
            index = -1;
        }

        public T Current => array[index];

        object? IEnumerator.Current => Current;

        public void Dispose() { }

        private void CheckVersion() {
            if (startVersion != array.version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
        }
    }
}