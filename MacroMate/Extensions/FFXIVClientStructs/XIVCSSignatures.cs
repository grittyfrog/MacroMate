using Dalamud.Utility.Signatures;

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

    public XIVCSSignatures() {
        Env.GameInteropProvider.InitializeFromAttributes(this);
    }
}
