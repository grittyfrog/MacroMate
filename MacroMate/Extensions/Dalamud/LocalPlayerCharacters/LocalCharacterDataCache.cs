using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Extensions.Dalamud.LocalPlayerCharacters;

/// <summary>
/// Cache of character data for all characters that have logged in on this installation.
/// Stored separately from MacroConfig in MacroMateCharacters.xml.
/// </summary>
public class LocalCharacterDataCache {
    public class Entry {
        public required ulong ContentId { get; set; }
        public required string Name { get; set; }
        public required ExcelId<World> World { get; set; }
    }

    public Dictionary<ulong, Entry> Characters = new();

    public void TrackCharacter(Entry character) {
        Characters[character.ContentId] = character;
    }

    public Entry? GetCharacter(ulong contentId) {
        return Characters.TryGetValue(contentId, out var character) ? character : null;
    }

    public IEnumerable<Entry> GetAllCharacters() {
        return Characters.Values.OrderBy(c => c.Name);
    }
}
