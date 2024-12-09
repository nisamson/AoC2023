using System.Diagnostics;

namespace AoC.Support.Numerics;

using System.Numerics;

public static class Conversion {
    public static T NextPowerOf<T>(this T value, T @base) where T : INumber<T> {
        var current = T.One;
        while (current.CompareTo(value) <= 0) {
            checked {
                current *= @base;
            }
        }

        return current;
    }

    public static T Concatenate<T>(this T value, T other, T @base) where T : INumber<T> {
        var power = other.NextPowerOf(@base);
        return checked(value * power + other);
    }

    public static long ConcatenateDecimalChars(this long a, long b) {
        Span<char> buffer = stackalloc char[20];
        var i = WriteMagnitudeDecimalDigitsReversed(b, buffer);
        Debug.Assert(i >= 1);
        var j = WriteMagnitudeDecimalDigitsReversed(a, buffer[i..]);
        Debug.Assert(j >= 1);
        var relevant = buffer[..(i + j)];
        relevant.Reverse();
        return long.Parse(relevant);
    }

    public static long ConcatenateDecimalMagnitude(this long a, long b) {
        Span<byte> buffer = stackalloc byte[20];
        var i = WriteMagnitudeDecimalBytesReversed(b, buffer);
        var j = WriteMagnitudeDecimalBytesReversed(a, buffer[i..]);
        var relevant = buffer[..(i + j)];
        relevant.Reverse();
        var result = 0L;
        foreach (var dig in relevant) {
            result = result * 10 + dig;
        }

        return result;
    }

    private static int WriteMagnitudeDecimalDigitsReversed(long value, Span<char> buffer) {
        var i = 0;
        for (; i < buffer.Length && value > 0; i++) {
            var (div, rem) = Math.DivRem(value, 10);
            buffer[i] = (char)('0' + rem);
            value = div;
        }
        return i;
    }
    
    private static int WriteMagnitudeDecimalBytesReversed(long value, Span<byte> buffer) {
        var i = 0;
        if (value == 0) {
            buffer[i++] = 0;
        }
        for (; i < buffer.Length && value > 0; i++) {
            var (div, rem) = Math.DivRem(value, 10);
            buffer[i] = (byte)rem;
            value = div;
        }
        
        return value == 0 ? i : -1;
    }
}