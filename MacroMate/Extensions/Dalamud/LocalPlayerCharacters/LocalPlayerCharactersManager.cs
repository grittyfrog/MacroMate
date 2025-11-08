using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Extensions.Dalamud.LocalPlayerCharacters;

/// <summary>
/// Manages tracking of characters that have logged in on this installation.
/// Character data is stored in a separate MacroMateCharacters.xml file.
/// </summary>
public class LocalPlayerCharactersManager : IDisposable {
    private bool isPollingForPlayerData = false;

    public LocalPlayerCharactersManager() {
        Env.ClientState.Login += OnLogin;

        // Track current character if already logged in (deferred to next tick to ensure we're on main thread)
        Env.Framework.RunOnTick(() => {
            if (Env.ClientState.LocalPlayer != null && Env.ClientState.LocalContentId != 0) {
                TrackCurrentCharacter();
            }
        });
    }

    public void Dispose() {
        Env.ClientState.Login -= OnLogin;
        if (isPollingForPlayerData) {
            Env.Framework.Update -= PollForPlayerData;
            isPollingForPlayerData = false;
        }
    }

    private void OnLogin() {
        // Start polling for player data until available
        isPollingForPlayerData = true;
        Env.Framework.Update += PollForPlayerData;
    }

    private void PollForPlayerData(IFramework framework) {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) return;

        var contentId = Env.ClientState.LocalContentId;
        if (contentId == 0) return;

        // Player data is available, track the character
        TrackCurrentCharacter();

        // Stop polling
        Env.Framework.Update -= PollForPlayerData;
        isPollingForPlayerData = false;
    }

    private void TrackCurrentCharacter() {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) return;

        var contentId = Env.ClientState.LocalContentId;
        if (contentId == 0) return;

        var character = new LocalCharacterDataCache.Entry {
            ContentId = contentId,
            Name = player.Name.TextValue,
            World = new ExcelId<World>(player.HomeWorld.RowId)
        };

        Env.MacroMateCache.LocalCharacterData.TrackCharacter(character);
        Env.MacroMateCache.Save();
    }

    public LocalCharacterDataCache.Entry? GetCurrentCharacter() {
        var contentId = Env.ClientState.LocalContentId;
        if (contentId == 0) return null;

        return Env.MacroMateCache.LocalCharacterData.GetCharacter(contentId);
    }

    public IEnumerable<LocalCharacterDataCache.Entry> GetAllCharacters() {
        return Env.MacroMateCache.LocalCharacterData.GetAllCharacters();
    }

    public LocalCharacterDataCache.Entry? GetCharacter(ulong contentId) {
        return Env.MacroMateCache.LocalCharacterData.GetCharacter(contentId);
    }
}
