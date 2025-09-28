# Adding New Conditions

This guide explains how to add new condition types to MacroMate, using the `PlayerLevelCondition` implementation as an example.

## Overview

Conditions in MacroMate determine when macros should be active based on game state. They implement either `IValueCondition` (for discrete values) or `INumericCondition` (for numeric comparisons).

## Steps to Add a New Condition

### 1. Create the Condition Class

Create a new file in `MacroMate/Conditions/` implementing the appropriate interface:

```csharp
namespace MacroMate.Conditions;

public record class PlayerLevelCondition(
    int Level
) : INumericCondition {
    public static PlayerLevelCondition? Current() {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) { return null; }

        return new PlayerLevelCondition(player.Level);
    }

    public int AsNumber() => Level;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Player Level";
        public string ExpressionName => "Player.Level";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.playerLevelCondition;
        public INumericCondition FromNumber(int num) =>
            new PlayerLevelCondition(num);
        public INumericCondition? Current() => PlayerLevelCondition.Current();
    }
}
```

**Key Requirements:**
- Use a `record class` for value semantics
- Implement `Current()` static method to get current game state
- Implement the appropriate interface (`INumericCondition` or `IValueCondition`)
- Provide a nested `ConditionFactory` class
- Use descriptive names for `ConditionName` and `ExpressionName`

### 2. Update CurrentConditions Record

Add the new condition to `MacroMate/Conditions/CurrentConditions.cs`:

```csharp
public record CurrentConditions(
    // ... existing conditions ...
    PlayerLevelCondition? playerLevelCondition,
    // ... rest of conditions ...
) {
    public static CurrentConditions Empty => new(
        // ... existing nulls ...
        playerLevelCondition: null,
        // ... rest of nulls ...
    );

    public IEnumerable<ICondition> AllConditions() {
        // ... existing conditions ...
        if (playerLevelCondition != null) { yield return playerLevelCondition; }
        // ... rest of conditions ...
    }

    public static CurrentConditions Current() => new(
        // ... existing conditions ...
        playerLevelCondition: PlayerLevelCondition.Current(),
        // ... rest of conditions ...
    );
}
```

### 3. Register in ICondition Interface

Add the factory to the `AllFactories` list in `MacroMate/Conditions/ICondition.cs`:

```csharp
public static ICondition.IFactory[] AllFactories => new ICondition.IFactory[] {
    // ... existing factories ...
    PlayerLevelCondition.Factory,
    // ... rest of factories ...
};
```

### 4. Add XML Serialization Support

Update `MacroMate/Serialization/V1/ConditionXML.cs`:

```csharp
// Add to XmlInclude attributes at the top
[XmlInclude(typeof(PlayerLevelConditionXML))]

// Add to the From method switch expression
PlayerLevelCondition cond => PlayerLevelConditionXML.From(cond),

// Add the XML class at the bottom
[XmlType("PlayerLevelCondition")]
public class PlayerLevelConditionXML : ConditionXML {
    public required int Level { get; set; }

    public override ICondition ToReal() => new PlayerLevelCondition(Level);

    public static PlayerLevelConditionXML From(PlayerLevelCondition cond) => new() {
        Level = cond.Level
    };
}
```

## Condition Types

### INumericCondition
Use for conditions with numeric values that support comparison operators (>, <, ==, etc.):
- Implement `int AsNumber()`
- Factory must implement `INumericCondition.IFactory`
- Users can write expressions like `Player.Level > 50`

### IValueCondition
Use for conditions with discrete values that only support equality:
- Implement `bool SatisfiedBy(ICondition other)`
- Implement `string ValueName` and `string NarrowName`
- Factory must implement `IValueCondition.IFactory`
- Users can only check exact matches

## Testing Your Condition

1. Build the project: `dotnet build MacroMate.sln`
2. Test the condition appears in the UI condition picker
3. Verify expressions parse correctly (e.g., `Player.Level >= 80`)
4. Test serialization by saving/loading configurations
5. Verify the condition updates in real-time as game state changes

## Common Patterns

- **Player State**: Access via `Env.ClientState.LocalPlayer`
- **Game Conditions**: Use `Env.Condition[ConditionFlag.xxx]`
- **Territory Info**: Use `Env.ClientState.TerritoryType`
- **Target State**: Access via `Env.TargetManager.Target`

Always check for null values when accessing game objects, as they may not be available in all contexts.

## Advanced Topics

### Handling Excel Sheets with Duplicate Entries

Some Excel sheets (like Weather) contain multiple entries with the same logical value but different IDs. When implementing conditions for such data:

**Problem**: The UI will show duplicate entries like "Clear Skies (1)", "Clear Skies (2)", etc.

**Solution**: Group by name in the `TopLevel()` method and use name-based comparison:

```csharp
public IEnumerable<IValueCondition> TopLevel() {
    return Env.DataManager.GetExcelSheet<Weather>()!
        .Where(weather => weather.RowId != 0 && !string.IsNullOrEmpty(weather.Name.ExtractText()))
        .GroupBy(weather => weather.Name.ExtractText())
        .Select(group => new WeatherCondition(group.First().RowId) as IValueCondition);
}

public bool SatisfiedBy(ICondition other) {
    if (other is WeatherCondition otherWeather) {
        // Compare by name instead of exact ID to handle duplicates
        return Weather.Name() == otherWeather.Weather.Name();
    }
    return false;
}
```

**Required imports**: Add `using MacroMate.Extensions.Lumina;` for `ExtractText()`.

### Adding Display Name Support

If your condition shows "<unknown>" in the UI, you need to add support for your Excel sheet type in `ExcelId<T>.Name()`:

**File**: `MacroMate/Extensions/Dalamud/Excel/ExcelId.cs`

```csharp
public string Name() {
    var gameData = GameData;
    // ... existing cases ...
    if (gameData is Weather weather) { return weather.Name.ExtractText(); }
    // ... rest of method ...
}
```

**Common Excel sheet name properties**:
- `Weather`: `weather.Name.ExtractText()`
- `Item`: `item.Name.ExtractText()`
- `ClassJob`: `job.Abbreviation.ExtractText()`
- `World`: `world.Name.ExtractText()`

### Using FFXIVClientStructs for Game State

For accessing game state not available through Dalamud services, use FFXIVClientStructs:

```csharp
using FFXIVClientStructs.FFXIV.Client.Game;

public static WeatherCondition? Current() {
    unsafe {
        var weatherManager = WeatherManager.Instance();
        if (weatherManager == null) { return null; }

        var currentWeatherId = weatherManager->WeatherId;
        if (currentWeatherId == 0) { return null; }

        return new WeatherCondition(currentWeatherId);
    }
}
```

**Note**: FFXIVClientStructs usage requires `unsafe` context and null checking.

## Complete Example: WeatherCondition

Here's a complete implementation showing all the advanced concepts:

```csharp
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Lumina;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MacroMate.Conditions;

public record class WeatherCondition(
    ExcelId<Weather> Weather
) : IValueCondition {
    public string ValueName => Weather.DisplayName();
    public string NarrowName => Weather.DisplayName();

    public bool SatisfiedBy(ICondition other) {
        if (other is WeatherCondition otherWeather) {
            // Compare by weather name instead of exact ID to handle duplicates
            return Weather.Name() == otherWeather.Weather.Name();
        }
        return false;
    }

    public WeatherCondition() : this(1) {}
    public WeatherCondition(uint weatherId) : this(new ExcelId<Weather>(weatherId)) {}

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    public static WeatherCondition? Current() {
        unsafe {
            var weatherManager = WeatherManager.Instance();
            if (weatherManager == null) { return null; }

            var currentWeatherId = weatherManager->WeatherId;
            if (currentWeatherId == 0) { return null; }

            return new WeatherCondition(currentWeatherId);
        }
    }

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Weather";
        public string ExpressionName => "Weather";

        public IValueCondition? Current() => WeatherCondition.Current();
        public IValueCondition Default() => new WeatherCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.weather;

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<Weather>()!
                .Where(weather => weather.RowId != 0 && !string.IsNullOrEmpty(weather.Name.ExtractText()))
                .GroupBy(weather => weather.Name.ExtractText())
                .Select(group => new WeatherCondition(group.First().RowId) as IValueCondition);
        }
    }
}
```

This example demonstrates:
- Name-based comparison for handling duplicates
- FFXIVClientStructs usage for game state access
- Grouping in TopLevel() to eliminate duplicate entries
- All required imports and patterns