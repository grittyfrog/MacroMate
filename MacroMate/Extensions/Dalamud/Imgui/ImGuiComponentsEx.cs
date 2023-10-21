using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace MacroMate.Extensions.Dalamud.Imgui;

public static class ImGuiComponentsEx {
    public static bool IconTextButton(FontAwesomeIcon icon, string label) {
        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon.ToIconString());
        ImGui.PopFont();

        var labelSize = ImGui.CalcTextSize(label);

        var style = ImGui.GetStyle();
        var size = new Vector2(
            iconSize.X + style.ItemSpacing.X + labelSize.X + (style.FramePadding.X * 2.0f),
            ImGui.GetTextLineHeight() + (style.FramePadding.Y * 2.0f)
        );
        var pos = ImGui.GetCursorScreenPos();

        var drawList = ImGui.GetWindowDrawList();

        ImGui.BeginGroup();

        ImGui.SetCursorScreenPos(pos + new Vector2(style.FramePadding.X, 0));

        // Draw Background
        drawList.AddRectFilled(
            pos,
            pos + size,
            ImGui.GetColorU32(ImGuiCol.Button),
            ImGui.GetStyle().FrameRounding
        );

        // Icon
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(icon.ToIconString());
        ImGui.PopFont();

        // Label
        ImGui.SameLine();
        ImGui.Text(label);

        ImGui.SetCursorScreenPos(pos + size);

        ImGui.EndGroup();

        if (ImGui.IsItemHovered()) {

        }

        // var drawList = ImGui.GetWindowDrawList();
        // var posBefore = ImGui.GetCursorScreenPos();

        // ImGui.SetCursorScreenPos(posBefore + new Vector2(style.FramePadding.X, 0));

        // // Re-order the next two sets of draws
        // drawList.ChannelsSplit(2);

        // // Icon and font
        // drawList.ChannelsSetCurrent(1);
        // ImGui.BeginGroup();
        // ImGui.PushFont(UiBuilder.IconFont);
        // //ImGui.Text(icon.ToIconString());
        // drawList.AddText(posBefore, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        // ImGui.PopFont();
        // ImGui.SameLine();
        // ImGui.Text(text);

        // ImGui.EndGroup();

        // var groupSize = ImGui.GetItemRectSize();
        // var endPos = posBefore + groupSize + new Vector2(style.FramePadding.X * 2, 0);

        // // Background
        // drawList.ChannelsSetCurrent(1);
        // drawList.AddRectFilled(
        //     posBefore,
        //     endPos,
        //     ImGui.GetColorU32(ImGuiCol.Button),
        //     ImGui.GetStyle().FrameRounding
        // );

        // drawList.ChannelsMerge();

        //if (ImGui.IsItemClicked()) { return true; }
        return false;
    }
}
