using Dalamud.IoC;
using Dalamud.Plugin;

namespace MacroMate;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Macro Mate";

    public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface) {
        // Only used when locally testing FFXIVClientStruct changes.
        // FFXIVClientStructs.Interop.Resolver.GetInstance.SetupSearchSpace();
        // FFXIVClientStructs.Interop.Resolver.GetInstance.Resolve();

        Env.Initialize(pluginInterface);
    }

    public void Dispose() {
        Env.Dispose();
    }
}
