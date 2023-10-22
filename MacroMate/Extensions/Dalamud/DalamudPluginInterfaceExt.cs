using System.Linq;
using Dalamud.Plugin;

namespace MacroMate.Extensions.Dalamud;

public static class DalamudPluginInterfaceExt {
    /// <summary>
    /// Returns true if the plugin identified by internalName is installed and loaded.
    /// </summary>
    public static bool PluginIsLoaded(this DalamudPluginInterface self, string internalName) {
        return self.InstalledPlugins.Any(plugin => plugin.InternalName == internalName && plugin.IsLoaded);
    }

    public static bool MacroChainPluginIsLoaded(this DalamudPluginInterface self) => self.PluginIsLoaded("MacroChain");
}
