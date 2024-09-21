using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Extensions.Imgui;

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

        void DrawButton(char c, bool hlFav) {
            ImGui.SetWindowFontScale(1.3f);
            if (ImGui.Button(c.ToString(), new Vector2(36 * ImGuiHelpers.GlobalScale))) {
                if (Callback != null) {
                    Callback(c);
                    ConsecutiveInserts += 1;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                if (Env.MacroConfig.CharPickerDialogFavourites.Contains(c)) {
                    Env.MacroConfig.CharPickerDialogFavourites.Remove(c);
                } else {
                    Env.MacroConfig.CharPickerDialogFavourites.Add(c);
                }
                Env.MacroConfig.NotifyEdit();
            }
            if (hlFav && Env.MacroConfig.CharPickerDialogFavourites.Contains(c)) {
                ImGuiExt.ItemBorder(
                    ImGui.ColorConvertFloat4ToU32(Colors.HighlightGold),
                    rounding: ImGui.GetStyle().FrameRounding,
                    thickness: 3.0f
                );
            }
            ImGui.SetWindowFontScale(1);
        }

        var favourtes = Choices.Where(choice => Env.MacroConfig.CharPickerDialogFavourites.Contains(choice)).ToList();

        // Draw Favourites first
        ImGui.TextUnformatted("Favourites");
        ImGuiExt.HoverTooltip("Right click to add/remove to favourites");
        ImGui.Separator();
        ImGuiClip.ClippedDraw(favourtes, (choice) => { DrawButton(choice, hlFav: false); }, columns, lineHeight: buttonSize);

        ImGui.NewLine();

        ImGui.TextUnformatted("All");
        ImGui.Separator();
        ImGuiClip.ClippedDraw(Choices, (choice) => { DrawButton(choice, hlFav: true); }, columns, lineHeight: buttonSize);

    }
}
