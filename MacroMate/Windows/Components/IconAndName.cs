
using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.MacroTree;

namespace MacroMate.Windows.Components;

/// An Editable Icon and Name component, with similar layout to vanilla
public static class IconAndName {
    public static bool Draw(MateNode.Macro macro) {
        var edited = false;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, ImGui.GetStyle().ItemSpacing.Y));
        var iconSize = ImGuiHelpers.ScaledVector2(48.0f);
        var icon = Env.TextureProvider.GetIcon(macro.IconId);

        var iconPicker = Env.PluginWindowManager.IconPicker;
        if (icon != null) {
            ImGui.Image(icon.ImGuiHandle, iconSize);
            if (ImGui.IsItemClicked()) {
                iconPicker.ShowOrFocus(macro.Id.ToString(), macro.IconId);
            }
        }
        while (iconPicker.TryDequeueEvent(macro.Id.ToString(), out uint selectedIconId)) {
            macro.IconId = selectedIconId;
            edited = true;
        }

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Name");

        if (macro.Link.IsBound()) {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
                Env.VanillaMacroManager.ShowMacroUI();
                Env.VanillaMacroManager.SelectMacroInUI(macro.Link.Set, macro.Link.Slots.First());
            };
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Show this macro in the 'User Macros' UI");
            }
        }

        ImGui.SetNextItemWidth(
            Math.Clamp(
                ImGui.GetContentRegionAvail().X,
                200.0f,
                500f - iconSize.X - ImGui.GetStyle().ItemSpacing.X
            )
        );
        var name = macro.Name;
        if (ImGui.InputText("###name", ref name, 255)) {
            macro.Name = name;
        };
        edited = edited || ImGui.IsItemDeactivatedAfterEdit();
        ImGui.EndGroup();
        ImGui.PopStyleVar();

        return edited;
    }
}
