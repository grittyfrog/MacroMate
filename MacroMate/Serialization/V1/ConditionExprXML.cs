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
    /// Deprecated field, kept to allow for forward-conversion of config
    [XmlElement("ContentCondition", typeof(ContentConditionXML))]
    [XmlElement("LocationCondition", typeof(LocationConditionXML))]
    [XmlElement("TargetNameCondition", typeof(TargetNameConditionXML))]
    [XmlElement("TargetNpcCondition", typeof(TargetNpcConditionXML))]
    [XmlElement("JobCondition", typeof(JobConditionXML))]
    [XmlElement("PvpStateCondition", typeof(PvpStateConditionXML))]
    [XmlElement("PlayerConditionCondition", typeof(PlayerConditionConditionXML))]
    public List<ConditionXML>? Conditions { get; set;  }

    public List<OpExprXML>? OpExprs { get; set; }

    public ConditionExpr.And ToReal() {
        // Forward-convert any old conditions into OpExpr
        var opExprFromConditions = Conditions ?.Select(xml => xml.ToReal().WrapInDefaultOp()) ?? new List<OpExpr>();

        // Load our normal path
        var opExprs = OpExprs?.Select(xml => xml.ToReal()) ?? new List<OpExpr>();

        return new ConditionExpr.And(opExprFromConditions.Concat(opExprs).ToImmutableList());
    }

    // Does not write to `Conditions` as it is a legacy field
    public static AndConditionXML From(ConditionExpr.And and) => new AndConditionXML {
        OpExprs = and.opExprs.Select(opExpr => OpExprXML.From(opExpr)).ToList()
    };
}
