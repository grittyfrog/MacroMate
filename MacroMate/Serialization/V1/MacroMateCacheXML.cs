using System.Xml.Serialization;
using MacroMate.Cache;

namespace MacroMate.Serialization.V1;

[XmlRoot("MacroMateCache")]
public class MacroMateCacheXML {
    [XmlAttribute]
    public int CacheVersion { get; set; } = 1;

    public required LocalCharacterDataCacheXML? LocalCharacterData { get; set; }

    public MacroMateCache ToReal() => new MacroMateCache {
        LocalCharacterData = LocalCharacterData?.ToReal() ?? new()
    };

    public static MacroMateCacheXML From(MacroMateCache cache) => new() {
        LocalCharacterData = LocalCharacterDataCacheXML.From(cache.LocalCharacterData)
    };
}
