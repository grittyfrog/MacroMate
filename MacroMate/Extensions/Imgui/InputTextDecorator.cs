using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace MacroMate.Extensions.Imgui;


/// High hackery to decorate an MultiLineTextInput
public class InputTextDecorator {
    private Vector2 scroll = new Vector2(0, 0);

    private unsafe ImGuiInputTextState* TextState =>
        (ImGuiInputTextState*)(ImGui.GetCurrentContext() + ImGuiInputTextState.TextStateOffset);

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
        unsafe {
            scroll.X = TextState->ScrollX;
            ImGui.BeginChild(label);
            scroll.Y = ImGui.GetScrollY();
            ImGui.EndChild();
        }

        // Prevent showing misplaced decorations when:
        //
        // 1. Put a decoration on some horizontally off-screen text (i.e. later in a long line)
        // 2. Scroll so the decoration is visible
        // 3. Click off the window
        //
        // Without this code, the scroll will be reset but the decoration will still be drawn. This
        // is because the scroll position in TextState remains set when unfocused, but the actual
        // visible scroll is reset.
        if (!ImGui.IsItemFocused()) {
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
            var posInText = InputTextCalcText2dPos(text, decoration.StartIndex);
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
        }
    }

    private Vector2 InputTextCalcText2dPos(
        string text,
        int textPos
    ) {
        var font = ImGui.GetFont();
        float lineHeight = ImGui.GetFontSize();
        float scale = lineHeight / font.FontSize;

        Vector2 textSize = new Vector2(0, 0);
        float lineWidth = 0.0f;

        int sIndex = 0;
        while (sIndex < textPos && sIndex < text.Length)
        {
            char c = text[sIndex];
            sIndex += 1;
            if (c == '\n')
            {
                textSize.X = 0;
                textSize.Y += lineHeight;
                lineWidth = 0.0f;
                continue;
            }
            if (c == '\r')
                continue;

            // ImGui.NET doesn't allow us to pass 32-bit wchar like the native implementation does, so instead
            // we need to account for surrogate pairs ourselves or the width gets misaligned
            // 0xE0F0 0x00BB   0xE0F00BB
            float charWidth = font.GetCharAdvance((ushort)c);
            lineWidth += charWidth;
        }


        if (textSize.X < lineWidth)
            textSize.X = lineWidth;

        return textSize;
    }
}

public interface InputTextDecoration {
    int StartIndex { get; }
    int EndIndex { get; }

    public record class TextColor(int StartIndex, int EndIndex, uint Col) : InputTextDecoration {}
}
