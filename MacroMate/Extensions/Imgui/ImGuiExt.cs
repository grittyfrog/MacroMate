using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;

namespace MacroMate.Extensions.Imgui;

public static class ImGuiExt {
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
}
