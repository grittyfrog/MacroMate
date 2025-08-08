using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Imgui;
using MacroMate.MacroTree;
using MacroMate.Windows.Components;

namespace MacroMate.Windows;

/// <summary>
/// Allows for the bulk editing of most macro fields.
///
/// Done by editing a "Fake" macro and copying the correct fields to our target macros.
///
/// Targets are selected by "Edit All".
/// </summary>
public class MacroBulkEditWindow : Window {
    public static readonly string NAME = "Macro Bulk Edit";

    private List<MacroBulkEdit> Edits { get; set; } = new();

    public MacroBulkEditWindow() : base(NAME, ImGuiWindowFlags.MenuBar) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        DrawMenuBar();
        ImGui.NewLine();
        DrawBulkEditActions();
    }

    private void DrawMenuBar() {
        var addBulkEditActionPopup = DrawAddBulkEditActionPopup();

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));
        if (ImGui.BeginMenuBar()) {
            if (ImGui.MenuItem("Add Action")) {
                ImGui.OpenPopup(addBulkEditActionPopup);
            }
            ImGuiExt.HoverTooltip("Add a new bulk edit action to the action list");

            if (ImGui.MenuItem("Apply")) {
                ApplyBulkEdit(Env.PluginWindowManager.MainWindow.EditModeTargets());
                Env.MacroConfig.NotifyEdit();
            }
            ImGuiExt.HoverTooltip("Applies all bulk edit actions to all selected macros");

            if (ImGui.MenuItem("Clear All")) {
                foreach (var edit in Edits) { edit.Dispose(); }
                Edits.Clear();
            }
            ImGuiExt.HoverTooltip("Clear all bulk edit actions");

            if (Edits.Any(edit => edit.Applied)) {
                if (ImGui.MenuItem("Clear Applied")) {
                    var toClearIndicies = Edits.WithIndex().Where(v => v.item.Applied).Select(v => v.index);
                    foreach (var editIndex in toClearIndicies) { Edits[editIndex].Dispose(); }
                    Edits.RemoveAllAt(toClearIndicies);
                }
                ImGuiExt.HoverTooltip("Clear applied bulk edit actions");
            }

            ImGui.EndMenuBar();
        }

        ImGui.PopStyleVar();
    }

    private void DrawBulkEditActions() {
        ImGui.TextUnformatted("Bulk Edit Actions");
        ImGuiExt.HoverTooltip("These actions will be applied to all selected macros after clicking 'Apply'");
        ImGui.Separator();

        if (ImGui.BeginTable("macro_bulk_edit_window/actions_table", 4, ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Applied", ImGuiTableColumnFlags.WidthFixed);

            var indiciesToRemove = new List<int>();
            foreach (var (edit, index) in Edits.WithIndex()) {
                ImGui.PushID(index);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                edit.DrawLabel(index);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                edit.DrawValue(index);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Minus.ToIconString())) {
                    indiciesToRemove.Add(index);
                }
                ImGuiExt.HoverTooltip("Delete this action");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                if (edit.Applied) {
                    ImGuiExt.YesIcon();
                    ImGuiExt.HoverTooltip("This action has been applied");
                }

                ImGui.PopID();
            }

            foreach (var editIndex in indiciesToRemove) { Edits[editIndex].Dispose(); }
            Edits.RemoveAllAt(indiciesToRemove);

            ImGui.EndTable();
        }
    }

    public void ApplyBulkEdit(IEnumerable<MateNode.Macro> targets) {
        foreach (var target in targets) {
            foreach (var edit in Edits) {
                edit.ApplyTo(target);
                edit.Applied = true;
            }
        }
    }

    public void ShowOrFocus() {
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    private uint DrawAddBulkEditActionPopup() {
        var popupName = $"macro_bulk_edit_window/add_bulk_edit_action_popup";
        var popupId = ImGui.GetID(popupName);

        if (ImGui.BeginPopup(popupName)) {
            foreach (var factory in MacroBulkEdit.Factory.All) {
                if (ImGui.Selectable(factory.Name)) {
                    Edits.Add(factory.Create());
                }
            }
            ImGui.EndPopup();
        }

        return popupId;
    }
}

// <summary>
// A single bulk-edit action that can be added in the bulk edit window.
// </summary>
internal interface MacroBulkEdit : IDisposable {
    public string Name { get => FactoryRef.Name; }
    public bool Applied { get; set; }

    public void DrawLabel(int id);
    public void DrawValue(int id);
    public void ApplyTo(MateNode.Macro target);

    public Factory FactoryRef { get; }

    internal interface Factory {
        public string Name { get; }
        public MacroBulkEdit Create();

        public static IEnumerable<MacroBulkEdit.Factory> All = new[] {
            SetIconMacroBulkEdit.Factory,
            SetLinkBulkEditAction.Factory,
            SetUseMacroChainBulkEditAction.Factory,
            SetAlwaysLinkedBulkEditAction.Factory,
            AddOrConditionBulkEditAction.Factory,
            AddAndConditionBulkEditAction.Factory,
            RemoveConditionBulkEditAction.Factory,
            RemoveAllConditionsBulkEditAction.Factory
        };
    }
}

internal class SetIconMacroBulkEdit : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    public uint IconId { get; set; } = VanillaMacro.DefaultIconId;

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Set Icon to");
    }

    public void DrawValue(int id) {
        var macroIcon = Env.TextureProvider.GetMacroIcon(IconId).GetWrapOrEmpty();
        if (macroIcon != null) {
            ImGui.Image(macroIcon.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()) * 1.3f);
        }

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Click to change, right click to reset to default");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
            Env.PluginWindowManager.IconPicker.Open(IconId, selectedIconId => {
                IconId = selectedIconId;
            });
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            IconId = VanillaMacro.DefaultIconId;
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.IconId = IconId;
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name { get => "Set Icon"; }
        public MacroBulkEdit Create() => new SetIconMacroBulkEdit();
    }
}


internal class SetLinkBulkEditAction : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    public MacroLink MacroLink { get; set; } = new();

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Set Link to");
    }

    public void DrawValue(int id) {
        var macroLinkPickerScope = $"set_link_bulk_edit_action_{id}";

        if (ImGui.Button(MacroLink.Name())) {
            Env.PluginWindowManager.MacroLinkPicker.ShowOrFocus(macroLinkPickerScope);
        }

        while (Env.PluginWindowManager.MacroLinkPicker.TryDequeueEvent(macroLinkPickerScope, out MacroLink? link)) {
            if (link != null) {
                MacroLink = link;
            }
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.Link = MacroLink.Clone();
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Set Link";
        public MacroBulkEdit Create() => new SetLinkBulkEditAction();
    }
}


internal class SetUseMacroChainBulkEditAction : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    public bool UseMacroChain { get; set; } = new();

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Set 'Use Macro Chain' to");
    }

    public void DrawValue(int id) {
        var buttonText = UseMacroChain ? "Enabled" : "Disabled";
        if (ImGui.Button(buttonText)) {
            UseMacroChain = !UseMacroChain;
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.LinkWithMacroChain = UseMacroChain;
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Set 'Use Macro Chain'";
        public MacroBulkEdit Create() => new SetUseMacroChainBulkEditAction();
    }
}


internal class SetAlwaysLinkedBulkEditAction : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    public bool AlwaysLinked { get; set; } = new();

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Set 'Always Linked' to");
    }

    public void DrawValue(int id) {
        var buttonText = AlwaysLinked ? "Enabled" : "Disabled";
        if (ImGui.Button(buttonText)) {
            AlwaysLinked = !AlwaysLinked;
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.AlwaysLinked = AlwaysLinked;
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Set 'Always Linked'";
        public MacroBulkEdit Create() => new SetAlwaysLinkedBulkEditAction();
    }
}


internal class AddOrConditionBulkEditAction : MacroBulkEdit {
    private ConditionExprEditor conditionExprEditor = new();

    public bool Applied { get; set; } = false;
    public ConditionExpr.Or ConditionExpr { get; set; } = Conditions.ConditionExpr.Or.Empty;

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Add 'Or' Condition");
        ImGuiExt.HoverTooltip("When applied add all conditions in this set as an 'Or' condition to selected macros");
    }

    public void DrawValue(int id) {
        ImGui.BeginGroup();

        var conditionExpr = ConditionExpr;
        if (conditionExprEditor.DrawEditor(ref conditionExpr)) {
            ConditionExpr = conditionExpr;
        }

        ImGui.EndGroup();
    }

    public void ApplyTo(MateNode.Macro target) {
        foreach (var andExpr in ConditionExpr.options) {
            target.AddAndExpression(andExpr);
        }
    }

    public void Dispose() { conditionExprEditor.Dispose(); }

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Add 'Or' Condition";
        public MacroBulkEdit Create() => new AddOrConditionBulkEditAction();
    }
}

internal class AddAndConditionBulkEditAction : MacroBulkEdit {
    private ConditionExprEditor conditionExprEditor = new();
    private static List<string?> HasConditionValues = new List<string?> { null }
        .Concat(ICondition.IFactory.All.Select(c => c.ConditionName).ToList())
        .ToList();

    public enum TargetGroup { FIRST, LAST, ALL };

    public bool Applied { get; set; } = false;
    public TargetGroup TargetGroupName = TargetGroup.FIRST;
    public string? HasConditionName = null;
    public ConditionExpr.Or ConditionExpr { get; set; } = Conditions.ConditionExpr.Or
        .Single(Conditions.ConditionExpr.And.Empty);

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Add 'And' Condition");
        ImGuiExt.HoverTooltip("Extends the 'And' condition that matches our target filters");
    }

    public void DrawValue(int id) {
        if (ImGui.BeginTable("###add_and_condition_draw_value_layout_table", 2)) {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Target Group");

            ImGui.TableNextColumn();
            ImGuiExt.EnumCombo("###target_group", ref TargetGroupName);
            if (ImGui.IsItemHovered()) {
                if (TargetGroupName == TargetGroup.FIRST) {
                    ImGui.SetTooltip("Only add conditions to the first 'And' group that matches all filters");
                } else if (TargetGroupName == TargetGroup.LAST) {
                    ImGui.SetTooltip("Only add conditions to the last 'And' group that matches all filters");
                } else if (TargetGroupName == TargetGroup.ALL) {
                    ImGui.SetTooltip("Add conditions to all groups that match our filters");
                }
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Condition Filter");
            ImGuiExt.HoverTooltip("If set, only update 'And' groups that include this condition");

            ImGui.TableNextColumn();
            ImGuiExt.NullableStringCombo("###has_condition_filter", "<no filter>", HasConditionValues, ref HasConditionName);

            ImGui.EndTable();
        }

        ImGui.TextUnformatted("Condition");
        ImGui.BeginGroup();
        var conditionExpr = ConditionExpr;
        if (conditionExprEditor.DrawEditor(ref conditionExpr, singleAndCondition: true)) {
            ConditionExpr = conditionExpr;
        }
        ImGui.EndGroup();
    }

    public void ApplyTo(MateNode.Macro target) {
        if (target.ConditionExpr.options.IsEmpty) { target.AddAndExpression(); }
        var matchingOptions = target.ConditionExpr.options.WithIndex().Where(andAndIndex =>
            HasConditionName == null || andAndIndex.item.opExprs.Any(expr => expr.Condition.ConditionName == HasConditionName)
        ).ToList();
        if (matchingOptions.Count == 0) { return; }

        var targetAndIndexes = TargetGroupName switch {
            TargetGroup.FIRST => matchingOptions.Take(1),
            TargetGroup.LAST => matchingOptions.TakeLast(1),
            TargetGroup.ALL => matchingOptions,
            _ => matchingOptions
        };

        foreach (var (and, index) in targetAndIndexes) {
            target.ConditionExpr = target.ConditionExpr
                .UpdateAnd(index, a => a.Merge(ConditionExpr.options.First()))
                .DeleteWhere(and => and.opExprs.Count == 0);
        }
    }

    public void Dispose() { conditionExprEditor.Dispose(); }

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Add 'And' Condition";
        public MacroBulkEdit Create() => new AddAndConditionBulkEditAction();
    }
}



internal class RemoveConditionBulkEditAction : MacroBulkEdit {
    private static List<string> Conditions = ICondition.IFactory.All.Select(c => c.ConditionName).ToList();

    public bool Applied { get; set; } = false;
    public string ConditionName = Conditions.First();

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Remove Condition");
    }

    public void DrawValue(int id) {
        ImGuiExt.StringCombo("", Conditions, ref ConditionName);
        var hovered = ImGui.IsItemHovered();

        if (hovered) {
            ImGui.SetTooltip("Removes all conditions of this type from selected macros");
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.ConditionExpr = target.ConditionExpr
            .UpdateAnds(and => and.DeleteWhere(op => op.Condition.ConditionName == ConditionName))
            .DeleteWhere(and => and.opExprs.Count == 0);
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Remove Condition";
        public MacroBulkEdit Create() => new RemoveConditionBulkEditAction();
    }
}


internal class RemoveAllConditionsBulkEditAction : MacroBulkEdit {
    public bool Applied { get; set; } = false;

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Remove All Conditions");
    }

    public void DrawValue(int id) {}

    public void ApplyTo(MateNode.Macro target) {
        target.ConditionExpr = ConditionExpr.Or.Empty;
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Remove All Conditions";
        public MacroBulkEdit Create() => new RemoveAllConditionsBulkEditAction();
    }
}
