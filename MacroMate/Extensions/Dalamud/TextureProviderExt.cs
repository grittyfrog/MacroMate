using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Plugin.Services;
using MacroMate.Extensions.Dalamud.Macros;

namespace MacroMate.Extensions.Dalamud;

public static class TextureProviderExt {
    /// <summary>
    /// Get the texture for an icon if possible, if the icon isn't valid use
    /// a fallback icon.
    /// </summary>
    public static ISharedImmediateTexture GetMacroIcon(
        this ITextureProvider self,
        uint iconId
    ) => self.GetIconOrFallback(iconId, VanillaMacro.DefaultIconId);

    /// <summary>
    /// Get the texture for an icon if possible, if the icon isn't valid use
    /// a fallback icon.
    /// </summary>
    public static ISharedImmediateTexture GetIconOrFallback(
        this ITextureProvider self,
        uint iconId,
        uint fallback
    ) {
        if (self.TryGetFromGameIcon(iconId, out var iconTexture)) {
            return iconTexture;
        } else {
            return self.GetFromGameIcon(fallback);
        }
    }
}
