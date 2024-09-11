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
using MacroMate.Subscription;

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

    public bool _enableSubscriptionAutoCheckForUpdates;
    public bool EnableSubscriptionAutoCheckForUpdates {
        get { return _enableSubscriptionAutoCheckForUpdates; }
        set {
            _enableSubscriptionAutoCheckForUpdates = value;
            NotifyEdit();
        }
    }

    public int _minutesBetweenSubscriptionAutoCheckForUpdates = 120;
    public int MinutesBetweenSubscriptionAutoCheckForUpdates {
        get { return _minutesBetweenSubscriptionAutoCheckForUpdates; }
        set {
            _minutesBetweenSubscriptionAutoCheckForUpdates = value;
            NotifyEdit();
        }
    }

    public SubscriptionUrlCache SubscriptionUrlCache { get; set; } = new();

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
        this._showVanillaMacroContextMenus = other.ShowVanillaMacroContextMenus;
        this._enableSubscriptionAutoCheckForUpdates = other.EnableSubscriptionAutoCheckForUpdates;
        this._minutesBetweenSubscriptionAutoCheckForUpdates = other.MinutesBetweenSubscriptionAutoCheckForUpdates;
        this._root = other.Root;
        this.SubscriptionUrlCache = other.SubscriptionUrlCache;
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

    public MateNode.SubscriptionGroup CreateSubscriptionGroup(MateNode parent, string subscriptionUrl) {
        var newSubscriptionGroup = new MateNode.SubscriptionGroup {
            Name = "<Subscription Name Pending Download>",
            SubscriptionUrl = subscriptionUrl
        };
        parent.Attach(newSubscriptionGroup);
        NotifyEdit();
        return newSubscriptionGroup;
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

    public MateNode.Macro CreateOrFindMacroByName(string name, MateNode targetGroup) {
        return CreateOrFindMacroByName(name, targetGroup, (_) => true);
    }

    /// <summary>
    /// Finds the group identified by path or creates all missing groups to ensure this path exists.
    /// </summary>
    /// <returns>Either a MateNode.Group or MateNode.SubscriptionGroup</returns>
    public MateNode CreateOrFindGroupByPath(MateNode root, MacroPath path) {
        MateNode? attachmentPoint = null;
        // The 'top' new group, only attached at the end in case we error mid-function
        MateNode? highestNewGroupAdded = null;

        MateNode currentNode = root;
        foreach (var segment in path.Segments) {
            var childNode = currentNode.GetChildFromPathSegment(segment);
            if (childNode == null) {
                if (segment is MacroPathSegment.ByName nameSegment) {
                    if (nameSegment.offset != 0) {
                        throw new ArgumentException($"cannot create group for missing named index (path: {path})");
                    }

                    var newChild = new MateNode.Group { Name = nameSegment.name };
                    // We
                    if (highestNewGroupAdded == null) {
                        attachmentPoint = currentNode;
                        highestNewGroupAdded = newChild;
                    } else {
                        currentNode.Attach(newChild);
                    }
                    currentNode = newChild;
                } else {
                    throw new ArgumentException($"cannot create group for missing index (path: {path}))");
                }
            } else if (childNode is MateNode.Group childGroup) {
                currentNode = childGroup;
            } else if (childNode is MateNode.SubscriptionGroup sGroup) {
                currentNode = sGroup;
            } else {
                throw new ArgumentException($"cannot create group for path containing macro (path: {path})");
            }
        }

        if (highestNewGroupAdded != null && attachmentPoint != null) {
            MoveInto(highestNewGroupAdded, attachmentPoint);
        }

        return currentNode; // Current node is the 'lowest' group after iteration
    }

    /// <summary>
    /// Finds a macro with [name] in [targetGroup] and returns it, or creates a new
    /// empty macro with [name] if it does not exist.
    /// </summary>
    public MateNode.Macro CreateOrFindMacroByName(
        string name,
        MateNode targetGroup,
        Func<MateNode.Macro, bool> fnExtraMatch
    ) {
        var existingNode = targetGroup.Children
            .FirstOrDefault(child => child is MateNode.Macro macroChild && child.Name == name && fnExtraMatch(macroChild));
        if (existingNode is MateNode.Macro existingMacro) {
            return existingMacro;
        } else {
            var newMacro = new MateNode.Macro { Name = name, AlwaysLinked = true };
            newMacro.MoveInto(targetGroup);
            NotifyEdit();
            return newMacro;
        }
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

        var targetMacroLink = new MacroLink { Set = macroSet, Slots = new() { macroSlot } };
        var importMacro = CreateOrFindMacroByName(name, parent, (existing) => existing.Link.Equals(targetMacroLink));
        importMacro.IconId = vanillaMacro.IconId;
        importMacro.Link = targetMacroLink;
        importMacro.Lines = vanillaMacro.Lines.Value;
        NotifyEdit();
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
            var sourceStr = shouldUpdate ? targetVanillaMacro.Lines.Value : vanillaMacro.Lines.Value;
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
        NotifyEdit();
    }

    /// Notifies the config that it has been edited.
    private bool notifyEditScheduled = false;
    public void NotifyEdit() {
        if (!notifyEditScheduled && ConfigChange != null) {
            notifyEditScheduled = true;
            Env.Framework.RunOnTick(() => {
                notifyEditScheduled = false;
                ConfigChange();
            });
        }
    }

}
