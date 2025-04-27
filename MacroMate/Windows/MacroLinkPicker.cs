
using System.Numerics;
using ImGuiNET;
using MacroMate.MacroTree;
using MacroMate.Windows.Components;

namespace MacroMate.Windows;

/// Macro Link Picker window, mainly used for "Edit All" since
/// the indnividual macro linking is embedded in the MacroWindow.
public class MacroLinkPicker : EventWindow<MacroLink> {
    public static readonly string NAME = "Macro Link Picker";

    /// This is the macro link we are editing, which will be copied out
    /// when apply is hit.
    private MacroLink macroLink = new MacroLink();

    public MacroLinkPicker() : base(NAME, ImGuiWindowFlags.AlwaysAutoResize) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (MacroLinkEditor.DrawEditor(ref macroLink)) {
            EnqueueEvent(macroLink);
        }
    }

    public void ShowOrFocus(string scopeName) {
        CurrentEventScope = scopeName;
        macroLink = new MacroLink();

        Env.PluginWindowManager.ShowOrFocus(this);
    }
}
