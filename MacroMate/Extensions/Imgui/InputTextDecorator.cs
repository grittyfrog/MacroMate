using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Lumina.Text.ReadOnly;

namespace MacroMate.Extensions.Imgui;


/// High hackery to decorate an MultiLineTextInput
public class InputTextDecorator {
    private Vector2 scroll = new Vector2(0, 0);

    private unsafe ImGuiInputTextStatePtr TextState => new(&ImGui.GetCurrentContext().Handle->InputTextState);

    /// Decorates an InputText
    ///
    /// Assumes that InputText was the last rendered ImGui widget.
    /// Assumes no overlapping decorations
    public void DecorateInputText(
        string label,
        ref string text,
        Vector2 size,
        IEnumerable<InputTextDecoration> decorations
    ) {
        scroll.X = TextState.ScrollX;
        ImGui.BeginChild(label);
        scroll.Y = ImGui.GetScrollY();
        ImGui.EndChild();

        // Prevent showing misplaced decorations when:
        //
        // 1. Put a decoration on some horizontally off-screen text (i.e. later in a long line)
        // 2. Scroll so the decoration is visible
        // 3. Click off the window
        //
        // Without this code, the scroll will be reset but the decoration will still be drawn. This
        // is because the scroll position in TextState remains set when unfocused, but the actual
        // visible scroll is reset.
        if (!ImGui.IsItemActive()) {
            scroll.X = 0;
        }

        ImGui.BeginChild(label);
        var drawList = ImGui.GetWindowDrawList();
        ImGui.EndChild();

        var clipMin = ImGui.GetItemRectMin();
        var clipMax = ImGui.GetItemRectMax();
        drawList.PushClipRect(clipMin, clipMax);

        foreach (var decoration in decorations) {
            // Figure out the position of this decoration
            var posInText = ImGuiExt.InputTextCalcText2dPos(text, decoration.StartIndex);
            var startIndex = Math.Clamp(decoration.StartIndex, 0, text.Length);
            var endIndex = Math.Clamp(decoration.EndIndex, 0, text.Length);
            DrawDecoration(drawList, posInText, text[startIndex..endIndex], decoration);
        }

        drawList.PopClipRect();
    }

    private void DrawDecoration(ImDrawListPtr drawList, Vector2 posInText, string textSlice, InputTextDecoration decoration) {
        var style = ImGui.GetStyle();
        var min = ImGui.GetItemRectMin() + style.FramePadding;
        var max = ImGui.GetItemRectMax();

        var fontSize = ImGui.GetFontSize();
        if (decoration is InputTextDecoration.TextColor textColor) {
            var pos = min + posInText - scroll;
            drawList.AddText(ImGui.GetFont(), fontSize, pos, textColor.Col, textSlice);
        } else if (decoration is InputTextDecoration.HoverTooltip hoverTooltip) {
            var textSize = ImGui.CalcTextSize(textSlice);
            var topLeft = min + posInText - scroll;
            var bottomRight = min + posInText + textSize;

            // If the mouse is in our region, and we are focused then we want to draw our tooltip
            if (ImGui.IsItemHovered() && ImGui.IsMouseHoveringRect(topLeft, bottomRight)) {
                var text = hoverTooltip.Text.Value;
                if (text.HasValue) {
                    ImGui.BeginTooltip();
                    ImGuiHelpers.SeStringWrapped(text.Value, new SeStringDrawParams() {
                        WrapWidth = 640 * ImGuiHelpers.GlobalScale
                    });
                    ImGui.EndTooltip();
                }
            }
        }
    }
}

public interface InputTextDecoration {
    int StartIndex { get; }
    int EndIndex { get; }

    public record class TextColor(int StartIndex, int EndIndex, uint Col) : InputTextDecoration {}
    public record class HoverTooltip(int StartIndex, int EndIndex, Lazy<ReadOnlySeString?> Text) : InputTextDecoration {}
}
