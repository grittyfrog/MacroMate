using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Imgui;
using MacroMate.MacroTree;

namespace MacroMate.Windows.Components;

public static class MacroLinkEditor {
    private static Vector2 IconSize => ImGuiHelpers.ScaledVector2(32.0f);
    private static int iconColumns = 10;
    private static int iconRows = 10;

    public static float Width => (IconSize.X * iconColumns) + (iconColumns * ImGui.GetStyle().ItemSpacing.X);

    public static bool DrawEditor(ref MacroLink macroLink) {
        bool edited = false;

        ImGui.SetNextItemWidth(Width);
        if (ImGui.BeginTable("vanillaMacroSetTable", 2, ImGuiTableFlags.SizingStretchSame)) {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Selectable("Individual", macroLink.Set == VanillaMacroSet.INDIVIDUAL)) {
                macroLink.Set = VanillaMacroSet.INDIVIDUAL;
                macroLink.Slots = new();
                edited = true;
            }

            ImGui.TableNextColumn();
            if (ImGui.Selectable("Shared", macroLink.Set == VanillaMacroSet.SHARED)) {
                macroLink.Set = VanillaMacroSet.SHARED;
                macroLink.Slots = new();
                edited = true;
            }

            ImGui.PopStyleVar();
            ImGui.EndTable();
        }

        ImGui.SetNextItemWidth(Width);
        if (ImGui.BeginTable("vanillaMacroSlotTable", 10, ImGuiTableFlags.SizingFixedSame)) {
            uint macroSlot = 0;
            foreach (var row in Enumerable.Range(0, iconRows)) {
                ImGui.TableNextRow();
                foreach (var col in Enumerable.Range(1, iconColumns)) {
                    ImGui.TableNextColumn();
                    using var _font = Env.FontManager.Axis18.Push();

                    if (DrawMacroButtion(macroSlot, ref macroLink)) {
                        edited = true;
                    }

                    macroSlot += 1;
                }
            }

            ImGui.EndTable();
        }

        return edited;
    }

    private static bool DrawMacroButtion(
        uint macroSlot,
        ref MacroLink macroLink
    ) {
        bool edited = false;

        var linkedMateNodes = Env.MacroConfig.LinkedNodesFor(macroLink.Set, macroSlot);
        var vanillaMacro = Env.VanillaMacroManager.GetMacro(macroLink.Set, macroSlot);

        Vector4 buttonColor;
        if (linkedMateNodes.Count > 0) {
            buttonColor = Colors.FinalFantasyBlue;
        } else if (vanillaMacro.IsDefined()) {
            buttonColor = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Button));
        } else {
            buttonColor = Vector4.Zero;
        }

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        if (ImGui.Button(macroSlot.ToString(), ImGuiHelpers.ScaledVector2(32.0f, 32.0f))) {
            BindSlot(ref macroLink, macroSlot);
            edited = true;
        }
        ImGui.PopStyleColor();

        // Draw the gold border if selected
        if (macroLink.Slots.Contains(macroSlot)) {
            ImGuiExt.ItemBorder(
                ImGui.ColorConvertFloat4ToU32(Colors.HighlightGold),
                rounding: ImGui.GetStyle().FrameRounding,
                thickness: 3.0f
            );
        }

        if (ImGui.IsItemHovered()) {
            if (linkedMateNodes.Count > 0) {
                var tooltipSb = new StringBuilder();
                tooltipSb.AppendLine("-- Linked Macros --");
                foreach (var linkedNode in linkedMateNodes) {
                    tooltipSb.AppendJoin(" » ", linkedNode.Path().Skip(1).Select(pn => pn.Name));
                    tooltipSb.AppendLine();
                }

                ImGui.SetTooltip(tooltipSb.ToString());
            } else if (vanillaMacro.IsDefined()) {
                var tooltipSb = new StringBuilder();
                tooltipSb.AppendLine(vanillaMacro.Title.IfEmpty("<Unnamed>"));

                var macroText = vanillaMacro.Lines.Value;
                if (macroText.IsNotEmpty()) {
                    tooltipSb.Append("\n");
                    tooltipSb.Append(macroText);
                }

                ImGui.SetTooltip(tooltipSb.ToString());
            }
        }

        return edited;
    }

    private static void BindSlot(ref MacroLink macroLink, uint slot) {
        if (macroLink.Slots.Contains(slot)) {
            macroLink.Slots.Remove(slot);
        } else {
            macroLink.Slots.Add(slot);
        }
    }
}
