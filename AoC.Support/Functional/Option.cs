#region license

// AoC2023 - AoC.Support - Option.cs
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

namespace AoC.Support.Functional;

public interface IOption<T> : IEnumerable<T>, IStructuralEquatable, IStructuralComparable where T : notnull {
    bool IsSome { get; }
    bool IsNone { get; }
    T Value { get; }

    static None<T> None => None<T>.Instance;


    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        if (IsSome) yield return Value;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    internal InternalOnly CannotImplementExternally();

    static Some<T> Some(T value) {
        return new Some<T>(value);
    }

    IOption<TResult> Map<TResult>(Func<T, TResult> f) where TResult : notnull {
        return IsSome ? IOption<TResult>.Some(f(Value)) : None<TResult>.Instance;
    }

    IOption<TResult> Bind<TResult>(Func<T, IOption<TResult>> f) where TResult : notnull {
        return IsSome ? f(Value) : None<TResult>.Instance;
    }

    IOption<T> Or(IOption<T> other) {
        return IsSome ? this : other;
    }

    IOption<T> OrElse(Func<IOption<T>> other) {
        return IsSome ? this : other();
    }

    IOption<T> Or(T other) {
        return IsSome ? this : Some(other);
    }

    IOption<T> OrElse(Func<T> other) {
        return IsSome ? this : Some(other());
    }

    TResult MapOrElse<TResult>(Func<T, TResult> f, Func<TResult> other) {
        return IsSome ? f(Value) : other();
    }

    TResult MapOr<TResult>(Func<T, TResult> f, TResult other) {
        return IsSome ? f(Value) : other;
    }

    IOption<T> And(IOption<T> other) {
        return IsSome ? other : this;
    }

    IOption<T> Filter(Predicate<T> predicate) {
        return IsSome && predicate(Value) ? this : None;
    }

    IOption<T> AndThen(Action<T> action) {
        if (IsSome) action(Value);
        return this;
    }


    bool IsSomeAnd(Predicate<T> predicate) {
        return IsSome && predicate(Value);
    }

    static IOption<T> OfNullable(T? value) {
        return value is not null ? Some(value) : None;
    }

    internal abstract class InternalOnly { }
}

public record None<T> : IOption<T> where T : notnull {
    public static readonly None<T> Instance = new();

    private None() { }

    IOption<T>.InternalOnly IOption<T>.CannotImplementExternally() {
        throw new NotImplementedException();
    }

    public bool IsSome => false;
    public bool IsNone => true;
    public T Value => throw new InvalidOperationException("Cannot get value of None");

    public bool Equals(object? other, IEqualityComparer comparer) {
        return other is None<T>;
    }

    public int GetHashCode(IEqualityComparer comparer) {
        return 0;
    }

    public int CompareTo(object? other, IComparer comparer) {
        return other is None<T> ? 0 : 1;
    }

    public override string ToString() {
        return "None";
    }
}

public record Some<T>(T Value) : IOption<T> where T : notnull {
    public bool IsSome => true;
    public bool IsNone => false;

    IOption<T>.InternalOnly IOption<T>.CannotImplementExternally() {
        throw new NotImplementedException();
    }

    public bool Equals(object? other, IEqualityComparer comparer) {
        return other is Some<T> some && comparer.Equals(Value, some.Value);
    }

    public int GetHashCode(IEqualityComparer comparer) {
        return comparer.GetHashCode(Value);
    }

    public int CompareTo(object? other, IComparer comparer) {
        return other is Some<T> some ? comparer.Compare(Value, some.Value) : 1;
    }

    public override string ToString() {
        return $"Some({Value})";
    }

    public static implicit operator T(Some<T> some) {
        return some.Value;
    }

    public static implicit operator Some<T>(T value) {
        return new Some<T>(value);
    }
}

public static class Option {
    public static IOption<T> None<T>() where T : notnull {
        return IOption<T>.None;
    }

    public static Some<T> Some<T>(this T value) where T : notnull {
        return new Some<T>(value);
    }


    public static IOption<T> FirstOrNone<T>(this IEnumerable<T> enumerable) where T : notnull {
        foreach (var item in enumerable) return Some(item);
        return None<T>();
    }

    public static IOption<T> FirstOrNone<T>(this IEnumerable<T> enumerable, Predicate<T> predicate) where T : notnull {
        foreach (var item in enumerable)
            if (predicate(item))
                return Some(item);

        return None<T>();
    }

    public static IOption<T> Flatten<T>(this IOption<IOption<T>> option) where T : notnull {
        return option.Bind(x => x);
    }
}