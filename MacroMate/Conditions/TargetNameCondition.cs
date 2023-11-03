using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;
using OneOf;
using Dalamud.Game.ClientState.Objects.Enums;

namespace MacroMate.Conditions;

public record class TargetNameCondition(
    OneOf<ExcelId<BNpcName>, ExcelId<ENpcResident>, string> targetName
) : ICondition {
    string DisplayName => targetName.Match(
        bNpc => bNpc.DisplayName(),
        eNpc => eNpc.DisplayName(),
        customName => $"{customName} (Custom)"
    );
    string ICondition.ValueName => DisplayName;
    string ICondition.NarrowName => DisplayName;

    string? Name => targetName.Match(
        bNpc => bNpc.Name(),
        eNpc => eNpc.Name(),
        customName => customName
    );
    bool ICondition.SatisfiedBy(ICondition other) {
        if (other is TargetNameCondition otherTarget) {
            // We check if names are equal by their string representation instead of
            // their ID. We do this because ENpcResident's can have different IDs but
            // the same name and we want to treat them as equal.
            var thisName = this.Name;
            var otherName = otherTarget.Name;
            if (thisName == null || otherName == null) { return false; }
            return thisName.Equals(otherName);
        }

        return false;
    }

    /// Default: ruins runner
    public TargetNameCondition() : this(new ExcelId<BNpcName>(2)) {}

    public static TargetNameCondition? Current() {
        var target = Env.TargetManager.Target;
        if (target == null) { return null; }

        if (target is Character targetCharacter) {
            var targetNameId = targetCharacter.NameId;
            if (targetNameId == 0) { return null; }

            if (target.ObjectKind == ObjectKind.BattleNpc) {
                return new TargetNameCondition(new ExcelId<BNpcName>(targetNameId));
            }

            if (target.ObjectKind == ObjectKind.EventNpc) {
                return new TargetNameCondition(new ExcelId<ENpcResident>(targetNameId));
            }
        }

        var name = target.Name.ToString();
        return new TargetNameCondition(name);
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Target";
        public ICondition? Current() => TargetNameCondition.Current();
        public ICondition Default() => new TargetNameCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.targetNpc;

        public List<ICondition> TopLevel() {
            var bnpcNames = Env.DataManager.GetExcelSheet<BNpcName>()!
                .Where(npcName => npcName.Singular != "")
                .Select(npcName =>
                    new TargetNameCondition(new ExcelId<BNpcName>(npcName.RowId)) as ICondition
                );

            // We match enpcNames on their string names, since the same name is repeated multiple times
            // for identical NPCs. But we still use the IDs under the hood to allow macros to work cross-language.
            var enpcNames = Env.DataManager.GetExcelSheet<ENpcResident>()!
                .Where(enpc => enpc.Singular != "")
                .DistinctBy(enpc => enpc.Singular)
                .Select(enpc => new TargetNameCondition(new ExcelId<ENpcResident>(enpc.RowId)) as ICondition);

            return bnpcNames.Concat(enpcNames).ToList();
        }
    }
}
