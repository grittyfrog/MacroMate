using System;
using Dalamud.Interface.GameFonts;

namespace MacroMate.Extensions.Dalamud.Font;

public class FontManager : IDisposable {
    public GameFontHandle Axis18 = Env.PluginInterface.UiBuilder.GetGameFontHandle(
        new GameFontStyle(GameFontFamilyAndSize.Axis18)
    );

    public void Dispose() {
        Axis18.Dispose();
    }
}
