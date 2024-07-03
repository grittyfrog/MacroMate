namespace MacroMate.Extensions.FFXIVClientStructs;

/// <summary>
/// Signatures that should eventually move into client structs
/// </summary>
/// <remarks>
/// Example usage:
///
/// [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 49 ?? 0F B7 9E ")]
/// public AgentMacroOpenMacroDelegate? AgentMacroOpenMacro { get; init; } = null;
/// public delegate void AgentMacroOpenMacroDelegate(
///   AgentMacro* self,
///   uint selectedMacroSet,
///   uint selectedMacroIndex
/// );
///
/// </remarks>
public unsafe class XIVCSSignatures {
    public XIVCSSignatures() {
        // Env.GameInteropProvider.InitializeFromAttributes(this);
    }
}
