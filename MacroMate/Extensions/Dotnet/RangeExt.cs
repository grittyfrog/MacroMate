using System;
using System.Collections.Generic;

namespace MacroMate.Extensions.Dotnet;

public static class RangeExt {
    public static int Length(this Range range) {
        return (range.End.Value - range.Start.Value) + 1;
    }

    public static IEnumerable<int> Enumerate(this Range range) {
        if (range.Start.IsFromEnd || range.End.IsFromEnd) {
            throw new ArgumentException(nameof(range));
        }

        for (var value = range.Start.Value; value < range.End.Value; ++value) {
            yield return value;
        }
    }
}
