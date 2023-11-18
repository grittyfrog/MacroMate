using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.FFXIVClientStructs;

// TODO: Replace with https://github.com/aers/FFXIVClientStructs/pull/706 (once available in Dalamud)
[Agent(AgentId.Macro)]
[StructLayout(LayoutKind.Explicit, Size = 0xbf0)]
public unsafe partial struct AgentMacro {
    [FieldOffset(0x0)]
    public AgentInterface AgentInterface;

    [FieldOffset(0x6b8)]
    public uint SelectedMacroSet;

    [FieldOffset(0x6bc)]
    public uint SelectedMacroIndex;
}
