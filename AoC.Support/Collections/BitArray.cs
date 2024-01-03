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
using System.Numerics;
using System.Runtime.CompilerServices;
using AoC.Support.Numerics;
using CommunityToolkit.HighPerformance;

namespace AoC.Support.Collections;

public sealed class BitArray : ICollection, IEnumerable<bool>, IStructuralEquatable, ICloneable {
    private readonly ulong[] data;
    
    private const int BitsPerLong = 64;
    private const int IndexMask = BitsPerLong - 1;
    private const int ShiftMask = 6;
    private static int VectorSize => Vector<ulong>.Count;

    public BitArray(int initialSize = 0, bool defaultValue = false) {
        data = new ulong[(initialSize + BitsPerLong - 1) / BitsPerLong];
        if (defaultValue) {
            Array.Fill(data, ulong.MaxValue);
        }
        
        Count = initialSize;
        FixUpLastElement();
    }

    IEnumerator<bool> IEnumerable<bool>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this);
    }

    public void Clear() {
        Array.Clear(data, 0, data.Length);
    }

    public bool Contains(bool item) {
        return item ? data.Any(l => l != 0) : data.Any(l => l == 0);
    }

    public void CopyTo(bool[] array, int index) {
        for (var i = 0; i < data.Length; i++) {
            var longValue = data[i];
            for (var j = 0; j < BitsPerLong; j++) {
                array[index++] = (longValue & (1UL << j)) != 0;
            }
        }
    }

    public void CopyTo(BitArray array) {
        Array.Copy(data, array.data, data.Length);
    }

    public void CopyTo(Array array, int index) {
        if (array is not bool[] boolArray) {
            throw new ArgumentException("The array must be of type bool[]", nameof(array));
        }

        CopyTo(boolArray, index);
    }

    /// <summary>
    /// Returns the number of bits in the BitArray. If the BitArray is larger than <see cref="int.MaxValue"/>, returns <see cref="int.MaxValue"/>.
    /// </summary>
    public int Count { get; private set; }

    public bool IsSynchronized => false;
    public object SyncRoot => this;

    public int IndexOf(bool item) {
        return this.Select((b, i) => (b, i)).Where((tuple => tuple.b == item)).Select(t => t.i).FirstOrDefault(-1);
    }

    private delegate Vector<ulong> Mutator(Vector<ulong> a, Vector<ulong> b);

    private delegate ulong MutatorScalar(ulong a, ulong b);
    
    private delegate Vector<ulong> Transformer(Vector<ulong> a);
    private delegate ulong TransformerScalar(ulong a);

    private void MutateWithOther(BitArray other, Mutator mutator, MutatorScalar scalar) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Count, Count);
        var span = new Span<ulong>(data);
        var otherSpan = new Span<ulong>(other.data);

        while (span.Length >= VectorSize) {
            var thisVec = new Vector<ulong>(span);
            var otherVec = new Vector<ulong>(otherSpan);
            thisVec = mutator(thisVec, otherVec);
            thisVec.CopyTo(span);
            span = span[VectorSize..];
            otherSpan = otherSpan[VectorSize..];
        }

        while (span.Length > 0) {
            span[0] = scalar(span[0], otherSpan[0]);
            span = span[1..];
            otherSpan = otherSpan[1..];
        }
    }

    private void TransformInPlace(Transformer transformer, TransformerScalar scalar) {
        var span = new Span<ulong>(data);
        
        while (span.Length >= VectorSize) {
            var thisVec = new Vector<ulong>(span);
            thisVec = transformer(thisVec);
            thisVec.CopyTo(span);
            span = span[VectorSize..];
        }
        
        while (span.Length > 0) {
            span[0] = scalar(span[0]);
            span = span[1..];
        }
    }
    
    public BitArray Not() {
        TransformInPlace(a => ~a, a => ~a);
        FixUpLastElement();
        return this;
    }

    // Some operations modify bits in the last element beyond the Count. This method fixes those bits.
    private void FixUpLastElement() {
        if (data.Length > 0) {
            data[^1] &= ulong.MaxValue >> (BitsPerLong - (Count & IndexMask));
        }
    }

    private static (int, int) GetIndices(uint index) {
        return ((int, int)) (index >> ShiftMask, index & IndexMask);
    }

    public bool this[int index] {
        get {
            var (longIndex, bitIndex) = GetIndices((uint) index);
            return (data[longIndex] & (1UL << bitIndex)) != 0;
        }
        set {
            var (longIndex, bitIndex) = GetIndices((uint) index);
            if (value) {
                data[longIndex] |= 1UL << bitIndex;
            } else {
                data[longIndex] &= ~(1UL << bitIndex);
            }
        }
    }

    public sealed class Enumerator : IEnumerator<bool> {
        private readonly BitArray bitArray;
        private int index = -1;

        public Enumerator(BitArray bitArray) {
            this.bitArray = bitArray;
        }

        public bool MoveNext() {
            index++;
            return index < bitArray.Count;
        }

        public void Reset() {
            index = -1;
        }

        public bool Current => bitArray[index];

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }

    public BitArray And(BitArray other) {
        MutateWithOther(
            other,
            (a, b) => a & b,
            (a, b) => a & b
        );
        return this;
    }

    public BitArray Or(BitArray other) {
        MutateWithOther(
            other,
            (a, b) => a | b,
            (a, b) => a | b
            );
        return this;
    }
    
    public BitArray Xor(BitArray other) {
        MutateWithOther(
            other,
            (a, b) => a ^ b,
            (a, b) => a ^ b
        );
        return this;
    }

    public bool IntersectionIsEmpty(BitArray other) {
        for (var i = 0; i < data.Length; i++) {
            if ((data[i] & other.data[i]) != 0) {
                return false;
            }
        }
        
        return true;
    }
    
    public delegate T Accumulator<T>(T acc, Vector<ulong> vec);

    public delegate T BiAccumulator<T>(T acc, Vector<ulong> a, Vector<ulong> b);

    public delegate T AccumulatorScalar<T>(T acc, ulong vec);

    public delegate T BiAccumulatorScalar<T>(T acc, ulong a, ulong b);
    
    public T Accumulate<T>(T seed, Accumulator<T> accumulator, AccumulatorScalar<T> scalar) {
        var span = new Span<ulong>(data);
        var acc = seed;
        while (span.Length >= VectorSize) {
            var vec = new Vector<ulong>(span);
            acc = accumulator(acc, vec);
            span = span[VectorSize..];
        }

        while (span.Length > 0) {
            acc = scalar(acc, span[0]);
            span = span[1..];
        }

        return acc;
    }

    private static bool AddBool(bool a, bool b) => a | b;
    private static bool MulBool(bool a, bool b) => a | b;
    
    private static Vector<ulong> AddVector(Vector<ulong> a, Vector<ulong> b) => a | b;
    private static Vector<ulong> MulVector(Vector<ulong> a, Vector<ulong> b) => a & b;
    private static bool SumVector(Vector<ulong> a) => a != Vector<ulong>.Zero;
    
    private static ulong AddScalar(ulong a, ulong b) => a | b;
    private static ulong MulScalar(ulong a, ulong b) => a & b;

    public bool DotProduct(BitArray other) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Count, Count);
        return BiAccumulate(other, false, (b, av, bv) => b || SumVector(MulVector(av, bv)), (b, a, c) => (b.ToByte() | (a & c)) != 0);
    }

    public T BiAccumulate<T>(BitArray other, T seed, BiAccumulator<T> biAccumulator, BiAccumulatorScalar<T> scalar) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(other.Count, Count);
        var span = new Span<ulong>(data);
        var otherSpan = new Span<ulong>(other.data);
        var acc = seed;

        while (span.Length >= VectorSize) {
            var thisVec = new Vector<ulong>(span);
            var otherVec = new Vector<ulong>(otherSpan);
            acc = biAccumulator(seed, thisVec, otherVec);
            span = span[VectorSize..];
            otherSpan = otherSpan[VectorSize..];
        }

        while (span.Length > 0) {
            acc = scalar(acc, span[0], otherSpan[0]);
            span = span[1..];
            otherSpan = otherSpan[1..];
        }

        return acc;
    }
    
    public void SetAll(bool value) {
        if (value) {
            Array.Fill(data, ulong.MaxValue);
        } else {
            Array.Fill(data, 0UL);
        }
        
        FixUpLastElement();
    }

    public IEnumerable<int> EnumerateSetBits() {
        return this.Select((b, i) => (b, i)).Where(tuple => tuple.b).Select(tuple => tuple.i);
    }

    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) {
        return Equals(other, comparer);
    }

    public override bool Equals(object? other) {
        return Equals(other, EqualityComparer<bool>.Default);
    }

    public bool Equals(object? other, IEqualityComparer comparer) {
        if (ReferenceEquals(this, other)) {
            return true;
        }
        
        if (other is not BitArray bitArray) {
            return false;
        }
        
        if (comparer is not IEqualityComparer<bool> boolComparer) {
            throw new ArgumentException("The comparer must be of type IEqualityComparer<bool>", nameof(comparer));
        }

        return this.SequenceEqual(bitArray, boolComparer);
    }


    public bool Equals(BitArray other) {
        if (Count != other.Count) {
            return false;
        }

        return data.SequenceEqual(other.data);
    }

    public int GetHashCode(IEqualityComparer comparer) {
        return this.Aggregate(0, (i, b) => HashCode.Combine(i, comparer.GetHashCode(b)));
    }

    public override int GetHashCode() {
        return data.GetDjb2HashCode();
    }

    public int CountSetBits() {
        var count = data.AsReadOnlySpan().PopCount();
        return (int) count;
    }

    object ICloneable.Clone() {
        return Clone();
    }

    public BitArray Clone() {
        var clone = new BitArray(Count);
        Array.Copy(data, clone.data, data.Length);
        return clone;
    }

    public override string ToString() {
        return $"{nameof(BitArray)}[{Count}]";
    }

    public void AndNot(BitArray otherData) {
        MutateWithOther(
            otherData,
            (a, b) => a & ~b,
            (a, b) => a & ~b
        );
    }
}
