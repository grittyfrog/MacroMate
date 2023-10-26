using MacroMate.MacroTree;
using MacroMate.Commands;
using MacroMate.Extensions.Dalamud.PlayerLocation;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using MacroMate.Windows;
using System;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud.Macros;
using Dalamud.Plugin.Services;
using MacroMate.Extensions.Dalamud.Font;
using MacroMate.Serialization;
using Dalamud.Game;

namespace MacroMate;

/**
 * The Environment that all classes have access to.
 */
public class Env {
    public static void Initialize(DalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Env>();

        FontManager = new FontManager();
        WindowSystem = new("MacroMate");
        Random = new Random();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        PlayerLocationManager = new PlayerLocationManager();
        ConditionManager = new ConditionManager();
        VanillaMacroManager = new VanillaMacroManager();
        PluginCommandManager = new PluginCommandManager();
        PluginWindowManager = new PluginWindowManager();
        SaveManager = new SaveManager();
        MacroConfig = new MacroConfig();

        MacroMate = new MacroMate();
    }

    public static void Dispose() {
        ConditionManager.Dispose();
        PluginWindowManager.Dispose();
        PluginCommandManager.Dispose();
        FontManager.Dispose();
        VanillaMacroManager.Dispose();
    }

    /// ===
    /// Dalamud Injections
    /// ===

    [PluginService]
    [RequiredVersion("1.0")]
    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static ITargetManager TargetManager { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IFramework Framework { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static ISigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IPluginLog PluginLog { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    public static WindowSystem WindowSystem { get; private set; } = null!;

    public static Random Random { get; private set; } = null!;

    public static FontManager FontManager { get; private set; } = null!;

    /// ===
    /// MacroMate Injections
    /// ===
    public static MacroMate MacroMate { get; private set; } = null!;

    public static Configuration Configuration { get; private set; } = null!;

    public static ConditionManager ConditionManager { get; private set; } = null!;

    public static VanillaMacroManager VanillaMacroManager { get; private set; } = null!;

    public static PlayerLocationManager PlayerLocationManager { get; private set; } = null!;

    public static MacroConfig MacroConfig { get; private set; } = null!;

    public static PluginCommandManager PluginCommandManager { get; private set; } = null!;

    public static PluginWindowManager PluginWindowManager { get; private set; } = null!;

    public static SaveManager SaveManager { get; private set; } = null!;
}
