using System.Runtime.InteropServices;

namespace MacroMate.Extensions.Dalamud.PlayerLocation;

[StructLayout(LayoutKind.Explicit, Size = 76)]
public readonly struct TerritoryInfoStruct {
    [FieldOffset(8)] private readonly int InSanctuary;
    [FieldOffset(16)] public readonly uint RegionID;
    [FieldOffset(20)] public readonly uint SubAreaID;

    public bool IsInSanctuary => InSanctuary == 1;
}
