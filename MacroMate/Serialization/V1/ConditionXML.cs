using System;
using System.Xml;
using System.Xml.Serialization;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Serialization.V1;

[XmlInclude(typeof(ContentConditionXML))]
[XmlInclude(typeof(LocationConditionXML))]
[XmlInclude(typeof(TargetNpcConditionXML))]
[XmlInclude(typeof(JobConditionXML))]
public abstract class ConditionXML {
    public abstract ICondition ToReal();

    public static ConditionXML From(ICondition condition) => condition switch {
        ContentCondition cond => ContentConditionXML.From(cond),
        LocationCondition cond => LocationConditionXML.From(cond),
        TargetNpcCondition cond => TargetNpcConditionXML.From(cond),
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

[XmlType("TargetNpcCondition")]
public class TargetNpcConditionXML : ConditionXML {
    [XmlAnyElement("TargetNameComment")]
    public XmlComment? TargetNameComment { get => TargetName.Comment; set {} }
    public required ExcelIdXML TargetName { get; set; }

    public override ICondition ToReal() => new TargetNpcCondition(TargetName.Id);

    public static TargetNpcConditionXML From(TargetNpcCondition cond) => new() {
        TargetName = new ExcelIdXML(cond.targetName)
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
