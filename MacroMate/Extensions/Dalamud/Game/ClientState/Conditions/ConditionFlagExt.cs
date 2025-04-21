using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Extensions.Dalamud.Game.ClientState.Conditions;

public static class ConditionFlagExt {
    private static Dictionary<ConditionFlag, string> Names = EnumExt.BuildNameMap<ConditionFlag>();

    /// <summary>
    /// Gets the correct name of ConditionFlag, making sure to avoid [Obsolete] names
    /// </summary>
    public static string Name(this ConditionFlag self) {
        if (Names.TryGetValue(self, out var name)) { return name; }

        return self.ToString();
    }
}
