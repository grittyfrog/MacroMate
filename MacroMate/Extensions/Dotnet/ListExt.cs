using System.Collections.Generic;
using System.Linq;

namespace MacroMate.Extensions.Dotnet;

public static class ListExt {
    public static void RemoveAllAt<T>(this List<T> self, IEnumerable<int> indicies) {
        foreach (var index in indicies.OrderByDescending(x => x)) {
            self.RemoveAt(index);
        }
    }
}
