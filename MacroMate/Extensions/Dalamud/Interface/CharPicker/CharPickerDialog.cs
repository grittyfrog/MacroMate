using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MacroMate.Extensions.Dalamud.Interface.CharPicker;

/// Class to pick a character from a list of possible characters
public class CharPickerDialog : Window {
    public static readonly string NAME = "Char Picker";

    private Action<char>? Callback { get; set; }
    private List<char> Choices { get; set; } = new();

    /// <summary>
    /// Counts the number of inserts since this window gained focus.
    ///
    /// Resets when focus is lost or when the window is opened.
    /// </summary>
    public int ConsecutiveInserts { get; private set; } = 0;

    public CharPickerDialog() : base(NAME) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Open(
        List<char> choices,
        Action<char> callback
    ) {
        Choices = choices;
        Callback = callback;
        ConsecutiveInserts = 0;
        if (!IsOpen) {
            IsOpen = true;
        } else {
            ImGui.SetWindowFocus(WindowName);
        }
    }

    public override void Draw() {
        var buttonSize = 36 * ImGuiHelpers.GlobalScale;
        var columns = (int)((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / (buttonSize + ImGui.GetStyle().ItemSpacing.X));

        if (!IsFocused) { ConsecutiveInserts = 0; }

        ImGuiClip.ClippedDraw(Choices, (choice) => {
            var text = (choice).ToString();
            ImGui.SetWindowFontScale(1.3f);
            if (ImGui.Button(text, new Vector2(36 * ImGuiHelpers.GlobalScale))) {
                if (Callback != null) {
                    Callback(choice);
                    ConsecutiveInserts += 1;
                }
            }
            ImGui.SetWindowFontScale(1);
        }, columns, lineHeight: buttonSize);
    }
}
