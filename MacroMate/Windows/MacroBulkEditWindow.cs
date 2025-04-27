using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Imgui;
using MacroMate.MacroTree;

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
                Edits.Clear();
            }
            ImGuiExt.HoverTooltip("Clear all bulk edit actions");

            if (Edits.Any(edit => edit.Applied)) {
                if (ImGui.MenuItem("Clear Applied")) {
                    var toClearIndicies = Edits.WithIndex().Where(v => v.item.Applied).Select(v => v.index);
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

        if (ImGui.BeginTable("macro_bulk_edit_window/actions_table", 3)) {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Applied", ImGuiTableColumnFlags.WidthFixed);

            var indiciesToRemove = new List<int>();
            foreach (var (edit, index) in Edits.WithIndex()) {
                ImGui.PushID(index);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                edit.Draw(index);

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
internal interface MacroBulkEdit {
    public string Name { get => FactoryRef.Name; }
    public bool Applied { get; set; }

    public void Draw(int id);
    public void ApplyTo(MateNode.Macro target);

    public Factory FactoryRef { get; }

    internal interface Factory {
        public string Name { get; }
        public MacroBulkEdit Create();

        public static IEnumerable<MacroBulkEdit.Factory> All = new[] {
            SetIconMacroBulkEdit.Factory,
            SetLinkBulkEditAction.Factory,
            SetUseMacroChainBulkEditAction.Factory
        };
    }
}

internal class SetIconMacroBulkEdit : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    public uint IconId { get; set; } = VanillaMacro.DefaultIconId;

    public void Draw(int id) {
        ImGui.TextUnformatted("Set Icon to");
        var setIconHovered = ImGui.IsItemHovered();
        var setIconClicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        var setIconRightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        ImGui.SameLine();
        var macroIcon = Env.TextureProvider.GetMacroIcon(IconId).GetWrapOrEmpty();
        if (macroIcon != null) {
            ImGui.Image(macroIcon.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()) * 1.3f);
        }
        setIconHovered |= ImGui.IsItemHovered();
        setIconClicked |= ImGui.IsItemClicked(ImGuiMouseButton.Left);
        setIconRightClicked |= ImGui.IsItemClicked(ImGuiMouseButton.Right);

        if (setIconHovered) {
            ImGui.SetTooltip("Click to change, right click to reset to default");
        }
        if (setIconClicked) {
            Env.PluginWindowManager.IconPicker.Open(IconId, selectedIconId => {
                IconId = selectedIconId;
            });
        }
        if (setIconRightClicked) {
            IconId = VanillaMacro.DefaultIconId;
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.IconId = IconId;
    }

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

    public void Draw(int id) {
        var macroLinkPickerScope = $"set_link_bulk_edit_action_{id}";

        ImGui.TextUnformatted("Set Link to");
        ImGui.SameLine();
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

    public void Draw(int id) {
        ImGui.TextUnformatted("Set 'Use Macro Chain' to");
        ImGui.SameLine();
        var buttonText = UseMacroChain ? "Enabled" : "Disabled";
        if (ImGui.Button(buttonText)) {
            UseMacroChain = !UseMacroChain;
        }
    }

    public void ApplyTo(MateNode.Macro target) {
        target.LinkWithMacroChain = UseMacroChain;
    }

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Set 'Use Macro Chain'";
        public MacroBulkEdit Create() => new SetUseMacroChainBulkEditAction();
    }
}
