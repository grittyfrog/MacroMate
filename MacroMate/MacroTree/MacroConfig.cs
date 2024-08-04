using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Dotnet.Tree;
using MacroMate.Extensions.Dalamud.Macros;
using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace MacroMate.MacroTree;

/**
 * Provides the mechanism to add/query/save contextual macros.
 */
public class MacroConfig {
    /// The icon used by inactive placeholder macros
    private uint _linkPlaceholderIconId = VanillaMacro.InactiveIconId;
    public uint LinkPlaceholderIconId {
        get { return _linkPlaceholderIconId; }
        set {
            _linkPlaceholderIconId = value;
            NotifyEdit();
        }
    }

    /// Toggles the display of all context menu items in the Vanilla Macro UI
    private bool _showVanillaMacroContextMenus = true;
    public bool ShowVanillaMacroContextMenus {
        get { return _showVanillaMacroContextMenus; }
        set {
            _showVanillaMacroContextMenus = value;
            NotifyEdit();
        }
    }

    private MateNode _root;
    public MateNode Root {
        get { return _root; }
        set {
            _root = value;
            NotifyEdit();
        }
    }

    public ImmutableList<MateNode.Macro> ActiveMacros { get; private set; } = ImmutableList<MateNode.Macro>.Empty;

    public delegate void OnConfigChangeDelegate();
    public event OnConfigChangeDelegate? ConfigChange;

    public MacroConfig() {
        _root = new MateNode.Group { Name = "Root" };
    }

    /// We want to update in place to not break other parts of the tree
    public void OverwiteFrom(MacroConfig other) {
        this._linkPlaceholderIconId = other.LinkPlaceholderIconId;
        this._root = other.Root;
        NotifyEdit();
    }

    /// Update ActiveMacros to reflect the current conditions
    public void ActivateMacrosForConditions(CurrentConditions conditions) {
        var newActiveMacros = new List<MateNode.Macro>();
        var allocatedLinks = new List<VanillaMacroLink>();

        var macroNodes = Root.Values().SelectNonNull(node => node as MateNode.Macro);
        foreach (var macroNode in macroNodes) {
            // If the macro doesn't have anything to link to, skip it
            if (macroNode.Link.IsUnbound()) { continue; }

            // If we've already linked a macro in any of the slots this macro wants, skip it
            if (macroNode.Link.VanillaMacroLinks().Any(link => allocatedLinks.Contains(link))) {
                continue;
            }

            if (macroNode.SatisfiedBy(conditions)) {
                newActiveMacros.Add(macroNode);
                allocatedLinks.AddRange(macroNode.Link.VanillaMacroLinks());
            }
        }

        ActiveMacros = newActiveMacros.ToImmutableList();
    }

    public ImmutableList<VanillaMacroLink> AllVanillaLinks() {
        return Root.Values()
            .OfType<MateNode.Macro>()
            .Select(macro => macro.Link.Clone())
            .Where(link => link.IsBound())
            .SelectMany(link => link.VanillaMacroLinks())
            .Distinct()
            .ToImmutableList();
    }

    public List<MateNode> LinkedNodesFor(VanillaMacroSet macroSet, uint macroSlot) {
        return Root.Values().Where(node => node switch {
            MateNode.Macro macro =>
              macro.Link.Set == macroSet && macro.Link.Slots.Contains(macroSlot),
            MateNode.Group => false,
            _ => false
        }).ToList();
    }

    /// <summary>Get the macro that is actively linked to a vanilla macro slot (if any)</summary>
    public MateNode.Macro? ActiveMacroLinkedTo(VanillaMacroSet macroSet, uint macroSlot) {
        return ActiveMacros.FirstOrDefault(macro => macro.Link.Set == macroSet && macro.Link.Slots.Contains(macroSlot));
    }

    public MateNode.Group CreateGroup(string groupName) {
        var newGroup = new MateNode.Group { Name = groupName };
        Root = Root.Attach(newGroup);
        NotifyEdit();
        return newGroup;
    }

    public MateNode.Group CreateGroup(TreeNode<MateNode> parent, string groupName) {
        var newGroup = new MateNode.Group { Name = groupName };
        parent.Attach(newGroup);
        NotifyEdit();
        return newGroup;
    }

    public MateNode.Macro CreateMacro(MateNode parent) {
        var newMacroNode = new MateNode.Macro {
            Name = "New Macro"
        };

        var templateSibling = parent.Children.OfType<MateNode.Macro>().LastOrDefault();
        if (templateSibling != null) {
            // We want to inherit the Icon ID and Links of our closest sibling or ancestor so it's
            // easier to create nodes that fit into an individual "group"
            newMacroNode.IconId = templateSibling.IconId;
            newMacroNode.Link = templateSibling.Link.Clone();
        }

        newMacroNode.AlwaysLinked = true;

        parent.Attach(newMacroNode);
        NotifyEdit();
        return newMacroNode;
    }

    /**
     * Moves [source] into [targetGroup] if it doesn't exist, or update an existing macro
     * with the same name.
     */
    public void MoveMacroIntoOrUpdate(
        MateNode.Macro macro,
        MateNode targetGroup,
        Func<MateNode.Macro, MateNode.Macro, bool> equalsFn // (existing, replacement) => equals
    ) {
        var existingNode = targetGroup.Children
            .FirstOrDefault(child => child is MateNode.Macro macroChild && equalsFn(macroChild, macro));
        if (existingNode is MateNode.Macro existingMacro) {
            existingMacro.StealValuesFrom(macro);
            NotifyEdit();
        } else {
            Env.MacroConfig.MoveInto(macro, targetGroup);
        }
    }

    public void MoveMacroIntoOrUpdate(MateNode.Macro macro, MateNode targetGroup) {
        MoveMacroIntoOrUpdate(macro, targetGroup, (existing, replacement) => existing.Name == replacement.Name);
    }

    public void MoveBeside(MateNode source, MateNode target, TreeNodeOffset offset) {
        source.MoveBeside(target, offset);
        NotifyEdit();
    }

    public void MoveInto(MateNode source, MateNode targetGroup) {
        source.MoveInto(targetGroup);
        NotifyEdit();
    }

    public void Delete(MateNode node) {
        Root.Delete(node.Id);
        NotifyEdit();
    }

    public void SortChildrenBy<TKey>(
        MateNode node,
        Func<MateNode, TKey> keySelector,
        bool ascending = true,
        bool sortSubgroups = false
    ) {
        node.SortChildrenBy(keySelector, ascending, sortSubgroups);
        NotifyEdit();
    }

    /// <summary>Imports multiple macros from the base game (macros that are already managed by macro mate are skipped)</summary>
    /// <returns>A list of messages indicating the imports</returns>
    public List<string> BulkImportFromXIV(
        MateNode parent,
        VanillaMacroSet macroSet,
        uint startMacroSlot,
        uint endMacroSlot
    ) {
        if (startMacroSlot > endMacroSlot) { throw new ArgumentException("Invalid import: start must be before end"); }

        var results = new List<string>();
        var linkedSlots = ActiveMacros.SelectMany(macro => macro.Link.Slots).ToHashSet();
        for (var macroSlot = startMacroSlot; macroSlot <= endMacroSlot; ++macroSlot) {
            if (linkedSlots.Contains(macroSlot)) {
                results.Add($"Slot {macroSlot}: Skipped (already managed by Macro Mate)");
                continue;
            }

            var vanillaMacro = Env.VanillaMacroManager.GetMacro(macroSet, macroSlot);
            if (vanillaMacro.LineCount == 0) {
                results.Add($"Slot {macroSlot}: Skipped (no lines)");
                continue;
            }

            var inactiveMacro = VanillaMacro.Inactive;
            if (vanillaMacro.Title == inactiveMacro.Title && vanillaMacro.Lines.ToString() == inactiveMacro.Lines.ToString()) {
                results.Add($"Slot {macroSlot}: Skipped (inactive macro mate link)");
                continue;
            }

            ImportFromXIV(parent, macroSet, macroSlot);
            results.Add($"Slot {macroSlot}: Imported");
        }

        return results;
    }

    /// Import a macro from the game
    public void ImportFromXIV(
        MateNode parent,
        VanillaMacroSet macroSet,
        uint macroSlot
    ) {
        var vanillaMacro = Env.VanillaMacroManager.GetMacro(macroSet, macroSlot);
        var name = string.IsNullOrEmpty(vanillaMacro.Title) ? "Imported Macro" : vanillaMacro.Title;

        var importMacro = new MateNode.Macro {
            Name = name,
            IconId = vanillaMacro.IconId,
            Link = new MacroLink {
                Set = macroSet,
                Slots = new() { macroSlot }
            },
            Lines = vanillaMacro.Lines.Value,
            AlwaysLinked = true,
        };

        MoveMacroIntoOrUpdate(
            importMacro,
            parent,
            (existing, replacement) => existing.Name == replacement.Name && existing.Link.Equals(replacement.Link)
        );
    }

    /// Update an existing macro with the name, icon and lines of a vanilla macro slot
    public void UpdateFromXIV(
        MateNode.Macro macro,
        VanillaMacroSet macroSet,
        uint macroSlot
    ) {
        var targetVanillaMacro = Env.VanillaMacroManager.GetMacro(macroSet, macroSlot);

        // Split the active macro into Vanilla Macros, update the correct part of the Vanilla macro, then
        // reconstitute it back into a macro mate macro.
        //
        // This approach is a bit more complex, but it allows for updating "part" of a multi-link macro.
        var existingMacroLinkBindings = macro.VanillaMacroLinkBinding();
        var replacementLines = new SeStringBuilder();
        foreach (var (vanillaLink, vanillaMacro) in existingMacroLinkBindings) {
            var shouldUpdate = vanillaLink.Set == macroSet && vanillaLink.Slot == macroSlot;
            var sourceStr = shouldUpdate ? vanillaMacro.Lines.Value : vanillaMacro.Lines.Value;
            foreach (var payload in sourceStr.Payloads) {
                // If we have Macro Chain enabled then we've added `/nextmacro` and should remove it
                if (macro.LinkWithMacroChain && payload is TextPayload textPayload && textPayload.Text == "/nextmacro") {
                    continue;
                }

                replacementLines.Add(payload);
            }
            replacementLines.Add(new NewLinePayload());
        }

        // Only update the name for single-link situations since multi-link appends "Foo 1", "Foo 2", "Foo 3" etc.
        if (existingMacroLinkBindings.Count() <= 1) {
            macro.Name = string.IsNullOrEmpty(targetVanillaMacro.Title) ? "Updated Macro" : targetVanillaMacro.Title;
        }
        macro.IconId = targetVanillaMacro.IconId;
        macro.Lines = replacementLines.Build();
        Env.MacroConfig.NotifyEdit();
    }

    /// Notifies the config that it has been edited.
    public void NotifyEdit() {
        if (ConfigChange != null) {
            ConfigChange();
        }
    }
}
