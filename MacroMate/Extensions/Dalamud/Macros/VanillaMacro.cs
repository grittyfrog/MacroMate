using System;

namespace MacroMate.Extensions.Dalamud.Macros;

public record class VanillaMacro(
    uint IconId,
    string Title,
    uint LineCount,
    Lazy<string> Lines,

    /// MacroIcon, exclusive of /micon or similar. Oddly, off by one from Lumina's table.
    uint IconRowId = 1  // Must be set to non-zero for the macro to be "active"
) {
    public static uint MaxLines = 15;
    public static uint MaxLineLength = 181;
    public static uint MaxNameLength = 20;

    public static uint DefaultIconId = 66001; // Default "M" Icon
    public static uint InactiveIconId = 60861; // A red cross

    public static VanillaMacro Empty => new VanillaMacro(
        IconId: DefaultIconId,
        Title: "",
        LineCount: 0,
        Lines: new(() => "")
    );

    /// Macros seem to "exist" if they have any title or lines.
    public bool IsDefined() {
        return Title != "" || LineCount > 0 && IconRowId > 0;
    }
}
