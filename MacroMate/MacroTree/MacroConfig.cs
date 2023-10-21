using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Dotnet.Tree;
using MacroMate.Extensions.Dalamud.Macros;

namespace MacroMate.MacroTree;

/**
 * Provides the mechanism to add/query/save contextual macros.
 */
public class MacroConfig {
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

    /// Update ActiveMacros to reflect the current conditions
    public void ActivateMacrosForConditions(CurrentConditions conditions) {
        var newActiveMacros = new List<MateNode.Macro>();
        var allocatedSlots = new List<uint>();

        var macroNodes = Root.Values().SelectNonNull(node => node as MateNode.Macro);
        foreach (var macroNode in macroNodes) {
            // If the macro doesn't have anything to link to, skip it
            if (macroNode.Link.IsUnbound()) { continue; }

            // If we've already linked a macro in any of the slots this macro wants, skip it
            if (macroNode.Link.Slots.Any(slot => allocatedSlots.Contains(slot))) {
                continue;
            }

            if (macroNode.SatisfiedBy(conditions)) {
                newActiveMacros.Add(macroNode);
                allocatedSlots.AddRange(macroNode.Link.Slots);
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

        // Rather then starting with an empty condition, lets start with one with every applicable
        // condition so you have an idea of the possibilities
        newMacroNode.ConditionExpr = Conditions.ConditionExpr.Or.Default();

        parent.Attach(newMacroNode);
        NotifyEdit();
        return newMacroNode;
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


    /// Notifies the config that it has been edited.
    public void NotifyEdit() {
        if (ConfigChange != null) {
            ConfigChange();
        }
    }
}
