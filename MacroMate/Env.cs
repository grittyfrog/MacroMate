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
using MacroMate.Extensions.FFXIVClientStructs;

namespace MacroMate;

/**
 * The Environment that all classes have access to.
 */
public class Env {
    public static void Initialize(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Env>();

        FontManager = new FontManager();
        WindowSystem = new("MacroMate");
        Random = new Random();
        XIVCSSignatures = new XIVCSSignatures();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        PlayerLocationManager = new PlayerLocationManager();
        ConditionManager = new ConditionManager();
        VanillaMacroManager = new VanillaMacroManager();
        SaveManager = new SaveManager();
        MacroConfig = new MacroConfig();
        MacroMate = new MacroMate();

        PluginCommandManager = new PluginCommandManager();
        PluginWindowManager = new PluginWindowManager();
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
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    public static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    public static ITargetManager TargetManager { get; private set; } = null!;

    [PluginService]
    public static IFramework Framework { get; private set; } = null!;

    [PluginService]
    public static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService]
    public static ISigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    public static IPluginLog PluginLog { get; private set; } = null!;

    [PluginService]
    public static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    [PluginService]
    public static Dalamud.Plugin.Services.ICondition PlayerCondition { get; private set; } = null!;

    public static WindowSystem WindowSystem { get; private set; } = null!;

    public static Random Random { get; private set; } = null!;

    public static FontManager FontManager { get; private set; } = null!;

    public static XIVCSSignatures XIVCSSignatures { get; private set; } = null!;

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
