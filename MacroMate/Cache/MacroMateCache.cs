using System.IO;
using MacroMate.Extensions.Dalamud.LocalPlayerCharacters;
using MacroMate.Serialization.V1;
using MacroMate.Subscription;

namespace MacroMate.Cache;

public class MacroMateCache {
    public required LocalCharacterDataCache LocalCharacterData { get; set; }
    public required SubscriptionUrlCache SubscriptionUrlCache { get; set; }

    private static FileInfo CacheDataFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMateCache.xml"));
    }

    private static FileInfo CacheDataSaveAttemptFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMateCache.saveAttempt.xml"));
    }

    public void Save() {
        // Write to an empty file then copy it over the existing one.
        //
        // This helps prevent issues when the save fails, since we won't
        // corrupt the users existing save file.
        MacroMateCacheV1.Write(this, CacheDataSaveAttemptFile);
        File.Move(CacheDataSaveAttemptFile.FullName, CacheDataFile.FullName, overwrite: true);
    }

    public static MacroMateCache Load() {
        if (!CacheDataFile.Exists) {
            return new MacroMateCache {
                LocalCharacterData = new(),
                SubscriptionUrlCache = new()
            };
        }
        return MacroMateCacheV1.Read(CacheDataFile);
    }
}
