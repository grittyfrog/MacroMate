
using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.MacroTree;

namespace MacroMate.Windows.Components;

/// An Editable Icon and Name component, with similar layout to vanilla
public static class IconAndName {
    public static bool Draw(MateNode.Macro macro) {
        var edited = false;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));
        var iconSize = ImGuiHelpers.ScaledVector2(48.0f);
        var icon = Env.TextureProvider.GetMacroIcon(macro.IconId).GetWrapOrEmpty();

        var iconPicker = Env.PluginWindowManager.IconPicker;
        if (icon != null) {
            ImGui.Image(icon.ImGuiHandle, iconSize);
            if (ImGui.IsItemClicked()) {
                Env.PluginWindowManager.IconPicker.Open(macro.IconId, selectedIconId => {
                    macro.IconId = selectedIconId;
                    Env.MacroConfig.NotifyEdit();
                });
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
                ImGui.SetTooltip($"icon id: {macro.IconId}");
            }
        }

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Name");

        if (macro.Link.IsBound()) {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
                Env.VanillaMacroManager.EditMacroInUI(macro.Link.Set, macro.Link.Slots.First());
            };
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Show this macro in the 'User Macros' UI");
            }
        }

        ImGui.SetNextItemWidth(-1);
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
