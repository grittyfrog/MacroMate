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
using System.Collections.Immutable;
using System.Globalization;
using MacroMate.Serialization.V1;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;

namespace MacroMate.Windows;

public class MainWindow : Window, IDisposable {
    public static readonly string NAME = "Macro Mate";
    private MateNode? Dragging = null;

    /// Special mode that shows Selection checkboxes to allow for bulk-editing Macros
    private bool editMode = false;
    private HashSet<Guid> editModeMacroSelection = new();

    private enum SearchFilter { ALL, ACTIVE, LINKED, UNLINKED }
    private string searchText = "";
    private SearchFilter searchFilter = SearchFilter.ALL;
    private ISet<Guid> searchedNodeIds = new HashSet<Guid>();

    private bool IsTextSearching => searchText != "";
    private bool IsSearching => IsTextSearching || searchFilter != SearchFilter.ALL;

    // We track these explicitly because the expansion rules for searching/non-searching are tricky.
    //
    // We want:
    //
    // 1. To expand all nodes when switching from non-search to search (so you can see your searched nodes)
    // 2. To return to the previous state when swhtcing from non-search to search (so you don't have every single group
    //    expanded whenever you search)
    //
    // To do this we need to explicitly track the expansion in both states
    private class GroupNodeExpansion {
        public bool expanded = false;
        public bool expandedForTextSearch = true;
    }
    private Dictionary<Guid, GroupNodeExpansion> GroupNodeExpanded = new();

    public MainWindow() : base(NAME, ImGuiWindowFlags.MenuBar) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        RefreshSearch();
        Env.MacroConfig.ConfigChange += OnConfigChange;
    }

    public void Dispose() { }

    private void OnConfigChange() {
        RefreshSearch();
    }

    public override void Draw() {
        DrawMenuBar();
        DrawEditModeActions();
        DrawSearchBar();

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
        var importPopupId = DrawImportPopup(Env.MacroConfig.Root);
        var linkPlaceholderIconScope = LinkPlaceholderIconPicker();

        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("New")) {
                if (ImGui.MenuItem("Group")) {
                    ImGui.OpenPopup(newGroupPopupId);
                }
                if (ImGui.MenuItem("Macro")) {
                    Env.MacroConfig.CreateMacro(Env.MacroConfig.Root);
                }
                if (ImGui.MenuItem("Import")) {
                    ImGui.OpenPopup(importPopupId);
                }
                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Edit Mode", null, editMode)) {
                EditModeSetEnabled(!editMode);
            }
            if (ImGui.IsItemHovered()) {
                var editModeState = editMode ? "Enabled" : "Disabled";
                ImGui.SetTooltip($"Toggle Edit Mode, which can be used to to edit multiple macros at once ({editModeState})");
            }

            if (ImGui.BeginMenu("Settings")) {
                ImGui.BeginGroup();
                if (ImGui.MenuItem("Link Placeholder Icon")) {
                    Env.PluginWindowManager.IconPicker.ShowOrFocus(linkPlaceholderIconScope);
                }
                ImGui.SameLine();
                var macroIcon = Env.TextureProvider.GetIcon(Env.MacroConfig.LinkPlaceholderIconId);
                if (macroIcon != null) {
                    ImGui.Image(macroIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
                    ImGui.SameLine();
                }
                ImGui.EndGroup();
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Select the icon used for macro slots that are managed by Macro Mate but currently unlinked (right click to reset to default)");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                    Env.MacroConfig.LinkPlaceholderIconId = VanillaMacro.InactiveIconId;
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Backups")) {
                Env.PluginWindowManager.BackupWindow.ShowOrFocus();
            }

            if (ImGui.MenuItem("Help")) {
                Env.PluginWindowManager.HelpWindow.ShowOrFocus();
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

    private void DrawSearchBar() {
        ImGui.SetNextItemWidth(90);
        var searchFilterName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(searchFilter.ToString().ToLower());
        if (ImGui.BeginCombo("###searchcombo", searchFilterName)) {
            foreach (var searchFilterOption in Enum.GetValues<SearchFilter>()) {
                bool isSelected = searchFilterOption == searchFilter;
                var searchFilterOptionName = CultureInfo.InvariantCulture.TextInfo
                    .ToTitleCase(searchFilterOption.ToString().ToLower());
                if (ImGui.Selectable(searchFilterOptionName, isSelected)) {
                    searchFilter = searchFilterOption;
                    RefreshSearch();
                }
                if (isSelected) { ImGui.SetItemDefaultFocus(); }

                if (ImGui.IsItemHovered()) {
                    if (searchFilterOption == SearchFilter.ALL) {
                        ImGui.SetTooltip("Show all macros");
                    } else if (searchFilterOption == SearchFilter.ACTIVE) {
                        ImGui.SetTooltip("Only show macros that are currently bound to a vanilla macro slot");
                    } else if (searchFilterOption == SearchFilter.LINKED) {
                        ImGui.SetTooltip("Only show macros that are linked to a vanilla macro slot");
                    } else if (searchFilterOption == SearchFilter.UNLINKED) {
                        ImGui.SetTooltip("Only show macros that do not have any links to a vanilla macro slot");
                    }
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();

        if (ImGui.InputTextWithHint("###mainwindow_macrosearch", "Search", ref searchText, 255)) {
            RefreshSearch();
        }

        if (searchText != "") {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times)) {
                searchText = "";
                RefreshSearch();
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Clear search text");
            }
        }
    }

    private void RefreshSearch() {
        Func<MateNode, bool>? textPredicate = searchText != ""
            ? (node) => node.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant())
            : null;

        Func<MateNode, bool>? filterPredicate = searchFilter switch {
            SearchFilter.ALL => null,
            SearchFilter.ACTIVE => (node) => Env.MacroConfig.ActiveMacros.Contains(node),
            SearchFilter.LINKED => (node) => node is MateNode.Macro macro && macro.HasLink,
            SearchFilter.UNLINKED => (node) => node is MateNode.Macro macro && !macro.HasLink,
            _ => null
        };

        // If we have text in the text predicate then we want to expand all the search nodes to make
        // sure you can see what you searched for
        if (textPredicate != null) {
            foreach (var expansionState in GroupNodeExpanded.Values) {
                expansionState.expandedForTextSearch = true;
            }
        }

        if (textPredicate != null || filterPredicate != null) {
            Func<MateNode, bool> textPredicateOrDefault = textPredicate ?? (_ => true);
            Func<MateNode, bool> filterPredicateOrDefault = filterPredicate ?? (_ => true);
            Func<MateNode, bool> predicate = (node) => textPredicateOrDefault(node) && filterPredicateOrDefault!(node);
            searchedNodeIds = Env.MacroConfig.Root.Search(predicate).Select(node => node.Id).ToHashSet();
        } else {
            searchedNodeIds = new HashSet<Guid>();
        }
    }

    private void DrawNode(MateNode node, int index) {
        // If we are searching we only want to draw matching nodes
        if (IsSearching && !searchedNodeIds.Contains(node.Id)) { return; }

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

        var expandedState = GroupNodeExpanded.GetOrAdd(group.Id, () => new());
        var expandedStateFlag = IsTextSearching ? expandedState.expandedForTextSearch : expandedState.expanded;
        ImGui.SetNextItemOpen(expandedStateFlag);
        bool groupOpen = ImGui.CollapsingHeader($"{group.Name}###{group.Id}");
        if (ImGui.IsItemToggledOpen()) {
            if (IsTextSearching) {
                expandedState.expandedForTextSearch = !expandedState.expandedForTextSearch;
            } else {
                expandedState.expanded = !expandedState.expanded;
            }
        }

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
        var importPopupId = DrawImportPopup(node);
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

            if (node is MateNode.Group) {
                if (ImGui.Selectable("Import Here")) {
                    ImGui.OpenPopup(importPopupId);
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Import a Macro Mate preset into this group");
                }
            }

            if (ImGui.Selectable("Export to Clipboard")) {
                var preset = MacroMateSerializerV1.Export(node);
                ImGui.SetClipboardText(preset);
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Export this item to the clipboard as a Macro Mate preset");
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
                ImGui.CloseCurrentPopup();
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

    private string importCode = "";
    private bool lastImportWasError = false;
    private uint DrawImportPopup(MateNode node) {
        var importPopupName = $"Import###mainwindow/import_popup/{node.Id}";
        var importPopupId = ImGui.GetID(importPopupName);

        if (ImGui.BeginPopup(importPopupName)) {
            // Make everything red if the import is an error
            if (lastImportWasError) {
                ImGui.PushStyleColor(ImGuiCol.Text, Colors.ErrorRed);
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Import Code");
            ImGui.SameLine();

            bool finished = false;

            // These can get reasonably big, so we just allocate a 16mb buffer and be done with it.
            var maxSize = 16u * 1024u * 1024u;
            if (
                ImGui.InputTextWithHint(
                    "###import_code",
                    "Paste a preset here and click 'Import'",
                    ref importCode,
                    maxSize,
                    ImGuiInputTextFlags.EnterReturnsTrue
                )
            ) {
                finished = true;
            }
            if (ImGui.IsItemEdited()) {
                lastImportWasError = false;
            }

            ImGui.SameLine();

            if (!lastImportWasError) {
                if (ImGui.Button("Import")) {
                    finished = true;
                }
            } else {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
                ImGui.PopFont();
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Preset string is invalid");
                }
            }

            if (lastImportWasError) {
                ImGui.PopStyleColor();
            }

            if (finished) {
                var importedNode = MacroMateSerializerV1.Import(importCode);
                if (importedNode != null) {
                    importCode = "";
                    lastImportWasError = false;

                    node.Attach(importedNode);
                    Env.MacroConfig.NotifyEdit();
                    ImGui.CloseCurrentPopup();
                } else {
                    lastImportWasError = true;
                }
            }
            ImGui.EndPopup();
        }

        return importPopupId;
    }

    private void EditModeSetEnabled(bool enabled) {
        editMode = enabled;

        // If entering edit mode from the root button we want to clear the selection.
        if (editMode == true) {
            editModeMacroSelection = new();
        }
    }

    private bool EditModeIsSelected(MateNode node) {
        // If we are a group then we are selected if all our descendants are selected. (Unless we are empty)
        if (node is MateNode.Group) {
            if (node.Children.Count == 0) { return false; }

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

    private string LinkPlaceholderIconPicker() {
        var iconPicker = Env.PluginWindowManager.IconPicker;
        var iconPickerScope = "main_window_link_placeholder_icon_picker";

        while (iconPicker.TryDequeueEvent(iconPickerScope, out uint selectedIconId)) {
            Env.MacroConfig.LinkPlaceholderIconId = selectedIconId;
        }

        return iconPickerScope;
    }
}
