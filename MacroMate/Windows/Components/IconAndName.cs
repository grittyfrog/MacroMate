
using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Imgui;
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
            ImGui.Image(icon.Handle, iconSize);
            if (ImGui.IsItemClicked()) {
                Env.PluginWindowManager.IconPicker.Open(macro.IconId, selectedIconId => {
                    macro.IconId = selectedIconId;
                    Env.MacroConfig.NotifyEdit();
                });
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip($"icon id: {macro.IconId}");
            }
        }

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Name");

        if (macro.Notes != string.Empty) {
            ImGui.SameLine();
            ImGuiComponents.IconButton(FontAwesomeIcon.StickyNote);
            ImGuiExt.HoverTooltip(macro.Notes);
        }

        if (macro.Link.IsBound()) {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
                Env.VanillaMacroManager.EditMacroInUI(macro.Link.Set, macro.Link.Slots.First());
            };
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Show this macro in the 'User Macros' UI");
            }
        }

        ImGui.SetNextItemWidth(MathF.Min(ImGui.GetContentRegionAvail().X - iconSize.X, 500f * ImGuiHelpers.GlobalScale));
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
