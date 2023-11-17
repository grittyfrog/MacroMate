using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.FFXIVClientStructs;

[Agent(AgentId.Macro)]
[StructLayout(LayoutKind.Explicit, Size = 0xbf0)]
public unsafe partial struct AgentMacro {
    [FieldOffset(0x0)]
    public AgentInterface AgentInterface;

    [FieldOffset(0x6b8)]
    public uint SelectedMacroSet;

    [FieldOffset(0x6bc)]
    public uint SelectedMacroIndex;

    public static string UpdateSignature = "40 53 48 83 EC ?? 48 83 B9 ?? ?? ?? ?? ?? 48 8B D9 0F 29 74 24 ?? 75";
    public delegate void UpdateDelegate(AgentMacro* self);
}
