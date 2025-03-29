using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace MacroMate.Conditions;

public record class HUDLayoutCondition(
    int HudLayout
) : IValueCondition {
    public string ValueName => (HudLayout + 1).ToString();
    public string NarrowName => (HudLayout + 1).ToString();

    public HUDLayoutCondition() : this(0) {}

    public bool SatisfiedBy(ICondition other) {
        return this.Equals(other);
    }

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    public static HUDLayoutCondition? Current() {
        int hudLayout;
        unsafe {
            var addonConfig = Framework.Instance()->GetUIModule()->GetAddonConfig();
            hudLayout = addonConfig->ModuleData->CurrentHudLayout;
        }
        return new HUDLayoutCondition(hudLayout);
    }

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "HUD Layout";
        public string ExpressionName => "HUD Layout";

        public IValueCondition? Current() => HUDLayoutCondition.Current();
        public IValueCondition Default() => new HUDLayoutCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) {
            return conditions.hudLayoutCondition;
        }

        public IEnumerable<IValueCondition> TopLevel() {
            return new[] { 0, 1, 2, 3 }.Select(hl => new HUDLayoutCondition(hl));
        }
    }
}
