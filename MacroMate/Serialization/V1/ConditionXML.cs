using System;
using System.Xml;
using System.Xml.Serialization;
using Lumina.Excel.GeneratedSheets;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Serialization.V1;

[XmlInclude(typeof(ContentConditionXML))]
[XmlInclude(typeof(LocationConditionXML))]
[XmlInclude(typeof(TargetNpcConditionXML))]
[XmlInclude(typeof(TargetNameConditionXML))]
[XmlInclude(typeof(JobConditionXML))]
public abstract class ConditionXML {
    public abstract ICondition ToReal();

    public static ConditionXML From(ICondition condition) => condition switch {
        ContentCondition cond => ContentConditionXML.From(cond),
        LocationCondition cond => LocationConditionXML.From(cond),
        TargetNameCondition cond => TargetNameConditionXML.From(cond),
        JobCondition cond => JobConditionXML.From(cond),
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
            return new TargetNameCondition(new ExcelId<BNpcName>(TargetName.Id));
        }

        if (TargetNameCustom != null) {
            return new TargetNameCondition(TargetNameCustom);
        }

        return new TargetNameCondition();
    }

    public static TargetNpcConditionXML From(TargetNameCondition cond) => new() {
        TargetName = cond.targetName.Match(
            bNpc => new ExcelIdXML(bNpc),
            customName => null
        ),
        TargetNameCustom = cond.targetName.Match(
            bNpc => null,
            customName => customName
        )
    };
}

[XmlType("TargetNameCondition")]
public class TargetNameConditionXML : ConditionXML {
    [XmlAnyElement("TargetNameComment")]
    public XmlComment? TargetNameComment { get => TargetNameNpc?.Comment; set {} }
    public ExcelIdXML? TargetNameNpc { get; set; }
    public string? TargetNameCustom { get; set; }

    public override ICondition ToReal() {
        if (TargetNameNpc != null) {
            return new TargetNameCondition(new ExcelId<BNpcName>(TargetNameNpc.Id));
        }

        if (TargetNameCustom != null) {
            return new TargetNameCondition(TargetNameCustom);
        }

        return new TargetNameCondition();
    }

    public static TargetNameConditionXML From(TargetNameCondition cond) => new() {
        TargetNameNpc = cond.targetName.Match(
            bNpc => new ExcelIdXML(bNpc),
            customName => null
        ),
        TargetNameCustom = cond.targetName.Match(
            bNpc => null,
            customName => customName
        )
    };
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
