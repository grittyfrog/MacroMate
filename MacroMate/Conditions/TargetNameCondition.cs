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
    string ICondition.ValueName {
        get {
            if (TargetId == 0) { return TargetKind.ToString(); }
            return  $"{Name} ({TargetId} - {TargetKind})";
        }
    }
    string ICondition.NarrowName {
        get {
            if (TargetId == 0) { return TargetKind.ToString(); }
            return $"{Name} ({TargetId})";
        }
    }

    bool ICondition.SatisfiedBy(ICondition other) {
        if (other is TargetNameCondition otherTarget) {
            // If TargetId is 0, assume it is satisfied
            if (TargetId == 0) { return this.TargetKind.Equals(otherTarget.TargetKind); }

            // We check if names are equal by their string representation instead of
            // their ID. We do this because ENpcResident's can have different IDs but
            // the same name and we want to treat them as equal.
            var thisName = this.Name;
            var otherName = otherTarget.Name;
            if (thisName == "<unknown>" || otherName == "<unknown>") { return false; }
            return this.TargetKind == otherTarget.TargetKind
                && thisName.ToLowerInvariant().Equals(otherName.ToLowerInvariant());
        }

        return false;
    }

    /// Default: ruins runner
    public TargetNameCondition() : this(ObjectKind.BattleNpc, 2) {}

    public static TargetNameCondition? Current() {
        var target = Env.TargetManager.Target;
        if (target == null) { return null; }

        // For BNpc's we need to use BNpcName directly since the
        // regular sheet doesn't have a name or link to BNpcName
        if (target.ObjectKind == ObjectKind.BattleNpc) {
            if (target is Character targetCharacter) {
                var targetNameId = targetCharacter.NameId;
                if (targetNameId == 0) { return null; }

            return new TargetNameCondition(ObjectKind.BattleNpc, targetNameId);
            }
        }

        // For everything else we assume their usual Data sheet has their name.

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
            var kinds = new List<ObjectKind>() {
                ObjectKind.BattleNpc,
                ObjectKind.EventNpc,
                ObjectKind.EventObj,
                ObjectKind.Companion,
                ObjectKind.Housing
            };

            return kinds.Select(kind => new TargetNameCondition(kind, 0));
        }

        public IEnumerable<ICondition> Narrow(ICondition search) {
            var targetCondition = search as TargetNameCondition;
            if (targetCondition == null) { return new List<ICondition>(); }

            // If we already have a target we can't narrow any further
            if (targetCondition.TargetId != 0) {  return new List<ICondition>(); }

            if (targetCondition.TargetKind == ObjectKind.BattleNpc) {
                return Env.DataManager.GetExcelSheet<BNpcName>()!
                    .Where(npcName => npcName.Singular != "")
                    .Select(npcName =>
                        new TargetNameCondition(ObjectKind.BattleNpc, npcName.RowId)
                    );
            }

            if (targetCondition.TargetKind == ObjectKind.EventNpc) {
                // We match enpcNames on their string names, since the same name is repeated multiple times
                // for identical NPCs. But we still use the IDs under the hood to allow macros to work cross-language.
                return Env.DataManager.GetExcelSheet<ENpcResident>()!
                    .Where(enpc => enpc.Singular != "")
                    .DistinctBy(enpc => enpc.Singular.Text())
                    .Select(enpc => new TargetNameCondition(ObjectKind.EventNpc, enpc.RowId));
            }

            if (targetCondition.TargetKind == ObjectKind.EventObj) {
                // Same as enpc -- identical names, different ids
                return Env.DataManager.GetExcelSheet<EObjName>()!
                    .Where(eobj => eobj.Singular != "")
                    .DistinctBy(eobj => eobj.Singular.Text())
                    .Select(eobj => new TargetNameCondition(ObjectKind.EventObj, eobj.RowId));
            }

            if (targetCondition.TargetKind == ObjectKind.Companion) {
                return Env.DataManager.GetExcelSheet<Companion>()!
                    .Where(c => c.Singular != "")
                    .Select(c => new TargetNameCondition(ObjectKind.Companion, c.RowId));
            }

            if (targetCondition.TargetKind == ObjectKind.Housing) {
                return Env.DataManager.GetExcelSheet<HousingFurniture>()!
                    .Select(furniture => new TargetNameCondition(ObjectKind.Housing, furniture.RowId));
            }

            return new List<ICondition>();
        }
    }
}
