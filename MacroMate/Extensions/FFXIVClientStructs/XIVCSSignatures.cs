using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MacroMate.Extensions.FFXIVClientStructs;

/// <summary>
/// Signatures that should eventually move into client structs
/// </summary>
public unsafe class XIVCSSignatures {
    [Signature("40 53 48 83 EC ?? 48 83 B9 ?? ?? ?? ?? ?? 48 8B D9 0F 29 74 24 ?? 75")]
    public UpdateDelegate? AgentMacroUpdate { get; init; } = null;
    public delegate void UpdateDelegate(AgentMacro* self);

    /// <summary>
    /// Applies changes to SelectedMacroSet and SelectedMacroSlot in AgentMacro.
    /// </summary>
    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 4D 8B C6 49 8B D7 48 8B CB E8 ?? ?? ?? ?? 4C 8B 05 ")]
    public AgentMacroReloadSelectionDelegate? AgentMacroReloadSelection { get; init; } = null;
    public delegate void AgentMacroReloadSelectionDelegate(
        AgentMacro* self,
        uint selectedMacroIndex,
        bool unkBool
    );

    // TODO: Replace with https://github.com/aers/FFXIVClientStructs/pull/660
    [Signature("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? 39 87")]
    public RaptureHotbarModuleReloadMacroSlotsDelegate? RaptureHotbarModuleReloadMacroSlots { get; init; } = null;
    public delegate void RaptureHotbarModuleReloadMacroSlotsDelegate(
        RaptureHotbarModule* raptureHotbarModule,
        byte macroSet,
        byte macroIndex
    );

    public XIVCSSignatures() {
        Env.GameInteropProvider.InitializeFromAttributes(this);
    }
}
