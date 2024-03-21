using System;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace MacroMate.Extensions.Dalamud.Font;

public class FontManager : IDisposable {
    public IFontHandle Axis18 = Env.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
        new GameFontStyle(GameFontFamilyAndSize.Axis18)
    );

    public void Dispose() {
        Axis18.Dispose();
    }
}
