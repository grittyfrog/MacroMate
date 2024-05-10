using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;
using MacroMate.Conditions;

namespace MacroMate.Serialization.V1;

[XmlType("OrCondition")]
public class OrConditionXML {
    [XmlElement("AndCondition")]
    public List<AndConditionXML>? Options { get; set; }

    public ConditionExpr.Or ToReal() => new ConditionExpr.Or(
        Options?.Select(option => option.ToReal())?.ToImmutableList() ?? ImmutableList<ConditionExpr.And>.Empty
    );

    public static OrConditionXML From(ConditionExpr.Or or) => new OrConditionXML {
        Options = or.options.Select(option => AndConditionXML.From(option)).ToList()
    };
}

[XmlType("AndCondition")]
public class AndConditionXML {
    [XmlElement("ContentCondition", typeof(ContentConditionXML))]
    [XmlElement("LocationCondition", typeof(LocationConditionXML))]
    [XmlElement("TargetNameCondition", typeof(TargetNameConditionXML))]
    [XmlElement("TargetNpcCondition", typeof(TargetNpcConditionXML))]
    [XmlElement("JobCondition", typeof(JobConditionXML))]
    [XmlElement("PvpStateCondition", typeof(PvpStateConditionXML))]
    [XmlElement("PlayerConditionCondition", typeof(PlayerConditionConditionXML))]
    public List<ConditionXML>? Conditions { get; set;  }

    public ConditionExpr.And ToReal() => new ConditionExpr.And(
        Conditions
            ?.Select(condition => condition.ToReal())
            ?.ToImmutableList()
            ?? ImmutableList<ICondition>.Empty
    );

    public static AndConditionXML From(ConditionExpr.And and) => new AndConditionXML {
        Conditions = and.conditions.Select(condition => ConditionXML.From(condition)).ToList()
    };
}
