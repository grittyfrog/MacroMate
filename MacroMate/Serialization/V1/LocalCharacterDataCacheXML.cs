using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamud.LocalPlayerCharacters;

namespace MacroMate.Serialization.V1;

public class LocalCharacterDataCacheXML {
    public required List<LocalCharacterDataCacheEntryXML> Characters { get; set; }

    public LocalCharacterDataCache ToReal() {
        var characters = Characters.ToDictionary(
            c => c.ContentId,
            c => new LocalCharacterDataCache.Entry {
                ContentId = c.ContentId,
                Name = c.Name,
                World = c.World.ToReal<World>()
            }
        );
        return new LocalCharacterDataCache { Characters = characters };
    }

    public static LocalCharacterDataCacheXML From(LocalCharacterDataCache cache) {
        var charactersXML = cache.Characters.Values
            .Select(c =>
                new LocalCharacterDataCacheEntryXML {
                    ContentId = c.ContentId,
                    Name = c.Name,
                    World = new ExcelIdXML(c.World)
                }
            );
        return new LocalCharacterDataCacheXML { Characters = charactersXML.ToList() };
    }
}

public class LocalCharacterDataCacheEntryXML {
    public required ulong ContentId { get; set; }
    public required string Name { get; set; }
    public required ExcelIdXML World { get; set; }
}
