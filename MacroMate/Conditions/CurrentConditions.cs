using System.Collections.Generic;

namespace MacroMate.Conditions;

/// The Current Conditions used to evaluate condition expressions.
///
/// All objects in the records should use _structural equality_. If anything
/// is using reference equality we will see significant performance regression.
public record CurrentConditions(
    ContentCondition? content,
    CurrentCharacterCondition? currentCharacter,
    LocationCondition? location,
    TargetNameCondition? targetNpc,
    JobCondition? job,
    PvpStateCondition? pvpState,
    PlayerConditionCondition? playerCondition,
    PlayerLevelCondition? playerLevelCondition,
    PlayerStatusCondition? playerStatusCondition,
    HUDLayoutCondition? hudLayoutCondition,
    CurrentCraftMaxDurabilityCondition? craftMaxDurabilityCondition,
    CurrentCraftMaxQualityCondition? craftMaxQualityCondition,
    CurrentCraftDifficultyCondition? craftDifficultyCondition,
    MapMarkerLocationCondition? mapMarkerLocationCondition,
    WorldCondition? world,
    WeatherCondition? weather
) {
    public static CurrentConditions Empty => new CurrentConditions(
        content: null,
        currentCharacter: null,
        location: null,
        targetNpc: null,
        job: null,
        pvpState: null,
        playerCondition: null,
        playerLevelCondition: null,
        playerStatusCondition: null,
        hudLayoutCondition: null,
        craftMaxDurabilityCondition: null,
        craftMaxQualityCondition: null,
        craftDifficultyCondition: null,
        mapMarkerLocationCondition: null,
        world: null,
        weather: null
    );

    public IEnumerable<ICondition> Enumerate() {
        if (content != null) { yield return content; }
        if (currentCharacter != null) { yield return currentCharacter; }
        if (location != null) { yield return location; }
        if (targetNpc != null) { yield return targetNpc; }
        if (job != null) { yield return job; }
        if (pvpState != null) { yield return pvpState; }
        if (playerCondition != null) { yield return playerCondition; }
        if (playerLevelCondition != null) { yield return playerLevelCondition; }
        if (playerStatusCondition != null) { yield return playerStatusCondition; }
        if (hudLayoutCondition != null) { yield return hudLayoutCondition; }
        if (craftMaxDurabilityCondition != null) { yield return craftMaxDurabilityCondition; }
        if (craftMaxQualityCondition != null) { yield return craftMaxQualityCondition; }
        if (craftDifficultyCondition != null) { yield return craftDifficultyCondition; }
        if (mapMarkerLocationCondition != null) { yield return mapMarkerLocationCondition; }
        if (world != null) { yield return world; }
        if (weather != null) { yield return weather; }
    }

    public static CurrentConditions Query() {
        return new CurrentConditions(
            content: ContentCondition.Current(),
            currentCharacter: CurrentCharacterCondition.Current(),
            location: LocationCondition.Current(),
            targetNpc: TargetNameCondition.Current(),
            job: JobCondition.Current(),
            pvpState: PvpStateCondition.Current(),
            playerCondition: PlayerConditionCondition.Current(),
            playerLevelCondition: PlayerLevelCondition.Current(),
            playerStatusCondition: PlayerStatusCondition.Current(),
            hudLayoutCondition: HUDLayoutCondition.Current(),
            craftMaxDurabilityCondition: CurrentCraftMaxDurabilityCondition.Current(),
            craftMaxQualityCondition: CurrentCraftMaxQualityCondition.Current(),
            craftDifficultyCondition: CurrentCraftDifficultyCondition.Current(),
            mapMarkerLocationCondition: MapMarkerLocationCondition.Current(),
            world: WorldCondition.Current(),
            weather: WeatherCondition.Current()
        );
    }
}
