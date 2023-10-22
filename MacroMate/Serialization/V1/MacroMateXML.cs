
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

[XmlRoot("MacroMate")]
public class MacroMateV1XML {
    [XmlAttribute]
    public int ConfigVersion { get; set; } = 1;

    [XmlElement("Root", typeof(GroupXML))]
    [XmlElement("RootMacro", typeof(MacroXML))]
    public required MateNodeXML Root { get; set; }

    public MateNode ToReal() => Root.ToReal();

    public static MacroMateV1XML From(MateNode root) => new() {
        Root = MateNodeXML.From(root)
    };
}

[XmlInclude(typeof(GroupXML))]
[XmlInclude(typeof(MacroXML))]
public abstract class MateNodeXML {
    public required string Name { get; set; }

    public abstract MateNode ToReal();

    public static MateNodeXML From(MateNode node) => node switch {
        MateNode.Group group => GroupXML.FromGroup(group),
        MateNode.Macro macro => MacroXML.FromMacro(macro),
        _ => throw new Exception("Unknown MateNode type {node}")
    };
}

public class GroupXML : MateNodeXML {
    [XmlArrayItem("Group", typeof(GroupXML))]
    [XmlArrayItem("Macro", typeof(MacroXML))]
    public required List<MateNodeXML> Nodes { get; set; } = new();

    public override MateNode ToReal() => new MateNode.Group {
        Name = Name
    }.Attach(Nodes.Select(node => node.ToReal()));

    public static GroupXML FromGroup(MateNode.Group group) => new GroupXML {
        Name = group.Name,
        Nodes = group.Children.Select(child => MateNodeXML.From(child)).ToList()
    };
}

public class MacroXML : MateNodeXML {
    public uint? IconId { get; set; }
    public VanillaMacroSet? MacroSet { get; set; }
    public List<uint>? MacroSlots { get; set; }
    public bool? LinkWithMacroChain { get; set; }
    public string? Lines { get; set; }
    public OrConditionXML? OrCondition { get; set; }


    public override MateNode ToReal() => new MateNode.Macro {
        Name = Name,
        IconId = IconId ?? VanillaMacro.DefaultIconId,
        Link = new MacroLink {
            Set = MacroSet ?? VanillaMacroSet.INDIVIDUAL,
            Slots = MacroSlots ?? new()
        },
        LinkWithMacroChain = LinkWithMacroChain ?? false,
        Lines = Lines ?? "",
        ConditionExpr = OrCondition?.ToReal() ?? ConditionExpr.Or.Empty
    };

    public static MacroXML FromMacro(MateNode.Macro macro) => new MacroXML {
        Name = macro.Name,
        IconId = macro.IconId,
        MacroSet = macro.Link.Set,
        MacroSlots = macro.Link.Slots,
        LinkWithMacroChain = macro.LinkWithMacroChain,
        Lines = macro.Lines,
        OrCondition = OrConditionXML.From(macro.ConditionExpr)
    };
}
