using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.ClientState.Objects.Enums;

namespace MacroMate.Conditions;

public record class TargetNameCondition(
    ObjectKind TargetKind,
    uint TargetId
) : ICondition {
    string DisplayName => TargetExcelId()?.DisplayName() ?? "<unknown>";
    string ICondition.ValueName => DisplayName;
    string ICondition.NarrowName => DisplayName;

    string? Name => TargetExcelId()?.Name();

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
    public TargetNameCondition() : this(ObjectKind.BattleNpc, 2) {}

    public static TargetNameCondition? Current() {
        var target = Env.TargetManager.Target;
        if (target == null) { return null; }

        if (target is Character targetCharacter) {
            var targetNameId = targetCharacter.NameId;
            if (targetNameId == 0) { return null; }

            return new TargetNameCondition(target.ObjectKind, targetNameId);
        }

        return null;
    }

    private ExcelId? TargetExcelId() {
        return TargetKind switch {
            ObjectKind.None => null,
            ObjectKind.Player => null, // Not supported
            ObjectKind.BattleNpc => new ExcelId<BNpcName>(TargetId),
            ObjectKind.EventNpc => new ExcelId<ENpcResident>(TargetId),
            ObjectKind.Treasure => null,
            ObjectKind.Aetheryte => null,
            ObjectKind.GatheringPoint => null,
            ObjectKind.EventObj => null,
            ObjectKind.MountType => null,
            ObjectKind.Companion => null, // Minion
            ObjectKind.Retainer => null,
            ObjectKind.Area => null,
            ObjectKind.Housing => null,
            ObjectKind.Cutscene => null,
            ObjectKind.CardStand => null,
            ObjectKind.Ornament => null,
            _ => null
        };
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
                    new TargetNameCondition(ObjectKind.BattleNpc, npcName.RowId) as ICondition
                );

            // We match enpcNames on their string names, since the same name is repeated multiple times
            // for identical NPCs. But we still use the IDs under the hood to allow macros to work cross-language.
            var enpcNames = Env.DataManager.GetExcelSheet<ENpcResident>()!
                .Where(enpc => enpc.Singular != "")
                .ToList()
                .DistinctBy(enpc => enpc.Singular)
                .Select(enpc => new TargetNameCondition(ObjectKind.EventNpc, enpc.RowId) as ICondition);

            return bnpcNames.Concat(enpcNames).ToList();
        }
    }
}
