# Character Tracking Service & Current Character Condition

## Implementation Progress

- [ ] 1. Create CharacterInfo and CharacterTrackingData models
- [ ] 2. Update Configuration.cs to store character tracking data
- [ ] 3. Create CharacterTrackingManager service
- [ ] 4. Register CharacterTrackingManager in Env.cs
- [ ] 5. Create CurrentCharacterCondition
- [ ] 6. Update CurrentConditions.cs
- [ ] 7. Register condition in ICondition.cs
- [ ] 8. Add XML serialization support in ConditionXML.cs
- [ ] 9. Implement character tracking on login
- [ ] 10. Test build and functionality

## Overview

This document outlines the plan for implementing a character tracking service in MacroMate that will track all characters a player has logged in with. This will enable a new "Current Character" condition that allows macros to be activated based on which character is currently logged in.

## Goals

- Track all characters a player has logged in with (across all accounts)
- Provide a "Current Character" condition for macro activation
- Persist character tracking data across sessions
- Update character information (name, world) when it changes

## Implementation Plan

### 1. Create Character Tracking Data Models

**File**: `MacroMate/CharacterTracking/CharacterInfo.cs` (new)

Create a `CharacterInfo` record to store character information:
- `ulong ContentId` - Unique identifier for the character
- `string Name` - Character name
- `string WorldName` - Home world name
- `ulong? AccountId` - Account ID (if available, for informational purposes)
- `DateTime FirstSeen` - When character was first tracked
- `DateTime LastSeen` - Last login time

Create a `CharacterTrackingData` class to store the tracking database:
- `Dictionary<ulong, CharacterInfo>` - Keyed by ContentId
- Simple flat structure since MacroMate config is shared across all accounts

### 2. Update Configuration to Store Character Data

**File**: `MacroMate/Configuration.cs`

- Add `CharacterTrackingData` property to the Configuration class
- This will automatically serialize via Dalamud's plugin config system
- No need for custom XML serialization (unlike MacroConfig which uses its own serializer)

### 3. Create CharacterTrackingManager Service

**File**: `MacroMate/CharacterTracking/CharacterTrackingManager.cs` (new)

**Responsibilities**:
- Track characters on login
- Provide list of all tracked characters
- Provide current character info
- Update character information when name or world changes

**Event Subscriptions**:
- `IClientState.Login` event - track character on login
- `IFramework.Update` (first tick after login) - get character info

**Public Methods**:
- `GetAllCharacters()` - Returns list of all known characters (across all accounts)
- `GetCurrentCharacter()` - Returns current character info (or null if not logged in)

**Implementation Notes**:
- Account ID can be optionally tracked for informational purposes if available
- Store character info in Configuration and trigger save when tracking new characters
- Update existing character info (name, world) on each login to handle transfers/renames

### 4. Register CharacterTrackingManager in Env

**File**: `MacroMate/Env.cs`

**Changes**:
- Add static property: `public static CharacterTrackingManager CharacterTrackingManager { get; private set; } = null!;`
- Initialize in `Env.Initialize()` after Configuration is loaded:
  ```csharp
  CharacterTrackingManager = new CharacterTrackingManager();
  ```
- Dispose in `Env.Dispose()` if the manager implements IDisposable:
  ```csharp
  CharacterTrackingManager.Dispose();
  ```

### 5. Create CurrentCharacterCondition

**File**: `MacroMate/Conditions/CurrentCharacterCondition.cs` (new)

**Implementation Details**:
- Implement `IValueCondition` (discrete value, not numeric comparison)
- Store character's Content ID (not name, for stability across renames)
- `Current()` static method gets character from `Env.CharacterTrackingManager.GetCurrentCharacter()`
- `TopLevel()` returns all characters from `Env.CharacterTrackingManager.GetAllCharacters()`
- `ValueName` / `NarrowName` display character name + world (e.g., "Firstname Lastname@Phoenix")
- `SatisfiedBy()` compares Content IDs for equality
- Factory implements `IValueCondition.IFactory` with:
  - `ConditionName`: "Current Character"
  - `ExpressionName`: "Character"

### 6. Update CurrentConditions

**File**: `MacroMate/Conditions/CurrentConditions.cs`

**Changes**:
1. Add `CurrentCharacterCondition? currentCharacter` parameter to the record
2. Add `currentCharacter: null` to the `Empty` static property
3. Add null check and yield in `Enumerate()` method:
   ```csharp
   if (currentCharacter != null) { yield return currentCharacter; }
   ```
4. Add to `Query()` method:
   ```csharp
   currentCharacter: CurrentCharacterCondition.Current()
   ```

### 7. Register Condition in ICondition

**File**: `MacroMate/Conditions/ICondition.cs`

**Changes**:
- Add `CurrentCharacterCondition.Factory` to the `AllFactories` array
- This makes the condition available in the UI condition picker and expression parser

### 8. Add XML Serialization Support

**File**: `MacroMate/Serialization/V1/ConditionXML.cs`

**Changes**:
1. Add `[XmlInclude(typeof(CurrentCharacterConditionXML))]` attribute at the class level
2. Add switch case in `From()` method:
   ```csharp
   CurrentCharacterCondition cond => CurrentCharacterConditionXML.From(cond),
   ```
3. Create `CurrentCharacterConditionXML` class:
   ```csharp
   [XmlType("CurrentCharacterCondition")]
   public class CurrentCharacterConditionXML : ConditionXML {
       public required ulong ContentId { get; set; }

       public override ICondition ToReal() => new CurrentCharacterCondition(ContentId);

       public static CurrentCharacterConditionXML From(CurrentCharacterCondition cond) => new() {
           ContentId = cond.ContentId
       };
   }
   ```

### 9. Character Tracking on Login

**Character Tracking on Login**:
```csharp
private void OnLogin() {
    Env.Framework.RunOnTick(() => {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) return;

        var contentId = Env.ClientState.LocalContentId;
        var existingCharacter = GetCharacterByContentId(contentId);

        var characterInfo = new CharacterInfo(
            ContentId: contentId,
            Name: player.Name.TextValue,
            WorldName: player.HomeWorld.Value.Name.ToString(),
            AccountId: null, // Optional: Try to get Account ID if available
            FirstSeen: existingCharacter?.FirstSeen ?? DateTime.UtcNow,
            LastSeen: DateTime.UtcNow
        );

        TrackCharacter(characterInfo);
    });
}
```

**Note on Account ID**:
- Account ID tracking is optional and not required for functionality
- If easily accessible via Dalamud API, store it in `CharacterInfo.AccountId` for future use
- If not available, leave as null - functionality works without it
- MacroMate config is shared across accounts anyway, so filtering by account is not needed

### 10. Testing Plan

**Build Verification**:
- Build project and verify CharacterTrackingManager initializes without errors
- Verify Configuration serialization includes character tracking data

**Functionality Testing**:
- Login with multiple characters to populate tracking data
- Verify each character is tracked with correct name, world, and Content ID
- Verify condition appears in UI condition picker with correct name
- Test expressions like `Character == "Firstname Lastname@Phoenix"`
- Verify dropdown shows all tracked characters (from all accounts)

**Serialization Testing**:
- Create macro with character condition
- Save configuration
- Reload plugin
- Verify macro still has correct character condition
- Test with multiple character conditions

**Edge Cases**:
- Login before any characters tracked (should track first character)
- Character rename (Content ID should remain stable, name should update)
- World transfer (Content ID stable, world name should update)
- Deleted character (remains in list but harmless - no cleanup needed)

## Design Decisions

### Why Content ID instead of Character Name?

- Content ID is stable across renames and world transfers
- Names can be duplicated across worlds
- Content ID is the official unique identifier from SE

### Why Show All Characters (Not Filtered by Account)?

- MacroMate's configuration is shared across all accounts on the same machine
- Users may want to create macros specific to a character on a different account
- Simpler data structure (flat dictionary instead of nested)
- Consistent with how MacroMate's config works (global, not per-account)
- Account ID can still be tracked optionally for informational/future use

### Why Use Configuration instead of MacroConfig?

- Character tracking is plugin-level data, not macro configuration data
- Configuration auto-serializes via Dalamud's system (simpler)
- MacroConfig uses custom XML serialization for complex macro tree
- Separates concerns: MacroConfig = user macros, Configuration = plugin state

## Questions & Risks

### Open Questions

1. **Character Migration**: First-time users won't have tracked characters
   - Risk: Empty dropdown initially
   - Mitigation: Show helpful message "Login with characters to populate list"

2. **Character Deletion**: No automatic cleanup if SE deletes a character
   - Risk: Deleted characters remain in list
   - Impact: Minor UX issue only, not functional problem
   - Mitigation: Add manual "Remove Character" option in future

3. **Multi-Account Confusion**: Users with multiple accounts will see all characters
   - Risk: Might select wrong character from different account
   - Impact: Low - condition just won't match when logged into different account
   - Mitigation: Show account ID in character display if available (future enhancement)

### Technical Risks

1. **Race Condition on Login**: Player data may not be available immediately on login event
   - Mitigation: Use `Framework.RunOnTick()` to delay access until data available

2. **Performance**: Dictionary lookups should be fast, but verify no lag on login
   - Mitigation: Use efficient data structures (Dictionary with ulong keys)

3. **Content ID Stability**: Assume Content ID is stable across renames/transfers
   - Impact: If Content ID changes, character will be duplicated in list
   - Likelihood: Very low - Content ID is designed to be stable identifier

## Future Enhancements

- Manual character management UI (rename, remove, merge accounts)
- Export/import character tracking data
- Character statistics (first seen, last seen, login count)
- Multi-account support with account naming
- Character aliases for privacy
