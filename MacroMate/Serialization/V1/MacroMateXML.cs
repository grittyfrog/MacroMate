
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

[XmlRoot("MacroMate")]
public class MacroMateV1XML {
    [XmlAttribute]
    public int ConfigVersion { get; set; } = 1;

    public required uint? LinkPlaceholderIconId { get; set; } = VanillaMacro.InactiveIconId;
    public required bool? ShowVanillaMacroContextMenus { get; set; } = true;
    public required bool? UseMacroChainByDefault { get; set; } = false;
    public required bool? EnableSubscriptionAutoCheckForUpdates { get; set; } = true;
    public required int? MinutesBetweenSubscriptionAutoCheckForUpdates { get; set; } = 120;
    public required List<char>? CharPickerDialogFavourites { get; set; } = new();

    [XmlElement("Root", typeof(GroupXML))]
    [XmlElement("RootMacro", typeof(MacroXML))]
    public required MateNodeXML Root { get; set; }

    public required SubscriptionUrlCacheXML? SubscriptionUrlCache { get; set; }

    public MacroConfig ToReal() => new MacroConfig {
        LinkPlaceholderIconId = LinkPlaceholderIconId ?? VanillaMacro.InactiveIconId,
        ShowVanillaMacroContextMenus = ShowVanillaMacroContextMenus ?? true,
        UseMacroChainByDefault = UseMacroChainByDefault ?? false,
        EnableSubscriptionAutoCheckForUpdates = EnableSubscriptionAutoCheckForUpdates ?? true,
        MinutesBetweenSubscriptionAutoCheckForUpdates = MinutesBetweenSubscriptionAutoCheckForUpdates ?? 120,
        Root = Root.ToReal(),
        CharPickerDialogFavourites = (CharPickerDialogFavourites ?? new()).ToHashSet(),
        SubscriptionUrlCache = SubscriptionUrlCache?.ToReal() ?? new()
    };

    public static MacroMateV1XML From(MacroConfig config) => new() {
        LinkPlaceholderIconId = config.LinkPlaceholderIconId,
        ShowVanillaMacroContextMenus = config.ShowVanillaMacroContextMenus,
        UseMacroChainByDefault = config.UseMacroChainByDefault,
        EnableSubscriptionAutoCheckForUpdates = config.EnableSubscriptionAutoCheckForUpdates,
        MinutesBetweenSubscriptionAutoCheckForUpdates = config.MinutesBetweenSubscriptionAutoCheckForUpdates,
        Root = MateNodeXML.From(config.Root),
        CharPickerDialogFavourites = config.CharPickerDialogFavourites.ToList(),
        SubscriptionUrlCache = SubscriptionUrlCacheXML.From(config.SubscriptionUrlCache)
    };
}

[XmlInclude(typeof(GroupXML))]
[XmlInclude(typeof(MacroXML))]
public abstract class MateNodeXML {
    public required string Name { get; set; }

    public abstract MateNode ToReal();

    public static MateNodeXML From(MateNode node) => node switch {
        MateNode.SubscriptionGroup group => SubscriptionGroupXML.FromSubscriptionGroup(group),
        MateNode.Group group => GroupXML.FromGroup(group),
        MateNode.Macro macro => MacroXML.FromMacro(macro),
        _ => throw new Exception("Unknown MateNode type {node}")
    };
}

public class GroupXML : MateNodeXML {
    [XmlArrayItem("Group", typeof(GroupXML))]
    [XmlArrayItem("Macro", typeof(MacroXML))]
    [XmlArrayItem("SubscriptionGroup", typeof(SubscriptionGroupXML))]
    public required List<MateNodeXML> Nodes { get; set; } = new();

    public override MateNode ToReal() => new MateNode.Group {
        Name = Name
    }.Attach(Nodes.Select(node => node.ToReal()));

    public static GroupXML FromGroup(MateNode.Group group) => new GroupXML {
        Name = group.Name,
        Nodes = group.Children.Select(child => MateNodeXML.From(child)).ToList()
    };
}

public class SubscriptionGroupXML : MateNodeXML {
    public required string SubscriptionUrl { get; set; }
    public DateTimeOffset? LastSyncTime { get; set; }
    public List<string> LastSyncETags { get; set; } = new();
    public bool HasUpdate { get; set; } = false;

    [XmlArrayItem("Group", typeof(GroupXML))]
    [XmlArrayItem("Macro", typeof(MacroXML))]
    [XmlArrayItem("SubscriptionGroup", typeof(SubscriptionGroupXML))]
    public required List<MateNodeXML> Nodes { get; set; } = new();

    public override MateNode ToReal() => new MateNode.SubscriptionGroup {
        Name = Name,
        SubscriptionUrl = SubscriptionUrl,
        LastSyncTime = LastSyncTime,
        LastSyncETags = LastSyncETags,
        HasUpdate = HasUpdate
    }.Attach(Nodes.Select(node => node.ToReal()));

    public static SubscriptionGroupXML FromSubscriptionGroup(MateNode.SubscriptionGroup group) => new SubscriptionGroupXML {
        Name = group.Name,
        SubscriptionUrl = group.SubscriptionUrl,
        LastSyncTime = group.LastSyncTime,
        LastSyncETags = group.LastSyncETags,
        HasUpdate = group.HasUpdate,
        Nodes = group.Children.Select(child => MateNodeXML.From(child)).ToList()
    };
}

public class MacroXML : MateNodeXML {
    public uint? IconId { get; set; }
    public VanillaMacroSet? MacroSet { get; set; }
    public List<uint>? MacroSlots { get; set; }
    public bool? LinkWithMacroChain { get; set; }
    public string? Lines { get; set; }
    public string? Notes { get; set; }
    public bool? AlwaysLinked { get; set; }
    public OrConditionXML? OrCondition { get; set; }

    public override MateNode ToReal() => new MateNode.Macro {
        Name = Name,
        IconId = IconId ?? VanillaMacro.DefaultIconId,
        Link = new MacroLink {
            Set = MacroSet ?? VanillaMacroSet.INDIVIDUAL,
            Slots = MacroSlots ?? new()
        },
        LinkWithMacroChain = LinkWithMacroChain ?? false,
        Lines = Lines?.Let(lines => SeStringEx.ParseFromText(lines)) ?? "",
        Notes = Notes ?? "",
        AlwaysLinked = AlwaysLinked ?? false,
        ConditionExpr = OrCondition?.ToReal() ?? ConditionExpr.Or.Empty
    };

    public static MacroXML FromMacro(MateNode.Macro macro) {
        return new MacroXML {
            Name = macro.Name,
            IconId = macro.IconId,
            MacroSet = macro.Link.Set,
            MacroSlots = macro.Link.Slots,
            LinkWithMacroChain = macro.LinkWithMacroChain,
            Lines = macro.Lines.Unparse(),
            Notes = macro.Notes,
            AlwaysLinked = macro.AlwaysLinked,
            OrCondition = OrConditionXML.From(macro.ConditionExpr)
        };
    }
}
