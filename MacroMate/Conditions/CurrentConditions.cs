using System.Collections.Generic;

namespace MacroMate.Conditions;

/// The Current Conditions used to evaluate condition expressions.
///
/// All objects in the records should use _structural equality_. If anything
/// is using reference equality we will see significant performance regression.
public record CurrentConditions(
    ContentCondition? content,
    LocationCondition? location,
    TargetNameCondition? targetNpc,
    JobCondition? job,
    PvpStateCondition? pvpState,
    PlayerConditionCondition? playerCondition,
    PlayerStatusCondition? playerStatusCondition,
    HUDLayoutCondition? hudLayoutCondition,
    CurrentCraftMaxDurabilityCondition? craftMaxDurabilityCondition,
    CurrentCraftMaxQualityCondition? craftMaxQualityCondition,
    CurrentCraftDifficultyCondition? craftDifficultyCondition,
    WorldCondition? world
) {
    public static CurrentConditions Empty => new CurrentConditions(
        content: null,
        location: null,
        targetNpc: null,
        job: null,
        pvpState: null,
        playerCondition: null,
        playerStatusCondition: null,
        hudLayoutCondition: null,
        craftMaxDurabilityCondition: null,
        craftMaxQualityCondition: null,
        craftDifficultyCondition: null,
        world: null
    );

    public IEnumerable<ICondition> Enumerate() {
        if (content != null) { yield return content; }
        if (location != null) { yield return location; }
        if (targetNpc != null) { yield return targetNpc; }
        if (job != null) { yield return job; }
        if (pvpState != null) { yield return pvpState; }
        if (playerCondition != null) { yield return playerCondition; }
        if (playerStatusCondition != null) { yield return playerStatusCondition; }
        if (hudLayoutCondition != null) { yield return hudLayoutCondition; }
        if (craftMaxDurabilityCondition != null) { yield return craftMaxDurabilityCondition; }
        if (craftMaxQualityCondition != null) { yield return craftMaxQualityCondition; }
        if (craftDifficultyCondition != null) { yield return craftDifficultyCondition; }
        if (world != null) { yield return world; }
    }

    public static CurrentConditions Query() {
        return new CurrentConditions(
            content: ContentCondition.Current(),
            location: LocationCondition.Current(),
            targetNpc: TargetNameCondition.Current(),
            job: JobCondition.Current(),
            pvpState: PvpStateCondition.Current(),
            playerCondition: PlayerConditionCondition.Current(),
            playerStatusCondition: PlayerStatusCondition.Current(),
            hudLayoutCondition: HUDLayoutCondition.Current(),
            craftMaxDurabilityCondition: CurrentCraftMaxDurabilityCondition.Current(),
            craftMaxQualityCondition: CurrentCraftMaxQualityCondition.Current(),
            craftDifficultyCondition: CurrentCraftDifficultyCondition.Current(),
            world: WorldCondition.Current()
        );
    }
}
