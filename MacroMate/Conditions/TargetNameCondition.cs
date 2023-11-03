using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.ClientState.Objects.Enums;
using MacroMate.Extensions.Lumina;

namespace MacroMate.Conditions;

public record class TargetNameCondition(
    ObjectKind TargetKind,
    uint TargetId
) : ICondition {
    string ICondition.ValueName => DisplayName;
    string ICondition.NarrowName => DisplayName;

    bool ICondition.SatisfiedBy(ICondition other) {
        if (other is TargetNameCondition otherTarget) {
            // We check if names are equal by their string representation instead of
            // their ID. We do this because ENpcResident's can have different IDs but
            // the same name and we want to treat them as equal.
            var thisName = this.Name;
            var otherName = otherTarget.Name;
            if (thisName == null || otherName == null) { return false; }
            return this.TargetKind == otherTarget.TargetKind && thisName.Equals(otherName);
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

        return new TargetNameCondition(target.ObjectKind, target.DataId);
    }

    public string Name => TargetKind switch {
        ObjectKind.None => null,
        ObjectKind.Player => null, // Not supported
        ObjectKind.BattleNpc =>
            Env.DataManager.GetExcelSheet<BNpcName>()?.GetRow(TargetId)?.Singular?.Text(),
        ObjectKind.EventNpc =>
            Env.DataManager.GetExcelSheet<ENpcResident>()?.GetRow(TargetId)?.Singular?.Text(),
        ObjectKind.Treasure => null,
        ObjectKind.Aetheryte => null,
        ObjectKind.GatheringPoint => null,
        ObjectKind.EventObj => Env.DataManager.GetExcelSheet<EObjName>()?.GetRow(TargetId)?.Singular?.Text(),
        ObjectKind.MountType => null,
        ObjectKind.Companion => Env.DataManager.GetExcelSheet<Companion>()?.GetRow(TargetId)?.Singular?.Text(), // Minion
        ObjectKind.Retainer => null,
        ObjectKind.Area => null,
        ObjectKind.Housing =>
            Env.DataManager.GetExcelSheet<HousingFurniture>()
              ?.GetRow(TargetId)?.Item?.Value?.Name?.Text(),
        ObjectKind.Cutscene => null,
        ObjectKind.CardStand => null,
        ObjectKind.Ornament => null,
        _ => null
    } ?? "<unknown>";

    public string DisplayName => $"{Name} ({TargetId} - {TargetKind})";

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Target";
        public ICondition? Current() => TargetNameCondition.Current();
        public ICondition Default() => new TargetNameCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.targetNpc;

        public IEnumerable<ICondition> TopLevel() {
            var bnpcNames = Env.DataManager.GetExcelSheet<BNpcName>()!
                .Where(npcName => npcName.Singular != "")
                .Select(npcName =>
                    new TargetNameCondition(ObjectKind.BattleNpc, npcName.RowId)
                )
                .AsParallel();

            // We match enpcNames on their string names, since the same name is repeated multiple times
            // for identical NPCs. But we still use the IDs under the hood to allow macros to work cross-language.
            var enpcNames = Env.DataManager.GetExcelSheet<ENpcResident>()!
                .Where(enpc => enpc.Singular != "")
                .DistinctBy(enpc => enpc.Singular.Text())
                .Select(enpc => new TargetNameCondition(ObjectKind.EventNpc, enpc.RowId))
                .AsParallel();

            // Same as enpc -- identical names, different ids
            var eobjNames = Env.DataManager.GetExcelSheet<EObjName>()!
                .Where(eobj => eobj.Singular != "")
                .DistinctBy(eobj => eobj.Singular.Text())
                .Select(eobj => new TargetNameCondition(ObjectKind.EventObj, eobj.RowId))
                .AsParallel();

            var companionNames = Env.DataManager.GetExcelSheet<Companion>()!
                .Where(c => c.Singular != "")
                .Select(c => new TargetNameCondition(ObjectKind.Companion, c.RowId))
                .AsParallel();

            var housingNames = Env.DataManager.GetExcelSheet<HousingFurniture>()!
                .Select(furniture => new TargetNameCondition(ObjectKind.Housing, furniture.RowId))
                .AsParallel();

            return bnpcNames
                .Concat(enpcNames)
                .Concat(eobjNames)
                .Concat(companionNames)
                .Concat(housingNames);
        }
    }
}
