using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroMate.Extensions.Dotnet;

public static class EnumerableExt {
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) {
        return source.Select((item, index) => (item, index));
    }

    public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> source) {
        return source.Where(item => item != null)!;
    }

    public static IEnumerable<B> SelectNonNull<A, B>(this IEnumerable<A?> source, Func<A?, B?> f) {
        return source.Select(a => f(a)).WithoutNull();
    }
}
