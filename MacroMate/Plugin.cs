using Dalamud.IoC;
using Dalamud.Plugin;

namespace MacroMate;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Macro Mate";

    public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface) {
        Env.Initialize(pluginInterface);
    }

    public void Dispose() {
        Env.Dispose();
    }
}
