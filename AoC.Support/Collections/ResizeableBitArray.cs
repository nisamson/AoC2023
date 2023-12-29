#region license

// AoC2023 - AoC.Support - BitArray.cs
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

namespace AoC.Support.Collections;

public class ResizeableBitArray : IList<bool>, IReadOnlyList<bool>, ICollection<bool>, IEnumerable<bool> {
    private readonly ResizeableArray<ulong> data;

    private const long BitsPerLong = sizeof(ulong) * 8;
    private ulong version = 0;

    public ResizeableBitArray(long initialSize = 0) {
        data = new ResizeableArray<ulong>((int) ((initialSize + BitsPerLong - 1) / BitsPerLong));
        LongCount = initialSize;
    }

    IEnumerator<bool> IEnumerable<bool>.GetEnumerator() {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
    
    public Enumerator GetEnumerator() {
        return new Enumerator(this);
    }

    public void Add(bool item) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    public bool Contains(bool item) {
        throw new NotImplementedException();
    }

    public void CopyTo(bool[] array, int arrayIndex) {
        throw new NotImplementedException();
    }

    public bool Remove(bool item) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the number of bits in the BitArray. If the BitArray is larger than <see cref="int.MaxValue"/>, returns <see cref="int.MaxValue"/>.
    /// </summary>
    public int Count => int.CreateSaturating(LongCount);

    public long LongCount { get; private set; }
    public bool IsReadOnly => false;

    public int IndexOf(bool item) {
        throw new NotImplementedException();
    }
    
    public int LongIndexOf(bool item) {
        throw new NotImplementedException();
    }

    public void Insert(int index, bool item) {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index) {
        throw new NotImplementedException();
    }

    public bool this[int index] {
        get => this[(long)index];
        set => this[(long)index] = value;
    }
    
    public bool this[long index] {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public sealed class Enumerator : IEnumerator<bool> {
        
        private readonly ResizeableBitArray bitArray;
        private long index = -1;
        private readonly ulong version;
        
        public Enumerator(ResizeableBitArray bitArray) {
            this.bitArray = bitArray;
            version = bitArray.version;
        }
        
        public bool MoveNext() {
            CheckVersion();
            index++;
            return index < bitArray.LongCount;
        }

        public void Reset() {
            CheckVersion();
            index = -1;
        }
        
        private void CheckVersion() {
            if (version != bitArray.version) {
                throw new InvalidOperationException("The underlying collection was modified after the enumerator was created.");
            }
        }

        public bool Current => bitArray[index];

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }
}
