using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using MacroMate.Extensions.Imgui;
using System.Collections.Generic;
using MacroMate.Extensions.Dotnet.Tree;
using MacroMate.Windows.Components;
using System.Linq;
using Dalamud.Interface.Utility;
using System.Text;

namespace MacroMate.Windows;

public class MainWindow : Window, IDisposable {
    public static readonly string NAME = "Macro Mate";
    private MateNode? Dragging = null;

    /// Special mode that shows Selection checkboxes to allow for bulk-editing Macros
    private bool editMode = false;
    private HashSet<Guid> editModeMacroSelection = new();

    public MainWindow() : base(NAME, ImGuiWindowFlags.MenuBar) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw() {
        DrawMenuBar();
        DrawEditModeActions();

        if (ImGui.BeginTable("main_window_layout_table", 3, ImGuiTableFlags.Hideable)) {
            ImGui.TableSetupColumn("Node", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("MenuButton", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableSetColumnEnabled(2, editMode);

            foreach (var (binding, bindingIndex) in Env.MacroConfig.Root.Children.WithIndex()) {
                ImGui.PushID(bindingIndex);
                DrawNode(binding, bindingIndex);
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
    }

    private void DrawMenuBar() {
        var newGroupPopupId = DrawNewGroupPopup(Env.MacroConfig.Root);

        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("New")) {
                if (ImGui.MenuItem("Group")) {
                    ImGui.OpenPopup(newGroupPopupId);
                }
                if (ImGui.MenuItem("Macro")) {
                    Env.MacroConfig.CreateMacro(Env.MacroConfig.Root);
                }
                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Edit Mode", null, editMode)) {
                EditModeSetEnabled(!editMode);
            }

            if (ImGui.MenuItem("Backups")) {
                Env.PluginWindowManager.BackupWindow.ShowOrFocus();
            }

            ImGui.EndMenuBar();
        }
    }

    private void DrawEditModeActions() {
        var iconPicker = Env.PluginWindowManager.IconPicker;
        var iconPickerScope = "main_window_edit_mode_icon_picker";

        var macroLinkPicker = Env.PluginWindowManager.MacroLinkPicker;
        var macroLinkPickerScope = "main_window_edit_mode_macro_link_picker";

        // Edit Buttons (if in edit mode)
        if (editMode) {
            if (ImGui.Button("Set Icon")) {
                iconPicker.ShowOrFocus(iconPickerScope);
            };
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Set the icon of all selected macros");
            }

            ImGui.SameLine();

            if (ImGui.Button("Set Link")) {
                macroLinkPicker.ShowOrFocus(macroLinkPickerScope);
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Set the link of all selected macros");
            }
        }

        while (iconPicker.TryDequeueEvent(iconPickerScope, out uint selectedIconId)) {
            foreach (var macro in Env.MacroConfig.Root.Values().OfType<MateNode.Macro>()) {
                if (editModeMacroSelection.Contains(macro.Id)) {
                    macro.IconId = selectedIconId;
                }
            }
            Env.MacroConfig.NotifyEdit();
        }

        while (macroLinkPicker.TryDequeueEvent(macroLinkPickerScope, out MacroLink? link)) {
            foreach (var macro in Env.MacroConfig.Root.Values().OfType<MateNode.Macro>()) {
                if (editModeMacroSelection.Contains(macro.Id)) {
                    macro.Link = link!;
                }
            }
            Env.MacroConfig.NotifyEdit();
        }
    }

    private void DrawNode(MateNode node, int index) {
        ImGui.PushID(index);
        ImGui.TableNextRow();
        switch (node) {
            case MateNode.Group group:
                DrawGroupNode(group);
                break;

            case MateNode.Macro macro:
                DrawMacroNode(macro);
                break;
        }
        ImGui.PopID();
    }

    private void DrawGroupNode(MateNode.Group group) {
        ImGui.TableNextColumn();
        bool groupOpen = ImGui.CollapsingHeader($"{group.Name}###{group.Id}");

        var nodeActionsPopupId = DrawNodeActionsPopup(group);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.OpenPopup(nodeActionsPopupId);
        }

        NodeDragDropSource(group, DragDropType.MACRO_OR_GROUP_NODE, drawPreview: () => { ImGui.CollapsingHeader(group.Name); });
        NodeDragDropTarget(group, DragDropType.MACRO_OR_GROUP_NODE, allowInto: true, allowBeside: true);

        DrawGroupLinkIcon(group);

        // Action Button
        ImGui.TableNextColumn();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Bars)) {
            ImGui.OpenPopup(nodeActionsPopupId);
        }

        // Select Button (if editMode)
        ImGui.TableNextColumn();
        bool selected = EditModeIsSelected(group);
        if (ImGui.Checkbox($"###{group.Id}_group_selected", ref selected)) {
            EditModeSetSelected(group, selected);
        }

        if (groupOpen) {
            DrawNodeChildren(group.Children);
        }
    }

    private void DrawGroupLinkIcon(MateNode.Group group) {
        var activelyLinkedChildren = group.Descendants()
            .OfType<MateNode.Macro>()
            .Where(macro => Env.MacroConfig.ActiveMacros.Contains(macro))
            .ToList();

        if (activelyLinkedChildren.Count == 0) { return; }

        ImGui.PushFont(UiBuilder.IconFont);
        var linkIconSize = ImGui.CalcTextSize(FontAwesomeIcon.Link.ToIconString());
        ImGui.SameLine(ImGui.GetContentRegionMax().X - linkIconSize.X);
        ImGui.Text(FontAwesomeIcon.Link.ToIconString());
        ImGui.PopFont();

        if (ImGui.IsItemHovered()) {
            var tooltipSb = new StringBuilder();
            tooltipSb.AppendLine("-- Active Macros Links --");
            foreach (var activeMacro in activelyLinkedChildren) {
                tooltipSb.AppendJoin(" » ", activeMacro.PathFrom(group).Select(pn => pn.Name));
                tooltipSb.Append($": bound to {activeMacro.Link.Name()}");
                tooltipSb.AppendLine();
            }

            ImGui.SetTooltip(tooltipSb.ToString());
        }
    }


    private void DrawMacroNode(MateNode.Macro macro) {
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 2f));

        ImGui.TableNextColumn();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
        var macroIcon = Env.TextureProvider.GetIcon(macro.IconId);
        if (macroIcon != null) {
            ImGui.Image(macroIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
            ImGui.SameLine();
        }

        if (ImGui.Selectable($"{macro.Name}###{macro.Id}")) {
            Env.PluginWindowManager.MacroWindow.ShowOrFocus(macro);
        }

        var nodeActionsPopupId = DrawNodeActionsPopup(macro);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.OpenPopup(nodeActionsPopupId);
        }

        NodeDragDropSource(macro, DragDropType.MACRO_OR_GROUP_NODE, drawPreview: () => {
            if (macroIcon != null) {
                ImGui.Image(macroIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
                ImGui.SameLine();
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().FramePadding.Y);
            ImGui.Selectable(macro.Name);
        });
        NodeDragDropTarget(macro, DragDropType.MACRO_OR_GROUP_NODE, allowBeside: true, allowInto: false);

        DrawMacroLinkIcon(macro);

        ImGui.PopStyleVar();
        ImGui.PopStyleVar();

        // Action Button: Nothing here
        ImGui.TableNextColumn();

        // Select Button (if editMode)
        ImGui.TableNextColumn();
        bool selected = EditModeIsSelected(macro);
        if (ImGui.Checkbox($"###{macro.Id}_group_selected", ref selected)) {
            EditModeSetSelected(macro, selected);
        }
    }

    private void DrawNodeChildren(IEnumerable<MateNode> children) {
        foreach (var (child, childIndex) in children.WithIndex()) {
            ImGui.Indent();
            DrawNode(child, childIndex);
            ImGui.Unindent();
        }
    }

    private void DrawMacroLinkIcon(MateNode.Macro macro) {
        if (!Env.MacroConfig.ActiveMacros.Contains(macro)) { return; }

        ImGui.PushFont(UiBuilder.IconFont);
        var linkIconSize = ImGui.CalcTextSize(FontAwesomeIcon.Link.ToIconString());
        ImGui.SameLine(ImGui.GetContentRegionMax().X - linkIconSize.X);
        ImGui.Text(FontAwesomeIcon.Link.ToIconString());
        ImGui.PopFont();

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip($"Macro link is active, bound to: {macro.Link.Name()}");
        }
    }

    private void NodeDragDropSource(MateNode node, DragDropType dragDropType, Action drawPreview) {
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoHoldToOpenOthers)) {
            ImGui.SetDragDropPayload(Enum.GetName(dragDropType), IntPtr.Zero, 0);
            drawPreview();
            Dragging = node;
            ImGui.EndDragDropSource();
        }
    }

    private void NodeDragDropTarget(MateNode node, DragDropType dragDropType, bool allowInto, bool allowBeside) {
        if (!allowInto && !allowBeside) { return; }

        if (ImGui.BeginDragDropTarget()) {
            var payload = ImGui.AcceptDragDropPayload(Enum.GetName(dragDropType), ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery);
            if (payload.IsNotNull()) {
                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                var itemSize = ImGui.GetItemRectSize();

                // For "beside" drag and drop (i.e. sibling drop) we need to decide how much of the item will count as "above" and "below"
                //
                // If we're also allowing an "into" drop we need to leave some room for the "into" part. This gives us the following spacing:
                //
                //                             | "Above" Size | "Into" Size | "Below" Size |
                //     ------------------------+--------------+-------------+--------------+
                //     allowInto + allowBeside | 25%          | 50%         | 25%          |
                //     allowInto               | 0%           | 100%        | 0%           |
                //                 allowBeside | 50%          | 0%          | 50%          |
                //

                bool cursorIsAbove = allowBeside && allowInto switch {
                    false => ImGui.GetMousePos().Y < (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.5f)),
                    true => ImGui.GetMousePos().Y < (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.75f))
                };
                bool cursorIsBelow = allowBeside && allowInto switch {
                    false => ImGui.GetMousePos().Y >= (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.5f)),
                    true => ImGui.GetMousePos().Y >= (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.25f))
                };

                if (payload.IsPreview()) {
                    if (cursorIsAbove) {
                        ImGui.GetWindowDrawList().AddLine(
                            new Vector2(itemMin.X, itemMin.Y),
                            new Vector2(itemMax.X, itemMin.Y),
                            ImGui.GetColorU32(ImGuiCol.DragDropTarget)
                        );
                    } else if (cursorIsBelow) {
                        ImGui.GetWindowDrawList().AddLine(
                            new Vector2(itemMin.X, itemMax.Y),
                            new Vector2(itemMax.X, itemMax.Y),
                            ImGui.GetColorU32(ImGuiCol.DragDropTarget)
                        );
                    } else if (allowInto) {
                        ImGui.GetWindowDrawList().AddRect(itemMin, itemMax, ImGui.GetColorU32(ImGuiCol.DragDropTarget));
                    }
                }

                if (payload.IsDelivery() && Dragging != null) {
                    if (cursorIsAbove) {
                        Env.MacroConfig.MoveBeside(Dragging, node, TreeNodeOffset.ABOVE);
                    } else if (cursorIsBelow) {
                        Env.MacroConfig.MoveBeside(Dragging, node, TreeNodeOffset.BELOW);
                    } else if (allowInto) {
                        Env.MacroConfig.MoveInto(Dragging, node);
                    }
                    Dragging = null;
                }
                ImGui.EndDragDropTarget();
            }
        }
    }

    private uint DrawNodeActionsPopup(MateNode node) {
        var nodeActionsPopupName = $"mainwindow/node_actions_popup/{node.Id}";
        var nodeActionsPopupId = ImGui.GetID(nodeActionsPopupName);

        var newGroupPopupId = DrawNewGroupPopup(node);
        var renamePopupId = DrawRenamePopup(node);
        var deletePopupId = DrawDeletePopupModal(node);

        if (ImGui.BeginPopup(nodeActionsPopupName)) {
            if (node is MateNode.Group) {
                if (ImGui.Selectable("Add Macro")) {
                    var newMacroNode = Env.MacroConfig.CreateMacro(node);
                    Env.PluginWindowManager.MacroWindow.ShowOrFocus(newMacroNode);
                }

                if (ImGui.Selectable("Add Group")) {
                    ImGui.OpenPopup(newGroupPopupId);
                }

                if (ImGui.Selectable("Edit All")) {
                    EditModeSetEnabled(true);
                    EditModeSetSelected(node, true);
                }
            }

            if (node is MateNode.Macro) {
                MacroRunMenuItem.Draw(
                    (node as MateNode.Macro)!,
                    showLinesOnSingleRunHover: true,
                    showLinesOnMultiRunHover: true
                );
            }

            if (ImGui.Selectable("Rename")) {
                ImGui.OpenPopup(renamePopupId);
            }

            if (node is MateNode.Macro macro) {
                if (ImGui.Selectable("Duplicate")) {
                    Env.MacroConfig.MoveBeside(macro.Clone(), macro, TreeNodeOffset.BELOW);
                }
            }

            if (ImGui.Selectable("Delete")) {
                ImGui.OpenPopup(deletePopupId);
            }

            ImGui.EndPopup();
        }

        return nodeActionsPopupId;
    }

    private uint DrawNewGroupPopup(MateNode parent) {
        var newGroupPopupName = $"mainwindow/new_group_popup/{parent.Id}";
        var newGroupPopupId = ImGui.GetID(newGroupPopupName);

        if (ImGui.BeginPopup(newGroupPopupName)) {
            string name = "";
            ImGui.Text("Name");
            ImGui.SameLine();
            ImGui.InputText("###group_name", ref name, 255);
            if (ImGui.IsItemDeactivatedAfterEdit()) {
                Env.MacroConfig.CreateGroup(parent, name);
            }
            ImGui.EndPopup();
        }

        return newGroupPopupId;
    }

    private uint DrawRenamePopup(MateNode node) {
        var renamePopupName = $"Rename###mainwindow/rename_popup/{node.Id}";
        var renamePopupId = ImGui.GetID(renamePopupName);

        if (ImGui.BeginPopup(renamePopupName)) {
            string name = node.Name;
            ImGui.Text("Name");
            ImGui.SameLine();
            ImGui.InputText("###node_name", ref name, 255);
            if (ImGui.IsItemDeactivatedAfterEdit()) {
                node.Name = name;
                Env.MacroConfig.NotifyEdit();
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        return renamePopupId;
    }

    private uint DrawDeletePopupModal(MateNode toDelete) {
        var deletePopupName = $"Delete###mainwindow/delete_popup/{toDelete.Id}";
        var deletePopupId = ImGui.GetID(deletePopupName);

        if (ImGui.BeginPopupModal(deletePopupName)) {
            ImGui.Text($"Are you sure you want to delete {toDelete.Name}?");
            if (ImGui.Button("Delete!")) {
                Env.MacroConfig.Delete(toDelete);
            }
            ImGui.SameLine();
            if (ImGui.Button("No!")) {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        return deletePopupId;
    }

    private void EditModeSetEnabled(bool enabled) {
        editMode = enabled;

        // If entering edit mode from the root button we want to clear the selection.
        if (editMode == true) {
            editModeMacroSelection = new();
        }
    }

    private bool EditModeIsSelected(MateNode node) {
        // If we are a group then we are selected if all our descendants are selected.
        if (node is MateNode.Group) {
            return node.Descendants()
                .OfType<MateNode.Macro>()
                .All(macro => editModeMacroSelection.Contains(macro.Id));
        }

        return editModeMacroSelection.Contains(node.Id);
    }

    private void EditModeSetSelected(MateNode node, bool selected) {
        // If we are a group then we want to set all of our descendant macros to selected/unselected.
        List<Guid> impactedNodeIds;
        if (node is MateNode.Group) {
            impactedNodeIds = node.Descendants().OfType<MateNode.Macro>().Select(macro => macro.Id).ToList();
        } else {
            impactedNodeIds = new() { node.Id };
        }

        if (selected) {
            foreach (var nodeId in impactedNodeIds) {
                editModeMacroSelection.Add(nodeId);
            }
        } else {
            foreach (var nodeId in impactedNodeIds) {
                editModeMacroSelection.Remove(nodeId);
            }
        }
    }
}
