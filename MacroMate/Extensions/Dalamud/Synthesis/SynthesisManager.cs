using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.Dalamud.Synthesis;

/**
 * Manages information about the player's active craft
 *
 * See also: https://github.com/WorkingRobot/Craftimizer/blob/main/Craftimizer%2FUtils%2FSynthesisValues.cs
 */
public unsafe class SynthesisManager {
    public uint? CurrentCraftDifficulty { get; set; } = null; // aka max progress
    public uint? CurrentCraftMaxDurability { get; set; } = null;
    public uint? CurrentCraftMaxQuality { get; set; } = null;

    public delegate void OnSynthesisConditionsChanged();
    public event OnSynthesisConditionsChanged? SynthesisConditionChange;

    public SynthesisManager() {
        Env.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Synthesis", OnSynthesisSetup);
        Env.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Synthesis", OnSynthesisFinalize);
    }

    private void OnSynthesisSetup(AddonEvent type, AddonArgs args) {
        var synth = (AddonSynthesis*)args.Addon;
        var atkValues = new ReadOnlySpan<AtkValue>(synth->AtkUnitBase.AtkValues, synth->AtkUnitBase.AtkValuesCount);
        CurrentCraftDifficulty = atkValues[6].UInt;
        CurrentCraftMaxDurability = atkValues[8].UInt;
        CurrentCraftMaxQuality = atkValues[17].UInt;

        if (SynthesisConditionChange != null) { SynthesisConditionChange(); }
    }

    private void OnSynthesisFinalize(AddonEvent type, AddonArgs args) {
        CurrentCraftDifficulty = null;
        CurrentCraftMaxDurability = null;
        CurrentCraftMaxQuality = null;

        if (SynthesisConditionChange != null) { SynthesisConditionChange(); }
    }
}
