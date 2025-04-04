using System.Collections.Generic;

namespace MacroMate.Conditions;

/// The Current Conditions used to evaluate condition expressions.
///
/// All objects in the records should use _structural equality_. If anything
/// is using reference equality we will see significant performance regression.
public record CurrentConditions(
    ContentCondition? content = null,
    LocationCondition? location = null,
    TargetNameCondition? targetNpc = null,
    JobCondition? job = null,
    PvpStateCondition? pvpState = null,
    PlayerConditionCondition? playerCondition = null,
    HUDLayoutCondition? hudLayoutCondition = null,
    CurrentCraftMaxDurabilityCondition? craftMaxDurabilityCondition = null,
    CurrentCraftMaxQualityCondition? craftMaxQualityCondition = null,
    CurrentCraftDifficultyCondition? craftDifficultyCondition = null
) {
    public static CurrentConditions Empty => new CurrentConditions();

    public IEnumerable<ICondition> Enumerate() {
        if (content != null) { yield return content; }
        if (location != null) { yield return location; }
        if (targetNpc != null) { yield return targetNpc; }
        if (job != null) { yield return job; }
        if (pvpState != null) { yield return pvpState; }
        if (playerCondition != null) { yield return playerCondition; }
        if (hudLayoutCondition != null) { yield return hudLayoutCondition; }
        if (craftMaxDurabilityCondition != null) { yield return craftMaxDurabilityCondition; }
        if (craftMaxQualityCondition != null) { yield return craftMaxQualityCondition; }
        if (craftDifficultyCondition != null) { yield return craftDifficultyCondition; }
    }
}
