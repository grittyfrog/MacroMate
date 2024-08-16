using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Serialization.V1;

[XmlInclude(typeof(ContentConditionXML))]
[XmlInclude(typeof(LocationConditionXML))]
[XmlInclude(typeof(TargetNpcConditionXML))]
[XmlInclude(typeof(TargetNameConditionXML))]
[XmlInclude(typeof(JobConditionXML))]
[XmlInclude(typeof(PvpStateConditionXML))]
[XmlInclude(typeof(PlayerConditionConditionXML))]
[XmlInclude(typeof(CurrentCraftMaxDurabilityConditionXML))]
public abstract class ConditionXML {
    public abstract ICondition ToReal();

    public static ConditionXML From(ICondition condition) => condition switch {
        ContentCondition cond => ContentConditionXML.From(cond),
        LocationCondition cond => LocationConditionXML.From(cond),
        TargetNameCondition cond => TargetNameConditionXML.From(cond),
        JobCondition cond => JobConditionXML.From(cond),
        PvpStateCondition cond => PvpStateConditionXML.From(cond),
        PlayerConditionCondition cond => PlayerConditionConditionXML.From(cond),
        CurrentCraftMaxDurabilityCondition cond => CurrentCraftMaxDurabilityConditionXML.From(cond),
        _ => throw new Exception($"Unexpected condition {condition}")
    };
}

[XmlType("ContentCondition")]
public class ContentConditionXML : ConditionXML {
    [XmlAnyElement("ContentComment")]
    public XmlComment? ContentComment { get => Content.Comment; set {} }
    public required ExcelIdXML Content { get; set; }

    public override ICondition ToReal() => new ContentCondition(Content.Id);

    public static ContentConditionXML From(ContentCondition cond) => new() {
        Content = new ExcelIdXML(cond.content)
    };
}

[XmlType("LocationCondition")]
public class LocationConditionXML : ConditionXML {
    [XmlAnyElement("TerritoryComment")]
    public XmlComment? TerritoryComment { get => Territory.Comment; set {} }
    public required ExcelIdXML Territory { get; set; }

    [XmlAnyElement("RegionOrSubAreaNameComment")]
    public XmlComment? RegionOrSubAreaNameComment { get => RegionOrSubAreaName?.Comment; set {} }
    public ExcelIdXML? RegionOrSubAreaName { get; set; }

    public override ICondition ToReal() => new LocationCondition(Territory.Id, RegionOrSubAreaName?.Id);

    public static LocationConditionXML From(LocationCondition cond) => new() {
        Territory = new ExcelIdXML(cond.territory),
        RegionOrSubAreaName = cond.regionOrSubAreaName?.Let(rosan => new ExcelIdXML(rosan))
    };
}

/**
 * Deprecated in favor of TargetNameCondition
 */
[XmlType("TargetNpcCondition")]
public class TargetNpcConditionXML : ConditionXML {
    [XmlAnyElement("TargetNameComment")]
    public XmlComment? TargetNameComment { get => TargetName?.Comment; set {} }
    public ExcelIdXML? TargetName { get; set; }
    public string? TargetNameCustom { get; set; }

    public override ICondition ToReal() {
        if (TargetName != null) {
            return new TargetNameCondition(ObjectKind.BattleNpc, TargetName.Id);
        }

        return new TargetNameCondition();
    }

    public static TargetNpcConditionXML From(TargetNameCondition cond) {
        throw new Exception("TargetNpcConditionXML is deprecated and should not be written");
    }
}

[XmlType("TargetNameCondition")]
public class TargetNameConditionXML : ConditionXML {
    [XmlAnyElement("TargetNameComment")]
    public XmlComment? TargetNameComment { get => TargetNameNpc?.Comment; set {} }

    public uint? TargetId { get; set; }
    public ObjectKind? TargetKind { get; set; }

    // Old Fields: Deprecated on next version
    public ExcelIdXML? TargetNameNpc { get; set; }
    public ExcelIdXML? TargetNameENpc { get; set; }
    public string? TargetNameCustom { get; set; }

    public override ICondition ToReal() {
        if (TargetId != null && TargetKind != null) {
            return new TargetNameCondition((ObjectKind)TargetKind, (uint)TargetId);
        }

        if (TargetNameNpc != null) {
            return new TargetNameCondition(ObjectKind.BattleNpc, TargetNameNpc.Id);
        }

        if (TargetNameENpc != null) {
            return new TargetNameCondition(ObjectKind.EventNpc, TargetNameENpc.Id);
        }

        return new TargetNameCondition();
    }

    public static TargetNameConditionXML From(TargetNameCondition cond) {
        return new TargetNameConditionXML {
            TargetId = cond.TargetId,
            TargetKind = cond.TargetKind
        };
    }
}

[XmlType("JobCondition")]
public class JobConditionXML : ConditionXML {
    [XmlAnyElement("JobComment")]
    public XmlComment? JobComment { get => Job.Comment; set {} }
    public required ExcelIdXML Job { get; set;  }

    public override ICondition ToReal() => new JobCondition(Job.Id);

    public static JobConditionXML From(JobCondition cond) => new() {
        Job = new ExcelIdXML(cond.Job)
    };
}

[XmlType("PvPStateCondition")]
public class PvpStateConditionXML : ConditionXML {
    public enum StateXML {
        IN_PVP,
        IN_PVP_NO_WOLVES_DEN,
        NOT_IN_PVP
    }

    public required StateXML PvpState { get; set; }

    public override ICondition ToReal() => new PvpStateCondition(
        PvpState switch {
            StateXML.IN_PVP => PvpStateCondition.State.IN_PVP,
            StateXML.IN_PVP_NO_WOLVES_DEN => PvpStateCondition.State.IN_PVP_NO_WOLVES_DEN,
            StateXML.NOT_IN_PVP => PvpStateCondition.State.NOT_IN_PVP,
            _ => PvpStateCondition.State.IN_PVP
        }
    );

    public static PvpStateConditionXML From(PvpStateCondition cond) => new() {
        PvpState = cond.state switch {
            PvpStateCondition.State.IN_PVP => StateXML.IN_PVP,
            PvpStateCondition.State.IN_PVP_NO_WOLVES_DEN => StateXML.IN_PVP_NO_WOLVES_DEN,
            PvpStateCondition.State.NOT_IN_PVP => StateXML.NOT_IN_PVP,
            _ => throw new ArgumentException($"Unknown Pvp state {cond.state}")
        }
    };
}

[XmlType("PlayerConditionCondition")]
public class PlayerConditionConditionXML : ConditionXML {
    public required List<ConditionFlag> Conditions { get; set; }

    public override ICondition ToReal() => new PlayerConditionCondition(Conditions.ToHashSet());

    public static PlayerConditionConditionXML From(PlayerConditionCondition cond) => new() {
        Conditions = cond.Conditions.ToList()
    };
}

[XmlType("CurrentCraftMaxDurabilityCondition")]
public class CurrentCraftMaxDurabilityConditionXML : ConditionXML {
    public required int MaxDurability { get; set; }

    public override ICondition ToReal() => new CurrentCraftMaxDurabilityCondition(MaxDurability);

    public static CurrentCraftMaxDurabilityConditionXML From(CurrentCraftMaxDurabilityCondition cond) => new() {
        MaxDurability = cond.MaxDurability
    };
}
