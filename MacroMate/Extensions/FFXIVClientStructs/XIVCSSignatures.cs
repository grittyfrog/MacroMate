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
    /// Show the Macro UI and select the given Set and Index
    /// </summary>
    /// <remarks>
    /// This is the same behaviour as "Right click on Macro" > "Edit Macro"
    /// </remarks>
    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 49 ?? 0F B7 9E ")]
    public AgentMacroEditMacroInUIDelegate? AgentMacroEditMacroInUI { get; init; } = null;
    public delegate void AgentMacroEditMacroInUIDelegate(
        AgentMacro* self,
        uint selectedMacroSet,
        uint selectedMacroIndex
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
