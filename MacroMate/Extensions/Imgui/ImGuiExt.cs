using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;

namespace MacroMate.Extensions.Imgui;

public static class ImGuiExt {
    /// <summary>
    /// Returns the screen position of the text at `textPos` in `text` assuming it was drawn in
    /// an InputTextMultiline
    /// </summary>
    public static Vector2 InputTextCalcText2dPos(string text, int textPos) {
        var font = ImGui.GetFont();
        var fontScale = ImGui.GetIO().FontGlobalScale;
        float lineHeight = ImGui.GetFontSize();

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
            float charWidth = font.GetCharAdvance((ushort)c) * fontScale;
            lineWidth += charWidth;
        }


        if (textSize.X < lineWidth)
            textSize.X = lineWidth;

        return textSize;
    }

    public static void HelpMarker(string text) {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// Applies a border over the previous item
    public static void ItemBorder(
        uint col,
        float rounding = 1.0f,
        float thickness = 1.0f
    ) {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRect(
            ImGui.GetItemRectMin(),
            ImGui.GetItemRectMax(),
            col,
            rounding,
            ImDrawFlags.None,
            thickness
        );
    }

    public static void EnumCombo<T>(string label, ref T currentValue) where T : Enum {
        if (ImGui.BeginCombo(label, currentValue.ToString())) {
            foreach (var enumValue in Enum.GetValues(typeof(T)).OfType<T>()) {
                if (ImGui.Selectable(enumValue.ToString(), enumValue.Equals(currentValue))) {
                    currentValue = enumValue;
                }
            }
            ImGui.EndCombo();
        }
    }

    public static void StringCombo(string label, IEnumerable<string> values, ref string currentValue) {
        if (ImGui.BeginCombo(label, currentValue.ToString())) {
            foreach (var strValue in values) {
                if (ImGui.Selectable(strValue, strValue.Equals(currentValue))) {
                    currentValue = strValue;
                }
            }
            ImGui.EndCombo();
        }
    }

    public static void NullableStringCombo(
        string label,
        string nullValue,
        IEnumerable<string?> values,
        ref string? currentValue
    ) {
        if (ImGui.BeginCombo(label, currentValue?.ToString() ?? nullValue)) {
            foreach (var strValue in values) {
                if (ImGui.Selectable(strValue ?? nullValue, strValue == currentValue)) {
                    currentValue = strValue;
                }
            }
            ImGui.EndCombo();
        }
    }

    public static bool InputTextInt(string label, ref int value) {
        string buffer = value.ToString();
        bool inputTextEdited = ImGui.InputText(label, ref buffer, 255, ImGuiInputTextFlags.CharsDecimal);
        if (inputTextEdited) {
            if (int.TryParse(buffer, out var parsedInt)) {
                value = parsedInt;
            }
        }
        return inputTextEdited;
    }

    public static void HoverTooltip(string? message) {
        if (message != null && ImGui.IsItemHovered()) {
            ImGui.SetTooltip(message);
        }
    }

    public static ImGuiInputTextCallback CallbackCharFilterFn(Func<char, bool> predicate) {
        unsafe {
            return (ev) => {
                return predicate((char)ev->EventChar) ? 0 : 1;
            };
        }
    }

    public static bool IsNotNull(this ImGuiPayloadPtr payload) {
        unsafe {
            return payload.NativePtr != null;
        }
    }

    /// <summary>
    /// Draws text to the left of the current cursor and positions the cursor at the start of the text.
    ///
    /// Leaves the cursor on the same line
    /// </summary>
    public static void TextUnformattedHorizontalRTL(string text) {
        var textSize = ImGui.CalcTextSize(text);
        var startPos = ImGui.GetCursorPosX() - textSize.X;
        ImGui.SetCursorPosX(startPos);
        ImGui.TextUnformatted(text);
        ImGui.SameLine(startPos - ImGui.GetStyle().FramePadding.X);
    }

    /// <summary>
    /// Move the cursor such that we are ready to draw an item of `itemWidth` at the RHS ofa table cell.
    /// </summary>
    public static void SetCursorTableRHS(float itemWidth) {
        var newX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - itemWidth
            - ImGui.GetScrollX() - (2 * ImGui.GetStyle().ItemSpacing.X);
        if (newX > ImGui.GetCursorPosX()) {
            ImGui.SetCursorPosX(newX);
        }
    }

    /// ImGui.NET doesn't provide any way to pass ImGuiWindowFlags to BeginPopupModal
    /// without also passing the isOpen bool ref.
    ///
    /// So lets steal the code and implement it here.
    ///
    /// See also:
    /// - https://github.com/ImGuiNET/ImGui.NET/issues/135
    /// - https://github.com/ImGuiNET/ImGui.NET/blob/379d6300cf082cd559b6a44c185fb8597af56c88/src/ImGui.NET/Generated/ImGui.gen.cs#L1002
    public static bool BeginPopupModal(string name, ImGuiWindowFlags flags) {
        unsafe {
            // See: https://github.com/ImGuiNET/ImGui.NET/blob/379d6300cf082cd559b6a44c185fb8597af56c88/src/ImGui.NET/Util.cs#L9
            var stackAllocationSizeLimit = 2048;

            byte* native_name;
            int name_byteCount = 0;
            if (name != null) {
                name_byteCount = Encoding.UTF8.GetByteCount(name);
                if (name_byteCount > stackAllocationSizeLimit) {
                    native_name = (byte*)Marshal.AllocHGlobal(name_byteCount + 1);
                } else {
                    byte* native_name_stackBytes = stackalloc byte[name_byteCount + 1];
                    native_name = native_name_stackBytes;
                }

                int native_name_offset;
                if (name == "") { native_name_offset = 0; }
                else {
                    fixed (char* utf16Ptr = name) {
                        native_name_offset =
                            Encoding.UTF8.GetBytes(utf16Ptr, name.Length, native_name, name_byteCount);
                    }
                }

                native_name[native_name_offset] = 0;
            }
            else { native_name = null; }
            byte* p_open = null;
            byte ret = ImGuiNative.igBeginPopupModal(native_name, p_open, flags);
            if (name_byteCount > stackAllocationSizeLimit) {
                Marshal.FreeHGlobal((IntPtr)native_name);
            }
            return ret != 0;
        }
    }

    public static void Spinner(string label, float radius, float thickness, uint color) {
        var style = ImGui.GetStyle();
        ImGui.PushID(label);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.FramePadding.Y);
        var size = new Vector2(radius * 2, radius * 2);

        ImGui.Dummy(size);
        var dummyPos = ImGui.GetItemRectMin();
        var dummySize = ImGui.GetItemRectSize();
        var center = new Vector2(
            dummyPos.X + (dummySize.X / 2),
            dummyPos.Y + (dummySize.Y / 2)
        );

        // Render
        ImGui.GetWindowDrawList().PathClear();

        var numSegments = 30;
        var start = Math.Abs(Math.Sin(ImGui.GetTime() * 1.8f) * (numSegments - 5));

        var aMin = Math.PI * 2.0f * ((float)start / (float)numSegments);
        var aMax = Math.PI * 2.0f * (((float)numSegments - 3) / (float)numSegments);

        for (var i = 0; i < numSegments; ++i) {
            var a = aMin + ((float)i / (float)numSegments) * (aMax - aMin);
            ImGui.GetWindowDrawList().PathLineTo(
                new Vector2(
                    center.X + (float)Math.Cos(a + (float)ImGui.GetTime() * 8) * (radius - thickness / 2),
                    center.Y + (float)Math.Sin(a + (float)ImGui.GetTime() * 8) * (radius - thickness / 2)
                )
            );
        }

        ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, thickness);

        ImGui.PopID();
    }

    public static void SpinnerRTL(string label, float radius, int thickness, uint color) {
        var style = ImGui.GetStyle();
        var spinnerSize = new Vector2(radius * 2, (radius + style.FramePadding.Y) * 2); // Keep in sync with Spinner()
        var startPos = ImGui.GetCursorPosX() - spinnerSize.X;
        ImGui.SetCursorPosX(startPos);
        Spinner(label, radius, thickness, color);
        ImGui.SameLine(startPos - ImGui.GetStyle().FramePadding.X);
    }

    public static void YesIcon() {
        var yesIconAvailable = Env.TextureProvider.TryGetFromGameIcon(60081, out var yesIconTexture);
        if (yesIconAvailable && yesIconTexture != null) {
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
            ImGui.Image(
                yesIconTexture.GetWrapOrEmpty().ImGuiHandle,
                new Vector2(ImGui.GetTextLineHeight()),
                new Vector2(0, 0),
                new Vector2(1, 1),
                Colors.ActiveGreen
            );

        } else {
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("✓");
        }
    }
}
