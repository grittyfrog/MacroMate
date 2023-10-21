
using System.Linq;
using ImGuiNET;
using MacroMate.MacroTree;

namespace MacroMate.Windows.Components;

public static class MacroRunMenuItem {
    public static void Draw(
        MateNode.Macro macro,
        bool showLinesOnSingleRunHover,
        bool showLinesOnMultiRunHover
    ) {
        var vanillaMacros = macro.VanillaMacros().ToList();
        if (vanillaMacros.Count == 1) {
            if (ImGui.MenuItem("Run")) {
                // If we can fit the whole macro into a single vanilla macro then just run it
                Env.VanillaMacroManager.ExecuteMacro(vanillaMacros.First());
            }
            if (ImGui.IsItemHovered() && showLinesOnSingleRunHover) {
                ImGui.SetTooltip(vanillaMacros.First().Lines.Value);
            }
        } else {
            if (ImGui.BeginMenu("Run")) {
                foreach (var vanillaMacro in vanillaMacros) {
                    if (ImGui.MenuItem(vanillaMacro.Title)) {
                        Env.VanillaMacroManager.ExecuteMacro(vanillaMacro);
                    }

                    if (ImGui.IsItemHovered() && showLinesOnMultiRunHover) {
                        ImGui.SetTooltip(vanillaMacro.Lines.Value);
                    }
                }
                ImGui.EndMenu();
            }
        }
    }
}
