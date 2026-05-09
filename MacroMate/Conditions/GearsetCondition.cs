using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Conditions;

public record class GearsetCondition(
    int Index
) : IValueCondition {
    public string ValueName {
        get {
            unsafe {
                var entry = TryGetEntry(Index);
                if (entry == null) { return $"{Index + 1} - <empty>"; }
                var job = new ExcelId<ClassJob>(entry->ClassJob);
                return $"{Index + 1} - {entry->NameString} ({job.Name()})";
            }
        }
    }
    public string NarrowName => ValueName;

    public GearsetCondition() : this(0) {}

    public bool SatisfiedBy(ICondition other) {
        return this.Equals(other);
    }

    public static GearsetCondition? Current() {
        unsafe {
            var module = RaptureGearsetModule.Instance();
            if (module == null) { return null; }
            var index = module->CurrentGearsetIndex;
            return TryGetEntry(index) != null ? new GearsetCondition(index) : null;
        }
    }

    /// <summary>
    /// Returns a pointer to the gearset entry at the given index, or null if the
    /// index is invalid, the module is not ready, or the slot is empty.
    /// </summary>
    private static unsafe RaptureGearsetModule.GearsetEntry* TryGetEntry(int index) {
        if (index < 0) { return null; }
        var module = RaptureGearsetModule.Instance();
        if (module == null) { return null; }
        if (!module->IsValidGearset(index)) { return null; }
        return module->GetGearset(index);
    }

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Gearset";
        public string ExpressionName => "Gearset";

        public IValueCondition? Current() => GearsetCondition.Current();
        public IValueCondition Default() => new GearsetCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.gearset;

        public IEnumerable<IValueCondition> TopLevel() {
            var results = new List<IValueCondition>();
            unsafe {
                for (int i = 0; i < 100; i++) {
                    if (TryGetEntry(i) != null) {
                        results.Add(new GearsetCondition(i));
                    }
                }
            }
            return results;
        }
    }
}
