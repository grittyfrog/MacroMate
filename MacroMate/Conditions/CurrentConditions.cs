using System.Collections.Generic;

namespace MacroMate.Conditions;

public record CurrentConditions(
    ContentCondition? content = null,
    LocationCondition? location = null,
    TargetNameCondition? targetNpc = null,
    JobCondition? job = null
) {
    public static CurrentConditions Empty => new CurrentConditions();

    public IEnumerable<ICondition> Enumerate() {
        if (content != null) { yield return content; }
        if (location != null) { yield return location; }
        if (targetNpc != null) { yield return targetNpc; }
        if (job != null) { yield return job; }
    }
}

