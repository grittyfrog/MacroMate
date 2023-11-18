using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MacroMate.Extensions.FFXIVClientStructs;

/// <summary>
/// Signatures that should eventually move into client structs
/// </summary>
public unsafe class XIVCSSignatures {
    // TODO: Replace with https://github.com/aers/FFXIVClientStructs/pull/706 (once available in Dalamud)
    /// <summary>
    /// Show the Macro UI and select the given Set and Index
    /// </summary>
    /// <remarks>
    /// This is the same behaviour as "Right click on Macro" > "Edit Macro"
    /// </remarks>
    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 49 ?? 0F B7 9E ")]
    public AgentMacroOpenMacroDelegate? AgentMacroOpenMacro { get; init; } = null;
    public delegate void AgentMacroOpenMacroDelegate(
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
