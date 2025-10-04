using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;
using MacroMate.Extensions.Lumina;
using System;

namespace MacroMate.Conditions;

public record class TargetNameCondition(
    ObjectKind TargetKind,
    uint TargetId
) : IValueCondition {
    private enum ObjectKindSupport {
        SUPPORTED,

        /**
         * <summary>
         * We support targeting this object, but not specific types of this object.
         *
         * For example, you can target any "Treasure", but not a specific "Treasure Coffer"
         * </summary>
         */
        KIND_ONLY,

        UNSUPPORTED
    }

    /**
     * Defines support levels for targeting different object kinds
     */
    private static Dictionary<ObjectKind, ObjectKindSupport> ObjectKindSupportLevel = new() {
        { ObjectKind.None,           ObjectKindSupport.UNSUPPORTED },
        { ObjectKind.Player,         ObjectKindSupport.KIND_ONLY   },
        { ObjectKind.BattleNpc,      ObjectKindSupport.SUPPORTED   },
        { ObjectKind.EventNpc,       ObjectKindSupport.SUPPORTED   },
        { ObjectKind.Treasure,       ObjectKindSupport.KIND_ONLY   }, // No table
        { ObjectKind.Aetheryte,      ObjectKindSupport.KIND_ONLY   }, // Too many spoilers
        { ObjectKind.GatheringPoint, ObjectKindSupport.KIND_ONLY   }, // No useful table
        { ObjectKind.EventObj,       ObjectKindSupport.SUPPORTED   },
        { ObjectKind.MountType,      ObjectKindSupport.UNSUPPORTED },
        { ObjectKind.Companion,      ObjectKindSupport.SUPPORTED   }, // Minion
        { ObjectKind.Retainer,       ObjectKindSupport.UNSUPPORTED },
        { ObjectKind.Area,           ObjectKindSupport.UNSUPPORTED },
        { ObjectKind.Housing,        ObjectKindSupport.SUPPORTED   },
        { ObjectKind.Cutscene,       ObjectKindSupport.UNSUPPORTED },
        { ObjectKind.CardStand,      ObjectKindSupport.UNSUPPORTED }, // Island Sanctuary gathering
        { ObjectKind.Ornament,       ObjectKindSupport.UNSUPPORTED }
    };

    public string ValueName {
        get {
            if (TargetId == 0) { return TargetKind.ToString(); }
            return  $"{Name} ({TargetId} - {TargetKind})";
        }
    }
    public string NarrowName {
        get {
            if (TargetId == 0) { return TargetKind.ToString(); }
            return $"{Name} ({TargetId})";
        }
    }

    public bool SatisfiedBy(ICondition other) {
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

        var objectKindSupport = ObjectKindSupportLevel
            .GetValueOrDefault(target.ObjectKind); // Default is Unsupported

        if (objectKindSupport == ObjectKindSupport.UNSUPPORTED) { return null; }
        if (objectKindSupport == ObjectKindSupport.KIND_ONLY) {
            return new TargetNameCondition(target.ObjectKind, 0);
        }

        // For BNpc's we need to use BNpcName directly since the
        // regular sheet doesn't have a name or link to BNpcName
        if (target.ObjectKind == ObjectKind.BattleNpc || target.ObjectKind == ObjectKind.Treasure) {
            if (target is ICharacter targetCharacter) {
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
            Env.DataManager.GetExcelSheet<BNpcName>()?.GetRowOrNull(TargetId)?.Singular.ExtractText(),
        ObjectKind.EventNpc =>
            Env.DataManager.GetExcelSheet<ENpcResident>()?.GetRowOrNull(TargetId)?.Singular.ExtractText(),
        ObjectKind.Treasure => null, // Not supported
        ObjectKind.Aetheryte => null,
        ObjectKind.GatheringPoint => null,
        ObjectKind.EventObj => Env.DataManager.GetExcelSheet<EObjName>()?.GetRowOrNull(TargetId)?.Singular.ExtractText(),
        ObjectKind.MountType => null,
        ObjectKind.Companion => Env.DataManager.GetExcelSheet<Companion>()?.GetRowOrNull(TargetId)?.Singular.ExtractText(), // Minion
        ObjectKind.Retainer => null,
        ObjectKind.Area => null,
        ObjectKind.Housing =>
            Env.DataManager.GetExcelSheet<HousingFurniture>()
              ?.GetRowOrNull(TargetId)?.Item.ValueNullable?.Name.ExtractText(),
        ObjectKind.Cutscene => null,
        ObjectKind.CardStand => null,
        ObjectKind.Ornament => null,
        _ => null
    } ?? "<unknown>";

    public string DisplayName => $"{Name} ({TargetId} - {TargetKind})";

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Target";
        public string ExpressionName => "Target";

        public IValueCondition? Current() => TargetNameCondition.Current();
        public IValueCondition Default() => new TargetNameCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.targetNpc;

        public IEnumerable<IValueCondition> TopLevel() {
            var supportedKinds = Enum.GetValues<ObjectKind>()
                .Where(kind => ObjectKindSupportLevel.GetValueOrDefault(kind) != ObjectKindSupport.UNSUPPORTED);
            return supportedKinds.Select(kind => new TargetNameCondition(kind, 0));
        }

        public IEnumerable<IValueCondition> Narrow(IValueCondition search) {
            var targetCondition = search as TargetNameCondition;
            if (targetCondition == null) { return new List<IValueCondition>(); }

            // If we already have a target we can't narrow any further
            if (targetCondition.TargetId != 0) {  return new List<IValueCondition>(); }

            // If our support level for this kind isn't fully SUPPORTED then we can't narrow any further
            var supportLevel = ObjectKindSupportLevel.GetValueOrDefault(targetCondition.TargetKind);
            if (supportLevel != ObjectKindSupport.SUPPORTED) {
                return new List<IValueCondition>();
            }

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
                    .DistinctBy(enpc => enpc.Singular.ExtractText())
                    .Select(enpc => new TargetNameCondition(ObjectKind.EventNpc, enpc.RowId));
            }

            if (targetCondition.TargetKind == ObjectKind.EventObj) {
                // Same as enpc -- identical names, different ids
                return Env.DataManager.GetExcelSheet<EObjName>()!
                    .Where(eobj => eobj.Singular != "")
                    .DistinctBy(eobj => eobj.Singular.ExtractText())
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

            return new List<IValueCondition>();
        }
    }
}
