using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace MacroMate.Extensions.Dotnet;

public static class ImmutuableListExt {
    [Pure]
    public static ImmutableList<T> CastUp<TDerived, T>(
        this ImmutableList<TDerived> items
    ) where TDerived : class, T {
        return ((IEnumerable<T>)items).ToImmutableList();
    }
}
