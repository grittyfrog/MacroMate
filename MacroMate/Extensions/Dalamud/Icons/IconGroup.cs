
using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Extensions.Dalamud.Icons;

public record struct IconGroup(Lazy<List<uint>> LazyIconIds, string Name) {
    public List<uint> IconIds => LazyIconIds.Value;

    public IconGroup(Range range, string name) : this(GetIconIds(range), name) {}
    public IconGroup(List<uint> iconIds, string name) : this(new Lazy<List<uint>>(() => iconIds), name) {}

    private static Lazy<List<uint>> GetIconIds(Range possibleIconIdsRanges) {
        return new Lazy<List<uint>>(() => EnumerateIconIds(possibleIconIdsRanges).ToList());
    }

    private static IEnumerable<uint> EnumerateIconIds(Range possibleIconIdsRange) {
        foreach (var possibleIconId in possibleIconIdsRange.Enumerate()) {
            if (Env.TextureProvider.GetIconPath((uint)possibleIconId) != null) {
                yield return (uint)possibleIconId;
            }
        }
    }
}
