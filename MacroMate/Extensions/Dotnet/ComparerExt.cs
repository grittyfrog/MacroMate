using System.Collections.Generic;

namespace MacroMate.Extensions.Dotnet;

public static class ComparerExt {
    public static Comparer<T> Compose<T>(params Comparer<T>[] comparers) =>
        Comparer<T>.Create((a, b) => {
            var result = 0;
            foreach (var comparer in comparers) {
                result = comparer.Compare(a, b);
                if (result != 0) { break; }
            }
            return result;
        });
}
