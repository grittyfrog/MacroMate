using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace MacroMate.Extensions.Dalamud.Str;

public static partial class SeStringEx {
    /// Same as [text] but without the surrounding markers
    public static string RawText(this AutoTranslatePayload self) {
        var text = self.Text;
        return text.Substring(1, text.Length - 2).Trim();
    }
}
