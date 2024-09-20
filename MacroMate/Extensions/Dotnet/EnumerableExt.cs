using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroMate.Extensions.Dotnet;

public static class EnumerableExt {
    public static IEnumerable<int> RangeSE(int start, int end) => Enumerable.Range(start, end - start);

    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) {
        return source.Select((item, index) => (item, index));
    }

    public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> source) {
        return source.Where(item => item != null)!;
    }

    public static IEnumerable<B> SelectNonNull<A, B>(this IEnumerable<A?> source, Func<A?, B?> f) {
        return source.Select(a => f(a)).WithoutNull();
    }

    public static IEnumerable<List<T>> Lookahead<T>(
        this IEnumerable<T> source,
        int size
    ) {
        var buffer = new List<T>();
        foreach (var item in source) {
            buffer.Add(item);
            if (buffer.Count == size) {
                yield return buffer;
                buffer.RemoveAt(0);
            }
        }

        // Produce the partial windows
        while (buffer.Count > 0) {
            yield return buffer;
            buffer.RemoveAt(0);
        }
        if (buffer.Count == 0) yield return buffer;
    }

    public static IEnumerable<(TFirst?, TSecond?)> ZipWithDefault<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second
    ) => ZipWithDefault(first, second, (a, b) => (a, b));

    public static IEnumerable<TResult> ZipWithDefault<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst?, TSecond?, TResult> selector
    ) {
        bool firstMoveNext, secondMoveNext;

        using (var enum1 = first.GetEnumerator())
        using (var enum2 = second.GetEnumerator()) {
            while ((firstMoveNext = enum1.MoveNext()) & (secondMoveNext = enum2.MoveNext())) {
                yield return selector(enum1.Current, enum2.Current);
            }

            if (firstMoveNext && !secondMoveNext) {
                yield return selector(enum1.Current, default(TSecond));
                while (enum1.MoveNext()) {
                    yield return selector(enum1.Current, default(TSecond));
                }
            } else if (!firstMoveNext && secondMoveNext) {
                yield return selector(default(TFirst), enum2.Current);
                while (enum2.MoveNext()) {
                    yield return selector(default(TFirst), enum2.Current);
                }
            }
        }
    }

    // public static IEnumerable<(T, T?)> Lookahead<T>(this IEnumerable<T> source) where T : class {
    //     using (var iterator = source.GetEnumerator()) {
    //         if (!iterator.MoveNext()) {  }

    //         T current = iterator.Current;

    //         while (iterator.MoveNext()) {
    //             T? next = iterator.MoveNext() ? iterator.Current : null;
    //             yield return (current!, next);

    //             if (next != null) {
    //                 current = next;
    //             } else {
    //                 break;
    //             }
    //         }
    //     }
    // }
}
